namespace ChickenNuget.Data
{
    public class NugetDependency : INugetInformation, INugetPackage
    {
        public NugetDependency(string id, string version)
        {
            Id = id;
            Version = version;
        }

        public string Id { get; }
        public string Version { get; }
    }
}