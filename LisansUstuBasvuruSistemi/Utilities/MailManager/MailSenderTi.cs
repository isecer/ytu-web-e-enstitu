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
    public class MailSenderTi
    {
        public static MmMessage SendMailTiBilgisi(int? tiBasvuruAraRaporId, int? srTalepId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {

                    var tiAraRapor = new TIBasvuruAraRapor();
                    var srTalebi = new SRTalepleri();

                    if (tiBasvuruAraRaporId.HasValue)
                    {
                        tiAraRapor = db.TIBasvuruAraRapors.First(p => p.TIBasvuruAraRaporID == tiBasvuruAraRaporId);
                        if (srTalepId.HasValue) srTalebi = tiAraRapor.SRTalepleris.FirstOrDefault();
                    }
                    else if (srTalepId.HasValue)
                    {
                        srTalebi = db.SRTalepleris.First(p => p.SRTalepID == srTalepId);
                        tiAraRapor = srTalebi.TIBasvuruAraRapor;
                    }

                    var juriler = tiAraRapor.TIBasvuruAraRaporKomites.ToList();
                    var sablonTipIDs = new List<int>();
                    var mModel = new List<SablonMailModel>();
                    var danisman = juriler.First(p => p.JuriTipAdi == "TezDanismani");
                    var isAraRaporOrToplanti = false;
                    var gonderilenMEkleris = new List<GonderilenMailEkleri>
                    {
                        new GonderilenMailEkleri
                        {
                            EkAdi = tiAraRapor.TICalismaRaporDosyaAdi,
                            EkDosyaYolu = tiAraRapor.TICalismaRaporDosyaYolu,
                        }
                    };
                    var kul = tiAraRapor.TIBasvuru.Kullanicilar;
                    if (tiBasvuruAraRaporId.HasValue)
                    {
                        isAraRaporOrToplanti = true;
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipiEnum.TiAraRaporBaslatildiOgrenci, MailSablonTipiEnum.TiAraRaporBaslatildiDanisman });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "TezDanismani",
                            UnvanAdi = danisman.UnvanAdi,
                            AdSoyad = danisman.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TiAraRaporBaslatildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci " + kul.Ad + " " + kul.Soyad,
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = tiAraRapor.TIBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TiAraRaporBaslatildiOgrenci
                        });
                    }
                    if (srTalepId.HasValue)
                    {
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipiEnum.TiToplantiBilgiKomite, MailSablonTipiEnum.TiToplantiBilgiOgrenci });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci " + kul.Ad + " " + kul.Soyad,
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = tiAraRapor.TIBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TiToplantiBilgiOgrenci
                        });
                        mModel.AddRange(juriler.Select(item => new SablonMailModel
                        {
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TiToplantiBilgiKomite,
                            JuriTipAdi = item.JuriTipAdi,
                            TIBasvuruAraRaporKomiteID = danisman.TIBasvuruAraRaporKomiteID
                        }));
                    }

                    var enstitu = tiAraRapor.TIBasvuru.Enstituler;

                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var abdL = tiAraRapor.TIBasvuru.Programlar.AnabilimDallari;
                    var prgL = tiAraRapor.TIBasvuru.Programlar;
                    var oncekiMailTarihi = isAraRaporOrToplanti ? tiAraRapor.RSBaslatildiMailGonderimTarihi : tiAraRapor.ToplantiBilgiGonderimTarihi;

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
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tiAraRapor.TIBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tiAraRapor.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tiAraRapor.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YokDrBursiyeriBilgi", Value = tiAraRapor.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + tiAraRapor.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var donemBilgi = tiAraRapor.RaporTarihi.ToAraRaporDonemBilgi();
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = donemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "OncekiMailTarihi", Value = oncekiMailTarihi?.ToFormatDateAndTime() });
                        }
                        #region SR Talebi
                        if (item.MailSablonTipID == MailSablonTipiEnum.TiToplantiBilgiKomite || item.MailSablonTipID == MailSablonTipiEnum.TiToplantiBilgiOgrenci)
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
                        foreach (var itemTik in juriler.Where(p => p.JuriTipAdi != "TezDanismani").Select((s, inx) => new { s, inx = inx + 1 }))
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
                        if (isAraRaporOrToplanti) tiAraRapor.RSBaslatildiMailGonderimTarihi = DateTime.Now;
                        else tiAraRapor.ToplantiBilgiGonderimTarihi = DateTime.Now;

                        LogIslemleri.LogEkle("TIBasvuruAraRapor", LogCrudType.Update, tiAraRapor.ToJson());
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez İzleme toplantısı için Komite üyelerine mail gönderilirken bir hata oluştu! \r\nSRTalepID:" + srTalepId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailTiDegerlendirmeLink(int tiBasvuruAraRaporId, Guid? uniqueId, bool isLinkOrSonuc)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var tiAraRapor = db.TIBasvuruAraRapors.First(p => p.TIBasvuruAraRaporID == tiBasvuruAraRaporId);
                    var juriler = tiAraRapor.TIBasvuruAraRaporKomites.Where(p => (isLinkOrSonuc ? p.JuriTipAdi != "TezDanismani" : p.JuriTipAdi == "TezDanismani") && p.UniqueID == (uniqueId ?? p.UniqueID)).ToList();

                    var mModel = new List<SablonMailModel>();

                    var enstitu = tiAraRapor.TIBasvuru.Enstituler;

                    var abdL = tiAraRapor.TIBasvuru.Programlar.AnabilimDallari;
                    var prgL = tiAraRapor.TIBasvuru.Programlar;
                    var kul = tiAraRapor.TIBasvuru.Kullanicilar;
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
                            EMails = new List<MailSendList> { new MailSendList { EMail = tiAraRapor.TIBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TiDegerlendirmeSonucGonderimOgrenci,
                        });
                    }

                    mModel.AddRange(juriler.Select(item => new SablonMailModel
                    {
                        UniqueID = item.UniqueID,
                        UnvanAdi = item.UnvanAdi,
                        AdSoyad = item.AdSoyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                        MailSablonTipID = isLinkOrSonuc ? MailSablonTipiEnum.TiDegerlendirmeLinkGonderimKomite : MailSablonTipiEnum.TiDegerlendirmeSonucGonderimDanisman,
                        JuriTipAdi = item.JuriTipAdi,
                    }));
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
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
                            var ds = new List<int?>() { tiBasvuruAraRaporId };
                            if (item.MailSablonTipID == MailSablonTipiEnum.TiDegerlendirmeSonucGonderimDanisman) ds.Add(1);
                            var ekler = Management.ExportRaporPdf(RaporTipiEnum.TezIzlemeDegerlendirmeFormu, ds);
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
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tiAraRapor.TIBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tiAraRapor.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tiAraRapor.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YokDrBursiyeriBilgi", Value = tiAraRapor.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + tiAraRapor.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var donemBilgi = tiAraRapor.RaporTarihi.ToAraRaporDonemBilgi();
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = donemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TIBasvuru/Index?IsDegerlendirme=" + item.UniqueID, IsLink = true });
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
                                    Value = tiAraRapor.DegerlendirmeSonucMailTarihi?.ToFormatDateAndTime()
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
                            juri.IsBasarili = null;
                            juri.IsTezIzlemeRaporuAltAlanUygun = null;
                            juri.IsTezIzlemeRaporuTezOnerisiUygun = null;
                            juri.Aciklama = null;
                            juri.IsLinkGonderildi = true;
                            juri.LinkGonderimTarihi = DateTime.Now;
                            juri.LinkGonderenID = UserIdentity.Current.Id;

                        }

                        db.GonderilenMaillers.Add(kModel);
                        db.SaveChanges();
                        if (isLinkOrSonuc) LogIslemleri.LogEkle("TIBasvuruAraRaporKomite", LogCrudType.Update, juri.ToJson());
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
               
                var message = isLinkOrSonuc ? "Tez İzleme değerlendirmesi için Komite üyelerine değerlendirme davetiye linki mail olarak gönderilirken bir hata oluştu!" : "Tez İzleme değerlendirmesi sonucu Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
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