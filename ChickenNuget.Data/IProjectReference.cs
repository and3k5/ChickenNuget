namespace ChickenNuget.Data
{
    public interface IProjectReference
    {
        string GetIdentifier();
        string GetName();
        string GetLink();
    }
}