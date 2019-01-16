using System;

namespace ChickenNuget.Data
{
    public class NugetFeedPackage : INugetPackage, INugetInformation, ICloneable
    {
        public NugetFeedPackage(string id, string version, bool isPrerelease, INugetPackage[] dependencies, bool isLatestVersion, bool isAbsoluteLatestVersion)
        {
            Id = id;
            Version = version;
            IsPrerelease = isPrerelease;
            Dependencies = dependencies;
            IsLatestVersion = isLatestVersion;
            IsAbsoluteLatestVersion = isAbsoluteLatestVersion;
        }

        public string Id { get; }
        public string Version { get; }
        public bool IsPrerelease { get; }
        public INugetPackage[] Dependencies { get; }
        public bool IsLatestVersion { get; }
        public bool IsAbsoluteLatestVersion { get; }
        public object Clone() => MemberwiseClone();
    }
}