using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RestSharp;

namespace ChickenNuget.Data
{
    public class NugetFeedReader
    {
        private readonly string _feedUrl;
        private readonly ICollection<NugetFeedPackage> _collection;

        public NugetFeedReader(string feedUrl) : this(feedUrl, null)
        {
        }

        private NugetFeedReader(string feedUrl, List<NugetFeedPackage> collection)
        {
            _feedUrl = feedUrl;
            _collection = collection ?? new List<NugetFeedPackage>();
        }

        public void FetchPackages()
        {
            if (_collection.Count > 0)
                throw new Exception("Already fetched nuget packages");

            var xmlDocument = LoadXmlDocument();

            var xmlNamespaceManager = CreateNamespaceManager(xmlDocument);

            var nugetPackageNodes = xmlDocument.SelectNodes("/atom:feed/atom:entry", xmlNamespaceManager);

            foreach (XmlNode nugetPackageNode in nugetPackageNodes)
            {
                var id = nugetPackageNode.SelectSingleNode("atom:title/text()", xmlNamespaceManager).Value;
                var version = nugetPackageNode.SelectSingleNode("m:properties/d:Version/text()", xmlNamespaceManager).Value;
                var isPrerelease = bool.Parse(nugetPackageNode.SelectSingleNode("m:properties/d:IsPrerelease/text()", xmlNamespaceManager).Value);
                var dependencies = nugetPackageNode.SelectSingleNode("m:properties/d:Dependencies/text()", xmlNamespaceManager)?.Value.Split('|').Select(s =>
                {
                    var idVersion = s.Split(':');
                    var dependency = new NugetDependency(idVersion[0], idVersion[1]);
                    return (INugetPackage) dependency;
                }).ToArray() ?? new INugetPackage[0];
                var isLatestVersion = bool.Parse(nugetPackageNode.SelectSingleNode("m:properties/d:IsLatestVersion/text()", xmlNamespaceManager).Value);
                var isAbsoluteLatestVersion = bool.Parse(nugetPackageNode.SelectSingleNode("m:properties/d:IsAbsoluteLatestVersion/text()", xmlNamespaceManager).Value);

                _collection.Add(new NugetFeedPackage(id, version, isPrerelease, dependencies, isLatestVersion, isAbsoluteLatestVersion));
            }
        }

        public void FilterToLatestPackages()
        {
            for (var i = 0; i < _collection.Count; i++)
            {
                var package = _collection.ElementAt(i);
                if (!package.IsLatestVersion)
                {
                    _collection.Remove(package);
                    i--;
                }
            }
        }

        public IEnumerable<NugetFeedPackage> GetCurrentCollection()
        {
            foreach (var nugetFeedPackage in _collection)
                yield return nugetFeedPackage;
        }

        public NugetFeedReader CloneResult()
        {
            return new NugetFeedReader(_feedUrl, new List<NugetFeedPackage>(_collection.Select(x => (NugetFeedPackage) x.Clone())));
        }

        private XmlDocument LoadXmlDocument()
        {
            var restClient = new RestClient(_feedUrl);
            var request = new RestRequest("Packages");

            var response = restClient.Execute(request);

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(response.Content);
            return xmlDocument;
        }

        private static XmlNamespaceManager CreateNamespaceManager(XmlDocument xmlDocument)
        {
            var xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
            xmlNamespaceManager.AddNamespace("atom", xmlDocument.DocumentElement.GetAttribute("xmlns"));

            foreach (XmlAttribute attribute in xmlDocument.DocumentElement.Attributes)
                if (attribute.Prefix == "xmlns")
                    xmlNamespaceManager.AddNamespace(attribute.LocalName, attribute.Value);
            return xmlNamespaceManager;
        }
    }
}