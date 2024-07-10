using System;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class AppEventController : Controller
    {
        //
        // GET: /PageNotFound/
        public ActionResult PageNotFound(string url, int? errC = null)
        {
            ViewBag.SayfaAdi = url;
            ViewBag.ErrC = errC;
            return View();
        }

        public ActionResult Error(string url, int errC, Exception exception, string exceptionMessage)
        {
            ViewBag.SayfaAdi = url;
            ViewBag.ErrC = errC;

            if (!exceptionMessage.IsNullOrWhiteSpace()) exception = new Exception(exceptionMessage + "\r\n" + url);
            return View(exception);
        }
    }
}