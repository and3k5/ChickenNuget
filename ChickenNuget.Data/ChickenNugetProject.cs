namespace ChickenNuget.Data
{
    public class ChickenNugetProject : IChickenNugetProject {
        public IProjectFile[] NugetPackageConfigs { get; }
        public IProjectFile[] OnlyNuspecFiles { get; }
    }
}