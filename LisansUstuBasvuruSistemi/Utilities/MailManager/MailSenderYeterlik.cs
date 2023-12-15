using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
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
    public class MailSenderYeterlik
    {

        public static MmMessage SendMailBasvuruOnayi(Guid basvuruUniqueId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var basvuru = db.YeterlikBasvurus.First(p => p.UniqueID == basvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = db.Kullanicilars.Find(basvuru.TezDanismanID);
                    var surec = basvuru.YeterlikSureci;
                    var enstitu = basvuru.YeterlikSureci.Enstituler;
                    var anabilimDali = basvuru.Programlar.AnabilimDallari;
                    var program = basvuru.Programlar;



                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID =basvuru.IsEnstituOnaylandi.Value? MailSablonTipiEnum.YeterlikBasvuruOnaylandiOgrenciye:MailSablonTipiEnum.YeterlikBasvuruRetEdildiOgrenciye,
                        }
                    };
                    if (basvuru.IsEnstituOnaylandi == true)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.YeterlikBasvuruOnaylandiDanismana
                        });
                    }
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {

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

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (basvuru.IsEnstituOnaylandi == false && item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "RetAciklamasi", Value = basvuru.EnstituOnayAciklama });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
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
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }

        public static MmMessage SendMailKomiteDegerlendirmeLink(Guid yeterlikBasvuruUniqueId, Guid? komiteUniqueId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var basvuru = db.YeterlikBasvurus.First(p => p.UniqueID == yeterlikBasvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = db.Kullanicilars.Find(basvuru.TezDanismanID);
                    var surec = basvuru.YeterlikSureci;
                    var enstitu = basvuru.YeterlikSureci.Enstituler;
                    var anabilimDali = basvuru.Programlar.AnabilimDallari;
                    var program = basvuru.Programlar;
                    var komiteler = basvuru.YeterlikBasvuruKomitelers.Where(p => p.UniqueID == (komiteUniqueId ?? p.UniqueID)).ToList();
                    foreach (var item in komiteler)
                    {
                        item.UniqueID = Guid.NewGuid();
                    }
                    var mModel = new List<SablonMailModel>();
                    foreach (var item in komiteler)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Komite Üyesi",
                            UniqueID = item.UniqueID,
                            UnvanAdi = item.Kullanicilar.Unvanlar.UnvanAdi,
                            AdSoyad = item.Kullanicilar.Ad + " " + item.Kullanicilar.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.YeterlikJuriUyeleriTanimlandiKomiteyeLink,
                        });
                    }
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {
                        var komite = komiteler.FirstOrDefault(p => p.UniqueID == item.UniqueID);
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
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@KomiteUyesiAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "KomiteUyesiAdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@KomiteUyesiUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "KomiteUyesiUnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/Yeterlik/Index?isKomiteOrJuri=true&isDegerlendirme=" + item.UniqueID, IsLink = true });

                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OncekiMailTarihi", Value = komite.LinkGonderimTarihi?.ToFormatDateAndTime() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
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
                            komite.DegerlendirmeIslemTarihi = null;
                            komite.DegerlendirmeIslemYapanIP = null;
                            komite.DegerlendirmeYapanID = null;
                            komite.IsJuriOnaylandi = null;
                            komite.IsLinkGonderildi = true;
                            komite.LinkGonderimTarihi = DateTime.Now;
                            komite.LinkGonderenID = UserIdentity.Current.Id;
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                            LogIslemleri.LogEkle("YeterlikBasvuruKomiteler", LogCrudType.Update, komite.ToJson());
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Yeterlik Jüri üyeleri onayı için Komite üyelerine onay davet linki mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailKomiteDegerlendirmeSonuc(Guid yeterlikBasvuruUniqueId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var basvuru = db.YeterlikBasvurus.First(p => p.UniqueID == yeterlikBasvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = db.Kullanicilars.Find(basvuru.TezDanismanID);
                    var surec = basvuru.YeterlikSureci;
                    var enstitu = basvuru.YeterlikSureci.Enstituler;
                    var anabilimDali = basvuru.Programlar.AnabilimDallari;
                    var program = basvuru.Programlar;
                    var mModel = new List<SablonMailModel>();
                    if (basvuru.IsEnstituOnaylandi == true)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.YeterlikKomiteDegerlendimreyiTamamladiDanismana
                        });
                    }
                    //var komiteler = basvuru.YeterlikBasvuruKomitelers.ToList();
                    //foreach (var item in komiteler)
                    //{
                    //    item.UniqueID = Guid.NewGuid();
                    //}
                    //foreach (var item in komiteler)
                    //{
                    //    mModel.Add(new SablonMailModel
                    //    {
                    //        JuriTipAdi = "Komite Üyesi",
                    //        UniqueID = item.UniqueID,
                    //        UnvanAdi = item.Kullanicilar.Unvanlar.UnvanAdi,
                    //        AdSoyad = item.Kullanicilar.Ad + " " + item.Kullanicilar.Soyad,
                    //        EMails = new List<MailSendList> { new MailSendList { EMail = item.Kullanicilar.EMail, ToOrBcc = true } },
                    //        MailSablonTipID = MailSablonTipi.Yeterlik_JuriUyeleriTanimlandiKomiteyeLink,
                    //    });
                    //}
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {
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
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
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

                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Yeterlik ABD komitesi Jüri üyeleri onayını tamamladıktan sonra mail gönderilirken hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }

            return mmMessage;
        }
        public static MmMessage SendMailSinavBilgi(Guid basvuruUniqueId, bool isYaziliOrSozlu = true, Guid? uniqueId = null)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var basvuru = db.YeterlikBasvurus.First(p => p.UniqueID == basvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = db.Kullanicilars.Find(basvuru.TezDanismanID);
                    var surec = basvuru.YeterlikSureci;
                    var enstitu = basvuru.YeterlikSureci.Enstituler;
                    var anabilimDali = basvuru.Programlar.AnabilimDallari;
                    var program = basvuru.Programlar;
                    var juriler = basvuru.YeterlikBasvuruJuriUyeleris.Where(p => p.IsSecilenJuri && p.UniqueID == (uniqueId ?? p.UniqueID)).ToList();



                    var ogrenciMailSablonTipId = 0;
                    var danismanMailSablonTipId = 0;
                    var juriMailSablonTipId = 0;
                    if (isYaziliOrSozlu)
                    {
                        if (!basvuru.IsYaziliSinavinaKatildi.HasValue)
                        {
                            ogrenciMailSablonTipId = MailSablonTipiEnum.YeterlikYaziliSinavTalebiYapildiOgrenciye;
                            danismanMailSablonTipId = MailSablonTipiEnum.YeterlikYaziliSinavTalebiYapildiDanismana;
                            juriMailSablonTipId = MailSablonTipiEnum.YeterlikYaziliSinavTalebiYapildiJurilere;
                        }
                        else
                        {
                            if (basvuru.IsYaziliSinavinaKatildi.Value)
                            {

                                ogrenciMailSablonTipId = basvuru.IsYaziliSinavBasarili.Value
                                    ? MailSablonTipiEnum.YeterlikYaziliSinavBasariliGirisiYapildiOgrenciye
                                    : MailSablonTipiEnum.YeterlikYaziliSinavBasarisizOnayYapildiOgrenciye;
                                danismanMailSablonTipId = basvuru.IsYaziliSinavBasarili.Value
                                    ? MailSablonTipiEnum.YeterlikYaziliSinavBasariliGirisiYapildiDanismana
                                    : MailSablonTipiEnum.YeterlikYaziliSinavBasarisizOnayYapildiDanismana;
                                if (basvuru.IsYaziliSinavBasarili.Value) juriMailSablonTipId = MailSablonTipiEnum.YeterlikYaziliSinavBasariliGirisiYapildiJurilere;
                            }
                            else
                            {
                                ogrenciMailSablonTipId =
                                    MailSablonTipiEnum.YeterlikYaziliSinavKatilmadiGirisiYapildiOgrenciye;
                                danismanMailSablonTipId =
                                    MailSablonTipiEnum.YeterlikYaziliSinavKatilmadiGirisiYapildiDanismana;
                            }
                        }
                    }
                    else
                    {
                        if (!basvuru.IsSozluSinavinaKatildi.HasValue)
                        {
                            ogrenciMailSablonTipId = MailSablonTipiEnum.YeterlikSozluSinavTalebiYapildiOgrenciye;
                            danismanMailSablonTipId = MailSablonTipiEnum.YeterlikSozluSinavTalebiYapildiDanismana;
                            juriMailSablonTipId = MailSablonTipiEnum.YeterlikSozluSinavTalebiYapildiJurilere;
                        }
                        else
                        {


                            if (basvuru.IsSozluSinavinaKatildi.Value)
                            {
                                if (juriler.All(a => a.IsSonucOnaylandi.HasValue))
                                {
                                    ogrenciMailSablonTipId = basvuru.IsGenelSonucBasarili.Value ? MailSablonTipiEnum.YeterlikGenelSinavSonucuBasariliOgrenciye : MailSablonTipiEnum.YeterlikGenelSinavSonucuBasarisizOgrenciye;
                                    danismanMailSablonTipId = basvuru.IsGenelSonucBasarili.Value ? MailSablonTipiEnum.YeterlikGenelSinavSonucuBasariliDanismana : MailSablonTipiEnum.YeterlikGenelSinavSonucuBasarisizDanismana;
                                    juriMailSablonTipId = basvuru.IsGenelSonucBasarili.Value ? MailSablonTipiEnum.YeterlikGenelSinavSonucuBasariliJurilere : MailSablonTipiEnum.YeterlikGenelSinavSonucuBasarisizJurilere;

                                }
                            }
                            else
                            {
                                ogrenciMailSablonTipId = MailSablonTipiEnum.YeterlikSozluSinavKatilmadiGirisiYapildiOgrenciye;
                                danismanMailSablonTipId = MailSablonTipiEnum.YeterlikSozluSinavKatilmadiGirisiYapildiDanismana;
                            }
                        }
                    }

                    var mModel = new List<SablonMailModel>();
                    if (!uniqueId.HasValue)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList>
                                { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = ogrenciMailSablonTipId
                        });
                        mModel.Add(
                            new SablonMailModel
                            {

                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList>
                                    { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = danismanMailSablonTipId,
                                Attachments = basvuru.IsGenelSonucBasarili.HasValue ?
                                    Management.ExportRaporPdf(RaporTipiEnum.YeterlikDoktoraSinavSonucFormu, new List<int?> { basvuru.YeterlikBasvuruID }) : new List<Attachment>()
                            }
                        );
                        juriler = juriler.Where(p => p.JuriTipAdi != "TezDanismani").ToList();
                    }

                    foreach (var item in juriler)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = item.JuriTipAdi,
                            UniqueID = item.UniqueID,
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                            MailSablonTipID = juriMailSablonTipId
                        });
                    }


                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        var gonderilenMailEkleri = item.Attachments.Select(s => new GonderilenMailEkleri { EkAdi = s.Name }).ToList();
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

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@JuriUyesiAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "JuriUyesiAdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@JuriUyesiUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "JuriUyesiUnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "SinavTarihi",
                                Value = isYaziliOrSozlu ?
                                basvuru.YaziliSinavTarihi.ToFormatDateAndTime()
                                : basvuru.SozluSinavTarihi.ToFormatDateAndTime()
                            });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            mailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "SinavYeri",
                                Value = isYaziliOrSozlu ?
                                basvuru.YaziliSinavYeri
                                : basvuru.SozluSinavYeri
                            });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavSekli") && basvuru.IsSozluSinavOnline.HasValue)
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "SinavSekli", Value = basvuru.IsSozluSinavOnline == true ? "Online" : "Yüz Yüze" });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavNotu"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "SinavNotu", Value = basvuru.YaziliSinaviNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@YaziliNotu"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "YaziliNotu", Value = basvuru.YaziliSinaviNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SozluNotuOrtalama"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "SozluNotuOrtalama", Value = basvuru.SozluSinaviOrtalamaNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@GenelOrtalama"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "GenelOrtalama", Value = basvuru.GenelBasariNotu.ToString() });
                        }
                        if (item.UniqueID.HasValue && item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/Yeterlik/Index?isKomiteOrJuri=false&isDegerlendirme=" + item.UniqueID, IsLink = true });
                        }
                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
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
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailSinavJuriLink(Guid basvuruUniqueId, bool isYaziliOrSozlu = true, Guid? uniqueId = null)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var basvuru = db.YeterlikBasvurus.First(p => p.UniqueID == basvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = db.Kullanicilars.Find(basvuru.TezDanismanID);
                    var surec = basvuru.YeterlikSureci;
                    var enstitu = basvuru.YeterlikSureci.Enstituler;
                    var anabilimDali = basvuru.Programlar.AnabilimDallari;
                    var program = basvuru.Programlar;
                    var juriler = basvuru.YeterlikBasvuruJuriUyeleris.Where(p => p.IsSecilenJuri && p.UniqueID == (uniqueId ?? p.UniqueID)).ToList();


                    foreach (var item in juriler)
                    {
                        item.UniqueID = Guid.NewGuid();
                    }

                    var juriMailSablonTipId = 0;
                    if (isYaziliOrSozlu)
                    {
                        if (basvuru.IsYaziliSinavinaKatildi == false)
                        {
                            juriMailSablonTipId = MailSablonTipiEnum.YeterlikYaziliSinavKatilmadiGirisiYapildiJurilereLink;
                        }
                        else if (basvuru.IsYaziliSinavinaKatildi == true && basvuru.IsYaziliSinavBasarili == false)
                        {
                            juriMailSablonTipId = MailSablonTipiEnum.YeterlikYaziliSinavBasarisizGirisiYapildiJurilereLink;
                        }

                    }
                    else
                    {
                        if (basvuru.IsSozluSinavinaKatildi.HasValue)
                        {
                            foreach (var item in juriler)
                            {
                                item.UniqueID = Guid.NewGuid();
                            }
                            juriMailSablonTipId = basvuru.IsSozluSinavinaKatildi.Value ? MailSablonTipiEnum.YeterlikSozluNotGirisJurilereLink : MailSablonTipiEnum.YeterlikSozluSinavKatilmadiGirisiYapildiJurilereLink;
                        }
                    }

                    var mModel = new List<SablonMailModel>();


                    foreach (var item in juriler)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = item.JuriTipAdi,
                            UniqueID = item.UniqueID,
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                            MailSablonTipID = juriMailSablonTipId
                        });
                    }
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

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@JuriUyesiAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "JuriUyesiAdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@JuriUyesiUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "JuriUyesiUnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "SinavTarihi",
                                Value = isYaziliOrSozlu ?
                                    basvuru.YaziliSinavTarihi.ToFormatDateAndTime()
                                    : basvuru.SozluSinavTarihi.ToFormatDateAndTime()
                            });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            mailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "SinavYeri",
                                Value = isYaziliOrSozlu ?
                                    basvuru.YaziliSinavYeri
                                    : basvuru.SozluSinavYeri
                            });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavSekli") && basvuru.IsSozluSinavOnline.HasValue)
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "SinavSekli", Value = basvuru.IsSozluSinavOnline == true ? "Online" : "Yüz Yüze" });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavNotu"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "SinavNotu", Value = basvuru.YaziliSinaviNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@YaziliNotu"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "YaziliNotu", Value = basvuru.YaziliSinaviNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SozluNotuOrtalama"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "SozluNotuOrtalama", Value = basvuru.SozluSinaviOrtalamaNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@GenelOrtalama"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "GenelOrtalama", Value = basvuru.GenelBasariNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OncekiMailTarihi", Value = juri.LinkGonderimTarihi?.ToFormatDateAndTime() });

                        if (item.UniqueID.HasValue && item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/Yeterlik/Index?isKomiteOrJuri=false&isDegerlendirme=" + item.UniqueID, IsLink = true });
                        }
                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
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

                            juri.IsSonucOnaylandi = null;
                            juri.SozluNotu = null;
                            juri.DegerlendirmeTarihi = null;
                            juri.LinkGonderimTarihi = DateTime.Now;
                            juri.LinkGonderenID = UserIdentity.Current.Id;
                            juri.IsLinkGonderildi = true;
                            basvuru.GenelBasariNotu = null;
                            basvuru.IsGenelSonucBasarili = null;



                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);  
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
    }
}