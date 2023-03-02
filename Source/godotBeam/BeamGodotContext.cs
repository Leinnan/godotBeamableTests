using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Dependencies;
using Godot;
using Newtonsoft.Json;

namespace GodotBeamable.BeamGodot
{
	public class BeamGodotContext : IUserContext
	{
		public Dictionary<string, string> Stats { get; private set; }
		public long UserId { get; private set; }
		public IBeamableRequester Requester => _beamableRequester;
		public IDependencyProvider Services => _dependencyProvider;
		public IAuthApi Auth => _authApi;

		private BaseToken _token;
		private readonly string _playerCode;
		private IBeamableRequester _beamableRequester;
		private IDependencyProvider _dependencyProvider;
		private readonly IAuthApi _authApi;

		private BeamGodotContext(BaseToken token, string playerCode)
		{
			_dependencyProvider = Beam.DependencyBuilder.Build();
			_token = token;
			_playerCode = playerCode;
			TokenResponse tokenResponse = new TokenResponse()
			{
				access_token = token.Token, refresh_token = token.RefreshToken
			};
			_beamableRequester = Services.GetService<IBeamableRequester>().WithAccessToken(tokenResponse);
			_authApi = new AuthApi(_beamableRequester);
		}

		public static string GetPathForCode(string playerCode = null)
		{
			return $"user://beamable_user_{(string.IsNullOrWhiteSpace(playerCode) ? "default" : playerCode)}.json";
		}

		public static bool HasUserConfig(string playerCode = null)
		{
			var exists = FileAccess.FileExists(GetPathForCode(playerCode));
			return exists;
		}

		public static BaseToken ReadToken(string playerCode = null)
		{
			var file = FileAccess.Open(GetPathForCode(playerCode), FileAccess.ModeFlags.Read);

			var content = file.GetAsText();
			file.Close();
			var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
			var token = new BaseToken(new TokenResponse()
										  {access_token = dict["access_token"], refresh_token = dict["refresh_token"]});
			return token;
		}

		public static BeamGodotContext Get(string playerCode = null)
		{
			var token = ReadToken(playerCode);
			var ctx = new BeamGodotContext(token, playerCode);
			return ctx;
		}

		public static BeamGodotContext Instantiate(BaseToken token, string playerCode = null)
		{
			var ctx = new BeamGodotContext(token, playerCode);
			ctx.Save();
			return ctx;
		}

		public async Promise RefreshToken()
		{
			var tokenResponse = await Auth.LoginRefreshToken(_token.RefreshToken);
			_token = new BaseToken(tokenResponse);
			_beamableRequester = _beamableRequester.WithAccessToken(tokenResponse);
			Save();
		}

		private void Save()
		{
			var dict = new Dictionary<string, string>()
			{
				{"access_token", _token.Token},
				{"refresh_token", _token.RefreshToken},
				{"user_id", UserId.ToString()},
			};
			var content = JsonConvert.SerializeObject(dict);
			var file = FileAccess.Open(GetPathForCode(_playerCode), FileAccess.ModeFlags.Write);
			file.StoreString(content);
			file.Close();
		}


		public static async Task<BeamGodotContext> GetOrCreate()
		{
			BeamGodotContext context;
			if (HasUserConfig())
			{
				GD.Print("Read user info...");
				context = Get();
				await context.RefreshToken();
			}
			else
			{
				GD.Print("Create new user...");
				IBeamableRequester requester = new GodotRequester();
				var auth = new AuthApi(requester);
				var tokenResponse = await auth.CreateUser();
				context = Instantiate(new BaseToken(tokenResponse));
			}

			var userInfo = await context.Auth.GetUser();
			context.UserId = userInfo.id;
			await context.GetStatsFromServer();

			return context;
		}

		public bool TryGetStatFromCache(string key, out string value)
		{
			bool hasValue = Stats.TryGetValue(key, out value);
			return hasValue;
		}

		public Promise<EmptyResponse> SetPublicStat(string key, string value) => SetStat("public", key, value);

		public Promise<EmptyResponse> SetStat(string access, string key, string value)
		{
			Dictionary<string, string> stats = new Dictionary<string, string>
			{
				{key, value}
			};
			return Requester.SetStats(UserId, access, stats);
		}

		public async Promise GetStatsFromServer()
		{
			var stats = (await Requester.GetStats(UserId)).ToDictionary();
			if (stats.ContainsKey(UserId))
			{
				Stats = stats[UserId];
			}
		}
	}
}
