using System;
using System.Runtime.Caching;
using System.Web.Mvc;

namespace DevTrends.MvcDonutCaching.Demo.Controllers
{
    public partial class HomeController : ApplicationController
    {
        public virtual ActionResult Index()
        {
            return RedirectToAction("Simple");
        }

        //
        // GET: /Home/
        [DonutOutputCache(Duration = 24 * 3600)]
        public virtual ActionResult Simple()
        {
            return View(DateTime.Now);
        }

        [ChildActionOnly, DonutOutputCache(Duration = 60)]
        public virtual ActionResult SimpleDonutOne()
        {
            return PartialView(DateTime.Now);
        }

        [ChildActionOnly]
        public virtual ActionResult SimpleDonutTwo()
        {
            return PartialView(DateTime.Now);
        }

        public virtual ActionResult ExpireSimpleDonutCache()
        {
            OutputCacheManager.RemoveItem("Home", "Simple");

            return Content("OK", "text/plain");
        }

        public virtual ActionResult ExpireSimpleDonutOneCache()
        {
            OutputCacheManager.RemoveItem("Home", "SimpleDonutOne");

            return Content("OK", "text/plain");
        }

    }
}
