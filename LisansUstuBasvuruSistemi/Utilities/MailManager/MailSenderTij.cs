using System;
using System.Collections.Generic;
using System.Linq;
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
                    var enstitu = tijBasvuruOneri.TijBasvuru.Enstituler;
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var ogrenci = tijBasvuruOneri.TijBasvuru.Kullanicilar;
                    var danisman = tijBasvuruOneri.Kullanicilar;
                    var mModel = new List<SablonMailModel>
                        {
                            new SablonMailModel
                            {

                                AdSoyad =danisman.Unvanlar.UnvanAdi+" "+  danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList>
                                    { new MailSendList { EMail = danisman.EMail,KullaniciId =danisman.KullaniciID, ToOrBcc = true } },
                                MailSablonTipId = tijBasvuruOneri.DanismanOnayladi == true
                                    ? MailSablonTipiEnum.TijOneriFormuDanismanTarafindanOnaylandiDanismana
                                    : MailSablonTipiEnum.TijOneriFormuDanismanTarafindanRetEdildiDanismana,
                            }
                        };

                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;
                        
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;

                        if (tijBasvuruOneri.DanismanOnayladi == true)
                        {
                            if (tijBasvuruOneri.TijFormTipID == TijFormTipiEnum.YeniForm)
                                item.Attachments.AddRange(MailReportAttachment.GetTezIzlemeJuriOneriFormuAttachments(tijBasvuruOneri.TijBasvuruOneriID));
                            else item.Attachments.AddRange(MailReportAttachment.GetTezIzlemeJuriDegisiklikFormuAttachments(tijBasvuruOneri.TijBasvuruOneriID));

                        }
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = (tijBasvuruOneri.DonemBaslangicYil + " " + tijBasvuruOneri.DonemBaslangicYil + 1) + " " + tijBasvuruOneri.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tijBasvuruOneri.TijBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@RetTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "RetTarihi", Value = tijBasvuruOneri.DanismanOnayTarihi.ToFormatDateAndTime() });
                        if (item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "RetAciklamasi", Value = tijBasvuruOneri.DanismanOnaylanmamaAciklamasi });


                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstitu.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = contentDetailDto.Title + " (" + item.AdSoyad + ")",
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
                    var enstitu = tijBasvuruOneri.TijBasvuru.Enstituler;
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && p.EnstituKod == enstitu.EnstituKod).ToList();
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
                                    { new MailSendList { EMail = ogrenci.EMail,KullaniciId =danisman.KullaniciID, ToOrBcc = true } },
                                MailSablonTipId = MailSablonTipiEnum.TijOneriFormuEykdaOnaylandiOgrenciye
                            });
                            var jurler = tijBasvuruOneri.TijBasvuruOneriJurilers.Where(p => p.IsYeniOrOnceki && p.IsAsil == true && !p.IsTezDanismani).OrderBy(o => o.RowNum).ToList();
                            mModel.AddRange(jurler.Select(item => new SablonMailModel
                            {
                                JuriTipAdi = "Jüri Üyesi",
                                UnvanAdi = item.UnvanAdi,
                                AdSoyad = item.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                                MailSablonTipId = MailSablonTipiEnum.TijOneriFormuEykdaOnaylandiJuriUyelerine
                            }));
                        }
                        mModel.Add(
                            new SablonMailModel
                            {

                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Unvanlar.UnvanAdi + " " + danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList>
                                    { new MailSendList { EMail = danisman.EMail,KullaniciId =danisman.KullaniciID, ToOrBcc = true } },
                                MailSablonTipId = onaylandi
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
                                    { new MailSendList { EMail = danisman.EMail,KullaniciId =danisman.KullaniciID, ToOrBcc = true } },
                                MailSablonTipId = MailSablonTipiEnum.TijOneriFormuEykyaGonderimiRetEdildiDanismana
                            });
                    }

                    List<TijBasvuruOneriJuriler> juriUyeleri = null;

                    if (mModel.Any(a => a.MailSablonTipId == MailSablonTipiEnum.TijOneriFormuEykdaOnaylandiJuriUyelerine))
                        juriUyeleri = tijBasvuruOneri.TijBasvuruOneriJurilers.Where(p => p.IsYeniOrOnceki && p.IsAsil == true).OrderBy(o => o.RowNum).ToList();

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
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = (tijBasvuruOneri.DonemBaslangicYil + " " + tijBasvuruOneri.DonemBaslangicYil + 1) + " " + tijBasvuruOneri.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tijBasvuruOneri.TijBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.ProgramAdi });
                        if (juriUyeleri != null)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@JuriUyesiAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "JuriUyesiAdSoyad", Value = item.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@JuriUyesiUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "JuriUyesiUnvanAdi", Value = item.UnvanAdi });

                            var juriDanisman = juriUyeleri.First(f => f.IsTezDanismani);
                            if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanBilgi", Value = juriDanisman.UnvanAdi + " " + juriDanisman.AdSoyad });
                            var juriler = juriUyeleri.Where(p => !p.IsTezDanismani).OrderBy(o => o.RowNum).ToList();
                            var juriRowInx = 0;
                            foreach (var itemJuri in juriler)
                            {
                                juriRowInx++;
                                if (item.SablonParametreleri.Any(a => a == "@AsilBilgi" + juriRowInx))
                                    item.MailParameterDtos.Add(new MailParameterDto { Key = "AsilBilgi" + juriRowInx, Value = itemJuri.UnvanAdi + " " + itemJuri.AdSoyad });
                            }
                        }
                        item.MailParameterDtos.Add(new MailParameterDto { Key = item.JuriTipAdi, Value = item.AdSoyad });
                        if (eykDaOnayOrEykYaGonderim)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@EykTarihi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "EykTarihi", Value = tijBasvuruOneri.EYKTarihi.ToFormatDate() });
                        }
                        else
                        {
                            if (item.SablonParametreleri.Any(a => a == "@RetTarihi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "RetTarihi", Value = tijBasvuruOneri.EYKYaGonderildiIslemTarihi.ToFormatDateAndTime() });

                        }

                        if (item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                            item.MailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "RetAciklamasi",
                                Value = eykDaOnayOrEykYaGonderim ? tijBasvuruOneri.EYKDaOnaylanmadiDurumAciklamasi : tijBasvuruOneri.EYKYaGonderimDurumAciklamasi
                            });


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
                    var enstitu = tijBasvuruOneri.TijBasvuru.Enstituler;
                    var sablonlar = entities.MailSablonlaris.Where(p =>p.IsAktif&& p.EnstituKod == enstitu.EnstituKod).ToList();
                    var ogrenci = tijBasvuruOneri.TijBasvuru.Kullanicilar;
                    var danisman = tijBasvuruOneri.Kullanicilar;

                    var mModel = new List<SablonMailModel>
                    {

                        new SablonMailModel
                        {

                            AdSoyad =danisman.Unvanlar.UnvanAdi+" "+  danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList>
                                { new MailSendList { EMail = danisman.EMail,KullaniciId =danisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TijFormuOlusturulduDanismana
                        }
                    };

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
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = (tijBasvuruOneri.DonemBaslangicYil + " " + tijBasvuruOneri.DonemBaslangicYil + 1) + " " + tijBasvuruOneri.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tijBasvuruOneri.TijBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = tijBasvuruOneri.TijBasvuru.Programlar.ProgramAdi });


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