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
            if (ViewBag.HasConfig = BaseProvider.ConfigProvider.HasConfig())
            {
                BaseProvider.InitializeProjectSource();
                var source = BaseProvider.ProjectSource;
                var config = BaseProvider.ConfigProvider.GetConfig((int?)null);
                ViewBag.ConnectionName = config.Name;
                ViewBag.ConnectionType = config.ProjectSource.ToString("G");

                var model = new Dictionary<IProjectReference, IProjectFile[]>();
                
                var projects = source.GetAllProjects();

                var tasks = new List<Task>();

                foreach (var project in projects)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        var files = source.GetAllNugetPackagesConfig(project);
                        if (files.Length > 0)
                            model.Add(project, files);
                    }));
                }

                Task.WhenAll(tasks).Wait();

                ViewBag.NugetOverview = model;
            }
            return View();
        }

        public IActionResult Configuration(int id = 0)
        {
            var config = BaseProvider.ConfigProvider.GetConfig(id == 0 ? (int?)null : id);
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

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

