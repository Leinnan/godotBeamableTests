using System.Collections.Generic;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Godot;
using Newtonsoft.Json;

namespace GodotBeamable.BeamGodot
{
	public class BaseGodotContext : IAppContext
	{
		public static BaseGodotContext Instance { get; } = new BaseGodotContext();
		public string Cid => _cid;
		public string Pid => _pid;
		public string Host => _host;
		public IAccessToken Token => _token;
		private BaseToken _token;
		private string _cid;
		private string _pid;
		private string _host;

		public BaseGodotContext()
		{
			var files = FileAccess.Open("res://.beamable/config-defaults.json", FileAccess.ModeFlags.Read);

			string text = files.GetAsText();
			files.Close();

			Dictionary<string, string> ParsedData = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
			foreach (var key in ParsedData.Keys)
			{
				GD.Print($"{key}: {ParsedData[key]}");
			}
			_cid = ParsedData["cid"];
			_pid = ParsedData["pid"];
			_host = ParsedData["host"];

			_token = new BaseToken(ParsedData["access_token"], ParsedData["refresh_token"], Cid, Pid);
			Set(Cid, Pid, Host);
			GD.Print("Created TestGodotContext");
		}

		public void Set(string cid, string pid, string host)
		{
			_cid = cid;
			_pid = pid;
			_host = host;
			_token.Cid = _cid;
			_token.Pid = _pid;
		}

		public void UpdateToken(TokenResponse response)
		{
			_token.Token = response.access_token;
			_token.RefreshToken = response.refresh_token;
		}
	}
}
