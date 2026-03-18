using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity.Core.Objects;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [OutputCache(NoStore = false, Duration = 0, VaryByParam = "*")]
    public class KullanicilarController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        [Authorize(Roles = RoleNames.Kullanicilar)]
        public ActionResult Index()
        {

            return Index(new FmKullanicilarDto() { PageSize = 15, Expand = false });
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.Kullanicilar)]
        public ActionResult Index(FmKullanicilarDto model, List<string> programKod = null)
        {

            programKod = programKod ?? new List<string>();

            // Timeout süresini artır
            _entities.Database.CommandTimeout = 120; // 120 saniye

            // Sıralama parametrelerini ayarla
            string sortColumn = null;
            string sortDirection = "DESC";

            if (!model.Sort.IsNullOrWhiteSpace())
            {
                if (model.Sort.Contains("DESC"))
                {
                    sortDirection = "DESC";
                    sortColumn = model.Sort.Replace(" DESC", "").Trim();
                }
                else
                {
                    sortDirection = "ASC";
                    sortColumn = model.Sort;
                }

                // AdSoyad için özel mapping
                if (sortColumn == "AdSoyad")
                    sortColumn = "Ad";
                else if (sortColumn == "EnstituAdi")
                    sortColumn = "EnstituAd";
            }

            // Stored procedure output parametreleri 
            ObjectParameter totalCount = new ObjectParameter("TotalCount", typeof(int));
            ObjectParameter aktifCount = new ObjectParameter("AktifCount", typeof(int));
            ObjectParameter pasifCount = new ObjectParameter("PasifCount", typeof(int));

            // Stored procedure çağrısı
            var results = _entities.sp_SearchUsersOptimized(
                string.Join(",", UserIdentity.Current.EnstituKods),
                !string.IsNullOrWhiteSpace(model.EnstituKod) ? model.EnstituKod : null,
                !string.IsNullOrWhiteSpace(model.AdSoyad) ? model.AdSoyad : null,
                model.IsAktif,
                model.KullaniciTipID,
                model.BirimID,
                model.OgrenimTipKod,
                model.IsAdmin,
                model.OgrenimDurumID,
                model.CinsiyetID,
                model.YetkiGrupID,
                model.StartRowIndex,
                model.PageSize,
                sortColumn,
                sortDirection,
                totalCount,
                aktifCount,
                pasifCount
            ).ToList();

            // Sayfa bilgilerini model'e ata
            model.RowCount = totalCount?.Value == DBNull.Value ? 0 : Convert.ToInt32(totalCount?.Value ?? 0);

            // Index model bilgileri
            var indexModel = new MIndexBilgi
            {
                Toplam =
                    (aktifCount?.Value == DBNull.Value ? 0 : Convert.ToInt32(aktifCount?.Value ?? 0)) +
                    (pasifCount?.Value == DBNull.Value ? 0 : Convert.ToInt32(pasifCount?.Value ?? 0)),

                Aktif = aktifCount?.Value == DBNull.Value ? 0 : Convert.ToInt32(aktifCount?.Value ?? 0),
                Pasif = pasifCount?.Value == DBNull.Value ? 0 : Convert.ToInt32(pasifCount?.Value ?? 0)
            };


            // DTO dönüşümü
            model.KullanicilarDtos = results.Select(r => new FrKullanicilarDto
            {
                KullaniciID = r.KullaniciID.Value,
                UserKey = r.UserKey.Value,
                EnstituAdi = r.EnstituAd,
                EnstituKod = r.EnstituKod,
                YetkiGrupAdi = r.YetkiGrupAdi,
                KtipBasvuruYapabilir = r.KtipBasvuruYapabilir ?? false,
                ResimAdi = r.ResimAdi,
                KullaniciTipID = r.KullaniciTipID.Value,
                KullaniciTipAdi = r.KullaniciTipAdi,
                KullaniciAdi = r.KullaniciAdi,
                SicilNo = r.SicilNo,
                Ad = r.Ad,
                Soyad = r.Soyad,
                UnvanID = r.UnvanID,
                BirimID = r.BirimID,
                ABDKoordinatoru = r.ABDKoordinatoru ?? false,
                CinsiyetID = r.CinsiyetID,
                OgrenimTipKod = r.OgrenimTipKod,
                OgrenimDurumID = r.OgrenimDurumID,
                ProgramKod = r.ProgramKod,
                TcKimlikNo = r.TcKimlikNo,
                OgrenciNo = r.OgrenciNo,
                CepTel = r.CepTel,
                EMail = r.EMail,
                IsAktif = r.IsAktif.Value,
                IsAdmin = r.IsAdmin ?? false,
                YtuOgrencisi = r.YtuOgrencisi ?? false,
                OlusturmaTarihi = r.OlusturmaTarihi,
                LastLogonDate = r.LastLogonDate,
                LastLogonIP = r.LastLogonIP,
                IslemTarihi = r.IslemTarihi,
                IslemYapanIP = r.IslemYapanIP
            }).ToArray();

            // ViewBag ayarları
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.BirimID = new SelectList(BirimlerBus.GetBirimlerTreeList(), "BirimID", "BirimAdi", model.BirimID);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.IsAdmin = new SelectList(ComboData.GetCmbVarYokData(true), "Value", "Caption", model.IsAdmin);
            ViewBag.ProgramKod = new SelectList(ProgramlarBus.CmbGetAktifProgramlar(), "Value", "Caption", model.ProgramKod);
            ViewBag.OgrenimDurumID = new SelectList(KullanicilarBus.CmbAktifOgrenimDurumu(true, isHesapKayittaGozuksun: true), "Value", "Caption", model.OgrenimDurumID);
            ViewBag.KullaniciTipID = new SelectList(KullanicilarBus.GetCmbKullaniciTipleri(true, false), "Value", "Caption", model.KullaniciTipID);
            ViewBag.CinsiyetID = new SelectList(KullanicilarBus.CmbCinsiyetler(true), "Value", "Caption", model.CinsiyetID);
            ViewBag.YetkiGrupID = new SelectList(YetkiGrupBus.CmbYetkiGruplari(), "Value", "Caption", model.YetkiGrupID);
            ViewBag.SelectedPrograms = programKod;
            ViewBag.IndexModel = indexModel;

            return View(model);

        }
        [Authorize(Roles = RoleNames.KullanicilarKayit)]
        public ActionResult Kayit(int? id, string ekd)
        {
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var model = new Kullanicilar
            {
                IsAktif = true
            };
            bool isKurumIci = true;
            bool isYerli = true;
            bool resimVar = false;
            if (id.HasValue && id > 0)
            {
                var data = _entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == id);
                if (data != null)
                {
                    isKurumIci = data.KullaniciTipleri.KurumIci;
                    isYerli = data.KullaniciTipleri.Yerli;
                    resimVar = data.ResimAdi.IsNullOrWhiteSpace() == false;
                    data.ResimAdi = data.ResimAdi;
                    model = data;
                }
                model.Sifre = "";
            }
            else
            {
                model.EnstituKod = enstituKod;
            }
            ViewBag.ResimVar = resimVar;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            if (model.OgrenimEnstituKod.IsNullOrWhiteSpace())
                model.OgrenimEnstituKod = model.EnstituKod;
            ViewBag.OgrenimEnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.OgrenimEnstituKod);
            ViewBag.KullaniciTipID = new SelectList(KullanicilarBus.GetCmbKullaniciTipleri(true, false), "Value", "Caption", model.KullaniciTipID);
            ViewBag.UnvanID = new SelectList(UnvanlarBus.CmbUnvanlar(true, model.KullaniciTipID == KullaniciTipiEnum.AkademikPersonel), "Value", "Caption", model.UnvanID);
            ViewBag.BirimID = new SelectList(BirimlerBus.CmbBirimler(true), "Value", "Caption", model.BirimID);
            ViewBag.CinsiyetID = new SelectList(KullanicilarBus.CmbCinsiyetler(true), "Value", "Caption", model.CinsiyetID);

            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.ProgramKod = new SelectList(ProgramlarBus.CmbGetAktifProgramlar(model.OgrenimEnstituKod, true, true), "Value", "Caption", model.ProgramKod);
            ViewBag.OgrenimDurumID = new SelectList(KullanicilarBus.CmbAktifOgrenimDurumu(true, isHesapKayittaGozuksun: true), "Value", "Caption", model.OgrenimDurumID);
            ViewBag.YetkiGrupID = new SelectList(YetkiGrupBus.CmbYetkiGruplari(), "Value", "Caption", model.YetkiGrupID);
            ViewBag.IsKurumIci = isKurumIci;
            ViewBag.IsYerli = isYerli;
            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.KullanicilarKayit)]
        public ActionResult Kayit(Kullanicilar kModel, HttpPostedFileBase profilResmi, bool yetkilendirmeyeGit = false)
        {
            var mmMessage = new MmMessage();
            bool isKurumIci = true;
            bool isYerli = true;
            var erisimYetki = RoleNames.KullanicilarIslemYetkileri.InRoleCurrent();
            var isOgrenci = new List<int> { KullaniciTipiEnum.YerliOgrenci, KullaniciTipiEnum.YabanciOgrenci }.Contains(kModel.KullaniciTipID);
            #region Kontrol
            kModel.KullaniciAdi = kModel.KullaniciAdi != null ? kModel.KullaniciAdi.Trim() : "";
            if (erisimYetki)
            {
                if (kModel.YetkiGrupID <= 0)
                {
                    mmMessage.Messages.Add("Yetki Grubu Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "YetkiGrupID" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "YetkiGrupID" });
            }
            if (kModel.KullaniciTipID <= 0)
            {
                mmMessage.Messages.Add("Kullanıcı Tipi Seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "KullaniciTipID" });
            }
            else
            {
                var ktp = _entities.KullaniciTipleris.First(p => p.KullaniciTipID == kModel.KullaniciTipID);
                isKurumIci = ktp.KurumIci;
                isYerli = ktp.Yerli;
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "KullaniciTipID" });
            }
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Kullanıcının Kayıt Edileceği Enstitüyü Seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EnstituKod" });

            if (kModel.KullaniciID <= 0)
            {
                if (profilResmi == null)
                {
                    mmMessage.Messages.Add("Profil Resmi Yükleyiniz");
                }
            }
            else
            {
                var kul = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);
                if (kul.ResimAdi.IsNullOrWhiteSpace() && profilResmi == null)
                {
                    mmMessage.Messages.Add("Profil Resmi Yükleyiniz");
                }
                else kModel.ResimAdi = kul.ResimAdi;
            }
            if (kModel.Ad.IsNullOrWhiteSpace())
            {

                mmMessage.Messages.Add("Ad Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Ad" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "Ad" });
            if (kModel.Soyad.IsNullOrWhiteSpace())
            {

                mmMessage.Messages.Add("Soyad Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Soyad" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "Soyad" });

            if (kModel.TcKimlikNo.IsNullOrWhiteSpace())
            {

                mmMessage.Messages.Add("T.C. Kimlik No Giriniz.");

                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TcKimlikNo" });
            }
            else if (kModel.TcKimlikNo.IsNumber() == false)
            {

                mmMessage.Messages.Add("T.C. Kimlik No Sadece sayıdan oluşmalıdır");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TcKimlikNo" });

            }
            else if (kModel.TcKimlikNo.Length != 11)
            {

                mmMessage.Messages.Add("T.C. Kimlik No 11 haneli olmalıdır");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TcKimlikNo" });

            }
            //else if (!kModel.TcKimlikNo.ToIsValidateTckn())
            //{
            //    mmMessage.Messages.Add("T.C. Kimlik Numarasını hatalı girmediğinizden emin olunuz.");
            //    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TcKimlikNo" });
            //}
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TcKimlikNo" });

            if (!kModel.CinsiyetID.HasValue)
            {
                mmMessage.Messages.Add("Cinsiyet Bilgisini Seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "CinsiyetID" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "CinsiyetID" });



            if (kModel.CepTel.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Cep Telefonu Numarası Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "CepTel" });
            }
            else
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "CepTel" });
            }

            if (kModel.EMail.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("E Mail Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EMail" });
            }
            else if (!kModel.EMail.ToIsValidEmail())
            {
                mmMessage.Messages.Add("Lütfen EMail Formatını Doğru Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EMail" });
            }
            else
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EMail" });
            }
            if (!isKurumIci || !isYerli)
                if (kModel.Adres.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Adres Bilgisi Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Adres" });
                }
                else
                {
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "Adres" });
                }

            if (isOgrenci && !kModel.YtuOgrencisi && kModel.IsAktif)
            {
                mmMessage.Messages.Add("Seçilen kullanıcı tipi için Ytü öğrencisi olduğunuzu belirtmek zorunludur.");
                mmMessage.MessagesDialog.Add(new MrMessage
                {
                    MessageType = MsgTypeEnum.Warning,
                    PropertyName = "YtuOgrencisi"
                });
            }
            if (kModel.YtuOgrencisi)
            {
                if (kModel.OgrenimEnstituKod.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Öğrenci Olduğunuz Enstitüyü Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage
                    {
                        MessageType = MsgTypeEnum.Warning,
                        PropertyName = "OgrenimEnstituKod"
                    });
                }
                if (kModel.OgrenciNo.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Öğrenci No Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgrenciNo" });
                }
                else if (kModel.OgrenciNo.Length > 20)
                {

                    mmMessage.Messages.Add("Öğrenci Numarası 20 Haneden Daha Fazla Olamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgrenciNo" });

                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OgrenciNo" });
                if (kModel.OgrenimTipKod.HasValue == false)
                {
                    mmMessage.Messages.Add("Öğrenim Seviyesi Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgrenimTipKod" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OgrenimTipKod" });
                if (kModel.ProgramKod.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Program Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "ProgramKod" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "ProgramKod" });
                if (kModel.OgrenimDurumID.HasValue == false)
                {

                    mmMessage.Messages.Add("Öğrenim durumunuzu Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgrenimDurumID" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OgrenimDurumID" });




            }
            if (isKurumIci)
                if (!kModel.BirimID.HasValue)
                {
                    mmMessage.Messages.Add("Birim Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BirimID" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "BirimID" });
            if (isKurumIci)
                if (!kModel.UnvanID.HasValue)
                {
                    mmMessage.Messages.Add("Unvan Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "UnvanID" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "UnvanID" });
            if (isKurumIci)
                if (kModel.SicilNo.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Sicil No Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SicilNo" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "SicilNo" });


            if (kModel.IzinBaslamaTarihi.HasValue || kModel.IzinBitisTarihi.HasValue)
            {
                if (!kModel.IzinBaslamaTarihi.HasValue)
                {
                    mmMessage.Messages.Add("İzin Başlangıç Tarihi giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IzinBaslamaTarihi" });
                }
                if (!kModel.IzinBitisTarihi.HasValue)
                {
                    mmMessage.Messages.Add("İzin Bitiş Tarihi giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IzinBitisTarihi" });
                }

                if (kModel.IzinBaslamaTarihi > kModel.IzinBitisTarihi)
                {
                    mmMessage.Messages.Add("İzin Başlangıç Tarihi, İzin Bitiş Tarihinden büyük olamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IzinBaslamaTarihi" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IzinBitisTarihi" });
                }
            }

            if (kModel.KullaniciAdi.IsNullOrWhiteSpace())
            {

                mmMessage.Messages.Add("Kullanıcı Adı Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "KullaniciAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "KullaniciAdi" });

            if (kModel.KullaniciID <= 0)
            {
                if (kModel.Sifre.IsNullOrWhiteSpace())
                {

                    mmMessage.Messages.Add("Şifre Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Sifre" });
                }
                else if (kModel.Sifre.Length < 4)
                {

                    mmMessage.Messages.Add("Şifre en az 4 haneli olmalıdır.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Sifre" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "Sifre" });
            }
            else if (!kModel.Sifre.IsNullOrWhiteSpace())
            {
                if (kModel.Sifre.Length < 4 && kModel.KullaniciID > 0)
                {

                    mmMessage.Messages.Add("Şifre en az 4 haneli olmalıdır.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Sifre" });
                }
                else if (kModel.Sifre.Length >= 4 && kModel.KullaniciID > 0) mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "Sifre" });
            }

            #endregion

            if (mmMessage.Messages.Count == 0)
            {
                var qPersonel = _entities.Kullanicilars.AsQueryable();
                var cUserName = qPersonel.Count(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.KullaniciAdi == kModel.KullaniciAdi);
                if (cUserName > 0)
                {

                    mmMessage.Messages.Add("Tanımlamak istediğiniz kullanıcı adı sistemde zaten mevcut!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "KullaniciAdi" });
                }

                var cTc = qPersonel.Count(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.TcKimlikNo == kModel.TcKimlikNo);
                if (cTc > 0)
                {
                    mmMessage.Messages.Add("Tanımlamak istediğiniz Kimlik No sistemde zaten mevcut!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TcKimlikNo" });
                }
                if (isKurumIci)
                {
                    var cSicil = qPersonel.Count(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.SicilNo == kModel.SicilNo);
                    if (cSicil > 0)
                    {
                        mmMessage.Messages.Add("Tanımlamak istediğiniz Sicil No sistemde zaten mevcut!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SicilNo" });
                    }
                }

                if (kModel.YtuOgrencisi)
                {

                    if (kModel.KullaniciID <= 0)
                    {
                        var cOgrNo = qPersonel.Count(p => p.IsAktif && p.KullaniciID != kModel.KullaniciID && p.OgrenciNo == kModel.OgrenciNo);
                        if (cOgrNo > 0)
                        {
                            mmMessage.Messages.Add("Girmiş olduğunuz öğrenci numarası ile daha önceden sisteme kayıt yapılmıştır. Tekrar kayıt yapamazsınız!");
                            mmMessage.MessagesDialog.Add(new MrMessage
                            { MessageType = MsgTypeEnum.Warning, PropertyName = "OgrenciNo" });
                        }
                    }

                    //if (kModel.OgrenimDurumID != OgrenimDurumEnum.OzelOgrenci)
                    //{
                    var ogrenciInfo = KullanicilarBus.OgrenciKontrol(kModel.OgrenciNo);
                    if (ogrenciInfo.Hata)
                    {
                        mmMessage.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu! " + ogrenciInfo.HataMsj);
                    }
                    else
                    {
                        if (ogrenciInfo.KayitVar)
                        {
                            if (kModel.TcKimlikNo != ogrenciInfo.OgrenciInfo.TCKIMLIKNO)
                            {
                                mmMessage.Messages.Add(
                                    "Girdiğiniz Öğrenci Numarası bilgisi OBS sisteminde doğrulanamadı.");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgrenciNo" });
                            }
                            if (kModel.OgrenimTipKod != ogrenciInfo.OgrenciInfo.OGRENIMSEVIYE_ID.ToIntObj())
                            {
                                mmMessage.Messages.Add(
                                    "Girdiğiniz Öğrenim Seviyesi bilgisi OBS sisteminde doğrulanamadı.");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgrenimTipKod" });
                            }
                            if (!mmMessage.Messages.Any())
                            {
                                kModel.ProgramKod = kModel.ProgramKod;
                                kModel.OgrenimTipKod = ogrenciInfo.OgrenciInfo.OGRENIMSEVIYE_ID.ToIntObj().Value;
                                kModel.KayitTarihi = ogrenciInfo.KayitTarihi;
                                kModel.KayitYilBaslangic = ogrenciInfo.BaslangicYil;
                                kModel.KayitDonemID = ogrenciInfo.DonemID;
                            }

                        }
                        else
                        {
                            mmMessage.Messages.Add(
                                "Girdiğiniz Kimlik bilgisi OBS sisteminde doğrulanamadı.");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TcKimlikNo" });
                        }

                    }
                    //}

                }

            }
            if (mmMessage.Messages.Count == 0 && isKurumIci)
            {
                var emailHosts = new List<string> { "@std.yildiz.edu.tr", "@yildiz.edu.tr" };
                if (kModel.IsActiveDirectoryUser && (emailHosts.Any(a => kModel.EMail.Contains(a)) == false))
                {
                    mmMessage.Messages.Add("Active Directory Girişi Yapmasını İstediğiniz Kullanıcının yildiz.edu.tr e mailini tanımlamanız gerekir!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsActiveDirectoryUser" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EMail" });
                }
            }

            if (mmMessage.Messages.Count == 0)
            {

                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanIP = UserIdentity.Ip;
                if (!isKurumIci)
                {
                    kModel.BirimID = null;
                    kModel.UnvanID = null;
                    kModel.SicilNo = "";
                    kModel.ABDKoordinatoru = false;
                }
                else
                {
                    kModel.Adres = "";

                }
                if (!kModel.YtuOgrencisi)
                {
                    kModel.OgrenimEnstituKod = null;
                    kModel.OgrenciNo = null;
                    kModel.OgrenimTipKod = null;
                    kModel.OgrenimDurumID = null;
                    kModel.ProgramKod = null;
                    kModel.KayitTarihi = null;
                    kModel.KayitYilBaslangic = null;
                    kModel.KayitDonemID = null;
                }
                else
                {

                    kModel.OgrenciNo = kModel.OgrenciNo.RemoveNonAlphanumeric();
                }
                if (isOgrenci)
                {
                    kModel.EnstituKod = kModel.OgrenimEnstituKod;
                }
                kModel.TcKimlikNo = kModel.TcKimlikNo.RemoveNonAlphanumeric();
                var yeniKullanici = kModel.KullaniciID <= 0;
                if (yeniKullanici)
                {
                    var sfr = kModel.Sifre;
                    kModel.UserKey = Guid.NewGuid();
                    kModel.YetkiGrupID = erisimYetki ? kModel.YetkiGrupID : 1;
                    kModel.IsTezAtamaAcik = YetkiGrupBus.TezKontrolYetkiGrupId == kModel.YetkiGrupID;
                    kModel.OlusturmaTarihi = DateTime.Now;
                    kModel.Sifre = kModel.Sifre.ComputeHash(GlobalSistemSetting.Tuz);
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

                    if (profilResmi != null)
                    {
                        kModel.ResimAdi = KullanicilarBus.ResimKaydet(profilResmi);

                    }

                    kModel = _entities.Kullanicilars.Add(kModel);
                    _entities.SaveChanges();
                    _entities.KullaniciEnstituYetkileris.Add(new KullaniciEnstituYetkileri
                    {
                        EnstituKod = kModel.EnstituKod,
                        KullaniciID = kModel.KullaniciID,
                        IslemYapanID = kModel.IslemYapanID.Value,
                        IslemTarihi = kModel.IslemTarihi.Value,
                        IslemYapanIP = kModel.IslemYapanIP

                    });
                    _entities.SaveChanges();

                    var sended = KullanicilarBus.SendMailYeniHesap(kModel, sfr);
                    if (!sended.IsSuccess)
                    {
                        mmMessage.Messages.Add(kModel.KullaniciAdi + " kullanıcı hesabı oluşturuldu fakat kullanıcıya bilgi maili atılırken bir hata oluştu!");
                        mmMessage.Messages.AddRange(sended.Messages);
                        MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
                    }
                }
                else
                {
                    var kullanici = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);
                    kullanici.EnstituKod = kModel.EnstituKod;
                    kullanici.OgrenimEnstituKod = kModel.OgrenimEnstituKod;
                    kullanici.KullaniciTipID = kModel.KullaniciTipID;
                    kullanici.Ad = kModel.Ad;
                    var eskiYetkiGrupId = kullanici.YetkiGrupID;
                    var yeniYetkiGrupId = kModel.YetkiGrupID;
                    bool isYetkiDegisti = false;
                    if (erisimYetki)
                    {
                        isYetkiDegisti = eskiYetkiGrupId != yeniYetkiGrupId;
                        kullanici.YetkiGrupID = yeniYetkiGrupId;

                        var tezKontrolYetkisineYeniGecis =
                            isYetkiDegisti &&
                            yeniYetkiGrupId == YetkiGrupBus.TezKontrolYetkiGrupId &&
                            eskiYetkiGrupId != YetkiGrupBus.TezKontrolYetkiGrupId;

                        if (tezKontrolYetkisineYeniGecis && !kullanici.IsTezAtamaAcik.HasValue)
                        {
                            kullanici.IsTezAtamaAcik = true;
                        }
                    }
                     
                    kullanici.Soyad = kModel.Soyad;
                    kullanici.TcKimlikNo = kModel.TcKimlikNo;
                    kullanici.CinsiyetID = kModel.CinsiyetID;
                    kullanici.CepTel = kModel.CepTel;
                    kullanici.EMail = kModel.EMail;
                    kullanici.Adres = kModel.Adres;

                    kullanici.YtuOgrencisi = kModel.YtuOgrencisi;
                    kullanici.OgrenimDurumID = kModel.OgrenimDurumID;
                    kullanici.OgrenimTipKod = kModel.OgrenimTipKod;
                    kullanici.OgrenciNo = kModel.OgrenciNo;
                    kullanici.ProgramKod = kModel.ProgramKod;
                    kullanici.KayitTarihi = kModel.KayitTarihi;
                    kullanici.KayitYilBaslangic = kModel.KayitYilBaslangic;
                    kullanici.KayitDonemID = kModel.KayitDonemID;

                    kullanici.BirimID = kModel.BirimID;
                    kullanici.UnvanID = kModel.UnvanID;
                    kullanici.SicilNo = kModel.SicilNo;
                    kullanici.ABDKoordinatoru = kModel.ABDKoordinatoru;

                    kullanici.IzinBaslamaTarihi = kModel.IzinBaslamaTarihi;
                    kullanici.IzinBitisTarihi = kModel.IzinBitisTarihi;

                    kullanici.KullaniciAdi = kModel.KullaniciAdi;
                    if (!kModel.Sifre.IsNullOrWhiteSpace())
                        kullanici.Sifre = kModel.Sifre.ComputeHash(GlobalSistemSetting.Tuz);
                    kullanici.SifresiniDegistirsin = kModel.SifresiniDegistirsin;
                    kullanici.Aciklama = kModel.Aciklama;
                    kullanici.IsActiveDirectoryUser = kModel.IsActiveDirectoryUser;
                    kullanici.IsAdmin = kModel.IsAdmin;
                    kullanici.IsAktif = kModel.IsAktif;

                    kullanici.IslemYapanID = kModel.IslemYapanID;
                    kullanici.IslemTarihi = kModel.IslemTarihi;
                    kullanici.IslemYapanIP = kModel.IslemYapanIP;
                    if (profilResmi != null)
                    {
                        if (kullanici.ResimAdi.IsNullOrWhiteSpace() == false)
                        {
                            var eskiResimLazim = LisansustuBasvuruBus.ResimBilgisiLazimOlanKayitVarMi(kullanici.KullaniciID);
                            if (eskiResimLazim == false)
                            {
                                var rsmYol = SistemAyar.KullaniciResimYolu;
                                var rsm = Server.MapPath("~/" + rsmYol + "/" + kullanici.ResimAdi);
                                if (System.IO.File.Exists(rsm)) System.IO.File.Delete(rsm);
                            }
                        }
                        kullanici.ResimAdi = KullanicilarBus.ResimKaydet(profilResmi);
                    }
                    _entities.SaveChanges();
                    _entities.SaveChanges();
                    if (kullanici.KullaniciEnstituYetkileris.All(a => a.EnstituKod != kullanici.EnstituKod))
                    {
                        _entities.KullaniciEnstituYetkileris.Add(new KullaniciEnstituYetkileri
                        {
                            EnstituKod = kullanici.EnstituKod,
                            KullaniciID = kullanici.KullaniciID,
                            IslemYapanID = kullanici.IslemYapanID.Value,
                            IslemTarihi = kullanici.IslemTarihi.Value,
                            IslemYapanIP = kullanici.IslemYapanIP

                        });
                        _entities.SaveChanges();
                    }
                    LogIslemleri.LogEkle("Kullanicilar", LogCrudType.Update, kullanici.ToJson());
                    if (isYetkiDegisti) UserBus.SetUserRoles(kullanici.KullaniciID, new List<int>(), kullanici.YetkiGrupID);
                    if (kullanici.KullaniciID == UserIdentity.Current.Id) { UserIdentity.Current.ImagePath = kullanici.ResimAdi.ToKullaniciResim(); }

                }


                return yetkilendirmeyeGit ? RedirectToAction("Yetkilendirme", new { id = kModel.KullaniciID }) : RedirectToAction("Index");

            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            if (kModel.OgrenimEnstituKod.IsNullOrWhiteSpace())
                kModel.OgrenimEnstituKod = kModel.EnstituKod;
            ViewBag.OgrenimEnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.OgrenimEnstituKod);
            ViewBag.KullaniciTipID = new SelectList(KullanicilarBus.GetCmbKullaniciTipleri(true, false), "Value", "Caption", kModel.KullaniciTipID);
            ViewBag.UnvanID = new SelectList(UnvanlarBus.CmbUnvanlar(true, kModel.KullaniciTipID == KullaniciTipiEnum.AkademikPersonel), "Value", "Caption", kModel.UnvanID);
            ViewBag.BirimID = new SelectList(BirimlerBus.CmbBirimler(true), "Value", "Caption", kModel.BirimID);
            ViewBag.CinsiyetID = new SelectList(KullanicilarBus.CmbCinsiyetler(true), "Value", "Caption", kModel.CinsiyetID);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(kModel.EnstituKod, true), "Value", "Caption", kModel.OgrenimTipKod);
            ViewBag.ProgramKod = new SelectList(ProgramlarBus.CmbGetAktifProgramlar(kModel.EnstituKod, true, true), "Value", "Caption", kModel.ProgramKod);
            ViewBag.OgrenimDurumID = new SelectList(KullanicilarBus.CmbAktifOgrenimDurumu(true, isHesapKayittaGozuksun: true), "Value", "Caption", kModel.OgrenimDurumID);
            ViewBag.YetkiGrupID = new SelectList(YetkiGrupBus.CmbYetkiGruplari(), "Value", "Caption", kModel.YetkiGrupID);

            ViewBag.ResimVar = kModel.ResimAdi.IsNullOrWhiteSpace() == false;
            ViewBag.MmMessage = mmMessage;
            ViewBag.IsKurumIci = isKurumIci;
            ViewBag.IsYerli = isYerli;
            return View(kModel);
        }

        [Authorize(Roles = RoleNames.KullanicilarIslemYetkileri)]
        public ActionResult Yetkilendirme(int? id)
        {
            if (id.HasValue == false) return RedirectToAction("Index");
            var kid = id;
            var roles = RollerBus.GetAllRoles().ToList();
            var userRoles = UserBus.GetUserRoles(kid.Value);
            var kullanici = UserBus.GetUser(kid.Value);
            ViewBag.Kullanici = kullanici;
            var data = roles.Select(s => new CheckObjectDto<Roller>
            {
                Value = s,
                Disabled = userRoles.YetkiGrupRolleri.Any(a => a.RolID == s.RolID),
                Checked = userRoles.TumRoller.Any(p => p.RolID == s.RolID)
            });
            ViewBag.Roller = data;
            var kategr = roles.Select(s => s.Kategori).Distinct().ToArray();
            var menuK = _entities.Menulers.Where(a => a.BagliMenuID == 0 && kategr.Contains(a.MenuAdi)).ToList();
            var dct = new List<CmbIntDto>();
            foreach (var item in menuK)
            {
                dct.Add(new CmbIntDto { Value = item.SiraNo.Value, Caption = item.MenuAdi });
            }
            ViewBag.cats = dct;
            ViewBag.YetkiGrupID = new SelectList(YetkiGrupBus.CmbYetkiGruplari(), "Value", "Caption", kullanici.YetkiGrupID);
            return View();
        }
        [HttpPost, ActionName("Yetkilendirme")]
        [Authorize(Roles = RoleNames.KullanicilarIslemYetkileri)]
        public ActionResult Yetkilendirme(List<int> rolId, int kullaniciId, int yetkiGrupId, string ekd, bool programYetkilerineGit = false)
        {

            rolId = rolId ?? new List<int>();
            UserBus.SetUserRoles(kullaniciId, rolId, yetkiGrupId);
            MessageBox.Show("Yetkiler Kaydedildi", MessageBox.MessageType.Success);
            if (programYetkilerineGit) return RedirectToAction("KullaniciProgramYetkileri", new { id = kullaniciId, EKD = ekd });
            return RedirectToAction("Index");
        }
        public ActionResult GetYetkiGrubuRolIDs(int id, int kullaniciId)
        {
            var checkedRollIDs = new List<CmbIntDto>();

            var kullanici = _entities.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
            if (kullanici.YetkiGrupID == id) checkedRollIDs = kullanici.Rollers.Select(s => new CmbIntDto { Value = s.RolID, Caption = RenkTipiEnum.Info }).ToList();
            var yetkiGrupRollIDs = _entities.YetkiGrupRolleris.Where(p => p.YetkiGrupID == id).Select(s => new CmbIntDto { Value = s.RolID, Caption = RenkTipiEnum.Danger }).ToList();
            checkedRollIDs.AddRange(yetkiGrupRollIDs);
            return checkedRollIDs.ToJsonResult();
        }



        [Authorize(Roles = RoleNames.KullanicilarEnstituYetkileri)]
        public ActionResult YetkilendirmeEnstitu(int? id)
        {
            if (id.HasValue == false) return RedirectToAction("Index");

            var roles = EnstituBus.GetEnstituler(true);
            var userRoles = UserBus.GetKullaniciEnstituler(id.Value);
            var kullanici = UserBus.GetUser(id.Value);
            ViewBag.Kullanici = kullanici;
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
        public ActionResult YetkilendirmeEnstitu(List<string> enstituKods, int kullaniciId)
        {
            var eKods = UserIdentity.Current.EnstituKods;
            if (enstituKods == null) enstituKods = new List<string>();
            var gEnstitu = _entities.KullaniciEnstituYetkileris.Where(p => p.KullaniciID == kullaniciId).AsQueryable();
            if (UserIdentity.Current.IsAdmin == false)
            {
                gEnstitu = gEnstitu.Where(p => eKods.Contains(p.EnstituKod));
            }
            _entities.KullaniciEnstituYetkileris.RemoveRange(gEnstitu.ToList());
            foreach (var item in enstituKods)
            {
                _entities.KullaniciEnstituYetkileris.Add(new KullaniciEnstituYetkileri
                {
                    EnstituKod = item,
                    KullaniciID = kullaniciId,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id,
                    IslemYapanIP = UserIdentity.Ip
                });
            }
            if (UserIdentity.Current.Id == kullaniciId) UserIdentity.Current.EnstituKods = enstituKods;
            _entities.SaveChanges();
            MessageBox.Show("Yetkiler Kaydedildi", MessageBox.MessageType.Success);
            return RedirectToAction("Index");
        }


        [Authorize(Roles = RoleNames.KullanicilarProgramYetkileri)]
        public ActionResult KullaniciProgramYetkileri(int? id, string ekd)
        {
            if (id.HasValue == false) return RedirectToAction("Index");
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var data = KullanicilarBus.GetKullaniciProgramlari(id.Value, enstituKod);
            var kullanici = UserBus.GetUser(id.Value);
            ViewBag.Kullanici = kullanici;
            return View(data);
        }
        [HttpPost, ActionName("KullaniciProgramYetkileri")]
        [Authorize(Roles = RoleNames.KullanicilarProgramYetkileri)]
        public ActionResult KullaniciProgramYetkileri(List<string> programKod, int kullaniciId, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            if (kullaniciId <= 0)
            {
                return RedirectToAction("Index");
            }
            programKod = programKod ?? new List<string>();
            var kProg = _entities.KullaniciProgramlaris.Where(p => p.KullaniciID == kullaniciId && p.Programlar.AnabilimDallari.EnstituKod.Contains(enstituKod)).ToList();
            _entities.KullaniciProgramlaris.RemoveRange(kProg);
            foreach (var item in programKod)
            {
                _entities.KullaniciProgramlaris.Add(new KullaniciProgramlari { KullaniciID = kullaniciId, ProgramKod = item });
            }
            _entities.SaveChanges();

            MessageBox.Show("Program Yetkileri Kaydedildi", MessageBox.MessageType.Success);
            return RedirectToAction("Index");
        }
        [Authorize(Roles = RoleNames.KullanicilarSil)]
        public ActionResult Sil(Guid userKey)
        {
            var kayit = _entities.Kullanicilars.Single(p => p.UserKey == userKey);

            string message;
            bool success = true;
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.Ad + " " + kayit.Soyad + "' Kullanıcısı Silindi!";
                    _entities.Kullanicilars.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Ad + " " + kayit.Soyad + "' Kullanıcısı  Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Kullanıcı sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        public ActionResult SetLogin(Guid userKey, string key = "")
        {

            if (!key.IsNullOrWhiteSpace())
            {
                var sUserKey = UserIdentity.Current.Informations.Where(p => p.Key == key).Select(s => s.Value.ToGuidObj()).FirstOrDefault();
                userKey = sUserKey ?? UserIdentity.Current.UserKey;

            }
            else if (!RoleNames.KullaniciHesabinaGecmeYetkisi.InRoleCurrent()) return RedirectToAction("Index", "Home");
            var kullanici = _entities.Kullanicilars.First(p => p.UserKey == userKey);
            var enstitu = _entities.Enstitulers.First(f => f.EnstituKod == kullanici.EnstituKod);
            var ekd = enstitu.EnstituKisaAd.ToLower();
            var prevUserKey = Guid.NewGuid().ToString();

            FormsAuthenticationUtil.SetAuthCookie(kullanici.KullaniciAdi, String.Empty, false);
            var ui = UserBus.GetUserIdentity(kullanici.KullaniciAdi);
            ui.Informations.Add("PrevUserKey", prevUserKey);
            ui.Informations.Add(prevUserKey, UserIdentity.Current.UserKey);
            Session["UserIdentity"] = ui;
            UserIdentity.SetCurrent();

            var redirectUrl = $"/{ekd}/home/index";
            return Redirect(redirectUrl);
        }

        protected override void Dispose(bool disposing)
        {
            _entities.Dispose();
            base.Dispose(disposing);
        }
    }
}
