using System.Collections.Generic;
using System.Xml;

namespace ChickenNuget.Data
{
    public static class PackagesConfigReader
    {
        public static IEnumerable<NugetDependency> Parse(string Content)
        {
            var packagesXml = new XmlDocument();
            packagesXml.LoadXml(Content.RemoveBOMCharacter());
            foreach (XmlElement packageElement in packagesXml.DocumentElement.SelectNodes("/packages/package"))
            {
                var id = packageElement.GetAttribute("id");
                var version = packageElement.GetAttribute("version");
                //var targetFramework = packageElement.GetAttribute("targetFramework");

                yield return new NugetDependency(id, version);
            }
        }
    }
}