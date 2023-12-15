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
    public class MailSenderTdo
    {
        public static MmMessage SendMailTdoBilgisi(int tdoBasvuruDanismanId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var tdoBasvuruDanisman = db.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
                    var tdoDanismanTalepTipId = tdoBasvuruDanisman.TDODanismanTalepTipID;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();

                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniOnerisi)
                    {
                        var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoDanismanOnerisiYapildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoDanismanOnerisiYapildiOgrenci
                        });
                    }
                    else if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniDegisikligi)
                    {
                        var varolanDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.VarolanTezDanismanID);
                        var yeniDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Varolan Danışman",
                            AdSoyad = varolanDanisman.Ad + " " + varolanDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = varolanDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoTezDanismanDegisikligiVarolanDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoTezDanismanDegisikligiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoTezDanismanDegisikligiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi)
                    {
                        var varolanDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.VarolanTezDanismanID);
                        var yeniDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Varolan Danışman",
                            AdSoyad = varolanDanisman.Ad + " " + varolanDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = varolanDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiVarolanDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezBasligiDegisikligi)
                    {
                        var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoTezBasligiDegisikligiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = danisman.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoTezBasligiDegisikligiOgrenci
                        });
                    }




                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = db.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;
                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "VarolanDanismanUnvanAdi", Value = tdoBasvuruDanisman.VarolanTDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "VarolanDanismanAdSoyad", Value = tdoBasvuruDanisman.VarolanTDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniDanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniDanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezDili", Value = tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce" });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });

                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniTezBaslikTr", Value = tdoBasvuruDanisman.YeniTezBaslikTr });

                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniTezBaslikEn", Value = tdoBasvuruDanisman.YeniTezBaslikEn });

                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }
                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
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

                            db.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOBasvuruDanismanID:" + tdoBasvuruDanismanId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(),  ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
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
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var tdoBasvuruDanisman = db.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
                    var tdoDanismanTalepTipId = tdoBasvuruDanisman.TDODanismanTalepTipID;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();
                    int? raporTipId = null;
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniOnerisi)
                    {
                        raporTipId = RaporTipiEnum.TezDanismanOneriFormu;
                        if (isOnayOrRed)
                        {
                            var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipiEnum.TdoDanismanOnerisiOnaylandiDanisman
                            });
                        }
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoDanismanOnerisiOnaylandiOgrenci : MailSablonTipiEnum.TdoDanismanOnerisiReddedildiOgrenci
                        });
                    }
                    else if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniDegisikligi)
                    {
                        raporTipId = RaporTipiEnum.TezDanismanDegisiklikFormu;
                        var yeniDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanDegisikligiOnaylandiYeniDanisman : MailSablonTipiEnum.TdoTezDanismanDegisikligiRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanDegisikligiOnaylandiOgrenci : MailSablonTipiEnum.TdoTezDanismanDegisikligiRetEdildiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi)
                    {
                        raporTipId = RaporTipiEnum.TezDanismanDegisiklikFormu;
                        var yeniDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiOnaylandiYeniDanisman : MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiOnaylandiOgrenci : MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiRetEdildiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezBasligiDegisikligi)
                    {
                        raporTipId = RaporTipiEnum.TezDanismanDegisiklikFormu;
                        var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoTezBasligiDegisikligiOnaylandiDanisman : MailSablonTipiEnum.TdoTezBasligiDegisikligiRetEdildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoTezBasligiDegisikligiOnaylandiOgrenci : MailSablonTipiEnum.TdoTezBasligiDegisikligiRetEdildiOgrenci
                        });
                    }



                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = db.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;

                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();
                        if (isOnayOrRed)
                        {
                            var ids = new List<int?>() { tdoBasvuruDanismanId };
                            var ekler = Management.ExportRaporPdf(raporTipId.Value, ids);
                            item.Attachments.AddRange(ekler);

                        }
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "VarolanDanismanUnvanAdi", Value = tdoBasvuruDanisman.VarolanTDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "VarolanDanismanAdSoyad", Value = tdoBasvuruDanisman.VarolanTDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniDanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniDanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniTezBaslikTr", Value = tdoBasvuruDanisman.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniTezBaslikEn", Value = tdoBasvuruDanisman.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezDili", Value = (tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });

                        if (tdoBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }
                        if (!isOnayOrRed)
                        {
                            var retAciklama = "";

                            if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezBasligiDegisikligi || tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniOnerisi) retAciklama = tdoBasvuruDanisman.DanismanOnaylanmadiAciklama;
                            else retAciklama = tdoBasvuruDanisman.VarolanDanismanOnaylanmadiAciklama;


                            if (item.SablonParametreleri.Any(a => a == "@RedAciklama"))
                            {
                                mailParameterDtos.Add(new MailParameterDto { Key = "RedAciklama", Value = retAciklama });
                            }
                            if (item.SablonParametreleri.Any(a => a == "@RetAciklama"))
                            {
                                mailParameterDtos.Add(new MailParameterDto { Key = "RetAciklama", Value = retAciklama });
                            }
                        }
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
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
                            kModel.GonderilenMailEkleris = item.Attachments.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList();
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            db.GonderilenMaillers.Add(kModel);
                        }
                    }

                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOBasvuruDanismanID:" + tdoBasvuruDanismanId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
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
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var tdoBasvuruDanisman = db.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);
                    var tdoDanismanTalepTipId = tdoBasvuruDanisman.TDODanismanTalepTipID;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniOnerisi)
                    {

                        var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoDanismanOnerisiEykDaOnaylandiDanisman : MailSablonTipiEnum.TdoDanismanOnerisiEykDaReddedildiOgrenciDanisman
                        });

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoDanismanOnerisiEykDaOnaylandiOgrenci : MailSablonTipiEnum.TdoDanismanOnerisiEykDaReddedildiOgrenciDanisman
                        });
                    }
                    else if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniDegisikligi)
                    {
                        var yeniDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanDegisikligiEykDaOnaylandiYeniDanisman : MailSablonTipiEnum.TdoTezDanismanDegisikligiEykDaRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanDegisikligiEykDaOnaylandiOgrenci : MailSablonTipiEnum.TdoTezDanismanDegisikligiEykDaRetEdildiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi)
                    {
                        var yeniDanisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            AdSoyad = yeniDanisman.Ad + " " + yeniDanisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = yeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiEykDaOnaylandiYeniDanisman : MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiEykDaRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiEykDaOnaylandiOgrenci : MailSablonTipiEnum.TdoTezDanismanVeBaslikDegisikligiEykDaRetEdildiOgrenci
                        });
                    }
                    if (tdoDanismanTalepTipId == TdoDanismanTalepTipEnum.TezBasligiDegisikligi)
                    {
                        var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoTezBasligiDegisikligiEykDaOnaylandiDanisman : MailSablonTipiEnum.TdoTezBasligiDegisikligiEykDaRetEdildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = isOnayOrRed ? MailSablonTipiEnum.TdoTezBasligiDegisikligiEykDaOnaylandiOgrenci : MailSablonTipiEnum.TdoTezBasligiDegisikligiEykDaRetEdildiOgrenci
                        });
                    }
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();

                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = db.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;

                    foreach (var item in mModel)
                    {
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "VarolanDanismanUnvanAdi", Value = tdoBasvuruDanisman.VarolanTDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "VarolanDanismanAdSoyad", Value = tdoBasvuruDanisman.VarolanTDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniDanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniDanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniTezBaslikTr", Value = tdoBasvuruDanisman.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniTezBaslikEn", Value = tdoBasvuruDanisman.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezDili", Value = (tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });

                        if (tdoBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }
                        if (!isOnayOrRed)
                        {
                            var retAciklama = tdoBasvuruDanisman.EYKDaOnaylanmadiDurumAciklamasi;

                            if (item.SablonParametreleri.Any(a => a == "@RetAciklama"))
                            {
                                mailParameterDtos.Add(new MailParameterDto { Key = "RetAciklama", Value = retAciklama });
                            }
                        }
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = tdoBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }
                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            isSended = true;
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = contentDetailDto.Title;
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
                            db.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOBasvuruDanismanID:" + tdoBasvuruDanismanId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
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
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var esDanisman = db.TDOBasvuruEsDanismen.First(p => p.TDOBasvuruEsDanismanID == tdoBasvuruEsDanismanId);
                    var tdoBasvuruDanisman = esDanisman.TDOBasvuruDanisman;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
                    var ogrenci = tdoBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();


                    if (esDanisman.IsDegisiklikTalebi)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoEsDanismanDegisikligiYapildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoEsDanismanDegisikligiYapildiOgrenci
                        });
                    }
                    else
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoEsDanismanOnerisiYapildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TdoEsDanismanOnerisiYapildiOgrenci
                        });
                    }



                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = db.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;
                    foreach (var item in mModel)
                    {
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();


                        var iDs = new List<int?>() { tdoBasvuruEsDanismanId };
                        var ekler = Management.ExportRaporPdf(RaporTipiEnum.TezEsDanismanOneriFormu, iDs);
                        item.Attachments.AddRange(ekler);



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EsDanismanUnvanAdi", Value = esDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EsDanismanAdSoyad", Value = esDanisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUniversite"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EsDanismanUniversite", Value = esDanisman.UniversiteAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanEsDanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "VarolanEsDanismanAdSoyad", Value = esDanisman.OncekiEsDanismanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniEsDanismanUnvanAdi", Value = esDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniEsDanismanUnvanAdi", Value = esDanisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = esDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezDili", Value = (tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }


                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            isSended = true;
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = contentDetailDto.Title;

                            if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";

                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = contentDetailDto.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailEkleris = item.Attachments.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList();
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            db.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez Eş danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOEsBasvuruDanismanID:" + tdoBasvuruEsDanismanId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
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
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var esDanisman = db.TDOBasvuruEsDanismen.First(p => p.TDOBasvuruEsDanismanID == tDoBasvuruEsDanismanId);
                    var tdoBasvuruDanisman = esDanisman.TDOBasvuruDanisman;
                    var tdoBasvuru = tdoBasvuruDanisman.TDOBasvuru;
                    var danisman = db.Kullanicilars.First(p => p.KullaniciID == tdoBasvuruDanisman.TezDanismanID);
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
                                EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true }, new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipiEnum.TdoEsDanismanDegisikligiEykDaRetEdildiOgrenciDanisman
                            });
                        }
                        else
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipiEnum.TdoEsDanismanDegisikligiEykDaOnaylandiDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Eş Danışman",
                                AdSoyad = esDanisman.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = esDanisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipiEnum.TdoEsDanismanDegisikligiEykDaOnaylandiEsDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci",
                                AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipiEnum.TdoEsDanismanDegisikligiEykDaOnaylandiOgrenci
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
                                EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true }, new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipiEnum.TdoEsDanismanOnerisiEykDaReddedildiOgrenciDanisman
                            });
                        }
                        else
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipiEnum.TdoEsDanismanOnerisiEykDaOnaylandiDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Eş Danışman",
                                AdSoyad = esDanisman.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = esDanisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipiEnum.TdoEsDanismanOnerisiEykDaOnaylandiEsDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci",
                                AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipiEnum.TdoEsDanismanOnerisiEykDaOnaylandiOgrenci
                            });
                        }
                    }


                    var enstitu = tdoBasvuruDanisman.TDOBasvuru.Enstituler;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var prgL = tdoBasvuru.Programlar;
                    var ogrS = db.OgrenimTipleris.First(p => p.OgrenimTipKod == tdoBasvuru.OgrenimTipKod && p.EnstituKod == enstitu.EnstituKod);

                    bool isSended = false;
                    foreach (var item in mModel)
                    {
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenimSeviyesiAdi", Value = ogrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tdoBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = tdoBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = tdoBasvuruDanisman.TDAdSoyad });


                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EsDanismanUnvanAdi", Value = esDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EsDanismanAdSoyad", Value = esDanisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUniversite"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EsDanismanUniversite", Value = esDanisman.UniversiteAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanEsDanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "VarolanEsDanismanAdSoyad", Value = esDanisman.OncekiEsDanismanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniEsDanismanUnvanAdi", Value = esDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YeniEsDanismanUnvanAdi", Value = esDanisman.AdSoyad });


                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tdoBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tdoBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezDili", Value = (tdoBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });

                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = esDanisman.EYKDaOnaylandiOnayTarihi.ToFormatDate() });
                        }
                        if (isOnayOrRed == false && item.SablonParametreleri.Any(a => a == "@RedAciklama"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "RedAciklama", Value = esDanisman.EYKDaOnaylanmadiDurumAciklamasi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + tdoBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }
                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            isSended = true;
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = contentDetailDto.Title;
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
                            db.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez Eş Danışmanı işlemi için mail gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
    }
}