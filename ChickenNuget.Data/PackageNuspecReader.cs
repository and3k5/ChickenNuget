using System.Xml;

namespace ChickenNuget.Data
{
    public static class PackageNuspecReader
    {
        public static NugetDefinition ParseNuspec(string Content)
        {
            var packagesXml = new XmlDocument();
            packagesXml.LoadXml(Content.RemoveBOMCharacter());

            var xmlNamespaceManager = new XmlNamespaceManager(packagesXml.NameTable);
            xmlNamespaceManager.AddNamespace("nuspec", packagesXml.DocumentElement.GetAttribute("xmlns"));

            var id = (XmlText) packagesXml.DocumentElement.SelectSingleNode("/nuspec:package/nuspec:metadata/nuspec:id/text()", xmlNamespaceManager);

            return new NugetDefinition(id.Value);
        }

        public static NugetDefinition ParseCsproj(string Content)
        {
            var packagesXml = new XmlDocument();
            packagesXml.LoadXml(Content.RemoveBOMCharacter());

            var xmlNamespaceManager = new XmlNamespaceManager(packagesXml.NameTable);
            xmlNamespaceManager.AddNamespace("msbuild", packagesXml.DocumentElement.GetAttribute("xmlns"));

            var id = (XmlText) packagesXml.DocumentElement.SelectSingleNode("/msbuild:Project/msbuild:PropertyGroup/msbuild:AssemblyName/text()", xmlNamespaceManager);

            return new NugetDefinition(id.Value);
        }
    }
}