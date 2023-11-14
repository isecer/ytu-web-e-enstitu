using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using BiskaUtil;
using System.Threading;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Models
{
    public class ApplicationClock
    {
        void Tick(object _)
        {
            lock (_tickLock)
            {
                try
                {
                    #region ZamanlananMailleriKontrolEt
                    //MessageBroker.Publisher.Publish(new Tick());
                    using (var db = new LisansustuBasvuruSistemiEntities())
                    {
                        var tarih = DateTime.Now.Date;
                        var enstituler = EnstituBus.GetEnstituler();
                        foreach (var itemE in enstituler)
                        {
                            var qAktifMzsId = MezuniyetBus.GetMezuniyetAktifSurecId(itemE.EnstituKod);
                            if (qAktifMzsId.HasValue)
                            {
                                var qAktifMs = db.MezuniyetSurecis.FirstOrDefault(p => p.MezuniyetSurecID == qAktifMzsId.Value);
                                if (qAktifMs != null)
                                {
                                    var bsOtoZamanBilgi = qAktifMs.MezuniyetSurecOtoMails.Where(p => p.Gonderildi == false && !p.MailSablonTipID.HasValue).ToList();
                                    var kalanGun = Convert.ToInt32((qAktifMs.BitisTarihi - tarih).TotalDays);
                                    foreach (var itmZmn in bsOtoZamanBilgi)
                                    {
                                        if (itmZmn.ZamanTipID == ZamanTipi.Gun && itmZmn.Zaman == kalanGun)
                                        {
                                            SendMailMsTaslak(qAktifMs, kalanGun, itmZmn.ZamanTipID, itmZmn.Zaman);
                                        }

                                    }
                                }
                            }

                            #region MezuniyetTezTeslimiBilgilendirme
                            var qMezuniyetOtoMails = db.MezuniyetSurecOtoMails.Where(p => p.MezuniyetSureci.EnstituKod == itemE.EnstituKod && p.Gonderildi == false && p.MailSablonTipID.HasValue).OrderByDescending(o => o.MezuniyetSurecID).ThenBy(o => o.MezuniyetSurecOtoMailID).ToList();
                            var mbControlCount = new Dictionary<string, int>();
                            if (qMezuniyetOtoMails.Any())
                            {
                                var mailSablonlari = db.MailSablonlaris.Where(p => p.EnstituKod == itemE.EnstituKod && p.IsAktif).ToList();
                                var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(itemE.EnstituKod);

                                foreach (var item in qMezuniyetOtoMails)
                                {
                                    var sblTr = mailSablonlari.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                                    var sblKey = sblTr.SablonAdi + "_" + sblTr.MailSablonTipID;
                                    if (mbControlCount.All(a => a.Key != sblKey))
                                    {
                                        mbControlCount.Add(sblKey, 0);
                                    }
                                    var kayitliGonderilenMailAdresleri = new List<string>();
                                    var gonderilecekMailAdresleri = new List<string>();

                                    var kayitliGonderilecekMbiDs = new List<int>();
                                    var gonderilecekMbiDs = new List<int>();
                                    if (item.Gonderilenler.IsNullOrWhiteSpace() == false)
                                    {
                                        kayitliGonderilenMailAdresleri.AddRange(item.Gonderilenler.Split(',').ToList().Select(s => s.Trim()).ToList());
                                    }
                                    if (item.GonderilenMBID.IsNullOrWhiteSpace() == false)
                                    {
                                        kayitliGonderilecekMbiDs.AddRange(item.GonderilenMBID.Split(',').ToList().Select(s => s.Trim().ToInt().Value).ToList());
                                    }
                                    var qbasvurular = item.MezuniyetSureci.MezuniyetBasvurularis.Where(p => p.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi && !p.IsMezunOldu.HasValue);
                                    if (item.MailSablonTipID == MailSablonTipiEnum.MezEykTarihineGoreSrAlinmali || item.MailSablonTipID == MailSablonTipiEnum.MezEykTarihineGoreSrAlinmadi)
                                    {
                                        //Eyk Tarihi Girildikten Sonra Sr Talebi Yapılmayanları Getir
                                        qbasvurular = qbasvurular.Where(p => p.EYKTarihi.HasValue && (p.SRTalepleris.Any(a => a.SRDurumID == SrTalepDurumEnum.Onaylandı) == false || p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma));
                                    }
                                    if (item.MailSablonTipID == MailSablonTipiEnum.MezSinavDegerlendirmeHatirlantmaDanismanDr || item.MailSablonTipID == MailSablonTipiEnum.MezSinavDegerlendirmeHatirlantmaDanismanYl)
                                    {
                                        //Sınav yeri onaylandıktan sonra değerlendirme girmeyen danışmanlar
                                        qbasvurular = qbasvurular.Where(p => p.EYKTarihi.HasValue && p.SRTalepleris.Any(a => a.SRDurumID == SrTalepDurumEnum.Onaylandı && (!a.MezuniyetSinavDurumID.HasValue || a.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.SonucGirilmedi) && a.SRTaleplerJuris.Any(a2 => a2.JuriTipAdi == "TezDanismani" && (!a2.MezuniyetSinavDurumID.HasValue || a2.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.SonucGirilmedi))));
                                    }
                                    else if (item.MailSablonTipID == MailSablonTipiEnum.MezTezSinavSonucuSistemeGirilmedi)
                                    {
                                        //SR Talebi Yapıldıktan Sonra Sınav Sonucu Girilmeyenleri Getir
                                        qbasvurular = qbasvurular.Where(p => p.SRTalepleris.Any(a => a.SRDurumID == SrTalepDurumEnum.Onaylandı && a.JuriSonucMezuniyetSinavDurumID.HasValue) && (p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.SonucGirilmedi || !p.MezuniyetSinavDurumID.HasValue));
                                    }
                                    else if (item.MailSablonTipID == MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklenmeli)
                                    {
                                        //Sınav sonucu başarılı olup tez dosyasını teslim etmeyenler getir.
                                        qbasvurular = qbasvurular.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili && !p.MezuniyetBasvurulariTezTeslimFormlaris.Any());
                                    }
                                    else if (item.MailSablonTipID == MailSablonTipiEnum.MezCiltliTezTeslimYapilmali || item.MailSablonTipID == MailSablonTipiEnum.MezCiltliTezTeslimYapilmadi)
                                    {
                                        //Sınav sonucu başarılı olup bezcilt formunu oluşturmayanlar ya da oluşturup teslim etmeyenleri getir.
                                        qbasvurular = qbasvurular.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili &&
                                                                             (!p.MezuniyetBasvurulariTezTeslimFormlaris.Any()
                                                                              ||
                                                                             (p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili && p.MezuniyetBasvurulariTezTeslimFormlaris.Any() && !p.IsMezunOldu.HasValue)
                                                                             )
                                                                        );
                                    }
                                    var basvurular = qbasvurular.Where(p => kayitliGonderilecekMbiDs.Contains(p.MezuniyetBasvurulariID) == false).ToList();
                                    foreach (var itemB in basvurular)
                                    {
                                        var otKriterBilgi = itemB.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == itemB.OgrenimTipKod);
                                        int? kalanGun = null;
                                        var eMailList = new List<MailSendList>();
                                        DateTime? sonTarih = null;
                                        DateTime? sinavTarihi = null;
                                        TimeSpan? sinavSaati = null;
                                        if (item.MailSablonTipID == MailSablonTipiEnum.MezEykTarihineGoreSrAlinmali || item.MailSablonTipID == MailSablonTipiEnum.MezEykTarihineGoreSrAlinmadi)
                                        {
                                            if (item.MailSablonTipID == MailSablonTipiEnum.MezEykTarihineGoreSrAlinmali) eMailList.Add(new MailSendList { EMail = itemB.Kullanicilar.EMail, ToOrBcc = true });

                                            if (itemB.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma)
                                            {
                                                var sonSr = itemB.SRTalepleris.OrderByDescending(o => o.SRTalepID).First();
                                                var srAlimSonTarih = sonTarih = sonSr.Tarih.AddDays(otKriterBilgi.MBSinavUzatmaSuresiGun).Date;
                                                kalanGun = Convert.ToInt32((srAlimSonTarih.Value - tarih).TotalDays);
                                                var juri = sonSr.SRTaleplerJuris.OrderBy(p => p.SRTalepJuriID).First();
                                                eMailList.Add(new MailSendList { EMail = juri.Email.Trim(), ToOrBcc = true });
                                                if (!itemB.TezEsDanismanEMail.IsNullOrWhiteSpace()) eMailList.Add(new MailSendList { EMail = itemB.TezEsDanismanEMail.Trim(), ToOrBcc = true });
                                            }
                                            else
                                            {
                                                var srAlimSonTarih = sonTarih = itemB.EYKTarihi.Value.AddDays(otKriterBilgi.MBTezTeslimSuresiGun);
                                                kalanGun = Convert.ToInt32((srAlimSonTarih.Value - tarih).TotalDays);
                                            }
                                        }
                                        else if (item.MailSablonTipID == MailSablonTipiEnum.MezSinavDegerlendirmeHatirlantmaDanismanDr || item.MailSablonTipID == MailSablonTipiEnum.MezSinavDegerlendirmeHatirlantmaDanismanYl)
                                        {
                                            var sonSr = itemB.SRTalepleris.Where(a => a.SRDurumID == SrTalepDurumEnum.Onaylandı && a.SRTaleplerJuris.Any(a2 => a2.JuriTipAdi == "TezDanismani" && !(a2.MezuniyetSinavDurumID > MezuniyetSinavDurumEnum.SonucGirilmedi))).OrderByDescending(o => o.SRTalepID).First();
                                            var danisman = sonSr.SRTaleplerJuris.First(p => p.JuriTipAdi == "TezDanismani");
                                            sinavTarihi = sonSr.Tarih;
                                            sinavSaati = sonSr.BasSaat;

                                            var toplantiTarihi = sinavTarihi.Value.Add(sinavSaati.Value);
                                            sonTarih = toplantiTarihi;
                                            kalanGun = Convert.ToInt32((toplantiTarihi - DateTime.Now).TotalHours);
                                            eMailList.Add(new MailSendList { EMail = danisman.Email, ToOrBcc = true });

                                        }
                                        else if (item.MailSablonTipID == MailSablonTipiEnum.MezTezSinavSonucuSistemeGirilmedi)
                                        {
                                            var sonSr = itemB.SRTalepleris.OrderByDescending(o => o.SRTalepID).First();
                                            var sinavSonucGirisSonTarih = sonTarih = sonSr.Tarih;
                                            kalanGun = Convert.ToInt32((sinavSonucGirisSonTarih.Value - tarih).TotalDays);
                                            eMailList.Add(new MailSendList { EMail = itemB.Kullanicilar.EMail, ToOrBcc = true });
                                            var juri = sonSr.SRTaleplerJuris.OrderBy(p => p.SRTalepJuriID).First();
                                            eMailList.Add(new MailSendList { EMail = juri.Email.Trim(), ToOrBcc = true });
                                            if (!itemB.TezEsDanismanEMail.IsNullOrWhiteSpace()) eMailList.Add(new MailSendList { EMail = itemB.TezEsDanismanEMail.Trim(), ToOrBcc = true });
                                        }
                                        else if (item.MailSablonTipID == MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklenmeli)
                                        {
                                            var sonSr = itemB.SRTalepleris.OrderByDescending(o => o.SRTalepID).First();
                                            var sinavSonucGirisSonTarih = sonTarih = sonSr.Tarih;
                                            kalanGun = Convert.ToInt32((sinavSonucGirisSonTarih.Value - tarih).TotalDays);
                                            eMailList.Add(new MailSendList { EMail = itemB.Kullanicilar.EMail, ToOrBcc = true });
                                        }
                                        else if (item.MailSablonTipID == MailSablonTipiEnum.MezCiltliTezTeslimYapilmali || item.MailSablonTipID == MailSablonTipiEnum.MezCiltliTezTeslimYapilmadi)
                                        {
                                            var sonSr = itemB.SRTalepleris.OrderByDescending(o => o.SRTalepID).First();
                                            var tezTeslimSonTarihi = sonTarih = itemB.TezTeslimSonTarih ?? sonSr.Tarih.AddDays(otKriterBilgi.MBTezTeslimSuresiGun).Date;

                                            kalanGun = Convert.ToInt32((tezTeslimSonTarihi.Value - tarih).TotalDays);
                                            if (item.MailSablonTipID == MailSablonTipiEnum.MezCiltliTezTeslimYapilmali)
                                            {
                                                eMailList.Add(new MailSendList { EMail = itemB.Kullanicilar.EMail, ToOrBcc = true });
                                                var srTalep = itemB.SRTalepleris.First(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili);
                                                var juri = srTalep.SRTaleplerJuris.OrderBy(p => p.SRTalepJuriID).First();
                                                eMailList.Add(new MailSendList { EMail = juri.Email.Trim(), ToOrBcc = true });
                                                if (!itemB.TezEsDanismanEMail.IsNullOrWhiteSpace()) eMailList.Add(new MailSendList { EMail = itemB.TezEsDanismanEMail.Trim(), ToOrBcc = true });
                                            }
                                            else
                                            {
                                            }
                                        }
                                        MailSablonlari sablon = null;
                                        bool gunIsSuccess = kalanGun.HasValue;
                                        //Covid-19 salgını nedeni ile 6. ayın 1 ine kadar olan tarihlerdeki işlemler mail atılmayacak
                                        if (!sonTarih.HasValue || sonTarih.Value < new DateTime(2020, 6, 1))
                                        {
                                            gunIsSuccess = false;
                                        }
                                        if (gunIsSuccess)
                                        {
                                            if (item.Zaman >= 0)
                                            {
                                                gunIsSuccess = kalanGun <= item.Zaman && kalanGun >= 0;
                                            }
                                            else
                                            {
                                                gunIsSuccess = kalanGun <= item.Zaman;
                                            }
                                            if (gunIsSuccess)
                                            {
                                                sablon = mailSablonlari.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID && p.EnstituKod == itemE.EnstituKod);
                                                if (sablon.GonderilecekEkEpostalar.IsNullOrWhiteSpace() == false)
                                                    eMailList.AddRange(sablon.GonderilecekEkEpostalar.Split(',').ToList().Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                                            }
                                        }
                                        //mailleri gönder
                                        if (item.ZamanTipID == ZamanTipi.Gun && gunIsSuccess && eMailList.Count > 0)
                                        {
                                            var enstituL = itemE;
                                            var paramereDegerleri = new List<MailReplaceParameterDto>();
                                            var parametreler = sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                                            var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                                            var attachments = new List<System.Net.Mail.Attachment>();
                                            foreach (var itemSe in sablon.MailSablonlariEkleris)
                                            {
                                                var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                                                if (System.IO.File.Exists(ekTamYol))
                                                {
                                                    var fExtension = System.IO.Path.GetExtension(ekTamYol);
                                                    attachments.Add(new System.Net.Mail.Attachment(new System.IO.MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                                        itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                                    gonderilenMEkleris.Add(new GonderilenMailEkleri
                                                    {
                                                        EkAdi = itemSe.EkAdi,
                                                        EkDosyaYolu = itemSe.EkDosyaYolu,
                                                    });
                                                }
                                                else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "ApplicationClock", LogTipiEnum.Uyarı);
                                            }
                                            if (parametreler.Any(a => a == "@EnstituAdi"))
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                                            if (parametreler.Any(a => a == "@WebAdresi"))
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = itemE.WebAdresi, IsLink = true });
                                            if (parametreler.Any(a => a == "@AdSoyad"))
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = itemB.Ad + " " + itemB.Soyad });
                                            if (parametreler.Any(a => a == "@OgrenciAdSoyad"))
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = itemB.Ad + " " + itemB.Soyad });
                                            if (parametreler.Any(a => a == "@DanismanAdSoyad"))
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = itemB.TezDanismanAdi });
                                            if (parametreler.Any(a => a == "@DanismanUnvanAdi"))
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = itemB.TezDanismanUnvani });
                                            if (parametreler.Any(a => a == "@OgrenciNo"))
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = itemB.OgrenciNo });
                                            if (parametreler.Any(a => a == "@EYKTarihi"))
                                            {
                                                var eykTarih = itemB.EYKTarihi.HasValue ? itemB.EYKTarihi.Value.ToFormatDate() : "";
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EYKTarihi", Value = eykTarih });
                                            }
                                            if (parametreler.Any(a => a == "@SRTarihi"))
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SRTarihi", Value = sonTarih.ToFormatDate() });
                                            if (parametreler.Any(a => a == "@SinavTarihi") && sinavTarihi.HasValue)
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SinavTarihi", Value = sinavTarihi.Value.ToLongDateString() });
                                            if (parametreler.Any(a => a == "@SinavSaati") && sinavSaati.HasValue)
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SinavSaati", Value = $"{sinavSaati:hh\\:mm}" });
                                            if (parametreler.Any(a => a == "@AnabilimDaliAdi"))
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimDaliAdi", Value = itemB.Programlar.AnabilimDallari.AnabilimDaliAdi });
                                            if (parametreler.Any(a => a == "@ProgramAdi"))
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = itemB.Programlar.ProgramAdi });
                                            if (parametreler.Any(a => a == "@CiltTeslimTarih"))//düzenlenecek
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "CiltTeslimTarih", Value = sonTarih.HasValue ? sonTarih.ToFormatDate() : "" });
                                            if (parametreler.Any(a => a == "@DonemAdi"))
                                            {
                                                var donemAdi = itemB.MezuniyetSureci.BaslangicYil + " " + itemB.MezuniyetSureci.BitisYil + " " + itemB.MezuniyetSureci.Donemler.DonemAdi;
                                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DonemAdi", Value = donemAdi });
                                            }
                                            if (parametreler.Any(a => a == "@Link"))
                                            {
                                                if (item.MailSablonTipID == MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklenmeli)
                                                {
                                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = itemE.SistemErisimAdresi + "/mezuniyet/Index?RowID=" + itemB.RowID, IsLink = true });
                                                }
                                            }
                                            // EMailList = new List<MailSendList> { new MailSendList { EMail = "irfansecer@gmail.com", ToOrBcc = true } };
                                            var mCOntent = SystemMails.GetSystemMailContent(enstituL.EnstituAd, sablon.SablonHtml, sablon.SablonAdi, paramereDegerleri);

                                            var snded = MailManager.SendMail(itemE.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, eMailList, attachments);
                                            if (snded)
                                            {
                                                gonderilecekMbiDs.Add(itemB.MezuniyetBasvurulariID);
                                                gonderilecekMailAdresleri.AddRange(eMailList.Select(s => s.EMail));
                                                var kModel = new GonderilenMailler
                                                {
                                                    Tarih = DateTime.Now,
                                                    EnstituKod = mailBilgi.EnstituKod,
                                                    MesajID = null,
                                                    IslemTarihi = DateTime.Now,
                                                    Konu = mCOntent.Title + " (" + itemB.Ad + " " + itemB.Soyad + ")",
                                                    IslemYapanID = 1,
                                                    IslemYapanIP = UserIdentity.Ip,
                                                    Aciklama = sablon.Sablon ?? "",
                                                    AciklamaHtml = mCOntent.HtmlContent ?? "",
                                                    Gonderildi = true,
                                                    GonderilenMailKullanicilars = eMailList.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList(),
                                                    GonderilenMailEkleris = gonderilenMEkleris
                                                };
                                                db.GonderilenMaillers.Add(kModel);
                                                db.SaveChanges();
                                            }
                                        }
                                    }

                                    if (gonderilecekMailAdresleri.Count > 0 || gonderilecekMbiDs.Count > 0)
                                    {
                                        item.GonderilenCount = kayitliGonderilecekMbiDs.Count + gonderilecekMbiDs.Count;
                                        item.Gonderilenler = (item.Gonderilenler.IsNullOrWhiteSpace() ? "" : item.Gonderilenler + ",") + string.Join(",", gonderilecekMailAdresleri);
                                        item.GonderilenMBID = (item.GonderilenMBID.IsNullOrWhiteSpace() ? "" : item.GonderilenMBID + ",") + string.Join(",", gonderilecekMbiDs);
                                        db.SaveChanges();
                                        mbControlCount[sblKey] = mbControlCount[sblKey] + gonderilecekMbiDs.Count;
                                    }

                                }
                                foreach (var itemCnt in mbControlCount)
                                {
                                }
                            }

                            #endregion
                            //db.SistemBilgilendirmes.Add(new SistemBilgilendirme()
                            //{
                            //    Message = MzamanlayiciMsg,
                            //    BilgiTipi = LogType.Uyarı,
                            //    IslemYapanID = 1,
                            //    IslemYapanIP = "::",
                            //    IslemTarihi = DateTime.Now,
                            //    StackTrace = "ApplicationClock"
                            //});
                            //db.SaveChanges();
                        }



                    }

                    //using (var db = new LisansustuBasvuruSistemiEntities())
                    //{
                    //    var kriterTarihi = DateTime.Now.AddDays(-10);
                    //    var mesajs = db.Mesajlars.Where(p => !p.IsAktif && !p.UstMesajID.HasValue && p.Tarih < kriterTarihi).ToList();
                    //    foreach (var mesaj in mesajs)
                    //    {
                    //        mesaj.IsAktif = true;
                    //    }

                    //    db.SaveChanges();
                    //}
                    #endregion



                }
                catch (Exception e)
                {
                    SistemBilgilendirmeBus.SistemBilgisiKaydet("Toplu mail gönderilirken bir hata oluştu! hata:" + e.ToExceptionMessage(), e.ToExceptionStackTrace(), LogTipiEnum.Hata, 1, "::");
                }
            }
        }
        readonly Object _tickLock = new Object();
        Timer _ticker = null;
        public ApplicationClock Start()
        {

            _ticker = new Timer(Tick, null, 0, 1 * 60 * 60 * 1000);
            return this;

            // ticker = new Timer(tick, null, 0, 1 * 5 * 1000);

        }
        public int SendMailMsTaslak(MezuniyetSureci bsurec, int kalanGun, int zamanTipId, int zaman)
        {

            int taslakCount = 0;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(bsurec.EnstituKod);
                var sistemErisimAdresi = mailBilgi.SistemErisimAdresi;
                var wurlAddr = sistemErisimAdresi.Split('/').ToList();
                if (sistemErisimAdresi.Contains("//"))
                    sistemErisimAdresi = wurlAddr[0] + "//" + wurlAddr.Skip(2).Take(1).First();
                else
                    sistemErisimAdresi = "http://" + wurlAddr.First();
                var qTaslaklar = db.MezuniyetBasvurularis.Where(p => p.MezuniyetSurecID == bsurec.MezuniyetSurecID && p.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Taslak).Select(s => new { s.Kullanicilar.EMail, s.Kullanicilar.KullaniciTipleri.Yerli, s.KullaniciID }).Distinct().ToList();
                var dct = new Dictionary<string, List<CmbIntDto>> {
                {
                    "",
                    qTaslaklar.Select(s => new CmbIntDto { Value = s.KullaniciID, Caption = s.EMail }).ToList()

                } };
                var bdurumAds = db.BasvuruDurumlaris.Where(p => p.BasvuruDurumID == MezuniyetYayinKontrolDurumuEnum.Taslak).ToList();
                taslakCount = qTaslaklar.Count();
                var zmnB = db.MezuniyetSurecOtoMails.FirstOrDefault(p => p.MezuniyetSurecID == bsurec.MezuniyetSurecID && p.MailSablonTipID.HasValue == false && p.ZamanTipID == zamanTipId && p.Zaman == zaman);
                zmnB.Gonderildi = true;
                zmnB.GonderilenCount = taslakCount;
                var gonderilecekTumMailler = dct.SelectMany(s => s.Value.Select(s2 => s2.Caption)).Distinct().ToList();
                zmnB.Gonderilenler = zmnB.Gonderilenler.IsNullOrWhiteSpace() ? string.Join(",", gonderilecekTumMailler) : zmnB.Gonderilenler + string.Join(",", gonderilecekTumMailler);
                if (taslakCount > 0)
                    foreach (var itemMailD in dct)
                    {


                        string dilKodu = itemMailD.Key;
                        string basvuruDurumAdi = bdurumAds.First().BasvuruDurumAdi;
                        var mmmC = new MailMainContentDto();
                        var enstitu = db.Enstitulers.First(p => p.EnstituKod == bsurec.EnstituKod);
                        mmmC.EnstituAdi = enstitu.EnstituAd;
                        mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                        mmmC.LogoPath = sistemErisimAdresi + "/Content/assets/images/ytu_logo_tr.png"; ;


                        var htmlCbilgi = new MailTableContentDto
                        {
                            AciklamaBasligi = "Mezuniyet Başvuru sürecinde yapmış olduğunuz başvurunuz taslak halindendir. Başvuru sürecinin bitimine " + kalanGun + "  gün kalmıştır. Eğer başvurunuzu " + kalanGun + " içerisinde onaylamazsanız başvurunuz geçersiz sayılacaktır.",
                            GrupBasligi = "BAŞVURU BİLGİSİ"
                        };
                        htmlCbilgi.Detaylar.Add(new MailTableRowDto { Baslik = "Başvuru Durumu", Aciklama = basvuruDurumAdi });
                        htmlCbilgi.Detaylar.Add(new MailTableRowDto { Baslik = "Başvuru Süreci", Aciklama = bsurec.BaslangicTarihi.ToFormatDateAndTime() + " - " + bsurec.BitisTarihi.ToFormatDateAndTime() });
                        htmlCbilgi.Detaylar.Add(new MailTableRowDto { Baslik = "Başvuruyu Onaylamanız İçin Kalan Süre", Aciklama = (kalanGun == 1 ? "Son" : kalanGun.ToString()) + " Gün" });
                        htmlCbilgi.Detaylar.Add(new MailTableRowDto { Baslik = "Sisteme Erişim Adresi", Aciklama = enstitu.SistemErisimAdresi });


                        var tableContent = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", htmlCbilgi);
                        mmmC.Content = tableContent;
                        string htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);
                        var eMailList = itemMailD.Value.Select(s => s.Caption).Distinct().Select(s => new MailSendList { EMail = s, ToOrBcc = false }).ToList();
                        var emailSend = MailManager.SendMailRetVal(bsurec.EnstituKod, "Başvurunuz taslak halindedir.Lütfen başvuru süreci bitmeden başvurunuzu onaylayınız", htmlMail, eMailList, null);
                        if (emailSend == null)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = bsurec.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = "Başvurunuz taslak halindedir.Lütfen başvuru süreci bitmeden başvurunuzu onaylayınız",
                                IslemYapanID = 1,
                                IslemYapanIP = UserIdentity.Ip,
                                Aciklama = "",
                                AciklamaHtml = htmlMail ?? "",
                                Gonderildi = true
                            };
                            var eklenen = db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                            foreach (var item in itemMailD.Value)
                            {
                                db.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = item.Caption, GonderilenMailID = kModel.GonderilenMailID, KullaniciID = item.Value });
                            }
                            eklenen.Gonderildi = true;
                            db.SaveChanges();
                        }

                    }
            }
            return taslakCount;
        }


    }
}