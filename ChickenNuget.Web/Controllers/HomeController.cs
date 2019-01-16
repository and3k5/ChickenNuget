using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChickenNuget.Data;
using ChickenNuget.Data.Config;
using Microsoft.AspNetCore.Mvc;
using ChickenNuget.Web.Models;

namespace ChickenNuget.Web.Controllers
{
    public class BaseController : Controller
    {
        protected BaseProvider BaseProvider = new BaseProvider();
    }

    public class HomeController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult List()
        {
            if (ViewBag.HasConfig = BaseProvider.ConfigProvider.HasConfig())
            {
                BaseProvider.InitializeProjectSource();
                var source = BaseProvider.ProjectSource;
                var config = BaseProvider.ConfigProvider.GetConfig((int?) null);
                ViewBag.ConnectionName = config.Name;
                ViewBag.ConnectionType = config.ProjectSource.ToString("G");

                var model = new List<Tuple<IProjectReference, IProjectFile[], IProjectFile[], bool>>();

                var projects = source.GetAllProjects(false);

                var tasks = new List<Task>();

                foreach (var project in projects)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        var chickenNugetProject = source.ReadChickenNugetProject(project, false);
                        var packagesConfig = source.GetAllNugetPackagesConfig(project, false);
                        var nuspecFiles = source.GetAllNugetSpecFiles(project, false);
                        if (packagesConfig.Length > 0 || nuspecFiles.Length > 0)
                            model.Add(new Tuple<IProjectReference, IProjectFile[], IProjectFile[], bool>(project, packagesConfig, nuspecFiles, chickenNugetProject != null));
                    }));
                }

                Task.WhenAll(tasks).Wait();

                ViewBag.NugetOverview = model;
            }

            return View();
        }

        public IActionResult Configuration(int id = 0)
        {
            var config = BaseProvider.ConfigProvider.GetConfig(id == 0 ? (int?) null : id);
            if (config == null)
            {
                config = new Configuration();
            }

            return View(config);
        }

        [HttpPost]
        public IActionResult Configuration(Configuration model)
        {
            new ConfigProvider().SaveConfig(model);
            return RedirectToAction("Configuration", new {id = model.Id});
        }

        public IActionResult Map()
        {
            if (ViewBag.HasConfig = BaseProvider.ConfigProvider.HasConfig())
            {
                BaseProvider.InitializeProjectSource();
                var source = BaseProvider.ProjectSource;

                var model = new List<Tuple<IProjectReference, Dictionary<Tuple<IProjectFile, IProjectInformation>, NugetDependency[]>, Dictionary<IProjectFile, NugetDefinition>>>();

                var projects = source.GetAllProjects(false);

                var tasks = new List<Task>();

                foreach (var project in projects)
                {
                    tasks.Add(FetchDependencyMapItems(source, project, model));
                }

                Task.WhenAll(tasks).Wait();

                return View(model);
            }

            return View((object) null);
        }

        private static Task FetchDependencyMapItems(IProjectSource source, IProjectReference project, List<Tuple<IProjectReference, Dictionary<Tuple<IProjectFile, IProjectInformation>, NugetDependency[]>, Dictionary<IProjectFile, NugetDefinition>>> model)
        {
            return Task.Run(() =>
            {
                try
                {
                    var nugetDep = source.GetAllNugetDependencies(project, false);
                    var nugetDef = source.GetAllNugetDefinitions(project, false);
                    // Dictionary<Tuple<IProjectFile, IProjectInformation>, NugetDependency[]>
                    if (nugetDep.Count > 0 || nugetDep.Count > 0)
                        model.Add(new Tuple<IProjectReference, Dictionary<Tuple<IProjectFile, IProjectInformation>, NugetDependency[]>, Dictionary<IProjectFile, NugetDefinition>>
                            (project, nugetDep, nugetDef));
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed for project: " + project.GetName(), ex);
                }
            });
        }

        public IActionResult ListUpdates(string nugetFeed, string[] projectFilter = null, string searchFilter = null)
        {
            projectFilter = projectFilter ?? new string[0];
            searchFilter = (searchFilter ?? "").ToLowerInvariant();
            if (ViewBag.HasConfig = BaseProvider.ConfigProvider.HasConfig())
            {
                var nugetFeedTask = Task.Run(() =>
                {
                    var nugetReader = new NugetFeedReader(nugetFeed);
                    nugetReader.FetchPackages();
                    nugetReader.FilterToLatestPackages();
                    return nugetReader.GetCurrentCollection().ToArray();
                });

                Task<Tuple<IProjectReference, Dictionary<Tuple<IProjectFile, IProjectInformation>, NugetDependency[]>>> FetchAllNugetDependencies(IProjectSource projectSource, IProjectReference project)
                {
                    return Task.Run(() =>
                    {
                        try
                        {
                            var nugetDep = projectSource.GetAllNugetDependencies(project, false);
                            return new Tuple<IProjectReference, Dictionary<Tuple<IProjectFile, IProjectInformation>, NugetDependency[]>>(project, nugetDep);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Failed for project: " + project.GetName(), ex);
                        }
                    });
                }

                BaseProvider.InitializeProjectSource();
                var source = BaseProvider.ProjectSource;
                var projectTask = Task.Run(() =>
                {
                    var projects = source.GetAllProjects(false);
                    if (!string.IsNullOrWhiteSpace(searchFilter)) projects = projects.Where(x => x.GetName().ToLowerInvariant().Contains(searchFilter)).ToArray();
                    if (projectFilter.Length > 0) projects = projects.Where(x => projectFilter.Contains(x.GetName())).ToArray();
                    var tasks = projects.Select(project => FetchAllNugetDependencies(source, project)).ToList();
                    Task.WhenAll(tasks).Wait();
                    return tasks.Select(x => x.Result).ToArray();
                });
                nugetFeedTask.Wait();
                projectTask.Wait();
                var resultString = new StringBuilder();
                foreach (var project in projectTask.Result)
                {
                    var projectReference = project.Item1;
                    var projectString = new StringBuilder();
                    foreach (var nugetDependencies in project.Item2)
                    {
                        var csprojString = new StringBuilder();
                        foreach (var nugetDependency in nugetDependencies.Value)
                        {
                            var nugetFeedPackage = nugetFeedTask.Result.SingleOrDefault(nfp => nfp.Id == nugetDependency.Id);
                            if (nugetFeedPackage == null) continue;
                            /*var projects = source.GetAllProjects(false);
                            foreach (var reference in projects)
                            {
                                var definitions = source.GetAllNugetDefinitions(reference, false);
                                foreach (var keyValuePair in definitions)
                                {
                                    keyValuePair.Value.Id ==
                                }
                            }*/
                            NugetVersion dependencyVersion;
                            try
                            {
                                dependencyVersion = NugetVersion.FromString(nugetDependency.Version);
                            }
                            catch (FormatException)
                            {
                                csprojString.AppendLine($":{nugetDependency.Id} ERROR: Invalid format [dependency] {nugetDependency.Version}");
                                continue;
                            }

                            NugetVersion nugetFeedPackageVersion;
                            try
                            {
                                nugetFeedPackageVersion = NugetVersion.FromString(nugetFeedPackage.Version);
                            }
                            catch (FormatException)
                            {
                                csprojString.AppendLine($":{nugetDependency.Id} ERROR: Invalid format [nugetFeed] {nugetFeedPackage.Version}");
                                continue;
                            }

                            if (dependencyVersion.Prerelease || dependencyVersion != nugetFeedPackageVersion)
                            {
                                csprojString.AppendLine($"{nugetDependency.Id} {nugetDependency.Version} => {nugetFeedPackage.Version}" + (dependencyVersion.Prerelease ? " [PRERELEASE]" : ""));
                                LookThroughDependencies(nugetFeedPackage, csprojString, nugetFeedTask.Result, "    - ");
                            }
                        }

                        if (csprojString.Length > 0)
                        {
                            projectString.AppendLine("Csproj: " + nugetDependencies.Key.Item2.CsprojFilePath);
                            projectString.AppendLine("    " + csprojString.ToString().Replace("\n", "\n    ").TrimEnd());
                        }
                    }

                    var str = projectString.ToString();
                    if (str.Length > 0)
                    {
                        resultString.AppendLine(projectReference.GetName());
                        resultString.AppendLine("    " + projectString.ToString().Replace("\n", "\n    ").TrimEnd());
                        resultString.AppendLine();
                    }
                }

                return Content(resultString.ToString());
            }

            return Content("Missing config/setup");
        }

        private void LookThroughDependencies(NugetFeedPackage nugetFeedPackage, StringBuilder csprojString, NugetFeedPackage[] nugetFeedPackages, string spacing)
        {
            foreach (var nugetPackage in nugetFeedPackage.Dependencies)
            {
                var nugetFeedPackageDependency = nugetFeedPackages.SingleOrDefault(p => nugetPackage.Id == p.Id);
                if (nugetFeedPackageDependency != null)
                {
                    NugetVersion dependencyVersion2;
                    NugetVersion nugetFeedPackageVersion2;
                    try
                    {
                        dependencyVersion2 = NugetVersion.FromString(nugetPackage.Version);
                        nugetFeedPackageVersion2 = NugetVersion.FromString(nugetFeedPackageDependency.Version);
                    }
                    catch (FormatException)
                    {
                        continue;
                    }

                    if (dependencyVersion2.Prerelease || dependencyVersion2 != nugetFeedPackageVersion2)
                    {
                        csprojString.AppendLine($"{spacing}{nugetPackage.Id} {nugetPackage.Version} => {nugetFeedPackageDependency.Version}" + (dependencyVersion2.Prerelease ? " [PRERELEASE]" : ""));
                        // recursive call
                        LookThroughDependencies(nugetFeedPackageDependency, csprojString, nugetFeedPackages, "    " + spacing);
                    }
                }
            }
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }

        public IActionResult CreateChickenNugetJson(string project)
        {
            if (ViewBag.HasConfig = BaseProvider.ConfigProvider.HasConfig())
            {
                var result = new System.Text.StringBuilder();

                BaseProvider.InitializeProjectSource();
                var source = BaseProvider.ProjectSource;

                var model = new List<Tuple<IProjectReference, IProjectFile[], bool>>();

                var projects = source.GetAllProjects(false);

                var tasks = new List<Task>();

                foreach (var proj in projects)
                {
                    if (proj.GetIdentifier() == project)
                    {
                        result.AppendLine("Found project");
                        source.CreateChickenNugetProject(proj);
                        break;
                    }
                }

                Task.WhenAll(tasks).Wait();

                result.AppendLine("Done");

                return Content(result.ToString());
            }

            return Content("Fail");
        }
    }
}