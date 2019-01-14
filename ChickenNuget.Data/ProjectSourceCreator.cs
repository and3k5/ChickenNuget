using System;

namespace ChickenNuget.Data
{
    internal static class ProjectSourceCreator
    {
        public static IProjectSource Create(Config.Configuration config)
        {
            IProjectSource source;
            switch (config.ProjectSource)
            {
                default:
                    throw new Exception("Unsupported config projectsource");
                case ProjectSources.Gitlab:
                    source = new GitlabProvider();
                    break;
            }
            
            source.InitializeSettings(config);

            return source;
        }
    }
}