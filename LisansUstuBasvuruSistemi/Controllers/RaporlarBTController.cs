using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.BelgeTalepleriRapor)]
    public class RaporlarBtController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        // GET: RaporlarBelgeTalep
        public ActionResult Index(string ekd)
        {
            var eKod = EnstituBus.GetSelectedEnstitu(ekd);
            var btBaslangicTarihi = _entities.BelgeTalepleris.Where(p => p.EnstituKod == eKod).OrderBy(s => s.TalepTarihi).FirstOrDefault();
            var btBitisTarihi = _entities.BelgeTalepleris.Where(p => p.EnstituKod == eKod).OrderByDescending(s => s.TalepTarihi).FirstOrDefault();
            var t1 = new List<CmbStringDto>();
            var t2 = new List<CmbStringDto>();


            var listTar = new List<DateTime>();
            string t1Selected = "";
            string t2Selected = "";
            if (btBaslangicTarihi != null)
            {
                for (DateTime i = btBaslangicTarihi.TalepTarihi; i <= btBitisTarihi.TalepTarihi; i = i.AddMonths(1))
                {
                    listTar.Add(i);
                    var tar = i.ToString("yyyy-MM");
                    t1.Add(new CmbStringDto { Value = tar, Caption = tar });
                    t2.Add(new CmbStringDto { Value = tar, Caption = tar });
                }
                var t1SVal = listTar.Where(p => p.Year == DateTime.Now.Year).OrderBy(o => o.Month).First();
                t1Selected = t1SVal.ToString("yyyy-MM");
                var t2SVal = listTar.Where(p => p.Year == DateTime.Now.Year).OrderByDescending(o => o.Month).First();
                t2Selected = t2SVal.ToString("yyyy-MM"); 
            }
            ViewBag.BaslangicTarihi = new SelectList(t1, "Value", "Caption", t1Selected);
            ViewBag.BitisTarihi = new SelectList(t2, "Value", "Caption", t2Selected);
            ViewBag.EnstituKod = eKod;
            return View();
        }
    }
}