using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LisansUstuBasvuruSistemi.Utilities.MailManager
{
    public class MailSenderTi
    {
        public static MmMessage SendMailTiBilgisi(int? tiBasvuruAraRaporId, int? srTalepId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
                {

                    var tiAraRapor = new TIBasvuruAraRapor();
                    var srTalebi = new SRTalepleri();

                    if (tiBasvuruAraRaporId.HasValue)
                    {
                        tiAraRapor = entities.TIBasvuruAraRapors.First(p => p.TIBasvuruAraRaporID == tiBasvuruAraRaporId);
                        if (srTalepId.HasValue) srTalebi = tiAraRapor.SRTalepleris.FirstOrDefault();
                    }
                    else if (srTalepId.HasValue)
                    {
                        srTalebi = entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);
                        tiAraRapor = srTalebi.TIBasvuruAraRapor;
                    }

                    var juriler = tiAraRapor.TIBasvuruAraRaporKomites.ToList();
                    var sablonTipIDs = new List<int>();
                    var mModel = new List<SablonMailModel>();
                    var danisman = juriler.First(p => p.JuriTipAdi == "TezDanismani");
                    var isAraRaporOrToplanti = false;

                    var ogrenci = tiAraRapor.TIBasvuru.Kullanicilar;
                    if (tiBasvuruAraRaporId.HasValue)
                    {
                        isAraRaporOrToplanti = true;
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipiEnum.TiAraRaporBaslatildiOgrenci, MailSablonTipiEnum.TiAraRaporBaslatildiDanisman });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "TezDanismani",
                            UnvanAdi = danisman.UnvanAdi,
                            AdSoyad = danisman.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.TIBasvuruAraRapor.TezDanismanID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TiAraRaporBaslatildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = tiAraRapor.TIBasvuru.Kullanicilar.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TiAraRaporBaslatildiOgrenci
                        });
                    }
                    if (srTalepId.HasValue)
                    {
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipiEnum.TiToplantiBilgiKomite, MailSablonTipiEnum.TiToplantiBilgiOgrenci });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = tiAraRapor.TIBasvuru.Kullanicilar.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TiToplantiBilgiOgrenci
                        });
                        mModel.AddRange(juriler.Select(item => new SablonMailModel
                        {
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = (item.JuriTipAdi == "TezDanismani" ? tiAraRapor.TezDanismanID : null), ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TiToplantiBilgiKomite,
                            JuriTipAdi = item.JuriTipAdi,
                            TiBasvuruAraRaporKomiteId = danisman.TIBasvuruAraRaporKomiteID
                        }));
                    }

                    var enstitu = tiAraRapor.TIBasvuru.Enstituler;

                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var anabilimDali = tiAraRapor.TIBasvuru.Programlar.AnabilimDallari;
                    var program = tiAraRapor.TIBasvuru.Programlar;
                    var oncekiMailTarihi = isAraRaporOrToplanti ? tiAraRapor.RSBaslatildiMailGonderimTarihi : tiAraRapor.ToplantiBilgiGonderimTarihi;

                    var snded = false;
                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);

                        if (item.Sablon == null) continue; 

                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);
                        item.SablonEkleri.Add(new MailSablonlariEkleri { EkAdi = ogrenci.Ad + " " + ogrenci.Soyad + " " + tiAraRapor.AraRaporSayisi + ". Tez İzleme Çalışma Raporu Dosyası", EkDosyaYolu = tiAraRapor.TICalismaRaporDosyaYolu });

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
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tiAraRapor.TIBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tiAraRapor.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tiAraRapor.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YokDrBursiyeriBilgi", Value = tiAraRapor.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + tiAraRapor.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var donemBilgi = tiAraRapor.RaporTarihi.ToAkademikDonemBilgi();
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = donemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OncekiMailTarihi", Value = oncekiMailTarihi?.ToFormatDateAndTime() });
                        }
                        #region SR Talebi
                        if (item.MailSablonTipId == MailSablonTipiEnum.TiToplantiBilgiKomite || item.MailSablonTipId == MailSablonTipiEnum.TiToplantiBilgiOgrenci)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiTarihi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "ToplantiTarihi", Value = srTalebi.Tarih.ToLongDateString() });
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiSaati"))
                                item.MailParameterDtos.Add(new MailParameterDto
                                {
                                    Key = "ToplantiSaati",
                                    Value = srTalebi.BasSaat.ToFormatTime()
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
                        foreach (var itemTik in juriler.Where(p => p.JuriTipAdi != "TezDanismani").Select((s, inx) => new { s, inx = inx + 1 }))
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
                            GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris,
                            GonderilenMailEkleris = item.GetGonderilenMailEkleris
                        };
                        entities.GonderilenMaillers.Add(kModel); 
                    }

                    if (snded)
                    {
                        entities.SaveChanges();
                        if (isAraRaporOrToplanti) tiAraRapor.RSBaslatildiMailGonderimTarihi = DateTime.Now;
                        else tiAraRapor.ToplantiBilgiGonderimTarihi = DateTime.Now; 
                        LogIslemleri.LogEkle("TIBasvuruAraRapor", LogCrudType.Update, tiAraRapor.ToJson());
                    }
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez İzleme toplantısı için Komite üyelerine mail gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
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

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = kul.EMail, KullaniciId = kul.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TiDegerlendirmeSonucGonderimOgrenci,
                        });
                    }

                    mModel.AddRange(juriler.Select(item => new SablonMailModel
                    {
                        UniqueId = item.UniqueID,
                        UnvanAdi = item.UnvanAdi,
                        AdSoyad = item.AdSoyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = (item.JuriTipAdi == "TezDanismani" ? tiAraRapor.TezDanismanID : null), ToOrBcc = true } },
                        MailSablonTipId = isLinkOrSonuc ? MailSablonTipiEnum.TiDegerlendirmeLinkGonderimKomite : MailSablonTipiEnum.TiDegerlendirmeSonucGonderimDanisman,
                        JuriTipAdi = item.JuriTipAdi,
                    }));

                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
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
                            var showDetay = item.MailSablonTipId == MailSablonTipiEnum.TiDegerlendirmeSonucGonderimDanisman;
                            var ekler = MailReportAttachment.GetTezIzlemeDegerlendirmeFormuAttachments(tiBasvuruAraRaporId, showDetay);
                            item.Attachments.AddRange(ekler);
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
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tiAraRapor.TIBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tiAraRapor.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tiAraRapor.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YokDrBursiyeriBilgi", Value = tiAraRapor.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + tiAraRapor.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var donemBilgi = tiAraRapor.RaporTarihi.ToAkademikDonemBilgi();
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = donemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TIBasvuru/Index?IsDegerlendirme=" + item.UniqueId, IsLink = true });
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
                                    Value = tiAraRapor.DegerlendirmeSonucMailTarihi?.ToFormatDateAndTime()
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
                            GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris,
                            GonderilenMailEkleris = item.GetGonderilenMailEkleris
                        };
                        db.GonderilenMaillers.Add(kModel);
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
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
    }
}