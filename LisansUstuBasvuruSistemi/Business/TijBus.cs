using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class TijBus
    {
        public static bool IsAktifDevamEdenTijVarMi(int kullaniciId, string ogrenciNo)
        {
            using (var entities = new LubsDbEntities())
            {
                return entities.TijBasvuruOneris.Any(a => a.TijBasvuru.KullaniciID == kullaniciId &&
                                                    a.TijBasvuru.OgrenciNo == ogrenciNo &&
                                                    !a.TijBasvuru.IsYeniBasvuruYapilabilir);

            }

        }
        public static List<CmbIntDto> CmbTijDegisiklikTipListe(bool bosSecimVar = false)
        {

            using (var entities = new LubsDbEntities())
            {
                var degisiklitTips = entities.TijDegisiklikTipleris
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

            using (var entities = new LubsDbEntities())
            {
                var qDegisiklitTips = entities.TijFormTipleris
                    .Select(s => new CmbIntDto
                    {
                        Value = s.TijFormTipID,
                        Caption = s.TikFormTipAdi

                    }).AsQueryable();
                if (isIlkOneri.HasValue)
                {
                    qDegisiklitTips = qDegisiklitTips.Where(p =>
                        isIlkOneri == true ? p.Value == TijFormTipiEnum.YeniForm : p.Value != TijFormTipiEnum.YeniForm);
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

            using (var entities = new LubsDbEntities())
            {
                var donems = entities.TijBasvuruOneris.Select(s => new { s.DonemBaslangicYil, s.DonemID, s.Donemler.DonemAdi })
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
            using (var entities = new LubsDbEntities())
            {
                var yeterliAnabilimDaliIds = entities.TijBasvurus
                    .Where(p => p.EnstituKod == enstituKod).Select(s => s.Programlar.AnabilimDaliID).Distinct().ToList();

                var anabilimDallaris = entities.AnabilimDallaris.Where(p => yeterliAnabilimDaliIds.Contains(p.AnabilimDaliID))
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
            dct.Add(new CmbIntDto { Value = 10, Caption = "EYK'Ya Hazırlandı" });
            dct.Add(new CmbIntDto { Value = 7, Caption = "EYK'da Onay Bekliyor" });
            dct.Add(new CmbIntDto { Value = 8, Caption = "EYK'Da Onaylandı" });
            dct.Add(new CmbIntDto { Value = 9, Caption = "EYK'Da Onaylanmadı" });
            return dct;
        }
        public static List<CmbIntDto> CmbTijOneriTipListe(bool bosSecimVar = false)
        {
            using (var entities = new LubsDbEntities())
            {

                var tips = entities.TijFormTipleris
                    .Select(s => new { s.TijFormTipID, s.TikFormTipAdi }).Select(
                        s =>
                            new CmbIntDto { Value = s.TijFormTipID, Caption = s.TikFormTipAdi }
                    ).ToList();
                if (bosSecimVar) tips.Insert(0, new CmbIntDto { Value = null, Caption = "" });

                return tips;
            }
        }
        public static IHtmlString ToTijBasvuruDurumView(this TijBasvuruOneriDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("TiJuriOnerileriGb", "BasvuruDurumView");
            return pagerString;
        }
        public static IHtmlString ToTijBasvuruDonemView(this TijBasvuruOneriDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("TiJuriOnerileriGb", "BasvuruDonemView");
            return pagerString;
        }
        public static FmTijBasvuru BasvuruBilgi(FmTijBasvuru model)
        {
            using (var entities = new LubsDbEntities())
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
                        if (kul.OgrenimTipKod.IsDoktora() && kul.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci)
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

                                if (TdoBus.IsAktifDanismanOneriVar(kul.KullaniciID))
                                {
                                    model.BasvuruKontrolBilgi.IsBasvuruAcik = false;
                                    model.BasvuruKontrolBilgi.Aciklama = "Adınıza ait yapılan bir Tez Danışman Öneri başvurusunuz bulunmakta. Jüri önerisi yapılabilmesi bu sürecinin tamamlanması gerekmektedir.";
                                }
                                else if (TdoBus.IsAktifEsDanismanOneriVar(kul.KullaniciID))
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
            using (var entities = new LubsDbEntities())
            {
                var kul = entities.Kullanicilars.First(f => f.KullaniciID == kullaniciId);
                if (kul.YtuOgrencisi)
                {
                    var obsOgrenci = KullanicilarBus.OgrenciKontrol(kul.OgrenciNo);
                    if (!obsOgrenci.Hata)
                    {
                        var juriler = obsOgrenci.TezIzlJuriBilgileri.ToList();
                        var juriDbDtoList = new List<TijBasvuruOneriJuriler>();
                        foreach (var juri in juriler)
                        {
                            int? universiteId = GlobalSistemSetting.UniversiteYtuKod;
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

                            juriDbDtoList.Add(new TijBasvuruOneriJuriler
                            {
                                IsYeniOrOnceki = true,
                                RowNum = isTezDanismani ? 0 : 1,
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

                        var isTezDanismanVar = juriDbDtoList.Any(p => p.IsTezDanismani);
                        var isYtuIcijuriVar = juriDbDtoList.Any(p => p.IsYtuIciJuri);
                        var isYtuDisiJuriVar = juriDbDtoList.Any(p => !p.IsYtuIciJuri);
                        if (!isTezDanismanVar || !isYtuIcijuriVar || !isYtuDisiJuriVar)
                        {
                            msg.Add(kul.Ad + " " + kul.Soyad + " öğrencisine ait OBS sisteminde tanımlı Tez İzleme Jüri bilgilerinde");

                            if (!isTezDanismanVar)
                            {
                                msg.Add("Tez Danışmanı bilgisi bulunmamaktadır.");
                            }
                            if (!isYtuIcijuriVar)
                            {
                                msg.Add("Ytu İçi Jüri bilgisi bulunmamaktadır.");
                            }
                            if (!isYtuDisiJuriVar)
                            {
                                msg.Add("Ytu Dışı Jüri bilgisi bulunmamaktadır.");
                            }
                            msg.Add("<b>Jüri önerisi yapabilmeniz için bu durumu enstitü yetkililerine iletiniz.</b>");
                            if (juriDbDtoList.Any())
                            {
                                StringBuilder htmlTable = new StringBuilder();
                                htmlTable.Append("<table class='table' style='width:100%;'>");
                                htmlTable.Append("<tr class='danger'>");
                                htmlTable.Append(
                                    "<th colspan='3' style='text-align:center';>OBS Sisteminde gözüken jüri bilgileriniz aşağıdaki gibidir</th>");
                                htmlTable.Append("</tr>");
                                htmlTable.Append("<tr class='info'>");
                                htmlTable.Append("<th></th>");
                                htmlTable.Append("<th>Jüri Ad Soyad</th>");
                                htmlTable.Append("<th>Üniversite</th>");
                                htmlTable.Append("</tr>");
                                foreach (var juri in juriDbDtoList.OrderBy(o =>
                                             o.IsTezDanismani ? 1 : (o.IsYtuIciJuri ? 2 : 3)))
                                {
                                    htmlTable.Append("<tr>");
                                    htmlTable.Append("<td>" + (juri.IsTezDanismani ? "Danışman" : "Jüri Üyesi") +
                                                     "</td>");
                                    htmlTable.Append("<td>" + juri.UnvanAdi + " " + juri.AdSoyad + "</td>");
                                    htmlTable.Append("<td>" + juri.UniversiteAdi + "</td>");
                                    htmlTable.Append("</tr>");
                                }

                                htmlTable.Append("</table>");
                                msg.Add(htmlTable.ToString());
                            }
                        }


                    }
                    else
                    {
                        msg.Add("OBS sisteminden öğrenci bilgisi kontrolü yapılamadı. Enstitü ile iletişime geçiniz! Hata: " + obsOgrenci.HataMsj);
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("OBS sisteminden öğrenci bilgisi kontrolü yapılamadı. Hata:" + obsOgrenci.HataMsj, "TezIzlemeJuriOneriBus/TezIzlemeJuriOneriSenkronizasyonMsg", BilgiTipiEnum.Kritik);
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

            using (var entities = new LubsDbEntities())
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
                                var donemBiligi = DateTime.Now.ToAkademikDonemBilgi();
                                var tijOneri = new TijBasvuruOneri
                                {
                                    UniqueID = uniqueId,
                                    FormKodu = uniqueId.ToString().Substring(0, 8).ToUpper(),
                                    TijFormTipID = TijFormTipiEnum.YeniForm,
                                    IsObsData = true,
                                    TezDanismanID = kul.DanismanID,
                                    BasvuruTarihi = DateTime.Now,
                                    DonemBaslangicYil = donemBiligi.BaslangicYil,
                                    DonemID = donemBiligi.DonemId,
                                    SozluSinavBasariTarihi = ogrenciYeterlikBilgi.DR_YET_SOZ_SNV_TARIH.ToDate().Value,
                                    IsTezDiliTr = obsOgrenci.IsTezDiliTr,
                                    TezBaslikTr = obsOgrenci.OgrenciTez.TEZ_BASLIK,
                                    TezBaslikEn = obsOgrenci.OgrenciTez.TEZ_BASLIK_ENG
                                };
                                foreach (var juri in juriler)
                                {
                                    int? universiteId = GlobalSistemSetting.UniversiteYtuKod;
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
                                        IsYeniOrOnceki = true,
                                        RowNum = isTezDanismani ? 0 : 1,
                                        IsYtuIciJuri = isYtuIciJuri,
                                        IsTezDanismani = isTezDanismani,
                                        UnvanAdi = juri.TEZ_IZLEME_JURI_UNVAN.ToJuriUnvanAdi(),
                                        AdSoyad = juri.TEZ_IZLEME_JURI_ADSOY.ToUpper(),
                                        EMail = juri.TEZ_IZLEME_JURI_EPOSTA,
                                        IsAsil = true,
                                        UniversiteID = universiteId,
                                        UniversiteAdi = juri.TEZ_IZLEME_JURI_UNIVER.ToUpper(),
                                        AnabilimdaliAdi = juri.TEZ_IZLEME_JURI_ANABLMDAL.ToUpper()
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
            using (var entities = new LubsDbEntities())
            {


                int? danismanId = null;
                var tiJuriOnerileriOgrenciAdina = RoleNames.TiJuriOnerileriOgrenciAdina.InRoleCurrent();
                var tiJuriOnerileriYetkili = RoleNames.TiJuriOnerileriEykDaOnay.InRoleCurrent() || RoleNames.TiJuriOnerileriEykYaGonder.InRoleCurrent();
                if (tiJuriOnerileriOgrenciAdina && !tiJuriOnerileriYetkili)
                    danismanId = UserIdentity.Current.Id;
                var basvuru = entities.TijBasvurus.First(p => p.UniqueID == uniqueId);

                var ogrenciObsBilgi =
                    KullanicilarBus.OgrenciBilgisiGuncelleObs(basvuru.KullaniciID);
                //ana başvurudaki danışman ile aktif danışman uyuşmuyor ise aktif danışmanı ana başvuruya eşleştir.
                if (ogrenciObsBilgi.KayitVar)
                {
                    if (basvuru.OgrenciNo == ogrenciObsBilgi.OgrenciInfo.OGR_NO && basvuru.TezDanismanID != ogrenciObsBilgi.AktifDanismanID)
                    {
                        basvuru.TezDanismanID = ogrenciObsBilgi.AktifDanismanID;
                        entities.SaveChanges();
                    }
                }

                var enstitu = entities.Enstitulers.First(p => p.EnstituKod == basvuru.EnstituKod);
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
                    DegisiklikAciklamasi = s.DegisiklikAciklamasi,
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
                    TezDanismaniUserKey = s.TezDanismanID.HasValue ? s.Kullanicilar.UserKey : (Guid?)null,
                    IsDilTaahhutuOnaylandi = s.IsDilTaahhutuOnaylandi,
                    DanismanOnayladi = s.DanismanOnayladi,
                    DanismanOnayTarihi = s.DanismanOnayTarihi,
                    DanismanOnaylanmamaAciklamasi = s.DanismanOnaylanmamaAciklamasi,
                    EYKYaGonderildi = s.EYKYaGonderildi,
                    EYKYaGonderildiIslemYapanID = s.EYKYaGonderildiIslemYapanID,
                    EYKYaGonderildiIslemTarihi = s.EYKYaGonderildiIslemTarihi,
                    EYKYaGonderimDurumAciklamasi = s.EYKYaGonderimDurumAciklamasi,
                    EYKYaHazirlandi = s.EYKYaHazirlandi,
                    EYKYaHazirlandiIslemTarihi = s.EYKYaHazirlandiIslemTarihi,
                    EYKYaHazirlandiIslemYapanID = s.EYKYaHazirlandiIslemYapanID,
                    EYKDaOnaylandi = s.EYKDaOnaylandi,
                    EYKDaOnaylandiIslemYapanID = s.EYKDaOnaylandiIslemYapanID,
                    EYKDaOnaylandiIslemTarihi = s.EYKDaOnaylandiIslemTarihi,
                    EYKTarihi = s.EYKTarihi,
                    EYKDaOnaylanmadiDurumAciklamasi = s.EYKDaOnaylanmadiDurumAciklamasi,
                    TijBasvuruOneriJurilers = s.TijBasvuruOneriJurilers.OrderBy(o => o.IsYeniOrOnceki ? 1 : 2)
                                                                       .ThenBy(t => t.IsTezDanismani ? 1 : 2)
                                                                       .ThenBy(o => o.IsYtuIciJuri ? 1 : 2)
                                                                       .ThenBy(t => t.RowNum).ToList()
                }).OrderByDescending(o => o.BasvuruTarihi).ToList();

                var sonTij = model.TijBasvuruOneriList.FirstOrDefault();
                model.DurumHtmlString = (sonTij ?? new TijBasvuruOneriDetayDto()).ToTijBasvuruDurumView().ToString();
                model.DonemHtmlString = (sonTij ?? new TijBasvuruOneriDetayDto()).ToTijBasvuruDonemView().ToString();
                model.IsYeniBasvuruYapilabilir = basvuru.IsYeniBasvuruYapilabilir;
                model.UniqueID = basvuru.UniqueID;
                model.TezDanismaniUserKey = entities.Kullanicilars.Where(p => p.KullaniciID == basvuru.TezDanismanID)
                    .Select(s => s.UserKey).FirstOrDefault();
                model.TezDanismanID = basvuru.TezDanismanID;
                model.TijBasvuruID = basvuru.TijBasvuruID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + entities.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;
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
        public static MmMessage GetTijBasvuruDetayIslemKontrol(Guid tijBasvuruOneriUniqueId)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };

            using (var entities = new LubsDbEntities())
            {
                var basvuru = entities.TijBasvuruOneris.FirstOrDefault(p => p.UniqueID == tijBasvuruOneriUniqueId);
                if (basvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("İşlem yapmak istenen jüri önerisi sistemde bulunamadı.");
                }
                else
                {
                    if (UserIdentity.Current.IsYetkiliTij)
                    {
                        if (!UserIdentity.Current.EnstituKods.Contains(basvuru.TijBasvuru.EnstituKod))
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu enstitüye ait jüri önerileri üstünde işlem yapmaya yetkili değilsiniz!");
                        }
                    }
                    else
                    {

                        var isOgrenci = basvuru.TijBasvuru.KullaniciID == UserIdentity.Current.Id;

                        if (isOgrenci)
                        {
                            if (basvuru.DanismanOnayladi.HasValue)
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Danışman tarafından onaylanan bir jüri önerisi için işlem yapılamaz!");
                            }
                        }
                        else
                        {
                            if (basvuru.EYKYaGonderildi.HasValue)
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Eyk ya gönderilen bir jüri önerisi için işlem yapılamaz!");
                            }
                        }
                    }
                }
            }
            return msg;
        }


        public static List<int> GetDanismanOgrencileriKullaniciId(int danismanId)
        {
            using (var entities = new LubsDbEntities())
            {
                var ogrencis = entities.Kullanicilars.Where(p => p.DanismanID == danismanId &&
                                                           p.YtuOgrencisi && p.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci && p.DanismanID.HasValue).Select(s => s.KullaniciID)
                    .ToList();
                return ogrencis;
            }
        }
        public static JsonResult GetFilterOgrenciJsonResult(string term, string enstituKod, int? danismanId = null)
        {
            using (var entities = new LubsDbEntities())
            {
                var qKul = entities.Kullanicilars.Where(p => p.YtuOgrencisi && p.EnstituKod == enstituKod && p.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci && p.DanismanID.HasValue).AsQueryable();

                if (!term.IsNullOrWhiteSpace())
                {
                    qKul = qKul.Where(p =>
                        ((p.Ad + " " + p.Soyad).Contains(term) || p.OgrenciNo.StartsWith(term) ||
                         p.TcKimlikNo.StartsWith(term)));
                }
                else if (danismanId.HasValue)
                {
                    qKul = qKul.Where(p => p.DanismanID == danismanId);
                }

                if (danismanId.HasValue) qKul = qKul.OrderBy(p => p.DanismanID == danismanId ? 1 : 2).ThenBy(t => t.Ad).ThenBy(t => t.Soyad);

                var ogrenciList = qKul.Select(s => new
                {
                    s.KullaniciID,
                    s.DanismanID,
                    s.Ad,
                    s.Soyad,
                    s.OgrenciNo,
                    s.ResimAdi,
                    s.Programlar.ProgramAdi
                }).Take(50).ToList().Select(s => new
                {
                    id = s.KullaniciID,
                    s.ProgramAdi,
                    text = s.OgrenciNo + " " + s.Ad + " " + s.Soyad,
                    IsDanismanOgrenci = s.DanismanID == danismanId,
                    Images = s.ResimAdi.ToKullaniciResim()
                }).ToList();

                return ogrenciList.ToJsonResult();
            }
        }



        public static MmMessage SendMailDanismanOnay(Guid tijBasvuruOneriUniqueId)
        {
            return MailSenderTij.SendMailDanismanOnay(tijBasvuruOneriUniqueId);
        }
        public static MmMessage SendMailEykOnay(Guid tijBasvuruOneriUniqueId, bool eykDaOnayOrEykYaGonderim, bool onaylandi)
        {
            return MailSenderTij.SendMailEykOnay(tijBasvuruOneriUniqueId, eykDaOnayOrEykYaGonderim, onaylandi);
        }
        public static MmMessage SendMailBasvuruYapildi(Guid tijBasvuruOneriUniqueId)
        {
            return MailSenderTij.SendMailBasvuruYapildi(tijBasvuruOneriUniqueId);
        }
    }
}