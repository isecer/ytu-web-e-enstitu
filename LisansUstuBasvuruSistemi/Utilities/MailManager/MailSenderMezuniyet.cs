using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web;
using BiskaUtil;
using DevExpress.Web.Internal.XmlProcessor;
using DevExpress.XtraCharts;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Ws_GsisMezuniyetBilgi;

namespace LisansUstuBasvuruSistemi.Utilities.MailManager
{
    public class MailSenderMezuniyet
    {
        public static MmMessage SendMailBasvuruDanismanOnay(int mezuniyetBasvurulariId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
                {
                    var mBasvur =
                        entities.MezuniyetBasvurularis.First(f => f.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                    var enstitu = mBasvur.MezuniyetSureci.Enstituler;
                    var sablonTipId = mBasvur.IsDanismanOnay == true ? MailSablonTipiEnum.MezDanismanOnayladiOgrenci : MailSablonTipiEnum.MezDanismanOnaylamadiOgrenci;
                    var sablonlar = entities.MailSablonlaris.Where(p => p.EnstituKod == enstitu.EnstituKod).ToList();



                    var mModel = new List<SablonMailModel>
                        {
                            new SablonMailModel
                            {

                                AdSoyad = mBasvur.Ad + " " + mBasvur.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = mBasvur.Kullanicilar.EMail, ToOrBcc = true } },
                                MailSablonTipID = sablonTipId,
                            }
                        };

                    var danisman = entities.Kullanicilars.First(p => p.KullaniciID == mBasvur.TezDanismanID);
                    foreach (var item in mModel)
                    {
                        var basvuruDonemAdi = mBasvur.MezuniyetSureci.BaslangicYil + " " + mBasvur.MezuniyetSureci.BitisYil + " / " + mBasvur.MezuniyetSureci.Donemler.DonemAdi;
                        var enstituL = mBasvur.MezuniyetSureci.Enstituler;

                        item.Sablon = sablonlar.First(p => p.MailSablonTipID == item.MailSablonTipID);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var mailParameterDtos = new List<MailParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@BasvuruDonemAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "BasvuruDonemAdi", Value = basvuruDonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = mBasvur.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = mBasvur.Ad + " " + mBasvur.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "RetAciklamasi", Value = mBasvur.DanismanOnayAciklama });

                        var attachs = new List<System.Net.Mail.Attachment>();

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, attachs);
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
                                GonderilenMailEkleris = attachs.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList(),
                                GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList()
                            };
                            entities.GonderilenMaillers.Add(kModel);
                            entities.SaveChanges();
                        }
                    }

                }
            }
            catch (Exception ex)
            {


                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet Danışman onay sonuç maili gönderilirken bir hata oluştu.\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
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
                using (var entities = new LisansustuBasvuruSistemiEntities())
                {
                    var mBasvur =
                        entities.MezuniyetBasvurularis.First(f => f.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                    var enstitu = mBasvur.MezuniyetSureci.Enstituler;
                    var sablonlar = entities.MailSablonlaris.Where(p => p.EnstituKod == enstitu.EnstituKod).ToList();


                    var mModel = new List<SablonMailModel>();
                    if (mBasvur.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.IptalEdildi)
                    {
                        var danisman = entities.Kullanicilars.First(p => p.KullaniciID == mBasvur.TezDanismanID);
                        mModel.Add(
                            new SablonMailModel
                            {

                                MailSablonTipID = MailSablonTipiEnum.MezYayinSartiSaglandiDanisman,
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                UnvanAdi = danisman.Unvanlar.UnvanAdi
                            });
                    }
                    var ogrenciMailSablonId = 1;
                    if (mBasvur.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.IptalEdildi) ogrenciMailSablonId = MailSablonTipiEnum.MezYayinSartiSaglanamadiOgrenci;
                    else if (mBasvur.OgrenimTipKod.IsDoktora()) ogrenciMailSablonId = MailSablonTipiEnum.MezYayinSartiSaglandiOgrenciDoktora;
                    else ogrenciMailSablonId = MailSablonTipiEnum.MezYayinSartiSaglandiOgrenciYl;
                    mModel.Add(new SablonMailModel
                    {

                        AdSoyad = mBasvur.Ad + " " + mBasvur.Soyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = mBasvur.Kullanicilar.EMail, ToOrBcc = true } },
                        MailSablonTipID = ogrenciMailSablonId,
                    });


                    foreach (var item in mModel)
                    {
                        var basvuruDonemAdi = mBasvur.MezuniyetSureci.BaslangicYil + " " + mBasvur.MezuniyetSureci.BitisYil + " / " + mBasvur.MezuniyetSureci.Donemler.DonemAdi;
                        var enstituL = mBasvur.MezuniyetSureci.Enstituler;

                        item.Sablon = sablonlar.First(p => p.MailSablonTipID == item.MailSablonTipID);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var mailParameterDtos = new List<MailParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@BasvuruDonemAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "BasvuruDonemAdi", Value = basvuruDonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = mBasvur.Ad + " " + mBasvur.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@IptalAciklamasi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "IptalAciklamasi", Value = mBasvur.MezuniyetYayinKontrolDurumAciklamasi });

                        var attachs = new List<System.Net.Mail.Attachment>();

                        if (item.MailSablonTipID != MailSablonTipiEnum.MezYayinSartiSaglandiDanisman && mBasvur.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.IptalEdildi)
                        {
                            attachs = Management.ExportRaporPdf(RaporTipiEnum.MezuniyetBasvuruRaporu, new List<int?> { mBasvur.MezuniyetBasvurulariID });
                        }
                        if (mBasvur.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
                        {
                            var ttfp = Management.ExportRaporPdf(RaporTipiEnum.MezuniyetTezTeslimFormu, new List<int?> { mBasvur.MezuniyetBasvurulariID, 1 });
                            attachs.AddRange(ttfp);
                        }
                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, attachs);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstitu.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = contentDetailDto.Title + " (" + item.AdSoyad + ")"
                            };
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu = kModel.Konu + " (" + item.JuriTipAdi + ")";
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


                }
            }
            catch (Exception ex)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet başvuru durumu değişikliği mail gönderme işlemi yapılırken bir hata oluştu.\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
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
                using (var entities = new LisansustuBasvuruSistemiEntities())
                {
                    var juriOneriFormu = entities.MezuniyetJuriOneriFormlaris.First(p => p.MezuniyetJuriOneriFormID == mezuniyetJuriOneriFormId);
                    var mBasvur = juriOneriFormu.MezuniyetBasvurulari;
                    var danismanSablonId = 0;
                    var asilSablonId = 0;
                    var ogrenciSablonId = 0;
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

                    var tezKonusu = "";
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
                    var sablonlar = entities.MailSablonlaris.Where(p => p.EnstituKod == enstitu.EnstituKod).ToList();

                    var mModel = new List<SablonMailModel> {
                            new SablonMailModel {

                            AdSoyad =mBasvur.Ad + " " + mBasvur.Soyad,
                            EMails= new List<MailSendList> { new MailSendList { EMail = mBasvur.Kullanicilar.EMail,ToOrBcc=true } },
                            MailSablonTipID=ogrenciSablonId
                            } };
                    var juriler = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek.HasValue).ToList();
                    foreach (var item in juriler.Where(p => p.IsAsilOrYedek == true))
                    {
                        mModel.Add(new SablonMailModel
                        {

                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                            MailSablonTipID = (item.JuriTipAdi == "TezDanismani" ? danismanSablonId : asilSablonId),
                            JuriTipAdi = item.JuriTipAdi,
                            UnvanAdi = item.UnvanAdi,
                            MezuniyetJuriOneriFormuJuriID = item.MezuniyetJuriOneriFormuJuriID,
                        });
                        if (item.JuriTipAdi == "TezDanismani" && !mBasvur.TezEsDanismanEMail.IsNullOrWhiteSpace())
                        {
                            //Eş danışman var ise Danışmana giden mail eş danışmana da gönderilmesi için.
                            mModel.Add(new SablonMailModel
                            {

                                AdSoyad = mBasvur.TezEsDanismanAdi,
                                EMails = new List<MailSendList> { new MailSendList { EMail = mBasvur.TezEsDanismanEMail, ToOrBcc = true } },
                                MailSablonTipID = danismanSablonId,
                                JuriTipAdi = item.JuriTipAdi,
                                UnvanAdi = mBasvur.TezEsDanismanUnvani,
                            });
                        }
                    }
                    var danisman = juriler.First(p => p.JuriTipAdi == "TezDanismani");

                    var enstituL = mBasvur.MezuniyetSureci.Enstituler;
                    var abdL = mBasvur.Programlar.AnabilimDallari;
                    var prgL = mBasvur.Programlar;
                    foreach (var item in mModel)
                    {

                        item.ProgramAdi = prgL.ProgramAdi;
                        item.Sablon = sablonlar.First(p => p.MailSablonTipID == item.MailSablonTipID);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        //Şablona ait ekler var ise attachmets e ekle
                        var gonderilenMailEkleri = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMailEkleri.Add(new GonderilenMailEkleri { EkAdi = itemSe.EkAdi, EkDosyaYolu = itemSe.EkDosyaYolu });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Uyarı);
                        }

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = mBasvur.EYKTarihi.Value.ToFormatDate() });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = mBasvur.Ad + " " + mBasvur.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciBilgi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciBilgi", Value = (mBasvur.OgrenciNo + " " + mBasvur.Ad + " " + mBasvur.Soyad + " (" + abdL.AnabilimDaliAdi + " / " + prgL.ProgramAdi + ")") });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tezKonusu });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanBilgi", Value = danisman.UnvanAdi + " " + danisman.AdSoyad });
                        foreach (var itemAsil in juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).Select((s, inx) => new { s, inx = inx + 1 }))
                            if (item.SablonParametreleri.Any(a => a == "@AsilBilgi" + itemAsil.inx))
                            {
                                var uniBilgi = "";
                                if (itemAsil.s.JuriTipAdi.Contains("YtuDisiJuri"))
                                {
                                    uniBilgi = " (" + (itemAsil.s.UniversiteAdi) + ")";
                                }
                                mailParameterDtos.Add(new MailParameterDto { Key = "AsilBilgi" + itemAsil.inx, Value = itemAsil.s.UnvanAdi + " " + itemAsil.s.AdSoyad + uniBilgi });
                            }
                        foreach (var itemYedek in juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == false).Select((s, inx) => new { s, inx = inx + 1 }))
                            if (item.SablonParametreleri.Any(a => a == "@YedekBilgi" + itemYedek.inx))
                            {
                                var uniBilgi = "";
                                if (itemYedek.s.JuriTipAdi.Contains("YtuDisiJuri"))
                                {
                                    uniBilgi = " (" + (itemYedek.s.UniversiteAdi) + ")";
                                }
                                mailParameterDtos.Add(new MailParameterDto { Key = "YedekBilgi" + itemYedek.inx, Value = itemYedek.s.UnvanAdi + " " + itemYedek.s.AdSoyad + uniBilgi });
                            }
                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        // item.EMails = new List<MailSendList> { new MailSendList { EMail = "irfansecer@gmail.com", ToOrBCC = true } }; //test için
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            gonderilenMailEkleri.AddRange(item.Attachments.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList());

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
                                GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList(),
                                GonderilenMailEkleris = gonderilenMailEkleri
                            };
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu = kModel.Konu + " (" + item.JuriTipAdi + ")";
                            entities.GonderilenMaillers.Add(kModel);
                            entities.SaveChanges();
                        }
                    }



                }
            }
            catch (Exception ex)
            {


                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet Jüri öneri formu onay sonuç maili gönderilirken bir hata oluştu.\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
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
                using (var db = new LisansustuBasvuruSistemiEntities())
                {

                    var srTalep = db.SRTalepleris.First(p => p.SRTalepID == srTalepId);
                    var qJuriler = srTalep.SRTaleplerJuris.AsQueryable();
                    qJuriler = uniqueId.HasValue ? qJuriler.Where(p => p.UniqueID == uniqueId.Value) : qJuriler.Where(p => p.JuriTipAdi != "TezDanismani");
                    var juriler = qJuriler.ToList();
                    var mb = srTalep.MezuniyetBasvurulari;
                    var mModel = new List<SablonMailModel>();

                    var enstitu = mb.MezuniyetSureci.Enstituler;

                    var abdL = mb.Programlar.AnabilimDallari;
                    var prgL = mb.Programlar;
                    var jof = mb.MezuniyetJuriOneriFormlaris.First();



                    if (isLinkOrSonuc)
                    {

                        foreach (var item in juriler)
                        {
                            if (isYeniLink) item.UniqueID = Guid.NewGuid();
                            mModel.Add(new SablonMailModel
                            {
                                UniqueID = item.UniqueID,

                                UnvanAdi = item.UnvanAdi,
                                AdSoyad = item.JuriAdi,
                                EMails = new List<MailSendList> { new MailSendList { EMail = (eMail.IsNullOrWhiteSpace() ? item.Email : eMail), ToOrBcc = true } },
                                MailSablonTipID = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipiEnum.MezSinavDegerlendirmeDavetGonderimJuriDr : MailSablonTipiEnum.MezSinavDegerlendirmeDavetGonderimJuriYl,
                                JuriTipAdi = item.JuriTipAdi,
                            });
                        }
                    }
                    else
                    {
                        var danisman = db.Kullanicilars.First(p => p.KullaniciID == mb.TezDanismanID);
                        var srDanisman = srTalep.SRTaleplerJuris.First(p => p.JuriTipAdi == "TezDanismani");
                        mModel.Add(new SablonMailModel
                        {
                            UniqueID = null,
                            UnvanAdi = danisman.Unvanlar.UnvanAdi,
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = !srDanisman.Email.IsNullOrWhiteSpace() ? srDanisman.Email : danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipiEnum.MezSinavSonucBilgiGonderimDanismanDr : MailSablonTipiEnum.MezSinavSonucBilgiGonderimDanismanYl,
                            JuriTipAdi = "TezDanismani",
                        });
                        var ogrenci = db.Kullanicilars.First(p => p.KullaniciID == mb.KullaniciID);
                        mModel.Add(new SablonMailModel
                        {
                            UniqueID = null,
                            UnvanAdi = "",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipiEnum.MezSinavSonucBilgiGonderimOgrenciDr : MailSablonTipiEnum.MezSinavSonucBilgiGonderimOgrenciYl,
                            JuriTipAdi = "",
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
                        if (!isLinkOrSonuc)
                        {
                            var ids = new List<int?>() { srTalepId };
                            if (item.JuriTipAdi == "TezDanismani")
                            {
                                ids.Add(1);
                            }
                            var ekler = Management.ExportRaporPdf(RaporTipiEnum.MezuniyetTezSinavSonucFormu, ids);
                            gonderilenMailEkleri.AddRange(ekler.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }));
                            item.Attachments.AddRange(ekler);
                        }
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(File.ReadAllBytes(ekTamYol)),
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
                        if (isLinkOrSonuc)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@JuriAdSoyad"))
                                mailParameterDtos.Add(new MailParameterDto { Key = "JuriAdSoyad", Value = item.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@JuriUnvanAdi"))
                                mailParameterDtos.Add(new MailParameterDto { Key = "JuriUnvanAdi", Value = item.UnvanAdi });
                        }
                        else
                        {
                            if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                                mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = mb.TezDanismanUnvani });
                            if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                                mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = mb.TezDanismanAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = mb.Ad + " " + mb.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = mb.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "SinavTarihi", Value = srTalep.Tarih.ToLongDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@SinavSaati"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "SinavSaati", Value = $"{srTalep.BasSaat:hh\\:mm}" });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/Mezuniyet/GSinavDegerlendir?UniqueID=" + item.UniqueID, IsLink = true });
                        }

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = enstitu.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title
                        };
                        if (snded)
                        {
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
                            if (isLinkOrSonuc)
                            {
                                var juri = juriler.FirstOrDefault(p => p.UniqueID == item.UniqueID);

                                juri.DegerlendirmeIslemTarihi = null;
                                juri.DegerlendirmeIslemYapanIP = null;
                                juri.DegerlendirmeYapanID = null;
                                juri.MezuniyetSinavDurumID = null;
                                juri.Aciklama = null;
                                juri.IsLinkGonderildi = true;
                                juri.LinkGonderimTarihi = DateTime.Now;
                                juri.LinkGonderenID = UserIdentity.Current.Id;
                                db.SaveChanges();
                                LogIslemleri.LogEkle("SRTaleplerJuri", LogCrudType.Update, juri.ToJson());
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
                var message = "";
                message = "Tez Sınavı değerlendirmesi için Jüri üyelerine değerlendirme davetiye linki mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
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
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var talep = db.SRTalepleris.First(p => p.SRTalepID == srTalepId);

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
                    var juriSablonTipId = 0;
                    var ogrenciSablonTipId = 0;
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
                        AdSoyad = mb.Ad + " " + mb.Soyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = mb.Kullanicilar.EMail, ToOrBcc = true } },
                        MailSablonTipID = ogrenciSablonTipId,
                    });
                    var danisman = juriler.First(p => p.JuriTipAdi == "TezDanismani");
                    if (!isOnaylandi)
                    {
                        mModel.Add(new SablonMailModel
                        {

                            AdSoyad = danisman.UnvanAdi + " " + danisman.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = juriSablonTipId,

                        });
                        if (!mb.TezEsDanismanEMail.IsNullOrWhiteSpace())
                            mModel.Add(new SablonMailModel
                            {

                                AdSoyad = mb.TezEsDanismanUnvani + " " + mb.TezEsDanismanAdi,
                                EMails = new List<MailSendList> { new MailSendList { EMail = mb.TezEsDanismanEMail, ToOrBcc = true } },
                                MailSablonTipID = juriSablonTipId,

                            });
                    }
                    else
                    {
                        foreach (var item in juriler.Where(p => p.IsAsilOrYedek == true))
                        {


                            mModel.Add(new SablonMailModel
                            {

                                AdSoyad = item.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                                MailSablonTipID = juriSablonTipId,
                                JuriTipAdi = item.JuriTipAdi,
                                UnvanAdi = item.UnvanAdi
                            });

                            if (item.JuriTipAdi == "TezDanismani" && !mb.TezEsDanismanEMail.IsNullOrWhiteSpace())
                            {
                                mModel.Add(new SablonMailModel
                                {

                                    AdSoyad = mb.TezEsDanismanAdi,
                                    EMails = new List<MailSendList> { new MailSendList { EMail = mb.TezEsDanismanEMail, ToOrBcc = true } },
                                    MailSablonTipID = juriSablonTipId,
                                    JuriTipAdi = item.JuriTipAdi,
                                    UnvanAdi = mb.TezEsDanismanUnvani
                                });
                            }
                        }


                    }


                    foreach (var item in mModel)
                    {

                        var abdL = mb.Programlar.AnabilimDallari;
                        var prgL = mb.Programlar;
                        item.Sablon = db.MailSablonlaris.First(p => p.IsAktif && p.MailSablonTipID == item.MailSablonTipID && p.EnstituKod == enstitu.EnstituKod);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
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
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = mb.EYKTarihi.Value.ToFormatDate() });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = mb.Ad + " " + mb.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = mb.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "SinavTarihi", Value = talep.Tarih.ToLongDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@SinavSaati"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "SinavSaati", Value = $"{talep.BasSaat:hh\\:mm}" + "-" + $"{talep.BitSaat:hh\\:mm}" });
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            var sinavYerAdi = talep.SRSalonID.HasValue ? talep.SRSalonlar.SalonAdi : talep.SalonAdi;
                            mailParameterDtos.Add(new MailParameterDto { Key = "SinavYeri", Value = sinavYerAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@IptalAciklamasi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "IptalAciklamasi", Value = talep.SRDurumAciklamasi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanBilgi", Value = danisman.UnvanAdi + " " + danisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUni"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUni", Value = danisman.UniversiteAdi });
                        }
                        foreach (var itemAsil in juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).Select((s, inx) => new { s, inx = inx + 1 }))
                        {

                            if (item.SablonParametreleri.Any(a => a == "@AsilBilgi" + itemAsil.inx))
                                mailParameterDtos.Add(new MailParameterDto { Key = "AsilBilgi" + itemAsil.inx, Value = itemAsil.s.UnvanAdi + " " + itemAsil.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@AsilBilgiUni" + itemAsil.inx))
                            {
                                mailParameterDtos.Add(new MailParameterDto { Key = "AsilBilgiUni" + itemAsil.inx, Value = itemAsil.s.UniversiteAdi });
                            }
                        }
                        foreach (var itemYedek in juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == false).Select((s, inx) => new { s, inx = inx + 1 }))
                        {
                            if (item.SablonParametreleri.Any(a => a == "@YedekBilgi" + itemYedek.inx))
                                mailParameterDtos.Add(new MailParameterDto { Key = "YedekBilgi" + itemYedek.inx, Value = itemYedek.s.UnvanAdi + " " + itemYedek.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@YedekBilgiUni" + itemYedek.inx))
                            {
                                mailParameterDtos.Add(new MailParameterDto { Key = "YedekBilgiUni" + itemYedek.inx, Value = itemYedek.s.UniversiteAdi });
                            }
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
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            kModel.GonderilenMailEkleris = gonderilenMEkleris;
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }
                    var message = "'" + talep.Kullanicilar.Ad + " " + talep.Kullanicilar.Soyad + "'  kullanıcısının yapmış olduğu salon rezervasyonu bilgisi " + juriler.Count + " adet jüriye mail olarak gönderildi!";
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Salon rezervasyonuna ait jürilere mail gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
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
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var talep = db.SRTalepleris.First(p => p.SRTalepID == srTalepId);

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
                                        new MailSendList {EMail= mb.Kullanicilar.EMail,
                                                           ToOrBcc = true
                                                         }
                                                    },
                        MailSablonTipID = sablonTipId,
                        Attachments = mezuniyetSinavDurumId == MezuniyetSinavDurumEnum.Basarili ? Management.ExportRaporPdf(RaporTipiEnum.MezuniyetTezDuzeltmeVeJuriUyelerineTezTeslimTutanagi, new List<int?> { srTalepId }) : new List<System.Net.Mail.Attachment>()
                    };


                    mezuniyetMailModel.EMails.Add(new MailSendList
                    {
                        EMail = tezDanismani.EMail,
                        ToOrBcc = false
                    });
                    if (!mb.TezEsDanismanEMail.IsNullOrWhiteSpace()) mezuniyetMailModel.EMails.Add(new MailSendList { EMail = mb.TezEsDanismanEMail, ToOrBcc = false });



                    mModel.Add(mezuniyetMailModel);


                    foreach (var item in mModel)
                    {


                        var abdL = mb.Programlar.AnabilimDallari;
                        var prgL = mb.Programlar;
                        item.Sablon = db.MailSablonlaris.First(p => p.IsAktif && p.MailSablonTipID == item.MailSablonTipID && p.EnstituKod == enstitu.EnstituKod);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));


                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Uyarı);
                        }

                        var mailParameterDtos = new List<MailParameterDto>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });

                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "SinavTarihi", Value = talep.Tarih.ToFormatDate() });
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            var sinavYerAdi = talep.SRSalonID.HasValue ? talep.SRSalonlar.SalonAdi : talep.SalonAdi;
                            mailParameterDtos.Add(new MailParameterDto { Key = "SinavYeri", Value = sinavYerAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@UzatmaTarihi"))
                        {
                            if (talep.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma)
                            {
                                var uzatmaTarihi = talep.Tarih.AddDays(mbOtipKriter.MBSinavUzatmaSinavAlmaSuresiMaxGun).ToFormatDate();
                                mailParameterDtos.Add(new MailParameterDto { Key = "UzatmaTarihi", Value = uzatmaTarihi });
                            }
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
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            kModel.GonderilenMailEkleris = gonderilenMEkleris;
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
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
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
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
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var mezuniyetBasvurulariTezDosyasi = db.MezuniyetBasvurulariTezDosyalaris.First(p => p.MezuniyetBasvurulariTezDosyaID == mezuniyetBasvurulariTezDosyaId);
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
                                                            EMail= ogrenci.EMail,
                                                            ToOrBcc = true
                                                         }
                                                    },
                        MailSablonTipID = sablonTipId,
                        Attachments = sablonTipId == MailSablonTipiEnum.MezTezKontrolTezDosyasiBasarili ? Management.ExportRaporPdf(RaporTipiEnum.MezuniyetTezKontrolFormu, new List<int?> { mezuniyetBasvurulariTezDosyaId }) : new List<System.Net.Mail.Attachment>()
                    };

                    mModel.Add(mezuniyetMailModel);


                    if (sablonTipId == MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklendi)
                    {
                        var tezKontrolKul = db.Kullanicilars.FirstOrDefault(f =>
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
                                        ToOrBcc = true
                                    }
                                },
                                MailSablonTipID = MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklendiKontrolYetkilisi
                            });

                        }
                    }

                    var sablonTipIds = mModel.Select(s => s.MailSablonTipID).ToList();
                    var sablonTipleri = db.MailSablonTipleris.Where(p => sablonTipIds.Contains(p.MailSablonTipID))
                        .ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && p.EnstituKod == enstitu.EnstituKod && sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var item in mModel)
                    {


                        item.Sablon = sablonlar.First(p => p.MailSablonTipID == item.MailSablonTipID && p.EnstituKod == enstitu.EnstituKod);
                        item.SablonParametreleri = sablonTipleri.First(f => f.MailSablonTipID == item.Sablon.MailSablonTipID).Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));


                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Uyarı);
                        }

                        var mailParameterDtos = new List<MailParameterDto>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });

                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@SRTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "SRTarihi", Value = srTalep.Tarih.ToShortDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@Aciklama"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "Aciklama", Value = aciklama });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = ogrenci.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = mezuniyetBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = mezuniyetBasvuru.Programlar.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/MezuniyetGelenBasvurular/Index?sMezuniyetBid=" + mezuniyetBasvuru.MezuniyetBasvurulariID + "&sTabId=4", IsLink = true });
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
                                Konu = contentDetailDto.Title + " (" + item.AdSoyad + ")"
                            };
                            if (sablonTipId == MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklendi || UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated) kModel.IslemYapanID = 1;
                            else kModel.IslemYapanID = UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = contentDetailDto.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            kModel.GonderilenMailEkleris = gonderilenMEkleris;
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
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
                if (sablonTipId == MailSablonTipiEnum.MezTezKontrolTezDosyasiBasarili)
                    msg = "Tez şablon dosyası kontrolü başarılı olduğu bilgisi mail olarak gönderilirken bir hata oluştu! \r\nMezuniyetBasvurulariTezDosyaID:" + mezuniyetBasvurulariTezDosyaId;
                else if (sablonTipId == MailSablonTipiEnum.MezTezKontrolTezDosyasiOnaylanmadi)
                    msg = "Tez şablon dosyası kontrolü onaylanmadığı bilgisii mail olarak gönderilirken bir hata oluştu! \r\nMezuniyetBasvurulariTezDosyaID:" + mezuniyetBasvurulariTezDosyaId;
                else if (sablonTipId == MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklendi)
                    msg = "Tez şablon dosyası yüklendi bilgisi mail olarak gönderilirken bir hata oluştu! \r\nMezuniyetBasvurulariTezDosyaID:" + mezuniyetBasvurulariTezDosyaId;

                SistemBilgilendirmeBus.SistemBilgisiKaydet(msg + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
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
                using (var entities = new LisansustuBasvuruSistemiEntities())
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
                    var sablonlar = entities.MailSablonlaris.Where(p => sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {


                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod)
                        };
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        item.EMails.Add(new MailSendList { EMail = basvuru.OgrenciEmail, ToOrBcc = true });

                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Uyarı);
                        }
                        var mailParameterDtos = new List<MailParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@SurecBaslangicTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "SurecBaslangicTarihi", Value = basvuru.BaslangicTarihi.ToFormatDateAndTime() });
                        if (item.SablonParametreleri.Any(a => a == "@SurecBitisTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "SurecBitisTarihi", Value = basvuru.BitisTarihi.ToFormatDateAndTime() });


                        var contentDetailDto = MailManager.CreateMailContentDetailModel(basvuru.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);

                        if (!snded) continue;

                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title + " (" + basvuru.OgrenciAdSoyad + ")",
                            IslemYapanID = 1,
                            IslemYapanIP = "::",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList(),
                            GonderilenMailEkleris = gonderilenMEkleris
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
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata, null, "::");
            }
        }
        public static async Task SendMailMezuniyetEykTarihineGoreSrAlinmaliOgrenciyeDanismana()
        {

            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
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
                               DbFunctions.DiffDays(nowDate, DbFunctions.AddDays(mez.EYKTarihi, ogrenimTipKriter.MBTezTeslimSuresiGun)) >= 0 &&
                               DbFunctions.DiffDays(nowDate, DbFunctions.AddDays(mez.EYKTarihi, ogrenimTipKriter.MBTezTeslimSuresiGun)) <= otoMail.Sure

                         select new
                         {
                             enst.EnstituKod,
                             enst.EnstituAd,
                             enst.WebAdresi,
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
                             ogrenimTipKriter.MBSinavUzatmaSinavAlmaSuresiMaxGun,
                             ogrenimTipKriter.MBTezTeslimSuresiGun,
                             otoMail.MailSablonTipID,
                             otoMail.MezuniyetSureciOtoMailID,
                             SonTar = DbFunctions.AddDays(mez.EYKTarihi, ogrenimTipKriter.MBTezTeslimSuresiGun),
                             otoMail.Sure

                         }).ToList();
                    if (!mezuniyetBasvurulari.Any()) return;

                    var sablonTipIds = mezuniyetBasvurulari.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => sablonTipIds.Contains(p.MailSablonTipID))
                        .ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {


                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod),
                            AdSoyad = basvuru.OgrenciAdSoyad
                        };
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        item.EMails.Add(new MailSendList { EMail = basvuru.DanismanEmail, ToOrBcc = true });
                        item.EMails.Add(new MailSendList { EMail = basvuru.TezEsDanismanEMail, ToOrBcc = true });
                        item.EMails.Add(new MailSendList { EMail = basvuru.OgrenciEmail, ToOrBcc = true });

                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Uyarı);
                        }

                        var mailParameterDtos = new List<MailParameterDto>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });

                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = basvuru.EYKTarihi.ToFormatDate() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(basvuru.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;

                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title + " (" + item.AdSoyad + ")",
                            IslemYapanID = 1,
                            IslemYapanIP = "::",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList(),
                            GonderilenMailEkleris = gonderilenMEkleris
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
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata, null, "::");
            }
        }
        public static async Task SendMailMezuniyetEykTarihineGoreSrAlinmadiEnstituye()
        {
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
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
                               DbFunctions.DiffDays(DbFunctions.AddDays(mez.EYKTarihi, ogrenimTipKriter.MBTezTeslimSuresiGun), nowDate) >= otoMail.Sure

                         select new
                         {
                             enst.EnstituKod,
                             enst.EnstituAd,
                             enst.WebAdresi,
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
                    var sablonlar = entities.MailSablonlaris.Where(p => sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {


                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod),
                            AdSoyad = basvuru.OgrenciAdSoyad
                        };
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        else continue;

                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Uyarı);
                        }
                        var mailParameterDtos = new List<MailParameterDto>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = basvuru.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EYKTarihi", Value = basvuru.EYKTarihi.ToFormatDate() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(basvuru.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;

                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title + " (" + item.AdSoyad + ")",
                            IslemYapanID = 1,
                            IslemYapanIP = "::",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList(),
                            GonderilenMailEkleris = gonderilenMEkleris
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
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata, null, "::");
            }
        }
        public static async Task SendMailMezuniyetDanismanDegerlendirmeHatirlatmaDanismana()
        {
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
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
                               drOgrenimTipKods.Contains(mez.OgrenimTipKod) == (otoMail.MailSablonTipID == MailSablonTipiEnum.MezSinavDegerlendirmeHatirlantmaDanismanDr) &&
                               DbFunctions.DiffHours(DbFunctions.CreateDateTime(sonRezervasyon.Tarih.Year, sonRezervasyon.Tarih.Month, sonRezervasyon.Tarih.Day, sonRezervasyon.BasSaat.Hours, sonRezervasyon.BasSaat.Minutes, 0), nowDate) >= otoMail.Sure


                         select new
                         {
                             enst.EnstituKod,
                             enst.EnstituAd,
                             enst.WebAdresi,
                             mez.MezuniyetBasvurulariID,
                             mez.OgrenimTipKod,
                             mez.OgrenciNo,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             OgrenciEmail = ogrenci.EMail,
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
                    var sablonlar = entities.MailSablonlaris.Where(p => sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {


                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod)
                        };
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        item.EMails.Add(new MailSendList { EMail = basvuru.DanismanEmail, ToOrBcc = true });

                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Uyarı);
                        }

                        var mailParameterDtos = new List<MailParameterDto>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanAdSoyad", Value = basvuru.DanismanAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUnvanAdi", Value = basvuru.DanismanUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "SinavTarihi", Value = basvuru.SinavTarihi.ToFormatDate() });
                        if (item.SablonParametreleri.Any(a => a == "@SinavSaati"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "SinavSaati", Value = basvuru.SinavSaati.ToFormatTime() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(basvuru.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;

                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title + " (" + basvuru.DanismanAdSoyad + ")",
                            IslemYapanID = 1,
                            IslemYapanIP = "::",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList(),
                            GonderilenMailEkleris = gonderilenMEkleris
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
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata, null, "::");
            }
        }
        public static async Task SendMailMezuniyetSinavSonucuGirilmediOgrenciyeDanismana()
        {
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
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
                             mez.MezuniyetBasvurulariID,
                             mez.OgrenciNo,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             OgrenciEmail = ogrenci.EMail,
                             program.ProgramAdi,
                             anabilimDali.AnabilimDaliAdi,
                             DanismanEmail = danisman.EMail,
                             SinavTarihi = sonRezervasyon.Tarih,
                             SinavSaati = sonRezervasyon.BasSaat,
                             otoMail.MailSablonTipID,
                             otoMail.MezuniyetSureciOtoMailID
                         }).ToList();
                    if (!mezuniyetBasvurulari.Any()) return;
                    var sablonTipIds = mezuniyetBasvurulari.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {


                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod)
                        };
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));

                        item.EMails.Add(new MailSendList { EMail = basvuru.OgrenciEmail, ToOrBcc = true });
                        item.EMails.Add(new MailSendList { EMail = basvuru.DanismanEmail, ToOrBcc = true });

                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Uyarı);
                        }

                        var mailParameterDtos = new List<MailParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = basvuru.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = basvuru.AnabilimDaliAdi });

                        if (item.SablonParametreleri.Any(a => a == "@SRTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "SRTarihi", Value = basvuru.SinavTarihi.ToFormatDate() + " " + basvuru.SinavSaati.ToFormatTime() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(basvuru.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;

                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title + " (" + basvuru.OgrenciAdSoyad + ")",
                            IslemYapanID = 1,
                            IslemYapanIP = "::",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList(),
                            GonderilenMailEkleris = gonderilenMEkleris
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
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata, null, "::");
            }
        }
        public static async Task SendMailMezuniyetTezKontrolTezDosyasiYuklenmeliOgrenciye()
        {
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
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
                    var sablonlar = entities.MailSablonlaris.Where(p => sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {


                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod)
                        };
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));

                        item.EMails.Add(new MailSendList { EMail = basvuru.OgrenciEmail, ToOrBcc = true });

                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Uyarı);
                        }

                        var mailParameterDtos = new List<MailParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@SRTarihi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "SRTarihi", Value = basvuru.SinavTarihi.ToFormatDate() + " " + basvuru.SinavSaati.ToFormatTime() });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = basvuru.SistemErisimAdresi + "/mezuniyet/Index?RowID=" + basvuru.RowID, IsLink = true });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(basvuru.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;

                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title + " (" + basvuru.OgrenciAdSoyad + ")",
                            IslemYapanID = 1,
                            IslemYapanIP = "::",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList(),
                            GonderilenMailEkleris = gonderilenMEkleris
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
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata, null, "::");
            }
        }
        public static async Task SendMailCiltliTezTeslimYapilmaliOgrenciye()
        {
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
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
                               DbFunctions.DiffDays(nowDate, (mez.TezTeslimSonTarih ?? DbFunctions.AddDays(sonRezervasyon.Tarih, ogrenimTipKriter.MBTezTeslimSuresiGun))) >= 0 &&
                               DbFunctions.DiffDays(nowDate, (mez.TezTeslimSonTarih ?? DbFunctions.AddDays(sonRezervasyon.Tarih, ogrenimTipKriter.MBTezTeslimSuresiGun))) <= otoMail.Sure


                         select new
                         {
                             enst.EnstituKod,
                             enst.EnstituAd,
                             enst.WebAdresi,
                             enst.SistemErisimAdresi,
                             mez.MezuniyetBasvurulariID,
                             mez.RowID,
                             OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             OgrenciEmail = ogrenci.EMail,
                             DanismanEmail = danisman.EMail,
                             SinavTarihi = sonRezervasyon.Tarih,
                             SinavSaati = sonRezervasyon.BasSaat,
                             otoMail.MailSablonTipID,
                             otoMail.MezuniyetSureciOtoMailID,
                             TezTeslimSonTarih = (mez.TezTeslimSonTarih ?? DbFunctions.AddDays(sonRezervasyon.Tarih, ogrenimTipKriter.MBTezTeslimSuresiGun))
                         }).ToList();
                    if (!mezuniyetBasvurulari.Any()) return;
                    var sablonTipIds = mezuniyetBasvurulari.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {


                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod)
                        };
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));

                        item.EMails.Add(new MailSendList { EMail = basvuru.OgrenciEmail, ToOrBcc = true });
                        item.EMails.Add(new MailSendList { EMail = basvuru.DanismanEmail, ToOrBcc = true });

                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Uyarı);
                        }
                        var mailParameterDtos = new List<MailParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@CiltTeslimTarih"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "CiltTeslimTarih", Value = basvuru.TezTeslimSonTarih.ToFormatDate() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(basvuru.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;

                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title + " (" + basvuru.OgrenciAdSoyad + ")",
                            IslemYapanID = 1,
                            IslemYapanIP = "::",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList(),
                            GonderilenMailEkleris = gonderilenMEkleris
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
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata, null, "::");
            }
        }
        public static async Task SendMailCiltliTezTeslimYapilmadiEnstituye()
        {
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
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
                               DbFunctions.DiffDays((mez.TezTeslimSonTarih ?? DbFunctions.AddDays(sonRezervasyon.Tarih, ogrenimTipKriter.MBTezTeslimSuresiGun)), nowDate) >= otoMail.Sure


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
                             TezTeslimSonTarih = (mez.TezTeslimSonTarih ?? DbFunctions.AddDays(sonRezervasyon.Tarih, ogrenimTipKriter.MBTezTeslimSuresiGun))
                         }).ToList();
                    if (!mezuniyetBasvurulari.Any()) return;
                    var sablonTipIds = mezuniyetBasvurulari.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = entities.MailSablonlaris.Where(p => sablonTipIds.Contains(p.MailSablonTipID)).ToList();
                    foreach (var basvuru in mezuniyetBasvurulari)
                    {


                        var item = new SablonMailModel
                        {
                            Sablon = sablonlar.FirstOrDefault(p => p.EnstituKod == basvuru.EnstituKod)
                        };
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        else continue;

                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, ObjectExtensions.GetCurrentMethodPath(), LogTipiEnum.Uyarı);
                        }
                        var mailParameterDtos = new List<MailParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = basvuru.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = basvuru.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = basvuru.OgrenciAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = basvuru.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimDaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimDaliAdi", Value = basvuru.AnabilimDaliAdi });

                        if (item.SablonParametreleri.Any(a => a == "@CiltTeslimTarih"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "CiltTeslimTarih", Value = basvuru.TezTeslimSonTarih.ToFormatDate() });

                        var contentDetailDto = MailManager.CreateMailContentDetailModel(basvuru.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(basvuru.EnstituKod, contentDetailDto.Title, contentDetailDto.HtmlContent, item.EMails, item.Attachments);

                        if (!snded) continue;

                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = basvuru.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = contentDetailDto.Title + " (" + basvuru.OgrenciAdSoyad + ")",
                            IslemYapanID = 1,
                            IslemYapanIP = "::",
                            Aciklama = item.Sablon.Sablon ?? "",
                            AciklamaHtml = contentDetailDto.HtmlContent ?? "",
                            Gonderildi = true,
                            GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList(),
                            GonderilenMailEkleris = gonderilenMEkleris
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
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Mezuniyet bilgilendirme maili gönderilirken bir hata oluştu! \r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata, null, "::");
            }
        }
    }
}