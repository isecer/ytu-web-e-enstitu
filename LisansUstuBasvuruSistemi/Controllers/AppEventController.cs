using System;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class AppEventController : Controller
    {
        //
        // GET: /PageNotFound/
        public ActionResult PageNotFound(string aspxerrorpath, int? errC=null)
        {
            ViewBag.SayfaAdi = aspxerrorpath;
            ViewBag.ErrC = errC;
            return View();
        }

        public ActionResult Error(string url, int errC,Exception exception)
        {
            ViewBag.SayfaAdi = url;
            ViewBag.ErrC = errC;
            return View(exception);
        }
    }
}