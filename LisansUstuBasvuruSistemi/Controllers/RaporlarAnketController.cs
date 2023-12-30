using LisansUstuBasvuruSistemi.Models;
using System;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;

namespace LisansUstuBasvuruSistemi.Controllers
{
    public class RaporlarAnketController : Controller
    { 
        public ActionResult Index(string ekd)
        { 
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            ViewBag.AnketID = new SelectList(AnketlerBus.CmbGetAktifAnketler(enstituKod,true), "Value", "Caption", null); 

            return View();
        }
    }
}