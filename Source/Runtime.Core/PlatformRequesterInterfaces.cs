using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using System;

namespace Beamable.Api
{
	public interface IPlatformRequester : IRequester
	{
		AccessToken Token { get; set; }
		string TimeOverride { get; set; }
		new string Cid { get; set; }
		new string Pid { get; set; }

		IAuthApi AuthService { set; }
		void DeleteToken();
	}

	public interface IPlatformRequesterFactory
	{
		IPlatformRequester Create(string cid);
		IPlatformRequester Create(string cid, string pid);
	}


}
