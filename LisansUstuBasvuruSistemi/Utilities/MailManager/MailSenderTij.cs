using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Utilities.MailManager
{
    public class MailSenderTij
    {
        public static MmMessage SendMailDanismanOnay(Guid tijBasvuruOneriUniqueId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
                {

                    #region sendMail

                    var tijBasvuruOneri = entities.TijBasvuruOneris.First(f => f.UniqueID == tijBasvuruOneriUniqueId);
                    var enstituL = tijBasvuruOneri.TijBasvuru.Enstituler;
                    var sablonlar = entities.MailSablonlaris.Where(p => p.EnstituKod == enstituL.EnstituKod).ToList();


                    var ogrenci = tijBasvuruOneri.TijBasvuru.Kullanicilar;
                    var danisman = tijBasvuruOneri.Kullanicilar;
                    var mModel = new List<SablonMailModel>
                        {
                            new SablonMailModel
                            {

                                AdSoyad =danisman.Unvanlar.UnvanAdi+" "+  danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList>
                                    { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = tijBasvuruOneri.DanismanOnayladi == true
                                    ? MailSablonTipiEnum.TijOneriFormuDanismanTarafindanOnaylandiDanismana
                                    : MailSablonTipiEnum.TijOneriFormuDanismanTarafindanRetEdildiDanismana,
                            }
                        };

                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;

                        if (tijBasvuruOneri.DanismanOnayladi == true)
                        {
                            var ids = new List<int?>() { tijBasvuruOneri.TijBasvuruOneriID };
                            var raporTipId = tijBasvuruOneri.TijFormTipID == TijFormTipiEnum.YeniForm
                                ? RaporTipiEnum.TezIzlemeJuriOneriFormu
                                : RaporTipiEnum.TezIzlemeJuriDegisiklikFormu;
                            var ekler = Management.ExportRaporPdf(raporTipId, ids);
                            item.Attachments.AddRange(ekler);
                        }
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var mailParameterDtos = new List<MailParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstituL.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = (tijBasvuruOneri.DonemBaslangicYil + " " + tijBasvuruOneri.DonemBaslangicYil + 1) + " " + tijBasvuruOneri.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tijBasvuruOneri.TijBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@RetTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "RetTarihi", Value = tijBasvuruOneri.DanismanOnayTarihi.ToFormatDateAndTime() });
                        if (item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "RetAciklamasi", Value = tijBasvuruOneri.DanismanOnaylanmamaAciklamasi });


                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstituL.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstituL.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = contentDetailDto.Title + " (" + item.AdSoyad + ")",
                                IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id,
                                IslemYapanIP = UserIdentity.Ip,
                                Aciklama = item.Sablon.Sablon ?? "",
                                AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                                Gonderildi = true,
                                GonderilenMailEkleris = item.Attachments.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList(),
                                GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList()
                            };
                            entities.GonderilenMaillers.Add(kModel);
                            entities.SaveChanges();

                        }
                    }

                    #endregion
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                //mmMessage.Title = "Hata"; 
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailEykOnay(Guid tijBasvuruOneriUniqueId, bool eykDaOnayOrEykYaGonderim, bool onaylandi)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
                {

                    #region sendMail 
                    var tijBasvuruOneri = entities.TijBasvuruOneris.First(f => f.UniqueID == tijBasvuruOneriUniqueId);
                    var enstituL = tijBasvuruOneri.TijBasvuru.Enstituler;
                    var sablonlar = entities.MailSablonlaris.Where(p => p.EnstituKod == enstituL.EnstituKod).ToList();
                    var ogrenci = tijBasvuruOneri.TijBasvuru.Kullanicilar;
                    var danisman = tijBasvuruOneri.Kullanicilar;
                    var mModel = new List<SablonMailModel>();

                    if (eykDaOnayOrEykYaGonderim)
                    {
                        if (onaylandi)
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci",
                                AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                                EMails = new List<MailSendList>
                                    { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipiEnum.TijOneriFormuEykdaOnaylandiOgrenciye
                            });
                            var jurler = tijBasvuruOneri.TijBasvuruOneriJurilers.Where(p => p.IsYeniOrOnceki && p.IsAsil == true && !p.IsTezDanismani).OrderBy(o => o.RowNum).ToList();
                            foreach (var item in jurler)
                            {

                                mModel.Add(new SablonMailModel
                                {
                                    JuriTipAdi = "Jüri Üyesi",
                                    UnvanAdi = item.UnvanAdi,
                                    AdSoyad = item.AdSoyad,
                                    EMails = new List<MailSendList>
                                        { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                                    MailSablonTipID = MailSablonTipiEnum.TijOneriFormuEykdaOnaylandiJuriUyelerine
                                });
                            }
                        }
                        mModel.Add(
                            new SablonMailModel
                            {

                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Unvanlar.UnvanAdi + " " + danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList>
                                    { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = onaylandi == true
                                    ? MailSablonTipiEnum.TijOneriFormuEykdaOnaylandiDanismana
                                    : MailSablonTipiEnum.TijOneriFormuEykdaOnaylanmadiEdildiDanismana
                            });

                    }
                    else if (onaylandi == false)
                    {
                        mModel.Add(
                            new SablonMailModel
                            {

                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Unvanlar.UnvanAdi + " " + danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList>
                                    { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipiEnum.TijOneriFormuEykyaGonderimiRetEdildiDanismana
                            });
                    }

                    List<TijBasvuruOneriJuriler> juriUyeleri = null;

                    if (mModel.Any(a => a.MailSablonTipID == MailSablonTipiEnum.TijOneriFormuEykdaOnaylandiJuriUyelerine))
                        juriUyeleri = tijBasvuruOneri.TijBasvuruOneriJurilers.Where(p => p.IsYeniOrOnceki && p.IsAsil == true).OrderBy(o => o.RowNum).ToList();

                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;

                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var mailParameterDtos = new List<MailParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstituL.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = (tijBasvuruOneri.DonemBaslangicYil + " " + tijBasvuruOneri.DonemBaslangicYil + 1) + " " + tijBasvuruOneri.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tijBasvuruOneri.TijBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.ProgramAdi });
                        if (juriUyeleri != null)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@JuriUyesiAdSoyad"))
                                mailParameterDtos.Add(new MailParameterDto { Key = "JuriUyesiAdSoyad", Value = item.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@JuriUyesiUnvanAdi"))
                                mailParameterDtos.Add(new MailParameterDto { Key = "JuriUyesiUnvanAdi", Value = item.UnvanAdi });

                            var juriDanisman = juriUyeleri.First(f => f.IsTezDanismani);
                            if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                                mailParameterDtos.Add(new MailParameterDto { Key = "DanismanBilgi", Value = juriDanisman.UnvanAdi + " " + juriDanisman.AdSoyad });
                            var juriler = juriUyeleri.Where(p => !p.IsTezDanismani).OrderBy(o => o.RowNum).ToList();
                            var juriRowInx = 0;
                            foreach (var itemJuri in juriler)
                            {
                                juriRowInx++;
                                if (item.SablonParametreleri.Any(a => a == "@AsilBilgi" + juriRowInx))
                                    mailParameterDtos.Add(new MailParameterDto { Key = "AsilBilgi" + juriRowInx, Value = itemJuri.UnvanAdi + " " + itemJuri.AdSoyad });
                            }
                        }
                        mailParameterDtos.Add(new MailParameterDto { Key = item.JuriTipAdi, Value = item.AdSoyad });
                        if (eykDaOnayOrEykYaGonderim)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@EykTarihi"))
                                mailParameterDtos.Add(new MailParameterDto { Key = "EykTarihi", Value = tijBasvuruOneri.EYKTarihi.ToFormatDate() });
                        }
                        else
                        {
                            if (item.SablonParametreleri.Any(a => a == "@RetTarihi"))
                                mailParameterDtos.Add(new MailParameterDto { Key = "RetTarihi", Value = tijBasvuruOneri.EYKYaGonderildiIslemTarihi.ToFormatDateAndTime() });

                        }

                        if (item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                            mailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "RetAciklamasi",
                                Value = eykDaOnayOrEykYaGonderim ? tijBasvuruOneri.EYKDaOnaylanmadiDurumAciklamasi : tijBasvuruOneri.EYKYaGonderimDurumAciklamasi
                            });

                        var attachs = new List<System.Net.Mail.Attachment>();

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstituL.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, attachs);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstituL.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = contentDetailDto.Title + "(" + item.JuriTipAdi + ")" + " (" + item.AdSoyad + ")"
                            };
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = contentDetailDto.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailEkleris = attachs.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList();
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            entities.GonderilenMaillers.Add(kModel);
                            entities.SaveChanges();
                        }
                    }

                    #endregion
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
        public static MmMessage SendMailBasvuruYapildi(Guid tijBasvuruOneriUniqueId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
                {

                    #region sendMail 
                    var tijBasvuruOneri = entities.TijBasvuruOneris.First(f => f.UniqueID == tijBasvuruOneriUniqueId);
                    var enstituL = tijBasvuruOneri.TijBasvuru.Enstituler;
                    var sablonlar = entities.MailSablonlaris.Where(p => p.EnstituKod == enstituL.EnstituKod).ToList();
                    var ogrenci = tijBasvuruOneri.TijBasvuru.Kullanicilar;
                    var danisman = tijBasvuruOneri.Kullanicilar;

                    var mModel = new List<SablonMailModel>
                    {

                        new SablonMailModel
                        {

                            AdSoyad =danisman.Unvanlar.UnvanAdi+" "+  danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList>
                                { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TijFormuOlusturulduDanismana
                        }
                    };

                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;

                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var mailParameterDtos = new List<MailParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstituL.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = (tijBasvuruOneri.DonemBaslangicYil + " " + tijBasvuruOneri.DonemBaslangicYil + 1) + " " + tijBasvuruOneri.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tijBasvuruOneri.TijBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.ProgramAdi });
                        var attachs = new List<System.Net.Mail.Attachment>();

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstituL.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, attachs);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = enstituL.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = contentDetailDto.Title + " (" + item.AdSoyad + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = contentDetailDto.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailEkleris = attachs.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList();
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            entities.GonderilenMaillers.Add(kModel);
                            entities.SaveChanges();
                        }
                    }

                    #endregion
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