using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    public class RaporlarAnketController : Controller
    {
        // GET: RaporlarAnket

        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {

            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var nowDate = DateTime.Now;
            ViewBag.AnketID = new SelectList(Management.cmbGetAktifAnketler(_EnstituKod,true), "Value", "Caption", null); 

            return View();
        }
    }
}