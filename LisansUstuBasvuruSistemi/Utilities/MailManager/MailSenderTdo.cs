using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.MailManager
{
    public class MailSenderTdo
    {
        public static MmMessage SendMailTdoBilgisi(int tdoBasvuruDanismanId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var tdoBasvuruDanisman = entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
                    var tdoDanismanTalepTipId = tdoBasvuruDanisman.TDODanismanTalepTipID;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();

                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniOnerisi)
                    {
                        var danisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoDanismanOnerisiYapildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoDanismanOnerisiYapildiOgrenci
                        });
                    }
                    else if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniDegisikligi)
                    {
                        var varolanDanisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.VarolanTezDanismanID);
                        var yeniDanisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Varolan Danışman",
                            AdSoyad = varolanDanisman.Ad + " " + varolanDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = varolanDanisman.EMail, KullaniciId = varolanDanisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoTezDanismanDegisikligiVarolanDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, KullaniciId = yeniDanisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoTezDanismanDegisikligiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoTezDanismanDegisikligiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi)
                    {
                        var varolanDanisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.VarolanTezDanismanID);
                        var yeniDanisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Varolan Danışman",
                            AdSoyad = varolanDanisman.Ad + " " + varolanDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = varolanDanisman.EMail, KullaniciId = varolanDanisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiVarolanDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, KullaniciId = yeniDanisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezBasligiDegisikligi)
                    {
                        var danisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoTezBasligiDegisikligiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = danisman.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoTezBasligiDegisikligiOgrenci
                        });
                    }




                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = entities.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;
                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));



                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "VarolanDanismanUnvanAdi", Value = tdoBasvuruDanisman.VarolanTDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "VarolanDanismanAdSoyad", Value = tdoBasvuruDanisman.VarolanTDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniDanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniDanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezDili", Value = tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce" });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });

                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikTr"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniTezBaslikTr", Value = tdoBasvuruDanisman.YeniTezBaslikTr });

                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikEn"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniTezBaslikEn", Value = tdoBasvuruDanisman.YeniTezBaslikEn });

                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            var isOgrenci = item.JuriTipAdi == "Öğrenci";
                            item.MailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "Link",
                                Value = $"{enstitu.SistemErisimAdresi}/{(isOgrenci ? "TDOBasvurular" : "TDOGelenBasvurular")}/Index?TDOBasvuruID={tdoBasvuruDanisman.TDOBasvuruID}",
                                IsLink = true
                            });
                        }
                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            isSended = true;
                            if (!item.AdSoyad.IsNullOrWhiteSpace()) contentDetailDto.Title += " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) contentDetailDto.Title += " (" + item.JuriTipAdi + ")";
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstitu.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = contentDetailDto.Title,
                                IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId,
                                IslemYapanIP = "::",
                                Aciklama = item.Sablon.Sablon ?? "",
                                AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                                Gonderildi = true,
                                GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris,
                                GonderilenMailEkleris = item.GetGonderilenMailEkleris
                            };
                            entities.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (isSended) entities.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOBasvuruDanismanID:" + tdoBasvuruDanismanId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailTdoDanismanOnay(int tdoBasvuruDanismanId, bool isOnayOrRed)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var tdoBasvuruDanisman = entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
                    var tdoDanismanTalepTipId = tdoBasvuruDanisman.TDODanismanTalepTipID;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();
                    var attachments = new List<Attachment>();
                    if (isOnayOrRed)
                    {
                        attachments = tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniOnerisi
                            ? MailReportAttachment.GetTezDanismanOneriFormuAttachments(tdoBasvuruDanismanId)
                            : MailReportAttachment.GetTezDanismanDegisiklikFormuAttachments(tdoBasvuruDanismanId);
                    }

                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniOnerisi)
                    {
                        if (isOnayOrRed)
                        {
                            var danisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                                MailSablonTipId = MailSablonTipiEnum.TdoDanismanOnerisiOnaylandiDanisman,
                                Attachments = attachments
                            });
                        }
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoDanismanOnerisiOnaylandiOgrenci : MailSablonTipiEnum.TdoDanismanOnerisiReddedildiOgrenci,
                            Attachments = attachments
                        });
                    }
                    else if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniDegisikligi)
                    {
                        var yeniDanisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, KullaniciId = yeniDanisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanDegisikligiOnaylandiYeniDanisman : MailSablonTipiEnum.TdoTezDanismanDegisikligiRetEdildiYeniDanisman,
                            Attachments = attachments

                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanDegisikligiOnaylandiOgrenci : MailSablonTipiEnum.TdoTezDanismanDegisikligiRetEdildiOgrenci,
                            Attachments = attachments

                        });
                    }
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi)
                    {
                        var yeniDanisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, KullaniciId = yeniDanisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiOnaylandiYeniDanisman : MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiRetEdildiYeniDanisman,
                            Attachments = attachments
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiOnaylandiOgrenci : MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiRetEdildiOgrenci,
                            Attachments = attachments
                        });
                    }
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezBasligiDegisikligi)
                    {
                        var danisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoTezBasligiDegisikligiOnaylandiDanisman : MailSablonTipiEnum.TdoTezBasligiDegisikligiRetEdildiDanisman,
                            Attachments = attachments,
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoTezBasligiDegisikligiOnaylandiOgrenci : MailSablonTipiEnum.TdoTezBasligiDegisikligiRetEdildiOgrenci,
                            Attachments = attachments
                        });
                    }



                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = entities.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    var snded = false;

                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "VarolanDanismanUnvanAdi", Value = tdoBasvuruDanisman.VarolanTDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "VarolanDanismanAdSoyad", Value = tdoBasvuruDanisman.VarolanTDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniDanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniDanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikTr"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniTezBaslikTr", Value = tdoBasvuruDanisman.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikEn"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniTezBaslikEn", Value = tdoBasvuruDanisman.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezDili", Value = (tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });

                        if (tdoBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }
                        if (!isOnayOrRed)
                        {
                            string retAciklama;

                            if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezBasligiDegisikligi || tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniOnerisi) retAciklama = tdoBasvuruDanisman.DanismanOnaylanmadiAciklama;
                            else retAciklama = tdoBasvuruDanisman.VarolanDanismanOnaylanmadiAciklama;

                            if (item.SablonParametreleri.Any(a => a == "@RetAciklama"))
                            {
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "RetAciklama", Value = retAciklama });
                            }
                        }
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            var isOgrenci = item.JuriTipAdi == "Öğrenci";
                            item.MailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "Link",
                                Value = $"{enstitu.SistemErisimAdresi}/{(isOgrenci ? "TDOBasvurular" : "TDOGelenBasvurular")}/Index?TDOBasvuruID={tdoBasvuruDanisman.TDOBasvuruID}",
                                IsLink = true
                            }); 
                        }

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
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
                                IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId,
                                IslemYapanIP = "::",
                                Aciklama = item.Sablon.Sablon ?? "",
                                AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                                Gonderildi = true,
                                GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris,
                                GonderilenMailEkleris = item.GetGonderilenMailEkleris
                            };
                            entities.GonderilenMaillers.Add(kModel);

                        }
                    }

                    if (snded) entities.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOBasvuruDanismanID:" + tdoBasvuruDanismanId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailTdoEykOnay(int tdoBasvuruDanismanId, bool isOnayOrRed)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var tdoBasvuruDanisman = entities.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
                    var tdoDanismanTalepTipId = tdoBasvuruDanisman.TDODanismanTalepTipID;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniOnerisi)
                    {

                        var danisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoDanismanOnerisiEykDaOnaylandiDanisman : MailSablonTipiEnum.TdoDanismanOnerisiEykDaReddedildiOgrenciDanisman
                        });

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoDanismanOnerisiEykDaOnaylandiOgrenci : MailSablonTipiEnum.TdoDanismanOnerisiEykDaReddedildiOgrenciDanisman
                        });
                    }
                    else if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniDegisikligi)
                    {
                        var yeniDanisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, KullaniciId = yeniDanisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanDegisikligiEykDaOnaylandiYeniDanisman : MailSablonTipiEnum.TdoTezDanismanDegisikligiEykDaRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanDegisikligiEykDaOnaylandiOgrenci : MailSablonTipiEnum.TdoTezDanismanDegisikligiEykDaRetEdildiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi)
                    {
                        var yeniDanisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, KullaniciId = yeniDanisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiEykDaOnaylandiYeniDanisman : MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiEykDaRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiEykDaOnaylandiOgrenci : MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiEykDaRetEdildiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezBasligiDegisikligi)
                    {
                        var danisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoTezBasligiDegisikligiEykDaOnaylandiDanisman : MailSablonTipiEnum.TdoTezBasligiDegisikligiEykDaRetEdildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = isOnayOrRed ? MailSablonTipiEnum.TdoTezBasligiDegisikligiEykDaOnaylandiOgrenci : MailSablonTipiEnum.TdoTezBasligiDegisikligiEykDaRetEdildiOgrenci
                        });
                    }
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).ToList();

                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = entities.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;

                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "VarolanDanismanUnvanAdi", Value = tdoBasvuruDanisman.VarolanTDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "VarolanDanismanAdSoyad", Value = tdoBasvuruDanisman.VarolanTDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniDanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniDanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikTr"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniTezBaslikTr", Value = tdoBasvuruDanisman.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikEn"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniTezBaslikEn", Value = tdoBasvuruDanisman.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezDili", Value = (tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });

                        if (tdoBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }
                        if (!isOnayOrRed)
                        {
                            var retAciklama = tdoBasvuruDanisman.EYKDaOnaylanmadiDurumAciklamasi;

                            if (item.SablonParametreleri.Any(a => a == "@RetAciklama"))
                            {
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "RetAciklama", Value = retAciklama });
                            }
                        }
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            var isOgrenci = item.JuriTipAdi == "Öğrenci";
                            item.MailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "Link",
                                Value = $"{enstitu.SistemErisimAdresi}/{(isOgrenci ? "TDOBasvurular" : "TDOGelenBasvurular")}/Index?TDOBasvuruID={tdoBasvuruDanisman.TDOBasvuruID}",
                                IsLink = true
                            });
                        }
                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            isSended = true;
                            if (!item.AdSoyad.IsNullOrWhiteSpace()) contentDetailDto.Title += " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) contentDetailDto.Title += " (" + item.JuriTipAdi + ")";
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstitu.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = contentDetailDto.Title,
                                IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId,
                                IslemYapanIP = "::",
                                Aciklama = item.Sablon.Sablon ?? "",
                                AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                                Gonderildi = true,
                                GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris,
                                GonderilenMailEkleris = item.GetGonderilenMailEkleris
                            };
                            entities.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (isSended) entities.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOBasvuruDanismanID:" + tdoBasvuruDanismanId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailTdoEsBilgisi(int tdoBasvuruEsDanismanId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var esDanisman = entities.TDOBasvuruEsDanismen.First(p => p.TDOBasvuruEsDanismanID == tdoBasvuruEsDanismanId);
                    var tdoBasvuruDanisman = esDanisman.TDOBasvuruDanisman;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var danisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();

                    var attachments =
                        MailReportAttachment.GetTezEsDanismanOneriFormuAttachments(tdoBasvuruEsDanismanId);
                    if (esDanisman.IsDegisiklikTalebi)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoEsDanismanDegisikligiYapildiDanisman,
                            Attachments = attachments
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoEsDanismanDegisikligiYapildiOgrenci,
                            Attachments = attachments
                        });
                    }
                    else
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoEsDanismanOnerisiYapildiDanisman,
                            Attachments = attachments
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.TdoEsDanismanOnerisiYapildiOgrenci,
                            Attachments = attachments
                        });
                    }



                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var program = tdoBasvuru.Programlar;
                    var ogrenimTipi = entities.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;
                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));



                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrenimTipi.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = program.AnabilimDallari.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EsDanismanUnvanAdi", Value = esDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EsDanismanAdSoyad", Value = esDanisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUniversite"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EsDanismanUniversite", Value = esDanisman.UniversiteAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanEsDanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "VarolanEsDanismanAdSoyad", Value = esDanisman.OncekiEsDanismanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniEsDanismanUnvanAdi", Value = esDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniEsDanismanAdSoyad", Value = esDanisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = esDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezDili", Value = (tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            var isOgrenci = item.JuriTipAdi == "Öğrenci";
                            item.MailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "Link",
                                Value = $"{enstitu.SistemErisimAdresi}/{(isOgrenci ? "TDOBasvurular" : "TDOGelenBasvurular")}/Index?TDOBasvuruID={tdoBasvuruDanisman.TDOBasvuruID}",
                                IsLink = true
                            });
                        }


                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            isSended = true;
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
                    }
                    if (isSended) entities.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez Eş danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailTdoEsEykOnay(int tDoBasvuruEsDanismanId, bool isOnayOrRed)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var esDanisman = entities.TDOBasvuruEsDanismen.First(p => p.TDOBasvuruEsDanismanID == tDoBasvuruEsDanismanId);
                    var tdoBasvuruDanisman = esDanisman.TDOBasvuruDanisman;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var danisman = entities.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();

                    if (esDanisman.IsDegisiklikTalebi)
                    {
                        if (!isOnayOrRed)
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci, Danışman",
                                AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad + " , " + danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true }, new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                                MailSablonTipId = MailSablonTipiEnum.TdoEsDanismanDegisikligiEykDaRetEdildiOgrenciDanisman
                            });
                        }
                        else
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                                MailSablonTipId = MailSablonTipiEnum.TdoEsDanismanDegisikligiEykDaOnaylandiDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Eş Danışman",
                                AdSoyad = esDanisman.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = esDanisman.EMail, ToOrBcc = true } },
                                MailSablonTipId = MailSablonTipiEnum.TdoEsDanismanDegisikligiEykDaOnaylandiEsDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci",
                                AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                                MailSablonTipId = MailSablonTipiEnum.TdoEsDanismanDegisikligiEykDaOnaylandiOgrenci
                            });
                        }
                    }
                    else
                    {
                        if (!isOnayOrRed)
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci, Danışman",
                                AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad + " , " + danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true }, new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                                MailSablonTipId = MailSablonTipiEnum.TdoEsDanismanOnerisiEykDaReddedildiOgrenciDanisman
                            });
                        }
                        else
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                                MailSablonTipId = MailSablonTipiEnum.TdoEsDanismanOnerisiEykDaOnaylandiDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Eş Danışman",
                                AdSoyad = esDanisman.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = esDanisman.EMail, ToOrBcc = true } },
                                MailSablonTipId = MailSablonTipiEnum.TdoEsDanismanOnerisiEykDaOnaylandiEsDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci",
                                AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                                MailSablonTipId = MailSablonTipiEnum.TdoEsDanismanOnerisiEykDaOnaylandiOgrenci
                            });
                        }
                    }


                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = entities.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;
                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SistemErisimAdresi = enstitu.SistemErisimAdresi;

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));



                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });


                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EsDanismanUnvanAdi", Value = esDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EsDanismanAdSoyad", Value = esDanisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUniversite"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EsDanismanUniversite", Value = esDanisman.UniversiteAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanEsDanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "VarolanEsDanismanAdSoyad", Value = esDanisman.OncekiEsDanismanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniEsDanismanUnvanAdi", Value = esDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "YeniEsDanismanAdSoyad", Value = esDanisman.AdSoyad });


                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezDili", Value = (tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });

                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = esDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }
                        if (isOnayOrRed == false && item.SablonParametreleri.Any(a => a == "@RetAciklama"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "RetAciklama", Value = esDanisman.EYKDaOnaylanmadiDurumAciklamasi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            var isOgrenci = item.JuriTipAdi == "Öğrenci";
                            item.MailParameterDtos.Add(new MailParameterDto
                            {
                                Key = "Link",
                                Value = $"{enstitu.SistemErisimAdresi}/{(isOgrenci ? "TDOBasvurular" : "TDOGelenBasvurular")}/Index?TDOBasvuruID={tdoBasvuruDanisman.TDOBasvuruID}",
                                IsLink = true
                            });
                        }
                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            isSended = true;
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
                    }
                    if (isSended) entities.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez Eş Danışmanı işlemi için mail gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
    }
}