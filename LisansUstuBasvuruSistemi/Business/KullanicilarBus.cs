using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.WebServiceData.ObsService;

namespace LisansUstuBasvuruSistemi.Business
{
    public class KullanicilarBus
    {
        public static List<int> GetDanismanUnvanIds()
        {
            //Doç.Dr Prof.Dr, Dr. Öğr. Üye
            return new List<int> { 17, 42, 73 };
        }
        public static StudentControl OgrenciKontrol(string tcKimlikNo = null, string donemId = null)
        {
            var obsData = new ObsServiceData();
            if (donemId == null)
            {
                var donem = DateTime.Now.Date.ToAraRaporDonemBilgi();
                donemId = donem.BaslangicTarihi.Year + "" + donem.DonemID;
            }

            return obsData.GetObsStudentControl(tcKimlikNo, donemId);
        }
        public static StudentControl OgrenciBilgisiGuncelleObs(int kullaniciId)
        {
            var kayitBilgi = new StudentControl();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var kul = db.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
                if (kul.YtuOgrencisi)
                {
                    var tcKimlikNo = kul.TcKimlikNo;
                    kayitBilgi = OgrenciKontrol(tcKimlikNo);
                    if (kayitBilgi.KayitVar && kayitBilgi.OgrenciInfo.OGRENIMSEVIYE_ID.ToIntObj() == kul.OgrenimTipKod)
                    {
                        kul.KayitDonemID = kayitBilgi.DonemID;
                        kul.KayitYilBaslangic = kayitBilgi.BaslangicYil;
                        kul.KayitTarihi = kayitBilgi.KayitTarihi;
                        if (kayitBilgi.OgrenciInfo != null)
                        {
                            int? danismanId = null;
                            if (!kayitBilgi.OgrenciInfo.DANISMAN_TC1.IsNullOrWhiteSpace())
                            {
                                var danisman = db.Kullanicilars.FirstOrDefault(p => p.TcKimlikNo == kayitBilgi.OgrenciInfo.DANISMAN_TC1);
                                if (danisman != null)
                                    danismanId = danisman.KullaniciID;
                                kayitBilgi.IsDanismanHesabiBulunamadi = !kul.DanismanID.HasValue;

                            }

                            kul.DanismanID = danismanId;
                            kayitBilgi.AktifDanismanID = danismanId;
                        }

                    }
                    else if (!kayitBilgi.Hata)
                    {
                        kul.YtuOgrencisi = false;
                        kul.OgrenimTipKod = null;
                        kul.ProgramKod = null;
                        kul.OgrenciNo = null;
                        kul.KayitDonemID = null;
                        kul.KayitYilBaslangic = null;
                        kul.KayitTarihi = null;
                    }
                    db.SaveChanges();
                }
                return kayitBilgi;
            }
        }

        public static JsonResult GetFilterOgrenciJsonResult(string term, string enstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var ogrenciList = db.Kullanicilars.Where(p => p.YtuOgrencisi && p.Programlar.AnabilimDallari.EnstituKod == enstituKod && ((p.Ad + " " + p.Soyad).Contains(term) || p.OgrenciNo.StartsWith(term) || p.TcKimlikNo.StartsWith(term))).Select(s => new
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
        public static List<CmbIntDto> GetCmbKullaniciTipleri(bool bosSecimVar, bool isHesapOlusturFiltre)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.KullaniciTipleris.Where(p => p.YeniHesapOlusturabilir == (isHesapOlusturFiltre || p.YeniHesapOlusturabilir)).OrderBy(o => o.KullaniciTipAdi);
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
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.KullaniciTipleris.Where(p => p.KurumIci == false).OrderBy(o => o.KullaniciTipAdi);
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
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.KullaniciTipleris.Where(p => p.BasvuruYapabilir).OrderBy(o => o.KullaniciTipAdi);
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.KullaniciTipID, Caption = item.KullaniciTipAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> CmbAktifOgrenimDurumu(bool bosSecimVar = false, bool? isAktif = true, int? haricOgreniDurumId = null, bool? isBasvurudaGozuksun = null, bool? isHesapKayittaGozuksun = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qData = db.OgrenimDurumlaris.AsQueryable();
                if (isAktif.HasValue) qData = qData.Where(p => p.IsAktif == isAktif.Value);
                if (haricOgreniDurumId.HasValue) qData = qData.Where(p => p.OgrenimDurumID == haricOgreniDurumId.Value);
                if (isBasvurudaGozuksun.HasValue) qData = qData.Where(p => p.IsBasvurudaGozuksun == isBasvurudaGozuksun.Value);
                if (isHesapKayittaGozuksun.HasValue) qData = qData.Where(p => p.IsHesapKayittaGozuksun == isHesapKayittaGozuksun.Value);
                var data = qData.OrderBy(o => o.OgrenimDurumAdi).ToList();
                dct.AddRange(data.Select(item => new CmbIntDto { Value = item.OgrenimDurumID, Caption = item.OgrenimDurumAdi }));
            }
            return dct;

        }
        public static List<CmbIntDto> CmbAktifOgrenimDurumu2(bool bosSecimVar = false, bool? isAktif = true, int? haricOgreniDurumId = null, bool? isBasvurudaGozuksun = null, bool? isHesapKayittaGozuksun = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qData = db.OgrenimDurumlaris.AsQueryable();
                if (isAktif.HasValue) qData = qData.Where(p => p.IsAktif == isAktif.Value);
                if (haricOgreniDurumId.HasValue) qData = qData.Where(p => p.OgrenimDurumID == haricOgreniDurumId.Value);
                if (isBasvurudaGozuksun.HasValue) qData = qData.Where(p => p.IsBasvurudaGozuksun == isBasvurudaGozuksun.Value);
                if (isHesapKayittaGozuksun.HasValue) qData = qData.Where(p => p.IsHesapKayittaGozuksun == isHesapKayittaGozuksun.Value);
                var data = qData.OrderBy(o => o.OgrenimDurumAdi).ToList();
                dct.AddRange(data.Select(item => new CmbIntDto { Value = item.OgrenimDurumID, Caption = item.OgrenimDurumAdi }));
            }
            return dct;

        }
        public static List<CmbIntDto> CmbCinsiyetler(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Cinsiyetlers.Where(p => p.IsAktif).OrderBy(o => o.CinsiyetAdi).ToList();
                dct.AddRange(data.Select(item => new CmbIntDto { Value = item.CinsiyetID, Caption = item.CinsiyetAdi }));
            }
            return dct;

        }
        public static List<CheckObject<Kullanicilar>> GetProgramYetkisiOlanKullanicilar(List<Kullanicilar> kullanicilar, string programKod, string enstituKod = null)
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
            using (var entities = new LisansustuBasvuruSistemiEntities())
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
                                YetkiVar = entities.KullaniciProgramlaris.Any(a => a.KullaniciID == kullaniciId && a.ProgramKod == s.ProgramKod)
                            });

                if (enstituKod.IsNullOrWhiteSpace() == false) kull = kull.Where(p => p.EnstituKod == enstituKod);
                var data = kull.OrderByDescending(o => o.YetkiVar).ThenBy(t => t.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ToList();
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
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(ex, "Resmin boyutlandırma işlemi yapılıp kayıt edilirken bir hata oluştu.\r\n Hata:" + ex.ToExceptionMessage(), LogTipiEnum.OnemsizHata);
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
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(errQuality, "Resmin kalitesi değiştirilirken hata oluştu.\r\n Hata:" + errQuality.ToExceptionMessage(), LogTipiEnum.OnemsizHata);
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

                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Rotasyon farklılığı görünen resim düzeltildi! Resim:" + resimYolu, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Bilgi);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex, "Hesap kayıt sırasında resim rotasyonu yapılırken bir hata oluştu.\r\n Hata:" + ex.ToExceptionMessage(), LogTipiEnum.OnemsizHata);
                }
                #endregion


                return resimAdi;
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Resim kaydedilirken bir hata oluştu! Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata, null, UserIdentity.Ip);
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