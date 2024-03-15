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
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.MailManager
{
    public class MailSenderMezuniyet
    {
        public static MmMessage SendMailBasvuruYapildi(int mezuniyetBasvurulariId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var mBasvuru =
                        entities.MezuniyetBasvurularis.First(f => f.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                    var enstitu = mBasvuru.MezuniyetSureci.Enstituler;
                    var sablonlar = entities.MailSablonlaris.Where(p => p.EnstituKod == enstitu.EnstituKod).ToList();


                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = mBasvuru.Ad + " " + mBasvuru.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = mBasvuru.Kullanicilar.EMail,KullaniciId = mBasvuru.KullaniciID,ToOrBcc = true } },
                            MailSablonTipId = MailSablonTipiEnum.MezBasvuruYapildiOgrenci,
                        }
                    };


                    var danisman = entities.Kullanicilars.First(p => p.KullaniciID == mBasvuru.TezDanismanID);
                    mModel.Add(new SablonMailModel
                    {
                        JuriTipAdi = "Danışman",
                        AdSoyad = danisman.Ad + " " + danisman.Soyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                        MailSablonTipId = MailSablonTipiEnum.MezBasvuruYapildiDanisman,
                    });

                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;

                        item.Sablon = sablonlar.First(p => p.MailSablonTipID == item.MailSablonTipId);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = mBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = mBasvuru.Ad + " " + mBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = mBasvuru.TezDanismanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = mBasvuru.TezDanismanUnvani });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, null);
                        if (!snded) continue;
                        if (!item.AdSoyad.IsNullOrWhiteSpace()) contentDetailDto.Title += " (" + item.AdSoyad + ")";
                        if (!item.JuriTipAdi.IsNullOrWhiteSpace()) contentDetailDto.Title += " (" + item.JuriTipAdi + ")";

                        var gm = new GonderilenMailler
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
                            GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris
                        };
                        entities.GonderilenMaillers.Add(gm);
                        entities.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {


                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet Başvurusu maili gönderilirken bir hata oluştu.\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add("Mezuniyet Başvurusu maili gönderilirken bir hata oluştu.</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailBasvuruDanismanOnay(int mezuniyetBasvurulariId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var mBasvur =
                        entities.MezuniyetBasvurularis.First(f => f.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                    var enstitu = mBasvur.MezuniyetSureci.Enstituler;
                    var sablonTipId = mBasvur.IsDanismanOnay == true ? MailSablonTipiEnum.MezDanismanOnayladiOgrenci : MailSablonTipiEnum.MezDanismanOnaylamadiOgrenci;
                    var sablon = entities.MailSablonlaris.FirstOrDefault(p => p.EnstituKod == enstitu.EnstituKod && p.MailSablonTipID == sablonTipId);
                    var basvuruDonemAdi = mBasvur.MezuniyetSureci.BaslangicYil + " " + mBasvur.MezuniyetSureci.BitisYil + " / " + mBasvur.MezuniyetSureci.Donemler.DonemAdi;



                    var mModel = new List<SablonMailModel>
                        {
                            new SablonMailModel
                            {

                                AdSoyad = mBasvur.Ad + " " + mBasvur.Soyad,
                                Sablon = sablon,
                                EnstituAdi = enstitu.EnstituAd,
                                WebAdresi = enstitu.WebAdresi,
                                SistemErisimAdresi = enstitu.SistemErisimAdresi,
                                EMails = new List<MailSendList> { new MailSendList { EMail = mBasvur.Kullanicilar.EMail,KullaniciId = mBasvur.KullaniciID,ToOrBcc = true } },
                                MailSablonTipId = sablonTipId,
                                JuriTipAdi = "Öğrenci"
                            }
                        };

                    var danisman = entities.Kullanicilars.First(p => p.KullaniciID == mBasvur.TezDanismanID);
                    foreach (var item in mModel)
                    {


                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = item.EnstituAdi });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = item.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@BasvuruDonemAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "BasvuruDonemAdi", Value = basvuruDonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = mBasvur.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = mBasvur.Ad + " " + mBasvur.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "RetAciklamasi", Value = mBasvur.DanismanOnayAciklama });


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

                }
            }
            catch (Exception ex)
            {


                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet Danışman onay sonuç maili gönderilirken bir hata oluştu.\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add("Mezuniyet Danışman onay sonuç maili gönderilirken bir hata oluştu.</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailBasvuruDurum(int mezuniyetBasvurulariId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var mBasvur = entities.MezuniyetBasvurularis.First(f => f.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                    var enstitu = mBasvur.MezuniyetSureci.Enstituler;

                    var basvuruDonemAdi = mBasvur.MezuniyetSureci.BaslangicYil + " " + mBasvur.MezuniyetSureci.BitisYil + " / " + mBasvur.MezuniyetSureci.Donemler.DonemAdi;

                    var mModel = new List<SablonMailModel>();

                    int ogrenciMailSablonId;
                    if (mBasvur.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.IptalEdildi) ogrenciMailSablonId = MailSablonTipiEnum.MezYayinSartiSaglanamadiOgrenci;
                    else if (mBasvur.OgrenimTipKod.IsDoktora()) ogrenciMailSablonId = MailSablonTipiEnum.MezYayinSartiSaglandiOgrenciDoktora;
                    else ogrenciMailSablonId = MailSablonTipiEnum.MezYayinSartiSaglandiOgrenciYl;
                    mModel.Add(new SablonMailModel
                    {
                        JuriTipAdi = "Öğrenci",
                        AdSoyad = mBasvur.Ad + " " + mBasvur.Soyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = mBasvur.Kullanicilar.EMail, KullaniciId = mBasvur.KullaniciID, ToOrBcc = true } },
                        MailSablonTipId = ogrenciMailSablonId,
                    });
                    if (mBasvur.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.IptalEdildi)
                    {
                        var danisman = entities.Kullanicilars.First(p => p.KullaniciID == mBasvur.TezDanismanID);
                        mModel.Add(
                            new SablonMailModel
                            {
                                JuriTipAdi = "Danışman",
                                MailSablonTipId = MailSablonTipiEnum.MezYayinSartiSaglandiDanisman,
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                                UnvanAdi = danisman.Unvanlar.UnvanAdi
                            });
                    }

                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && p.EnstituKod == enstitu.EnstituKod && sablonTipIDs.Contains(p.MailSablonTipID)).ToList();

                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(f => f.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;

                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@BasvuruDonemAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "BasvuruDonemAdi", Value = basvuruDonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = mBasvur.Ad + " " + mBasvur.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@IptalAciklamasi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "IptalAciklamasi", Value = mBasvur.MezuniyetYayinKontrolDurumAciklamasi });



                        if (item.MailSablonTipId != MailSablonTipiEnum.MezYayinSartiSaglandiDanisman && mBasvur.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.IptalEdildi)
                        {
                            item.Attachments.AddRange(MailReportAttachment.GetMezuniyetBasvuruRaporuAttachments(mBasvur.MezuniyetBasvurulariID));
                        }
                        if (mBasvur.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
                        {
                            item.Attachments.AddRange(MailReportAttachment.GetMezuniyetTezTeslimFormuAttachments(mBasvur.MezuniyetBasvurulariID, true));
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


                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet başvuru durumu değişikliği mail gönderme işlemi yapılırken bir hata oluştu.\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add("Mezuniyet başvuru durumu değişikliği mail gönderme işlemi yapılırken bir hata oluştu.</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailJuriOneriFormuOnay(int mezuniyetJuriOneriFormId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var juriOneriFormu = entities.MezuniyetJuriOneriFormlaris.First(p => p.MezuniyetJuriOneriFormID == mezuniyetJuriOneriFormId);
                    var mBasvur = juriOneriFormu.MezuniyetBasvurulari;
                    int danismanSablonId;
                    int asilSablonId;
                    int ogrenciSablonId;
                    if (mBasvur.OgrenimTipKod.IsDoktora())
                    {

                        danismanSablonId = MailSablonTipiEnum.MezEykTarihiGirildiDanismanDoktora;
                        asilSablonId = MailSablonTipiEnum.MezEykTarihiGirildiJuriAsilDoktora;
                        ogrenciSablonId = MailSablonTipiEnum.MezEykTarihiGirildiOgrenciDoktora;
                    }
                    else
                    {
                        danismanSablonId = MailSablonTipiEnum.MezEykTarihiGirildiDanismanYl;
                        asilSablonId = MailSablonTipiEnum.MezEykTarihiGirildiJuriAsilYl;
                        ogrenciSablonId = MailSablonTipiEnum.MezEykTarihiGirildiOgrenciYl;
                    }

                    string tezKonusu;
                    if (juriOneriFormu.IsTezBasligiDegisti == true)
                    {
                        tezKonusu = mBasvur.IsTezDiliTr == true
                            ? juriOneriFormu.YeniTezBaslikTr
                            : juriOneriFormu.YeniTezBaslikEn;
                    }
                    else tezKonusu = mBasvur.IsTezDiliTr == true
                        ? mBasvur.TezBaslikTr
                        : mBasvur.TezBaslikEn;

                    var enstitu = mBasvur.MezuniyetSureci.Enstituler;

                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = mBasvur.Ad + " " + mBasvur.Soyad,
                            EMails = new List<MailSendList>
                            {
                                new MailSendList { EMail = mBasvur.Kullanicilar.EMail, KullaniciId = mBasvur.KullaniciID, ToOrBcc = true }
                            },
                            MailSablonTipId = ogrenciSablonId
                        }
                    };
                    var juriler = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek.HasValue).ToList();
                    foreach (var item in juriler.Where(p => p.IsAsilOrYedek == true))
                    {
                        mModel.Add(new SablonMailModel
                        {

                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = (item.JuriTipAdi == "TezDanismani" ? mBasvur.TezDanismanID : null), ToOrBcc = true } },
                            MailSablonTipId = (item.JuriTipAdi == "TezDanismani" ? danismanSablonId : asilSablonId),
                            JuriTipAdi = item.JuriTipAdi,
                            UnvanAdi = item.UnvanAdi,
                            MezuniyetJuriOneriFormuJuriId = item.MezuniyetJuriOneriFormuJuriID,
                        });
                        if (item.JuriTipAdi == "TezDanismani" && !mBasvur.TezEsDanismanEMail.IsNullOrWhiteSpace())
                        {
                            //Eş danışman var ise Danışmana giden mail eş danışmana da gönderilmesi için.
                            mModel.Add(new SablonMailModel
                            {

                                AdSoyad = mBasvur.TezEsDanismanAdi,
                                EMails = new List<MailSendList> { new MailSendList { EMail = mBasvur.TezEsDanismanEMail, ToOrBcc = true } },
                                MailSablonTipId = danismanSablonId,
                                JuriTipAdi = item.JuriTipAdi,
                                UnvanAdi = mBasvur.TezEsDanismanUnvani,
                            });
                        }
                    }
                    var danisman = juriler.First(p => p.JuriTipAdi == "TezDanismani");

                    var anabilimDali = mBasvur.Programlar.AnabilimDallari;
                    var program = mBasvur.Programlar;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && p.EnstituKod == enstitu.EnstituKod && sablonTipIDs.Contains(p.MailSablonTipID)).ToList();

                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(f => f.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;

                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;
                        item.ProgramAdi = program.ProgramAdi;

                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());




                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = mBasvur.EYKTarihi.Value.ToFormatDate() });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = mBasvur.Ad + " " + mBasvur.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciBilgi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciBilgi", Value = (mBasvur.OgrenciNo + " " + mBasvur.Ad + " " + mBasvur.Soyad + " (" + anabilimDali.AnabilimDaliAdi + " / " + program.ProgramAdi + ")") });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tezKonusu });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanBilgi", Value = danisman.UnvanAdi + " " + danisman.AdSoyad });
                        foreach (var itemAsil in juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).Select((s, inx) => new { s, inx = inx + 1 }))
                            if (item.SablonParametreleri.Any(a => a == "@AsilBilgi" + itemAsil.inx))
                            {
                                var uniBilgi = "";
                                if (itemAsil.s.JuriTipAdi.Contains("YtuDisiJuri"))
                                {
                                    uniBilgi = " (" + (itemAsil.s.UniversiteAdi) + ")";
                                }
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "AsilBilgi" + itemAsil.inx, Value = itemAsil.s.UnvanAdi + " " + itemAsil.s.AdSoyad + uniBilgi });
                            }
                        foreach (var itemYedek in juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == false).Select((s, inx) => new { s, inx = inx + 1 }))
                            if (item.SablonParametreleri.Any(a => a == "@YedekBilgi" + itemYedek.inx))
                            {
                                var uniBilgi = "";
                                if (itemYedek.s.JuriTipAdi.Contains("YtuDisiJuri"))
                                {
                                    uniBilgi = " (" + (itemYedek.s.UniversiteAdi) + ")";
                                }
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "YedekBilgi" + itemYedek.inx, Value = itemYedek.s.UnvanAdi + " " + itemYedek.s.AdSoyad + uniBilgi });
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



                }
            }
            catch (Exception ex)
            {


                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet Jüri öneri formu onay sonuç maili gönderilirken bir hata oluştu.\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add("Mezuniyet Jüri öneri formu onay sonuç maili gönderilirken bir hata oluştu.</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailMezuniyetDegerlendirmeLink(int srTalepId, Guid? uniqueId = null, bool isLinkOrSonuc = false, bool isYeniLink = true, string eMail = "")
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {

                    var srTalep = entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);
                    var qJuriler = srTalep.SRTaleplerJuris.AsQueryable();
                    qJuriler = uniqueId.HasValue ? qJuriler.Where(p => p.UniqueID == uniqueId.Value) : qJuriler.Where(p => p.JuriTipAdi != "TezDanismani");
                    var juriler = qJuriler.ToList();
                    var mb = srTalep.MezuniyetBasvurulari;
                    var mModel = new List<SablonMailModel>();

                    var enstitu = mb.MezuniyetSureci.Enstituler;

                    var abdL = mb.Programlar.AnabilimDallari;
                    var prgL = mb.Programlar;


                    if (isLinkOrSonuc)
                    {

                        foreach (var item in juriler)
                        {
                            if (isYeniLink) item.UniqueID = Guid.NewGuid();
                            mModel.Add(new SablonMailModel
                            {
                                UniqueId = item.UniqueID,
                                UnvanAdi = item.UnvanAdi,
                                AdSoyad = item.JuriAdi,
                                EMails = new List<MailSendList>
                                {
                                    new MailSendList
                                    {
                                        EMail = (eMail.IsNullOrWhiteSpace() ? item.Email : eMail),
                                        KullaniciId = item.JuriTipAdi == "TezDanismani" ? mb.TezDanismanID : null,
                                        ToOrBcc = true
                                    }
                                },
                                MailSablonTipId = mb.OgrenimTipKod.IsDoktora()
                                    ? MailSablonTipiEnum.MezSinavDegerlendirmeDavetGonderimJuriDr
                                    : MailSablonTipiEnum.MezSinavDegerlendirmeDavetGonderimJuriYl,
                                JuriTipAdi = item.JuriTipAdi,
                            });
                        }
                    }
                    else
                    {
                        var danisman = entities.Kullanicilars.First(p => p.KullaniciID == mb.TezDanismanID);
                        var srDanisman = srTalep.SRTaleplerJuris.First(p => p.JuriTipAdi == "TezDanismani");
                        mModel.Add(new SablonMailModel
                        {
                            UniqueId = null,
                            UnvanAdi = danisman.Unvanlar.UnvanAdi,
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = !srDanisman.Email.IsNullOrWhiteSpace() ? srDanisman.Email : danisman.EMail, KullaniciId = danisman.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipiEnum.MezSinavSonucBilgiGonderimDanismanDr : MailSablonTipiEnum.MezSinavSonucBilgiGonderimDanismanYl,
                            JuriTipAdi = "TezDanismani",
                        });
                        var ogrenci = entities.Kullanicilars.First(p => p.KullaniciID == mb.KullaniciID);
                        mModel.Add(new SablonMailModel
                        {
                            UniqueId = null,
                            UnvanAdi = "",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, KullaniciId = ogrenci.KullaniciID, ToOrBcc = true } },
                            MailSablonTipId = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipiEnum.MezSinavSonucBilgiGonderimOgrenciDr : MailSablonTipiEnum.MezSinavSonucBilgiGonderimOgrenciYl,
                            JuriTipAdi = "Öğrenci",
                        });
                    }

                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;
                        if (!isLinkOrSonuc)
                        {
                            item.Attachments.AddRange(MailReportAttachment.GetMezuniyetTezSinavSonucFormuAttachments(srTalepId, item.JuriTipAdi == "TezDanismani"));
                        }
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());




                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (isLinkOrSonuc)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@JuriAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "JuriAdSoyad", Value = item.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@JuriUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "JuriUnvanAdi", Value = item.UnvanAdi });
                        }
                        else
                        {
                            if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = mb.TezDanismanUnvani });
                            if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = mb.TezDanismanAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = mb.Ad + " " + mb.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = mb.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavTarihi", Value = srTalep.Tarih.ToLongDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@SinavSaati"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavSaati", Value = $"{srTalep.BasSaat:hh\\:mm}" });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/Mezuniyet/GSinavDegerlendir?UniqueID=" + item.UniqueId, IsLink = true });
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
                        if (isLinkOrSonuc)
                        {
                            var juri = juriler.FirstOrDefault(p => p.UniqueID == item.UniqueId);

                            juri.DegerlendirmeIslemTarihi = null;
                            juri.DegerlendirmeIslemYapanIP = null;
                            juri.DegerlendirmeYapanID = null;
                            juri.MezuniyetSinavDurumID = null;
                            juri.Aciklama = null;
                            juri.IsLinkGonderildi = true;
                            juri.LinkGonderimTarihi = DateTime.Now;
                            juri.LinkGonderenID = UserIdentity.Current.Id;
                            entities.SaveChanges();
                            LogIslemleri.LogEkle("SRTaleplerJuri", LogCrudType.Update, juri.ToJson());
                        }

                        entities.GonderilenMaillers.Add(kModel);
                        entities.SaveChanges();
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                const string message = "Tez Sınavı değerlendirmesi için Jüri üyelerine değerlendirme davetiye linki mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailMezuniyetSinavYerBilgisi(int srTalepId, bool isOnaylandi)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var talep = entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);

                    var mb = talep.MezuniyetBasvurulari;
                    if (!mb.MezuniyetJuriOneriFormlaris.Any())
                        return new MmMessage()
                        {
                            Messages = new List<string>
                                { "Rezervasyona ait mezuniyet başvurusu bulunamadığı öğrenci ve jüri üyelerine için mail gönderilemedi!" }
                        };
                    var juriOneriFormu = mb.MezuniyetJuriOneriFormlaris.First();


                    var enstitu = mb.MezuniyetSureci.Enstituler;

                    var mModel = new List<SablonMailModel>();
                    int juriSablonTipId;
                    int ogrenciSablonTipId;
                    if (isOnaylandi)
                    {
                        juriSablonTipId = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipiEnum.MezSinavYerBilgisiGonderimJuriDoktora : MailSablonTipiEnum.MezSinavYerBilgisiGonderimJuriYl;
                        ogrenciSablonTipId = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipiEnum.MezSinavYerBilgisiGonderimOgrenciDoktora : MailSablonTipiEnum.MezSinavYerBilgisiGonderimOgrenciYl;
                    }
                    else
                    {
                        juriSablonTipId = MailSablonTipiEnum.MezSinavYerBilgisiOnaylanmadi;
                        ogrenciSablonTipId = MailSablonTipiEnum.MezSinavYerBilgisiOnaylanmadi;
                    }


                    var juriler = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek.HasValue).ToList();

                    mModel.Add(new SablonMailModel
                    {
                        JuriTipAdi = "Öğrenci",
                        AdSoyad = mb.Ad + " " + mb.Soyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = mb.Kullanicilar.EMail, KullaniciId = mb.KullaniciID, ToOrBcc = true } },
                        MailSablonTipId = ogrenciSablonTipId,
                    });
                    var danisman = juriler.First(p => p.JuriTipAdi == "TezDanismani");
                    if (!isOnaylandi)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.UnvanAdi + " " + danisman.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, KullaniciId = mb.TezDanismanID, ToOrBcc = true } },
                            MailSablonTipId = juriSablonTipId,

                        });
                        if (!mb.TezEsDanismanEMail.IsNullOrWhiteSpace())
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Eş Danışman",
                                AdSoyad = mb.TezEsDanismanUnvani + " " + mb.TezEsDanismanAdi,
                                EMails = new List<MailSendList> { new MailSendList { EMail = mb.TezEsDanismanEMail, ToOrBcc = true } },
                                MailSablonTipId = juriSablonTipId,

                            });
                    }
                    else
                    {
                        foreach (var item in juriler.Where(p => p.IsAsilOrYedek == true))
                        {


                            mModel.Add(new SablonMailModel
                            {

                                AdSoyad = item.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, KullaniciId = item.JuriTipAdi == "TezDanismani" ? mb.TezDanismanID : null, ToOrBcc = true } },
                                MailSablonTipId = juriSablonTipId,
                                JuriTipAdi = item.JuriTipAdi,
                                UnvanAdi = item.UnvanAdi
                            });

                            if (item.JuriTipAdi == "TezDanismani" && !mb.TezEsDanismanEMail.IsNullOrWhiteSpace())
                            {
                                mModel.Add(new SablonMailModel
                                {

                                    AdSoyad = mb.TezEsDanismanAdi,
                                    EMails = new List<MailSendList> { new MailSendList { EMail = mb.TezEsDanismanEMail, ToOrBcc = true } },
                                    MailSablonTipId = juriSablonTipId,
                                    JuriTipAdi = item.JuriTipAdi,
                                    UnvanAdi = mb.TezEsDanismanUnvani
                                });
                            }
                        }


                    }


                    var anabilimDali = mb.Programlar.AnabilimDallari;
                    var program = mb.Programlar;
                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && p.EnstituKod == enstitu.EnstituKod && sablonTipIDs.Contains(p.MailSablonTipID)).ToList();

                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(f => f.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;

                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = mb.EYKTarihi.ToFormatDate() });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = mb.Ad + " " + mb.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = mb.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavTarihi", Value = talep.Tarih.ToLongDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@SinavSaati"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavSaati", Value = $"{talep.BasSaat:hh\\:mm}" + "-" + $"{talep.BitSaat:hh\\:mm}" });
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            var sinavYerAdi = talep.SRSalonID.HasValue ? talep.SRSalonlar.SalonAdi : talep.SalonAdi;
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavYeri", Value = sinavYerAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@IptalAciklamasi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "IptalAciklamasi", Value = talep.SRDurumAciklamasi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanBilgi", Value = danisman.UnvanAdi + " " + danisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUni"))
                        {
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUni", Value = danisman.UniversiteAdi });
                        }
                        foreach (var itemAsil in juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).Select((s, inx) => new { s, inx = inx + 1 }))
                        {

                            if (item.SablonParametreleri.Any(a => a == "@AsilBilgi" + itemAsil.inx))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "AsilBilgi" + itemAsil.inx, Value = itemAsil.s.UnvanAdi + " " + itemAsil.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@AsilBilgiUni" + itemAsil.inx))
                            {
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "AsilBilgiUni" + itemAsil.inx, Value = itemAsil.s.UniversiteAdi });
                            }
                        }
                        foreach (var itemYedek in juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == false).Select((s, inx) => new { s, inx = inx + 1 }))
                        {
                            if (item.SablonParametreleri.Any(a => a == "@YedekBilgi" + itemYedek.inx))
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "YedekBilgi" + itemYedek.inx, Value = itemYedek.s.UnvanAdi + " " + itemYedek.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@YedekBilgiUni" + itemYedek.inx))
                            {
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "YedekBilgiUni" + itemYedek.inx, Value = itemYedek.s.UniversiteAdi });
                            }
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
                    var message = "'" + talep.Kullanicilar.Ad + " " + talep.Kullanicilar.Soyad + "'  kullanıcısının yapmış olduğu salon rezervasyonu bilgisi " + juriler.Count + " adet jüriye mail olarak gönderildi!";
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Salon rezervasyonu için jürilere mail gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailMezuniyetSinavSonucu(int srTalepId, int mezuniyetSinavDurumId)
        {
            var mmMessage = new MmMessage();
            try
            {
                var sablonTipId = 0;
                using (var entities = new LubsDbEntities())
                {
                    var talep = entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);

                    var mb = talep.MezuniyetBasvurulari;
                    var mbOtipKriter = talep.MezuniyetBasvurulari.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == mb.OgrenimTipKod);
                    var juriOneriFormu = mb.MezuniyetJuriOneriFormlaris.First();


                    var enstitu = mb.MezuniyetSureci.Enstituler;

                    var mModel = new List<SablonMailModel>();
                    if (mezuniyetSinavDurumId == MezuniyetSinavDurumEnum.Basarili)
                    {
                        sablonTipId = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipiEnum.MezSinavSonucuBasariliBilgisiGonderimDoktora : MailSablonTipiEnum.MezSinavSonucuBasariliBilgisiGonderimYl;
                    }
                    else if (mezuniyetSinavDurumId == MezuniyetSinavDurumEnum.Uzatma)
                    {
                        sablonTipId = MailSablonTipiEnum.MezSinavSonucuUzatmaBilgisiGonderim;
                    }


                    var tezDanismani = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TezDanismani");

                    var mezuniyetMailModel = new SablonMailModel
                    {

                        AdSoyad = mb.Ad + " " + mb.Soyad,
                        EMails = new List<MailSendList> {
                                        new MailSendList { EMail= mb.Kullanicilar.EMail, KullaniciId = mb.KullaniciID,
                                                           ToOrBcc = true
                                                         }
                                                    },
                        MailSablonTipId = sablonTipId
                    };
                    if (mezuniyetSinavDurumId == MezuniyetSinavDurumEnum.Basarili)
                        mezuniyetMailModel.Attachments.AddRange(MailReportAttachment.GetMezuniyetTezDuzeltmeVeJuriUyelerineTezTeslimTutanagiAttachments(srTalepId));

                    mezuniyetMailModel.EMails.Add(new MailSendList
                    {
                        EMail = tezDanismani.EMail,
                        ToOrBcc = false
                    });
                    if (!mb.TezEsDanismanEMail.IsNullOrWhiteSpace()) mezuniyetMailModel.EMails.Add(new MailSendList { EMail = mb.TezEsDanismanEMail, ToOrBcc = false });


                    mModel.Add(mezuniyetMailModel);

                    var sablonTipIDs = mModel.Select(s => s.MailSablonTipId).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && p.EnstituKod == enstitu.EnstituKod && sablonTipIDs.Contains(p.MailSablonTipID)).ToList();

                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(f => f.MailSablonTipID == item.MailSablonTipId);
                        if (item.Sablon == null) continue;
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;

                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = item.EnstituAdi });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = item.WebAdresi, IsLink = true });

                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavTarihi", Value = talep.Tarih.ToFormatDate() });
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            var sinavYerAdi = talep.SRSalonID.HasValue ? talep.SRSalonlar.SalonAdi : talep.SalonAdi;
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SinavYeri", Value = sinavYerAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@UzatmaTarihi"))
                        {
                            if (talep.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma)
                            {
                                var uzatmaTarihi = talep.Tarih.AddDays(mbOtipKriter.SinavUzatmaSinavAlmaSuresiMaxGun).ToFormatDate();
                                item.MailParameterDtos.Add(new MailParameterDto { Key = "UzatmaTarihi", Value = uzatmaTarihi });
                            }
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
                    var message = "'" + talep.Kullanicilar.Ad + " " + talep.Kullanicilar.Soyad + "'  öğrencisinin tez sınav sonucu bilgisi mail olarak gönderildi!";
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez sınav sonucu bilgisi mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailMezuniyetTezSablonKontrol(int mezuniyetBasvurulariTezDosyaId, int sablonTipId, string aciklama = "")
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LubsDbEntities())
                {
                    var mezuniyetBasvurulariTezDosyasi = entities.MezuniyetBasvurulariTezDosyalaris.First(p => p.MezuniyetBasvurulariTezDosyaID == mezuniyetBasvurulariTezDosyaId);
                    var mezuniyetBasvuru = mezuniyetBasvurulariTezDosyasi.MezuniyetBasvurulari;
                    var srTalep = mezuniyetBasvuru.SRTalepleris.First(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili);

                    var ogrenci = mezuniyetBasvuru.Kullanicilar;
                    var enstitu = mezuniyetBasvuru.MezuniyetSureci.Enstituler;

                    var mModel = new List<SablonMailModel>();

                    var mezuniyetMailModel = new SablonMailModel
                    {
                        AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                        EMails = new List<MailSendList> {
                                        new MailSendList {
                                                            EMail= ogrenci.EMail,KullaniciId = ogrenci.KullaniciID,
                                                            ToOrBcc = true
                                                         }
                                                    },
                        MailSablonTipId = sablonTipId
                    };
                    if (sablonTipId == MailSablonTipiEnum.MezTezKontrolTezDosyasiBasarili)
                        mezuniyetMailModel.Attachments.AddRange(MailReportAttachment.GetMezuniyetTezKontrolFormuAttachments(null, mezuniyetBasvurulariTezDosyaId));

                    mModel.Add(mezuniyetMailModel);


                    if (sablonTipId == MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklendi)
                    {
                        var tezKontrolKul = entities.Kullanicilars.FirstOrDefault(f =>
                            mezuniyetBasvuru.TezKontrolKullaniciID.HasValue && f.YetkiGrupID == 13 &&
                            f.KullaniciID == mezuniyetBasvuru.TezKontrolKullaniciID);
                        if (tezKontrolKul != null)
                        {
                            mModel.Add(new SablonMailModel
                            {
                                AdSoyad = tezKontrolKul.Ad + " " + tezKontrolKul.Soyad,
                                EMails = new List<MailSendList>
                                {
                                    new MailSendList
                                    {
                                        EMail = tezKontrolKul.EMail,
                                        KullaniciId = tezKontrolKul.KullaniciID,
                                        ToOrBcc = true
                                    }
                                },
                                MailSablonTipId = MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklendiKontrolYetkilisi
                            });

                        }
                    }

                    var sablonTipIds = mModel.Select(s => s.MailSablonTipId).ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && p.EnstituKod == enstitu.EnstituKod && sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var item in mModel)
                    {
                        item.EnstituAdi = enstitu.EnstituAd;
                        item.WebAdresi = enstitu.WebAdresi;

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipId && p.EnstituKod == enstitu.EnstituKod);
                        if (item.Sablon == null) continue;

                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = item.EnstituAdi });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = item.WebAdresi, IsLink = true });

                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@SRTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SRTarihi", Value = srTalep.Tarih.ToShortDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@Aciklama"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "Aciklama", Value = aciklama });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = ogrenci.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = mezuniyetBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = mezuniyetBasvuru.Programlar.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/MezuniyetGelenBasvurular/Index?sMezuniyetBid=" + mezuniyetBasvuru.MezuniyetBasvurulariID + "&sTabId=4", IsLink = true });
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
                    if (sablonTipId == MailSablonTipiEnum.MezTezKontrolTezDosyasiBasarili)
                        mmMessage.Messages.Add("'" + mezuniyetBasvuru.Kullanicilar.Ad + " " + mezuniyetBasvuru.Kullanicilar.Soyad + "'  öğrencisine ait tez şablon dosyası kontrolü başarılı olduğu bilgisi mail olarak gönderildi!");
                    else if (sablonTipId == MailSablonTipiEnum.MezTezKontrolTezDosyasiOnaylanmadi)
                        mmMessage.Messages.Add("'" + mezuniyetBasvuru.Kullanicilar.Ad + " " + mezuniyetBasvuru.Kullanicilar.Soyad + "'  öğrencisine ait tez şablon dosyası kontrolü onaylanmadığı bilgisi mail olarak gönderildi!");
                    else if (sablonTipId == MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklendi)
                        mmMessage.Messages.Add("'" + mezuniyetBasvuru.Kullanicilar.Ad + " " + mezuniyetBasvuru.Kullanicilar.Soyad + "'  öğrencisine ait tez şablon dosyası yüklendi bilgisi mail olarak gönderildi!");
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var msg = "";
                switch (sablonTipId)
                {
                    case MailSablonTipiEnum.MezTezKontrolTezDosyasiBasarili:
                        msg = "Tez şablon dosyası kontrolü başarılı olduğu bilgisi mail olarak gönderilirken bir hata oluştu! \r\nMezuniyetBasvurulariTezDosyaID:" + mezuniyetBasvurulariTezDosyaId;
                        break;
                    case MailSablonTipiEnum.MezTezKontrolTezDosyasiOnaylanmadi:
                        msg = "Tez şablon dosyası kontrolü onaylanmadığı bilgisii mail olarak gönderilirken bir hata oluştu! \r\nMezuniyetBasvurulariTezDosyaID:" + mezuniyetBasvurulariTezDosyaId;
                        break;
                    case MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklendi:
                        msg = "Tez şablon dosyası yüklendi bilgisi mail olarak gönderilirken bir hata oluştu! \r\nMezuniyetBasvurulariTezDosyaID:" + mezuniyetBasvurulariTezDosyaId;
                        break;
                }

                SistemBilgilendirmeBus.SistemBilgisiKaydet(msg + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                mmMessage.Messages.Add(msg + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }


        public static async Task SendTaslakBasvuruOgrenciye()
        {
            try
            {
                using (var entities = new LubsDbEntities())
                {

                    var nowDate = DateTime.Now;
                    var mezuniyetOtoMailIds = new List<int> { MailSablonTipiEnum.MezBasvuruTaslakHalinde };
                    var mezuniyetBasvurulari =
                        (from mez in entities.MezuniyetBasvurularis
                         join surec in entities.MezuniyetSurecis on mez.MezuniyetSurecID equals surec.MezuniyetSurecID
                         join enst in entities.Enstitulers on mez.MezuniyetSureci.EnstituKod equals enst.EnstituKod
                         join ogrenci in entities.Kullanicilars on mez.KullaniciID equals ogrenci.KullaniciID
                         join ogrenimTipKriter in entities.MezuniyetSureciOgrenimTipKriterleris on new { mez.MezuniyetSurecID, mez.OgrenimTipKod } equals new { ogrenimTipKriter.MezuniyetSurecID, ogrenimTipKriter.OgrenimTipKod }
                         join otoMail in entities.MezuniyetSureciOtoMails.Where(p => p.IsAktif && mezuniyetOtoMailIds.Contains(p.MailSablonTipID)) on new { mez.MezuniyetSurecID } equals new { otoMail.MezuniyetSurecID }
                         where mez.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Taslak &&
                               surec.IsAktif &&
                               surec.BitisTarihi >= nowDate &&
                               otoMail.MezuniyetSureciOtoMailGonderilenlers.All(a => a.MezuniyetBasvurulariID != mez.MezuniyetBasvurulariID) &&
                               DbFunctions.DiffDays(nowDate, surec.BitisTarihi) <= otoMail.Sure

                         select new
                         {
                             enst.EnstituKod,
                             enst.EnstituAd,
                             enst.WebAdresi,
                             enst.SistemErisimAdresi,
                             mez.KullaniciID,
                             mez.MezuniyetBasvurulariID,
                             mez.RowID,
                             mez.OgrenciNo,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             OgrenciEmail = ogrenci.EMail,
                             surec.BaslangicTarihi,
                             surec.BitisTarihi,
                             otoMail.MailSablonTipID,
                             otoMail.MezuniyetSureciOtoMailID
                         }).ToList();
                    if (!mezuniyetBasvurulari.Any()) return;
                    var sablonTipIds = mezuniyetBasvurulari.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {


                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod),
                            EnstituAdi = basvuru.EnstituAd,
                            WebAdresi = basvuru.WebAdresi,
                            SistemErisimAdresi = basvuru.SistemErisimAdresi
                        };
                        if (item.Sablon == null) continue;

                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());
                        item.EMails.Add(new MailSendList { EMail = basvuru.OgrenciEmail, KullaniciId = basvuru.KullaniciID, ToOrBcc = true });


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = item.EnstituAdi });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = item.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@SurecBaslangicTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SurecBaslangicTarihi", Value = basvuru.BaslangicTarihi.ToFormatDateAndTime() });
                        if (item.SablonParametreleri.Any(a => a == "@SurecBitisTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SurecBitisTarihi", Value = basvuru.BitisTarihi.ToFormatDateAndTime() });


                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);

                        if (!snded) continue;
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title += " (" + basvuru.OgrenciAdSoyad + ")",
                            IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId,
                            IslemYapanIP = "::1",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris,
                            GonderilenMailEkleris = item.GetGonderilenMailEkleris
                        };
                        entities.GonderilenMaillers.Add(kModel);
                        entities.MezuniyetSureciOtoMailGonderilenlers.Add(new MezuniyetSureciOtoMailGonderilenler
                        {
                            MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID,
                            MezuniyetSureciOtoMailID = basvuru.MezuniyetSureciOtoMailID,
                            Tarih = DateTime.Now

                        });

                    }

                    await entities.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata, GlobalSistemSetting.SystemDefaultAdminKullaniciId, "::1");
            }
        }
        public static async Task SendMailMezuniyetEykTarihineGoreSrAlinmaliOgrenciyeDanismana()
        {
            try
            {
                using (var entities = new LubsDbEntities())
                {

                    var nowDate = DateTime.Now.Date;
                    var mezuniyetOtoMailIds = new List<int> { MailSablonTipiEnum.MezEykTarihineGoreSrAlinmali };
                    var mezuniyetBasvurulari =
                        (from mez in entities.MezuniyetBasvurularis
                         join enst in entities.Enstitulers on mez.MezuniyetSureci.EnstituKod equals enst.EnstituKod
                         join ogrenci in entities.Kullanicilars on mez.KullaniciID equals ogrenci.KullaniciID
                         join danisman in entities.Kullanicilars on mez.TezDanismanID equals danisman.KullaniciID
                         join ogrenimTipKriter in entities.MezuniyetSureciOgrenimTipKriterleris on new { mez.MezuniyetSurecID, mez.OgrenimTipKod } equals new { ogrenimTipKriter.MezuniyetSurecID, ogrenimTipKriter.OgrenimTipKod }
                         join otoMail in entities.MezuniyetSureciOtoMails.Where(p => p.IsAktif && mezuniyetOtoMailIds.Contains(p.MailSablonTipID)) on new { mez.MezuniyetSurecID } equals new { otoMail.MezuniyetSurecID }
                         where mez.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi &&
                               !mez.IsMezunOldu.HasValue &&
                               mez.EYKTarihi.HasValue &&
                               mez.MezuniyetJuriOneriFormlaris.Any() &&
                               !mez.SRTalepleris.Any() &&
                               otoMail.MezuniyetSureciOtoMailGonderilenlers.All(a => a.MezuniyetBasvurulariID != mez.MezuniyetBasvurulariID) &&
                               DbFunctions.DiffDays(nowDate, DbFunctions.AddDays(mez.EYKTarihi, ogrenimTipKriter.TezTeslimSuresiGun)) >= 0 &&
                               DbFunctions.DiffDays(nowDate, DbFunctions.AddDays(mez.EYKTarihi, ogrenimTipKriter.TezTeslimSuresiGun)) <= otoMail.Sure

                         select new
                         {
                             enst.EnstituKod,
                             enst.EnstituAd,
                             enst.WebAdresi,
                             enst.SistemErisimAdresi,
                             mez.KullaniciID,
                             mez.MezuniyetBasvurulariID,
                             mez.OgrenciNo,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             OgrenciEmail = ogrenci.EMail,
                             mez.TezDanismanID,
                             DanismanEmail = danisman.EMail,
                             DanismanAdSoyad = danisman.Ad + " " + danisman.Soyad,
                             DanismanUnvanAdi = danisman.Unvanlar.UnvanAdi,
                             mez.TezEsDanismanEMail,
                             mez.EYKTarihi,
                             mez.MezuniyetSinavDurumID,
                             ogrenimTipKriter.SinavUzatmaSinavAlmaSuresiMaxGun,
                             ogrenimTipKriter.TezTeslimSuresiGun,
                             otoMail.MailSablonTipID,
                             otoMail.MezuniyetSureciOtoMailID,
                             SonTar = DbFunctions.AddDays(mez.EYKTarihi, ogrenimTipKriter.TezTeslimSuresiGun),
                             otoMail.Sure

                         }).ToList();
                    if (!mezuniyetBasvurulari.Any()) return;

                    var sablonTipIds = mezuniyetBasvurulari.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIds.Contains(p.MailSablonTipID)).ToList();



                    foreach (var basvuru in mezuniyetBasvurulari)
                    {

                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod),
                            EnstituAdi = basvuru.EnstituAd,
                            WebAdresi = basvuru.WebAdresi,
                            SistemErisimAdresi = basvuru.SistemErisimAdresi,
                            AdSoyad = basvuru.OgrenciAdSoyad
                        };
                        if (item.Sablon == null) continue;
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());

                        item.EMails.Add(new MailSendList { EMail = basvuru.DanismanEmail, KullaniciId = basvuru.TezDanismanID, ToOrBcc = true });
                        if (!basvuru.TezEsDanismanEMail.IsNullOrWhiteSpace()) item.EMails.Add(new MailSendList { EMail = basvuru.TezEsDanismanEMail, ToOrBcc = true });
                        item.EMails.Add(new MailSendList { EMail = basvuru.OgrenciEmail, KullaniciId = basvuru.KullaniciID, ToOrBcc = true });


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });

                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = basvuru.EYKTarihi.ToFormatDate() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title += " (" + item.AdSoyad + ")",
                            IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId,
                            IslemYapanIP = "::1",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris,
                            GonderilenMailEkleris = item.GetGonderilenMailEkleris
                        };

                        entities.GonderilenMaillers.Add(kModel);
                        entities.MezuniyetSureciOtoMailGonderilenlers.Add(new MezuniyetSureciOtoMailGonderilenler
                        {
                            MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID,
                            MezuniyetSureciOtoMailID = basvuru.MezuniyetSureciOtoMailID,
                            Tarih = DateTime.Now

                        });
                    }

                    await entities.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata, GlobalSistemSetting.SystemDefaultAdminKullaniciId, "::1");
            }
        }
        public static async Task SendMailMezuniyetEykTarihineGoreSrAlinmadiEnstituye()
        {
            try
            {
                using (var entities = new LubsDbEntities())
                {

                    var nowDate = DateTime.Now.Date;
                    var mezuniyetOtoMailIds = new List<int> { MailSablonTipiEnum.MezEykTarihineGoreSrAlinmadi };
                    var mezuniyetBasvurulari =
                        (from mez in entities.MezuniyetBasvurularis
                         join enst in entities.Enstitulers on mez.MezuniyetSureci.EnstituKod equals enst.EnstituKod
                         join ogrenci in entities.Kullanicilars on mez.KullaniciID equals ogrenci.KullaniciID
                         join danisman in entities.Kullanicilars on mez.TezDanismanID equals danisman.KullaniciID
                         join ogrenimTipKriter in entities.MezuniyetSureciOgrenimTipKriterleris on new { mez.MezuniyetSurecID, mez.OgrenimTipKod } equals new { ogrenimTipKriter.MezuniyetSurecID, ogrenimTipKriter.OgrenimTipKod }
                         join otoMail in entities.MezuniyetSureciOtoMails.Where(p => p.IsAktif && mezuniyetOtoMailIds.Contains(p.MailSablonTipID)) on new { mez.MezuniyetSurecID } equals new { otoMail.MezuniyetSurecID }
                         where mez.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi &&
                               !mez.IsMezunOldu.HasValue &&
                               mez.EYKTarihi.HasValue &&
                               mez.MezuniyetJuriOneriFormlaris.Any() &&
                               !mez.SRTalepleris.Any() &&
                               otoMail.MezuniyetSureciOtoMailGonderilenlers.All(a => a.MezuniyetBasvurulariID != mez.MezuniyetBasvurulariID) &&
                               DbFunctions.DiffDays(DbFunctions.AddDays(mez.EYKTarihi, ogrenimTipKriter.TezTeslimSuresiGun), nowDate) >= otoMail.Sure

                         select new
                         {
                             enst.EnstituKod,
                             enst.EnstituAd,
                             enst.WebAdresi,
                             enst.SistemErisimAdresi,
                             mez.MezuniyetBasvurulariID,
                             mez.OgrenciNo,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             mez.Programlar.AnabilimDallari.AnabilimDaliAdi,
                             OgrenciEmail = ogrenci.EMail,
                             DanismanEmail = danisman.EMail,
                             mez.TezEsDanismanEMail,
                             mez.EYKTarihi,
                             otoMail.MailSablonTipID,
                             otoMail.MezuniyetSureciOtoMailID
                         }).ToList();
                    if (!mezuniyetBasvurulari.Any()) return;

                    var sablonTipIds = mezuniyetBasvurulari.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {
                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod),
                            EnstituAdi = basvuru.EnstituAd,
                            WebAdresi = basvuru.WebAdresi,
                            SistemErisimAdresi = basvuru.SistemErisimAdresi,
                            AdSoyad = basvuru.OgrenciAdSoyad
                        };
                        if (item.Sablon == null) continue;
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());



                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = basvuru.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = basvuru.EYKTarihi.ToFormatDate() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;

                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title += " (" + item.AdSoyad + ")",
                            IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId,
                            IslemYapanIP = "::1",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris,
                            GonderilenMailEkleris = item.GetGonderilenMailEkleris
                        };

                        entities.GonderilenMaillers.Add(kModel);
                        entities.MezuniyetSureciOtoMailGonderilenlers.Add(new MezuniyetSureciOtoMailGonderilenler
                        {
                            MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID,
                            MezuniyetSureciOtoMailID = basvuru.MezuniyetSureciOtoMailID,
                            Tarih = DateTime.Now

                        });

                    }

                    await entities.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata, GlobalSistemSetting.SystemDefaultAdminKullaniciId, "::1");
            }
        }
        public static async Task SendMailMezuniyetDanismanDegerlendirmeHatirlatmaDanismana()
        {
            try
            {
                using (var entities = new LubsDbEntities())
                {

                    var nowDate = DateTime.Now;
                    var mezuniyetOtoMailIds = new List<int> { MailSablonTipiEnum.MezSinavDegerlendirmeHatirlantmaDanismanDr, MailSablonTipiEnum.MezSinavDegerlendirmeHatirlantmaDanismanYl };
                    var drOgrenimTipKods = OgrenimTipleriBus.DoktoraKods();

                    var mezuniyetBasvurulari =
                        (from mez in entities.MezuniyetBasvurularis
                         join enst in entities.Enstitulers on mez.MezuniyetSureci.EnstituKod equals enst.EnstituKod
                         join ogrenci in entities.Kullanicilars on mez.KullaniciID equals ogrenci.KullaniciID
                         join danisman in entities.Kullanicilars on mez.TezDanismanID equals danisman.KullaniciID
                         join ogrenimTipKriter in entities.MezuniyetSureciOgrenimTipKriterleris on new { mez.MezuniyetSurecID, mez.OgrenimTipKod } equals new { ogrenimTipKriter.MezuniyetSurecID, ogrenimTipKriter.OgrenimTipKod }
                         join otoMail in entities.MezuniyetSureciOtoMails.Where(p => p.IsAktif && mezuniyetOtoMailIds.Contains(p.MailSablonTipID)) on new { mez.MezuniyetSurecID } equals new { otoMail.MezuniyetSurecID }
                         join sonRezervasyon in entities.SRTalepleris.Where(p => p.SRDurumID == SrTalepDurumEnum.Onaylandı &&
                                                                                                     (!p.MezuniyetSinavDurumID.HasValue || p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.SonucGirilmedi) &&
                                                                                                     p.SRTaleplerJuris.Any(a => a.JuriTipAdi == "TezDanismani" &&
                                                                                                                                           (!a.MezuniyetSinavDurumID.HasValue || a.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.SonucGirilmedi)))
                                                       on mez.MezuniyetBasvurulariID equals sonRezervasyon.MezuniyetBasvurulariID
                         where mez.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi &&
                               !mez.IsMezunOldu.HasValue &&
                               mez.EYKTarihi.HasValue &&
                               mez.MezuniyetJuriOneriFormlaris.Any() &&
                               otoMail.MezuniyetSureciOtoMailGonderilenlers.All(a => a.MezuniyetBasvurulariID != mez.MezuniyetBasvurulariID) &&
                               DbFunctions.DiffHours(DbFunctions.CreateDateTime(sonRezervasyon.Tarih.Year, sonRezervasyon.Tarih.Month, sonRezervasyon.Tarih.Day, sonRezervasyon.BasSaat.Hours, sonRezervasyon.BasSaat.Minutes, 0), nowDate) >= otoMail.Sure


                         select new
                         {
                             enst.EnstituKod,
                             enst.EnstituAd,
                             enst.WebAdresi,
                             enst.SistemErisimAdresi,
                             mez.MezuniyetBasvurulariID,
                             mez.OgrenimTipKod,
                             mez.OgrenciNo,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             OgrenciEmail = ogrenci.EMail,
                             mez.TezDanismanID,
                             DanismanAdSoyad = danisman.Ad + " " + danisman.Soyad,
                             DanismanUnvanAdi = danisman.Unvanlar.UnvanAdi,
                             DanismanEmail = danisman.EMail,
                             mez.TezEsDanismanEMail,
                             mez.EYKTarihi,
                             SinavTarihi = sonRezervasyon.Tarih,
                             SinavSaati = sonRezervasyon.BasSaat,
                             otoMail.MailSablonTipID,
                             otoMail.MezuniyetSureciOtoMailID,
                             otoMail.Sure,
                             Sure1 = DbFunctions.DiffHours(DbFunctions.CreateDateTime(sonRezervasyon.Tarih.Year, sonRezervasyon.Tarih.Month, sonRezervasyon.Tarih.Day, sonRezervasyon.BasSaat.Hours, sonRezervasyon.BasSaat.Minutes, 0), nowDate),
                         }).ToList();
                    if (!mezuniyetBasvurulari.Any()) return;
                    var sablonTipIds = mezuniyetBasvurulari.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {
                        var mailSablonTipId = drOgrenimTipKods.Contains(basvuru.OgrenimTipKod) ? MailSablonTipiEnum.MezSinavDegerlendirmeHatirlantmaDanismanDr : MailSablonTipiEnum.MezSinavDegerlendirmeHatirlantmaDanismanYl;

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
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = basvuru.DanismanAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = basvuru.DanismanUnvanAdi });
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
                        entities.MezuniyetSureciOtoMailGonderilenlers.Add(new MezuniyetSureciOtoMailGonderilenler
                        {
                            MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID,
                            MezuniyetSureciOtoMailID = basvuru.MezuniyetSureciOtoMailID,
                            Tarih = DateTime.Now

                        });

                    }

                    await entities.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata, GlobalSistemSetting.SystemDefaultAdminKullaniciId, "::1");
            }
        }
        public static async Task SendMailMezuniyetSinavSonucuGirilmediOgrenciyeDanismana()
        {
            try
            {
                using (var entities = new LubsDbEntities())
                {

                    var nowDate = DateTime.Now.Date;
                    var mezuniyetOtoMailIds = new List<int> { MailSablonTipiEnum.MezTezSinavSonucuSistemeGirilmedi };

                    var mezuniyetBasvurulari =
                        (from mez in entities.MezuniyetBasvurularis
                         join enst in entities.Enstitulers on mez.MezuniyetSureci.EnstituKod equals enst.EnstituKod
                         join ogrenci in entities.Kullanicilars on mez.KullaniciID equals ogrenci.KullaniciID
                         join program in entities.Programlars on mez.ProgramKod equals program.ProgramKod
                         join anabilimDali in entities.AnabilimDallaris on program.AnabilimDaliID equals anabilimDali.AnabilimDaliID
                         join danisman in entities.Kullanicilars on mez.TezDanismanID equals danisman.KullaniciID
                         join ogrenimTipKriter in entities.MezuniyetSureciOgrenimTipKriterleris on new { mez.MezuniyetSurecID, mez.OgrenimTipKod } equals new { ogrenimTipKriter.MezuniyetSurecID, ogrenimTipKriter.OgrenimTipKod }
                         join otoMail in entities.MezuniyetSureciOtoMails.Where(p => p.IsAktif && mezuniyetOtoMailIds.Contains(p.MailSablonTipID)) on new { mez.MezuniyetSurecID } equals new { otoMail.MezuniyetSurecID }
                         join sonRezervasyon in entities.SRTalepleris.Where(p => p.SRDurumID == SrTalepDurumEnum.Onaylandı && p.JuriSonucMezuniyetSinavDurumID > MezuniyetSinavDurumEnum.SonucGirilmedi && (!p.MezuniyetSinavDurumID.HasValue || p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.SonucGirilmedi))
                                                       on mez.MezuniyetBasvurulariID equals sonRezervasyon.MezuniyetBasvurulariID
                         where mez.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi &&
                               (!mez.MezuniyetSinavDurumID.HasValue || mez.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.SonucGirilmedi) &&
                               !mez.IsMezunOldu.HasValue &&
                               mez.EYKTarihi.HasValue &&
                               mez.MezuniyetJuriOneriFormlaris.Any() &&
                               otoMail.MezuniyetSureciOtoMailGonderilenlers.All(a => a.MezuniyetBasvurulariID != mez.MezuniyetBasvurulariID) &&
                                DbFunctions.DiffDays(sonRezervasyon.Tarih, nowDate) >= otoMail.Sure
                         select new
                         {
                             enst.EnstituKod,
                             enst.EnstituAd,
                             enst.WebAdresi,
                             enst.SistemErisimAdresi,
                             mez.KullaniciID,
                             mez.MezuniyetBasvurulariID,
                             mez.OgrenciNo,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             OgrenciEmail = ogrenci.EMail,
                             program.ProgramAdi,
                             anabilimDali.AnabilimDaliAdi,
                             mez.TezDanismanID,
                             DanismanEmail = danisman.EMail,
                             SinavTarihi = sonRezervasyon.Tarih,
                             SinavSaati = sonRezervasyon.BasSaat,
                             otoMail.MailSablonTipID,
                             otoMail.MezuniyetSureciOtoMailID
                         }).ToList();
                    if (!mezuniyetBasvurulari.Any()) return;
                    var sablonTipIds = mezuniyetBasvurulari.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {


                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod),
                            EnstituAdi = basvuru.EnstituAd,
                            WebAdresi = basvuru.WebAdresi,
                            SistemErisimAdresi = basvuru.SistemErisimAdresi

                        };
                        if (item.Sablon == null) continue;
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());

                        item.EMails.Add(new MailSendList { EMail = basvuru.OgrenciEmail, KullaniciId = basvuru.KullaniciID, ToOrBcc = true });
                        item.EMails.Add(new MailSendList { EMail = basvuru.DanismanEmail, KullaniciId = basvuru.TezDanismanID, ToOrBcc = true });


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = basvuru.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = basvuru.AnabilimDaliAdi });

                        if (item.SablonParametreleri.Any(a => a == "@SRTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SRTarihi", Value = basvuru.SinavTarihi.ToFormatDate() + " " + basvuru.SinavSaati.ToFormatTime() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;

                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title += " (" + basvuru.OgrenciAdSoyad + ")",
                            IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId,
                            IslemYapanIP = "::1",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris,
                            GonderilenMailEkleris = item.GetGonderilenMailEkleris
                        };
                        entities.GonderilenMaillers.Add(kModel);
                        entities.MezuniyetSureciOtoMailGonderilenlers.Add(new MezuniyetSureciOtoMailGonderilenler
                        {
                            MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID,
                            MezuniyetSureciOtoMailID = basvuru.MezuniyetSureciOtoMailID,
                            Tarih = DateTime.Now

                        });

                    }

                    await entities.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata, GlobalSistemSetting.SystemDefaultAdminKullaniciId, "::1");
            }
        }
        public static async Task SendMailMezuniyetTezKontrolTezDosyasiYuklenmeliOgrenciye()
        {
            try
            {
                using (var entities = new LubsDbEntities())
                {

                    var nowDate = DateTime.Now.Date;
                    var mezuniyetOtoMailIds = new List<int> { MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklenmeli };
                    var mezuniyetBasvurulari =
                        (from mez in entities.MezuniyetBasvurularis
                         join enst in entities.Enstitulers on mez.MezuniyetSureci.EnstituKod equals enst.EnstituKod
                         join ogrenci in entities.Kullanicilars on mez.KullaniciID equals ogrenci.KullaniciID
                         join program in entities.Programlars on mez.ProgramKod equals program.ProgramKod
                         join anabilimDali in entities.AnabilimDallaris on program.AnabilimDaliID equals anabilimDali.AnabilimDaliID
                         join danisman in entities.Kullanicilars on mez.TezDanismanID equals danisman.KullaniciID
                         join ogrenimTipKriter in entities.MezuniyetSureciOgrenimTipKriterleris on new { mez.MezuniyetSurecID, mez.OgrenimTipKod } equals new { ogrenimTipKriter.MezuniyetSurecID, ogrenimTipKriter.OgrenimTipKod }
                         join otoMail in entities.MezuniyetSureciOtoMails.Where(p => p.IsAktif && mezuniyetOtoMailIds.Contains(p.MailSablonTipID)) on new { mez.MezuniyetSurecID } equals new { otoMail.MezuniyetSurecID }
                         join sonRezervasyon in entities.SRTalepleris.Where(p => p.SRDurumID == SrTalepDurumEnum.Onaylandı && p.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili)
                                                       on mez.MezuniyetBasvurulariID equals sonRezervasyon.MezuniyetBasvurulariID
                         where mez.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi && mez.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili &&
                               !mez.IsMezunOldu.HasValue &&
                               mez.EYKTarihi.HasValue &&
                               !mez.MezuniyetBasvurulariTezDosyalaris.Any() &&
                               !mez.MezuniyetBasvurulariTezTeslimFormlaris.Any() &&
                               otoMail.MezuniyetSureciOtoMailGonderilenlers.All(a => a.MezuniyetBasvurulariID != mez.MezuniyetBasvurulariID) &&
                               DbFunctions.DiffDays(sonRezervasyon.Tarih, nowDate) >= otoMail.Sure


                         select new
                         {
                             enst.EnstituKod,
                             enst.EnstituAd,
                             enst.WebAdresi,
                             enst.SistemErisimAdresi,
                             mez.KullaniciID,
                             mez.MezuniyetBasvurulariID,
                             mez.RowID,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             OgrenciEmail = ogrenci.EMail,
                             SinavTarihi = sonRezervasyon.Tarih,
                             SinavSaati = sonRezervasyon.BasSaat,
                             otoMail.MailSablonTipID,
                             otoMail.MezuniyetSureciOtoMailID
                         }).ToList();
                    if (!mezuniyetBasvurulari.Any()) return;
                    var sablonTipIds = mezuniyetBasvurulari.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {


                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod),
                            EnstituAdi = basvuru.EnstituAd,
                            WebAdresi = basvuru.WebAdresi,
                            SistemErisimAdresi = basvuru.SistemErisimAdresi
                        };
                        if (item.Sablon == null) continue;
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());

                        item.EMails.Add(new MailSendList { EMail = basvuru.OgrenciEmail, KullaniciId = basvuru.KullaniciID, ToOrBcc = true });


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@SRTarihi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "SRTarihi", Value = basvuru.SinavTarihi.ToFormatDate() + " " + basvuru.SinavSaati.ToFormatTime() });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = basvuru.SistemErisimAdresi + "/mezuniyet/Index?RowID=" + basvuru.RowID, IsLink = true });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title += " (" + basvuru.OgrenciAdSoyad + ")",
                            IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId,
                            IslemYapanIP = "::1",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris,
                            GonderilenMailEkleris = item.GetGonderilenMailEkleris
                        };
                        entities.GonderilenMaillers.Add(kModel);
                        entities.MezuniyetSureciOtoMailGonderilenlers.Add(new MezuniyetSureciOtoMailGonderilenler
                        {
                            MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID,
                            MezuniyetSureciOtoMailID = basvuru.MezuniyetSureciOtoMailID,
                            Tarih = DateTime.Now

                        });

                    }

                    await entities.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata, GlobalSistemSetting.SystemDefaultAdminKullaniciId, "::1");
            }
        }
        public static async Task SendMailCiltliTezTeslimYapilmaliOgrenciye()
        {
            try
            {
                using (var entities = new LubsDbEntities())
                {


                    var nowDate = DateTime.Now.Date;
                    var mezuniyetOtoMailIds = new List<int> { MailSablonTipiEnum.MezCiltliTezTeslimYapilmali };
                    var mezuniyetBasvurulari =
                        (from mez in entities.MezuniyetBasvurularis
                         join enst in entities.Enstitulers on mez.MezuniyetSureci.EnstituKod equals enst.EnstituKod
                         join ogrenci in entities.Kullanicilars on mez.KullaniciID equals ogrenci.KullaniciID
                         join program in entities.Programlars on mez.ProgramKod equals program.ProgramKod
                         join anabilimDali in entities.AnabilimDallaris on program.AnabilimDaliID equals anabilimDali.AnabilimDaliID
                         join danisman in entities.Kullanicilars on mez.TezDanismanID equals danisman.KullaniciID
                         join ogrenimTipKriter in entities.MezuniyetSureciOgrenimTipKriterleris on new { mez.MezuniyetSurecID, mez.OgrenimTipKod } equals new { ogrenimTipKriter.MezuniyetSurecID, ogrenimTipKriter.OgrenimTipKod }
                         join otoMail in entities.MezuniyetSureciOtoMails.Where(p => p.IsAktif && mezuniyetOtoMailIds.Contains(p.MailSablonTipID)) on new { mez.MezuniyetSurecID } equals new { otoMail.MezuniyetSurecID }
                         join sonRezervasyon in entities.SRTalepleris.Where(p => p.SRDurumID == SrTalepDurumEnum.Onaylandı && p.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili)
                                                       on mez.MezuniyetBasvurulariID equals sonRezervasyon.MezuniyetBasvurulariID
                         where mez.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi && mez.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili &&
                               !mez.IsMezunOldu.HasValue &&
                               mez.EYKTarihi.HasValue &&
                               mez.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true) &&
                               otoMail.MezuniyetSureciOtoMailGonderilenlers.All(a => a.MezuniyetBasvurulariID != mez.MezuniyetBasvurulariID) &&
                               DbFunctions.DiffDays(nowDate, (mez.TezTeslimSonTarih ?? DbFunctions.AddDays(sonRezervasyon.Tarih, ogrenimTipKriter.TezTeslimSuresiGun))) >= 0 &&
                               DbFunctions.DiffDays(nowDate, (mez.TezTeslimSonTarih ?? DbFunctions.AddDays(sonRezervasyon.Tarih, ogrenimTipKriter.TezTeslimSuresiGun))) <= otoMail.Sure


                         select new
                         {
                             enst.EnstituKod,
                             enst.EnstituAd,
                             enst.WebAdresi,
                             enst.SistemErisimAdresi,
                             mez.KullaniciID,
                             mez.MezuniyetBasvurulariID,
                             mez.RowID,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             OgrenciEmail = ogrenci.EMail,
                             mez.TezDanismanID,
                             DanismanEmail = danisman.EMail,
                             SinavTarihi = sonRezervasyon.Tarih,
                             SinavSaati = sonRezervasyon.BasSaat,
                             otoMail.MailSablonTipID,
                             otoMail.MezuniyetSureciOtoMailID,
                             TezTeslimSonTarih = (mez.TezTeslimSonTarih ?? DbFunctions.AddDays(sonRezervasyon.Tarih, ogrenimTipKriter.TezTeslimSuresiGun))
                         }).ToList();
                    if (!mezuniyetBasvurulari.Any()) return;
                    var sablonTipIds = mezuniyetBasvurulari.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {


                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod),
                            EnstituAdi = basvuru.EnstituAd,
                            WebAdresi = basvuru.WebAdresi,
                            SistemErisimAdresi = basvuru.SistemErisimAdresi
                        };
                        if (item.Sablon == null) continue;
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());

                        item.EMails.Add(new MailSendList { EMail = basvuru.OgrenciEmail, KullaniciId = basvuru.KullaniciID, ToOrBcc = true });
                        item.EMails.Add(new MailSendList { EMail = basvuru.DanismanEmail, KullaniciId = basvuru.TezDanismanID, ToOrBcc = true });


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@CiltTeslimTarih"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "CiltTeslimTarih", Value = basvuru.TezTeslimSonTarih.ToFormatDate() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title += " (" + basvuru.OgrenciAdSoyad + ")",
                            IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId,
                            IslemYapanIP = "::1",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris,
                            GonderilenMailEkleris = item.GetGonderilenMailEkleris
                        };
                        entities.GonderilenMaillers.Add(kModel);
                        entities.MezuniyetSureciOtoMailGonderilenlers.Add(new MezuniyetSureciOtoMailGonderilenler
                        {
                            MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID,
                            MezuniyetSureciOtoMailID = basvuru.MezuniyetSureciOtoMailID,
                            Tarih = DateTime.Now

                        });

                    }

                    await entities.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata, GlobalSistemSetting.SystemDefaultAdminKullaniciId, "::1");
            }
        }
        public static async Task SendMailCiltliTezTeslimYapilmadiEnstituye()
        {
            try
            {
                using (var entities = new LubsDbEntities())
                {


                    var nowDate = DateTime.Now.Date;
                    var mezuniyetOtoMailIds = new List<int> { MailSablonTipiEnum.MezCiltliTezTeslimYapilmadi };
                    var mezuniyetBasvurulari =
                        (from mez in entities.MezuniyetBasvurularis
                         join enst in entities.Enstitulers on mez.MezuniyetSureci.EnstituKod equals enst.EnstituKod
                         join ogrenci in entities.Kullanicilars on mez.KullaniciID equals ogrenci.KullaniciID
                         join program in entities.Programlars on mez.ProgramKod equals program.ProgramKod
                         join anabilimDali in entities.AnabilimDallaris on program.AnabilimDaliID equals anabilimDali.AnabilimDaliID
                         join danisman in entities.Kullanicilars on mez.TezDanismanID equals danisman.KullaniciID
                         join ogrenimTipKriter in entities.MezuniyetSureciOgrenimTipKriterleris on new { mez.MezuniyetSurecID, mez.OgrenimTipKod } equals new { ogrenimTipKriter.MezuniyetSurecID, ogrenimTipKriter.OgrenimTipKod }
                         join otoMail in entities.MezuniyetSureciOtoMails.Where(p => p.IsAktif && mezuniyetOtoMailIds.Contains(p.MailSablonTipID)) on new { mez.MezuniyetSurecID } equals new { otoMail.MezuniyetSurecID }
                         join sonRezervasyon in entities.SRTalepleris.Where(p => p.SRDurumID == SrTalepDurumEnum.Onaylandı && p.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili)
                                                       on mez.MezuniyetBasvurulariID equals sonRezervasyon.MezuniyetBasvurulariID
                         where mez.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi && mez.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili &&
                               !mez.IsMezunOldu.HasValue &&
                               mez.EYKTarihi.HasValue &&
                               otoMail.MezuniyetSureciOtoMailGonderilenlers.All(a => a.MezuniyetBasvurulariID != mez.MezuniyetBasvurulariID) &&
                               DbFunctions.DiffDays((mez.TezTeslimSonTarih ?? DbFunctions.AddDays(sonRezervasyon.Tarih, ogrenimTipKriter.TezTeslimSuresiGun)), nowDate) >= otoMail.Sure


                         select new
                         {
                             enst.EnstituKod,
                             enst.EnstituAd,
                             enst.WebAdresi,
                             enst.SistemErisimAdresi,
                             mez.MezuniyetBasvurulariID,
                             mez.RowID,
                             mez.OgrenciNo,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             OgrenciEmail = ogrenci.EMail,
                             program.ProgramAdi,
                             anabilimDali.AnabilimDaliAdi,
                             SinavTarihi = sonRezervasyon.Tarih,
                             SinavSaati = sonRezervasyon.BasSaat,
                             otoMail.MailSablonTipID,
                             otoMail.MezuniyetSureciOtoMailID,
                             TezTeslimSonTarih = (mez.TezTeslimSonTarih ?? DbFunctions.AddDays(sonRezervasyon.Tarih, ogrenimTipKriter.TezTeslimSuresiGun))
                         }).ToList();
                    if (!mezuniyetBasvurulari.Any()) return;
                    var sablonTipIds = mezuniyetBasvurulari.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => p.IsAktif && sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {
                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod),
                            EnstituAdi = basvuru.EnstituAd,
                            WebAdresi = basvuru.WebAdresi,
                            SistemErisimAdresi = basvuru.SistemErisimAdresi
                        };
                        if (item.Sablon == null) continue;
                        item.SablonEkleri.AddRange(item.Sablon.MailSablonlariEkleris);


                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                        item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = basvuru.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = basvuru.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@CiltTeslimTarih"))
                            item.MailParameterDtos.Add(new MailParameterDto { Key = "CiltTeslimTarih", Value = basvuru.TezTeslimSonTarih.ToFormatDate() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(item);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);

                        if (!snded) continue;
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title += " (" + basvuru.OgrenciAdSoyad + ")",
                            IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId,
                            IslemYapanIP = "::1",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.GetGonderilenMailKullanicilaris,
                            GonderilenMailEkleris = item.GetGonderilenMailEkleris
                        };
                        entities.GonderilenMaillers.Add(kModel);
                        entities.MezuniyetSureciOtoMailGonderilenlers.Add(new MezuniyetSureciOtoMailGonderilenler
                        {
                            MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID,
                            MezuniyetSureciOtoMailID = basvuru.MezuniyetSureciOtoMailID,
                            Tarih = DateTime.Now

                        });

                    }

                    await entities.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata, GlobalSistemSetting.SystemDefaultAdminKullaniciId, "::1");
            }
        }
    }
}