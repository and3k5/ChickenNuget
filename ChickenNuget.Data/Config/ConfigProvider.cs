using LiteDB;

namespace ChickenNuget.Data
{
}

namespace ChickenNuget.Data.Config
{
    public class ConfigProvider : DataStorageProvider
    {
        public ConfigProvider() : base(@"Config.db")
        {
        }

        public Configuration GetConfig(int? configuration)
        {
            using (var db = CreateStorage())
            {
                var configs = Configurations(db);

                if (configuration == null)
                    return configs.FindOne(x => true);
                else
                    return configs.FindOne(x => x.Id == configuration.Value);
            }
        }

        private static LiteCollection<Configuration> Configurations(LiteDatabase db)
        {
            return db.GetCollection<Configuration>("configuration");
        }

        public void SaveConfig(Configuration config)
        {
            using (var db = CreateStorage())
            {
                var configs = Configurations(db);

                configs.Upsert(config);
            }
        }

        public bool HasConfig()
        {
            return GetConfig(null) != null;
        }
    }
}