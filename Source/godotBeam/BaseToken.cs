using System;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace GodotBeamable.BeamGodot
{
	public class BaseToken : IAccessToken
	{
		public BaseToken(TokenResponse response, string cid = null, string pid = null)
		{
			Token = response?.access_token ?? string.Empty;
			RefreshToken = response?.refresh_token ?? string.Empty;
			Cid = cid ?? BaseGodotContext.Instance.Cid;
			Pid = pid ?? BaseGodotContext.Instance.Pid;
			ExpiresAt = DateTime.FromFileTimeUtc(response?.expires_in ?? 0);
		}

		public BaseToken(string accessToken, string refreshToken, string cid, string pid)
		{
			Token = accessToken;
			RefreshToken = refreshToken;
			Cid = cid;
			Pid = pid;
			ExpiresAt = DateTime.Now + TimeSpan.FromMinutes(3);
		}
		public string Token { get; set; }
		public string RefreshToken { get; set; }
		public DateTime ExpiresAt { get; }
		public string Cid { get; set; }
		public string Pid { get; set; }
	}
}