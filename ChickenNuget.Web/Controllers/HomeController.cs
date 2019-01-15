using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                var model = new List<Tuple<IProjectReference, Dictionary<IProjectFile, NugetDependency[]>, Dictionary<IProjectFile, NugetDefinition>>>();

                var projects = source.GetAllProjects(false);

                var tasks = new List<Task>();

                foreach (var project in projects)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        var nugetDep = source.GetAllNugetDependencies(project, false);
                        var nugetDef = source.GetAllNugetDefinitions(project, false);

                        if (nugetDep.Count > 0 || nugetDep.Count > 0)
                            model.Add(new Tuple<IProjectReference, Dictionary<IProjectFile, NugetDependency[]>, Dictionary<IProjectFile, NugetDefinition>>
                                (project, nugetDep, nugetDef));
                    }));
                }

                Task.WhenAll(tasks).Wait();

                return View(model);
            }

            return View((object) null);
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