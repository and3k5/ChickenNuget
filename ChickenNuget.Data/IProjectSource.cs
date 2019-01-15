using System.Collections.Generic;
using ChickenNuget.Data.Config;

namespace ChickenNuget.Data
{
    public interface IProjectSource
    {
        IDictionary<string, ParameterSpecs> GetAuthenticationKeys();
        IDictionary<string, ParameterSpecs> GetParameterKeys();
        void InitializeSettings(Configuration config);
        IProjectReference[] GetAllProjects(bool clearCache);
        IProjectFile[] GetAllNugetPackagesConfig(IProjectReference reference, bool clearCache);
        CacheControl GetCacheControl();
        IProjectFile ConstructProjectFile(IDictionary<string, string> data);
        Dictionary<string, string> PackProjectFile(IProjectFile projectFile);
        IChickenNugetProject ReadChickenNugetProject(IProjectReference reference, bool clearCache);
        void CreateChickenNugetProject(IProjectReference proj);
        IProjectFile[] GetAllNugetSpecFiles(IProjectReference reference, bool clearCache);
        Dictionary<IProjectFile, NugetDependency[]> GetAllNugetDependencies(IProjectReference reference, bool clearCache);
        Dictionary<IProjectFile, NugetDefinition> GetAllNugetDefinitions(IProjectReference reference, bool clearCache);
    }
}