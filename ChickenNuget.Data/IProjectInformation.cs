namespace ChickenNuget.Data
{
    public interface IProjectInformation
    {
        string AssemblyName { get; }
        string CsprojFilePath { get; }
    }
}