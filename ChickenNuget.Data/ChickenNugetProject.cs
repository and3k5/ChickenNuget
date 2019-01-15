namespace ChickenNuget.Data
{
    public class ChickenNugetProject : IChickenNugetProject
    {
        public ChickenNugetProject(IProjectFile[] nugetPackageConfigs, IProjectFile[] nuspecFiles)
        {
            NugetPackageConfigs = nugetPackageConfigs;
            NuspecFiles = nuspecFiles;
        }

        public IProjectFile[] NugetPackageConfigs { get; }
        public IProjectFile[] NuspecFiles { get; }
    }
}