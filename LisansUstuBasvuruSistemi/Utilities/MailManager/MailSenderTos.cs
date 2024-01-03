using System;
using System.Collections.Generic;
using System.Linq;
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
                using (var entities = new LisansustuBasvuruSistemiEntities())
                {

                    var toBasvuruSavunma = new ToBasvuruSavunma();
                    var srTalebi = new SRTalepleri();

                    if (toBasvuruSavunmaId.HasValue)
                    {
                        toBasvuruSavunma = entities.ToBasvuruSavunmas.First(p => p.ToBasvuruSavunmaID == toBasvuruSavunmaId);
                        if (srTalepId.HasValue) srTalebi = toBasvuruSavunma.SRTalepleris.FirstOrDefault();
                    }
                    else if (srTalepId.HasValue)
                    {
                        srTalebi = entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);
                        toBasvuruSavunma = srTalebi.ToBasvuruSavunma;
                    }

                    var juriler = toBasvuruSavunma.ToBasvuruSavunmaKomites.ToList();
                    var danisman = juriler.First(p => p.IsTezDanismani);
                    var isSavunmaOrToplanti = false;

                    var ogrenci = toBasvuruSavunma.ToBasvuru.Kullanicilar;

                    var mModel = new List<SablonMailModel>();
                    if (toBasvuruSavunmaId.HasValue)
                    {
                        isSavunmaOrToplanti = true;
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "TezDanismani",
                            UnvanAdi = danisman.UnvanAdi,
                            AdSoyad = danisman.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = toBasvuruSavunma.TezDanismanID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TosBaslatildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TosBaslatildiOgrenci
                        });
                    }
                    if (srTalepId.HasValue)
                    {
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TosToplantiBilgiOgrenci
                        });
                        mModel.AddRange(juriler.Select(item => new SablonMailModel
                        {
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = item.IsTezDanismani ? toBasvuruSavunma.TezDanismanID : null, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TosToplantiBilgiKomite,
                            JuriTipAdi = item.IsTezDanismani ? "Tez Danışmanı" : "Komite Üyesi"
                        }));
                    }

                    var enstitu = toBasvuruSavunma.ToBasvuru.Enstituler;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var abdL = toBasvuruSavunma.ToBasvuru.Programlar.AnabilimDallari;
                    var prgL = toBasvuruSavunma.ToBasvuru.Programlar;
                    var oncekiMailTarihi = isSavunmaOrToplanti ? toBasvuruSavunma.ToSavunmaBaslatildiMailGonderimTarihi : toBasvuruSavunma.ToplantiBilgiGonderimTarihi;

                    var donemAdi = (toBasvuruSavunma.DonemBaslangicYil + " - " + (toBasvuruSavunma.DonemBaslangicYil + 1) + " " + toBasvuruSavunma.Donemler.DonemAdi);
                    var snded = false;
                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);

                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);
                        item.SablonEkleri.Add(new MailSablonlariEkleri { EkAdi = ogrenci.Ad + " " + ogrenci.Soyad + " Tez Öneri Savunma Çalışma Raporu Dosyası", EkDosyaYolu = toBasvuruSavunma.CalismaRaporDosyaYolu });


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = toBasvuruSavunma.ToBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = toBasvuruSavunma.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = toBasvuruSavunma.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YokDrBursiyeriBilgi", Value = toBasvuruSavunma.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + toBasvuruSavunma.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = donemAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OncekiMailTarihi", Value = oncekiMailTarihi?.ToFormatDateAndTime() });
                        }
                        #region SR Talebi
                        if (item.MailSablonTipId == MailSablonTipiEnum.TosToplantiBilgiKomite || item.MailSablonTipId == MailSablonTipiEnum.TosToplantiBilgiOgrenci)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiTarihi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "ToplantiTarihi", Value = srTalebi.Tarih.ToLongDateString() });
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiSaati"))
                                item.MailParameterDtos.Add(new MailParameterDto
                                {
                                    Key = "ToplantiSaati",
                                    Value = $"{srTalebi.BasSaat:hh\\:mm}"
                                });

                            if (!srTalebi.IsOnline)
                            {
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiSekli"))
                                {
                                    item.MailParameterDtos.Add(new MailParameterDto { Key = "ToplantiSekli", Value = "Yüz Yüze" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeriBaslik"))
                                {
                                    item.MailParameterDtos.Add(new MailParameterDto { Key = "ToplantiYeriBaslik", Value = "Toplantı Salonu" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeri"))
                                {
                                    item.MailParameterDtos.Add(new MailParameterDto { Key = "ToplantiYeri", Value = srTalebi.SalonAdi });
                                }
                            }
                            else
                            {
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiSekli"))
                                {
                                    item.MailParameterDtos.Add(new MailParameterDto { Key = "ToplantiSekli", Value = "Çevrim İçi" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeriBaslik"))
                                {
                                    item.MailParameterDtos.Add(new MailParameterDto { Key = "ToplantiYeriBaslik", Value = "Toplantı Katılım Linki" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeri"))
                                {
                                    item.MailParameterDtos.Add(new MailParameterDto { Key = "ToplantiYeri", Value = srTalebi.SalonAdi, IsLink = true });
                                }
                            }
                        }
                        #endregion
                        #region DanismanKomite
                        if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanBilgi", Value = danisman.UnvanAdi + " " + danisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUni"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUni", Value = danisman.UniversiteAdi });
                        }
                        foreach (var itemTik in juriler.Where(p => !p.IsTezDanismani).Select((s, inx) => new { s, inx = inx + 1 }))
                        {

                            if (item.SablonParametreleri.Any(a => a == "@TikBilgi" + itemTik.inx))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "TikBilgi" + itemTik.inx, Value = itemTik.s.UnvanAdi + " " + itemTik.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@TikBilgiUni" + itemTik.inx))
                            {
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "TikBilgiUni" + itemTik.inx, Value = itemTik.s.UniversiteAdi });
                            }
                        }
                        #endregion

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;
                        if (!item.AdSoyad.IsNullOrWhiteSpace()) contentDetailDto.Title += " (" + item.AdSoyad + ")";
                        if (!item.JuriTipAdi.IsNullOrWhiteSpace()) contentDetailDto.Title += " (" + item.JuriTipAdi + ")";
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = enstitu.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title,
                            IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id,
                            IslemYapanIP = UserIdentity.Ip,
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailEkleris = item.GetGonderilenMailEkleris,
                            GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris
                        };
                        entities.GonderilenMaillers.Add(kModel);


                    }
                    if (snded)
                    {
                        entities.SaveChanges();
                        if (isSavunmaOrToplanti) toBasvuruSavunma.ToSavunmaBaslatildiMailGonderimTarihi = DateTime.Now;
                        else toBasvuruSavunma.ToplantiBilgiGonderimTarihi = DateTime.Now;
                        LogIslemleri.LogEkle("ToBasvuruSavunma", LogCrudType.Update, toBasvuruSavunma.ToJson());
                    }
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez Öneri Savunma toplantısı için Komite üyelerine mail gönderilirken bir hata oluştu!";
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
                        qJurilers = tosKomiteUniqueId.HasValue ? qJurilers.Where(p => !tosKomiteUniqueId.HasValue || p.UniqueID == tosKomiteUniqueId) : qJurilers.Where(p => !p.IsTezDanismani);
                    }
                    else
                    {
                        qJurilers = tosKomiteUniqueId.HasValue ? qJurilers.Where(p => p.UniqueID == tosKomiteUniqueId) : qJurilers.Where(p => p.IsTezDanismani);
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

                            JuriTipAdi = "Öğrenci ",
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = kul.EMail, KullaniciId = kul.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TosDegerlendirmeSonucGonderimOgrenci,
                        });
                    }

                    mModel.AddRange(juriler.Select(item => new SablonMailModel
                    {
                        UniqueId = item.UniqueID,
                        UnvanAdi = item.UnvanAdi,
                        AdSoyad = item.AdSoyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = item.IsTezDanismani ? toBasvuruSavunma.TezDanismanID : null, ToOrBcc = true } },
                        MailSablonTipId = isLinkOrSonuc ? MailSablonTipiEnum.TosDegerlendirmeLinkGonderimKomite : MailSablonTipiEnum.TosDegerlendirmeSonucGonderimDanisman,
                        JuriTipAdi = item.IsTezDanismani ? "Tez Danışmanı" : "Komite Üyesi",
                    }));
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    var donemAdi = (toBasvuruSavunma.DonemBaslangicYil + " - " + (toBasvuruSavunma.DonemBaslangicYil + 1) + " " + toBasvuruSavunma.Donemler.DonemAdi);
                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;

                        var juri = juriler.FirstOrDefault(p => p.UniqueID == item.UniqueId);
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;

                        if (!isLinkOrSonuc)
                        {
                            var showSavunmaDetay = item.MailSablonTipId == MailSablonTipiEnum.TosDegerlendirmeSonucGonderimDanisman;
                            item.Attachments.AddRange(MailReportAttachment.GetTezOneriSavunmaFormuAttachments(toBasvuruSavunma.ToBasvuruSavunmaID, showSavunmaDetay));
                        }
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = kul.Ad + " " + kul.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = toBasvuruSavunma.ToBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = toBasvuruSavunma.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = toBasvuruSavunma.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YokDrBursiyeriBilgi", Value = toBasvuruSavunma.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + toBasvuruSavunma.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = donemAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TosBasvuru/Index?IsDegerlendirme=" + item.UniqueId, IsLink = true });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {
                            item.MailParameterDtos.Add(isLinkOrSonuc
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
                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;

                        if (!item.AdSoyad.IsNullOrWhiteSpace()) contentDetailDto.Title += " (" + item.AdSoyad + ")";
                        if (!item.JuriTipAdi.IsNullOrWhiteSpace()) contentDetailDto.Title += " (" + item.JuriTipAdi + ")";
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = enstitu.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title,
                            IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id,
                            IslemYapanIP = UserIdentity.Ip,
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailEkleris = item.GetGonderilenMailEkleris,
                            GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris
                        };

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
                var message = isLinkOrSonuc ? "Tez Öneri Savunması değerlendirmesi için Komite üyelerine değerlendirme davetiye linki mail olarak gönderilirken bir hata oluştu!" : "Tez Öneri Savunması değerlendirme sonucu Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
    }
}