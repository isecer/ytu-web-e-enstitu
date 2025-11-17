using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.WebServiceData.ObsRestData;
using LisansUstuBasvuruSistemi.WebServiceData.ObsService;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Business
{
    public class KullanicilarBus
    {

        public static StudentControl OgrenciKontrol(string tcOrOgrenciNo = null, string donemId = "")
        {
            var obsData = new ObsServiceData();
            //if (donemId == null)
            //{
            //    var donem = DateTime.Now.Date.ToAraRaporDonemBilgi();
            //    donemId = donem.BaslangicTarihi.Year + "" + donem.DonemID;
            //} 
            return obsData.GetObsStudentControl(tcOrOgrenciNo, donemId);
        }
        public static async Task<MmMessage> OgrenciBilgileriniObsdenGetir(string tcKimlikNo, string ogrenciNo)
        {

            var messageModel = new MmMessage { Title = "OBS Bilgi Kontrolü" };

            try
            {
                using (var _entities = new LubsDbEntities())
                {
                    var ogrenciKontrol = KullanicilarBus.OgrenciKontrol(tcKimlikNo.Trim());

                    if (ogrenciKontrol == null || ogrenciKontrol.Hata)
                    {
                        messageModel.Messages.Add("OBS sisteminde öğrenci kaydınız bulunamadı: " +
                                                  (ogrenciKontrol?.HataMsj ?? ""));
                        messageModel.MessageType = MsgTypeEnum.Error;
                        return messageModel;
                    }

                    if (!ogrenciKontrol.KayitVar)
                    {
                        messageModel.Messages.Add("OBS sisteminde aktif öğrenci kaydınız bulunamadı.");
                        messageModel.MessageType = MsgTypeEnum.Error;
                        return messageModel;
                    }

                    var ogrenci = ogrenciKontrol.OgrenciInfo;
                    if (ogrenci == null)
                    {
                        messageModel.Messages.Add("Öğrenci bilgileri alınamadı.");
                        messageModel.MessageType = MsgTypeEnum.Error;
                        return messageModel;
                    }

                    if (ogrenci.OGR_NO?.Trim() != ogrenciNo.Trim())
                    {
                        messageModel.Messages.Add("Girdiğiniz Öğrenci Numarası TC Kimlik Numaranız ile eşleşmiyor.");
                        messageModel.Messages.Add("OBS'de kayıtlı öğrenci numaranız: " + ogrenci.OGR_NO);
                        messageModel.MessageType = MsgTypeEnum.Error;
                        return messageModel;
                    }

                    if (ogrenci.OGRENIMDURUM_ID?.ToUpper() == "MEZ")
                    {
                        messageModel.Messages.Add(
                            "Mezun olan öğrencilerin belge talep işlemini Davutpaşa Öğrenci İşleri Daire Başkanlığından yapmaları gerekmektedir.");
                        messageModel.MessageType = MsgTypeEnum.Error;
                        return messageModel;
                    }

                    // Program eşleştirmesi
                    var obsProgramlar = await ObsProgramCacheService.GetProgramsFlatAsync();
                    if (obsProgramlar == null || !obsProgramlar.Any())
                    {
                        messageModel.Messages.Add("OBS program listesi alınamadı.");
                        messageModel.MessageType = MsgTypeEnum.Error;
                        return messageModel;
                    }

                    var obsProgram = obsProgramlar.FirstOrDefault(p => p.ProgramId == ogrenci.PROGRAM_ID);
                    if (obsProgram == null)
                    {
                        messageModel.Messages.Add("OBS'de programınız bulunamadı");
                        messageModel.MessageType = MsgTypeEnum.Error;
                        return messageModel;
                    }

                    var obsProgramKod = ProgramKodGuncellemeBus.ProgramKodAyikla(obsProgram.ProgramKod);
                    var dbProgram =
                        _entities.Programlars.FirstOrDefault(p => p.ObsProgramKod == obsProgramKod && p.IsAktif);

                    if (dbProgram == null)
                    {
                        messageModel.Messages.Add("Programınız e-Enstitü sisteminde eşleştirilemedi.");
                        messageModel.MessageType = MsgTypeEnum.Warning;
                        return messageModel;
                    }

                    string enstituKod = dbProgram.AnabilimDallari.EnstituKod;
                    if (string.IsNullOrWhiteSpace(enstituKod))
                    {
                        messageModel.Messages.Add("Programınız için enstitü bilgisi bulunamadı.");
                        messageModel.MessageType = MsgTypeEnum.Warning;
                        return messageModel;
                    }

                    // Cinsiyet
                    int? cinsiyetID = null;
                    if (!string.IsNullOrWhiteSpace(ogrenci.CINSIYET))
                    {
                        var cinsiyetLower = ogrenci.CINSIYET.Trim().ToLower();
                        if (cinsiyetLower.Contains("erkek"))
                            cinsiyetID = 1;
                        else if (cinsiyetLower.Contains("kadın") || cinsiyetLower.Contains("kadin"))
                            cinsiyetID = 2;
                    }

                    // Öğrenim Durumu
                    int? ogrenimDurumID = null;
                    if (!string.IsNullOrWhiteSpace(ogrenci.OGRENIMDURUM_ID))
                    {
                        var durumKod = ogrenci.OGRENIMDURUM_ID.Trim().ToUpper();
                        ogrenimDurumID = durumKod == "AKT"
                            ? (int)OgrenimDurumEnum.HalenOğrenci
                            : (int)OgrenimDurumEnum.Mezun;
                    }

                    // Kayıt Tarihi
                    DateTime? kayitTarihi = null;
                    if (!string.IsNullOrWhiteSpace(ogrenci.KAYIT_TARIHI))
                    {
                        DateTime kayitTarih;
                        if (DateTime.TryParse(ogrenci.KAYIT_TARIHI, out kayitTarih))
                        {
                            kayitTarihi = kayitTarih;
                        }
                    }

                    var enstitu = EnstituBus.Enstitulers.FirstOrDefault(f => f.EnstituKod == dbProgram.AnabilimDallari.EnstituKod);
                    // Başarılı - tüm bilgileri Data'ya ekle
                    messageModel.IsSuccess = true;
                    messageModel.MessageType = MsgTypeEnum.Success;
                    messageModel.Table = new
                    {
                        Ad = ogrenci.AD?.Trim(),
                        Soyad = ogrenci.SOYAD?.Trim(),
                        TcKimlikNo = ogrenci.TCKIMLIKNO?.Trim(),
                        CinsiyetID = cinsiyetID,
                        EMail = !string.IsNullOrWhiteSpace(ogrenci.E_POSTA1)
                            ? ogrenci.E_POSTA1.Trim()
                            : ogrenci.E_POSTA2?.Trim(),
                        CepTel = !string.IsNullOrWhiteSpace(ogrenci.GSM1) ? ogrenci.GSM1.Trim() : ogrenci.GSM2?.Trim(),
                        EnstituKod = enstituKod,
                        OgrenimEnstituKod = enstituKod,
                        ProgramKod = dbProgram.ProgramKod,
                        ProgramAdi = dbProgram.ProgramAdi,
                        OgrenciNo = ogrenci.OGR_NO?.Trim(),
                        OgrenimTipKod = ogrenci.OGRENIMSEVIYE_ID.ToInt(),
                        OgrenimDurumID = ogrenimDurumID,
                        KayitTarihi = kayitTarihi,
                        EnstituAdi = enstitu?.EnstituAd ?? "",
                        OgrenimTipAdi = ogrenci.OGRENIMSEVIYE_AD?.Trim() ?? "",
                        OgrenimDurumAdi = ogrenci.OGRENIMDURUM_AD?.Trim() ?? ""
                    };

                    return messageModel;
                }
            }
            catch (Exception ex)
            {
                messageModel.Messages.Add("OBS bilgileri alınırken hata oluştu: " + ex.Message);
                messageModel.MessageType = MsgTypeEnum.Error;
                return messageModel;
            }
        }
        public static StudentControl OgrenciBilgisiGuncelleObs(Guid userKey)
        {
            return OgrenciBilgisiGuncelleObs(null, userKey);
        }

        public static StudentControl OgrenciBilgisiGuncelleObs(int kullaniciId)
        {
            return OgrenciBilgisiGuncelleObs(new List<int> { kullaniciId }, null);
        }
        public static StudentControl OgrenciBilgisiGuncelleObs(List<int> kullaniciIds)
        {
            return OgrenciBilgisiGuncelleObs(kullaniciIds, null);
        }

        private static StudentControl OgrenciBilgisiGuncelleObs(List<int> kullaniciId, Guid? userKey)
        {
            var kayitBilgi = new StudentControl();
            using (var entities = new LubsDbEntities())
            {
                var ogrenciQuery = entities.Kullanicilars.Where(p => p.YtuOgrencisi).AsQueryable();
                if (kullaniciId != null && kullaniciId.Any())
                    ogrenciQuery = ogrenciQuery.Where(p => kullaniciId.Contains(p.KullaniciID));
                if (userKey != null) ogrenciQuery = ogrenciQuery.Where(p => p.UserKey == userKey);

                var ogrenciler = ogrenciQuery.ToList();
                foreach (var ogrenci in ogrenciler)
                {

                    if (ogrenci.YtuOgrencisi)
                    {
                        kayitBilgi = OgrenciKontrol(ogrenci.OgrenciNo);
                         
                        if (kayitBilgi.KayitVar && kayitBilgi.OgrenciInfo.OGRENIMSEVIYE_ID.ToIntObj() == ogrenci.OgrenimTipKod)
                        {
                            if (ogrenci.ObsKayitDonemOtoGuncellemeKapali != true)
                            {
                                ogrenci.KayitDonemID = kayitBilgi.DonemID;
                                ogrenci.KayitYilBaslangic = kayitBilgi.BaslangicYil;

                            }
                            ogrenci.KayitTarihi = kayitBilgi.KayitTarihi;
                            if (kayitBilgi.OgrenciInfo != null)
                            {
                                int? danismanId = null;
                                if (!kayitBilgi.OgrenciInfo.DANISMAN_TC1.IsNullOrWhiteSpace() || !kayitBilgi.OgrenciInfo.OGR_DANISMAN_TC1.IsNullOrWhiteSpace())
                                {
                                    var danismanTc = kayitBilgi.OgrenciInfo.DANISMAN_TC1.IsNullOrWhiteSpace()
                                        ? kayitBilgi.OgrenciInfo.OGR_DANISMAN_TC1
                                        : kayitBilgi.OgrenciInfo.DANISMAN_TC1;
                                    var danisman = entities.Kullanicilars.FirstOrDefault(p =>
                                        p.TcKimlikNo == danismanTc);
                                    if (danisman != null)
                                        danismanId = danisman.KullaniciID;
                                    kayitBilgi.IsDanismanHesabiBulunamadi = !ogrenci.DanismanID.HasValue;

                                }

                                ogrenci.DanismanID = danismanId;
                                kayitBilgi.AktifDanismanID = danismanId;
                                if (!ogrenci.KullaniciOgrenimleris.Any(a => a.KullaniciID == ogrenci.KullaniciID && a.OgrenciNo == ogrenci.OgrenciNo))
                                {
                                    var kullaniciOgrenim = new KullaniciOgrenimleri
                                    {
                                        OgrenimDurumID = ogrenci.OgrenimDurumID,
                                        OgrenimTipKod = ogrenci.OgrenimTipKod,
                                        ProgramKod = ogrenci.ProgramKod,
                                        ProgramAdi = ogrenci.Programlar.ProgramAdi,
                                        OgrenciNo = ogrenci.OgrenciNo,
                                        ObsProgramAdi = kayitBilgi.OgrenciInfo.PROGRAM_AD,
                                        ObsProgramId = kayitBilgi.OgrenciInfo.PROGRAM_ID,
                                        DanismanID = danismanId,
                                        KayitDonemID = kayitBilgi.DonemID,
                                        KayitTarihi = kayitBilgi.KayitTarihi,
                                        KayitYilBaslangic = kayitBilgi.BaslangicYil,
                                        IslemTarihi = DateTime.Now,
                                        IslemYapanID = UserIdentity.Current.Id,
                                        IslemYapanIP = UserIdentity.Ip,
                                    };
                                    ogrenci.KullaniciOgrenimleris.Add(kullaniciOgrenim);

                                }
                            }

                        }
                        else if (!kayitBilgi.Hata)
                        {
                            ogrenci.YtuOgrencisi = false;
                        }
                        if (kayitBilgi.KayitVar)
                        {
                            var ad = kayitBilgi.OgrenciInfo?.AD;
                            var soyad = kayitBilgi.OgrenciInfo?.SOYAD; 
                            if (!ad.IsNullOrWhiteSpace() && ogrenci.Ad != ad)
                                ogrenci.Ad = ad;
                            if (!soyad.IsNullOrWhiteSpace() && ogrenci.Soyad != soyad)
                                ogrenci.Soyad = soyad;
                        }
                        entities.SaveChanges();
                    }
                }


                return kayitBilgi;
            }
        }

        public static JsonResult GetFilterOgrenciJsonResult(string term, string enstituKod)
        {
            using (var entities = new LubsDbEntities())
            {
                var ogrenciList = entities.Kullanicilars.Where(p =>
                        p.YtuOgrencisi && p.Programlar.AnabilimDallari.EnstituKod == enstituKod &&
                        ((p.Ad + " " + p.Soyad).Contains(term) || p.OgrenciNo.StartsWith(term) ||
                         p.TcKimlikNo.StartsWith(term))).Select(s => new
                         {
                             s.KullaniciID,
                             s.Ad,
                             s.Soyad,
                             s.OgrenciNo,
                             s.ResimAdi,
                             s.Programlar.ProgramAdi
                         }).Take(15).ToList()
                    .Select(s => new
                    {
                        id = s.KullaniciID,
                        s.ProgramAdi,
                        text = s.OgrenciNo + " " + s.Ad + " " + s.Soyad,
                        Images = s.ResimAdi.ToKullaniciResim()
                    }).ToList();

                return ogrenciList.ToJsonResult();
            }
        }
        public static JsonResult GetFilterAkademisyenJsonResult(string term, string enstituKod)
        {
            using (var entities = new LubsDbEntities())
            {
                var dataList = entities.Kullanicilars
                    .Where(p => p.IsAktif &&
                                p.KullaniciTipID == KullaniciTipiEnum.AkademikPersonel && p.Unvanlar.YetkiGrupID.HasValue &&
                                ((p.Ad + " " + p.Soyad).Contains(term) || p.TcKimlikNo.StartsWith(term))
                                )
                    .Select(s => new
                    {
                        s.KullaniciID,
                        s.Ad,
                        s.Soyad,
                        s.ResimAdi,
                        s.Unvanlar.UnvanSiraNo,
                        s.Unvanlar.UnvanAdi
                    }).OrderBy(o => o.UnvanSiraNo).ThenBy(t => t.Ad).ThenBy(t => t.Soyad).Take(15).ToList()
                    .Select(s => new
                    {
                        id = s.KullaniciID,
                        text = s.UnvanAdi + " " + s.Ad + " " + s.Soyad,
                        ResimAdi = s.ResimAdi.ToKullaniciResim()
                    }).ToList();

                return dataList.ToJsonResult();
            }
        }
        public static JsonResult GetFilterPersonelJsonResult(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return new List<object>().ToJsonResult();
            }

            using (var entities = new LubsDbEntities())
            {
                var kullaniciTipIds = new List<int> { KullaniciTipiEnum.AkademikPersonel, KullaniciTipiEnum.IdariPersonel };
                var dataList = entities.Kullanicilars
                    .Where(p => p.IsAktif &&
                                kullaniciTipIds.Contains(p.KullaniciTipID) &&
                                                ((p.Ad + " " + p.Soyad).Contains(term) || p.TcKimlikNo.StartsWith(term))
                    )
                    .Select(s => new
                    {
                        s.KullaniciID,
                        s.Ad,
                        s.Soyad,
                        s.ResimAdi,
                        s.Unvanlar.UnvanSiraNo,
                        s.Unvanlar.UnvanAdi
                    }).OrderBy(o => o.UnvanSiraNo).ThenBy(t => t.Ad).ThenBy(t => t.Soyad).Take(15).ToList()
                    .Select(s => new
                    {
                        id = s.KullaniciID,
                        text = s.UnvanAdi + " " + s.Ad + " " + s.Soyad,
                        ResimAdi = s.ResimAdi.ToKullaniciResim()
                    }).ToList();

                return dataList.ToJsonResult();
            }
        }
        public static List<CmbIntDto> GetCmbKullaniciTipleri(bool bosSecimVar, bool isHesapOlusturFiltre)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.KullaniciTipleris
                    .Where(p => p.YeniHesapOlusturabilir == (isHesapOlusturFiltre || p.YeniHesapOlusturabilir))
                    .OrderBy(o => o.KullaniciTipAdi);
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.KullaniciTipID, Caption = item.KullaniciTipAdi });
                }
            }

            return dct;

        }

        public static List<CmbIntDto> GetCmbKullaniciTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.KullaniciTipleris.Where(p => p.KurumIci == false).OrderBy(o => o.KullaniciTipAdi);
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.KullaniciTipID, Caption = item.KullaniciTipAdi });
                }
            }

            return dct;

        }

        public static List<CmbIntDto> GetCmbKullaniciTipleriOgrenciler(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.KullaniciTipleris.Where(p => p.BasvuruYapabilir).OrderBy(o => o.KullaniciTipAdi);
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.KullaniciTipID, Caption = item.KullaniciTipAdi });
                }
            }

            return dct;

        }

        public static List<CmbIntDto> CmbAktifOgrenimDurumu(bool bosSecimVar = false, bool? isAktif = true,
            int? haricOgreniDurumId = null, bool? isBasvurudaGozuksun = null, bool? isHesapKayittaGozuksun = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var qData = entities.OgrenimDurumlaris.AsQueryable();
                if (isAktif.HasValue) qData = qData.Where(p => p.IsAktif == isAktif.Value);
                if (haricOgreniDurumId.HasValue) qData = qData.Where(p => p.OgrenimDurumID == haricOgreniDurumId.Value);
                if (isBasvurudaGozuksun.HasValue)
                    qData = qData.Where(p => p.IsBasvurudaGozuksun == isBasvurudaGozuksun.Value);
                if (isHesapKayittaGozuksun.HasValue)
                    qData = qData.Where(p => p.IsHesapKayittaGozuksun == isHesapKayittaGozuksun.Value);
                var data = qData.OrderBy(o => o.OgrenimDurumAdi).ToList();
                dct.AddRange(data.Select(item => new CmbIntDto
                { Value = item.OgrenimDurumID, Caption = item.OgrenimDurumAdi }));
            }

            return dct;

        }

        public static List<CmbIntDto> CmbAktifOgrenimDurumu2(bool bosSecimVar = false, bool? isAktif = true,
            int? haricOgreniDurumId = null, bool? isBasvurudaGozuksun = null, bool? isHesapKayittaGozuksun = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var qData = entities.OgrenimDurumlaris.AsQueryable();
                if (isAktif.HasValue) qData = qData.Where(p => p.IsAktif == isAktif.Value);
                if (haricOgreniDurumId.HasValue) qData = qData.Where(p => p.OgrenimDurumID == haricOgreniDurumId.Value);
                if (isBasvurudaGozuksun.HasValue)
                    qData = qData.Where(p => p.IsBasvurudaGozuksun == isBasvurudaGozuksun.Value);
                if (isHesapKayittaGozuksun.HasValue)
                    qData = qData.Where(p => p.IsHesapKayittaGozuksun == isHesapKayittaGozuksun.Value);
                var data = qData.OrderBy(o => o.OgrenimDurumAdi).ToList();
                dct.AddRange(data.Select(item => new CmbIntDto
                { Value = item.OgrenimDurumID, Caption = item.OgrenimDurumAdi }));
            }

            return dct;

        }

        public static List<CmbIntDto> CmbCinsiyetler(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.Cinsiyetlers.Where(p => p.IsAktif).OrderBy(o => o.CinsiyetAdi).ToList();
                dct.AddRange(data.Select(item => new CmbIntDto
                { Value = item.CinsiyetID, Caption = item.CinsiyetAdi }));
            }

            return dct;

        }

        public static List<CheckObject<Kullanicilar>> GetProgramYetkisiOlanKullanicilar(List<Kullanicilar> kullanicilar,
            string programKod, string enstituKod = null)
        {
            var qData = kullanicilar.Where(p => p.EnstituKod == (enstituKod ?? p.EnstituKod))
                .Select(s => new CheckObject<Kullanicilar>
                {
                    Checked = s.KullaniciProgramlaris.Any(a => a.ProgramKod == programKod),
                    Value = s
                }).OrderByDescending(o => o.Checked).ThenBy(t => t.Value.Ad).ThenBy(t => t.Value.Soyad).ToList();
            return qData;

        }

        public static List<KulaniciProgramYetkiModel> GetKullaniciProgramlari(int kullaniciId, string enstituKod)
        {
            using (var entities = new LubsDbEntities())
            {
                var kull = (from s in entities.Programlars
                            join b in entities.AnabilimDallaris on s.AnabilimDaliID equals b.AnabilimDaliID
                            join e in entities.Enstitulers on b.EnstituKod equals e.EnstituKod
                            select new KulaniciProgramYetkiModel
                            {
                                EnstituKod = e.EnstituKod,
                                EnstituAdi = e.EnstituAd,
                                EnstituKisaAd = e.EnstituKisaAd,
                                AnabilimDaliAdi = b.AnabilimDaliAdi,
                                AnabilimDaliKod = b.AnabilimDaliKod,
                                ProgramKod = s.ProgramKod,
                                ProgramAdi = s.ProgramAdi,
                                YetkiVar = entities.KullaniciProgramlaris.Any(a =>
                                    a.KullaniciID == kullaniciId && a.ProgramKod == s.ProgramKod)
                            });

                if (enstituKod.IsNullOrWhiteSpace() == false) kull = kull.Where(p => p.EnstituKod == enstituKod);
                var data = kull.OrderByDescending(o => o.YetkiVar).ThenBy(t => t.AnabilimDaliAdi)
                    .ThenBy(t => t.ProgramAdi).ToList();
                return data;
            }
        }

        public static string ResimKaydet(HttpPostedFileBase resim)
        {
            try
            {
                var fileStream = resim.InputStream;
                var bmp = new Bitmap(fileStream);

                const string folderName = SistemAyar.KullaniciResimYolu;
                var rotasYonDegisimLog = SistemAyar.RotasyonuDegisenResimleriLogla.GetAyar().ToBoolean(false);
                var boyutlandirma = SistemAyar.KullaniciResimKaydiBoyutlandirma.GetAyar().ToBoolean(false);
                var kaliteOpt = SistemAyar.KullaniciResimKaydiKaliteOpt.GetAyar().ToBoolean(true);

                var resimAdi = resim.FileName.ToFileNameAddGuid();
                var resimYolu = Path.Combine(HttpContext.Current.Server.MapPath("~/" + folderName), resimAdi);


                if (boyutlandirma)
                {
                    try
                    {
                        var uzn = SistemAyar.KullaniciResimKaydiHeightPx.GetAyar();
                        var gens = SistemAyar.KullaniciResimKaydiWidthPx.GetAyar();

                        var uzunluk = uzn.ToInt(560);
                        var genislik = gens.ToInt(560);
                        var img = bmp.ResizeImage(new Size(genislik, uzunluk));
                        img.Save(resimYolu, ImageFormat.Jpeg);
                    }
                    catch (Exception ex)
                    {
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(ex,
                            "Resmin boyutlandırma işlemi yapılıp kayıt edilirken bir hata oluştu.\r\n Hata:" +
                            ex.ToExceptionMessage(), BilgiTipiEnum.OnemsizHata);
                    }
                }
                else
                {
                    bmp.Save(resimYolu, ImageFormat.Jpeg);
                }

                if (kaliteOpt)
                {
                    #region Quality check

                    try
                    {
                        var bmpQ = new Bitmap(resimYolu);
                        var jpgEncoder = ImageHelper.GetImageCodecInfo(ImageFormat.Jpeg);
                        var quality = 100L;
                        if (resim.ContentLength >= 80000 && resim.ContentLength < 200000) quality = 80;
                        else if (resim.ContentLength >= 200000 && resim.ContentLength < 400000) quality = 70;
                        else if (resim.ContentLength >= 400000 && resim.ContentLength < 600000) quality = 60;
                        else if (resim.ContentLength >= 600000 && resim.ContentLength < 800000) quality = 50;
                        else if (resim.ContentLength >= 800000 && resim.ContentLength < 1000000) quality = 40;
                        else if (resim.ContentLength >= 1000000) quality = 30;
                        var myEncoder = Encoder.Quality;
                        var path2 = resimYolu + Guid.NewGuid().ToString().Substring(0, 4) + ".jpg";
                        var myEncoderParameters = new EncoderParameters(1);
                        var myEncoderParameter = new EncoderParameter(myEncoder, quality);
                        myEncoderParameters.Param[0] = myEncoderParameter;
                        bmpQ.Save(path2, jpgEncoder, myEncoderParameters);
                        bmpQ.Dispose();
                        if (File.Exists(resimYolu))
                            File.Delete(resimYolu);
                        var imgTmp = Image.FromFile(path2);
                        imgTmp.Save(resimYolu, ImageFormat.Jpeg);
                        imgTmp.Dispose();
                        File.Delete(path2);
                    }
                    catch (Exception errQuality)
                    {
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(errQuality,
                            "Resmin kalitesi değiştirilirken hata oluştu.\r\n Hata:" + errQuality.ToExceptionMessage(),
                            BilgiTipiEnum.OnemsizHata);
                    }

                    #endregion
                }

                #region Rotation

                try
                {

                    var img1 = Image.FromFile(resimYolu);
                    var prop = img1.PropertyItems.FirstOrDefault(p => p.Id == 0x0112);

                    if (prop != null)
                    {
                        int orientationValue = img1.GetPropertyItem(prop.Id).Value[0];
                        RotateFlipType rotateFlipType = GetOrientationToFlipType(orientationValue);
                        img1.RotateFlip(rotateFlipType);
                        var path2 = resimYolu + Guid.NewGuid().ToString().Substring(0, 4) + ".jpg";
                        img1.Save(path2);
                        img1.Dispose();
                        if (File.Exists(resimYolu))
                            File.Delete(resimYolu);
                        var imgTmp = Image.FromFile(path2);
                        imgTmp.Save(resimYolu, ImageFormat.Jpeg);
                        imgTmp.Dispose();
                        File.Delete(path2);
                        if (rotasYonDegisimLog)
                        {

                            SistemBilgilendirmeBus.SistemBilgisiKaydet(
                                "Rotasyon farklılığı görünen resim düzeltildi! Resim:" + resimYolu,
                                ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex,
                        "Hesap kayıt sırasında resim rotasyonu yapılırken bir hata oluştu.\r\n Hata:" +
                        ex.ToExceptionMessage(), BilgiTipiEnum.OnemsizHata);
                }

                #endregion


                return resimAdi;
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet(
                    "Resim kaydedilirken bir hata oluştu! Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(),
                    BilgiTipiEnum.Hata, null, UserIdentity.Ip);
                return null;
            }
        }

        private static RotateFlipType GetOrientationToFlipType(int orientationValue)
        {
            RotateFlipType rotateFlipType;
            switch (orientationValue)
            {
                case 1:
                    rotateFlipType = RotateFlipType.RotateNoneFlipNone;
                    break;
                case 2:
                    rotateFlipType = RotateFlipType.RotateNoneFlipX;
                    break;
                case 3:
                    rotateFlipType = RotateFlipType.Rotate180FlipNone;
                    break;
                case 4:
                    rotateFlipType = RotateFlipType.Rotate180FlipX;
                    break;
                case 5:
                    rotateFlipType = RotateFlipType.Rotate90FlipX;
                    break;
                case 6:
                    rotateFlipType = RotateFlipType.Rotate90FlipNone;
                    break;
                case 7:
                    rotateFlipType = RotateFlipType.Rotate270FlipX;
                    break;
                case 8:
                    rotateFlipType = RotateFlipType.Rotate270FlipNone;
                    break;
                default:
                    rotateFlipType = RotateFlipType.RotateNoneFlipNone;
                    break;
            }

            return rotateFlipType;
        }

        public static MmMessage SendMailYeniHesap(Kullanicilar kModel, string sfr)
        {
            return MailSenderKullanici.SendMailYeniHesap(kModel, sfr);
        }



    }
}
