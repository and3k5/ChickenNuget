namespace ChickenNuget.Data
{
    public class NugetDefinition : INugetInformation
    {
        public NugetDefinition(string id)
        {
            Id = id;
        }

        public string Id { get; }
    }
}