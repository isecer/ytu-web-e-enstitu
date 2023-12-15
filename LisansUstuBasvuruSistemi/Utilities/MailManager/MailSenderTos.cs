using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;

namespace LisansUstuBasvuruSistemi.Utilities.MailManager
{
    public class MailSenderTos
    {
        public static MmMessage SendMailTosBilgisi(int? toBasvuruSavunmaId, int? srTalepId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {

                    var toBasvuruSavunma = new ToBasvuruSavunma();
                    var srTalebi = new SRTalepleri();

                    if (toBasvuruSavunmaId.HasValue)
                    {
                        toBasvuruSavunma = db.ToBasvuruSavunmas.First(p => p.ToBasvuruSavunmaID == toBasvuruSavunmaId);
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
                    if (toBasvuruSavunmaId.HasValue)
                    {
                        isSavunmaOrToplanti = true;
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipiEnum.TosBaslatildiOgrenci, MailSablonTipiEnum.TosBaslatildiDanisman });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "TezDanismani",
                            UnvanAdi = danisman.UnvanAdi,
                            AdSoyad = danisman.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TosBaslatildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci " + kul.Ad + " " + kul.Soyad,
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = toBasvuruSavunma.ToBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TosBaslatildiOgrenci
                        });
                    }
                    if (srTalepId.HasValue)
                    {
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipiEnum.TosToplantiBilgiKomite, MailSablonTipiEnum.TosToplantiBilgiOgrenci });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci " + kul.Ad + " " + kul.Soyad,
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = toBasvuruSavunma.ToBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TosToplantiBilgiOgrenci
                        });
                        mModel.AddRange(juriler.Select(item => new SablonMailModel
                        {
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TosToplantiBilgiKomite,
                            JuriTipAdi = item.IsTezDanismani ? "Tez Danışmanı" : "Komite Üyesi"
                        }));
                    }

                    var enstitu = toBasvuruSavunma.ToBasvuru.Enstituler;

                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var abdL = toBasvuruSavunma.ToBasvuru.Programlar.AnabilimDallari;
                    var prgL = toBasvuruSavunma.ToBasvuru.Programlar;
                    var oncekiMailTarihi = isSavunmaOrToplanti ? toBasvuruSavunma.ToSavunmaBaslatildiMailGonderimTarihi : toBasvuruSavunma.ToplantiBilgiGonderimTarihi;

                    var donemAdi = (toBasvuruSavunma.DonemBaslangicYil + " - " + (toBasvuruSavunma.DonemBaslangicYil + 1) + " " + toBasvuruSavunma.Donemler.DonemAdi);
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
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemEk.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Uyarı);
                        }
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = kul.Ad + " " + kul.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = toBasvuruSavunma.ToBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = toBasvuruSavunma.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = toBasvuruSavunma.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YokDrBursiyeriBilgi", Value = toBasvuruSavunma.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + toBasvuruSavunma.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = donemAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "OncekiMailTarihi", Value = oncekiMailTarihi?.ToFormatDateAndTime() });
                        }
                        #region SR Talebi
                        if (item.MailSablonTipID == MailSablonTipiEnum.TosToplantiBilgiKomite || item.MailSablonTipID == MailSablonTipiEnum.TosToplantiBilgiOgrenci)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiTarihi"))
                                mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiTarihi", Value = srTalebi.Tarih.ToLongDateString() });
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiSaati"))
                                mailParameterDtos.Add(new MailParameterDto
                                {
                                    Key = "ToplantiSaati",
                                    Value = $"{srTalebi.BasSaat:hh\\:mm}"
                                });

                            if (!srTalebi.IsOnline)
                            {
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiSekli"))
                                {
                                    mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiSekli", Value = "Yüz Yüze" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeriBaslik"))
                                {
                                    mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiYeriBaslik", Value = "Toplantı Salonu" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeri"))
                                {
                                    mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiYeri", Value = srTalebi.SalonAdi });
                                }
                            }
                            else
                            {
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiSekli"))
                                {
                                    mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiSekli", Value = "Çevrim İçi" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeriBaslik"))
                                {
                                    mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiYeriBaslik", Value = "Toplantı Katılım Linki" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeri"))
                                {
                                    mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiYeri", Value = srTalebi.SalonAdi, IsLink = true });
                                }
                            }
                        }
                        #endregion
                        #region DanismanKomite
                        if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanBilgi", Value = danisman.UnvanAdi + " " + danisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUni"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUni", Value = danisman.UniversiteAdi });
                        }
                        foreach (var itemTik in juriler.Where(p => !p.IsTezDanismani).Select((s, inx) => new { s, inx = inx + 1 }))
                        {

                            if (item.SablonParametreleri.Any(a => a == "@TikBilgi" + itemTik.inx))
                                mailParameterDtos.Add(new MailParameterDto { Key = "TikBilgi" + itemTik.inx, Value = itemTik.s.UnvanAdi + " " + itemTik.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@TikBilgiUni" + itemTik.inx))
                            {
                                mailParameterDtos.Add(new MailParameterDto { Key = "TikBilgiUni" + itemTik.inx, Value = itemTik.s.UniversiteAdi });
                            }
                        }
                        #endregion

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;
                        isSended = true;
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = enstitu.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title
                        };
                        if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                        if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                        kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                        kModel.IslemYapanIP = UserIdentity.Ip;
                        kModel.Aciklama = item.Sablon.Sablon ?? "";
                        kModel.AciklamaHtml = contentDetailDto.HtmlContent ?? "";
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

                        LogIslemleri.LogEkle("ToBasvuruSavunma", LogCrudType.Update, toBasvuruSavunma.ToJson());
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez Öneri Savunma toplantısı için Komite üyelerine mail gönderilirken bir hata oluştu! \r\nSRTalepID:" + srTalepId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
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
                            MailSablonTipID = MailSablonTipiEnum.TosDegerlendirmeSonucGonderimOgrenci,
                        });
                    }

                    mModel.AddRange(juriler.Select(item => new SablonMailModel
                    {
                        UniqueID = item.UniqueID,
                        UnvanAdi = item.UnvanAdi,
                        AdSoyad = item.AdSoyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                        MailSablonTipID = isLinkOrSonuc ? MailSablonTipiEnum.TosDegerlendirmeLinkGonderimKomite : MailSablonTipiEnum.TosDegerlendirmeSonucGonderimDanisman,
                        JuriTipAdi = item.IsTezDanismani ? "Tez Danışmanı" : "Komite Üyesi",
                    }));
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    var donemAdi = (toBasvuruSavunma.DonemBaslangicYil + " - " + (toBasvuruSavunma.DonemBaslangicYil + 1) + " " + toBasvuruSavunma.Donemler.DonemAdi);
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
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Uyarı);
                        }
                        if (!isLinkOrSonuc)
                        {
                            var ds = new List<int?>() { toBasvuruSavunma.ToBasvuruSavunmaID };
                            if (item.MailSablonTipID == MailSablonTipiEnum.TosDegerlendirmeSonucGonderimDanisman) ds.Add(1);
                            var ekler = Management.ExportRaporPdf(RaporTipiEnum.TezOneriSavunmaFormu, ds);
                            gonderilenMailEkleri.AddRange(ekler.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }));
                            item.Attachments.AddRange(ekler);
                        }

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = kul.Ad + " " + kul.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = toBasvuruSavunma.ToBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = toBasvuruSavunma.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = toBasvuruSavunma.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YokDrBursiyeriBilgi", Value = toBasvuruSavunma.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + toBasvuruSavunma.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = donemAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TosBasvuru/Index?IsDegerlendirme=" + item.UniqueID, IsLink = true });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {
                            mailParameterDtos.Add(isLinkOrSonuc
                                ? new MailParameterDto
                                {
                                    Key = "OncekiMailTarihi",
                                    Value = juri.LinkGonderimTarihi?.ToFormatDateAndTime()
                                }
                                : new MailParameterDto
                                {
                                    Key = "OncekiMailTarihi",
                                    Value = toBasvuruSavunma.DegerlendirmeSonucMailTarihi?.ToFormatDateAndTime()
                                });
                        }
                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = enstitu.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title
                        };
                        if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                        if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                        kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                        kModel.IslemYapanIP = UserIdentity.Ip;
                        kModel.Aciklama = item.Sablon.Sablon ?? "";
                        kModel.AciklamaHtml = contentDetailDto.HtmlContent ?? "";
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
                        if (isLinkOrSonuc) LogIslemleri.LogEkle("ToBasvuruSavunmaKomite", LogCrudType.Update, juri.ToJson());
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "";
                message = isLinkOrSonuc ? "Tez Öneri Savunması değerlendirmesi için Komite üyelerine değerlendirme davetiye linki mail olarak gönderilirken bir hata oluştu!" : "Tez Öneri Savunması değerlendirme sonucu Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(),  ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
    }
}