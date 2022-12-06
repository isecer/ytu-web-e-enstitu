using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class AppEventController : Controller
    {
        //
        // GET: /PageNotFound/
        public ActionResult PageNotFound(string aspxerrorpath, int? ErrC=null)
        {
            ViewBag.SayfaAdi = aspxerrorpath;
            ViewBag.ErrC = ErrC;
            return View();
        }

        public ActionResult Error(string url, int ErrC,Exception exception)
        {
            ViewBag.SayfaAdi = url;
            ViewBag.ErrC = ErrC;
            return View(exception);
        }
    }
}