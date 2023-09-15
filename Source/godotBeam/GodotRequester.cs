using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Beamable.Api;
using Beamable.Api.Autogenerated.Auth;
using Beamable.Api.Autogenerated.Models;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Config;
using Beamable.Server.Common;
using Godot;
using Newtonsoft.Json;
using UnityEngine;
using HttpClient = System.Net.Http.HttpClient;
using IAuthApi = Beamable.Common.Api.Auth.IAuthApi;
using TokenResponse = Beamable.Common.Api.Auth.TokenResponse;

namespace GodotBeamable.BeamGodot
{
    public class GodotRequester : IPlatformRequester
    {
        private readonly IDependencyProvider _provider;
        private readonly PackageVersion _beamableVersion;
        private readonly AccessTokenStorage accessTokenStorage;

        private IAuthApi _authService;
        private AccessToken _token;

        public string Language { get; set; }

        public IAuthApi AuthService
        {
            private get => _authService ??= _provider.GetService<IAuthApi>();
            set => _authService = value;
        }

        public void DeleteToken()
        {
            Token?.Delete();
            Token?.DeleteAsCustomerScoped();
            Token = null;
        }

        public IAccessToken AccessToken => Token;

        public AccessToken Token
        {
            get => _token;
            set
            {
                Debug.Log("Update token");
                _token = value;
            }
        }

        public string TimeOverride { get; set; }
        public string Host { get; set; }
        public string Cid { get; set; }
        public string Pid { get; set; }

        public Promise<T> BeamableRequest<T>(SDKRequesterOptions<T> req)
        {
            string contentType = null;
            byte[] bodyBytes = null;

            if (req.body != null)
            {
                bodyBytes = req.body is string json
                    ? Encoding.UTF8.GetBytes(json)
                    : Encoding.UTF8.GetBytes(JsonUtility.ToJson(req.body));
                contentType = "application/json";
            }

            var safetyClone = new SDKRequesterOptions<T>(req); // TODO: since its a struct, do we need to do this? 
            return MakeRequest<T>(contentType, bodyBytes, safetyClone);
        }

        private Promise<T> MakeRequest<T>(string _, byte[] bodyBytes, SDKRequesterOptions<T> safetyClone)
        {
            return Request<T>(safetyClone.method, safetyClone.Uri, bodyBytes, safetyClone.includeAuthHeader);
        }

        public GodotRequester(IDependencyProvider provider)
        {
            var resolver = provider.GetService<IPlatformRequesterHostResolver>();
            _provider = provider;
            Host = resolver.Host;
            _beamableVersion = resolver.PackageVersion;
            accessTokenStorage = provider.GetService<AccessTokenStorage>();
            GD.Print("Created GodotRequester");
        }

        public Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true, Func<string, T> parser = null,
            bool useCache = false)
        {
            return CustomRequest(method, uri, body, includeAuthHeader, parser, false).
                RecoverWith(error =>
                {
                    switch (error)
                    {
                        case RequesterException e when e.RequestError.error is "TimeOutError":
                            Debug.LogWarning("Timeout error, retrying in few seconds... ");
                            return Task.Delay(TimeSpan.FromSeconds(5)).ToPromise().FlatMap(_ =>
                                Request<T>(method, uri, body, includeAuthHeader, parser, useCache));
                        case RequesterException e when e.RequestError.error is "InvalidTokenError" or "ExpiredTokenError" ||
                                                       e.Status == 403 ||
                                                       (!string.IsNullOrWhiteSpace(AccessToken.RefreshToken) &&
                                                        AccessToken.ExpiresAt < DateTime.Now):
                            Debug.Log(
                                "Got failure for token " + AccessToken.Token + " because " + e.RequestError.error);
                            return AuthService.LoginRefreshToken(AccessToken.RefreshToken).Map(token =>
                                {
                                    Token = new AccessToken(accessTokenStorage, Cid, Pid, token.access_token, token.refresh_token,
                                        token.expires_in);
                                    Token.Save();
                                    return PromiseBase.Unit;
                                })
                                .FlatMap(_ => Request<T>(method, uri, body, includeAuthHeader, parser, useCache));
                    }

                    return Promise<T>.Failed(error);
                });
        }

        public async Promise<T> CustomRequest<T>(Method method, string uri, object body = null, bool includeAuthHeader = true,
            Func<string, T> parser = null, bool customerScoped = false, IEnumerable<string> customHeaders = null)
        {
            GD.Print($"{method} call: {uri}");
            using HttpClient client = GetClient(includeAuthHeader, AccessToken?.Pid ?? Pid, AccessToken?.Cid ?? Cid,
                AccessToken, customerScoped);
            var request = PrepareRequest(method, Host, uri, body);
            if (customHeaders != null)
            {
                foreach (string customHeader in customHeaders)
                {
                    var headers = customHeader.Split('=');
                    if (headers.Length == 2)
                    {
                        request.Headers.Add(headers[0], headers[1]);
                    }
                }
            }
            GD.Print($"Calling: {request}");


            var result = await client.SendAsync(request);

            GD.Print($"RESULT: {result}");

            if (!result.IsSuccessStatusCode)
            {
                await using Stream stream = await result.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var rawResponse = await reader.ReadToEndAsync();
                throw new RequesterException("Cli", method.ToReadableString(), uri, (int)result.StatusCode, rawResponse);
            }

            T parsed;
            {
                await using Stream stream = await result.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var rawResponse = await reader.ReadToEndAsync();
		
                if (typeof(T) == typeof(string) && rawResponse is T response)
                {
                    return response;
                }

                // if there is a custom parser, use that.
                parsed = parser != null ? parser(rawResponse) :
                    // otherwise use JSON
                    JsonConvert.DeserializeObject<T>(rawResponse, UnitySerializationSettings.Instance);
            }
            return parsed;
        }

        static HttpMethod FromMethod(Method method)
        {
            switch (method)
            {
                case Method.GET:
                    return HttpMethod.Get;
                case Method.POST:
                    return HttpMethod.Post;
                case Method.PUT:
                    return HttpMethod.Put;
                case Method.DELETE:
                    return HttpMethod.Delete;
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, null);
            }
        }

        private static HttpRequestMessage PrepareRequest(Method method, string basePath, string uri, object body = null)
        {
            var request = new HttpRequestMessage(FromMethod(method), basePath + uri);

            if (body == null)
            {
                return request;
            }

            if (body is string s)
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(s);
                request.Content = new ByteArrayContent(bodyBytes);
            }
            else
            {
                var ss = JsonConvert.SerializeObject(body, UnitySerializationSettings.Instance);
                request.Content = new StringContent(ss, Encoding.UTF8, "application/json");
            }

            return request;
        }

        private static HttpClient GetClient(bool includeAuthHeader, string pid, string cid, IAccessToken token, bool customerScoped)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("contentType", "application/json"); // confirm that it is required
            client.DefaultRequestHeaders.Add("X-DE-SCOPE", customerScoped ? cid : $"{cid}.{pid}");

            Debug.Log($"Adding token[{token?.Token}]: {includeAuthHeader && !string.IsNullOrWhiteSpace(token?.Token)}");
            if (includeAuthHeader && !string.IsNullOrWhiteSpace(token?.Token))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
            }

            return client;
        }

        public IBeamableRequester WithAccessToken(TokenResponse token)
        {
            Token = new AccessToken(accessTokenStorage, Cid, Pid, token.access_token, token.refresh_token,
                token.expires_in);
            Token.Save();
            return this;
        }

        public async Promise RefreshToken()
        {
            var rsp = await _provider.GetService<IBeamAuthApi>().PostRefreshToken(new RefreshTokenAuthRequest
            {
                refreshToken = new OptionalString(Token.RefreshToken), customerId = new OptionalString(Cid),
                realmId = new OptionalString(Pid)
            });
            Token = new AccessToken(accessTokenStorage, Token.Cid, Token.Pid, rsp.accessToken,
                rsp.refreshToken, long.MaxValue - 1);
            Token.Save();
        }

        public string EscapeURL(string url) => Uri.EscapeDataString(url);
    }
}