using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using System.Net.Mail;
using System.IO;
using System.Drawing;
using System.Net.Mime;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Web.Security;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [System.Web.Mvc.OutputCache(NoStore = false, Duration = 4, VaryByParam = "*")]
    public class KullanicilarController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        [Authorize(Roles = RoleNames.Kullanicilar)]
        public ActionResult Index()
        {
            return Index(new fmKullanicilar() { PageSize = 15, Expand = false });
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.Kullanicilar)]
        public ActionResult Index(fmKullanicilar model, List<string> ProgramKod = null)
        {
            ProgramKod = ProgramKod ?? new List<string>(); ;
            var userEnst = UserIdentity.Current.EnstituKods;
            var q = from s in db.Kullanicilars
                    join ktl in db.KullaniciTipleris on new { s.KullaniciTipID } equals new { ktl.KullaniciTipID }
                    join en in db.Enstitulers on s.EnstituKod equals en.EnstituKod
                    //where userEnst.Contains(s.EnstituKod)
                    select new
                    {
                        s.KullaniciID,
                        en.EnstituAd,
                        en.EnstituKisaAd,
                        s.YetkiGrupID,
                        s.YetkiGruplari.YetkiGrupAdi,
                        YetkiSayisi = s.Rollers.Count,
                        s.EnstituKod,
                        KtipBasvuruYapabilir = ktl.BasvuruYapabilir,
                        s.ResimAdi,
                        s.KullaniciTipID,
                        ktl.KullaniciTipAdi,
                        s.KullaniciAdi,
                        s.SicilNo,
                        s.Ad,
                        s.Soyad,
                        s.UnvanID,
                        s.BirimID,
                        s.ABDKoordinatoru,
                        s.CinsiyetID,
                        s.OgrenimTipKod,
                        s.OgrenimDurumID,
                        s.ProgramKod,
                        s.TcKimlikNo,
                        s.PasaportNo,
                        s.OgrenciNo,
                        s.CepTel,
                        s.EMail,
                        s.IsAktif,
                        s.IsAdmin,
                        s.OlusturmaTarihi,
                        s.LastLogonDate,
                        s.LastLogonIP,
                        s.IslemTarihi,
                        s.IslemYapanIP,
                        s.YtuOgrencisi   
                    };
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => (p.Ad + " " + p.Soyad).Contains(model.AdSoyad) || p.EMail.Contains(model.AdSoyad) || p.KullaniciAdi.Contains(model.AdSoyad) || p.TcKimlikNo == model.AdSoyad || p.PasaportNo == model.AdSoyad || p.OgrenciNo == model.AdSoyad);
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif.Value);
            if (model.KullaniciTipID.HasValue) q = q.Where(p => p.KullaniciTipID == model.KullaniciTipID.Value);
            if (model.BirimID.HasValue) q = q.Where(p => p.BirimID == model.BirimID.Value);
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod.Value);
            if (model.IsAdmin.HasValue)
            {
                if (model.IsAdmin.Value) q = q.Where(p => p.YetkiSayisi > 0);
                else q = q.Where(p => p.YetkiSayisi == 0);
            }
            if (model.OgrenimDurumID.HasValue) q = q.Where(p => p.OgrenimDurumID == model.OgrenimDurumID.Value);
            if (model.CinsiyetID.HasValue) q = q.Where(p => p.CinsiyetID == model.CinsiyetID.Value);
            if (model.YetkiGrupID.HasValue)
            {
                q = q.Where(p => p.YetkiGrupID == model.YetkiGrupID.Value);
            } 
            model.RowCount = q.Count();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif == true).Count();
            IndexModel.Pasif = q.Where(p => p.IsAktif == false).Count();
            if (!model.Sort.IsNullOrWhiteSpace())
                if (model.Sort == "AdSoyad") q = q.OrderBy(o => o.Ad).ThenBy(o => o.Soyad);
                else if (model.Sort.Contains("AdSoyad") && model.Sort.Contains("DESC")) q = q.OrderByDescending(o => o.Ad).ThenByDescending(o => o.Soyad);
                else if (model.Sort == "KullaniciTipAdi") q = q.OrderBy(o => o.KullaniciTipAdi);
                else if (model.Sort.Contains("KullaniciTipAdi") && model.Sort.Contains("DESC")) q = q.OrderByDescending(o => o.KullaniciTipAdi);
                else q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.OlusturmaTarihi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Select(s => new frKullanicilar
            {
                KullaniciID = s.KullaniciID,
                EnstituAdi = s.EnstituAd,
                EnstituKod = s.EnstituKod,
                YetkiGrupAdi = s.EnstituKisaAd + " - " + s.YetkiGrupAdi + (s.YetkiSayisi > 0 ? " (+ " + s.YetkiSayisi + " yetki)" : ""),
                KtipBasvuruYapabilir = s.KtipBasvuruYapabilir,
                ResimAdi = s.ResimAdi,
                KullaniciTipID = s.KullaniciTipID,
                KullaniciTipAdi = s.KullaniciTipAdi,
                KullaniciAdi = s.KullaniciAdi,
                SicilNo = s.SicilNo,
                Ad = s.Ad,
                Soyad = s.Soyad,
                UnvanID = s.UnvanID,
                BirimID = s.BirimID,
                ABDKoordinatoru = s.ABDKoordinatoru,
                CinsiyetID = s.CinsiyetID,
                OgrenimTipKod = s.OgrenimTipKod,
                OgrenimDurumID = s.OgrenimDurumID,
                ProgramKod = s.ProgramKod,
                TcKimlikNo = s.TcKimlikNo,
                PasaportNo = s.PasaportNo,
                CepTel = s.CepTel,
                EMail = s.EMail,
                IsAktif = s.IsAktif,
                IsAdmin = s.IsAdmin,
                YtuOgrencisi = s.YtuOgrencisi,
                OlusturmaTarihi = s.OlusturmaTarihi,
                LastLogonDate = s.LastLogonDate,
                LastLogonIP = s.LastLogonIP,
                IslemTarihi = s.IslemTarihi,
                IslemYapanIP = s.IslemYapanIP
            }).Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.BirimID = new SelectList(Management.getBirimler().ToOrderedList("BirimID", "UstBirimID", "BirimAdi"), "BirimID", "BirimAdi", model.BirimID);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.IsAdmin = new SelectList(Management.cmbVarYokData(true), "Value", "Caption", model.IsAdmin);
            ViewBag.ProgramKod = new SelectList(Management.cmbGetAktifProgramlar(false), "Value", "Caption", model.ProgramKod);
            ViewBag.OgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu(true, IsHesapKayittaGozuksun: true), "Value", "Caption", model.OgrenimDurumID);
            ViewBag.KullaniciTipID = new SelectList(KullanicilarBus.GetCmbKullaniciTipleri(true, false), "Value", "Caption", model.KullaniciTipID);
            ViewBag.CinsiyetID = new SelectList(Management.cmbCinsiyetler(true), "Value", "Caption", model.CinsiyetID);
            ViewBag.YetkiGrupID = new SelectList(Management.cmbYetkiGruplari(), "Value", "Caption", model.YetkiGrupID);
            ViewBag.SelectedPrograms = ProgramKod;
            ViewBag.IndexModel = IndexModel;
            return View(model);
        }
        [Authorize(Roles = RoleNames.KullanicilarKayit)]
        public ActionResult Kayit(int? id, string EKD)
        {
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            var _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            var model = new Kullanicilar();
            model.IsAktif = true;
            bool IsKurumIci = true;
            bool IsYerli = true;
            bool ResimVar = false;
            if (id.HasValue && id > 0)
            {
                var data = db.Kullanicilars.Where(p => p.KullaniciID == id).FirstOrDefault();
                if (data != null)
                {
                    IsKurumIci = data.KullaniciTipleri.KurumIci;
                    IsYerli = data.KullaniciTipleri.Yerli;
                    ResimVar = data.ResimAdi.IsNullOrWhiteSpace() == false;
                    data.ResimAdi = data.ResimAdi;
                    model = data;
                }
                model.Sifre = "";
            }
            else
            {
                model.EnstituKod = _EnstituKod;
            }
            ViewBag.ResimVar = ResimVar;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.KullaniciTipID = new SelectList(KullanicilarBus.GetCmbKullaniciTipleri(true, false), "Value", "Caption", model.KullaniciTipID);
            ViewBag.UnvanID = new SelectList(Management.cmbUnvanlar(true), "Value", "Caption", model.UnvanID);
            ViewBag.BirimID = new SelectList(Management.cmbBirimler(true), "Value", "Caption", model.BirimID);
            ViewBag.CinsiyetID = new SelectList(Management.cmbCinsiyetler(true), "Value", "Caption", model.CinsiyetID);

            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.ProgramKod = new SelectList(Management.cmbGetAktifProgramlar(model.EnstituKod, true, true), "Value", "Caption", model.ProgramKod);
            ViewBag.OgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu(true, IsHesapKayittaGozuksun: true), "Value", "Caption", model.OgrenimDurumID);
            ViewBag.YetkiGrupID = new SelectList(Management.cmbYetkiGruplari(), "Value", "Caption", model.YetkiGrupID);
            ViewBag.IsKurumIci = IsKurumIci;
            ViewBag.IsYerli = IsYerli;
            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.KullanicilarKayit)]
        public ActionResult Kayit(Kullanicilar kModel, HttpPostedFileBase ProfilResmi, bool YetkilendirmeyeGit = false)
        {
            var MmMessage = new MmMessage();
            bool IsKurumIci = true;
            bool IsYerli = true;
            var ErisimYetki = RoleNames.KullanicilarIslemYetkileri.InRoleCurrent();
            #region Kontrol
            kModel.KullaniciAdi = kModel.KullaniciAdi != null ? kModel.KullaniciAdi.Trim() : "";
            if (ErisimYetki)
            {
                if (kModel.YetkiGrupID <= 0)
                {
                    MmMessage.Messages.Add("Yetki Grubu Seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YetkiGrupID" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YetkiGrupID" });
            }
            if (kModel.KullaniciTipID <= 0)
            {
                MmMessage.Messages.Add("Kullanıcı Tipi Seçiniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciTipID" });
            }
            else
            {
                var ktp = db.KullaniciTipleris.Where(p => p.KullaniciTipID == kModel.KullaniciTipID).First();
                IsKurumIci = ktp.KurumIci;
                IsYerli = ktp.Yerli;
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "KullaniciTipID" });
            }
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Kullanıcının Kayıt Edileceği Enstitüyü Seçiniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });

            if (kModel.KullaniciID <= 0)
            {
                if (ProfilResmi == null)
                {
                    MmMessage.Messages.Add("Profil Resmi Yükleyiniz");
                }
            }
            else
            {
                var kul = db.Kullanicilars.Where(p => p.KullaniciID == kModel.KullaniciID).First();
                if (kul.ResimAdi.IsNullOrWhiteSpace() && ProfilResmi == null)
                {
                    MmMessage.Messages.Add("Profil Resmi Yükleyiniz");
                }
                else kModel.ResimAdi = kul.ResimAdi;
            }
            if (kModel.Ad.IsNullOrWhiteSpace())
            {

                MmMessage.Messages.Add("Ad Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Ad" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Ad" });
            if (kModel.Soyad.IsNullOrWhiteSpace())
            {

                MmMessage.Messages.Add("Soyad Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Soyad" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Soyad" });

            if (kModel.TcKimlikNo.IsNullOrWhiteSpace())
            {

                MmMessage.Messages.Add("T.C. Kimlik No Giriniz.");

                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
            }
            else if (kModel.TcKimlikNo.IsNumber() == false)
            {

                MmMessage.Messages.Add("T.C. Kimlik No Sadece sayıdan oluşmalıdır");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });

            }
            else if (kModel.TcKimlikNo.Length != 11)
            {

                MmMessage.Messages.Add("T.C. Kimlik No 11 haneli olmalıdır");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });

            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TcKimlikNo" });
            if (!IsYerli)
                if (kModel.PasaportNo.IsNullOrWhiteSpace())
                {

                    MmMessage.Messages.Add("Pasaport No Giriniz.");

                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "PasaportNo" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "PasaportNo" });
            if (!kModel.CinsiyetID.HasValue)
            {

                MmMessage.Messages.Add("Cinsiyet Bilgisini Seçiniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CinsiyetID" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CinsiyetID" });



            if (kModel.CepTel.IsNullOrWhiteSpace())
            {

                MmMessage.Messages.Add("Cep Telefonu Numarası Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CepTel" });
            }
            else
            {
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CepTel" });
            }

            if (kModel.EMail.IsNullOrWhiteSpace())
            {
                string msg = "E Mail Giriniz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
            }
            else if (kModel.EMail.ToIsValidEmail())
            {
                string msg = "Lütfen EMail Formatını Doğru Giriniz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
            }
            else
            {
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EMail" });
            }
            if (!IsKurumIci || !IsYerli)
                if (kModel.Adres.IsNullOrWhiteSpace())
                {
                    string msg = "Adres Bilgisi Giriniz.";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Adres" });
                }
                else
                {
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Adres" });
                }
            if (kModel.YtuOgrencisi)
            {
                if (kModel.OgrenciNo.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("Öğrenci No Giriniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });
                }
                else if (kModel.OgrenciNo.Length > 20)
                {

                    MmMessage.Messages.Add("Öğrenci Numarası 20 Haneden Daha Fazla Olamaz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });

                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenciNo" });
                if (kModel.OgrenimTipKod.HasValue == false)
                {
                    MmMessage.Messages.Add("Öğrenim Seviyesi Seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimTipKod" });
                if (kModel.ProgramKod.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("Program Seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProgramKod" });
                if (kModel.OgrenimDurumID.HasValue == false)
                {

                    MmMessage.Messages.Add("Öğrenim durumunuzu Seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimDurumID" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimDurumID" });




            }
            if (IsKurumIci)
                if (!kModel.BirimID.HasValue)
                {
                    MmMessage.Messages.Add("Birim Seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BirimID" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BirimID" });
            if (IsKurumIci)
                if (!kModel.UnvanID.HasValue)
                {
                    MmMessage.Messages.Add("Unvan Seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "UnvanID" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "UnvanID" });
            if (IsKurumIci)
                if (kModel.SicilNo.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("Sicil No Giriniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SicilNo" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SicilNo" });

            if (kModel.KullaniciAdi.IsNullOrWhiteSpace())
            {

                MmMessage.Messages.Add("Kullanıcı Adı Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciAdi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "KullaniciAdi" });

            if (kModel.KullaniciID <= 0)
            {
                if (kModel.Sifre.IsNullOrWhiteSpace())
                {

                    MmMessage.Messages.Add("Şifre Giriniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Sifre" });
                }
                else if (kModel.Sifre.Length < 4)
                {

                    MmMessage.Messages.Add("Şifre en az 4 haneli olmalıdır.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Sifre" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Sifre" });
            }
            else if (!kModel.Sifre.IsNullOrWhiteSpace())
            {
                if (kModel.Sifre.Length < 4 && kModel.KullaniciID > 0)
                {

                    MmMessage.Messages.Add("Şifre en az 4 haneli olmalıdır.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Sifre" });
                }
                else if (kModel.Sifre.Length >= 4 && kModel.KullaniciID > 0) MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Sifre" });
            }

            #endregion

            if (MmMessage.Messages.Count == 0)
            {
                var qPersonel = db.Kullanicilars.AsQueryable();
                var cUserName = qPersonel.Where(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.KullaniciAdi == kModel.KullaniciAdi).Count();
                if (cUserName > 0)
                {

                    MmMessage.Messages.Add("Tanımlamak istediğiniz kullanıcı adı sistemde zaten mevcut!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciAdi" });
                }

                var cTc = qPersonel.Where(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.TcKimlikNo == kModel.TcKimlikNo).Count();
                if (cTc > 0)
                {
                    MmMessage.Messages.Add("Tanımlamak istediğiniz Kimlik No sistemde zaten mevcut!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                }

                if (kModel.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci)
                {
                    var pass = qPersonel.Where(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.PasaportNo == kModel.PasaportNo).Count();
                    if (pass > 0)
                    {

                        MmMessage.Messages.Add("Tanımlamak istediğiniz Pasaport No sistemde zaten mevcut!");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                    }
                }
                if (IsKurumIci)
                {
                    var cSicil = qPersonel.Where(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.SicilNo == kModel.SicilNo).Count();
                    if (cSicil > 0)
                    {
                        MmMessage.Messages.Add("Tanımlamak istediğiniz Sicil No sistemde zaten mevcut!");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SicilNo" });
                    }
                }

                if (kModel.YtuOgrencisi)
                {

                    if (kModel.KullaniciID <= 0)
                    {
                        var cOgrNo = qPersonel.Where(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.OgrenciNo == kModel.OgrenciNo).Count();
                        if (cOgrNo > 0)
                        {
                            MmMessage.Messages.Add("Girmiş olduğunuz öğrenci numarası ile daha önceden sisteme kayıt yapılmıştır. Tekrar kayıt yapamazsınız!");
                            MmMessage.MessagesDialog.Add(new MrMessage
                            { MessageType = Msgtype.Warning, PropertyName = "OgrenciNo" });
                        }
                    }

                    if (kModel.OgrenimDurumID != OgrenimDurum.OzelOgrenci)
                    {
                        var ogrenciBilgi = Management.StudentControl(kModel.TcKimlikNo);
                        if (ogrenciBilgi.Hata)
                        {
                            MmMessage.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu! " + ogrenciBilgi.HataMsj);
                        }
                        else
                        {
                            if (ogrenciBilgi.KayitVar &&
                          kModel.OgrenimTipKod == ogrenciBilgi.OgrenciInfo.OGRENIMSEVIYE_ID.toIntObj())
                            {
                                var Program = db.Programlars.Where(p => p.ProgramKod == kModel.ProgramKod).First();
                                kModel.ProgramKod = Program.ProgramKod;
                                kModel.OgrenimTipKod = ogrenciBilgi.OgrenciInfo.OGRENIMSEVIYE_ID.toIntObj().Value;
                                kModel.KayitTarihi = ogrenciBilgi.KayitTarihi;
                                kModel.KayitYilBaslangic = ogrenciBilgi.BaslangicYil;
                                kModel.KayitDonemID = ogrenciBilgi.DonemID;
                            }
                            else
                            {
                                MmMessage.Messages.Add("Girdiğiniz Kimlik bilgisi OBS sisteminde doğrulanamadı.");
                                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                            }
                        }
                    }

                }

            }
            if (MmMessage.Messages.Count == 0 && IsKurumIci)
            {
                if (kModel.IsActiveDirectoryUser && kModel.EMail.Contains("@yildiz.edu.tr") == false)
                {

                    MmMessage.Messages.Add("Active Directory Girişi Yapmasını İstediğiniz Kullanıcının yildiz.edu.tr e mailini tanımlamanız gerekir!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsActiveDirectoryUser" });
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }
            }

            if (MmMessage.Messages.Count == 0)
            {

                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanIP = UserIdentity.Ip;
                if (!IsKurumIci)
                {
                    kModel.BirimID = null;
                    kModel.UnvanID = null;
                    kModel.SicilNo = "";
                    if (!IsYerli)
                    {
                        kModel.TcKimlikNo = null;
                    }
                    else { kModel.PasaportNo = null; }
                    kModel.ABDKoordinatoru = false;
                }
                else
                {
                    kModel.Adres = "";

                }
                if (!kModel.YtuOgrencisi)
                {
                    kModel.OgrenciNo = null;
                    kModel.OgrenimTipKod = null;
                    kModel.OgrenimDurumID = null;
                    kModel.ProgramKod = null;
                    kModel.KayitTarihi = null;
                    kModel.KayitYilBaslangic = null;
                    kModel.KayitDonemID = null;
                }

                var YeniKullanici = kModel.KullaniciID <= 0;
                if (YeniKullanici)
                {
                    var sfr = kModel.Sifre;
                    kModel.YetkiGrupID = ErisimYetki ? kModel.YetkiGrupID : 1;
                    kModel.OlusturmaTarihi = DateTime.Now;
                    kModel.Sifre = kModel.Sifre.ComputeHash(Management.Tuz);
                    kModel.IsAktif = true;
                    kModel.FixedHeader = false;
                    kModel.FixedSidebar = false;
                    kModel.ScrollSidebar = false;
                    kModel.RightSidebar = false;
                    kModel.CustomNavigation = true;
                    kModel.ToggledNavigation = false;
                    kModel.BoxedOrFullWidth = true;
                    kModel.ThemeName = "/Content/css/theme-forest.css";
                    kModel.BackgroundImage = "wall_2";

                    if (ProfilResmi != null)
                    {
                        kModel.ResimAdi = Management.ResimKaydet(ProfilResmi);

                    }

                    kModel = db.Kullanicilars.Add(kModel);
                    db.SaveChanges();
                    db.KullaniciEnstituYetkileris.Add(new KullaniciEnstituYetkileri
                    {
                        EnstituKod = kModel.EnstituKod,
                        KullaniciID = kModel.KullaniciID,
                        IslemYapanID = kModel.IslemYapanID.Value,
                        IslemTarihi = kModel.IslemTarihi.Value,
                        IslemYapanIP = kModel.IslemYapanIP

                    });
                    db.SaveChanges();

                    var excpt = KullanicilarBus.YeniHesapMailGonder(kModel, sfr);
                    if (excpt != null)
                    {
                        MmMessage.Messages.Add(kModel.KullaniciAdi + " kullanıcı hesabı oluşturuldu fakat kullanıcıya bilgi maili atılırken bir hata oluştu! Hata:" + excpt.ToExceptionMessage());
                        MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
                    }
                }
                else
                {
                    var data = db.Kullanicilars.Where(p => p.KullaniciID == kModel.KullaniciID).First();
                    data.EnstituKod = kModel.EnstituKod;
                    data.KullaniciTipID = kModel.KullaniciTipID;
                    data.Ad = kModel.Ad;
                    bool IsYetkiDegisti = false;
                    if (ErisimYetki)
                    {
                        IsYetkiDegisti = data.YetkiGrupID != kModel.YetkiGrupID;
                        data.YetkiGrupID = kModel.YetkiGrupID;

                    }
                    data.Soyad = kModel.Soyad;
                    data.TcKimlikNo = kModel.TcKimlikNo;
                    data.PasaportNo = kModel.PasaportNo;
                    data.CinsiyetID = kModel.CinsiyetID;
                    data.CepTel = kModel.CepTel;
                    data.EMail = kModel.EMail;
                    data.Adres = kModel.Adres;

                    data.YtuOgrencisi = kModel.YtuOgrencisi;
                    data.OgrenimDurumID = kModel.OgrenimDurumID;
                    data.OgrenimTipKod = kModel.OgrenimTipKod;
                    data.OgrenciNo = kModel.OgrenciNo;
                    data.ProgramKod = kModel.ProgramKod;
                    data.KayitTarihi = kModel.KayitTarihi;
                    data.KayitYilBaslangic = kModel.KayitYilBaslangic;
                    data.KayitDonemID = kModel.KayitDonemID;

                    data.BirimID = kModel.BirimID;
                    data.UnvanID = kModel.UnvanID;
                    data.SicilNo = kModel.SicilNo;
                    data.ABDKoordinatoru = kModel.ABDKoordinatoru;

                    data.KullaniciAdi = kModel.KullaniciAdi;
                    if (!kModel.Sifre.IsNullOrWhiteSpace())
                        data.Sifre = kModel.Sifre.ComputeHash(Management.Tuz);
                    data.SifresiniDegistirsin = kModel.SifresiniDegistirsin;
                    data.Aciklama = kModel.Aciklama;
                    data.IsActiveDirectoryUser = kModel.IsActiveDirectoryUser;
                    data.IsAdmin = kModel.IsAdmin;
                    data.IsAktif = kModel.IsAktif;

                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemTarihi = kModel.IslemTarihi;
                    data.IslemYapanIP = kModel.IslemYapanIP;
                    if (ProfilResmi != null)
                    {
                        if (data.ResimAdi.IsNullOrWhiteSpace() == false)
                        {
                            var EskiResimLazim = Management.ResimBilgisiLazimOlanKayitVarMi(data.KullaniciID);
                            if (EskiResimLazim == false)
                            {
                                var rsmYol = SistemAyar.KullaniciResimYolu;
                                var rsm = Server.MapPath("~/" + rsmYol + "/" + data.ResimAdi);
                                if (System.IO.File.Exists(rsm)) System.IO.File.Delete(rsm);
                            }
                        }
                        data.ResimAdi = Management.ResimKaydet(ProfilResmi);
                    }
                    db.SaveChanges();
                    LogIslemleri.LogEkle("Kullanicilar", IslemTipi.Update, data.ToJson());
                    if (IsYetkiDegisti) UserBus.SetUserRoles(data.KullaniciID, new List<int>(), data.YetkiGrupID);
                    if (data.KullaniciID == UserIdentity.Current.Id) { UserIdentity.Current.ImagePath = data.ResimAdi.ToKullaniciResim(); }

                }


                if (YetkilendirmeyeGit) return RedirectToAction("Yetkilendirme", new { id = kModel.KullaniciID });
                else return RedirectToAction("Index");

            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.ResimVar = kModel.ResimAdi.IsNullOrWhiteSpace() == false;
            ViewBag.KullaniciTipID = new SelectList(KullanicilarBus.GetCmbKullaniciTipleri(true, false), "Value", "Caption", kModel.KullaniciTipID);
            ViewBag.UnvanID = new SelectList(Management.cmbUnvanlar(true), "Value", "Caption", kModel.UnvanID);
            ViewBag.BirimID = new SelectList(Management.cmbBirimler(true), "Value", "Caption", kModel.BirimID);
            ViewBag.MmMessage = MmMessage;
            ViewBag.IsKurumIci = IsKurumIci;
            ViewBag.IsYerli = IsYerli;
            ViewBag.CinsiyetID = new SelectList(Management.cmbCinsiyetler(true), "Value", "Caption", kModel.CinsiyetID);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(kModel.EnstituKod, true), "Value", "Caption", kModel.OgrenimTipKod);
            ViewBag.ProgramKod = new SelectList(Management.cmbGetAktifProgramlar(kModel.EnstituKod, true, true), "Value", "Caption", kModel.ProgramKod);
            ViewBag.OgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu(true, IsHesapKayittaGozuksun: true), "Value", "Caption", kModel.OgrenimDurumID);
            ViewBag.YetkiGrupID = new SelectList(Management.cmbYetkiGruplari(), "Value", "Caption", kModel.YetkiGrupID);

            return View(kModel);
        }

        [Authorize(Roles = RoleNames.KullanicilarIslemYetkileri)]
        public ActionResult Yetkilendirme(int? id)
        {
            if (id.HasValue == false) return RedirectToAction("Index");
            var kid = id;
            var roles = RollerBus.GetAllRoles().ToList();
            var userRoles = UserBus.GetUserRoles(kid.Value);
            var Kullanici = UserBus.GetUser(kid.Value);
            ViewBag.Kullanici = Kullanici;
            var data = roles.Select(s => new CheckObjectX<Roller>
            {
                Value = s,
                Disabled = userRoles.YetkiGrupRolleri.Any(a => a.RolID == s.RolID),
                Checked = userRoles.TumRoller.Any(p => p.RolID == s.RolID)
            });
            ViewBag.Roller = data;
            var kategr = roles.Select(s => s.Kategori).Distinct().ToArray();
            var menuK = db.Menulers.Where(a => a.BagliMenuID == 0 && kategr.Contains(a.MenuAdi)).ToList();
            var dct = new List<CmbIntDto>();
            foreach (var item in menuK)
            {
                dct.Add(new CmbIntDto { Value = item.SiraNo.Value, Caption = item.MenuAdi });
            }
            ViewBag.cats = dct;
            ViewBag.YetkiGrupID = new SelectList(Management.cmbYetkiGruplari(), "Value", "Caption", Kullanici.YetkiGrupID);
            return View();
        }
        [HttpPost, ActionName("Yetkilendirme")]
        [Authorize(Roles = RoleNames.KullanicilarIslemYetkileri)]
        public ActionResult Yetkilendirme(List<int> RolID, int KullaniciID, int YetkiGrupID, string EKD, bool ProgramYetkilerineGit = false)
        {

            RolID = RolID ?? new List<int>();
            UserBus.SetUserRoles(KullaniciID, RolID, YetkiGrupID);
            MessageBox.Show("Yetkiler Kaydedildi", MessageBox.MessageType.Success);
            if (ProgramYetkilerineGit) return RedirectToAction("KullaniciProgramYetkileri", new { id = KullaniciID, EKD = EKD });
            else return RedirectToAction("Index");
        }
        public ActionResult getYetkiGrubuRolIDs(int id, int KullaniciID)
        {
            var CheckedRollIDs = new List<CmbIntDto>();

            var Kullanici = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).First();
            if (Kullanici.YetkiGrupID == id) CheckedRollIDs = Kullanici.Rollers.Select(s => new CmbIntDto { Value = s.RolID, Caption = RenkTiplier.Info }).ToList();
            var YetkiGrupRollIDs = db.YetkiGrupRolleris.Where(p => p.YetkiGrupID == id).Select(s => new CmbIntDto { Value = s.RolID, Caption = RenkTiplier.Danger }).ToList();
            CheckedRollIDs.AddRange(YetkiGrupRollIDs);
            return CheckedRollIDs.toJsonResult();
        }



        [Authorize(Roles = RoleNames.KullanicilarEnstituYetkileri)]
        public ActionResult YetkilendirmeEnstitu(int? id)
        {
            if (id.HasValue == false) return RedirectToAction("Index");

            var roles = EnstituBus.GetEnstituler(true);
            var userRoles = UserBus.GetKullaniciEnstituler(id.Value);
            var Kullanici = UserBus.GetUser(id.Value);
            ViewBag.Kullanici = Kullanici;
            var data = roles.Select(s => new CheckObject<Enstituler>
            {
                Value = s,
                Checked = userRoles.Any(p => p.EnstituKod == s.EnstituKod)
            });
            ViewBag.Enstituler = data;
            return View();
        }
        [HttpPost, ActionName("YetkilendirmeEnstitu")]
        [Authorize(Roles = RoleNames.KullanicilarEnstituYetkileri)]
        public ActionResult YetkilendirmeEnstitu(List<string> EnstituKods, int KullaniciID)
        {
            var eKods = UserIdentity.Current.EnstituKods;
            if (EnstituKods == null) EnstituKods = new List<string>();
            var gEnstitu = db.KullaniciEnstituYetkileris.Where(p => p.KullaniciID == KullaniciID).AsQueryable();
            if (UserIdentity.Current.IsAdmin == false)
            {
                gEnstitu.Where(p => eKods.Contains(p.EnstituKod));
            }
            db.KullaniciEnstituYetkileris.RemoveRange(gEnstitu.ToList());
            foreach (var item in EnstituKods)
            {
                db.KullaniciEnstituYetkileris.Add(new KullaniciEnstituYetkileri
                {
                    EnstituKod = item,
                    KullaniciID = KullaniciID,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id,
                    IslemYapanIP = UserIdentity.Ip
                });
            }
            if (UserIdentity.Current.Id == KullaniciID) UserIdentity.Current.EnstituKods = EnstituKods ?? new List<string>();
            db.SaveChanges();
            MessageBox.Show("Yetkiler Kaydedildi", MessageBox.MessageType.Success);
            return RedirectToAction("Index");
        }


        [Authorize(Roles = RoleNames.KullanicilarProgramYetkileri)]
        public ActionResult KullaniciProgramYetkileri(int? id, string EKD)
        {
            if (id.HasValue == false) return RedirectToAction("Index");

            var _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            var data = KullanicilarBus.GetKullaniciProgramlari(id.Value, _EnstituKod);
            var Kullanici = UserBus.GetUser(id.Value);
            ViewBag.Kullanici = Kullanici;
            return View(data);
        }
        [HttpPost, ActionName("KullaniciProgramYetkileri")]
        [Authorize(Roles = RoleNames.KullanicilarProgramYetkileri)]
        public ActionResult KullaniciProgramYetkileri(List<string> ProgramKod, int KullaniciID, string EKD)
        {
            var _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            if (KullaniciID <= 0)
            {
                return RedirectToAction("Index");
            }
            ProgramKod = ProgramKod == null ? new List<string>() : ProgramKod;
            var gEnstitu = UserIdentity.Current.EnstituKods;
            var kProg = db.KullaniciProgramlaris.Where(p => p.KullaniciID == KullaniciID && p.Programlar.AnabilimDallari.EnstituKod.Contains(_EnstituKod)).ToList();
            db.KullaniciProgramlaris.RemoveRange(kProg);
            foreach (var item in ProgramKod)
            {
                db.KullaniciProgramlaris.Add(new KullaniciProgramlari { KullaniciID = KullaniciID, ProgramKod = item });
            }
            db.SaveChanges();

            var roles = new List<string> { RoleNames.MulakatKayıt, RoleNames.MulakatSil, RoleNames.MulakatSureci };
            var EklenecekRoller = db.Rollers.Where(p => roles.Contains(p.RolAdi)).ToList();

            var kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).First();
            var VarolanYetkiler = kul.Rollers.Where(p => EklenecekRoller.Select(s => s.RolID).Contains(p.RolID)).ToList();
            foreach (var item in VarolanYetkiler)
            {
                kul.Rollers.Remove(item);
            }
            foreach (var item in EklenecekRoller)
            {
                kul.Rollers.Add(item);
            }
            db.SaveChanges();
            MessageBox.Show("Program Yetkileri Kaydedildi", MessageBox.MessageType.Success);
            return RedirectToAction("Index");
        }


        public ActionResult SifreDegistir(string dlgid = "")
        {
            var mmsg = new MmMessage();
            mmsg.DialogID = dlgid;
            ViewBag.MmMessage = mmsg;

            ViewBag.EskiSifre = "";
            ViewBag.YeniSifre = "";
            ViewBag.YeniSifreTekrar = "";
            return View();
        }
        [HttpPost]
        public ActionResult SifreDegistir(string EskiSifre, string YeniSifre, string YeniSifreTekrar, string dlgid = "")
        {
            var mmsg = new MmMessage();
            mmsg.IsDialog = dlgid != "";
            mmsg.DialogID = dlgid;

            if (EskiSifre.IsNullOrWhiteSpace())
            {
                string msg = "Kullanmakta Olduğunuz Şifreyi Giriniz.";
                mmsg.Messages.Add(msg);
                mmsg.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EskiSifre" });
            }
            else mmsg.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EskiSifre" });
            if (YeniSifre.IsNullOrWhiteSpace())
            {
                string msg = "Yani Şifrenizi Giriniz.";
                mmsg.Messages.Add(msg);
                mmsg.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifre" });
            }
            else mmsg.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YeniSifre" });

            if (YeniSifreTekrar.IsNullOrWhiteSpace())
            {
                string msg = "Yeni Şifrenizi Tekrar Giriniz.";
                mmsg.Messages.Add(msg);
                mmsg.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifreTekrar" });
            }
            else mmsg.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YeniSifreTekrar" });

            if (mmsg.Messages.Count == 0 && YeniSifre != YeniSifreTekrar)
            {
                string msg = "Yeni Şifre İle Yeni Şifre Tekrar Birbiriyle Uyuşmuyor";
                mmsg.Messages.Add(msg);
                mmsg.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifre" });
                mmsg.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YeniSifreTekrar" });
            }
            var kullanici = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();
            if (mmsg.Messages.Count == 0 && kullanici.Sifre != EskiSifre.ComputeHash(Management.Tuz))
            {
                string msg = "Kullanmakta Olduğunuz Şifreyi Hatalı Girdiniz.";
                mmsg.Messages.Add(msg);
                mmsg.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EskiSifre" });
            }
            if (mmsg.Messages.Count == 0)
            {

                kullanici.Sifre = YeniSifre.ComputeHash(Management.Tuz);
                db.SaveChanges();
                mmsg.IsSuccess = true;
                mmsg.IsCloseDialog = true;
                MessageBox.Show("Şifre Değitrime İşlemi", MessageBox.MessageType.Success, "Şifre Değiştirme İşlemi Başarılı");
            }
            else
            {
                MessageBox.Show("Hatalı İşlem", MessageBox.MessageType.Error, mmsg.Messages.ToArray());
            }




            ViewBag.MmMessage = mmsg;
            ViewBag.EskiSifre = EskiSifre;
            ViewBag.YeniSifre = YeniSifre;
            ViewBag.YeniSifreTekrar = YeniSifreTekrar;
            return View();
        }




        [Authorize(Roles = RoleNames.KullanicilarSil)]
        public ActionResult Sil(int id)
        {
            var kayit = db.Kullanicilars.Where(p => p.KullaniciID == id).Single();

            string message = "";
            bool success = true;
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.Ad + " " + kayit.Soyad + "' Kullanıcısı Silindi!";
                    db.Kullanicilars.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Ad + " " + kayit.Soyad + "' Kullanıcısı  Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "Kullanicilar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Kullanıcı sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        public ActionResult SetLogin(int kullaniciId, string key = "")
        {
            if (!key.IsNullOrWhiteSpace())
            {
                var skullaniciId = UserIdentity.Current.Informations.Where(p => p.Key == key).Select(s => s.Value.toIntObj()).FirstOrDefault();
                kullaniciId = skullaniciId ?? UserIdentity.Current.Id;

            }
            else if (!RoleNames.KullanicilarKayit.InRoleCurrent()) return RedirectToAction("Index", "Home");
            var kullanici = db.Kullanicilars.Where(p => p.KullaniciID == kullaniciId).First();

            var prevUserKey = Guid.NewGuid().ToString();

            FormsAuthenticationUtil.SetAuthCookie(kullanici.KullaniciAdi, "", false);
            var ui = UserBus.GetUserIdentity(kullanici.KullaniciAdi);
            ui.Informations.Add("PrevUserKey", prevUserKey);
            ui.Informations.Add(prevUserKey, UserIdentity.Current.Id);
            Session["UserIdentity"] = ui;
            UserIdentity.SetCurrent();


            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
