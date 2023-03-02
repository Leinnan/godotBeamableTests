using Beamable.Common.Api;
using Beamable.Common.Dependencies;

namespace GodotBeamable.BeamGodot
{
	public static class Beam
	{
		public static IDependencyBuilder DependencyBuilder;
		static Beam()
		{
			DependencyBuilder = new DependencyBuilder();
			DependencyBuilder.AddSingleton<IBeamableRequester, GodotRequester>(
				provider => provider.GetService<GodotRequester>());
			DependencyBuilder.AddSingleton<GodotRequester, GodotRequester>();
		}
	}
}