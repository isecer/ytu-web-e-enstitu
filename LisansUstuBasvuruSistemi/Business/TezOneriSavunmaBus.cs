using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System.IO;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Logs;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class TezOneriSavunmaBus
    {
        public static IHtmlString TosBasvuruDurumView(this TosDurumDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("TosBasvuru", "BasvuruDurumView");
            return pagerString;
        }
        public static IHtmlString TosBasvuruDonemView(this ToBasvuruSavunmaDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("TosBasvuru", "BasvuruDonemView");
            return pagerString;
        }
        public static bool BasvuruOlustur(int kullaniciId)
        {

            using (var entities = new LisansustuBasvuruSistemiEntities())
            {
                var kul = entities.Kullanicilars.First(f => f.KullaniciID == kullaniciId);
                if (kul.YtuOgrencisi)
                {
                    var obsOgrenci = KullanicilarBus.OgrenciKontrol(kul.OgrenciNo);


                    if (!obsOgrenci.Hata)
                    {
                        kul = entities.Kullanicilars.First(f => f.KullaniciID == kullaniciId);
                        var ogrenciYeterlikBilgi =
                            obsOgrenci.OgrenciYeters.FirstOrDefault(p => p.DR_YET_GNL_SNV_DURUM == "Başarılı" && p.DR_YET_SOZ_SNV_DURUM == "Başarılı");


                        if (ogrenciYeterlikBilgi != null && ogrenciYeterlikBilgi.DR_YET_SOZ_SNV_TARIH.ToDate().HasValue)
                        {
                            var yeterlikBasariTarihi = ogrenciYeterlikBilgi.DR_YET_SOZ_SNV_TARIH.ToDate().Value;

                            var basvuru = entities.ToBasvurus.FirstOrDefault(f =>
                                f.KullaniciID == kul.KullaniciID && f.EnstituKod == kul.EnstituKod &&
                                (f.ProgramKod == kul.ProgramKod || f.OgrenciNo == kul.OgrenciNo));
                            if (basvuru != null)
                            {
                                basvuru.ProgramKod = kul.ProgramKod;
                                basvuru.OgrenciNo = kul.OgrenciNo;
                                basvuru.OgrenimTipKod = kul.OgrenimTipKod.Value;
                                basvuru.KayitOgretimYiliBaslangic = kul.KayitYilBaslangic;
                                basvuru.KayitOgretimYiliDonemID = kul.KayitDonemID;
                                basvuru.YeterlikSozluSinavTarihi = yeterlikBasariTarihi;
                                basvuru.IslemTarihi = DateTime.Now;
                                basvuru.IslemYapanID = UserIdentity.Current.Id;
                                basvuru.IslemYapanIP = UserIdentity.Ip;
                            }
                            else
                            {
                                entities.ToBasvurus.Add(new ToBasvuru
                                {
                                    UniqueID = Guid.NewGuid(),
                                    EnstituKod = kul.EnstituKod,
                                    BasvuruTarihi = DateTime.Now,
                                    KullaniciID = kul.KullaniciID,
                                    OgrenciNo = kul.OgrenciNo,
                                    OgrenimTipKod = kul.OgrenimTipKod.Value,
                                    ProgramKod = kul.ProgramKod,
                                    KayitOgretimYiliBaslangic = kul.KayitYilBaslangic,
                                    KayitOgretimYiliDonemID = kul.KayitDonemID,
                                    KayitTarihi = kul.KayitTarihi,
                                    TezDanismanID = kul.DanismanID,
                                    YeterlikSozluSinavTarihi = yeterlikBasariTarihi,
                                    IslemTarihi = DateTime.Now,
                                    IslemYapanID = UserIdentity.Current.Id,
                                    IslemYapanIP = UserIdentity.Ip
                                });
                            }

                            entities.SaveChanges();
                        }

                    }
                    else
                    {
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("OBS sisteminden öğrenci bilgisi kontrolü yapılamadı. Hata:" + obsOgrenci.HataMsj, "TezOneriSavunmaBus/TezIzlemeJuriOneriSenkronizasyonMsg", LogType.Kritik);
                    }
                }

            }

            return true;
        }

        public static int TosSavunmaNo(Guid toUniqueId, Guid? tosUniqueId)
        {
            using (var entities = new LisansustuBasvuruSistemiEntities())
            {
                var toBasvuru = entities.ToBasvurus.First(p => p.UniqueID == toUniqueId);

                var tosbasvurus = toBasvuru.ToBasvuruSavunmas.Where(p => p.UniqueID != tosUniqueId)
                    .Select(s => new { s.ToBasvuruSavunmaID, s.SavunmaNo, s.ToBasvuruSavunmaDurumID }).ToList();
                var sonBasvuru = tosbasvurus.OrderByDescending(o => o.ToBasvuruSavunmaID).FirstOrDefault();
                if (!tosbasvurus.Any()) return 1;
                if (sonBasvuru.ToBasvuruSavunmaDurumID == 1) return 1;
                return sonBasvuru.SavunmaNo + 1;

            }
        }

        public static void TosSetBasarisizSavunmaSayisi(Guid toUniqueId)
        {
            using (var entities = new LisansustuBasvuruSistemiEntities())
            {
                var toBasvuru = entities.ToBasvurus.First(p => p.UniqueID == toUniqueId);

                var qTosbasvurus = toBasvuru.ToBasvuruSavunmas.Where(p => p.ToBasvuruSavunmaDurumID.HasValue).OrderBy(s => s.ToBasvuruSavunmaID).AsQueryable();


                var lastRecordWithDurumId = qTosbasvurus.LastOrDefault(s => s.ToBasvuruSavunmaDurumID == 1);

                if (lastRecordWithDurumId != null)
                {
                    toBasvuru.BasarisizSavunmaSayisi = qTosbasvurus
                        .Count(s => s.ToBasvuruSavunmaID > lastRecordWithDurumId.ToBasvuruSavunmaID &&
                                    s.ToBasvuruSavunmaDurumID != 1);

                }
                else toBasvuru.BasarisizSavunmaSayisi = qTosbasvurus
                        .Count();

                entities.SaveChanges();

            }
        }

        public static MmMessage GetTosSilKontrol(Guid toBasvuruSavunmaUniqueId)
        {
            var msg = new MmMessage();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var tezOneriSavunma = db.ToBasvuruSavunmas.FirstOrDefault(f => f.UniqueID == toBasvuruSavunmaUniqueId);
                if (tezOneriSavunma == null)
                {
                    msg.Messages.Add("Tez Öneri Savunma Sınavı Bulunamadı.");
                }
                else
                {
                    var silYetki = RoleNames.TosGelenBasvuruSil.InRoleCurrent();
                    var isAdmin = UserIdentity.Current.IsAdmin;
                    if (!UserIdentity.Current.EnstituKods.Contains(tezOneriSavunma.ToBasvuru.EnstituKod) && silYetki && tezOneriSavunma.ToBasvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                    }
                    else if (!isAdmin && !TiAyar.TezOneriSavunmaBasvuruAlimiAcik.GetAyarTi(tezOneriSavunma.ToBasvuru.EnstituKod, "false").ToBoolean().Value && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.Messages.Add("Başvuru süreci kapalı olduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (!isAdmin && silYetki == false && tezOneriSavunma.ToBasvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                    }
                    else if (!isAdmin && tezOneriSavunma.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue))
                    {
                        msg.Messages.Add("Komite üyeleri tarafından değerlendirme yapıldıktan sonra Tez Öneri Savunma Sınavı silinemez!");
                    }
                }

                msg.IsSuccess = !msg.Messages.Any();
            }
            return msg;
        }
        public static TosBasvuruDetayDto GetSecilenBasvuruDetay(Guid toUniqueId, Guid? tosKomiteUniqueId)
        {
            var model = new TosBasvuruDetayDto();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var basvuru = db.ToBasvurus.First(p => p.UniqueID == toUniqueId);
                var enstitu = db.Enstitulers.First(p => p.EnstituKod == basvuru.EnstituKod);

                var eslesenDanisman = db.Kullanicilars.FirstOrDefault(p => p.KullaniciID == (basvuru.TezDanismanID ?? 0));
                if (eslesenDanisman != null)
                {
                    var unvanAdi = eslesenDanisman.Unvanlar != null ? eslesenDanisman.Unvanlar.UnvanAdi : "";
                    model.TezDanismanBilgiEslesen = unvanAdi + " " + eslesenDanisman.Ad + " " + eslesenDanisman.Soyad;
                }
                else
                {
                    model.TezDanismanBilgiEslesen = "Sistemde eşleşen tez danışmanı bulunamadı.";
                }

                model.UniqueID = basvuru.UniqueID;

                model.ToBasvuruSavunmaList = basvuru.ToBasvuruSavunmas.Where(p => !tosKomiteUniqueId.HasValue || p.ToBasvuruSavunmaKomites.Any(a => a.UniqueID == tosKomiteUniqueId))
                    .Select(s => new ToBasvuruSavunmaDto
                    {
                        UniqueID = s.UniqueID,
                        FormKodu = s.FormKodu,
                        ToBasvuruSavunmaID = s.ToBasvuruSavunmaID,
                        ToBasvuruID = s.ToBasvuruID,
                        SavunmaBasvuruTarihi = s.SavunmaBasvuruTarihi,
                        SavunmaNo = s.SavunmaNo,
                        IsTezDiliTr = s.IsTezDiliTr,
                        TezBaslikTr = s.TezBaslikTr,
                        TezBaslikEn = s.TezBaslikEn,
                        IsTezBasligiDegisti = s.IsTezBasligiDegisti,
                        YeniTezBaslikTr = s.YeniTezBaslikTr,
                        YeniTezBaslikEn = s.YeniTezBaslikEn,
                        CalismaRaporDosyaAdi = s.CalismaRaporDosyaAdi,
                        CalismaRaporDosyaYolu = s.CalismaRaporDosyaYolu,
                        DonemAdi = s.DonemBaslangicYil + "/" + (s.DonemBaslangicYil + 1) + " " + (s.DonemID == 1 ? "Güz" : "Bahar"),
                        IsYokDrBursiyeriVar = s.IsYokDrBursiyeriVar,
                        YokDrOncelikliAlan = s.YokDrOncelikliAlan,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        ToBasvuruSavunmaDurumID = s.ToBasvuruSavunmaDurumID,
                        DurumAdi = s.ToBasvuruSavunmaDurumID.HasValue ? s.ToBasvuruSavunmaDurumlari.DurumAdi : "Henüz Değerlendirme Tamamlanmadı",
                        IsOyBirligiOrCoklugu = s.IsOyBirligiOrCoklugu,
                        DegerlendirmeSonucMailTarihi = s.DegerlendirmeSonucMailTarihi,
                        ToplantiBilgiGonderimTarihi = s.ToplantiBilgiGonderimTarihi,
                        ToSavunmaBaslatildiMailGonderimTarihi = s.ToSavunmaBaslatildiMailGonderimTarihi,
                        DurumModel = new TosDurumDto
                        {
                            IsTezOnerisiVar = true,
                            ToBasvuruSavunmaDurumID = s.ToBasvuruSavunmaDurumID,
                            IsSrTalebiYapildi = s.SRTalepleris.Any(),
                            DegerlendirmeBasladi = s.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue),
                            IsOyBirligiOrCoklugu = s.IsOyBirligiOrCoklugu
                        },

                        ToBasvuruSavunmaKomites = db.ToBasvuruSavunmaKomites.Where(p => p.ToBasvuruSavunmaID == s.ToBasvuruSavunmaID).Include("ToBasvuruSavunmaDurumlari").ToList(),
                        //ToBasvuruSavunmaKomites = s.ToBasvuruSavunmaKomites.AsQueryable().Include("ToBasvuruSavunmaDurumlari").ToList(),
                        SRModel = (from sR in s.SRTalepleris
                                   join tt in db.SRTalepTipleris on sR.SRTalepTipID equals tt.SRTalepTipID
                                   join hg in db.HaftaGunleris on sR.HaftaGunID equals hg.HaftaGunID
                                   join d in db.SRDurumlaris on sR.SRDurumID equals d.SRDurumID
                                   select new FrTalepler
                                   {
                                       SRTalepID = sR.SRTalepID,
                                       TalepYapanID = sR.TalepYapanID,
                                       TalepTipAdi = tt.TalepTipAdi,
                                       SRTalepTipID = sR.SRTalepTipID,
                                       SRSalonID = sR.SRSalonID,
                                       IsOnline = sR.IsOnline,
                                       SalonAdi = sR.SalonAdi,
                                       Tarih = sR.Tarih,
                                       HaftaGunID = sR.HaftaGunID,
                                       HaftaGunAdi = hg.HaftaGunAdi,
                                       BasSaat = sR.BasSaat,
                                       BitSaat = sR.BitSaat,
                                       SRDurumID = sR.SRDurumID,
                                       DurumAdi = d.DurumAdi,
                                       DurumListeAdi = d.DurumAdi,
                                       ClassName = d.ClassName,
                                       Color = d.Color,
                                       SRDurumAciklamasi = sR.SRDurumAciklamasi,
                                       IslemTarihi = s.IslemTarihi,
                                       IslemYapanID = s.IslemYapanID,
                                       IslemYapanIP = s.IslemYapanIP
                                   }).FirstOrDefault()
                    }).OrderByDescending(o => o.SavunmaBasvuruTarihi).ToList();

                var sonTos = model.ToBasvuruSavunmaList.FirstOrDefault();
                model.DurumHtmlString = (
                                            sonTos != null ? sonTos.DurumModel : new TosDurumDto()
                                        ).TosBasvuruDurumView().ToString();
                model.DonemHtmlString = (sonTos ?? new ToBasvuruSavunmaDto()).TosBasvuruDonemView().ToString();




                model.TezDanismanID = basvuru.TezDanismanID;
                model.ToBasvuruID = basvuru.ToBasvuruID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.YeterlikSozluSinavTarihi = basvuru.YeterlikSozluSinavTarihi;

                model.ToplamBasarisizTezOneriSavunmaHak = TiAyar.TezOneriToplamBasarisizTezOneriSavunmaHak.GetAyarTi(basvuru.EnstituKod).ToInt();
                model.IlkSavunmaHakkiAyKriter = TiAyar.TezOneriIlkSavunmaHakkiAyKriter.GetAyarTi(basvuru.EnstituKod).ToInt();
                model.IkinciSavunmaHakkiAyKriter = TiAyar.TezOneriIkinciSavunmaHakkiAyKriter.GetAyarTi(basvuru.EnstituKod).ToInt();
                model.BasarisizSavunmaSayisi = basvuru.BasarisizSavunmaSayisi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;
                model.ResimAdi = basvuru.Kullanicilar.ResimAdi;
                model.Ad = basvuru.Kullanicilar.Ad;
                model.Soyad = basvuru.Kullanicilar.Soyad;
                model.TcKimlikNo = basvuru.Kullanicilar.TcKimlikNo;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = db.OgrenimTipleris.First(p => p.EnstituKod == basvuru.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).OgrenimTipAdi;

                model.AnabilimdaliAdi = basvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = basvuru.Programlar.ProgramAdi;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
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
                model.DegerlendirenUniqueID = tosKomiteUniqueId;



            }
            return model;

        }



        public static MmMessage SendMailTosBilgisi(int? toBasvuruSavunmaID, int? srTalepId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {

                    var toBasvuruSavunma = new ToBasvuruSavunma();
                    var srTalebi = new SRTalepleri();

                    if (toBasvuruSavunmaID.HasValue)
                    {
                        toBasvuruSavunma = db.ToBasvuruSavunmas.First(p => p.ToBasvuruSavunmaID == toBasvuruSavunmaID);
                        if (srTalepId.HasValue) srTalebi = toBasvuruSavunma.SRTalepleris.FirstOrDefault();
                    }
                    else if (srTalepId.HasValue)
                    {
                        srTalebi = db.SRTalepleris.First(p => p.SRTalepID == srTalepId);
                        toBasvuruSavunma = srTalebi.ToBasvuruSavunma;
                    }

                    var juriler = toBasvuruSavunma.ToBasvuruSavunmaKomites.ToList();
                    var sablonTipIDs = new List<int>();
                    var mModel = new List<SablonMailModel>();
                    var danisman = juriler.First(p => p.IsTezDanismani);
                    var isSavunmaOrToplanti = false;
                    var gonderilenMEkleris = new List<GonderilenMailEkleri>
                    {
                        new GonderilenMailEkleri
                        {
                            EkAdi = toBasvuruSavunma.CalismaRaporDosyaAdi,
                            EkDosyaYolu = toBasvuruSavunma.CalismaRaporDosyaYolu,
                        }
                    };
                    var kul = toBasvuruSavunma.ToBasvuru.Kullanicilar;
                    if (toBasvuruSavunmaID.HasValue)
                    {
                        isSavunmaOrToplanti = true;
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipi.Tos_BaslatildiOgrenci, MailSablonTipi.Tos_BaslatildiDanisman });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "TezDanismani",
                            UnvanAdi = danisman.UnvanAdi,
                            AdSoyad = danisman.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.Tos_BaslatildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci " + kul.Ad + " " + kul.Soyad,
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = toBasvuruSavunma.ToBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.Tos_BaslatildiOgrenci
                        });
                    }
                    if (srTalepId.HasValue)
                    {
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipi.Tos_ToplantiBilgiKomite, MailSablonTipi.Tos_ToplantiBilgiOgrenci });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci " + kul.Ad + " " + kul.Soyad,
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = toBasvuruSavunma.ToBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.Tos_ToplantiBilgiOgrenci
                        });
                        mModel.AddRange(juriler.Select(item => new SablonMailModel
                        {
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.Tos_ToplantiBilgiKomite,
                            JuriTipAdi = item.IsTezDanismani ? "Tez Danışmanı" : "Komite Üyesi"
                        }));
                    }

                    var enstitu = toBasvuruSavunma.ToBasvuru.Enstituler;

                    var sablonlar = db.MailSablonlaris.Where(p => sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var abdL = toBasvuruSavunma.ToBasvuru.Programlar.AnabilimDallari;
                    var prgL = toBasvuruSavunma.ToBasvuru.Programlar;
                    var oncekiMailTarihi = isSavunmaOrToplanti ? toBasvuruSavunma.ToSavunmaBaslatildiMailGonderimTarihi : toBasvuruSavunma.ToplantiBilgiGonderimTarihi;

                    var isSended = false;
                    foreach (var item in mModel)
                    {
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        gonderilenMEkleris.AddRange(item.Sablon.MailSablonlariEkleris.Select(itemSe => new GonderilenMailEkleri { EkAdi = itemSe.EkAdi, EkDosyaYolu = itemSe.EkDosyaYolu, }));
                        foreach (var itemEk in gonderilenMEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemEk.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemEk.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));

                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemEk.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailTIToplantiBilgisi", LogType.Uyarı);
                        }
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = kul.Ad + " " + kul.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = toBasvuruSavunma.ToBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikTr", Value = toBasvuruSavunma.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikEn", Value = toBasvuruSavunma.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YokDrBursiyeriBilgi", Value = toBasvuruSavunma.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + toBasvuruSavunma.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var donemBilgi = toBasvuruSavunma.SavunmaBasvuruTarihi.ToAraRaporDonemBilgi();
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DonemAdi", Value = donemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OncekiMailTarihi", Value = oncekiMailTarihi?.ToFormatDateAndTime() });
                        }
                        #region SR Talebi
                        if (item.MailSablonTipID == MailSablonTipi.Tos_ToplantiBilgiKomite || item.MailSablonTipID == MailSablonTipi.Tos_ToplantiBilgiOgrenci)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiTarihi"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiTarihi", Value = srTalebi.Tarih.ToLongDateString() });
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiSaati"))
                                paramereDegerleri.Add(new MailReplaceParameterDto
                                {
                                    Key = "ToplantiSaati",
                                    Value = $"{srTalebi.BasSaat:hh\\:mm}"
                                });

                            if (!srTalebi.IsOnline)
                            {
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiSekli"))
                                {
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiSekli", Value = "Yüz Yüze" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeriBaslik"))
                                {
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiYeriBaslik", Value = "Toplantı Salonu" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeri"))
                                {
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiYeri", Value = srTalebi.SalonAdi });
                                }
                            }
                            else
                            {
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiSekli"))
                                {
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiSekli", Value = "Çevrim İçi" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeriBaslik"))
                                {
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiYeriBaslik", Value = "Toplantı Katılım Linki" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeri"))
                                {
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiYeri", Value = srTalebi.SalonAdi, IsLink = true });
                                }
                            }
                        }
                        #endregion
                        #region DanismanKomite
                        if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanBilgi", Value = danisman.UnvanAdi + " " + danisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUni"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUni", Value = danisman.UniversiteAdi });
                        }
                        foreach (var itemTik in juriler.Where(p => !p.IsTezDanismani).Select((s, inx) => new { s, inx = inx + 1 }))
                        {

                            if (item.SablonParametreleri.Any(a => a == "@TikBilgi" + itemTik.inx))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TikBilgi" + itemTik.inx, Value = itemTik.s.UnvanAdi + " " + itemTik.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@TikBilgiUni" + itemTik.inx))
                            {
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TikBilgiUni" + itemTik.inx, Value = itemTik.s.UniversiteAdi });
                            }
                        }
                        #endregion

                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;
                        isSended = true;
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = enstitu.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = mCOntent.Title
                        };
                        if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                        if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                        kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                        kModel.IslemYapanIP = UserIdentity.Ip;
                        kModel.Aciklama = item.Sablon.Sablon ?? "";
                        kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                        kModel.Gonderildi = true;
                        foreach (var itemGk in item.EMails)
                        {
                            kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                        }
                        foreach (var itemMe in gonderilenMEkleris)
                        {
                            kModel.GonderilenMailEkleris.Add(itemMe);
                        }
                        db.GonderilenMaillers.Add(kModel);
                        if (isSavunmaOrToplanti) toBasvuruSavunma.ToSavunmaBaslatildiMailGonderimTarihi = DateTime.Now;
                        else toBasvuruSavunma.ToplantiBilgiGonderimTarihi = DateTime.Now;

                        LogIslemleri.LogEkle("ToBasvuruSavunma", IslemTipi.Update, toBasvuruSavunma.ToJson());
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez Öneri Savunma toplantısı için Komite üyelerine mail gönderilirken bir hata oluştu! \r\nSRTalepID:" + srTalepId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "TezOneriSavunmaBus/SendMailTosBilgisi \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailTosDegerlendirmeLink(Guid toBasvuruSavunmaId, Guid? tosKomiteUniqueId, bool isLinkOrSonuc)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var toBasvuruSavunma = db.ToBasvuruSavunmas.First(p => p.UniqueID == toBasvuruSavunmaId);
                    var qJurilers = toBasvuruSavunma.ToBasvuruSavunmaKomites.AsQueryable();
                    if (isLinkOrSonuc)
                    {
                        if (tosKomiteUniqueId.HasValue) qJurilers = qJurilers.Where(p => p.UniqueID == (tosKomiteUniqueId ?? p.UniqueID));
                        else qJurilers = qJurilers.Where(p => !p.IsTezDanismani);
                    }
                    else
                    {
                        if (tosKomiteUniqueId.HasValue) qJurilers = qJurilers.Where(p => p.UniqueID == tosKomiteUniqueId);
                        else qJurilers = qJurilers.Where(p => p.IsTezDanismani);
                    }
                    var juriler = qJurilers.ToList();

                    var mModel = new List<SablonMailModel>();

                    var enstitu = toBasvuruSavunma.ToBasvuru.Enstituler;

                    var abdL = toBasvuruSavunma.ToBasvuru.Programlar.AnabilimDallari;
                    var prgL = toBasvuruSavunma.ToBasvuru.Programlar;
                    var kul = toBasvuruSavunma.ToBasvuru.Kullanicilar;
                    if (isLinkOrSonuc)
                    {
                        foreach (var item in juriler)
                        {
                            item.UniqueID = Guid.NewGuid();
                        }
                    }
                    else
                    {
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci " + kul.Ad + " " + kul.Soyad,
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = toBasvuruSavunma.ToBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.Tos_DegerlendirmeSonucGonderimOgrenci,
                        });
                    }

                    mModel.AddRange(juriler.Select(item => new SablonMailModel
                    {
                        UniqueID = item.UniqueID,
                        UnvanAdi = item.UnvanAdi,
                        AdSoyad = item.AdSoyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                        MailSablonTipID = isLinkOrSonuc ? MailSablonTipi.Tos_DegerlendirmeLinkGonderimKomite : MailSablonTipi.Tos_DegerlendirmeSonucGonderimDanisman,
                        JuriTipAdi = item.IsTezDanismani ? "Tez Danışmanı" : "Komite Üyesi",
                    }));
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {

                        var juri = juriler.FirstOrDefault(p => p.UniqueID == item.UniqueID);
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        var gonderilenMailEkleri = new List<GonderilenMailEkleri>();

                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMailEkleri.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailTIToplantiBilgisi", LogType.Uyarı);
                        }
                        if (false && !isLinkOrSonuc)
                        {
                            var ds = new List<int?>() { toBasvuruSavunma.ToBasvuruSavunmaID };
                            if (item.MailSablonTipID == MailSablonTipi.Tos_DegerlendirmeSonucGonderimDanisman) ds.Add(1);
                            var ekler = Management.exportRaporPdf(RaporTipleri.TezIzlemeDegerlendirmeFormu, ds);
                            gonderilenMailEkleri.AddRange(ekler.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }));
                            item.Attachments.AddRange(ekler);
                        }

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = kul.Ad + " " + kul.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = toBasvuruSavunma.ToBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikTr", Value = toBasvuruSavunma.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikEn", Value = toBasvuruSavunma.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YokDrBursiyeriBilgi", Value = toBasvuruSavunma.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + toBasvuruSavunma.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var donemBilgi = toBasvuruSavunma.SavunmaBasvuruTarihi.ToAraRaporDonemBilgi();
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DonemAdi", Value = donemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/ToBasvuru/Index?IsDegerlendirme=" + item.UniqueID, IsLink = true });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {
                            paramereDegerleri.Add(isLinkOrSonuc
                                ? new MailReplaceParameterDto
                                {
                                    Key = "OncekiMailTarihi",
                                    Value = juri.LinkGonderimTarihi?.ToFormatDateAndTime()
                                }
                                : new MailReplaceParameterDto
                                {
                                    Key = "OncekiMailTarihi",
                                    Value = toBasvuruSavunma.DegerlendirmeSonucMailTarihi?.ToFormatDateAndTime()
                                });
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = enstitu.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = mCOntent.Title
                        };
                        if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                        if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                        kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                        kModel.IslemYapanIP = UserIdentity.Ip;
                        kModel.Aciklama = item.Sablon.Sablon ?? "";
                        kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                        kModel.Gonderildi = true;
                        foreach (var itemGk in item.EMails)
                        {
                            kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                        }
                        foreach (var itemMe in gonderilenMailEkleri)
                        {
                            kModel.GonderilenMailEkleris.Add(itemMe);
                        }
                        if (isLinkOrSonuc)
                        {
                            juri.DegerlendirmeIslemTarihi = null;
                            juri.DegerlendirmeIslemYapanIP = null;
                            juri.DegerlendirmeYapanID = null;
                            juri.ToBasvuruSavunmaDurumID = null;
                            juri.IsCalismaRaporuAltAlanUygun = null;
                            juri.Aciklama = null;
                            juri.IsLinkGonderildi = true;
                            juri.LinkGonderimTarihi = DateTime.Now;
                            juri.LinkGonderenID = UserIdentity.Current.Id;

                        }

                        db.GonderilenMaillers.Add(kModel);
                        db.SaveChanges();
                        if (isLinkOrSonuc) LogIslemleri.LogEkle("ToBasvuruSavunmaKomite", IslemTipi.Update, juri.ToJson());
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "";
                message = isLinkOrSonuc ? "Tez Öneri Savunma değerlendirmesi için Komite üyelerine değerlendirme davetiye linki mail olarak gönderilirken bir hata oluştu!" : "Tez Öneri Savunma değerlendirmesi sonucu Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "TezOneriSavunmaBus/SendMailTosDegerlendirmeLink \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }


        public static List<CmbIntDto> CmbTosNumarasi(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            for (int i = 1; i <= 2; i++)
            {
                dct.Add(new CmbIntDto { Value = i, Caption = i + ". Savunma" });
            }

            return dct;

        }
        public static List<CmbIntDto> CmbTosDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();

            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });


            dct.Add(new CmbIntDto { Value = 1000, Caption = "Sınav Bilgisi Girilmeyenler" });
            dct.Add(new CmbIntDto { Value = 1001, Caption = "Sınav Bilgisi Girilenler" });
            dct.Add(new CmbIntDto { Value = 1002, Caption = "Değerlendirme Sürecinde Olanlar" });
            dct.Add(new CmbIntDto { Value = 1003, Caption = "Değerlendirme Sürecinde Tamamlananlar" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Başarılı Olanlar" });
            dct.Add(new CmbIntDto { Value = 2, Caption = "Başarısız Olanlar" });
            dct.Add(new CmbIntDto { Value = 3, Caption = "Uzatma Alanlar" });
            return dct;
        }

        public static List<CmbStringDto> CmbDonemListe(bool bosSecimVar = false)
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var donems = db.ToBasvuruSavunmas.Select(s => new { s.DonemBaslangicYil, s.DonemID, s.Donemler.DonemAdi })
                    .Distinct().OrderByDescending(o => o.DonemBaslangicYil).ThenByDescending(t => t.DonemID).Select(s => new CmbStringDto
                    {
                        Value = s.DonemBaslangicYil + "" + s.DonemID,
                        Caption = s.DonemBaslangicYil + "/" + (s.DonemBaslangicYil + 1) + " " + s.DonemAdi

                    }).ToList();
                if (bosSecimVar) donems.Insert(0, new CmbStringDto { Value = null, Caption = "" });
                return donems;
            }
        }

        public static List<CmbStringDto> CmbDonemListeBasvuru(bool bosSecimVar = false)
        {
            var cmbDonems = CmbDonemListe(false);
            if (!cmbDonems.Any())
            {
                var donem = DateTime.Now.ToAraRaporDonemBilgi();
                cmbDonems.Add(new CmbStringDto()
                {
                    Value = donem.BaslangicYil + "" + donem.DonemID,
                    Caption = donem.BaslangicYil + "/" + (donem.BaslangicYil + 1) + " " + donem.DonemAdi
                });
                if (bosSecimVar) cmbDonems.Insert(0, new CmbStringDto { Value = null, Caption = "" });
            }

            return cmbDonems;
        }

        public static List<CmbIntDto> GetCmbFilterAnabilimDallari(string enstituKod, bool bosSecimVar = false)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var anabilimDaliIds = db.ToBasvurus
                    .Where(p => p.EnstituKod == enstituKod).Select(s => s.Programlar.AnabilimDaliID).Distinct().ToList();

                var anabilimDallaris = db.AnabilimDallaris.Where(p => anabilimDaliIds.Contains(p.AnabilimDaliID))
                    .Select(s => new { s.AnabilimDaliID, s.AnabilimDaliAdi }).OrderBy(o => o.AnabilimDaliAdi).Select(
                        s =>
                            new CmbIntDto { Value = s.AnabilimDaliID, Caption = s.AnabilimDaliAdi }
                        ).ToList();
                if (bosSecimVar) anabilimDallaris.Insert(0, new CmbIntDto { Value = null, Caption = "" });

                return anabilimDallaris;
            }
        }
    }
}