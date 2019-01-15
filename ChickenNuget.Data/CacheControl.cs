using System.Collections.Generic;
using System.Linq;
using ChickenNuget.Data.Config;
using LiteDB;

namespace ChickenNuget.Data
{
    public class CacheControl : DataStorageProvider
    {
        private readonly IProjectSource _source;
        private readonly Configuration _config;

        public CacheControl(IProjectSource source, Configuration config) : base(@"Cache.db")
        {
            _source = source;
            _config = config;
        }

        private static LiteCollection<ProjectFileListCache> GetProjectFileCache(LiteDatabase db)
        {
            return db.GetCollection<ProjectFileListCache>("cache");
        }

        public IProjectFile[] GetCacheProjectFiles(string key, IProjectReference project)
        {
            using (var db = CreateStorage())
            {
                var identifier = project.GetIdentifier();
                var cache = GetProjectFileCache(db).FindOne(c => c.ConfigurationId == _config.Id && c.Key == key && c.ProjectIdentifier == identifier);

                if (cache == null)
                    return null;

                var list = new List<IProjectFile>();

                foreach (var file in cache.Files)
                {
                    var projectFile = _source.ConstructProjectFile(file);
                    list.Add(projectFile);
                }

                return list.ToArray();
            }
        }

        public void StoreCacheProjectFiles(string key, IProjectReference project, IProjectFile[] value)
        {
            using (var db = CreateStorage())
            {
                var caches = GetProjectFileCache(db);
                var identifier = project.GetIdentifier();
                caches.Delete(c => c.ConfigurationId == _config.Id && c.Key == key && c.ProjectIdentifier == identifier);

                var cache = new ProjectFileListCache()
                {
                    Key = key,
                    ConfigurationId = _config.Id,
                    ProjectIdentifier = identifier,
                    Files = value.Select(x => _source.PackProjectFile(x)).ToArray(),
                };

                caches.Upsert(cache);
            }
        }

        public class ProjectFileListCache
        {
            public int Id { get; set; }
            public int ConfigurationId { get; set; }
            public string ProjectIdentifier { get; set; }
            public string Key { get; set; }
            public Dictionary<string, string>[] Files { get; set; }
        }
    }
}