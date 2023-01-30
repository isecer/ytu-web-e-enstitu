using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;
using BiskaUtil;
using Newtonsoft.Json.Linq;
using System.Xml;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize]
    public class DekontBilgiController : Controller
    {
        // GET: DekontBilgi

        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string BTID, string EKD,   bool IsPopup = false)
        {
            var model = new KmDekontBilgi(); 
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            int? BasvuruSurecID = null;
            bool yetkili = RoleNames.BasvuruSureci.InRoleCurrent() || RoleNames.BasvuruSureciOgrenciKayit.InRoleCurrent();
            int kuLID;
            if (yetkili && BTID.IsNullOrWhiteSpace() == false)
            {
                var gd = new Guid(BTID);
                var tercih = db.BasvurularTercihleris.Where(p => p.UniqueID == gd).FirstOrDefault();
                _EnstituKod = tercih.Basvurular.BasvuruSurec.EnstituKod;
                kuLID = tercih.Basvurular.KullaniciID;
            }
            else kuLID = UserIdentity.Current.Id;
            var kul = db.Kullanicilars.Where(p => p.KullaniciID == kuLID).First();
            var enstitu = db.Enstitulers.Where(p =>   p.EnstituKod == _EnstituKod).First();
            model.EnstituAdi = enstitu.EnstituAd;
            model.AdSoyad = kul.Ad + " " + kul.Soyad;
            model.KullaniciTipID = kul.KullaniciTipID;
            model.TcPasaportNo = kul.KullaniciTipleri.Yerli ? kul.TcKimlikNo : kul.PasaportNo;
            model.KullaniciAktif = true;
            model.KullaniciID = kuLID;
            if (BTID.IsNullOrWhiteSpace() == false)
            {
                var gd = new Guid(BTID);
                var tercih = db.BasvurularTercihleris.Where(p => p.UniqueID == gd).FirstOrDefault();
                if (tercih != null)
                {
                    BasvuruSurecID = tercih.Basvurular.BasvuruSurecID;
                    model.ProgramBilgi = Management.getKontenjanProgramBilgi(tercih.ProgramKod, tercih.OgrenimTipKod, BasvuruSurecID.Value, tercih.Basvurular.KullaniciTipID.Value);
                }
            }
            ViewBag.IsPopup = IsPopup; 
            ViewBag.BasvuruSurecID = new SelectList(Management.getbasvuruSurecleri(kuLID, _EnstituKod, BasvuruSurecTipi.LisansustuBasvuru, true), "Value", "Caption", BasvuruSurecID);
            ViewBag.UniqueID = new SelectList(Management.getbasvuruSurecleriTercihlerOdeme(BasvuruSurecID, kuLID, true, false), "Value", "Caption", BTID);
            return View(model);
        }

        public ActionResult Kayit(string UniqueID, DateTime? DekontTarihi, string DekontNo)
        {
            var gd = new Guid(UniqueID); 
            var tercih = db.BasvurularTercihleris.Where(p => p.UniqueID == gd).FirstOrDefault();

            var prgOdemeBilig = Management.GetOnlineOdemeProgramDetay(UniqueID, true, true, true);
            bool saveSuccess = true;
            var mMessage = new MmMessage();
            if (tercih != null)
            {
                var mSonuc = tercih.MulakatSonuclaris.FirstOrDefault();
                if (mSonuc != null)
                {
                    if (mSonuc.MulakatSonucTipID == MulakatSonucTipi.Asil && mSonuc.KayitDurumID.HasValue)
                    {
                        mMessage.Messages.Add("Kayıt süreci tamamlanmış Asil adaylar dekont giriş işlemi yapamazlar. Lütfen ön kayıt işleminizi bekleyiniz.");
                        saveSuccess = false;
                    }
                    else if (mSonuc.MulakatSonucTipID == MulakatSonucTipi.Yedek && mSonuc.KayitDurumID != KayitDurumu.OnKayit)
                    {
                        mMessage.Messages.Add("Ön Kaydı yapılmayan Yedek adaylar dekont giriş işlemi yapamazlar. Lütfen ön kayıt işleminizi bekleyiniz.");
                        saveSuccess = false;
                    }
                }
                else saveSuccess = false;
            }
            else saveSuccess = false;


            if (DekontTarihi.HasValue == false)
            {
                saveSuccess = false;
                mMessage.Messages.Add("Dekont Tarihi Giriniz");
            }
            else if (DekontTarihi.Value > DateTime.Now.TodateToShortDate())
            {
                saveSuccess = false;
                mMessage.Messages.Add("Dekont tarihi günümüz tarihinden daha büyük olamaz!");
            }
            if (DekontNo.IsNullOrWhiteSpace())
            {
                saveSuccess = false;
                mMessage.Messages.Add("Dekont numarasını giriniz");
            }
            if (saveSuccess)
            {
                var dekont = tercih.BasvurularTercihleriKayitOdemeleris.Where(p => p.DonemNo == 1 && p.IsOdendi).FirstOrDefault();
                if (dekont == null)
                {
                    tercih.BasvurularTercihleriKayitOdemeleris.Add(new BasvurularTercihleriKayitOdemeleri
                    {
                        IsDekontGirisOrSanalPos = true,
                        DonemNo = 1,
                        DekontNo = DekontNo,
                        Ucret = prgOdemeBilig.OdenecekUcret,
                        DekontTarih = DekontTarihi,
                        IsOdendi = true,
                        Aciklama = prgOdemeBilig.Aciklama,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip
                    });
                }
                else
                {
                    dekont.DekontNo = DekontNo;
                    dekont.Ucret = prgOdemeBilig.OdenecekUcret;
                    dekont.DekontTarih = DekontTarihi;
                    dekont.IsOdendi = true;
                    dekont.Aciklama = prgOdemeBilig.Aciklama;
                    dekont.IslemTarihi = DateTime.Now;
                    dekont.IslemYapanID = UserIdentity.Current.Id;
                    dekont.IslemYapanIP = UserIdentity.Ip;
                }
                mMessage.Title = "Dekont Bilgisi Girişi İşlemi Başarılı.";
                mMessage.Messages.Add("Dekont bilgileriniz alınmıştır.");

                db.SaveChanges();
                mMessage.IsSuccess = true;
                mMessage.MessageType = Msgtype.Success;
            }
            else
            {
                mMessage.Title = "Dekont Bilgisi Giriş İşlemi Başarısız.";
                mMessage.IsSuccess = false;
                mMessage.MessageType = Msgtype.Error;
            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mMessage);
            return Json(new { IsSuccess = mMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult getProgramlar(int BasvuruSurecID, int? KullaniciID)
        {
            var yetki = RoleNames.BasvuruSureciKayit.InRoleCurrent();
            if (!yetki)
            {
                KullaniciID = UserIdentity.Current.Id;
            }
           
            var bolm = Management.getbasvuruSurecleriTercihlerOdeme(BasvuruSurecID, KullaniciID.Value, true, false);
            return bolm.Select(s => new { s.Value, s.Caption }).toJsonResult();
        }
        public ActionResult getPRdetay(string UniqueID)
        {
             
            var ngid = new Guid(UniqueID);
            var mdl = Management.GetOnlineOdemeProgramDetay(UniqueID, true, true,true);
         
            return View(mdl);
        }

    }
}