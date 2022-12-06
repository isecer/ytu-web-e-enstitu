using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using BiskaUtil;
using System.Web.UI.WebControls;
using System.IO;
using System.Web.UI;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(Duration = 0, VaryByParam = "*")]
    public class HomeController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();


        public ActionResult Index(string EKD, string MesajGroupID, int? BasvuruID, string RowID, bool IsMesajGonder = false)
        {
            var enstitu = db.Enstitulers.Where(p => p.EnstituKisaAd.Contains(EKD)).First();
            var DonemBilgi = DateTime.Now.ToAraRaporDonemBilgi();

         


            #region duyurular 
            var q = from s in db.Duyurulars
                    join e in db.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                    join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where s.IsAktif && s.Tarih <= DateTime.Now && (s.YayinSonTarih.HasValue ? s.YayinSonTarih.Value >= DateTime.Now : 1 == 1) && e.EnstituKisaAd.Contains(EKD) && s.AnaSayfadaGozuksun
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
                        s.AnaSayfaPopupAc,
                        s.BasvuruPopupAc,
                        s.YayinSonTarih,
                        s.IsAktif
                    };

            var Data = q.Select(s => new frDuyurular
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
                AnaSayfadaGozuksun = s.AnaSayfadaGozuksun,
                AnaSayfaPopupAc = s.AnaSayfaPopupAc,
                BasvuruPopupAc = s.BasvuruPopupAc,
                YayinSonTarih = s.YayinSonTarih
            }).OrderByDescending(o => o.Tarih).ToList();
            ViewBag.Duyurular = Data;
            #endregion

            if (MesajGroupID.IsNullOrWhiteSpace() == false)
            {
                var SecilenMesaj = db.Mesajlars.Where(p => p.GroupID == MesajGroupID).FirstOrDefault();
                ViewBag.MesajGroupID = SecilenMesaj != null ? MesajGroupID : "";
            }
            else ViewBag.MesajGroupID = "";

            if (BasvuruID.HasValue && RowID.IsNullOrWhiteSpace() == false)
            {
                var nRwID = new Guid(RowID);
                var basvuru = db.Basvurulars.Where(p => p.BasvuruID == BasvuruID.Value && p.RowID == nRwID).FirstOrDefault();
                if (basvuru != null && basvuru.BasvuruSurec.KayitOlmayanlarAnketID.HasValue && !basvuru.AnketCevaplaris.Where(p => p.AnketID == basvuru.BasvuruSurec.KayitOlmayanlarAnketID).Any())
                {

                    var AnketID = basvuru.BasvuruSurec.KayitOlmayanlarAnketID.Value;
                    var anketSorulari = (from bsa in db.Ankets.Where(p => p.AnketID == AnketID)
                                         join aso in db.AnketSorus on bsa.AnketID equals aso.AnketID
                                         join sb in db.AnketCevaplaris.Where(p => p.AnketID == AnketID && p.BasvuruID == BasvuruID) on aso.AnketSoruID equals sb.AnketSoruID into def1
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
                    var model = new kmAnketlerCevap();
                    model.AnketTipID = 3;
                    model.RowID = RowID;
                    model.AnketID = AnketID;
                    model.JsonStringData = anketSorulari.toJsonText();
                    foreach (var item in anketSorulari)
                    {
                        model.AnketCevapModel.Add(new AnketCevapModel
                        {
                            SecilenAnketSoruSecenekID = item.AnketSoruSecenekID,
                            SoruBilgi = new frAnketDetay { AnketSoruID = item.AnketSoruID, SoruAdi = item.SoruAdi, SiraNo = item.SiraNo, Aciklama = item.Aciklama, IsTabloVeriGirisi = item.IsTabloVeriGirisi, IsTabloVeriMaxSatir = item.IsTabloVeriMaxSatir, },
                            SoruSecenek = item.Secenekler.Select(s => new frAnketSecenekDetay
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

                    var AnketGiris = Management.RenderPartialView("Ajax", "getAnket", model);
                    ViewBag.AnketGiris = AnketGiris;

                }
            }
            ViewBag.IsMesajGonder = IsMesajGonder;

            return View(enstitu);
        }

        public ActionResult AuthenticatedControl()
        {
            if (Request.Browser.IsMobileDevice) { }
            return Json(UserIdentity.Current.IsAuthenticated, "application/json", JsonRequestBehavior.AllowGet);
        }




    }
}
