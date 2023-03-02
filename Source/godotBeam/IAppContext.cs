using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace GodotBeamable.BeamGodot
{
	public interface IAppContext
	{
		string Cid { get; }
		string Pid { get; }
		string Host { get; }
		IAccessToken Token { get; }

		void UpdateToken(TokenResponse response);
	}
}