using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

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
        [ValidateInput(false)]
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

            var renderFiles = false;
            foreach (var item in qModel)
            {
                var ayar = _entities.MezuniyetAyarlars.FirstOrDefault(p => p.AyarAdi == item.AyarAdi && p.EnstituKod == enstituKod);
                if (ayar != null)
                {
                    if (ayar.AyarAdi == MezuniyetAyar.TezSinaviDavetKartlariniAnaSayfadaGoster && ayar.AyarDegeri != item.AyarDegeri && item.AyarDegeri.ToBoolean() == true)
                    {
                        renderFiles = true;
                    }
                    else if (ayar.AyarAdi == MezuniyetAyar.TezSinaviDavetListesindeGosterilecekKisiSayisi && ayar.AyarDegeri != item.AyarDegeri)
                    {
                        renderFiles = true;
                    }
                    ayar.AyarDegeri = item.AyarDegeri;
                }
            }
            _entities.SaveChanges();
            if (renderFiles)
            {
                RederFiles(enstituKod);
            }
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

                    // Eğer DavetResmiGostermeDurum null ise, varsayılan olarak profil resmiyle göster
                    if (srTalebi.DavetResmiGostermeDurum == null)
                    {
                        srTalebi.DavetResmiGostermeDurum = itemD.AvatarPath != null
                            ? SrDavetResmiGostermeDurumEnum.DavetProfilResmiyleGoster
                            : SrDavetResmiGostermeDurumEnum.DavetProfilResmiOlmadanGoster;
                    }

                    // Mevcut durum kontrolü
                    if (srTalebi.DavetResmiGostermeDurum == SrDavetResmiGostermeDurumEnum.DavetResmiGosterme)
                    {
                        // Davet resmi gösterilmeyecek, resim dosyasını sil
                        if (!srTalebi.DavetResimYolu.IsNullOrWhiteSpace())
                        {
                            FileHelper.Delete(srTalebi.DavetResimYolu);
                            srTalebi.DavetResimYolu = null;
                        }
                    }
                    else
                    {
                        // Durum kontrolüne göre avatar path ayarla
                        if (srTalebi.DavetResmiGostermeDurum == SrDavetResmiGostermeDurumEnum.DavetProfilResmiOlmadanGoster)
                        {
                            itemD.AvatarPath = null;
                        }

                        // Davet resmini oluştur
                        srTalebi.DavetResimYolu = _inviteRenderService.RenderToFile(itemD);
                    }
                }
            }

            _entities.SaveChanges();
        }
    }
}