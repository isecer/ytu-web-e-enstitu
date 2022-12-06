using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.SistemAyarlari)]
    public class SistemAyarlariController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            var data = db.Ayarlars.OrderBy(o => o.Kategori).ThenBy(t => t.SiraNo).ToList();
            var cats = data.Select(s => new { s.Kategori, Toggle = true }).Distinct().ToList();
            var PanelToggled = new Dictionary<string, bool>();
            foreach (var item in cats)
            {
                PanelToggled.Add(item.Kategori, item.Toggle);
            }
            ViewBag.PanelToggled = PanelToggled;
            return View(data);
        }
        [HttpPost]
        public ActionResult Index(List<string> AyarAdi, List<string> AyarDegeri, List<string> PanelToggled)
        {
            var qSistemAyarAdi = AyarAdi.Select((s, Index) => new { inx = Index, s }).ToList();
            var qSistemAyarDegeri = AyarDegeri.Select((s, Index) => new { inx = Index, s }).ToList();

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
                var ayar = db.Ayarlars.Where(p => p.AyarAdi == item.AyarAdi).FirstOrDefault();
                if (ayar != null)
                {
                    ayar.AyarDegeri = item.AyarDegeri;
                }
            }
            db.SaveChanges();
            MessageBox.Show("Sistem Ayarları Güncellendi", MessageBox.MessageType.Success);
            var data = db.Ayarlars.OrderBy(o => o.Kategori).ThenBy(t => t.SiraNo).ToList();
            var _PanelToggled = new Dictionary<string, bool>();
            foreach (var item in PanelToggled)
            {
                var _ptg = item.Replace("__","◘").Split('◘');
                _PanelToggled.Add(_ptg[0], _ptg[1].ToBoolean().Value);
            } 
            ViewBag.PanelToggled = _PanelToggled;
            return View(data);
        }

    }

}
