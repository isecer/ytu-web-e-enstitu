using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class OgrenimController : Controller
    {
        // GET: Ogrenim
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {
            return Index(new fmMezuniyetBasvurulari() { PageSize = 10 }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmMezuniyetBasvurulari model, string EKD)
        {

           
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            #region bilgiModel
            var bbModel = new IndexPageInfoDto();
            var MezuniyetSurecID = MezuniyetBus.GetMezuniyetAktifSurecId(_EnstituKod);
            bbModel.AktifSurecID = MezuniyetSurecID ?? 0;
            bbModel.SistemBasvuruyaAcik = MezuniyetAyar.MezuniyetBasvurusuAcikmi.getAyarMZ(_EnstituKod, "0").ToBoolean().Value && MezuniyetSurecID.HasValue;
            bbModel.MezuniyetSurec = db.MezuniyetSurecis.Where(p => p.MezuniyetSurecID == MezuniyetSurecID.Value).FirstOrDefault();
            if (bbModel.MezuniyetSurec != null)
            {
                bbModel.DonemAdi = bbModel.MezuniyetSurec.BaslangicYil + "/" + bbModel.MezuniyetSurec.BitisYil + " " + db.Donemlers.Where(p => p.DonemID == bbModel.MezuniyetSurec.DonemID ).First().DonemAdi + " " + bbModel.MezuniyetSurec.SiraNo;
            }
            var kulls = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();
            if (kulls.YtuOgrencisi)
            {
                var otb = db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod  && p.OgrenimTipKod == kulls.OgrenimTipKod).First();

                bbModel.OgrenimDurumAdi = kulls.OgrenimDurumlari.OgrenimDurumAdi;
                bbModel.OgrenimTipAdi = otb.OgrenimTipAdi;
                bbModel.AnabilimdaliAdi = kulls.Programlar.AnabilimDallari.AnabilimDaliAdi;
                bbModel.ProgramAdi = kulls.Programlar.ProgramAdi;
                bbModel.OgrenciNo = kulls.OgrenciNo;
                bbModel.KullaniciTipYetki = kulls.OgrenimDurumID == OgrenimDurum.HalenOğrenci;

                if (kulls.KayitDonemID.HasValue == false && kulls.OgrenimDurumID == OgrenimDurum.HalenOğrenci && kulls.KayitDonemID.HasValue == false)
                {
                    var kullKayitB = KullanicilarBus.KullaniciObsOgrenciBilgisiGuncelle(UserIdentity.Current.Id);
                    if (kullKayitB.KayitVar == false)
                    {
                        bbModel.KullaniciTipYetki = false;
                        bbModel.KullaniciTipYetkiYokMsj = "GSIS sisteminde aktif öğrenim bilginize rastlanmadı! Profil bilgilerinizde giriş yaptığınız YTU Lüsansüstü Öreğnci bilgilerinizin doğruluğunu kontrol ediniz lütfen.";
                    }
                    else
                    {
                        kulls.KayitYilBaslangic = kullKayitB.BaslangicYil;
                        kulls.KayitDonemID = kullKayitB.DonemID;
                        kulls.KayitTarihi = kullKayitB.KayitTarihi;
                    }
                }
                if (bbModel.KullaniciTipYetki) bbModel.KayitDonemi = kulls.KayitYilBaslangic + "/" + (kulls.KayitYilBaslangic + 1) + " " + db.Donemlers.Where(p => p.DonemID == kulls.KayitDonemID.Value ).First().DonemAdi + " , " + kulls.KayitTarihi.ToString("dd.MM.yyyy");

            }
            else
            {
                bbModel.KullaniciTipYetki = false;
                bbModel.KullaniciTipYetkiYokMsj =  "Profil bilgilerinizde YTU Lisansütü öğrencisi olduğunuza dair bilgiler doldurulmadığı için mezuniyet başvurusu yapamazsınız. Sağ üst köşeden profil bilgilerinizi düzenle butonuna tıklayıp YTÜ Lisansüstü Öğrencisi Misiniz? sorusunu cevaplayarak öğrenim bilgilerinizi doldurup profilinizi güncelleyerek tekrar başvuru yapmayı deneyiniz.";
            }
            bbModel.Enstitü = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod ).First();
            bbModel.Kullanici = kulls;
            #endregion 
            var nowDate = DateTime.Now;
            string EnstituKod = Management.getSelectedEnstitu(EKD);
            var KullaniciID = UserIdentity.Current.Id;
            var q = from s in db.MezuniyetBasvurularis
                    join mOT in db.MezuniyetSureciOgrenimTipKriterleris on new { s.MezuniyetSurecID, s.OgrenimTipKod } equals new { mOT.MezuniyetSurecID, mOT.OgrenimTipKod }
                    join k in db.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    join ot in db.OgrenimTipleris on new { k.OgrenimTipKod, k.EnstituKod } equals new { OgrenimTipKod = (int?)ot.OgrenimTipKod, ot.EnstituKod }
                    join pr in db.Programlars on   k.ProgramKod equals  pr.ProgramKod 
                    join ab in db.AnabilimDallaris on   k.Programlar.AnabilimDaliKod equals  ab.AnabilimDaliKod 
                    join en in db.Enstitulers on   s.MezuniyetSureci.EnstituKod equals  en.EnstituKod 
                    join bs in db.MezuniyetSurecis on s.MezuniyetSurecID equals bs.MezuniyetSurecID
                    join d in db.Donemlers on  bs.DonemID equals  d.DonemID 
                    join ktip in db.KullaniciTipleris on  s.Kullanicilar.KullaniciTipID equals   ktip.KullaniciTipID 
                    join dr in db.MezuniyetYayinKontrolDurumlaris on  s.MezuniyetYayinKontrolDurumID equals  dr.MezuniyetYayinKontrolDurumID 
                    join qmsd in db.MezuniyetSinavDurumlaris on  s.MezuniyetSinavDurumID equals  qmsd.MezuniyetSinavDurumID into defMsd
                    from Msd in defMsd.DefaultIfEmpty()
                    where bs.Enstituler.EnstituKisaAd.Contains(EKD) && s.KullaniciID == KullaniciID
                    select new frMezuniyetBasvurulari
                    {
                        MezuniyetBasvurulariID = s.MezuniyetBasvurulariID,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        OgrenimTipAdi = ot.OgrenimTipAdi,
                        AnabilimdaliAdi = ab.AnabilimDaliAdi,
                        ProgramAdi = pr.ProgramAdi,
                        MezuniyetSurecID = s.MezuniyetSurecID,
                        MezuniyetSurecAdi = bs.BaslangicYil + "/" + bs.BitisYil + " " + d.DonemAdi + " " + bs.SiraNo,
                        BasTar = bs.BaslangicTarihi,
                        BitTar = bs.BitisTarihi,
                        KullaniciID = s.KullaniciID,
                        TezBaslikTr = s.TezBaslikTr,
                        AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                        TcPasaPortNo = s.Kullanicilar.TcKimlikNo != null ? s.Kullanicilar.TcKimlikNo : s.Kullanicilar.PasaportNo,
                        OgrenciNo = s.OgrenciNo,
                        Kullanicilar = s.Kullanicilar,
                        ResimAdi = s.Kullanicilar.ResimAdi,
                        KullaniciTipID = s.Kullanicilar.KullaniciTipID,
                        KullaniciTipAdi = ktip.KullaniciTipAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        MezuniyetYayinKontrolDurumID = s.MezuniyetYayinKontrolDurumID,
                        MezuniyetYayinKontrolDurumAdi = dr.MezuniyetYayinKontrolDurumAdi,
                        DurumClassName = dr.ClassName,
                        DurumColor = dr.Color,
                        MezuniyetSinavDurumID = Msd.MezuniyetSinavDurumID,
                        MezuniyetSinavDurumAdi = Msd != null ? Msd.MezuniyetSinavDurumAdi : "",
                        SDurumClassName = Msd != null ? Msd.ClassName : "",
                        SDurumColor = Msd != null ? Msd.Color : "",
                        MezuniyetYayinKontrolDurumAciklamasi = s.MezuniyetYayinKontrolDurumAciklamasi,
                        BasvuruTarihi = s.BasvuruTarihi,
                        IsMezunOldu = s.IsMezunOldu,
                        MezuniyetTarihi = s.MezuniyetTarihi,
                        SrTalebi = s.SRTalepleris.OrderByDescending(p => p.SRTalepID).FirstOrDefault(),
                        UzatmaSuresiGun = mOT.MBSinavUzatmaSuresiGun,
                    };
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.MezuniyetSurecID.HasValue) q = q.Where(p => p.MezuniyetSurecID == model.MezuniyetSurecID.Value);
            //if (model.KullaniciTipID.HasValue) q = q.Where(p => p.KullaniciTipID == model.KullaniciTipID.Value);
            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.TcPasaPortNo == model.AdSoyad || p.KullaniciTipAdi.Contains(model.AdSoyad));
            if (model.MezuniyetYayinKontrolDurumID.HasValue) q = q.Where(p => p.MezuniyetYayinKontrolDurumID == model.MezuniyetYayinKontrolDurumID.Value);
            model.RowCount = q.Count();
            var IndexModel = new MIndexBilgi();
            //IndexModel.Toplam = model.RowCount;
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.BasvuruTarihi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            var qdata = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToList();

            model.Data = qdata;
            ViewBag.IndexModel = IndexModel;
            ViewBag.MezuniyetSurecID = new SelectList(MezuniyetBus.GetCmbMezuniyetSurecleri(EnstituKod, true), "Value", "Caption", model.MezuniyetSurecID);
            ViewBag.MezuniyetYayinKontrolDurumID = new SelectList(MezuniyetBus.GetCmbMezuniyetYayinDurumListe(true, true), "Value", "Caption", model.MezuniyetYayinKontrolDurumID);
           
            ViewBag.bModel = bbModel;
            return View(model);
        }




     
    }
}