using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System.Data.Entity;
using System.Threading.Tasks;

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
                    var harcBirimiOnaySorumlusuKullaniciId = KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.GetAyar(kayitSilmeBasvuru.EnstituKod).ToInt();

                    var harcBirimSorumlusu = entities.Kullanicilars.Where(f => f.KullaniciID == harcBirimiOnaySorumlusuKullaniciId).Select(s => new
                    {
                        s.KullaniciID,
                        s.Unvanlar.UnvanAdi,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.EMail

                    }).FirstOrDefault();

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
                    if (harcBirimSorumlusu != null)
                    {
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Harç Birimi",
                            AdSoyad = harcBirimSorumlusu.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = harcBirimSorumlusu.EMail, KullaniciId = harcBirimSorumlusu.KullaniciID, ToOrBcc = true } },
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

                        if (harcBirimSorumlusu != null)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluUnvanAdi", Value = harcBirimSorumlusu.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluAdSoyad", Value = harcBirimSorumlusu.AdSoyad });
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
                    var kutuphaneBirimiOnaySorumlusuKullaniciId = KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.GetAyar(kayitSilmeBasvuru.EnstituKod).ToInt();
                    var harcBirimiOnaySorumlusuKullaniciId = KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.GetAyar(kayitSilmeBasvuru.EnstituKod).ToInt();
                    var kutuphaneBirimSorumlusu = entities.Kullanicilars.Where(f => f.KullaniciID == kutuphaneBirimiOnaySorumlusuKullaniciId).Select(s => new
                    {
                        s.KullaniciID,
                        s.Unvanlar.UnvanAdi,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.EMail

                    }).FirstOrDefault();
                    var harcBirimSorumlusu = entities.Kullanicilars.Where(f => f.KullaniciID == harcBirimiOnaySorumlusuKullaniciId).Select(s => new
                    {
                        s.KullaniciID,
                        s.Unvanlar.UnvanAdi,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.EMail

                    }).FirstOrDefault();
                    var mModel = new List<SablonMailModel>();
                    if (isOnayOrRet)
                    {
                        if (kutuphaneBirimSorumlusu != null)
                        {
                            mModel.Add(new SablonMailModel
                            {

                                JuriTipAdi = "Kütüphane Birimi",
                                AdSoyad = kutuphaneBirimSorumlusu.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = kutuphaneBirimSorumlusu.EMail, KullaniciId = kutuphaneBirimSorumlusu.KullaniciID, ToOrBcc = true } },
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

                        if (harcBirimSorumlusu != null)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluUnvanAdi", Value = harcBirimSorumlusu.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluAdSoyad", Value = harcBirimSorumlusu.AdSoyad });
                        }
                        if (kutuphaneBirimSorumlusu != null)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@KutuphaneBirimSorumluUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "KutuphaneBirimSorumluUnvanAdi", Value = kutuphaneBirimSorumlusu.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@KutuphaneBirimSorumluAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "KutuphaneBirimSorumluAdSoyad", Value = kutuphaneBirimSorumlusu.AdSoyad });
                        }

                        if (!isOnayOrRet)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@RetAciklama"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "RetAciklama", Value = kayitSilmeBasvuru.HarcBirimiOnayAciklamasi });

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
                    var kutuphaneBirimiOnaySorumlusuKullaniciId = KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.GetAyar(kayitSilmeBasvuru.EnstituKod).ToInt();
                    var harcBirimiOnaySorumlusuKullaniciId = KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.GetAyar(kayitSilmeBasvuru.EnstituKod).ToInt();
                    var kutuphaneBirimSorumlusu = entities.Kullanicilars.Where(f => f.KullaniciID == kutuphaneBirimiOnaySorumlusuKullaniciId).Select(s => new
                    {
                        s.KullaniciID,
                        s.Unvanlar.UnvanAdi,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.EMail

                    }).FirstOrDefault();
                    var harcBirimSorumlusu = entities.Kullanicilars.Where(f => f.KullaniciID == harcBirimiOnaySorumlusuKullaniciId).Select(s => new
                    {
                        s.KullaniciID,
                        s.Unvanlar.UnvanAdi,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.EMail

                    }).FirstOrDefault();
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

                        if (harcBirimSorumlusu != null)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluUnvanAdi", Value = harcBirimSorumlusu.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluAdSoyad", Value = harcBirimSorumlusu.AdSoyad });
                        }
                        if (kutuphaneBirimSorumlusu != null)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@KutuphaneBirimSorumluUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "KutuphaneBirimSorumluUnvanAdi", Value = kutuphaneBirimSorumlusu.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@KutuphaneBirimSorumluAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "KutuphaneBirimSorumluAdSoyad", Value = kutuphaneBirimSorumlusu.AdSoyad });
                        } 
                        if (item.SablonParametreleri.Any(a => a == "@RetAciklama"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "RetAciklama", Value = kayitSilmeBasvuru.KutuphaneBirimiOnayAciklamasi });



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
                    var kutuphaneBirimiOnaySorumlusuKullaniciId = KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.GetAyar(kayitSilmeBasvuru.EnstituKod).ToInt();
                    var harcBirimiOnaySorumlusuKullaniciId = KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.GetAyar(kayitSilmeBasvuru.EnstituKod).ToInt();
                    var kutuphaneBirimSorumlusu = entities.Kullanicilars.Where(f => f.KullaniciID == kutuphaneBirimiOnaySorumlusuKullaniciId).Select(s => new
                    {
                        s.KullaniciID,
                        s.Unvanlar.UnvanAdi,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.EMail

                    }).FirstOrDefault();
                    var harcBirimSorumlusu = entities.Kullanicilars.Where(f => f.KullaniciID == harcBirimiOnaySorumlusuKullaniciId).Select(s => new
                    {
                        s.KullaniciID,
                        s.Unvanlar.UnvanAdi,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.EMail

                    }).FirstOrDefault();
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
                    if (kutuphaneBirimSorumlusu != null)
                    {
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Kütüphane Birimi",
                            AdSoyad = kutuphaneBirimSorumlusu.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = kutuphaneBirimSorumlusu.EMail, KullaniciId = kutuphaneBirimSorumlusu.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isEykYaOrEykDa ? MailSablonTipiEnum.KsEykYaGonderimiReddedildiKutuphaneBirimine : MailSablonTipiEnum.KsEYkDaOnaylanmadiKutuphaneBirimine
                        });
                    }
                    if (harcBirimSorumlusu != null)
                    {
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Harç Birimi",
                            AdSoyad = harcBirimSorumlusu.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = harcBirimSorumlusu.EMail, KullaniciId = harcBirimSorumlusu.KullaniciID, ToOrBcc = true } },
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

                        if (harcBirimSorumlusu != null)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluUnvanAdi", Value = harcBirimSorumlusu.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluAdSoyad", Value = harcBirimSorumlusu.AdSoyad });
                        }
                        if (kutuphaneBirimSorumlusu != null)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@KutuphaneBirimSorumluUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "KutuphaneBirimSorumluUnvanAdi", Value = kutuphaneBirimSorumlusu.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@KutuphaneBirimSorumluAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "KutuphaneBirimSorumluAdSoyad", Value = kutuphaneBirimSorumlusu.AdSoyad });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = kayitSilmeBasvuru.EYKTarihi.ToFormatDate() });
                        if (item.SablonParametreleri.Any(a => a == "@EYKSayisi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKSayisi", Value = kayitSilmeBasvuru.EYKSayisi });
                        if (item.SablonParametreleri.Any(a => a == "@RetAciklama"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "RetAciklama", Value = isEykYaOrEykDa ? kayitSilmeBasvuru.EYKYaGonderimDurumAciklamasi : kayitSilmeBasvuru.EYKDaOnaylanmadiDurumAciklamasi });

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
                    var kutuphaneBirimiOnaySorumlusuKullaniciId = KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.GetAyar(kayitSilmeBasvuru.EnstituKod).ToInt();
                    var harcBirimiOnaySorumlusuKullaniciId = KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.GetAyar(kayitSilmeBasvuru.EnstituKod).ToInt();
                    var kutuphaneBirimSorumlusu = entities.Kullanicilars.Where(f => f.KullaniciID == kutuphaneBirimiOnaySorumlusuKullaniciId).Select(s => new
                    {
                        s.KullaniciID,
                        s.Unvanlar.UnvanAdi,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.EMail

                    }).FirstOrDefault();
                    var harcBirimSorumlusu = entities.Kullanicilars.Where(f => f.KullaniciID == harcBirimiOnaySorumlusuKullaniciId).Select(s => new
                    {
                        s.KullaniciID,
                        s.Unvanlar.UnvanAdi,
                        AdSoyad = s.Ad + " " + s.Soyad,
                        s.EMail

                    }).FirstOrDefault();
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

                        if (harcBirimSorumlusu != null)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluUnvanAdi", Value = harcBirimSorumlusu.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@HarcBirimSorumluAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "HarcBirimSorumluAdSoyad", Value = harcBirimSorumlusu.AdSoyad });
                        }
                        if (kutuphaneBirimSorumlusu != null)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@KutuphaneBirimSorumluUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "KutuphaneBirimSorumluUnvanAdi", Value = kutuphaneBirimSorumlusu.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@KutuphaneBirimSorumluAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "KutuphaneBirimSorumluAdSoyad", Value = kutuphaneBirimSorumlusu.AdSoyad });
                        }
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