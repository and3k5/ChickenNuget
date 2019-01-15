namespace ChickenNuget.Data
{
    public interface IChickenNugetProject
    {
        IProjectFile[] NugetPackageConfigs { get; }
        IProjectFile[] NuspecFiles { get; }
    }
}