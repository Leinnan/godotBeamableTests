using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Server.Common;
using Godot;
using Newtonsoft.Json;
using HttpClient = System.Net.Http.HttpClient;

namespace GodotBeamable.BeamGodot {
	public class GodotRequester : IBeamableRequester
	{
		static bool Validator (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, X509Chain chain,
							   SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}
		private readonly IAppContext _ctx;
		public IAccessToken AccessToken => _ctx.Token;
		public string Pid => AccessToken.Pid;
		public string Cid => AccessToken.Cid;

		public GodotRequester()
		{
			ServicePointManager.ServerCertificateValidationCallback = Validator;
			_ctx = BaseGodotContext.Instance;
			GD.Print("Created GodotRequester");
		}

		public async Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true,
										   Func<string, T> parser = null,
										   bool useCache = false)
		{
			GD.Print($"{method} call: {uri}");
			HttpClient client = GetClient(includeAuthHeader, AccessToken?.Pid ?? Pid, AccessToken?.Cid ?? Cid, AccessToken);
			var request = PrepareRequest(method, _ctx.Host, uri, body);
			GD.Print($"Calling: {request}");


			var result = await client.SendAsync(request);

			GD.Print($"RESULT: {result}");

			T parsed = default(T);
			if (result.Content != null)
			{
				Stream stream = await result.Content.ReadAsStreamAsync();
				var reader = new StreamReader(stream, Encoding.UTF8);
				var rawResponse = await reader.ReadToEndAsync();
			
				if (typeof(T) == typeof(string) && rawResponse is T response)
				{
					return response;
				}
				if (parser != null)
				{
					// if there is a custom parser, use that.
					parsed = parser(rawResponse);
				}
				else
				{
					// otherwise use JSON
					parsed = JsonConvert.DeserializeObject<T>(rawResponse, UnitySerializationSettings.Instance);
				}
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

		private static HttpClient GetClient(bool includeAuthHeader, string pid, string cid, IAccessToken token)
		{
			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("contentType", "application/json"); // confirm that it is required

			if (!string.IsNullOrEmpty(cid))
			{
				client.DefaultRequestHeaders.Add("X-KS-CLIENTID", cid);
			}
			if (!string.IsNullOrEmpty(pid))
			{
				client.DefaultRequestHeaders.Add("X-KS-PROJECTID", pid);
			}

			if (includeAuthHeader && !string.IsNullOrWhiteSpace(token?.Token))
			{
				client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
			}

			return client;
		}

		// public async Promise<T> Request2<T>(Method method, string uri, object body = null, bool includeAuthHeader = true,
		// 									Func<string, T> parser = null,
		// 									bool useCache = false)
		// {
		// 	GD.Print($"Request {uri}");
		// 	Error err;
		// 	HTTPClient http = new HTTPClient();
		//
		// 	err = http.ConnectToHost(_ctx.Host, useSsl: true); // Connect to host/port.
		// 	if (err != Error.Ok)
		// 	{
		// 		GD.PrintErr(err.ToString());
		// 	}
		//
		// 	Debug.Assert(err == Error.Ok); // Make sure the connection is OK.
		//
		// 	// Wait until resolved and connected.
		// 	while (http.GetStatus() == HTTPClient.Status.Connecting || http.GetStatus() == HTTPClient.Status.Resolving)
		// 	{
		// 		http.Poll();
		// 		GD.Print("Connecting...");
		// 		OS.DelayMsec(500);
		// 	}
		//
		// 	GD.Print($"STATUS: {http.GetStatus()}");
		//
		// 	var bodyString = body == null ? null :JsonConvert.SerializeObject(body);
		// 	List<string> headers = new List<string>();
		// 	if (body != null)
		// 	{
		// 		headers.Add("contentType: application/json");
		// 		headers.Add($"Content-Length: {bodyString.Length}");
		// 	}
		//
		// 	if (!string.IsNullOrEmpty(Cid))
		// 	{
		// 		headers.Add($"X-KS-CLIENTID: {Cid}");
		// 	}
		//
		// 	if (!string.IsNullOrEmpty(Pid))
		// 	{
		// 		headers.Add($"X-KS-PROJECTID: {Pid}");
		// 	}
		//
		// 	if (includeAuthHeader && !string.IsNullOrWhiteSpace(AccessToken?.Token))
		// 	{
		// 		headers.Add($"Authorization: Bearer {AccessToken.Token}");
		// 	}
		//
		// 	GD.Print($"BODY: {bodyString}");
		// 	GD.Print($"Headers: {JsonConvert.SerializeObject(headers)}");
		//
		// 	GD.Print("Perform call...");
		// 	if(body != null)
		// 	{
		// 		err = http.Request(ToGodotMethod(method), uri, headers.ToArray(), bodyString); // Request a page from the site.
		// 	}
		// 	else
		// 	{
		// 		err = http.Request(ToGodotMethod(method), uri, headers.ToArray()); // Request a page from the site.
		// 	}
		// 	GD.Print($"Call result: {err.ToString()}");
		// 	Debug.Assert(err == Error.Ok); // Make sure all is OK.
		// 	T parsed = default(T);
		// 	GD.Print($"Call has response: {http.HasResponse()}");
		// 	GD.Print("Code: ", http.GetResponseCode()); // Show response code.
		// 	if (http.HasResponse())
		// 	{
		// 		headers = http.GetResponseHeaders().ToList(); // Get response headers.
		// 		GD.Print("Code: ", http.GetResponseCode()); // Show response code.
		// 		GD.Print("Headers:");
		// 		foreach (string header in headers)
		// 		{
		// 			// Show headers.
		// 			GD.Print(header);
		// 		}
		//
		// 		if (http.IsResponseChunked())
		// 		{
		// 			// Does it use chunks?
		// 			GD.Print("Response is Chunked!");
		// 		}
		// 		else
		// 		{
		// 			// Or just Content-Length.
		// 			GD.Print("Response Length: ", http.GetResponseBodyLength());
		// 		}
		//
		// 		// This method works for both anyways.
		// 		List<byte> rb = new List<byte>(); // List that will hold the data.
		//
		// 		// While there is data left to be read...
		// 		while (http.GetStatus() == HTTPClient.Status.Body)
		// 		{
		// 			http.Poll();
		// 			byte[] chunk = http.ReadResponseBodyChunk(); // Read a chunk.
		// 			if (chunk.Length == 0)
		// 			{
		// 				// If nothing was read, wait for the buffer to fill.
		// 				OS.DelayMsec(500);
		// 			}
		// 			else
		// 			{
		// 				// Append the chunk to the read buffer.
		// 				rb.AddRange(chunk);
		// 			}
		// 		}
		//
		// 		// Done!
		// 		GD.Print("Bytes Downloaded: ", rb.Count);
		// 		string rawResponse = Encoding.ASCII.GetString(rb.ToArray());
		// 		GD.Print($"RAW RESPONSE: {rawResponse}");
		//
		// 		if (typeof(T) == typeof(string) && rawResponse is T response)
		// 		{
		// 			return response;
		// 		}
		//
		// 		if (parser != null)
		// 		{
		// 			// if there is a custom parser, use that.
		// 			parsed = parser(rawResponse);
		// 		}
		// 		else
		// 		{
		// 			// otherwise use JSON
		// 			parsed = JSON.Parse(rawResponse).Result is T ? (T) JSON.Parse(rawResponse).Result : default;
		// 		}
		// 	}
		//
		// 	return parsed;
		// }

		public IBeamableRequester WithAccessToken(TokenResponse tokenResponse)
		{
			var requester = new GodotRequester();
			requester._ctx.UpdateToken(tokenResponse);
			return requester;
		}

		public string EscapeURL(string url) => Uri.EscapeUriString(url);

		// private static HTTPClient.Method ToGodotMethod(Method method)
		// {
		// 	switch (method)
		// 	{
		// 		case Method.GET:
		// 			return HTTPClient.Method.Get;
		// 		case Method.POST:
		// 			return HTTPClient.Method.Post;
		// 		case Method.PUT:
		// 			return HTTPClient.Method.Put;
		// 		case Method.DELETE:
		// 			return HTTPClient.Method.Delete;
		// 		default:
		// 			throw new ArgumentOutOfRangeException(nameof(method), method, null);
		// 	}
		// }
	}
}
