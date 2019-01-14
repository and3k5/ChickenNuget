using System.Collections.Generic;
using ChickenNuget.Data.Config;

namespace ChickenNuget.Data
{
    public interface IProjectSource
    {
        IDictionary<string,ParameterSpecs> GetAuthenticationKeys();
        IDictionary<string,ParameterSpecs> GetParameterKeys();
        void InitializeSettings(Configuration config);
        IProjectReference[] GetAllProjects();
        IProjectFile[] GetAllNugetPackagesConfig(IProjectReference reference);
        CacheControl GetCacheControl();
        IProjectFile ConstructProjectFile(IDictionary<string, string> data);
        Dictionary<string,string> PackProjectFile(IProjectFile projectFile);
    }
}