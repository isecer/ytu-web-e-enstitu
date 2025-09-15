using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.MailManager
{
    public class MailSenderKs
    {
        public static MmMessage SendMailBasvuruYapildi(int donemProjesiBasvuruId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var kayitSilmeBasvuru = entities.KayitSilmeBasvurus.First(p => p.KayitSilmeBasvuruID == donemProjesiBasvuruId);
                    var ogrenci = kayitSilmeBasvuru.Kullanicilar;
                    var enstitu = kayitSilmeBasvuru.Enstituler;
                    var ogrenimSeviyesi = entities.OgrenimTipleris.FirstOrDefault(f => f.OgrenimTipKod == kayitSilmeBasvuru.OgrenimTipKod && f.EnstituKod == kayitSilmeBasvuru.EnstituKod);
                    var program = kayitSilmeBasvuru.Programlar;
                    var harcBirimiOnaySorumlusuKullaniciIds = KayitSilmeAyar.GetHarcBirimiOnaySorumlusuKullaniciIds();

                    var harcBirimSorumlulari = entities.Kullanicilars.Where(f => harcBirimiOnaySorumlusuKullaniciIds.Contains(f.KullaniciID)).Select(s => new
                    {
                        s.KullaniciID,
                        s.Unvanlar.UnvanAdi,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.EMail

                    }).ToList();

                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId =MailSablonTipiEnum.KsYapildiOgrenciye
                        }
                    };
                    foreach (var item in harcBirimSorumlulari)
                    {
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Harç Birimi",
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = item.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.KsYapildiHarcBirimine
                        });
                    }
                    var sablonTipIds = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIds.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();




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
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrenimSeviyesi.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = kayitSilmeBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });

                        if (item.JuriTipAdi == "Harç Birimi")
                        {
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluUnvanAdi", Value = item.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluAdSoyad", Value = item.AdSoyad });
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
                        entities.GonderilenMaillers.Add(kModel);
                        entities.SaveChanges();
                    }
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Kayıt Silme Başvurusu başvuru bilgileri mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailHarcBirimiOnay(int kayitSilmeBasvuruId, bool isOnayOrRet)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var kayitSilmeBasvuru = entities.KayitSilmeBasvurus.First(f => f.KayitSilmeBasvuruID == kayitSilmeBasvuruId);
                    var ogrenci = kayitSilmeBasvuru.Kullanicilar;
                    var enstitu = kayitSilmeBasvuru.Enstituler;
                    var ogrenimSeviyesi = entities.OgrenimTipleris.FirstOrDefault(f => f.OgrenimTipKod == kayitSilmeBasvuru.OgrenimTipKod && f.EnstituKod == kayitSilmeBasvuru.EnstituKod);
                    var program = kayitSilmeBasvuru.Programlar;

                    var kutuphaneBirimiOnaySorumlusuKullaniciIds = KayitSilmeAyar.GetKutuphaneBirimiOnaySorumlusuKullaniciIds();
                    var kutuphaneBirimSorumlulari = entities.Kullanicilars.Where(f => kutuphaneBirimiOnaySorumlusuKullaniciIds.Contains(f.KullaniciID)).Select(s => new
                    {
                        s.KullaniciID,
                        s.Unvanlar.UnvanAdi,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.EMail

                    }).ToList();

                    var mModel = new List<SablonMailModel>();
                    if (isOnayOrRet)
                    {
                        foreach (var item in kutuphaneBirimSorumlulari)
                        {
                            mModel.Add(new SablonMailModel
                            {

                                JuriTipAdi = "Kütüphane Birimi",
                                UnvanAdi = item.UnvanAdi,
                                AdSoyad = item.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = item.KullaniciID, ToOrBcc = true } },
                                MailSablonTipId = MailSablonTipiEnum.KsHarcBirimiTarafindanOnaylandiKutuphaneBirimine
                            });
                        }
                    }
                    else
                    {
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.KsHarcBirimiTarafindanReddedildiOgrenciye
                        });

                    }

                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


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
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrenimSeviyesi.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = kayitSilmeBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });


                        if (item.JuriTipAdi == "Kütüphane Birimi")
                        {
                            if (item.SablonParametreleri.Any(a => a == "@KutuphaneBirimSorumluUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "KutuphaneBirimSorumluUnvanAdi", Value = item.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@KutuphaneBirimSorumluAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "KutuphaneBirimSorumluAdSoyad", Value = item.AdSoyad });
                        }

                        if (!isOnayOrRet)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "RetAciklamasi", Value = kayitSilmeBasvuru.HarcBirimiOnayAciklamasi });

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
                        entities.GonderilenMaillers.Add(kModel);
                        entities.SaveChanges();
                    }
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Kayıt Silme Başvurusu Eyk Onay sonucu bilgisi mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailKutuphaneBirimiRet(int kayitSilmeBasvuruId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var kayitSilmeBasvuru = entities.KayitSilmeBasvurus.First(f => f.KayitSilmeBasvuruID == kayitSilmeBasvuruId);
                    var ogrenci = kayitSilmeBasvuru.Kullanicilar;
                    var enstitu = kayitSilmeBasvuru.Enstituler;
                    var ogrenimSeviyesi = entities.OgrenimTipleris.FirstOrDefault(f => f.OgrenimTipKod == kayitSilmeBasvuru.OgrenimTipKod && f.EnstituKod == kayitSilmeBasvuru.EnstituKod);
                    var program = kayitSilmeBasvuru.Programlar;

                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.KsKutuphaneBirimiTarafindanReddedildiOgrenciye
                        }
                    };


                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


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
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrenimSeviyesi.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = kayitSilmeBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });

                        if (item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "RetAciklamasi", Value = kayitSilmeBasvuru.KutuphaneBirimiOnayAciklamasi });



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
                        entities.GonderilenMaillers.Add(kModel);
                        entities.SaveChanges();
                    }
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Kayıt Silme Başvurusu Eyk Onay sonucu bilgisi mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }


        public static MmMessage SendMailEykOnaylanmadi(int kayitSilmeBasvuruId, bool isEykYaOrEykDa)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var kayitSilmeBasvuru = entities.KayitSilmeBasvurus.First(f => f.KayitSilmeBasvuruID == kayitSilmeBasvuruId);
                    var ogrenci = kayitSilmeBasvuru.Kullanicilar;
                    var enstitu = kayitSilmeBasvuru.Enstituler;
                    var ogrenimSeviyesi = entities.OgrenimTipleris.FirstOrDefault(f => f.OgrenimTipKod == kayitSilmeBasvuru.OgrenimTipKod && f.EnstituKod == kayitSilmeBasvuru.EnstituKod);
                    var program = kayitSilmeBasvuru.Programlar;
                    var harcBirimiOnaySorumlusuKullaniciIds = KayitSilmeAyar.GetHarcBirimiOnaySorumlusuKullaniciIds();
                    var harcBirimSorumlulari = entities.Kullanicilars.Where(f => harcBirimiOnaySorumlusuKullaniciIds.Contains(f.KullaniciID)).Select(s => new
                    {
                        s.KullaniciID,
                        s.Unvanlar.UnvanAdi,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.EMail

                    }).ToList();
                    var kutuphaneBirimiOnaySorumlusuKullaniciIds = KayitSilmeAyar.GetKutuphaneBirimiOnaySorumlusuKullaniciIds();
                    var kutuphaneBirimSorumlulari = entities.Kullanicilars.Where(f => kutuphaneBirimiOnaySorumlusuKullaniciIds.Contains(f.KullaniciID)).Select(s => new
                    {
                        s.KullaniciID,
                        s.Unvanlar.UnvanAdi,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.EMail

                    }).ToList();
                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId =isEykYaOrEykDa? MailSablonTipiEnum.KsEykYaGonderimiReddedildiOgrenciye:MailSablonTipiEnum.KsEykDaOnaylanmadiOgrenciye
                        }
                    };

                    foreach (var item in kutuphaneBirimSorumlulari)
                    {
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Kütüphane Birimi",
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = item.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isEykYaOrEykDa ? MailSablonTipiEnum.KsEykYaGonderimiReddedildiKutuphaneBirimine : MailSablonTipiEnum.KsEYkDaOnaylanmadiKutuphaneBirimine
                        });
                    }
                    foreach (var item in harcBirimSorumlulari)
                    {
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Harç Birimi",
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = item.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isEykYaOrEykDa ? MailSablonTipiEnum.KsEykYaGonderimiReddedildiHarcBirimine : MailSablonTipiEnum.KsEYkDaOnaylanmadiHarcBirimine
                        });
                    }
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


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
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrenimSeviyesi.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = kayitSilmeBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });

                        if (item.JuriTipAdi == "Harç Birimi")
                        {
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluUnvanAdi", Value = item.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluAdSoyad", Value = item.AdSoyad });
                        }
                        if (item.JuriTipAdi == "Kütüphane Birimi")
                        {
                            if (item.SablonParametreleri.Any(a => a == "@KutuphaneBirimSorumluUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "KutuphaneBirimSorumluUnvanAdi", Value = item.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@KutuphaneBirimSorumluAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "KutuphaneBirimSorumluAdSoyad", Value = item.AdSoyad });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = kayitSilmeBasvuru.EYKTarihi.ToFormatDate() });
                        if (item.SablonParametreleri.Any(a => a == "@EYKSayisi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKSayisi", Value = kayitSilmeBasvuru.EYKSayisi });
                        if (item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "RetAciklamasi", Value = isEykYaOrEykDa ? kayitSilmeBasvuru.EYKYaGonderimDurumAciklamasi : kayitSilmeBasvuru.EYKDaOnaylanmadiDurumAciklamasi });

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
                        entities.GonderilenMaillers.Add(kModel);
                        entities.SaveChanges();
                    }
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Kayıt Silme Başvurusu Eyk Onay sonucu bilgisi mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailEykOnaylandi(int kayitSilmeBasvuruId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var kayitSilmeBasvuru = entities.KayitSilmeBasvurus.First(f => f.KayitSilmeBasvuruID == kayitSilmeBasvuruId);
                    var ogrenci = kayitSilmeBasvuru.Kullanicilar;
                    var enstitu = kayitSilmeBasvuru.Enstituler;
                    var ogrenimSeviyesi = entities.OgrenimTipleris.FirstOrDefault(f => f.OgrenimTipKod == kayitSilmeBasvuru.OgrenimTipKod && f.EnstituKod == kayitSilmeBasvuru.EnstituKod);
                    var program = kayitSilmeBasvuru.Programlar;
                   
                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId =MailSablonTipiEnum.KsEykDaOnaylandiOgrenciye
                        }
                    };

                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


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
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrenimSeviyesi.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = kayitSilmeBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
 
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = kayitSilmeBasvuru.EYKTarihi.ToFormatDate() });
                        if (item.SablonParametreleri.Any(a => a == "@EYKSayisi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKSayisi", Value = kayitSilmeBasvuru.EYKSayisi });
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
                        entities.GonderilenMaillers.Add(kModel);
                        entities.SaveChanges();
                    }
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Kayıt Silme Başvurusu Eyk Onay sonucu bilgisi mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }


    }
}