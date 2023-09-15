using Beamable.Common;

namespace Beamable.Config;

public class EnvironmentData : IPlatformRequesterHostResolver
{
    public string Host => apiUrl;
    public string Environment => _version.IsNightly ? "dev" : "prod";
    public string ApiUrl => apiUrl;
    public PackageVersion PackageVersion => _version;
    
    private readonly string apiUrl;
    private readonly PackageVersion _version = PackageVersion.FromSemanticVersionString("1.18.0");

    public EnvironmentData()
    {
        ConfigDatabase.Init();
        apiUrl = ConfigDatabase.GetString("host");
    }
}
public interface IPlatformRequesterHostResolver
{
    string Host { get; }
    PackageVersion PackageVersion { get; }
}