using LiteDB;

namespace ChickenNuget.Data
{
    public abstract class DataStorageProvider
    {
        public DataStorageProvider(string databaseName)
        {
            this.DatabaseName = databaseName;
        }

        protected LiteDatabase CreateStorage()
        {
            return new LiteDatabase(this.DatabaseName);
        }

        public string DatabaseName { get; set; }
    }
}