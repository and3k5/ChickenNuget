using System;
using System.Collections.Generic;
using ChickenNuget.Data.Config;

namespace ChickenNuget.Data
{
    public abstract class ProjectSource : IProjectSource
    {
        public abstract IDictionary<string, ParameterSpecs> GetAuthenticationKeys();
        public abstract IDictionary<string, ParameterSpecs> GetParameterKeys();

        public virtual void InitializeSettings(Configuration config)
        {
            Config = config;
        }

        protected Configuration Config;

        public abstract IProjectReference[] GetAllProjects(bool clearCache);
        public abstract IProjectFile[] GetAllNugetPackagesConfig(IProjectReference reference, bool clearCache);

        public CacheControl GetCacheControl()
        {
            return new CacheControl(this, Config);
        }

        public abstract IProjectFile ConstructProjectFile(IDictionary<string, string> data);
        public abstract Dictionary<string, string> PackProjectFile(IProjectFile projectFile);
        public abstract IProjectFile[] GetAllNugetSpecFiles(IProjectReference reference, bool clearCache);
        public abstract Dictionary<Tuple<IProjectFile, IProjectInformation>, NugetDependency[]> GetAllNugetDependencies(IProjectReference reference, bool clearCache);
        public abstract Dictionary<IProjectFile, NugetDefinition> GetAllNugetDefinitions(IProjectReference reference, bool clearCache);
        public abstract IChickenNugetProject ReadChickenNugetProject(IProjectReference reference, bool clearCache);
        public abstract void CreateChickenNugetProject(IProjectReference proj);
    }
}