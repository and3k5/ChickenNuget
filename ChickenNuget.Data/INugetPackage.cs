namespace ChickenNuget.Data
{
    public interface INugetPackage : INugetInformation
    {
        string Version { get; }
        //string TargetFramework { get; }
    }
}