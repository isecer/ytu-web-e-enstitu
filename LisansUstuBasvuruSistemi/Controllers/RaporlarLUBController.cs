using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.LisansustuBasvuruRapor)]
    public class RaporlarLUBController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        // GET: RaporlarLUB
        public ActionResult Index(string EKD)
        {
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
           
            ViewBag.BasvuruSurecID = new SelectList(Management.getbasvuruSurecleri(_EnstituKod, BasvuruSurecTipi.LisansustuBasvuru ,true), "Value", "Caption");
            return View();
        }
    }
}