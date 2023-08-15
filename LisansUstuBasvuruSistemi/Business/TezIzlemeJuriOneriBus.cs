using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class TezIzlemeJuriOneriBus
    {
        public static bool IsAktifDevamEdenTijVarMi(int kullaniciId, string ogrenciNo)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                return db.TijBasvuruOneris.Any(a => a.TijBasvuru.KullaniciID == kullaniciId &&
                                                    a.TijBasvuru.OgrenciNo == ogrenciNo &&
                                                    !a.TijBasvuru.IsYeniBasvuruYapilabilir);

            }

        }
        public static List<CmbIntDto> CmbTijDegisiklikTipListe(bool bosSecimVar = false)
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var degisiklitTips = db.TijDegisiklikTipleris
                    .Select(s => new CmbIntDto
                    {
                        Value = s.TijDegisiklikTipID,
                        Caption = s.TijDegisiklikTipAdi

                    }).ToList();
                if (bosSecimVar) degisiklitTips.Insert(0, new CmbIntDto { Value = null, Caption = "" });
                return degisiklitTips;
            }
        }
        public static List<CmbIntDto> CmbTijFormTipListe(bool bosSecimVar = false, bool? isIlkOneri = null)
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qDegisiklitTips = db.TijFormTipleris
                    .Select(s => new CmbIntDto
                    {
                        Value = s.TijFormTipID,
                        Caption = s.TikFormTipAdi

                    }).AsQueryable();
                if (isIlkOneri.HasValue)
                {
                    qDegisiklitTips = qDegisiklitTips.Where(p =>
                        isIlkOneri == true ? p.Value == TijFormTipi.YeniForm : p.Value != TijFormTipi.YeniForm);
                }

                var degisiklitTips = qDegisiklitTips.ToList();
                if (bosSecimVar) degisiklitTips.Insert(0, new CmbIntDto { Value = null, Caption = "" });
                return degisiklitTips;
            }
        }
        public static List<string> IsAktifDevamEdenTijMessage(int kullaniciId, string ogrenciNo)
        {
            var messages = new List<string>();
            var result = IsAktifDevamEdenTijVarMi(kullaniciId, ogrenciNo);
            if (result)
            {
                messages.Add("Aktif olarak devam eden bir tez izleme jüri önerisi süreciniz bulunduğu için danışman değişikliği işlemi yapamazsınız.");
                messages.Add("Konuyla ilgili Enstitünüz ile görüşebilirsiniz.");
            }
            return messages;
        }
        public static List<CmbStringDto> CmbTiDonemListe(string enstituKod, bool bosSecimVar = false)
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var donems = db.TijBasvuruOneris.Select(s => new { s.DonemBaslangicYil, s.DonemID, s.Donemler.DonemAdi })
                    .Distinct().OrderByDescending(o => o.DonemBaslangicYil).ThenByDescending(t => t.DonemID).Select(s => new CmbStringDto
                    {
                        Value = s.DonemBaslangicYil + "" + s.DonemID,
                        Caption = s.DonemBaslangicYil + "/" + (s.DonemBaslangicYil + 1) + " " + s.DonemAdi

                    }).ToList();
                if (bosSecimVar) donems.Insert(0, new CmbStringDto { Value = null, Caption = "" });
                return donems;
            }
        }
        public static List<CmbIntDto> GetCmbFilterTiAnabilimDallari(string enstituKod, bool bosSecimVar = false)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var yeterliAnabilimDaliIds = db.TijBasvurus
                    .Where(p => p.EnstituKod == enstituKod).Select(s => s.Programlar.AnabilimDaliID).Distinct().ToList();

                var anabilimDallaris = db.AnabilimDallaris.Where(p => yeterliAnabilimDaliIds.Contains(p.AnabilimDaliID))
                    .Select(s => new { s.AnabilimDaliID, s.AnabilimDaliAdi }).OrderBy(o => o.AnabilimDaliAdi).Select(
                        s =>
                            new CmbIntDto { Value = s.AnabilimDaliID, Caption = s.AnabilimDaliAdi }
                    ).ToList();
                if (bosSecimVar) anabilimDallaris.Insert(0, new CmbIntDto { Value = null, Caption = "" });

                return anabilimDallaris;
            }
        }
        public static List<CmbIntDto> CmbTdoOneriDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();

            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            dct.Add(new CmbIntDto { Value = 1, Caption = "Danışman Onayı Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 2, Caption = "Danışman Tarafından Onaylandı" });
            dct.Add(new CmbIntDto { Value = 3, Caption = "Danışman Tarafından Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = 4, Caption = "EYK'ya Gönderimi Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 5, Caption = "EYK'ya Gönderimi Onaylandı" });
            dct.Add(new CmbIntDto { Value = 6, Caption = "EYK'ya Gönderimi Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = 7, Caption = "EYK'da Onay Bekliyor" });
            dct.Add(new CmbIntDto { Value = 8, Caption = "EYK'Da Onaylandı" });
            dct.Add(new CmbIntDto { Value = 9, Caption = "EYK'Da Onaylanmadı" });
            return dct;
        }
        public static List<CmbBoolDto> CmbTijOneriTipListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();

            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });

            dct.Add(new CmbBoolDto { Value = false, Caption = "Yeni Jüri Önerisi" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Jüri Önerisi Değişikliği" });
            return dct;
        }
        public static IHtmlString ToTijBasvuruDurumView(this TijBasvuruOneriDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("TiJuriOneri", "BasvuruDurumView");
            return pagerString;
        }

        public static FmTijBasvuru BasvuruBilgi(FmTijBasvuru model)
        {
            using (var entities = new LisansustuBasvuruSistemiEntities())
            {

                var obsOgrenci = KullanicilarBus.OgrenciBilgisiGuncelleObs(model.KullaniciID.Value);
                var kul = entities.Kullanicilars.First(f => f.KullaniciID == model.KullaniciID);
                model.BasvuruKontrolBilgi.EnstituAdi = entities.Enstitulers.First(p => p.EnstituKod == model.EnstituKod).EnstituAd;
                model.BasvuruKontrolBilgi.IsBasvuruAcik = TiAyar.TikOneriAlimiAcik.GetAyarTi(model.EnstituKod, "false").ToBoolean(false);
                model.BasvuruKontrolBilgi.AdSoyad = kul.Ad + " " + kul.Soyad;

                if (!model.BasvuruKontrolBilgi.IsBasvuruAcik)
                {
                    model.BasvuruKontrolBilgi.Aciklama =
                        "Jüri öneri işlemlerine kapalıdır. Detaylı bilgi için duyuruları takip edebilirsiniz.";
                }
                else if (kul.YtuOgrencisi)
                {

                    model.BasvuruKontrolBilgi.IsOgrenci = true;

                    if (obsOgrenci.KayitVar == false)
                    {
                        model.BasvuruKontrolBilgi.IsBasvuruAcik = false;
                        model.BasvuruKontrolBilgi.Aciklama = "OBS sisteminde aktif öğrenim bilginize rastlanmadı! Hesap bilgilerinizde bulunan YTÜ Lüsansüstü Öğrenci bilgilerinizin doğruluğunu kontrol ediniz lütfen.";
                    }
                    else
                    {
                        obsOgrenci = KullanicilarBus.OgrenciKontrol(kul.OgrenciNo);
                        if (kul.OgrenimTipKod.IsDoktora() && kul.OgrenimDurumID == OgrenimDurum.HalenOğrenci)
                        {
                            model.BasvuruKontrolBilgi.AdSoyad += " (" + kul.OgrenciNo + ")";
                            var ot = entities.OgrenimTipleris.First(f =>
                                f.OgrenimTipKod == kul.OgrenimTipKod && f.EnstituKod == kul.EnstituKod);
                            model.BasvuruKontrolBilgi.OgrenimTipAdiProgramAdi = ot.OgrenimTipAdi + " / " + kul.Programlar.ProgramAdi;
                            model.BasvuruKontrolBilgi.AktifOgrenimIcinBasvuruVar = entities.TijBasvurus.Any(a => a.KullaniciID == kul.KullaniciID && a.OgrenciNo == kul.OgrenciNo);



                            var ogrenciYeterlikBilgi =
                                obsOgrenci.OgrenciYeters.FirstOrDefault(p => p.DR_YET_GNL_SNV_DURUM == "Başarılı" && p.DR_YET_SOZ_SNV_DURUM == "Başarılı");
                            if (ogrenciYeterlikBilgi != null)
                            {

                                if (TezDanismanOneriBus.IsAktifDanismanOneriVar(kul.KullaniciID))
                                {
                                    model.BasvuruKontrolBilgi.IsBasvuruAcik = false;
                                    model.BasvuruKontrolBilgi.Aciklama = "Adınıza ait yapılan bir Tez Danışman Öneri başvurusunuz bulunmakta. Jüri önerisi yapılabilmesi bu sürecinin tamamlanması gerekmektedir.";
                                }
                                else if (TezDanismanOneriBus.IsAktifEsDanismanOneriVar(kul.KullaniciID))
                                {
                                    model.BasvuruKontrolBilgi.IsBasvuruAcik = false;
                                    model.BasvuruKontrolBilgi.Aciklama = "Adınıza ait yapılan bir Tez Eş Danışman başvurunuz bulunmakta. Jüri önerisi yapılabilmesi için Eş Danışman sürecinin tamamlanması gerekmektedir.";
                                }
                                else
                                {
                                    var basvuru = entities.TijBasvurus.Any(f =>
                                        f.KullaniciID == model.KullaniciID && f.OgrenciNo == kul.OgrenciNo);
                                    if (!basvuru) model.BasvuruKontrolBilgi.IsBasvuruAcik = true;
                                }


                            }
                            else
                            {
                                model.BasvuruKontrolBilgi.IsBasvuruAcik = false;
                                model.BasvuruKontrolBilgi.Aciklama = "Tez izleme jüri önerisi yapabilmeniz için yeterlik sınavından başarılı olmanız gerekmektedir.";
                            }





                        }
                        else if (kul.Programlar.AnabilimDallari.EnstituKod != model.EnstituKod)
                        {
                            model.BasvuruKontrolBilgi.IsBasvuruAcik = false;
                            model.BasvuruKontrolBilgi.Aciklama = "Kayıtlı olduğunuz program ve başvuru yapmaya çalıştığınız enstitü birbiri ile uyuşmamaktadır. Doğru enstitü sayfasından başvuru yaptığınızdan emin olunuz.";
                        }
                        else
                        {
                            model.BasvuruKontrolBilgi.IsBasvuruAcik = false;
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

        public static List<string> TezIzlemeJuriOneriSenkronizasyonMsg(int kullaniciId)
        {
            var msg = new List<string>();
            using (var entities = new LisansustuBasvuruSistemiEntities())
            {
                var kul = entities.Kullanicilars.First(f => f.KullaniciID == kullaniciId);
                if (kul.YtuOgrencisi)
                {
                    var obsOgrenci = KullanicilarBus.OgrenciKontrol(kul.OgrenciNo);
                    if (!obsOgrenci.Hata)
                    {
                        var ogrenciYeterlikBilgi =
                            obsOgrenci.OgrenciYeters.FirstOrDefault(p => p.DR_YET_GNL_SNV_DURUM == "Başarılı" && p.DR_YET_SOZ_SNV_DURUM == "Başarılı");
                        var juriler = obsOgrenci.TezIzlJuriBilgileri.ToList();

                        if (juriler.Count > 0)
                        {
                            if (juriler.Count < 3)
                            {
                                msg.Add("OBS sisteminde tanımlı tez izleme jürilerinizin eksik tanımlandığı tespit edilmiştir. Jüri önerisi yapabilmeniz için bu durumu enstitü yetkililerine iletiniz.");
                            }
                            else if (juriler.All(a => a.TEZ_DANISMAN != "1"))
                            {
                                msg.Add("OBS sisteminde tanımlı tez izleme jüri bilgilerinizde Tez danışmanı bilgisi tanımlı gözükmemektedir. Jüri önerisi yapabilmeniz için bu durumu enstitü yetkililerine iletiniz.");
                            }
                        }

                    }
                    else
                    {
                        msg.Add("OBS sisteminden öğrenci bilgisi kontrolü yapılamadı. Enstitü ile iletişime geçiniz! Hata: " + obsOgrenci.HataMsj);
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("OBS sisteminden öğrenci bilgisi kontrolü yapılamadı. Hata:" + obsOgrenci.HataMsj, "TezIzlemeJuriOneriBus/TezIzlemeJuriOneriSenkronizasyonMsg", LogType.Kritik);
                    }
                }

                if (!msg.Any())
                {
                    TezIzlemeJuriOneriSenkronizasyon(kullaniciId);
                }
                return msg;
            }
        }

        public static Guid? TezIzlemeJuriOneriSenkronizasyon(int kullaniciId)
        {
            Guid? basvuruUniqueId = null;

            using (var entities = new LisansustuBasvuruSistemiEntities())
            {
                var kul = entities.Kullanicilars.First(f => f.KullaniciID == kullaniciId);
                if (kul.YtuOgrencisi)
                {
                    var obsOgrenci = KullanicilarBus.OgrenciKontrol(kul.OgrenciNo);
                    kul = entities.Kullanicilars.First(f => f.KullaniciID == kullaniciId);
                    if (!obsOgrenci.Hata)
                    {
                        var ogrenciYeterlikBilgi =
                            obsOgrenci.OgrenciYeters.FirstOrDefault(p => p.DR_YET_GNL_SNV_DURUM == "Başarılı" && p.DR_YET_SOZ_SNV_DURUM == "Başarılı");
                        var juriler = obsOgrenci.TezIzlJuriBilgileri.ToList();
                        if (ogrenciYeterlikBilgi != null && juriler.Count == 3 && juriler.Any(a => a.TEZ_DANISMAN == "1"))
                        {
                            var tijBasvuru = kul.TijBasvurus.FirstOrDefault(f => f.KullaniciID == kul.KullaniciID && f.OgrenciNo == kul.OgrenciNo);
                            if (tijBasvuru != null) basvuruUniqueId = tijBasvuru.UniqueID;
                            if ((tijBasvuru == null || !tijBasvuru.TijBasvuruOneris.Any()))
                            {
                                var program = entities.Programlars.First(f => f.ProgramKod == kul.ProgramKod);
                                var enstituKod = program.AnabilimDallari.EnstituKod;
                                var oot = entities.OgrenimTipleris.First(f =>
                                    f.OgrenimTipKod == kul.OgrenimTipKod && f.EnstituKod == enstituKod);
                                basvuruUniqueId = Guid.NewGuid();
                                if (tijBasvuru == null)
                                    tijBasvuru = new TijBasvuru
                                    {
                                        UniqueID = basvuruUniqueId.Value,
                                        IsYeniBasvuruYapilabilir = true,
                                        EnstituKod = enstituKod,
                                        BasvuruTarihi = DateTime.Now,
                                        KullaniciID = kullaniciId,
                                        TezDanismanID = kul.DanismanID,
                                        OgrenciNo = kul.OgrenciNo,
                                        OgrenimTipID = oot.OgrenimTipID,
                                        ProgramKod = kul.ProgramKod,
                                        KayitOgretimYiliBaslangic = kul.KayitYilBaslangic,
                                        KayitOgretimYiliDonemID = kul.KayitDonemID,
                                        KayitTarihi = kul.KayitTarihi,
                                        IslemTarihi = DateTime.Now,
                                        IslemYapanIP = UserIdentity.Ip,
                                        IslemYapanID = kullaniciId
                                    };
                                var uniqueId = Guid.NewGuid();
                                var donemBiligi = DateTime.Now.ToAraRaporDonemBilgi();
                                var tijOneri = new TijBasvuruOneri
                                {
                                    UniqueID = uniqueId,
                                    FormKodu = uniqueId.ToString().Substring(0, 8).ToUpper(),
                                    TijFormTipID = TijFormTipi.YeniForm,
                                    IsObsData = true,
                                    TezDanismanID = kul.DanismanID,
                                    BasvuruTarihi = DateTime.Now,
                                    DonemBaslangicYil = donemBiligi.BaslangicYil,
                                    DonemID = donemBiligi.DonemID,
                                    SozluSinavBasariTarihi = ogrenciYeterlikBilgi.DR_YET_SOZ_SNV_TARIH.ToDate().Value,
                                    IsTezDiliTr = obsOgrenci.IsTezDiliTr,
                                    TezBaslikTr = obsOgrenci.OgrenciTez.TEZ_BASLIK,
                                    TezBaslikEn = obsOgrenci.OgrenciTez.TEZ_BASLIK_ENG
                                };
                                foreach (var juri in juriler)
                                {
                                    int? universiteId = Management.UniversiteYtuKod;
                                    var isTezDanismani = false;
                                    var isYtuIciJuri = false;
                                    if (juri.TEZ_DANISMAN == "1") isTezDanismani = true;
                                    if (juri.TEZ_IZLEME_JURI_UNIVER.Contains("Yıldız Teknik"))
                                    {
                                        isYtuIciJuri = true;
                                    }
                                    else
                                    {
                                        universiteId = null;
                                    }

                                    tijOneri.TijBasvuruOneriJurilers.Add(new TijBasvuruOneriJuriler
                                    {
                                        IsYtuIciJuri = isYtuIciJuri,
                                        IsTezDanismani = isTezDanismani,
                                        UnvanAdi = juri.TEZ_IZLEME_JURI_UNVAN.ToJuriUnvanAdi(),
                                        AdSoyad = juri.TEZ_IZLEME_JURI_ADSOY,
                                        EMail = juri.TEZ_IZLEME_JURI_EPOSTA,
                                        IsAsil = true,
                                        UniversiteID = universiteId,
                                        UniversiteAdi = juri.TEZ_IZLEME_JURI_UNIVER,
                                        AnabilimdaliAdi = juri.TEZ_IZLEME_JURI_ANABLMDAL
                                    });
                                }

                                tijOneri.TijBasvuruOneriJurilers = tijOneri.TijBasvuruOneriJurilers.OrderBy(o =>
                                        (o.IsTezDanismani
                                            ? 1
                                            : (o.IsYtuIciJuri ? 2 : 3)))
                                    .ToList();
                                tijBasvuru.TijBasvuruOneris.Add(tijOneri);
                                entities.TijBasvurus.Add(tijBasvuru);
                                entities.SaveChanges();
                            }
                        }

                    }
                }
                return basvuruUniqueId;
            }

        }


        public static TijBasvuruDetayDto GetSecilenBasvuruTijDetay(Guid uniqueId)
        {
            var model = new TijBasvuruDetayDto();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                int? danismanId = null;
                var tiJuriOnerileriOgrenciAdina = RoleNames.TiJuriOnerileriOgrenciAdina.InRoleCurrent();
                var tiJuriOnerileriKayit = RoleNames.TiJuriOnerileriKayit.InRoleCurrent();
                if (tiJuriOnerileriOgrenciAdina && !tiJuriOnerileriKayit)
                    danismanId = UserIdentity.Current.Id;
                var basvuru = db.TijBasvurus.First(p => p.UniqueID == uniqueId);

                var enstitu = db.Enstitulers.First(p => p.EnstituKod == basvuru.EnstituKod);

                var sonTijBasvuruOneri = basvuru.TijBasvuruOneris.OrderByDescending(o => o.TijBasvuruOneriID).FirstOrDefault();
                model.TijBasvuruOneriList = basvuru.TijBasvuruOneris.ToList().Where(p => p.TezDanismanID == (danismanId ?? p.TezDanismanID)).Select(s => new TijBasvuruOneriDetayDto
                {
                    TijBasvuruOneriID = s.TijBasvuruOneriID,
                    TijBasvuruID = s.TijBasvuruID,
                    IsSonBasvuru = sonTijBasvuruOneri == null || s.TijBasvuruOneriID == sonTijBasvuruOneri.TijBasvuruOneriID,
                    UniqueID = s.UniqueID,
                    FormKodu = s.FormKodu,
                    TijDegisiklikTipID = s.TijDegisiklikTipID,
                    TijDegisiklikTipAdi = s.TijDegisiklikTipID.HasValue ? s.TijDegisiklikTipleri.TijDegisiklikTipAdi : "",
                    TijFormTipID = s.TijFormTipID,
                    TijFormTipAdi = s.TijFormTipleri.TikFormTipAdi,
                    IsObsData = s.IsObsData,
                    DonemBaslangicYil = s.DonemBaslangicYil,
                    DonemID = s.DonemID,
                    DonemAdi = s.DonemBaslangicYil + "/" + (s.DonemBaslangicYil + 1) + " " + s.Donemler.DonemAdi,
                    BasvuruTarihi = s.BasvuruTarihi,
                    SozluSinavBasariTarihi = s.SozluSinavBasariTarihi,
                    IsTezDiliTr = s.IsTezDiliTr,
                    TezBaslikTr = s.TezBaslikTr,
                    TezBaslikEn = s.TezBaslikEn,
                    TezDanismanID = s.TezDanismanID,
                    DanismanAdi = s.TezDanismanID.HasValue ? (s.Kullanicilar.Unvanlar.UnvanAdi + " " + s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad) : "",
                    IsDilTaahhutuOnaylandi = s.IsDilTaahhutuOnaylandi,
                    DanismanOnayladi = s.DanismanOnayladi,
                    DanismanOnayTarihi = s.DanismanOnayTarihi,
                    DanismanOnaylanmamaAciklamasi = s.DanismanOnaylanmamaAciklamasi,
                    EYKYaGonderildi = s.EYKYaGonderildi,
                    EYKYaGonderildiIslemYapanID = s.EYKYaGonderildiIslemYapanID,
                    EYKYaGonderildiIslemTarihi = s.EYKYaGonderildiIslemTarihi,
                    EYKYaGonderimDurumAciklamasi = s.EYKYaGonderimDurumAciklamasi,
                    EYKDaOnaylandi = s.EYKDaOnaylandi,
                    EYKDaOnaylandiIslemYapanID = s.EYKDaOnaylandiIslemYapanID,
                    EYKDaOnaylandiIslemTarihi = s.EYKDaOnaylandiIslemTarihi,
                    EYKTarihi = s.EYKTarihi,
                    EYKDaOnaylanmadiDurumAciklamasi = s.EYKDaOnaylanmadiDurumAciklamasi,
                    SelectEykYaGonderildi = new SelectList(ComboData.GetCmbEykGonderimDurumData(true, s.EYKYaGonderildi == true ? s.EYKYaGonderildiIslemTarihi : null), "Value", "Caption", s.EYKYaGonderildi),
                    SelectEykDaOnaylandi = new SelectList(ComboData.GetCmbEykOnayDurumData(true), "Value", "Caption", s.EYKDaOnaylandi),
                    TijBasvuruOneriJurilers = s.TijBasvuruOneriJurilers.ToList()
                }).OrderByDescending(o => o.BasvuruTarihi).ToList();
                model.IsYeniBasvuruYapilabilir = basvuru.IsYeniBasvuruYapilabilir;
                model.UniqueID = basvuru.UniqueID;
                model.TezDanismanID = basvuru.TezDanismanID;
                model.TijBasvuruID = basvuru.TijBasvuruID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;
                model.ResimAdi = basvuru.Kullanicilar.ResimAdi;
                model.Ad = basvuru.Kullanicilar.Ad;
                model.Soyad = basvuru.Kullanicilar.Soyad;
                model.TcKimlikNo = basvuru.Kullanicilar.TcKimlikNo;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipID = basvuru.OgrenimTipID;
                model.OgrenimTipAdi = basvuru.OgrenimTipleri.OgrenimTipAdi;
                model.AnabilimdaliAdi = basvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = basvuru.Programlar.ProgramAdi;
                model.ProgramKod = basvuru.ProgramKod;
                model.KayitOgretimYiliBaslangic = basvuru.KayitOgretimYiliBaslangic;
                model.KayitOgretimYiliDonemID = basvuru.KayitOgretimYiliDonemID;
                model.KayitTarihi = basvuru.KayitTarihi;
                model.EnstituAdi = enstitu.EnstituAd;
                model.KullaniciID = basvuru.KullaniciID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.IslemTarihi = basvuru.IslemTarihi;
                model.IslemYapanID = basvuru.IslemYapanID;
                model.IslemYapanIP = basvuru.IslemYapanIP;
            }
            return model;
        }


        public static MmMessage GetTijBasvuruDetaySilKontrol(Guid tijBasvuruOneriUniqueId)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var silmeYetkisi = RoleNames.TiJuriOnerileriSil.InRoleCurrent();
                var basvuru = db.TijBasvuruOneris.FirstOrDefault(p => p.UniqueID == tijBasvuruOneriUniqueId);
                if (basvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Silinmek istenen jüri önerisi sistemde bulunamadı.");
                }
                else
                {
                    if (silmeYetkisi)
                    {
                        if (!UserIdentity.Current.EnstituKods.Contains(basvuru.TijBasvuru.EnstituKod))
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                        }
                    }
                    else
                    {

                        if (basvuru.DanismanOnayladi.HasValue)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Danışman tarafından onaylanan bir jüri önerisi silinemez!");
                        }
                    }
                }
            }
            return msg;
        }


        public static List<int> GetDanismanOgrencileriKullaniciId(int danismanId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var ogrencis = db.Kullanicilars.Where(p => p.DanismanID == danismanId &&
                                                           p.YtuOgrencisi && p.OgrenimDurumID == OgrenimDurum.HalenOğrenci && p.DanismanID.HasValue).Select(s => s.KullaniciID)
                    .ToList();
                return ogrencis;
            }
        }
        public static JsonResult GetFilterOgrenciJsonResult(string term, string enstituKod, int? danismanId = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qKul = db.Kullanicilars.Where(p => p.YtuOgrencisi && p.EnstituKod == enstituKod && p.OgrenimDurumID == OgrenimDurum.HalenOğrenci && p.DanismanID.HasValue).AsQueryable();

                if (!term.IsNullOrWhiteSpace())
                {
                    qKul = qKul.Where(p =>
                        ((p.Ad + " " + p.Soyad).Contains(term) || p.OgrenciNo.StartsWith(term) ||
                         p.TcKimlikNo.StartsWith(term)));
                }

                if (danismanId.HasValue) qKul = qKul.Where(p => p.DanismanID == danismanId);

                var ogrenciList = qKul.Select(s => new
                {
                    s.KullaniciID,
                    s.Ad,
                    s.Soyad,
                    s.OgrenciNo,
                    s.ResimAdi,
                    s.Programlar.ProgramAdi
                }).Take(danismanId.HasValue ? int.MaxValue : 15).ToList().Select(s => new
                {
                    id = s.KullaniciID,
                    s.ProgramAdi,
                    text = s.OgrenciNo + " " + s.Ad + " " + s.Soyad,
                    Images = s.ResimAdi.ToKullaniciResim()
                }).ToList();

                return ogrenciList.ToJsonResult();
            }
        }



        public static MmMessage SendMailDanismanOnay(Guid tijBasvuruOneriUniqueId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
                {

                    #region sendMail

                    var tijBasvuruOneri = entities.TijBasvuruOneris.First(f => f.UniqueID == tijBasvuruOneriUniqueId);
                    var enstituL = tijBasvuruOneri.TijBasvuru.Enstituler;
                    var sablonlar = entities.MailSablonlaris.Where(p => p.EnstituKod == enstituL.EnstituKod).ToList();


                    var ogrenci = tijBasvuruOneri.TijBasvuru.Kullanicilar;
                    var danisman = tijBasvuruOneri.Kullanicilar;
                    var mModel = new List<SablonMailModel>
                        {
                            new SablonMailModel
                            {

                                AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                                EMails = new List<MailSendList>
                                    { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                                MailSablonTipID = tijBasvuruOneri.DanismanOnayladi == true
                                    ? MailSablonTipi.TijOneriFormuDanismanTarafindanOnaylandiOgrenciye
                                    : MailSablonTipi.TijOneriFormuDanismanTarafindanRetEdildiOgrenciye,
                            },
                            new SablonMailModel
                            {

                                AdSoyad =danisman.Unvanlar.UnvanAdi+" "+  danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList>
                                    { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = tijBasvuruOneri.DanismanOnayladi == true
                                    ? MailSablonTipi.TijOneriFormuDanismanTarafindanOnaylandiDanismana
                                    : MailSablonTipi.TijOneriFormuDanismanTarafindanRetEdildiDanismana,
                            }
                        };

                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;

                        if (tijBasvuruOneri.DanismanOnayladi == true)
                        {
                            var ids = new List<int?>() { tijBasvuruOneri.TijBasvuruOneriID };
                            var raporTipId = tijBasvuruOneri.TijFormTipID == TijFormTipi.YeniForm
                                ? RaporTipleri.TezIzlemeJuriOneriFormu
                                : RaporTipleri.TezIzlemeJuriDegisiklikFormu;
                            var ekler = Management.exportRaporPdf(raporTipId, ids);
                            item.Attachments.AddRange(ekler);
                        }
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var paramereDegerleri = new List<MailReplaceParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstituL.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DonemAdi", Value = (tijBasvuruOneri.DonemBaslangicYil + " " + tijBasvuruOneri.DonemBaslangicYil + 1) + " " + tijBasvuruOneri.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = tijBasvuruOneri.TijBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@RetTarihi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "RetTarihi", Value = tijBasvuruOneri.DanismanOnayTarihi.ToFormatDateAndTime() });
                        if (item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "RetAciklamasi", Value = tijBasvuruOneri.DanismanOnaylanmamaAciklamasi });


                        var mCOntent = SystemMails.GetSystemMailContent(enstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstituL.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = enstituL.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title + " (" + item.AdSoyad + ")"; 
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailEkleris = item.Attachments.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList();
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            entities.GonderilenMaillers.Add(kModel);
                            entities.SaveChanges();

                        }
                    }

                    #endregion
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "";
                //message = isLinkOrSonuc ? "Yeterlik Jüri üyeleri onayı için Komite üyelerine onay davet linki mail olarak gönderilirken bir hata oluştu!" : "Yeterlik Jüri üyeleri onayı  davet linki  Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
                //SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTIDegerlendirmeLink \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                ////mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailEykOnay(Guid tijBasvuruOneriUniqueId, bool eykDaOnayOrEykYaGonderim, bool onaylandi)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
                {

                    #region sendMail 
                    var tijBasvuruOneri = entities.TijBasvuruOneris.First(f => f.UniqueID == tijBasvuruOneriUniqueId);
                    var enstituL = tijBasvuruOneri.TijBasvuru.Enstituler;
                    var sablonlar = entities.MailSablonlaris.Where(p => p.EnstituKod == enstituL.EnstituKod).ToList();
                    var ogrenci = tijBasvuruOneri.TijBasvuru.Kullanicilar;
                    var danisman = tijBasvuruOneri.Kullanicilar;
                    var ogrenciMailSablonTipId = 0;
                    var danismanMailSablonTipId = 0;
                    if (eykDaOnayOrEykYaGonderim)
                    {
                        ogrenciMailSablonTipId =
                            onaylandi == true
                                ? MailSablonTipi.TijOneriFormuEykdaOnaylandiEdildiOgrenciye
                                : MailSablonTipi.TijOneriFormuEykdaOnaylanmadiEdildiOgrenciye;

                        danismanMailSablonTipId =
                            onaylandi == true
                                ? MailSablonTipi.TijOneriFormuEykdaOnaylandiEdildiDanismana
                                : MailSablonTipi.TijOneriFormuEykdaOnaylanmadiEdildiDanismana;
                    }
                    else if (onaylandi == false)
                    {
                        ogrenciMailSablonTipId = MailSablonTipi.TijOneriFormuEykyaGonderimiRetEdildiOgrenciye;
                        danismanMailSablonTipId = MailSablonTipi.TijOneriFormuEykyaGonderimiRetEdildiDanismana;
                    }
                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {

                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList>
                                { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID =ogrenciMailSablonTipId
                        },
                        new SablonMailModel
                        {

                            AdSoyad =danisman.Unvanlar.UnvanAdi+" "+ danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList>
                                { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = danismanMailSablonTipId
                        }
                    };

                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;

                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var paramereDegerleri = new List<MailReplaceParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstituL.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DonemAdi", Value = (tijBasvuruOneri.DonemBaslangicYil + " " + tijBasvuruOneri.DonemBaslangicYil + 1) + " " + tijBasvuruOneri.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = tijBasvuruOneri.TijBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.ProgramAdi });
                        if (eykDaOnayOrEykYaGonderim)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@EykTarihi"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EykTarihi", Value = tijBasvuruOneri.EYKTarihi.ToFormatDate() });
                        }
                        else
                        {
                            if (item.SablonParametreleri.Any(a => a == "@RetTarihi"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "RetTarihi", Value = tijBasvuruOneri.EYKYaGonderildiIslemTarihi.ToFormatDateAndTime() });

                        }

                        if (item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "RetAciklamasi", Value = tijBasvuruOneri.DanismanOnaylanmamaAciklamasi });

                        var attachs = new List<System.Net.Mail.Attachment>();

                        var mCOntent = SystemMails.GetSystemMailContent(enstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstituL.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, attachs);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstituL.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")"
                            };
                            kModel.Konu = mCOntent.Title + " (" + item.AdSoyad + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailEkleris = attachs.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList();
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            entities.GonderilenMaillers.Add(kModel);
                            entities.SaveChanges();
                        }
                    }

                    #endregion
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "";
                //message = isLinkOrSonuc ? "Yeterlik Jüri üyeleri onayı için Komite üyelerine onay davet linki mail olarak gönderilirken bir hata oluştu!" : "Yeterlik Jüri üyeleri onayı  davet linki  Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
                //SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTIDegerlendirmeLink \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                ////mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailBasvuruYapildi(Guid tijBasvuruOneriUniqueId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
                {

                    #region sendMail 
                    var tijBasvuruOneri = entities.TijBasvuruOneris.First(f => f.UniqueID == tijBasvuruOneriUniqueId);
                    var enstituL = tijBasvuruOneri.TijBasvuru.Enstituler;
                    var sablonlar = entities.MailSablonlaris.Where(p => p.EnstituKod == enstituL.EnstituKod).ToList();
                    var ogrenci = tijBasvuruOneri.TijBasvuru.Kullanicilar;
                    var danisman = tijBasvuruOneri.Kullanicilar;

                    var mModel = new List<SablonMailModel>
                    {

                        new SablonMailModel
                        {

                            AdSoyad =danisman.Unvanlar.UnvanAdi+" "+  danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList>
                                { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TijFormuOlusturulduDanismana
                        }
                    };

                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;

                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var paramereDegerleri = new List<MailReplaceParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstituL.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DonemAdi", Value = (tijBasvuruOneri.DonemBaslangicYil + " " + tijBasvuruOneri.DonemBaslangicYil + 1) + " " + tijBasvuruOneri.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = tijBasvuruOneri.TijBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.ProgramAdi });
                        var attachs = new List<System.Net.Mail.Attachment>();

                        var mCOntent = SystemMails.GetSystemMailContent(enstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstituL.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, attachs);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = enstituL.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title + " (" + item.AdSoyad + ")"; 
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailEkleris = attachs.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList();
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            entities.GonderilenMaillers.Add(kModel);
                            entities.SaveChanges();
                        }
                    }

                    #endregion
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "";
                //message = isLinkOrSonuc ? "Yeterlik Jüri üyeleri onayı için Komite üyelerine onay davet linki mail olarak gönderilirken bir hata oluştu!" : "Yeterlik Jüri üyeleri onayı  davet linki  Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
                //SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTIDegerlendirmeLink \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                ////mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
    }
}