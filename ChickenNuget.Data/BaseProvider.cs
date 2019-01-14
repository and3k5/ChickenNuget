using ChickenNuget.Data.Config;

namespace ChickenNuget.Data
{
    public class BaseProvider
    {
        public IProjectSource ProjectSource { get; private set; }

        public ConfigProvider ConfigProvider { get; private set; } = new ConfigProvider();

        public bool InformationStored => ConfigProvider.HasConfig();

        public void InitializeProjectSource()
        {
            var config = ConfigProvider.GetConfig((int?)null);
            if (config != null)
            {
                ProjectSource = ProjectSourceCreator.Create(config);
            }
        }
    }
}