using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class YYDGelenBasvurularController : Controller
    {

        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();

        [Authorize(Roles = RoleNames.YydGelenBasvurular)]
        public ActionResult Index(string EKD, int? BelgeDetailBasvuruID = null)
        {
            var model = new FmBasvurularDto() { PageSize = 10, Expand = false };

            model.BasvuruSurecID = Management.GetAktifBasvuruSurecId(EnstituBus.GetSelectedEnstitu(EKD), BasvuruSurecTipiEnum.YTUYeniMezunDRBasvuru);
            model.Expand = model.BasvuruSurecID.HasValue;
            return Index(model, EKD, null, false, BelgeDetailBasvuruID);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.YydGelenBasvurular)]
        public ActionResult Index(FmBasvurularDto model, string EKD, List<string> ProgramKod = null, bool export = false, int? BelgeDetailBasvuruID = null)
        {

            var nowDate = DateTime.Now;
            ProgramKod = ProgramKod ?? new List<string>();
            var _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);

            var q = from s in db.Basvurulars
                    join en in db.Enstitulers on s.BasvuruSurec.EnstituKod equals en.EnstituKod
                    join bs in db.BasvuruSurecs.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipiEnum.YTUYeniMezunDRBasvuru) on s.BasvuruSurecID equals bs.BasvuruSurecID
                    join d in db.Donemlers on bs.DonemID equals d.DonemID
                    join ktip in db.KullaniciTipleris on s.Kullanicilar.KullaniciTipID equals ktip.KullaniciTipID
                    join dr in db.BasvuruDurumlaris on s.BasvuruDurumID equals dr.BasvuruDurumID
                    //join un in db.Universitelers on s.LUniversiteID equals un.UniversiteID into def
                    //from defUn in def.DefaultIfEmpty()
                    //join lb in db.OgrenciBolumleris on s.LOgrenciBolumID equals lb.OgrenciBolumID into deflb
                    //from lOb in deflb.DefaultIfEmpty()
                    //join lo in db.OgrenimDurumlaris on s.LOgrenimDurumID equals lo.OgrenimDurumID into deflod
                    //from lOd in deflod.DefaultIfEmpty()
                    where en.EnstituKisaAd.Contains(EKD)
                    select new
                    {
                        KullaniciID = s.KullaniciID,
                        BasvuruSurecID = s.BasvuruSurecID,
                        BasvuruID = s.BasvuruID,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        UyrukKod = s.UyrukKod,
                        CinsiyetID = s.CinsiyetID,
                        BasvuruDurumAciklamasi = s.BasvuruDurumAciklamasi,
                        BasvuruSurecAdi = bs.BaslangicYil + "/" + bs.BitisYil + " " + d.DonemAdi,
                        BasTar = bs.BaslangicTarihi,
                        BitTar = bs.BitisTarihi,
                        ResimAdi = s.Kullanicilar.ResimAdi,
                        s.TcKimlikNo,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.Ad,
                        s.Soyad,
                        s.DogumTarihi,
                        KullaniciTipID = s.Kullanicilar.KullaniciTipID,
                        KullaniciTipAdi = ktip.KullaniciTipAdi,
                        s.LOgrenimDurumID,
                        s.EMail,
                        Telefon = s.CepTel ?? s.EvTel ?? s.IsTel,
                        TercihSayisi = s.BasvurularTercihleris.Count,
                        BasvuruDurumID = s.BasvuruDurumID,
                        BasvuruDurumAdi = dr.BasvuruDurumAdi,
                        DurumClassName = dr.ClassName,
                        DurumColor = dr.Color,
                        BasvuruTarihi = s.BasvuruTarihi,
                        BasvurularTercihleris = s.BasvurularTercihleris.Select(s2 => new { s2.OgrenimTipKod, s2.ProgramKod }),
                        BasvurularSinavBilgis = s.BasvurularSinavBilgis.Select(s2 => new { s2.SinavTipKod, s2.IsTaahhutVar }),
                        //UnAdi = defUn != null ? defUn.Ad : "",
                        //s.LFakulteAdi,
                        //LOgrenciBolumAdi = lOb != null ? lOb.BolumAdi : "",
                        //LOgrenimDurumu = lOd != null ? lOd.OgrenimDurumAdi : "",
                        LNotSistemi = s.LNotSistemID,
                        s.LMezuniyetNotu,
                        s.LMezuniyetNotu100LukSistem,
                        MulakatSonucTipIDs = s.MulakatSonuclaris.Select(s2 => s2.MulakatSonucTipID),
                        KayitliTercihVar = s.BasvurularTercihleris.Any(a => s.BasvuruID == a.BasvuruID && s.BasvuruDurumID == BasvuruDurumuEnum.Onaylandı && s.MulakatSonuclaris.Any(a2 => a2.KayitDurumID.HasValue && a2.KayitDurumlari.IsKayitOldu)),
                        IsNotDuzelt = (s.BasvuruSurec.AGNOGirisBaslangicTarihi.HasValue && s.LUniversiteID == Management.UniversiteYtuKod && (s.BasvuruSurec.AGNOGirisBaslangicTarihi.Value <= nowDate && s.BasvuruSurec.AGNOGirisBitisTarihi.Value >= nowDate && s.BasvurularTercihleris.Any(a => a.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans))),
                    };
            var q2 = q;
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.BasvuruSurecID.HasValue) q = q.Where(p => p.BasvuruSurecID == model.BasvuruSurecID.Value);
            if (model.LOgrenimDurumID.HasValue) q = q.Where(p => p.LOgrenimDurumID == model.LOgrenimDurumID.Value);
            if (ProgramKod.Count > 0) q = q.Where(p => p.BasvurularTercihleris.Any(a => ProgramKod.Contains(a.ProgramKod)));
            if (model.BasvuruDurumID.HasValue)
            {
                if (model.BasvuruDurumID.Value == BasvuruDurumuEnum.Gonderildi) q = q.Where(p => p.KayitliTercihVar);
                else q = q.Where(p => p.BasvuruDurumID == model.BasvuruDurumID);
            }
            if (model.MulakatSonucTipID.HasValue) q = q.Where(p => p.MulakatSonucTipIDs.Any(a => a == model.MulakatSonucTipID.Value));
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.BasvurularTercihleris.Any(a => a.OgrenimTipKod == model.OgrenimTipKod));
            if (model.KullaniciTipID.HasValue) q = q.Where(p => p.KullaniciTipID == model.KullaniciTipID);
            if (model.CinsiyetID.HasValue) q = q.Where(p => p.CinsiyetID == model.CinsiyetID);
            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.TcKimlikNo == model.AdSoyad);
            if (model.SinavTipKod.HasValue && model.IsTaahhutVar.HasValue) q = q.Where(p => p.BasvurularSinavBilgis.Any(a => a.SinavTipKod == model.SinavTipKod && a.IsTaahhutVar == (model.IsTaahhutVar == false ? null : model.IsTaahhutVar)));
            else if (model.SinavTipKod.HasValue) q = q.Where(p => p.BasvurularSinavBilgis.Any(a => a.SinavTipKod == model.SinavTipKod));
            else if (model.IsTaahhutVar.HasValue) q = q.Where(p => p.BasvurularSinavBilgis.Any(a => a.IsTaahhutVar == (model.IsTaahhutVar == false ? null : model.IsTaahhutVar)));
            if (model.UyrukKod.HasValue) q = q.Where(p => p.UyrukKod == model.UyrukKod);
            bool isFiltered = false;
            if (q != q2)
                isFiltered = true;

            model.RowCount = q.Count();
            var IndexModel = new MIndexBilgi();
            var KayitCountDurum = db.BasvuruDurumlaris.Where(p => p.BasvuruDurumID == BasvuruDurumuEnum.Onaylandı).Select(s => new { s.BasvuruDurumID, s.BasvuruDurumAdi, s.ClassName, s.Color }).FirstOrDefault();
            if (KayitCountDurum != null)
            {
                IndexModel.ListB.Add(new mxRowModel { Key = "Toplam", ClassName = "", Color = KayitCountDurum.Color, Toplam = model.RowCount });
            }
            //var BasvuruDurumlaris = db.BasvuruDurumlaris.Select(s => new { s.BasvuruDurumID, s.BasvuruDurumAdi, s.BasvuruDurumlari.ClassName, s.BasvuruDurumlari.Color }).ToList();
            //foreach (var item in BasvuruDurumlaris)
            //{
            //    IndexModel.ListB.Add(new mxRowModel
            //    {
            //        Key = item.BasvuruDurumAdi,
            //        ClassName = item.ClassName,
            //        Color = item.Color,
            //        Toplam = q.Where(p => p.BasvuruDurumID == item.BasvuruDurumID).Count(),
            //        KayitOlan = q.Where(p => p.BasvuruDurumID == item.BasvuruDurumID && p.KayitliTercihVar).Count()
            //    });
            //}
            //IndexModel.ListB = IndexModel.ListB.Where(p => p.Toplam > 0 || p.KayitOlan > 0).ToList();

            //IndexModel.ListB = (from s in q
            //                    group new { s.BasvuruDurumID, s.KayitliTercihVar } by new { s.BasvuruDurumID, s.BasvuruDurumAdi, s.DurumClassName, s.DurumColor } into g1
            //                    select new mxRowModel
            //                    {
            //                        Key = g1.Key.BasvuruDurumAdi,
            //                        ClassName = g1.Key.DurumClassName,
            //                        Color = g1.Key.DurumColor,
            //                        //Toplam = g1.Count(),
            //                        //KayitOlan = g1.Where(p => p.KayitliTercihVar).Count()
            //                    }).ToList();

            //var KayitCountDurum = db.BasvuruDurumlaris.Where(p => p.BasvuruDurumID == BasvuruDurumu.Gonderildi ).Select(s => new { s.BasvuruDurumID, s.BasvuruDurumAdi, s.BasvuruDurumlari.ClassName, s.BasvuruDurumlari.Color }).FirstOrDefault();
            //if (KayitCountDurum != null)
            //{
            //    IndexModel.ListB.Add(new mxRowModel { Key = KayitCountDurum.BasvuruDurumAdi, ClassName = KayitCountDurum.ClassName, Color = KayitCountDurum.Color, Toplam = IndexModel.ListB.Sum(s => s.KayitOlan) });
            //}
            IndexModel.Toplam = model.RowCount;
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi); 
            var qdata = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s =>
            new FrBasvurularDto
            {
                KullaniciID = s.KullaniciID,
                BasvuruSurecID = s.BasvuruSurecID,
                BasvuruID = s.BasvuruID,
                EnstituKod = s.EnstituKod,
                EnstituAdi = s.EnstituAdi,
                BasvuruDurumAciklamasi = s.BasvuruDurumAciklamasi,
                BasvuruSurecAdi = s.BasvuruSurecAdi,
                BasTar = s.BasTar,
                BitTar = s.BitTar,
                Ad = s.Ad,
                Soyad = s.Soyad,
                ResimAdi = s.ResimAdi,
                TcKimlikNo = s.TcKimlikNo,
                AdSoyad = s.AdSoyad,
                EMail = s.EMail,
                CepTel = s.Telefon,
                KullaniciTipID = s.KullaniciTipID,
                KullaniciTipAdi = s.KullaniciTipAdi,
                TercihSayisi = s.TercihSayisi,
                BasvuruDurumID = s.BasvuruDurumID,
                BasvuruDurumAdi = s.BasvuruDurumAdi,
                DurumClassName = s.DurumClassName,
                DurumColor = s.DurumColor,
                BasvuruTarihi = s.BasvuruTarihi,
                IsNotDuzelt = s.IsNotDuzelt,
                KayitliTercihVar = s.KayitliTercihVar
            }).ToList();
            model.Data = qdata;



            if (export && model.RowCount > 0)
            {
                db.Database.CommandTimeout = 240;
                GridView gv = new GridView();
                var basvuruIDs = q.Select(s => s.BasvuruID).ToList();
                var qx = (from s in db.Basvurulars.Where(p => basvuruIDs.Contains(p.BasvuruID))
                          join un in db.Universitelers on s.LUniversiteID equals un.UniversiteID into def
                          from defUn in def.DefaultIfEmpty()
                          join lb in db.OgrenciBolumleris on s.LOgrenciBolumID equals lb.OgrenciBolumID into deflb
                          from lOb in deflb.DefaultIfEmpty()
                          join lo in db.OgrenimDurumlaris on s.LOgrenimDurumID equals lo.OgrenimDurumID into deflod
                          from lOd in deflod.DefaultIfEmpty()
                          let os = (from sq in db.BasvurularTercihleris.Where(p => p.MulakatSonuclaris.Any(a => a.KayitDurumID == KayitDurumuEnum.KayitOldu) && p.BasvuruID == s.BasvuruID)
                                    join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod) on sq.OgrenimTipKod equals ot.OgrenimTipKod
                                      select new { sq.OgrenimTipKod, ot.OgrenimTipAdi }).FirstOrDefault()
                          select new
                          {
                              s.TcKimlikNo,
                              s.Ad,
                              s.Soyad,
                              s.EMail,
                              Telefon = s.CepTel ?? s.EvTel ?? s.IsTel,
                              s.DogumTarihi,
                              UniversiteAdi = defUn.Ad,
                              FakulteAdi = s.LFakulteAdi,
                              BolumAdi = lOb.BolumAdi,
                              NotSistyemi = s.LNotSistemID,
                              MezuniyetNotu = s.LMezuniyetNotu,
                              MezuniyetNotu100LukSistem = s.LMezuniyetNotu100LukSistem,
                              KayitOlduguOgrenimSeviyesi = os != null ? os.OgrenimTipAdi : ""
                          }).ToList();
                gv.DataSource = qx;
                gv.DataBind();
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_BasvuruListesi_" + DateTime.Now.ToFormatDate() + ".xls");
            }



            ViewBag.IndexModel = IndexModel;
            ViewBag.BasvuruSurecID = new SelectList(Management.GetbasvuruSurecleri(_EnstituKod, BasvuruSurecTipiEnum.YTUYeniMezunDRBasvuru, true), "Value", "Caption", model.BasvuruSurecID);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(_EnstituKod, true, true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.BasvuruDurumID = new SelectList(Management.CmbBasvuruDurumListe( true, true), "Value", "Caption", model.BasvuruDurumID);
            ViewBag.MulakatSonucTipID = new SelectList(Management.CmbMulakatSonucTip( true), "Value", "Caption", model.MulakatSonucTipID);
            ViewBag.ProgramKod = new SelectList(Management.CmbGetAktifProgramlar( _EnstituKod, false), "Value", "Caption", model.ProgramKod);
            ViewBag.LOgrenimDurumID = new SelectList(Management.CmbAktifOgrenimDurumu2( true, isBasvurudaGozuksun: true), "Value", "Caption", model.LOgrenimDurumID);
            ViewBag.SinavTipKod = new SelectList(Management.CmbGetBsAktifSinavlar(_EnstituKod, new List<int> { SinavTipGrupEnum.DilSinavlari, SinavTipGrupEnum.Tomer, SinavTipGrupEnum.Ales_Gree }, true), "Value", "Caption", model.SinavTipKod);
            ViewBag.KullaniciTipID = new SelectList(KullanicilarBus.GetCmbKullaniciTipleriOgrenciler(true), "Value", "Caption", model.KullaniciTipID);
            ViewBag.CinsiyetID = new SelectList(Management.CmbCinsiyetler( true), "Value", "Caption", model.CinsiyetID);
            ViewBag.IsTaahhutVar = new SelectList(Management.CmbSinavBelgeTaahhut(true), "Value", "Caption", model.IsTaahhutVar);
            ViewBag.UyrukKod = new SelectList(Management.CmbUyruk(true), "Value", "Caption", model.UyrukKod);
           
            if (isFiltered)
            {
                ViewBag.kIds = q.Select(s => s.KullaniciID).ToList();
            }
            else ViewBag.kIds = new List<int>();
            ViewBag.SelectedPrograms = ProgramKod;
            return View(model);
        }
         

        

    }
}
