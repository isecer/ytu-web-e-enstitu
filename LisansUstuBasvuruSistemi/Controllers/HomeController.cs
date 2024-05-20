using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(Duration = 0, VaryByParam = "*")]
    public class HomeController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();

 

        public ActionResult Index(string ekd, string mesajGroupId, int? basvuruId, string rowId, bool isMesajGonder = false)
        { 
           

            var enstitu = _entities.Enstitulers.First(p => p.EnstituKisaAd.Contains(ekd));

            #region duyurular 
            var q = from s in _entities.Duyurulars
                    join e in _entities.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where s.IsAktif && s.Tarih <= DateTime.Now && (!s.YayinSonTarih.HasValue || s.YayinSonTarih.Value >= DateTime.Now) && e.EnstituKisaAd.Contains(ekd) && s.AnaSayfadaGozuksun
                    select new
                    {
                        s.EnstituKod,
                        e.EnstituAd,
                        s.DuyuruID,
                        s.Tarih,
                        s.Baslik,
                        s.Aciklama,
                        s.AciklamaHtml,
                        DuyuruYapan = k.Ad + " " + k.Soyad,
                        s.IslemYapanIP,
                        EkSayisi = s.DuyuruEkleris.Count,
                        Ekler = s.DuyuruEkleris,
                        s.AnaSayfadaGozuksun,
                        AnaSayfaPopupAc = s.DuyuruPopuplars.Any(a => a.DuyuruPopupTipID == DuyuruPopupTipiEnum.AnaSayfa),
                        s.YayinSonTarih,
                        s.IsEnUsteSabitle,
                        s.IsAktif
                    };

            var data = q.Select(s => new FrDuyurularDto
            {
                EnstituAdi = s.EnstituAd,
                EnstituKod = s.EnstituKod,
                DuyuruID = s.DuyuruID,
                Baslik = s.Baslik,
                Aciklama = s.Aciklama,
                AciklamaHtml = s.AciklamaHtml,
                Tarih = s.Tarih,
                DuyuruYapan = s.DuyuruYapan,
                IslemYapanIP = s.IslemYapanIP,
                EkSayisi = s.EkSayisi,
                DuyuruEkleris = s.Ekler,
                YayinSonTarih = s.YayinSonTarih,
                IsEnUsteSabitle = s.IsEnUsteSabitle,
            }).OrderBy(o => o.IsEnUsteSabitle ? 1 : 2).ThenByDescending(o => o.Tarih).ToList();
            ViewBag.Duyurular = data;
            #endregion

            if (mesajGroupId.IsNullOrWhiteSpace() == false)
            {
                var secilenMesaj = _entities.Mesajlars.FirstOrDefault(p => p.GroupID == mesajGroupId);
                ViewBag.MesajGroupID = secilenMesaj != null ? mesajGroupId : "";
            }
            else ViewBag.MesajGroupID = "";

            if (basvuruId.HasValue && rowId.IsNullOrWhiteSpace() == false)
            {
                var nRwId = new Guid(rowId);
                var basvuru = _entities.Basvurulars.FirstOrDefault(p => p.BasvuruID == basvuruId.Value && p.RowID == nRwId);
                if (basvuru?.BasvuruSurec.KayitOlmayanlarAnketID != null && basvuru.AnketCevaplaris.All(p => p.AnketID != basvuru.BasvuruSurec.KayitOlmayanlarAnketID))
                {

                    var anketId = basvuru.BasvuruSurec.KayitOlmayanlarAnketID.Value;
                    var anketSorulari = (from bsa in _entities.Ankets.Where(p => p.AnketID == anketId)
                                         join aso in _entities.AnketSorus on bsa.AnketID equals aso.AnketID
                                         join sb in _entities.AnketCevaplaris.Where(p => p.AnketID == anketId && p.BasvuruID == basvuruId) on aso.AnketSoruID equals sb.AnketSoruID into def1
                                         from sbc in def1.DefaultIfEmpty()
                                         select new
                                         {
                                             aso.AnketSoruID,
                                             AnketSoruSecenekID = sbc != null ? sbc.AnketSoruSecenekID : (int?)null,
                                             Aciklama = sbc != null ? sbc.EkAciklama : "",
                                             aso.SiraNo,
                                             aso.SoruAdi,
                                             aso.IsTabloVeriGirisi,
                                             aso.IsTabloVeriMaxSatir,
                                             Secenekler = (from s in aso.AnketSoruSeceneks
                                                           select new
                                                           {
                                                               Value = s.AnketSoruSecenekID,
                                                               s.SiraNo,
                                                               s.IsEkAciklamaGir,
                                                               s.IsYaziOrSayi,
                                                               Caption = s.SecenekAdi,

                                                           }).OrderBy(o => o.SiraNo).ToList()


                                         }).OrderBy(o => o.SiraNo).ToList();
                    var model = new KmAnketlerCevap
                    {
                        AnketTipID = 3,
                        RowID = rowId,
                        AnketID = anketId,
                        JsonStringData = anketSorulari.ToJson()
                    };
                    foreach (var item in anketSorulari)
                    {
                        model.AnketCevapModel.Add(new AnketCevapDto
                        {
                            SecilenAnketSoruSecenekID = item.AnketSoruSecenekID,
                            SoruBilgi = new FrAnketDetayDto { AnketSoruID = item.AnketSoruID, SoruAdi = item.SoruAdi, SiraNo = item.SiraNo, Aciklama = item.Aciklama, IsTabloVeriGirisi = item.IsTabloVeriGirisi, IsTabloVeriMaxSatir = item.IsTabloVeriMaxSatir, },
                            SoruSecenek = item.Secenekler.Select(s => new FrAnketSecenekDetayDto
                            {
                                AnketSoruSecenekID = s.Value,
                                SiraNo = s.SiraNo,
                                IsEkAciklamaGir = s.IsEkAciklamaGir,
                                IsYaziOrSayi = s.IsYaziOrSayi,
                                SecenekAdi = s.Caption
                            }).ToList(),
                            SelectListSoruSecenek = new SelectList(item.Secenekler.ToList(), "Value", "Caption", item.AnketSoruSecenekID)
                        });
                    }

                    var anketGiris = ViewRenderHelper.RenderPartialView("Ajax", "GetAnket", model);
                    ViewBag.AnketGiris = anketGiris;

                }
            }
            ViewBag.IsMesajGonder = isMesajGonder;

            return View(enstitu);
        }

        public ActionResult AuthenticatedControl()
        {
            if (Request.Browser.IsMobileDevice) { }
            return Json(UserIdentity.Current.IsAuthenticated, "application/json", JsonRequestBehavior.AllowGet);
        }




    }
}
