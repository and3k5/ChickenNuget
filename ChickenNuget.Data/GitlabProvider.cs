using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ChickenNuget.Data.Config;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace ChickenNuget.Data
{
    public class GitlabProvider : ProjectSource
    {
        private const string RestApiBaseUrl = "https://gitlab.com/api/v4/";
        public override IDictionary<string, ParameterSpecs> GetAuthenticationKeys() => new Dictionary<string, ParameterSpecs>() {{"private_token", new ParameterSpecs() {Required = true, Description = "Token to authenticate gitlab",}}};
        public override IDictionary<string, ParameterSpecs> GetParameterKeys() => new Dictionary<string, ParameterSpecs>() {{"group", new ParameterSpecs() {Required = true, Description = "Group name to get all projects by",}}};

        public override void InitializeSettings(Configuration config)
        {
            base.InitializeSettings(config);
            PrivateToken = config.Authentication["private_token"];
            Group = config.Parameters["group"];
        }

        public string Group { get; private set; }
        private string PrivateToken { get; set; }

        private class ProjectReference : IProjectReference
        {
            public ProjectReference(int id, string name, string url, string repoUrl)
            {
                Id = id;
                Name = name;
                Url = url;
                RepoUrl = repoUrl;
            }

            public int Id { get; }
            public string Name { get; }
            public string Url { get; }
            public string RepoUrl { get; }

            public string GetIdentifier()
            {
                return Id.ToString();
            }

            public string GetName() => Name;
            public string GetLink() => Url;
        }

        public override IProjectReference[] GetAllProjects()
        {
            var client = CreateRestClient();
            var list = new List<IProjectReference>();
            var page = 1;
            JArray projects;
            do
            {
                projects = client.Execute(CreateGetAllProjectsRequest(page++)).ToJsonArray();
                foreach (var project in projects)
                {
                    var id = project["id"].Value<int>();
                    var name = project["name"].Value<string>();
                    var webUrl = project["web_url"].Value<string>();
                    var repoUrl = project["http_url_to_repo"].Value<string>();
                    var reference = new ProjectReference(id, name, webUrl, repoUrl);
                    list.Add(reference);
                }
            } while (projects.Count == 100);

            return list.ToArray();
        }

        private RestRequest CreateGetAllProjectsRequest(int page)
        {
            var request = new RestRequest("groups/{username}/projects", Method.GET, DataFormat.Json);
            request.Parameters.Add(new Parameter("username", Group, ParameterType.UrlSegment));
            request.Parameters.Add(new Parameter("per_page", "100", ParameterType.QueryString));
            request.Parameters.Add(new Parameter("page", page, ParameterType.QueryString));
            return request;
        }

        private RestClient CreateRestClient()
        {
            var client = new RestClient(RestApiBaseUrl);
            client.DefaultParameters.Add(new Parameter("private_token", PrivateToken, ParameterType.QueryString));
            return client;
        }

        private RestRequest CreateGetAllNugetPackagesConfigRequest(int id, string path, int page)
        {
            var request = new RestRequest("/projects/{id}/repository/tree", Method.GET, DataFormat.Json);
            request.Parameters.Add(new Parameter("id", id, ParameterType.UrlSegment));
            request.Parameters.Add(new Parameter("per_page", "100", ParameterType.QueryString));
            request.Parameters.Add(new Parameter("recursive", "true", ParameterType.QueryString));
            request.Parameters.Add(new Parameter("page", page, ParameterType.QueryString));
            if (path != null) request.Parameters.Add(new Parameter("path", path, ParameterType.QueryString));
            return request;
        }

        private class ProjectFile : IProjectFile
        {
            public ProjectFile(string id, string name, string type, string path, string mode)
            {
                Id = id;
                Name = name;
                Type = type;
                Path = path;
                Mode = mode;
            }

            public string Id { get; }
            public string Name { get; }
            public string Type { get; }
            public string Path { get; }
            public string Mode { get; }
            public string FilePath() => Path;
        }

        public IChickenNugetProject ReadChickenNugetProject(IProjectReference reference)
        {
            var project = (ProjectReference) reference;
            var client = CreateRestClient();
            var request = new RestRequest("/projects/{id}/repository/files/{file_path}/raw", Method.GET, DataFormat.Json);
            request.Parameters.Add(new Parameter("id", project.Id, ParameterType.UrlSegment));
            request.Parameters.Add(new Parameter("file_path", "project.chkngt.yml", ParameterType.UrlSegment));
            request.Parameters.Add(new Parameter("ref", "master", ParameterType.QueryString));
            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            var chickenNugetProject = new ChickenNugetProject();

            // TODO implement read
            //chickenNugetProject.NugetPackageConfigs = 
            //chickenNugetProject.OnlyNuspecFiles = 
            return chickenNugetProject;
        }

        public override IProjectFile[] GetAllNugetPackagesConfig(IProjectReference reference)
        {
            var config = ReadChickenNugetProject(reference);
            if (config != null) return config.NugetPackageConfigs;
            var cacheControl = this.GetCacheControl();
            var nugetFiles = cacheControl.GetCacheProjectFiles("nuget", reference);
            if (nugetFiles != null) return nugetFiles;
            var allProjectFiles = GetAllProjectFiles(reference);
            nugetFiles = allProjectFiles.Where(x => ((ProjectFile) x).Name.Equals("packages.config", StringComparison.OrdinalIgnoreCase)).ToArray();
            cacheControl.StoreCacheProjectFiles("nuget", reference, nugetFiles);
            return nugetFiles;
        }

        public override IProjectFile[] GetAllNugetSpecFiles(IProjectReference reference)
        {
            var config = ReadChickenNugetProject(reference);
            if (config != null) return config.OnlyNuspecFiles;
            var cacheControl = this.GetCacheControl();
            var nugetFiles = cacheControl.GetCacheProjectFiles("nuspec", reference);
            if (nugetFiles != null) return nugetFiles;
            var allProjectFiles = GetAllProjectFiles(reference);
            nugetFiles = allProjectFiles.Where(x => ((ProjectFile) x).Name.Equals("package.nuspec", StringComparison.OrdinalIgnoreCase)).ToArray();
            cacheControl.StoreCacheProjectFiles("nuspec", reference, nugetFiles);
            return nugetFiles;
        }

        public override IProjectFile ConstructProjectFile(IDictionary<string, string> data)
        {
            var id = data["id"];
            var name = data["name"];
            var type = data["type"];
            var path = data["path"];
            var mode = data["mode"];
            var projectFile = new ProjectFile(id, name, type, path, mode);
            return projectFile;
        }

        public override Dictionary<string, string> PackProjectFile(IProjectFile projectFile)
        {
            var pFile = (ProjectFile) projectFile;
            return new Dictionary<string, string>() {{"id", pFile.Id}, {"name", pFile.Name}, {"type", pFile.Type}, {"path", pFile.Path}, {"mode", pFile.Mode},};
        }

        private IProjectFile[] GetAllProjectFiles(IProjectReference reference)
        {
            var project = (ProjectReference) reference;
            var client = CreateRestClient();
            var list = new List<IProjectFile>();
            IterateTreeWithRequests(client, project, list, null);
            return list.ToArray();
        }

        private void IterateTreeWithRequests(IRestClient client, ProjectReference project, ICollection<IProjectFile> list, string basePath)
        {
            var page = 1;
            JArray files;
            do
            {
                files = client.Execute(CreateGetAllNugetPackagesConfigRequest(project.Id, basePath, page++)).ToJsonArray();
                foreach (var file in files)
                {
                    var id = file["id"].Value<string>();
                    var name = file["name"].Value<string>();
                    var type = file["type"].Value<string>();
                    var path = file["path"].Value<string>();
                    var mode = file["mode"].Value<string>();
                    switch (type)
                    {
                        case "tree":
                            //IterateTreeWithRequests(client, project, list, path);
                            break;
                        case "blob":
                            var projectFile = new ProjectFile(id, name, type, path, mode);
                            list.Add(projectFile);
                            break;
                        default:
                            throw new Exception("Unknown tree request type: " + type);
                            break;
                    }
                }
            } while (files.Count == 100);
        }
    }
}