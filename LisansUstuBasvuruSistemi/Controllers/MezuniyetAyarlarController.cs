using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.MezuniyetAyarları)]
    public class MezuniyetAyarlarController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        private readonly InviteRenderService _inviteRenderService = new InviteRenderService();
        public ActionResult Index(string ekd)
        {
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var data = _entities.MezuniyetAyarlars.Where(p => p.EnstituKod == enstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod)).OrderBy(o => o.Kategori).ThenBy(t => t.SiraNo).ToList();
            var cats = data.Select(s => new { s.Kategori, Toggle = true }).Distinct().ToList();
            var panelToggled = new Dictionary<string, bool>();
            foreach (var item in cats)
            {
                panelToggled.Add(item.Kategori, item.Toggle);
            }
            ViewBag.PanelToggled = panelToggled;
            return View(data);
        }
        [HttpPost]
        public ActionResult Index(List<string> ayarAdi, List<string> ayarDegeri, List<string> panelToggled, string ekd)
        {

            var updateSliderData = false;

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
                var ayar = _entities.MezuniyetAyarlars.FirstOrDefault(p => p.AyarAdi == item.AyarAdi && p.EnstituKod == enstituKod);
                if (ayar != null)
                {
                    if (ayar.AyarAdi == MezuniyetAyar.TezSinaviDavetKartlariniAnaSayfadaGoster && ayar.AyarDegeri != item.AyarDegeri && item.AyarDegeri.ToBoolean()==true)
                    {
                        RederFiles(enstituKod);
                    }
                    ayar.AyarDegeri = item.AyarDegeri;
                }
            }
            _entities.SaveChanges();
            MessageBox.Show("Mezuniyet Ayarları Güncellendi", MessageBox.MessageType.Success);
            var data = _entities.MezuniyetAyarlars.Where(p => p.EnstituKod == enstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod)).OrderBy(o => o.Kategori).ThenBy(t => t.SiraNo).ToList();
            var toggled = new Dictionary<string, bool>();
            foreach (var item in panelToggled)
            {
                var ptg = item.Replace("__", "◘").Split('◘');
                toggled.Add(ptg[0], ptg[1].ToBoolean().Value);
            }
            ViewBag.PanelToggled = toggled;



            return View(data);
        }

        private void RederFiles(string enstituKod = null)
        {
            var enstituler = EnstituBus.Enstitulers.Where(p => p.EnstituKod == (enstituKod.IsNullOrWhiteSpace() ? p.EnstituKod : enstituKod)).ToList();
            foreach (var iteme in enstituler)
            {
                var dataR = SrTalepleriBus.GetSonSrTalebiDavetData(iteme);
                foreach (var itemD in dataR)
                {
                    var srTalebi = _entities.SRTalepleris.First(f => f.SRTalepID == itemD.TableId);
                    srTalebi.DavetResimYolu = _inviteRenderService.RenderToFile(itemD);
                }
            }
            _entities.SaveChanges();
        }
    }
}