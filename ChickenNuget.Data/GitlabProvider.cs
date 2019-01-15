using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ChickenNuget.Data.Config;
using Newtonsoft.Json;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace ChickenNuget.Data
{
    public class GitlabProvider : ProjectSource
    {
        private const string RestApiBaseUrl = "https://gitlab.com/api/v4/";
        private const string ChkngtProjectFilePath = "project.chkngt.json";
        public override IDictionary<string, ParameterSpecs> GetAuthenticationKeys() => new Dictionary<string, ParameterSpecs>() {{"private_token", new ParameterSpecs() {Required = true, Description = "Token to authenticate gitlab",}}};
        public override IDictionary<string, ParameterSpecs> GetParameterKeys() => new Dictionary<string, ParameterSpecs>() {{"group", new ParameterSpecs() {Required = true, Description = "Group name to get all projects by",}}};

        public override void InitializeSettings(Configuration config)
        {
            base.InitializeSettings(config);
            PrivateToken = config.Authentication["private_token"];
            Group = config.Parameters["group"];
            AuthorName = config.Parameters["author_name"];
            AuthorEmailAddress = config.Parameters["author_email"];
        }

        public string Group { get; private set; }
        private string PrivateToken { get; set; }
        public string AuthorName { get; private set; }
        public string AuthorEmailAddress { get; private set; }

        private class ProjectReference : IProjectReference
        {
            public ProjectReference(int id, string name, string url, string repoUrl)
            {
                Id = id;
                Name = name;
                Url = url;
                RepoUrl = repoUrl;
            }

            public IChickenNugetProject ChickenNugetProject = null;

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

        public override IProjectReference[] GetAllProjects(bool clearCache)
        {
            var cache = SimpleCache<int, IProjectReference[]>.CreateCache("Source-AllProjects");
            if (!clearCache)
            {
                var cacheObj = cache.Get(this.Config.Id);
                if (cacheObj != null)
                    return cacheObj;
            }

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

            var projectReferences = list.ToArray();

            cache.Insert(Config.Id, projectReferences);

            return projectReferences;
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
            public ProjectFile(string id, string name, string type, string path)
            {
                Id = id;
                Name = name;
                Type = type;
                Path = path;
            }

            public string Id { get; }
            public string Name { get; }
            public string Type { get; }
            public string Path { get; }
            public string FilePath() => Path;
        }

        public override Dictionary<IProjectFile, NugetDependency[]> GetAllNugetDependencies(IProjectReference reference, bool clearCache)
        {
            var cache = SimpleCache<Tuple<int, string>, Dictionary<IProjectFile, NugetDependency[]>>.CreateCache("Source-Project-AllNugetDependencies");
            if (!clearCache)
            {
                var cacheObj = cache.Get(new Tuple<int, string>(this.Config.Id, reference.GetIdentifier()));
                if (cacheObj != null)
                    return cacheObj;
            }

            var result = new Dictionary<IProjectFile, NugetDependency[]>();
            var packageFiles = GetAllNugetPackagesConfig(reference, clearCache);
            if (packageFiles == null || packageFiles.Length == 0)
                return result;

            var client = CreateRestClient();

            foreach (var packageFile in packageFiles)
            {
                var file = (ProjectFile) packageFile;
                var request = CreateFileReadRequest((ProjectReference) reference, file.Path, "master");
                var response = client.Execute(request);

                if (response.ErrorException != null)
                    throw new Exception("Failed to read " + file.Path + " from " + reference.GetName(), response.ErrorException);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    throw new Exception("Failed to read " + file.Path + " from " + reference.GetName() + ": 404 not found");

                result.Add(file, PackagesConfigReader.Parse(response.Content).ToArray());
            }

            cache.Insert(new Tuple<int, string>(this.Config.Id, reference.GetIdentifier()), result);

            return result;
        }

        public override Dictionary<IProjectFile, NugetDefinition> GetAllNugetDefinitions(IProjectReference reference, bool clearCache)
        {
            var cache = SimpleCache<Tuple<int, string>, Dictionary<IProjectFile, NugetDefinition>>.CreateCache("Source-Project-AllNugetDefinitions");
            if (!clearCache)
            {
                var cacheObj = cache.Get(new Tuple<int, string>(this.Config.Id, reference.GetIdentifier()));
                if (cacheObj != null)
                    return cacheObj;
            }

            var result = new Dictionary<IProjectFile, NugetDefinition>();
            var packageFiles = GetAllNugetSpecFiles(reference, clearCache);
            if (packageFiles == null || packageFiles.Length == 0)
                return result;

            var client = CreateRestClient();

            foreach (var packageFile in packageFiles)
            {
                var file = (ProjectFile) packageFile;
                var request = CreateFileReadRequest((ProjectReference) reference, file.Path, "master");
                var response = client.Execute(request);

                if (response.ErrorException != null)
                    throw new Exception("Failed to read " + file.Path + " from " + reference.GetName(), response.ErrorException);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    throw new Exception("Failed to read " + file.Path + " from " + reference.GetName() + ": 404 not found");

                if (file.Name.Split('.').Last().Equals("nuspec", StringComparison.OrdinalIgnoreCase))
                    result.Add(file, PackageNuspecReader.ParseNuspec(response.Content));
                else if (file.Name.Split('.').Last().Equals("csproj", StringComparison.OrdinalIgnoreCase))
                    result.Add(file, PackageNuspecReader.ParseCsproj(response.Content));
                else
                    throw new Exception("Could not handle nuspec file: " + file.Name);
            }

            cache.Insert(new Tuple<int, string>(this.Config.Id, reference.GetIdentifier()), result);

            return result;
        }

        public override IChickenNugetProject ReadChickenNugetProject(IProjectReference reference, bool clearCache)
        {
            var cache = SimpleCache<Tuple<int, string>, IChickenNugetProject>.CreateCache("Source-Project-ChickenNugetProject");
            if (!clearCache)
            {
                var cacheObj = cache.Get(new Tuple<int, string>(this.Config.Id, reference.GetIdentifier()));
                if (cacheObj != null)
                    return cacheObj;
            }

            var project = (ProjectReference) reference;
            if (project.ChickenNugetProject != null)
                return project.ChickenNugetProject;

            var client = CreateRestClient();
            var request = CreateFileReadRequest(project, ChkngtProjectFilePath, "master");
            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (response.ErrorException != null)
                throw new Exception("Failed to read ChickenNuget file", response.ErrorException);

            var json = response.ToJsonObject();

            var configsJson = (JArray) json["files"]["packages-config"];
            var nuspecsJson = (JArray) json["files"]["package-nuspec"];

            var configs = configsJson?.Select(x => (IProjectFile) CreateProjectFileFromPath(reference, x.Value<string>())).ToArray();
            var nuspecs = nuspecsJson?.Select(x => (IProjectFile) CreateProjectFileFromPath(reference, x.Value<string>())).ToArray();

            var chickenNugetProject = new ChickenNugetProject(configs ?? new IProjectFile[0], nuspecs ?? new IProjectFile[0]);

            project.ChickenNugetProject = chickenNugetProject;

            cache.Insert(new Tuple<int, string>(this.Config.Id, reference.GetIdentifier()), chickenNugetProject);

            return chickenNugetProject;
        }

        private RestRequest CreateFileReadRequest(ProjectReference project, string filePath, string reference)
        {
            var request = new RestRequest("/projects/{id}/repository/files/{file_path}/raw", Method.GET, DataFormat.Json);
            request.Parameters.Add(new Parameter("id", project.Id, ParameterType.UrlSegment));
            request.Parameters.Add(new Parameter("file_path", filePath, ParameterType.UrlSegment));
            request.Parameters.Add(new Parameter("ref", reference, ParameterType.QueryString));
            return request;
        }

        public override void CreateChickenNugetProject(IProjectReference proj)
        {
            if (ReadChickenNugetProject(proj, false) != null)
                throw new Exception("Already has file");

            var packageConfigs = this.GetAllNugetPackagesConfig(proj, false);
            var nuspecs = this.GetAllNugetSpecFiles(proj, false);

            if (packageConfigs.Length == 0 && nuspecs.Length == 0)
                return;

            var project = (ProjectReference) proj;
            var client = CreateRestClient();
            var request = new RestRequest("/projects/{id}/repository/files/{file_path}", Method.POST, DataFormat.Json);
            request.Parameters.Add(new Parameter("id", project.Id, ParameterType.UrlSegment));
            request.Parameters.Add(new Parameter("file_path", ChkngtProjectFilePath, ParameterType.UrlSegment));

            request.Parameters.Add(new Parameter("branch", "master", ParameterType.GetOrPost));
            request.Parameters.Add(new Parameter("author_email", AuthorEmailAddress, ParameterType.GetOrPost));
            request.Parameters.Add(new Parameter("author_name", AuthorName, ParameterType.GetOrPost));
            request.Parameters.Add(new Parameter("content", AssembleChickenNugetProject(packageConfigs, nuspecs), ParameterType.GetOrPost));
            request.Parameters.Add(new Parameter("commit_message", "Create chicken nuget file.", ParameterType.GetOrPost));

            var response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.Created)
                throw new Exception("Failed to create file");
        }

        private string AssembleChickenNugetProject(IProjectFile[] packageConfigs, IProjectFile[] nuspecs)
        {
            var writer = new JTokenWriter();

            // main obj
            writer.WriteStartObject();

            // files obj
            writer.WritePropertyName("files");
            writer.WriteStartObject();

            if (packageConfigs.Length > 0)
            {
                // packages-config array
                writer.WritePropertyName("packages-config");
                writer.WriteStartArray();
                foreach (var packageConfig in packageConfigs)
                {
                    writer.WriteValue(packageConfig.FilePath());
                }

                writer.WriteEndArray();
            }

            if (nuspecs.Length > 0)
            {
                // packages-config array
                writer.WritePropertyName("package-nuspec");
                writer.WriteStartArray();
                foreach (var projectFile in nuspecs)
                {
                    writer.WriteValue(projectFile.FilePath());
                }

                writer.WriteEndArray();
            }

            // files obj
            writer.WriteEndObject();

            // main obj
            writer.WriteEndObject();

            return writer.Token.ToString(Formatting.Indented);
        }

        private ProjectFile CreateProjectFileFromPath(IProjectReference reference, string path)
        {
            var project = (ProjectReference) reference;
            var client = CreateRestClient();
            var request = new RestRequest("/projects/{id}/repository/files/{file_path}", Method.GET, DataFormat.Json);
            request.Parameters.Add(new Parameter("id", project.Id, ParameterType.UrlSegment));
            request.Parameters.Add(new Parameter("file_path", path, ParameterType.UrlSegment));
            request.Parameters.Add(new Parameter("ref", "master", ParameterType.QueryString));

            var json = client.Execute(request).ToJsonObject();

            var id = json["blob_id"].Value<string>();
            var name = json["file_name"].Value<string>();
            var type = "blob";
            var fPath = json["file_path"].Value<string>();
            var projectFile = new ProjectFile(id, name, type, fPath);

            return projectFile;
        }

        public override IProjectFile[] GetAllNugetPackagesConfig(IProjectReference reference, bool clearCache)
        {
            var config = ReadChickenNugetProject(reference, clearCache);
            if (config != null) return config.NugetPackageConfigs;
            var cacheControl = this.GetCacheControl();
            var nugetFiles = cacheControl.GetCacheProjectFiles("nuget", reference);
            if (nugetFiles != null) return nugetFiles;
            var allProjectFiles = GetAllProjectFiles(reference, clearCache);
            nugetFiles = allProjectFiles.Where(x => ((ProjectFile) x).Name.Equals("packages.config", StringComparison.OrdinalIgnoreCase)).ToArray();
            cacheControl.StoreCacheProjectFiles("nuget", reference, nugetFiles);
            return nugetFiles;
        }

        public override IProjectFile[] GetAllNugetSpecFiles(IProjectReference reference, bool clearCache)
        {
            var config = ReadChickenNugetProject(reference, clearCache);
            if (config != null) return config.NuspecFiles;
            var cacheControl = this.GetCacheControl();
            var nugetFiles = cacheControl.GetCacheProjectFiles("nuspec", reference);
            if (nugetFiles != null) return nugetFiles;
            var allProjectFiles = GetAllProjectFiles(reference, clearCache);
            nugetFiles = allProjectFiles.Where(x => ((ProjectFile) x).Name.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase)).ToArray();
            cacheControl.StoreCacheProjectFiles("nuspec", reference, nugetFiles);
            return nugetFiles;
        }

        public override IProjectFile ConstructProjectFile(IDictionary<string, string> data)
        {
            var id = data["id"];
            var name = data["name"];
            var type = data["type"];
            var path = data["path"];
            var projectFile = new ProjectFile(id, name, type, path);
            return projectFile;
        }

        public override Dictionary<string, string> PackProjectFile(IProjectFile projectFile)
        {
            var pFile = (ProjectFile) projectFile;
            return new Dictionary<string, string>() {{"id", pFile.Id}, {"name", pFile.Name}, {"type", pFile.Type}, {"path", pFile.Path}};
        }

        private IProjectFile[] GetAllProjectFiles(IProjectReference reference, bool clearCache)
        {
            var cache = SimpleCache<Tuple<int, string>, IProjectFile[]>.CreateCache("Source-Project-AllProjectFiles");
            if (!clearCache)
            {
                var cacheObj = cache.Get(new Tuple<int, string>(this.Config.Id, reference.GetIdentifier()));
                if (cacheObj != null)
                    return cacheObj;
            }

            var project = (ProjectReference) reference;
            var client = CreateRestClient();
            var list = new List<IProjectFile>();
            IterateTreeWithRequests(client, project, list, null);

            var allProjectFiles = list.ToArray();
            cache.Insert(new Tuple<int, string>(this.Config.Id, reference.GetIdentifier()), allProjectFiles);
            return allProjectFiles;
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
                    switch (type)
                    {
                        case "tree":
                            //IterateTreeWithRequests(client, project, list, path);
                            break;
                        case "blob":
                            var projectFile = new ProjectFile(id, name, type, path);
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