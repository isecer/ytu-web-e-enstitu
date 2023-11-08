using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.TiAyarlari)]
    public class TiAyarlarController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        { 
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var data = _entities.TIAyarlars.Where(p => p.EnstituKod == enstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod)).OrderBy(o => o.Kategori).ThenBy(t => t.SiraNo).ToList();
            var cats = data.Select(s => new { s.Kategori, Toggle = true }).Distinct().ToList();
            var panelToggled = cats.ToDictionary(item => item.Kategori, item => item.Toggle);
            ViewBag.PanelToggled = panelToggled; 
            return View(data);
        }
        [HttpPost]
        public ActionResult Index(List<string> ayarAdi, List<string> ayarDegeri, List<string> panelToggled, string ekd)
        {
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var qSistemAyarAdi = ayarAdi.Select((s, index) => new { inx = index, s }).ToList();
            var qSistemAyarDegeri = ayarDegeri.Select((s, index) => new { inx = index, s }).ToList();

            var qModel = (from sa in qSistemAyarAdi
                          join sad in qSistemAyarDegeri on sa.inx equals sad.inx
                          select new
                          {
                              RowID = sa.inx,
                              AyarAdi = sa.s,
                              AyarDegeri = sad.s,
                          }).ToList();
            foreach (var item in qModel)
            {
                var ayar = _entities.TIAyarlars.FirstOrDefault(p => p.AyarAdi == item.AyarAdi && p.EnstituKod == enstituKod);
                if (ayar != null)
                {
                    ayar.AyarDegeri = item.AyarDegeri;
                }
            }
            _entities.SaveChanges();
            MessageBox.Show("Tez İzleme Ayarları Güncellendi", MessageBox.MessageType.Success);
            var data = _entities.TIAyarlars.Where(p => p.EnstituKod == enstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod)).OrderBy(o => o.Kategori).ThenBy(t => t.SiraNo).ToList();
            var toggled = new Dictionary<string, bool>();
            foreach (var item in panelToggled)
            {
                var ptg = item.Replace("__", "◘").Split('◘');
                toggled.Add(ptg[0], ptg[1].ToBoolean().Value);
            }
            ViewBag.PanelToggled = toggled; 
            return View(data);
        }
    }
}