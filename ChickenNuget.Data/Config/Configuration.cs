using System.Collections.Generic;

namespace ChickenNuget.Data.Config
{
    public class Configuration
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ProjectSources ProjectSource { get; set; }
        public Dictionary<string,string> Authentication { get; set; }
        public Dictionary<string, string> Parameters { get;set; }
    }
}