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
    public class MailSenderDp
    {
        public static MmMessage SendMailBasvuruBilgisi(int donemProjesiBasvuruId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var donemProjesiBasvuru = entities.DonemProjesiBasvurus.First(p => p.DonemProjesiBasvuruID == donemProjesiBasvuruId);
                    var ogrenci = donemProjesiBasvuru.DonemProjesi.Kullanicilar;
                    var projeYurutucusu = donemProjesiBasvuru.Kullanicilar;
                    var enstitu = donemProjesiBasvuru.DonemProjesi.Enstituler;
                    var anabilimDali = donemProjesiBasvuru.DonemProjesi.Programlar.AnabilimDallari;
                    var program = donemProjesiBasvuru.DonemProjesi.Programlar;

                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {

                            JuriTipAdi = "Proje Yürütücüsü",
                            UnvanAdi = projeYurutucusu.Unvanlar.UnvanAdi,
                            AdSoyad = projeYurutucusu.Ad+" "+projeYurutucusu.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = projeYurutucusu.EMail, KullaniciId = projeYurutucusu.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.DpBasvuruYapildiYurutucuye
                        }
                    };
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
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = donemProjesiBasvuru.DonemProjesi.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuUnvanAdi", Value = projeYurutucusu.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuAdSoyad", Value = projeYurutucusu.Ad + " " + projeYurutucusu.Soyad });
                         
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
                var message = "Dönem Projesi başvuru bilgileri mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailEnstituOnay(int donemProjesiBasvuruId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var donemProjesiBasvuru = entities.DonemProjesiBasvurus.First(p => p.DonemProjesiBasvuruID == donemProjesiBasvuruId);
                    var ogrenci = donemProjesiBasvuru.DonemProjesi.Kullanicilar;
                    var projeYurutucusu = donemProjesiBasvuru.Kullanicilar;
                    var enstitu = donemProjesiBasvuru.DonemProjesi.Enstituler;
                    var anabilimDali = donemProjesiBasvuru.DonemProjesi.Programlar.AnabilimDallari;
                    var program = donemProjesiBasvuru.DonemProjesi.Programlar;

                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad+" "+ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId =donemProjesiBasvuru.DonemProjesiEnstituOnayDurumID==DonemProjesiEnstituOnayDurumEnum.Reddedildi ? MailSablonTipiEnum.DpBasvuruReddedildiOgrenciye:MailSablonTipiEnum.DpBasvuruIptalEdildiOgrenciye
                        },
                        new SablonMailModel
                        {
                            UnvanAdi = projeYurutucusu.Unvanlar.UnvanAdi,
                            AdSoyad = projeYurutucusu.Ad + " " + projeYurutucusu.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = projeYurutucusu.EMail, KullaniciId = projeYurutucusu.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId =donemProjesiBasvuru.DonemProjesiEnstituOnayDurumID==DonemProjesiEnstituOnayDurumEnum.Reddedildi ? MailSablonTipiEnum.DpBasvuruReddedildiYurutucuye:MailSablonTipiEnum.DpBasvuruIptalEdildiYurutucuye,
                            JuriTipAdi = "Proje Yürütücüsü"
                        }
                    };
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
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = donemProjesiBasvuru.DonemProjesi.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuUnvanAdi", Value = projeYurutucusu.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuAdSoyad", Value = projeYurutucusu.Ad + " " + projeYurutucusu.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@Gerekce"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "Gerekce", Value = donemProjesiBasvuru.EnstituOnayAciklama });

                         
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
                var message = "Dönem Projesi enstitü başvuru ret/iptal bilgisi mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailYurutucuOnay(int donemProjesiBasvuruId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var donemProjesiBasvuru = entities.DonemProjesiBasvurus.First(p => p.DonemProjesiBasvuruID == donemProjesiBasvuruId);

                    var ogrenci = donemProjesiBasvuru.DonemProjesi.Kullanicilar;
                    var projeYurutucusu = donemProjesiBasvuru.Kullanicilar;
                    var enstitu = donemProjesiBasvuru.DonemProjesi.Enstituler;
                    var anabilimDali = donemProjesiBasvuru.DonemProjesi.Programlar.AnabilimDallari;
                    var program = donemProjesiBasvuru.DonemProjesi.Programlar;

                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad+" "+ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = donemProjesiBasvuru.IsDanismanOnay.Value ? MailSablonTipiEnum.DpProjeYurutucusuOnayladiOgrenciye:MailSablonTipiEnum.DpProjeYurutucusuRetEttiOgrenciye
                        }
                    };
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
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = donemProjesiBasvuru.DonemProjesi.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuUnvanAdi", Value = projeYurutucusu.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuAdSoyad", Value = projeYurutucusu.Ad + " " + projeYurutucusu.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@Aciklama"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "Aciklama", Value = donemProjesiBasvuru.DanismanOnayAciklama });

                         
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
                var message = "Dönem Projesi danışman onay bilgisi mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailSinavBilgisi(int? srTalepId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var srTalebi = entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);
                    var donemProjesiBasvuru = srTalebi.DonemProjesiBasvuru;
                    var ogrenci = donemProjesiBasvuru.DonemProjesi.Kullanicilar;
                    var projeYurutucusu = donemProjesiBasvuru.Kullanicilar;
                    var enstitu = donemProjesiBasvuru.DonemProjesi.Enstituler;
                    var anabilimDali = donemProjesiBasvuru.DonemProjesi.Programlar.AnabilimDallari;
                    var program = donemProjesiBasvuru.DonemProjesi.Programlar;

                    var juriler = donemProjesiBasvuru.DonemProjesiJurileris.ToList();

                    var mModel = new List<SablonMailModel>();

                    if (srTalepId.HasValue)
                    {
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = donemProjesiBasvuru.DonemProjesi.Kullanicilar.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.DpToplantiBilgiOgrenciye
                        });
                        mModel.AddRange(juriler.Select(item => new SablonMailModel
                        {
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = (item.IsTezDanismani ? donemProjesiBasvuru.TezDanismanID : (int?)null), ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.DpToplantiBilgiJurilere,
                            JuriTipAdi = item.IsTezDanismani ? "Proje Yürütücüsü" : "Jüri Üyesi"
                        }));
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
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = donemProjesiBasvuru.DonemProjesi.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuUnvanAdi", Value = projeYurutucusu.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuAdSoyad", Value = projeYurutucusu.Ad + " " + projeYurutucusu.Soyad });

                        #region SR Talebi
                        if (item.MailSablonTipId == MailSablonTipiEnum.DpToplantiBilgiOgrenciye || item.MailSablonTipId == MailSablonTipiEnum.DpToplantiBilgiJurilere)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavTarihi", Value = srTalebi.Tarih.ToLongDateString() });
                            if (item.SablonParametreleri.Any(a => a == "@SinavSaati"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavSaati", Value = srTalebi.BasSaat.ToFormatTime() });


                            if (item.SablonParametreleri.Any(a => a == "@SinavSekli"))
                            {
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavSekli", Value = srTalebi.IsOnline ? "Çevrim İçi" : "Yüz Yüze" });
                            }

                            if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                            {
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavYeri", Value = srTalebi.SalonAdi, IsLink = srTalebi.IsOnline });
                            }
                        }
                        #endregion
                        #region DanismanKomite
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucuBilgi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucuBilgi", Value = projeYurutucusu.Unvanlar.UnvanAdi + " " + projeYurutucusu.Ad + " " + projeYurutucusu.Soyad });

                        foreach (var itemTik in juriler.Where(p => !p.IsTezDanismani).Select((s, inx) => new { s, inx = inx + 1 }))
                        {

                            if (item.SablonParametreleri.Any(a => a == "@JuriUyeBilgi" + itemTik.inx))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "JuriUyeBilgi" + itemTik.inx, Value = itemTik.s.UnvanAdi + " " + itemTik.s.AdSoyad });

                        }
                        #endregion

                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;
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
                var message = "Dönem Projesi Sınavı toplantısı için Jüri üyelerine mail gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailDegerlendirmeLink(int donemProjesiBasvuruId, Guid? uniqueId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var donemProjesiBasvuru = entities.DonemProjesiBasvurus.First(p => p.DonemProjesiBasvuruID == donemProjesiBasvuruId);
                    var srTalebi = donemProjesiBasvuru.SRTalepleris.First();
                    var qjuriler = donemProjesiBasvuru.DonemProjesiJurileris.AsQueryable();
                    qjuriler = uniqueId.HasValue ? qjuriler.Where(p => p.UniqueID == uniqueId) : qjuriler.Where(p => !p.IsTezDanismani);
                    var juriler = qjuriler.ToList();
                    var mModel = new List<SablonMailModel>();

                    var enstitu = donemProjesiBasvuru.DonemProjesi.Enstituler;

                    var anabilimDali = donemProjesiBasvuru.DonemProjesi.Programlar.AnabilimDallari;
                    var program = donemProjesiBasvuru.DonemProjesi.Programlar;
                    var ogrenci = donemProjesiBasvuru.DonemProjesi.Kullanicilar;

                    foreach (var item in juriler)
                    {
                        item.UniqueID = Guid.NewGuid();
                    }

                    mModel.AddRange(juriler.Select(item => new SablonMailModel
                    {
                        UniqueId = item.UniqueID,
                        UnvanAdi = item.UnvanAdi,
                        AdSoyad = item.AdSoyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = (item.IsTezDanismani ? donemProjesiBasvuru.TezDanismanID : (int?)null), ToOrBcc = true } },
                        MailSablonTipId = MailSablonTipiEnum.DpDegerlendirmeLinkGonderimJurilere,
                        JuriTipAdi = item.IsTezDanismani ? "Proje Yürütücüsü" : "Jüri Üyesi"
                    }));

                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;

                        var juri = juriler.FirstOrDefault(p => p.UniqueID == item.UniqueId);
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;

                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);
                        if (!donemProjesiBasvuru.IntihalRaporuDosyaYolu.IsNullOrWhiteSpace()) item.SablonEkleri.Add(new MailSablonlariEkleri { EkAdi = ogrenci.Ad + " " + ogrenci.Soyad + " - Benzerlik Raporu Pdf Dosyası ", EkDosyaYolu = donemProjesiBasvuru.IntihalRaporuDosyaYolu });
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = donemProjesiBasvuru.DonemProjesi.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });

                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/DpBasvuru/Index?IsDegerlendirme=" + item.UniqueId, IsLink = true });
                        }
                        #region SR Talebi

                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavTarihi", Value = srTalebi.Tarih.ToLongDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@SinavSaati"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavSaati", Value = srTalebi.BasSaat.ToFormatTime() });


                        if (item.SablonParametreleri.Any(a => a == "@SinavSekli"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavSekli", Value = srTalebi.IsOnline ? "Çevrim İçi" : "Yüz Yüze" });
                        }

                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavYeri", Value = srTalebi.SalonAdi, IsLink = srTalebi.IsOnline });
                        }

                        #endregion

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

                        juri.DegerlendirmeIslemTarihi = null;
                        juri.DegerlendirmeIslemYapanIP = null;
                        juri.DegerlendirmeYapanID = null;
                        juri.DonemProjesiJuriOnayDurumID = null;
                        juri.Aciklama = null;
                        juri.IsLinkGonderildi = true;
                        juri.LinkGonderimTarihi = DateTime.Now;
                        juri.LinkGonderenID = UserIdentity.Current.Id;


                        entities.SaveChanges();
                        LogIslemleri.LogEkle("DonemProjesiJurileri", LogCrudType.Update, juri.ToJson());
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {

                var message = "Dönem Projesi Sınavı değerlendirmesi için Jüri üyelerine değerlendirme davetiye linki mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailSinavSonucBilgisi(int donemProjesiBasvuruId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var donemProjesiBasvuru = entities.DonemProjesiBasvurus.First(f => f.DonemProjesiBasvuruID == donemProjesiBasvuruId);
                    var ogrenci = donemProjesiBasvuru.DonemProjesi.Kullanicilar;
                    var projeYurutucusu = donemProjesiBasvuru.Kullanicilar;
                    var enstitu = donemProjesiBasvuru.DonemProjesi.Enstituler;
                    var anabilimDali = donemProjesiBasvuru.DonemProjesi.Programlar.AnabilimDallari;
                    var program = donemProjesiBasvuru.DonemProjesi.Programlar;


                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.DpSinavSonucuGonderimOgrenciye,
                            Attachments = MailReportAttachment.GetDpSinavTutanagiAttachments(donemProjesiBasvuruId,false),
                        },
                        new SablonMailModel
                        {
                            UnvanAdi = projeYurutucusu.Unvanlar.UnvanAdi,
                            AdSoyad = projeYurutucusu.Ad + " " + projeYurutucusu.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = projeYurutucusu.EMail, KullaniciId = projeYurutucusu.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.DpSinavSonucuGonderimYurutucuye,
                            Attachments = MailReportAttachment.GetDpSinavTutanagiAttachments(donemProjesiBasvuruId,true),
                            JuriTipAdi = "Proje Yürütücüsü"
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
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = donemProjesiBasvuru.DonemProjesi.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuUnvanAdi", Value = projeYurutucusu.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuAdSoyad", Value = projeYurutucusu.Ad + " " + projeYurutucusu.Soyad });


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
                var message = "Dönem Projesi Sınavı sonucu bilgisi mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }

        public static MmMessage SendMailEykOnaylanmadi(int donemProjesiBasvuruId, bool isEykYaOrEykDa)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var donemProjesiBasvuru = entities.DonemProjesiBasvurus.First(f => f.DonemProjesiBasvuruID == donemProjesiBasvuruId);
                    var ogrenci = donemProjesiBasvuru.DonemProjesi.Kullanicilar;
                    var projeYurutucusu = donemProjesiBasvuru.Kullanicilar;
                    var enstitu = donemProjesiBasvuru.DonemProjesi.Enstituler;
                    var anabilimDali = donemProjesiBasvuru.DonemProjesi.Programlar.AnabilimDallari;
                    var program = donemProjesiBasvuru.DonemProjesi.Programlar;


                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId =isEykYaOrEykDa? MailSablonTipiEnum.DpEykYaGonderimiReddedildiOgrenciye:MailSablonTipiEnum.DpEykDaOnaylanmadiOgrenciye,
                            Attachments = MailReportAttachment.GetDpSinavTutanagiAttachments(donemProjesiBasvuruId,false),
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
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = donemProjesiBasvuru.DonemProjesi.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuUnvanAdi", Value = projeYurutucusu.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuAdSoyad", Value = projeYurutucusu.Ad + " " + projeYurutucusu.Soyad });


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
                var message = "Dönem Projesi Eyk Onay sonucu bilgisi mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }


        public static async Task SendMailDegerlendirmeHatirlatma()
        {
            try
            {
                using (var entities = new LubsDbEntities())
                {

                    var nowDate = DateTime.Now;

                    var donemProjesiBasvurulari =
                        (from donemProjesiBasvuru in entities.DonemProjesiBasvurus
                         join donemProjesi in entities.DonemProjesis on donemProjesiBasvuru.DonemProjesiID equals donemProjesi.DonemProjesiID
                         join enst in entities.Enstitulers on donemProjesi.EnstituKod equals enst.EnstituKod
                         join ogrenci in entities.Kullanicilars on donemProjesiBasvuru.DonemProjesi.KullaniciID equals ogrenci.KullaniciID
                         join danisman in entities.Kullanicilars on donemProjesiBasvuru.TezDanismanID equals danisman.KullaniciID
                         join sonRezervasyon in entities.SRTalepleris on donemProjesiBasvuru.DonemProjesiBasvuruID equals sonRezervasyon.DonemProjesiBasvuruID
                         where donemProjesiBasvuru.DonemProjesiDurumID == DonemProjesiDurumEnum.SinavDegerlendirmeSureci && !donemProjesiBasvuru.IsDanismanDegerlendirmeHatirlatmasiYapildi.HasValue
                                && donemProjesiBasvuru.DonemProjesiJurileris.Any(a => a.IsTezDanismani && !a.DonemProjesiJuriOnayDurumID.HasValue)
                                && DbFunctions.DiffHours(DbFunctions.CreateDateTime(sonRezervasyon.Tarih.Year, sonRezervasyon.Tarih.Month, sonRezervasyon.Tarih.Day, sonRezervasyon.BasSaat.Hours, sonRezervasyon.BasSaat.Minutes, 0), nowDate) >= 24

                         select new
                         {
                             enst.EnstituKod,
                             enst.EnstituAd,
                             enst.WebAdresi,
                             enst.SistemErisimAdresi,
                             donemProjesiBasvuru.DonemProjesiBasvuruID,
                             donemProjesi.OgrenimTipKod,
                             donemProjesi.OgrenciNo,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             OgrenciEmail = ogrenci.EMail,
                             donemProjesiBasvuru.TezDanismanID,
                             DanismanAdSoyad = danisman.Ad + " " + danisman.Soyad,
                             DanismanUnvanAdi = danisman.Unvanlar.UnvanAdi,
                             DanismanEmail = danisman.EMail,
                             SinavTarihi = sonRezervasyon.Tarih,
                             SinavSaati = sonRezervasyon.BasSaat,
                             donemProjesiBasvuru,
                             Sure1 = DbFunctions.DiffHours(DbFunctions.CreateDateTime(sonRezervasyon.Tarih.Year, sonRezervasyon.Tarih.Month, sonRezervasyon.Tarih.Day, sonRezervasyon.BasSaat.Hours, sonRezervasyon.BasSaat.Minutes, 0), nowDate),
                         }).ToList();
                    if (!donemProjesiBasvurulari.Any()) return;
                    var sablonTipIds = new List<int> { MailSablonTipiEnum.DpDegerlendirmeHatirlatmaYurutucuye };
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in donemProjesiBasvurulari)
                    {
                        var mailSablonTipId = MailSablonTipiEnum.DpDegerlendirmeHatirlatmaYurutucuye;

                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod && p.MailSablonTipID == mailSablonTipId),
                            EnstituAdi = basvuru.EnstituAd,
                            WebAdresi = basvuru.WebAdresi,
                            SistemErisimAdresi = basvuru.SistemErisimAdresi
                        };
                        if (item.Sablon == null) continue;
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());

                        item.EMails.Add(new MailSendList { EMail = basvuru.DanismanEmail, KullaniciId = basvuru.TezDanismanID, ToOrBcc = true });


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuUnvanAdi", Value = basvuru.DanismanUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProjeYurutucusuAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProjeYurutucusuAdSoyad", Value = basvuru.DanismanAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavTarihi", Value = basvuru.SinavTarihi.ToFormatDate() });
                        if (item.SablonParametreleri.Any(a => a == "@SinavSaati"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavSaati", Value = basvuru.SinavSaati.ToFormatTime() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;

                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title += " (" + basvuru.DanismanAdSoyad + ")",
                            IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId,
                            IslemYapanIP = "::1",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris,
                            GonderilenMailEkleris = item.GetGonderilenMailEkleris
                        };

                        entities.GonderilenMaillers.Add(kModel);
                        basvuru.donemProjesiBasvuru.IsDanismanDegerlendirmeHatirlatmasiYapildi = true;
                        basvuru.donemProjesiBasvuru.DanismanDegerlendirmeHatirlatmasiTarihi = DateTime.Now;


                    }

                    await entities.SaveChangesAsync();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet("MailTaskRunner.SendMailDegerlendirmeHatirlatma() => " + donemProjesiBasvurulari.Count + " başvuru için bilgilendirme maili gönderildi.", ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);
                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Dönem projesi Proje Yürütücüsüne değerlendirme hatırlatması bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata, GlobalSistemSetting.SystemDefaultAdminKullaniciId, "::1");
            }
        }
    }
}