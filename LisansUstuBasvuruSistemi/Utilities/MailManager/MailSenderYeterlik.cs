using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
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
                using (var  entities = new LubsDbEntities())
                {
                    var basvuru = entities.YeterlikBasvurus.First(p => p.UniqueID == basvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = entities.Kullanicilars.First(f => f.KullaniciID == basvuru.TezDanismanID);
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
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail,KullaniciId =ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId =basvuru.IsEnstituOnaylandi==true? MailSablonTipiEnum.YeterlikBasvuruOnaylandiOgrenciye:MailSablonTipiEnum.YeterlikBasvuruRetEdildiOgrenciye,
                        }
                    };
                    if (basvuru.IsEnstituOnaylandi == true)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.YeterlikBasvuruOnaylandiDanismana
                        });
                    }
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());



                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (basvuru.IsEnstituOnaylandi == false && item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "RetAciklamasi", Value = basvuru.EnstituOnayAciklama });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
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
                            entities.SaveChanges();
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
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
                using (var  entities = new LubsDbEntities())
                {
                    var basvuru = entities.YeterlikBasvurus.First(p => p.UniqueID == yeterlikBasvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = entities.Kullanicilars.Find(basvuru.TezDanismanID);
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
                            UniqueId = item.UniqueID,
                            UnvanAdi = item.Kullanicilar.Unvanlar.UnvanAdi,
                            AdSoyad = item.Kullanicilar.Ad + " " + item.Kullanicilar.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.Kullanicilar.EMail, KullaniciId = item.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.YeterlikJuriUyeleriTanimlandiKomiteyeLink,
                        });
                    }
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;

                        var komite = komiteler.FirstOrDefault(p => p.UniqueID == item.UniqueId);
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@KomiteUyesiAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "KomiteUyesiAdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@KomiteUyesiUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "KomiteUyesiUnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/Yeterlik/Index?isKomiteOrJuri=true&isDegerlendirme=" + item.UniqueId, IsLink = true });

                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OncekiMailTarihi", Value = komite.LinkGonderimTarihi?.ToFormatDateAndTime() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
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
                            komite.DegerlendirmeIslemTarihi = null;
                            komite.DegerlendirmeIslemYapanIP = null;
                            komite.DegerlendirmeYapanID = null;
                            komite.IsJuriOnaylandi = null;
                            komite.IsLinkGonderildi = true;
                            komite.LinkGonderimTarihi = DateTime.Now;
                            komite.LinkGonderenID = UserIdentity.Current.Id;
                            entities.SaveChanges();
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
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
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
                using (var  entities = new LubsDbEntities())
                {
                    var basvuru = entities.YeterlikBasvurus.First(p => p.UniqueID == yeterlikBasvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = entities.Kullanicilars.First(f=>f.KullaniciID==basvuru.TezDanismanID);
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
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.YeterlikKomiteDegerlendimreyiTamamladiDanismana
                        });
                    }
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
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
                            entities.SaveChanges();
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Yeterlik ABD komitesi Jüri üyeleri onayını tamamladıktan sonra mail gönderilirken hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
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
                using (var  entities = new LubsDbEntities())
                {
                    var basvuru = entities.YeterlikBasvurus.First(p => p.UniqueID == basvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = entities.Kullanicilars.First(f => f.KullaniciID == basvuru.TezDanismanID);
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

                                ogrenciMailSablonTipId = basvuru.IsYaziliSinavBasarili != null && basvuru.IsYaziliSinavBasarili.Value
                                    ? MailSablonTipiEnum.YeterlikYaziliSinavBasariliGirisiYapildiOgrenciye
                                    : MailSablonTipiEnum.YeterlikYaziliSinavBasarisizOnayYapildiOgrenciye;
                                danismanMailSablonTipId = basvuru.IsYaziliSinavBasarili != null && basvuru.IsYaziliSinavBasarili.Value
                                    ? MailSablonTipiEnum.YeterlikYaziliSinavBasariliGirisiYapildiDanismana
                                    : MailSablonTipiEnum.YeterlikYaziliSinavBasarisizOnayYapildiDanismana;
                                if (basvuru.IsYaziliSinavBasarili != null && basvuru.IsYaziliSinavBasarili.Value) juriMailSablonTipId = MailSablonTipiEnum.YeterlikYaziliSinavBasariliGirisiYapildiJurilere;
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
                                    ogrenciMailSablonTipId = basvuru.IsGenelSonucBasarili != null && basvuru.IsGenelSonucBasarili.Value ? MailSablonTipiEnum.YeterlikGenelSinavSonucuBasariliOgrenciye : MailSablonTipiEnum.YeterlikGenelSinavSonucuBasarisizOgrenciye;
                                    danismanMailSablonTipId = basvuru.IsGenelSonucBasarili != null && basvuru.IsGenelSonucBasarili.Value ? MailSablonTipiEnum.YeterlikGenelSinavSonucuBasariliDanismana : MailSablonTipiEnum.YeterlikGenelSinavSonucuBasarisizDanismana;
                                    juriMailSablonTipId = basvuru.IsGenelSonucBasarili != null && basvuru.IsGenelSonucBasarili.Value ? MailSablonTipiEnum.YeterlikGenelSinavSonucuBasariliJurilere : MailSablonTipiEnum.YeterlikGenelSinavSonucuBasarisizJurilere;

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
                                { new MailSendList { EMail = ogrenci.EMail,KullaniciId =ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = ogrenciMailSablonTipId
                        });
                        mModel.Add(
                            new SablonMailModel
                            {

                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList>
                                    { new MailSendList { EMail = danisman.EMail,KullaniciId =danisman.KullaniciID, ToOrBcc = true } },
                                MailSablonTipId = danismanMailSablonTipId,
                                Attachments = basvuru.IsGenelSonucBasarili.HasValue ?
                                    MailReportAttachment.GetYeterlikDoktoraSinavSonucFormuAttachments(basvuru.YeterlikBasvuruID) : new List<Attachment>()
                            }
                        );
                        juriler = juriler.Where(p => p.JuriTipAdi != "TezDanismani").ToList();
                    }

                    foreach (var item in juriler)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = item.JuriTipAdi,
                            UniqueId = item.UniqueID,
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = (item.JuriTipAdi == "TezDanismani" ? danisman?.KullaniciID : null), ToOrBcc = true } },
                            MailSablonTipId = juriMailSablonTipId
                        });
                    }


                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());
                         

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@JuriUyesiAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "JuriUyesiAdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@JuriUyesiUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "JuriUyesiUnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "SinavTarihi",
                                Value = isYaziliOrSozlu ?
                                basvuru.YaziliSinavTarihi.ToFormatDateAndTime()
                                : basvuru.SozluSinavTarihi.ToFormatDateAndTime()
                            });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "SinavYeri",
                                Value = isYaziliOrSozlu ?
                                basvuru.YaziliSinavYeri
                                : basvuru.SozluSinavYeri
                            });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavSekli") && basvuru.IsSozluSinavOnline.HasValue)
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavSekli", Value = basvuru.IsSozluSinavOnline == true ? "Online" : "Yüz Yüze" });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavNotu"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavNotu", Value = basvuru.YaziliSinaviNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@YaziliNotu"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YaziliNotu", Value = basvuru.YaziliSinaviNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SozluNotuOrtalama"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SozluNotuOrtalama", Value = basvuru.SozluSinaviOrtalamaNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@GenelOrtalama"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "GenelOrtalama", Value = basvuru.GenelBasariNotu.ToString() });
                        }
                        if (item.UniqueId.HasValue && item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/Yeterlik/Index?isKomiteOrJuri=false&isDegerlendirme=" + item.UniqueId, IsLink = true });
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
                        entities.GonderilenMaillers.Add(kModel);
                        entities.SaveChanges();
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
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
                using (var  entities = new LubsDbEntities())
                {
                    var basvuru = entities.YeterlikBasvurus.First(p => p.UniqueID == basvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = entities.Kullanicilars.Find(basvuru.TezDanismanID);
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
                            UniqueId = item.UniqueID,
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EnstituAdi = enstitu.EnstituAd,
                            WebAdresi = enstitu.WebAdresi,
                            SistemErisimAdresi = enstitu.SistemErisimAdresi,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = item.JuriTipAdi == "TezDanismani" ? danisman?.KullaniciID : null, ToOrBcc = true } },
                            MailSablonTipId = juriMailSablonTipId
                        });
                    }
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {

                        var juri = juriler.FirstOrDefault(p => p.UniqueID == item.UniqueId);
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);

                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = item.EnstituAdi });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = item.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@JuriUyesiAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "JuriUyesiAdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@JuriUyesiUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "JuriUyesiUnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "SinavTarihi",
                                Value = isYaziliOrSozlu ? basvuru.YaziliSinavTarihi.ToFormatDateAndTime() : basvuru.SozluSinavTarihi.ToFormatDateAndTime()
                            });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "SinavYeri",
                                Value = isYaziliOrSozlu ? basvuru.YaziliSinavYeri : basvuru.SozluSinavYeri
                            });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavSekli") && basvuru.IsSozluSinavOnline.HasValue)
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavSekli", Value = basvuru.IsSozluSinavOnline == true ? "Online" : "Yüz Yüze" });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavNotu"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavNotu", Value = basvuru.YaziliSinaviNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@YaziliNotu"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YaziliNotu", Value = basvuru.YaziliSinaviNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SozluNotuOrtalama"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SozluNotuOrtalama", Value = basvuru.SozluSinaviOrtalamaNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@GenelOrtalama"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "GenelOrtalama", Value = basvuru.GenelBasariNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OncekiMailTarihi", Value = juri.LinkGonderimTarihi?.ToFormatDateAndTime() });

                        if (item.UniqueId.HasValue && item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/Yeterlik/Index?isKomiteOrJuri=false&isDegerlendirme=" + item.UniqueId, IsLink = true });
                        }
                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
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

                            juri.IsSonucOnaylandi = null;
                            juri.SozluNotu = null;
                            juri.DegerlendirmeTarihi = null;
                            juri.LinkGonderimTarihi = DateTime.Now;
                            juri.LinkGonderenID = UserIdentity.Current.Id;
                            juri.IsLinkGonderildi = true;
                            basvuru.GenelBasariNotu = null;
                            basvuru.IsGenelSonucBasarili = null;
                            entities.SaveChanges();
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
    }
}