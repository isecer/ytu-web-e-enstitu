using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class TezIzlemeJuriOneriBus
    {

        public static DateTime? IsYeterlikUygunlukKontrol(int kullaniciId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var ogrenci = db.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
                var ogrenciObsBilgi = KullanicilarBus.OgrenciKontrol(ogrenci.OgrenciNo);
                if (!ogrenciObsBilgi.Hata)
                {
                    var ogrenciYeterlikBilgi = ogrenciObsBilgi.OgrenciYeters.FirstOrDefault(p => p.DR_YET_GNL_SNV_DURUM == "Başarılı");
                    if (ogrenciYeterlikBilgi != null) return ogrenciYeterlikBilgi.DR_YET_SOZ_SNV_TARIH.ToDate();
                    return null;
                }
                throw new Exception("Yeterlik uygunluk kontrol işlemi sırasında servis hata döndürdü. Hata:" +
                                    ogrenciObsBilgi.HataMsj);
            }
        }

        public static bool IsAktifDanismanOneriVar(int kullaniciId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var danismanOneri = db.TDOBasvuruDanismen.Where(p => p.TDOBasvuru.KullaniciID == kullaniciId)
                    .OrderByDescending(o => o.TDOBasvuruID).FirstOrDefault();
                var isAktif = true;
                if (danismanOneri == null) isAktif = false;
                else if (danismanOneri.EYKDaOnaylandi.HasValue) isAktif = false;
                else if (danismanOneri.EYKYaGonderildi == false) isAktif = false;
                else if (danismanOneri.VarolanTezDanismanID.HasValue && danismanOneri.VarolanDanismanOnayladi == false) isAktif = false;
                else if (danismanOneri.DanismanOnayladi == false) isAktif = false;

                return isAktif;
            }
        }

        public static FmTikBasvuru BasvuruBilgi(FmTikBasvuru model)
        {
            using (var entities = new LisansustuBasvuruSistemiEntities())
            {
                var kul = entities.Kullanicilars.First(f => f.KullaniciID == model.KullaniciID);
                model.BasvuruKontrolBilgi.EnstituAdi = entities.Enstitulers.First(p => p.EnstituKod == model.EnstituKod).EnstituAd;
                model.BasvuruKontrolBilgi.IsBasvuruAcik = TiAyar.TikOneriAlimiAcik.GetAyarTi(model.EnstituKod, "false").ToBoolean(false);
                model.BasvuruKontrolBilgi.AdSoyad = kul.Ad + " " + kul.Soyad;

                if (!model.BasvuruKontrolBilgi.IsBasvuruAcik)
                {
                    model.BasvuruKontrolBilgi.Aciklama =
                        "Sistem tik öneri işlemlerine kapalıdır. Detaylı bilgi için duyuruları takip edebilirsiniz.";
                }
                else if (kul.YtuOgrencisi)
                {

                    model.BasvuruKontrolBilgi.IsOgrenci = true;
                    var obsOgrenci = KullanicilarBus.OgrenciBilgisiGuncelleObs(model.KullaniciID.Value);

                    if (obsOgrenci.KayitVar == false)
                    {
                        model.BasvuruKontrolBilgi.Aciklama = "OBS sisteminde aktif öğrenim bilginize rastlanmadı! Hesap bilgilerinizde bulunan YTÜ Lüsansüstü Öğrenci bilgilerinizin doğruluğunu kontrol ediniz lütfen.";
                    }
                    else
                    {
                        if (kul.OgrenimTipKod.IsDoktora() && kul.OgrenimDurumID == OgrenimDurum.HalenOğrenci)
                        {

                            var basvuru = entities.TijBasvurus.FirstOrDefault(f =>
                                  f.KullaniciID == model.KullaniciID && f.OgrenciNo == kul.OgrenciNo);

                            

                            model.BasvuruKontrolBilgi.AdSoyad += " (" + kul.OgrenciNo + ")";
                            model.BasvuruKontrolBilgi.IsBasvuruAcik = true;
                            var ot = entities.OgrenimTipleris.First(f =>
                                f.OgrenimTipKod == kul.OgrenimTipKod && f.EnstituKod == kul.EnstituKod);
                            model.BasvuruKontrolBilgi.OgrenimTipAdiProgramAdi = ot.OgrenimTipAdi + " / " + kul.Programlar.ProgramAdi;
                            model.BasvuruKontrolBilgi.AktifOgrenimIcinBasvuruVar = entities.TijBasvurus.Any(a => a.KullaniciID == kul.KullaniciID && a.OgrenciNo == kul.OgrenciNo);

                        }
                        else if (kul.Programlar.AnabilimDallari.EnstituKod != model.EnstituKod)
                        {
                            model.BasvuruKontrolBilgi.Aciklama = "Kayıtlı olduğunuz program ve başvuru yapmaya çalıştığınız enstitü birbiri ile uyuşmamaktadır. Doğru enstitü sayfasından başvuru yaptığınızdan emin olunuz.";
                        }
                        else
                        {
                            model.BasvuruKontrolBilgi.Aciklama = "Tik öneri başvurusu yapılabilmesi için Doktora öğrencisi olunması gerekmektedir.";

                        }
                    }
                }
                else
                {
                    model.BasvuruKontrolBilgi.IsBasvuruAcik = false;
                    model.BasvuruKontrolBilgi.Aciklama = "Hesap bilgilerinizde YTÜ Lisansütü öğrencisi olduğunuza dair bilgiler doldurulmadığı için Tik öneri başvurusu yapamazsınız. Sağ üst köşeden hesap bilgilerini düzenle butonuna tıklayıp YTÜ Lisansüstü Öğrencisi Misiniz? sorusunu cevaplayarak öğrenim bilgilerinizi doldurup profilinizi güncelleyerek tekrar başvuru yapmayı deneyiniz.";
                }
            }

            return model;
        }
    }
}