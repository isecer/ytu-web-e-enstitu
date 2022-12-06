using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;using LisansUstuBasvuruSistemi.Utilities.Dtos.KmDtos;using LisansUstuBasvuruSistemi.Utilities.Dtos.DmDtos;using LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos;using LisansUstuBasvuruSistemi.Utilities.Dtos.CmbDtos;
using BiskaUtil;
using System.Threading;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public class ApplicationClock
    {
        void tick(object _)
        {
            lock (tickLock)
            {
                try
                {
                    #region ZamanlananMailleriKontrolEt
                    //MessageBroker.Publisher.Publish(new Tick());
                    using (var db = new LubsDBEntities())
                    {
                        var tarih = DateTime.Now.Date;
                        var enstituler = db.Enstitulers.Where(p => p.IsAktif).ToList();
                        foreach (var itemE in enstituler)
                        {
                            var EnstituAdi = itemE.EnstituAd;
                            string MzamanlayiciMsg = "Mail Zamanlayıcısı Kontrolü Başlatıldı!<br />Enstitü: " + EnstituAdi;
                            var AktifBs = db.BasvuruSurecs.Where(p => p.IsAktif && (p.BaslangicTarihi <= tarih && p.BitisTarihi >= tarih) && (p.EnstituKod == itemE.EnstituKod)).ToList();
                            int TaslakCount = 0;
                            foreach (var itemBs in AktifBs)
                            {
                                TaslakCount = 0;
                                var bsOtoZamanBilgi = itemBs.BasvuruSurecOtoMails.Where(p => p.Gonderildi == false).ToList();
                                var kalanGun = Convert.ToInt32((itemBs.BitisTarihi - tarih).TotalDays);
                                var kalanSaat = Convert.ToInt32((itemBs.BitisTarihi - tarih).TotalHours);

                                foreach (var itmZmn in bsOtoZamanBilgi)
                                {
                                    if (itmZmn.ZamanTipID == ZamanTipi.Gun && itmZmn.Zaman == kalanGun)
                                    {
                                        TaslakCount += sendMailBsTaslak(itemBs, kalanGun, itmZmn.ZamanTipID, itmZmn.Zaman);
                                    }
                                }
                                if (itemBs.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) MzamanlayiciMsg += "<br />Lisansüstü taslak durumundaki başvuru hatırlatmaları (İşem Yapılan Başvuru Sayısı: " + TaslakCount + ")";
                                else if (itemBs.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru) MzamanlayiciMsg += "<br />Yatay Geçiş taslak durumundaki başvuru hatırlatmaları (İşem Yapılan Başvuru Sayısı: " + TaslakCount + ")";
                                else MzamanlayiciMsg += "<br />YTU Yeni Mezun Doktora taslak durumundaki başvuru hatırlatmaları (İşem Yapılan Başvuru Sayısı: " + TaslakCount + ")";

                            }

                            TaslakCount = 0;
                            var qAktifMzsID = Management.getAktifMezuniyetSurecID(itemE.EnstituKod);
                            if (qAktifMzsID.HasValue)
                            {
                                var qAktifMs = db.MezuniyetSurecis.Where(p => p.MezuniyetSurecID == qAktifMzsID.Value).FirstOrDefault();
                                if (qAktifMs != null)
                                {
                                    var bsOtoZamanBilgi = qAktifMs.MezuniyetSurecOtoMails.Where(p => p.Gonderildi == false && !p.MailSablonTipID.HasValue).ToList();
                                    var kalanGun = Convert.ToInt32((qAktifMs.BitisTarihi - tarih).TotalDays);
                                    foreach (var itmZmn in bsOtoZamanBilgi)
                                    {
                                        if (itmZmn.ZamanTipID == ZamanTipi.Gun && itmZmn.Zaman == kalanGun)
                                        {
                                            TaslakCount += sendMailMsTaslak(qAktifMs, kalanGun, itmZmn.ZamanTipID, itmZmn.Zaman);
                                        }

                                    }
                                    MzamanlayiciMsg += "<br />Mezuniyet taslak durumundaki başvuru hatırlatmaları (İşem Yapılan Başvuru Sayısı: " + TaslakCount + ")";
                                }
                            }

                            #region MezuniyetTezTeslimiBilgilendirme
                            var qMezuniyetOtoMails = db.MezuniyetSurecOtoMails.Where(p => p.MezuniyetSureci.EnstituKod == itemE.EnstituKod && p.Gonderildi == false && p.MailSablonTipID.HasValue).OrderByDescending(o => o.MezuniyetSurecID).ThenBy(o => o.MezuniyetSurecOtoMailID).ToList();
                            var MBControlCount = new Dictionary<string, int>();
                            if (qMezuniyetOtoMails.Any())
                            {
                                var MailSablonlari = db.MailSablonlaris.Where(p => p.EnstituKod == itemE.EnstituKod && p.IsAktif).ToList();
                                var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(itemE.EnstituKod);

                                foreach (var item in qMezuniyetOtoMails)
                                {
                                    var sblTr = MailSablonlari.Where(p => p.MailSablonTipID == item.MailSablonTipID).FirstOrDefault();
                                    var SblKey = sblTr.SablonAdi + "_" + sblTr.MailSablonTipID;
                                    if (!MBControlCount.Any(a => a.Key == SblKey))
                                    {
                                        MBControlCount.Add(SblKey, 0);
                                    }
                                    var KayitliGonderilenMailAdresleri = new List<string>();
                                    var GonderilecekMailAdresleri = new List<string>();

                                    var KayitliGonderilecekMBIDs = new List<int>();
                                    var GonderilecekMBIDs = new List<int>();
                                    if (item.Gonderilenler.IsNullOrWhiteSpace() == false)
                                    {
                                        KayitliGonderilenMailAdresleri.AddRange(item.Gonderilenler.Split(',').ToList().Select(s => s.Trim()).ToList());
                                    }
                                    if (item.GonderilenMBID.IsNullOrWhiteSpace() == false)
                                    {
                                        KayitliGonderilecekMBIDs.AddRange(item.GonderilenMBID.Split(',').ToList().Select(s => s.Trim().ToInt().Value).ToList());
                                    }
                                    var qbasvurular = item.MezuniyetSureci.MezuniyetBasvurularis.Where(p => p.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.KabulEdildi && !p.IsMezunOldu.HasValue);
                                    if (item.MailSablonTipID == MailSablonTipi.Mez_EykTarihineGoreSrAlinmali || item.MailSablonTipID == MailSablonTipi.Mez_EykTarihineGoreSrAlinmadi)
                                    {
                                        //Eyk Tarihi Girildikten Sonra Sr Talebi Yapılmayanları Getir
                                        qbasvurular = qbasvurular.Where(p => p.EYKTarihi.HasValue && (p.SRTalepleris.Any(a => a.SRDurumID == SRTalepDurum.Onaylandı) == false || p.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma));
                                    }
                                    if (item.MailSablonTipID == MailSablonTipi.Mez_SinavDegerlendirmeHatirlantmaDanismanDR || item.MailSablonTipID == MailSablonTipi.Mez_SinavDegerlendirmeHatirlantmaDanismanYL)
                                    {
                                        //Sınav yeri onaylandıktan sonra değerlendirme girmeyen danışmanlar
                                        qbasvurular = qbasvurular.Where(p => p.EYKTarihi.HasValue && p.SRTalepleris.Any(a => a.SRDurumID == SRTalepDurum.Onaylandı && (!a.MezuniyetSinavDurumID.HasValue || a.MezuniyetSinavDurumID == MezuniyetSinavDurum.SonucGirilmedi) && a.SRTaleplerJuris.Any(a2 => a2.JuriTipAdi == "TezDanismani" && (!a2.MezuniyetSinavDurumID.HasValue || a2.MezuniyetSinavDurumID == MezuniyetSinavDurum.SonucGirilmedi))));
                                    }
                                    else if (item.MailSablonTipID == MailSablonTipi.Mez_TezSinavSonucuSistemeGirilmedi)
                                    {
                                        //SR Talebi Yapıldıktan Sonra Sınav Sonucu Girilmeyenleri Getir
                                        qbasvurular = qbasvurular.Where(p => p.SRTalepleris.Any(a => a.SRDurumID == SRTalepDurum.Onaylandı && a.JuriSonucMezuniyetSinavDurumID.HasValue) && (p.MezuniyetSinavDurumID == MezuniyetSinavDurum.SonucGirilmedi || !p.MezuniyetSinavDurumID.HasValue));
                                    }
                                    else if (item.MailSablonTipID == MailSablonTipi.Mez_TezKontrolTezDosyasiYuklenmeli)
                                    {
                                        //Sınav sonucu başarılı olup tez dosyasını teslim etmeyenler getir.
                                        qbasvurular = qbasvurular.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili &&
                                                                             p.MezuniyetBasvurulariTezDosyalaris.Any() == false &&
                                                                             p.SRTalepleris.Any(p2 => p2.SRDurumID == SRTalepDurum.Onaylandı && p2.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili && p2.SRTalepleriBezCiltFormus.Any() == false)
                                                                        );
                                    }
                                    else if (item.MailSablonTipID == MailSablonTipi.Mez_CiltliTezTeslimYapilmali || item.MailSablonTipID == MailSablonTipi.Mez_CiltliTezTeslimYapilmadi)
                                    {
                                        //Sınav sonucu başarılı olup bezcilt formunu oluşturmayanlar ya da oluşturup teslim etmeyenleri getir.
                                        qbasvurular = qbasvurular.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili
                                                                          && (
                                                                             p.SRTalepleris.Any(p2 => p2.SRDurumID == SRTalepDurum.Onaylandı && p2.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili && p2.SRTalepleriBezCiltFormus.Any() == false)
                                                                             ||
                                                                             p.SRTalepleris.Any(p2 => p2.SRDurumID == SRTalepDurum.Onaylandı && p2.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili && p2.SRTalepleriBezCiltFormus.Any() && !p.IsMezunOldu.HasValue)
                                                                             )
                                                                        );
                                    }
                                    var basvurular = qbasvurular.Where(p => KayitliGonderilecekMBIDs.Contains(p.MezuniyetBasvurulariID) == false).ToList();
                                    foreach (var itemB in basvurular)
                                    {
                                        var OtKriterBilgi = itemB.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.Where(p => p.OgrenimTipKod == itemB.OgrenimTipKod).First();
                                        int? KalanGun = null;
                                        var EMailList = new List<MailSendList>();
                                        DateTime? SonTarih = null;
                                        DateTime? SinavTarihi = null;
                                        TimeSpan? SinavSaati = null;
                                        if (item.MailSablonTipID == MailSablonTipi.Mez_EykTarihineGoreSrAlinmali || item.MailSablonTipID == MailSablonTipi.Mez_EykTarihineGoreSrAlinmadi)
                                        {
                                            if (item.MailSablonTipID == MailSablonTipi.Mez_EykTarihineGoreSrAlinmali) EMailList.Add(new MailSendList { EMail = itemB.Kullanicilar.EMail, ToOrBcc = true });

                                            if (itemB.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma)
                                            {
                                                var SonSr = itemB.SRTalepleris.OrderByDescending(o => o.SRTalepID).First();
                                                var SrAlimSonTarih = SonTarih = SonSr.Tarih.AddDays(OtKriterBilgi.MBSinavUzatmaSuresiGun).Date;
                                                KalanGun = Convert.ToInt32((SrAlimSonTarih.Value - tarih).TotalDays);
                                                var Juri = SonSr.SRTaleplerJuris.OrderBy(p => p.SRTalepJuriID).First();
                                                EMailList.Add(new MailSendList { EMail = Juri.Email.Trim(), ToOrBcc = true });
                                                if (!itemB.TezEsDanismanEMail.IsNullOrWhiteSpace()) EMailList.Add(new MailSendList { EMail = itemB.TezEsDanismanEMail.Trim(), ToOrBcc = true });
                                            }
                                            else
                                            {
                                                var SrAlimSonTarih = SonTarih = itemB.EYKTarihi.Value.AddDays(OtKriterBilgi.MBTezTeslimSuresiGun);
                                                KalanGun = Convert.ToInt32((SrAlimSonTarih.Value - tarih).TotalDays);
                                            }
                                        }
                                        else if (item.MailSablonTipID == MailSablonTipi.Mez_SinavDegerlendirmeHatirlantmaDanismanDR || item.MailSablonTipID == MailSablonTipi.Mez_SinavDegerlendirmeHatirlantmaDanismanYL)
                                        {
                                            var SonSr = itemB.SRTalepleris.Where(a => a.SRDurumID == SRTalepDurum.Onaylandı && a.SRTaleplerJuris.Any(a2 => a2.JuriTipAdi == "TezDanismani" && !(a2.MezuniyetSinavDurumID > MezuniyetSinavDurum.SonucGirilmedi))).OrderByDescending(o => o.SRTalepID).First();
                                            var Danisman = SonSr.SRTaleplerJuris.Where(p => p.JuriTipAdi == "TezDanismani").First();
                                            SinavTarihi = SonSr.Tarih;
                                            SinavSaati = SonSr.BasSaat;

                                            var ToplantiTarihi = SinavTarihi.Value.Add(SinavSaati.Value);
                                            SonTarih = ToplantiTarihi;
                                            KalanGun = Convert.ToInt32((ToplantiTarihi - DateTime.Now).TotalHours);
                                            EMailList.Add(new MailSendList { EMail = Danisman.Email, ToOrBcc = true });

                                        }
                                        else if (item.MailSablonTipID == MailSablonTipi.Mez_TezSinavSonucuSistemeGirilmedi)
                                        {
                                            var SonSr = itemB.SRTalepleris.OrderByDescending(o => o.SRTalepID).First();
                                            var SinavSonucGirisSonTarih = SonTarih = SonSr.Tarih;
                                            KalanGun = Convert.ToInt32((SinavSonucGirisSonTarih.Value - tarih).TotalDays);
                                            EMailList.Add(new MailSendList { EMail = itemB.Kullanicilar.EMail, ToOrBcc = true });
                                            var Juri = SonSr.SRTaleplerJuris.OrderBy(p => p.SRTalepJuriID).First();
                                            EMailList.Add(new MailSendList { EMail = Juri.Email.Trim(), ToOrBcc = true });
                                            if (!itemB.TezEsDanismanEMail.IsNullOrWhiteSpace()) EMailList.Add(new MailSendList { EMail = itemB.TezEsDanismanEMail.Trim(), ToOrBcc = true });
                                        }
                                        else if (item.MailSablonTipID == MailSablonTipi.Mez_TezKontrolTezDosyasiYuklenmeli)
                                        {
                                            var SonSr = itemB.SRTalepleris.OrderByDescending(o => o.SRTalepID).First();
                                            var SinavSonucGirisSonTarih = SonTarih = SonSr.Tarih;
                                            KalanGun = Convert.ToInt32((SinavSonucGirisSonTarih.Value - tarih).TotalDays);
                                            EMailList.Add(new MailSendList { EMail = itemB.Kullanicilar.EMail, ToOrBcc = true });
                                        }
                                        else if (item.MailSablonTipID == MailSablonTipi.Mez_CiltliTezTeslimYapilmali || item.MailSablonTipID == MailSablonTipi.Mez_CiltliTezTeslimYapilmadi)
                                        {
                                            var SonSr = itemB.SRTalepleris.OrderByDescending(o => o.SRTalepID).First();
                                            var TezTeslimSonTarihi = SonTarih = itemB.TezTeslimSonTarih ?? SonSr.Tarih.AddDays(OtKriterBilgi.MBTezTeslimSuresiGun).Date;

                                            KalanGun = Convert.ToInt32((TezTeslimSonTarihi.Value - tarih).TotalDays);
                                            if (item.MailSablonTipID == MailSablonTipi.Mez_CiltliTezTeslimYapilmali)
                                            {
                                                EMailList.Add(new MailSendList { EMail = itemB.Kullanicilar.EMail, ToOrBcc = true });
                                                var srTalep = itemB.SRTalepleris.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili).First();
                                                var Juri = srTalep.SRTaleplerJuris.OrderBy(p => p.SRTalepJuriID).First();
                                                EMailList.Add(new MailSendList { EMail = Juri.Email.Trim(), ToOrBcc = true });
                                                if (!itemB.TezEsDanismanEMail.IsNullOrWhiteSpace()) EMailList.Add(new MailSendList { EMail = itemB.TezEsDanismanEMail.Trim(), ToOrBcc = true });
                                            }
                                            else
                                            {
                                            }
                                        }
                                        MailSablonlari Sablon = null;
                                        bool GunIsSuccess = KalanGun.HasValue;
                                        //Covid-19 salgını nedeni ile 6. ayın 1 ine kadar olan tarihlerdeki işlemler mail atılmayacak
                                        if (!SonTarih.HasValue || SonTarih.Value < new DateTime(2020, 6, 1))
                                        {
                                            GunIsSuccess = false;
                                        }
                                        if (GunIsSuccess)
                                        {
                                            if (item.Zaman >= 0)
                                            {
                                                GunIsSuccess = KalanGun <= item.Zaman && KalanGun >= 0;
                                            }
                                            else
                                            {
                                                GunIsSuccess = KalanGun <= item.Zaman;
                                            }
                                            if (GunIsSuccess)
                                            {
                                                Sablon = MailSablonlari.Where(p => p.MailSablonTipID == item.MailSablonTipID && p.EnstituKod == itemE.EnstituKod).FirstOrDefault();
                                                if (Sablon.GonderilecekEkEpostalar.IsNullOrWhiteSpace() == false)
                                                    EMailList.AddRange(Sablon.GonderilecekEkEpostalar.Split(',').ToList().Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                                            }
                                        }
                                        //mailleri gönder
                                        if (item.ZamanTipID == ZamanTipi.Gun && GunIsSuccess && EMailList.Count > 0)
                                        {
                                            var EnstituL = itemE;
                                            var ParamereDegerleri = new List<MailReplaceParameterModel>();
                                            var Parametreler = Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                                            var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                                            var Attachments = new List<System.Net.Mail.Attachment>();
                                            foreach (var itemSe in Sablon.MailSablonlariEkleris)
                                            {
                                                var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                                                if (System.IO.File.Exists(ekTamYol))
                                                {
                                                    var FExtension = System.IO.Path.GetExtension(ekTamYol);
                                                    Attachments.Add(new System.Net.Mail.Attachment(new System.IO.MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                                        itemSe.EkAdi.ToSetNameFileExtension(FExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                                    gonderilenMEkleris.Add(new GonderilenMailEkleri
                                                    {
                                                        EkAdi = itemSe.EkAdi,
                                                        EkDosyaYolu = itemSe.EkDosyaYolu,
                                                    });
                                                }
                                                else Management.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "ApplicationClock", BilgiTipi.Uyarı);
                                            }
                                            if (Parametreler.Any(a => a == "@EnstituAdi"))
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = EnstituL.EnstituAd });
                                            if (Parametreler.Any(a => a == "@WebAdresi"))
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = itemE.WebAdresi, IsLink = true });
                                            if (Parametreler.Any(a => a == "@AdSoyad"))
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = itemB.Ad + " " + itemB.Soyad });
                                            if (Parametreler.Any(a => a == "@OgrenciAdSoyad"))
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciAdSoyad", Value = itemB.Ad + " " + itemB.Soyad });
                                            if (Parametreler.Any(a => a == "@DanismanAdSoyad"))
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanAdSoyad", Value = itemB.TezDanismanAdi });
                                            if (Parametreler.Any(a => a == "@DanismanUnvanAdi"))
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanUnvanAdi", Value = itemB.TezDanismanUnvani });
                                            if (Parametreler.Any(a => a == "@OgrenciNo"))
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciNo", Value = itemB.OgrenciNo });
                                            if (Parametreler.Any(a => a == "@EYKTarihi"))
                                            {
                                                var EykTarih = itemB.EYKTarihi.HasValue ? itemB.EYKTarihi.Value.ToDateString() : "";
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EYKTarihi", Value = EykTarih });
                                            }
                                            if (Parametreler.Any(a => a == "@SRTarihi"))
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "SRTarihi", Value = SonTarih.ToDateString() });
                                            if (Parametreler.Any(a => a == "@SinavTarihi") && SinavTarihi.HasValue)
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "SinavTarihi", Value = SinavTarihi.Value.ToLongDateString() });
                                            if (Parametreler.Any(a => a == "@SinavSaati") && SinavSaati.HasValue)
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "SinavSaati", Value = string.Format("{0:hh\\:mm}", SinavSaati) });
                                            if (Parametreler.Any(a => a == "@AnabilimDaliAdi"))
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AnabilimDaliAdi", Value = itemB.Programlar.AnabilimDallari.AnabilimDaliAdi });
                                            if (Parametreler.Any(a => a == "@ProgramAdi"))
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ProgramAdi", Value = itemB.Programlar.ProgramAdi });
                                            if (Parametreler.Any(a => a == "@CiltTeslimTarih"))//düzenlenecek
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "CiltTeslimTarih", Value = SonTarih.HasValue ? SonTarih.ToDateString() : "" });
                                            if (Parametreler.Any(a => a == "@DonemAdi"))
                                            {
                                                var DonemAdi = itemB.MezuniyetSureci.BaslangicYil + " " + itemB.MezuniyetSureci.BitisYil + " " + itemB.MezuniyetSureci.Donemler.DonemAdi;
                                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DonemAdi", Value = DonemAdi });
                                            }
                                            if (Parametreler.Any(a => a == "@Link"))
                                            {
                                                if (item.MailSablonTipID == MailSablonTipi.Mez_TezKontrolTezDosyasiYuklenmeli)
                                                {
                                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Link", Value = itemE.SistemErisimAdresi + "/mezuniyet/Index?RowID=" + itemB.RowID, IsLink = true });
                                                }
                                            }
                                            // EMailList = new List<MailSendList> { new MailSendList { EMail = "irfansecer@gmail.com", ToOrBcc = true } };
                                            var mCOntent = SystemMails.GetSystemMailContent(EnstituL.EnstituAd, Sablon.SablonHtml, Sablon.SablonAdi, ParamereDegerleri);

                                            var snded = MailManager.sendMail(itemE.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, EMailList, Attachments);
                                            if (snded)
                                            {
                                                GonderilecekMBIDs.Add(itemB.MezuniyetBasvurulariID);
                                                GonderilecekMailAdresleri.AddRange(EMailList.Select(s => s.EMail));
                                                var kModel = new GonderilenMailler();
                                                kModel.Tarih = DateTime.Now;
                                                kModel.EnstituKod = mailBilgi.EnstituKod;
                                                kModel.MesajID = null;
                                                kModel.IslemTarihi = DateTime.Now;
                                                kModel.Konu = mCOntent.Title + " (" + itemB.Ad + " " + itemB.Soyad + ")";
                                                kModel.IslemYapanID = 1;
                                                kModel.IslemYapanIP = UserIdentity.Ip;
                                                kModel.Aciklama = Sablon.Sablon ?? "";
                                                kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                                                kModel.Gonderildi = true;
                                                kModel.GonderilenMailKullanicilars = EMailList.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                                                kModel.GonderilenMailEkleris = gonderilenMEkleris;
                                                db.GonderilenMaillers.Add(kModel);
                                                db.SaveChanges();
                                            }
                                        }
                                    }

                                    if (GonderilecekMailAdresleri.Count > 0 || GonderilecekMBIDs.Count > 0)
                                    {
                                        item.GonderilenCount = KayitliGonderilecekMBIDs.Count + GonderilecekMBIDs.Count;
                                        item.Gonderilenler = (item.Gonderilenler.IsNullOrWhiteSpace() ? "" : item.Gonderilenler + ",") + string.Join(",", GonderilecekMailAdresleri);
                                        item.GonderilenMBID = (item.GonderilenMBID.IsNullOrWhiteSpace() ? "" : item.GonderilenMBID + ",") + string.Join(",", GonderilecekMBIDs);
                                        db.SaveChanges();
                                        MBControlCount[SblKey] = MBControlCount[SblKey] + GonderilecekMBIDs.Count;
                                    }

                                }
                                foreach (var itemCnt in MBControlCount)
                                {
                                    MzamanlayiciMsg += "<br />" + itemCnt.Key + " (İşem Yapılan Başvuru Sayısı: " + itemCnt.Value + ")";
                                }
                            }

                            #endregion
                            db.SistemBilgilendirmes.Add(new SistemBilgilendirme()
                            {
                                Message = MzamanlayiciMsg,
                                BilgiTipi = BilgiTipi.Uyarı,
                                IslemYapanID = 1,
                                IslemYapanIP = "::",
                                IslemTarihi = DateTime.Now,
                                StackTrace = "ApplicationClock"
                            });
                            db.SaveChanges();
                        }



                    }
                    #endregion
                }
                catch (Exception e)
                {
                    Management.SistemBilgisiKaydet("Toplu mail gönderilirken bir hata oluştu! hata:" + e.ToExceptionMessage(), e.ToExceptionStackTrace(), BilgiTipi.Hata, 1, "::");
                }
            }
        }
        readonly Object tickLock = new Object();
        Timer ticker = null;
        public ApplicationClock Start()
        {

            ticker = new Timer(tick, null, 0, 1 * 60 * 60 * 1000);
            return this;

            // ticker = new Timer(tick, null, 0, 1 * 5 * 1000);

        }
        public int sendMailBsTaslak(BasvuruSurec Bsurec, int kalanGun, int ZamanTipID, int Zaman)
        {

            int TaslakCount = 0;
            using (var db = new LubsDBEntities())
            {
                var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(Bsurec.EnstituKod);
                var _ea = mailBilgi.SistemErisimAdresi;
                var WurlAddr = _ea.Split('/').ToList();
                if (_ea.Contains("//"))
                    _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
                else
                    _ea = "http://" + WurlAddr.First();
                var qTaslaklar = db.Basvurulars.Where(p => p.BasvuruSurecID == Bsurec.BasvuruSurecID && p.BasvuruDurumID == BasvuruDurumu.Taslak).Select(s => new { s.EMail, s.Kullanicilar.KullaniciTipleri.Yerli, s.KullaniciID }).Distinct().ToList();
                Dictionary<string, List<ComboModelInt>> dct = new Dictionary<string, List<ComboModelInt>>();
                dct.Add("", qTaslaklar.Select(s => new ComboModelInt { Value = s.KullaniciID, Caption = s.EMail }).ToList()); 
                var bdurumAds = db.BasvuruDurumlaris.Where(p => p.BasvuruDurumID == BasvuruDurumu.Taslak).ToList();
                TaslakCount = qTaslaklar.Count();
                var zmnB = db.BasvuruSurecOtoMails.Where(p => p.BasvuruSurecID == Bsurec.BasvuruSurecID && p.ZamanTipID == ZamanTipID && p.Zaman == Zaman).FirstOrDefault();
                zmnB.Gonderildi = true;
                zmnB.GonderilenCount = TaslakCount;
                var GonderilecekTumMailler = dct.SelectMany(s => s.Value.Select(s2 => s2.Caption)).Distinct().ToList();
                zmnB.Gonderilenler = zmnB.Gonderilenler.IsNullOrWhiteSpace() ? string.Join(",", GonderilecekTumMailler) : zmnB.Gonderilenler + string.Join(",", GonderilecekTumMailler);

                if (TaslakCount > 0)
                    foreach (var itemMailD in dct)
                    {

                         
                        string BasvuruDurumAdi = bdurumAds.First().BasvuruDurumAdi;
                        var mmmC = new mdlMailMainContent();
                        var enstitu = db.Enstitulers.Where(p => p.EnstituKod == Bsurec.EnstituKod).First();
                        mmmC.EnstituAdi = enstitu.EnstituAd;
                        mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                        mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png"; ;




                        var htmlCbilgi = new mailTableContent();

                        var ackName = "";
                        if (Bsurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) ackName = "Lisansüstü Başvuru sürecinde yapmış olduğunuz başvurunuz taslak halindendir. Başvuru sürecinin bitimine _xXxGun_  gün kalmıştır. Eğer başvurunuzu _xXxGun_ içerisinde onaylamazsanız başvurunuz geçersiz sayılacaktır.";
                        else if (Bsurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru) ackName = "Yatay Geçiş Başvuru sürecinde yapmış olduğunuz başvurunuz taslak halindendir. Başvuru sürecinin bitimine _xXxGun_  gün kalmıştır. Eğer başvurunuzu _xXxGun_ içerisinde onaylamazsanız başvurunuz geçersiz sayılacaktır.";
                        else ackName = "YTU Yeni Mezun Doktora Başvuru sürecinde yapmış olduğunuz başvurunuz taslak halindendir. Başvuru sürecinin bitimine _xXxGun_  gün kalmıştır. Eğer başvurunuzu _xXxGun_ içerisinde onaylamazsanız başvurunuz geçersiz sayılacaktır.";
                        htmlCbilgi.AciklamaBasligi = ackName.Replace("_xXxGun_", kalanGun.ToString() + " Gün ");
                        htmlCbilgi.GrupBasligi = "BAŞVURU BİLGİSİ";
                        htmlCbilgi.Detaylar.Add(new mailTableRow { Baslik = "Başvuru Durumu", Aciklama = BasvuruDurumAdi });
                        htmlCbilgi.Detaylar.Add(new mailTableRow { Baslik = "Başvuru Süreci", Aciklama = Bsurec.BaslangicTarihi.ToFormatDateAndTime() + " - " + Bsurec.BitisTarihi.ToFormatDateAndTime() });
                        htmlCbilgi.Detaylar.Add(new mailTableRow { Baslik = "Başvuruyu Onaylamanız İçin Kalan Süre", Aciklama = (kalanGun == 1 ? "Son" : kalanGun.ToString()) + " Gün" });
                        htmlCbilgi.Detaylar.Add(new mailTableRow { Baslik = "Sisteme Erişim Adresi", Aciklama = enstitu.SistemErisimAdresi });

                        var tableContent = Management.RenderPartialView("Ajax", "getMailTableContent", htmlCbilgi);
                        mmmC.Content = tableContent;
                        string htmlMail = Management.RenderPartialView("Ajax", "getMailContent", mmmC);
                        var EMailList = itemMailD.Value.Select(s => s.Caption).Distinct().Select(s => new MailSendList { EMail = s, ToOrBcc = true }).ToList();
                        //EMailList = new List<MailSendList> { new MailSendList { EMail = "irfansecer@gmail.com", ToOrBcc = true } };

                        var emailSend = MailManager.sendMailRetVal(Bsurec.EnstituKod, "Başvurunuz taslak halindedir. Lütfen başvuru süreci bitmeden başvurunuzu onaylayınız", htmlMail, EMailList, null);

                        if (emailSend == null)
                        {



                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Bsurec.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = "Başvurunuz taslak halindedir. Lütfen başvuru süreci bitmeden başvurunuzu onaylayınız";
                            kModel.IslemYapanID = 1;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = "";
                            kModel.AciklamaHtml = htmlMail ?? "";
                            kModel.Gonderildi = true;
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
            return TaslakCount;
        }
        public int sendMailMsTaslak(MezuniyetSureci Bsurec, int kalanGun, int ZamanTipID, int Zaman)
        {

            int TaslakCount = 0;
            using (var db = new LubsDBEntities())
            {
                var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(Bsurec.EnstituKod);
                var _ea = mailBilgi.SistemErisimAdresi;
                var WurlAddr = _ea.Split('/').ToList();
                if (_ea.Contains("//"))
                    _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
                else
                    _ea = "http://" + WurlAddr.First();
                var qTaslaklar = db.MezuniyetBasvurularis.Where(p => p.MezuniyetSurecID == Bsurec.MezuniyetSurecID && p.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.Taslak).Select(s => new { s.Kullanicilar.EMail, s.Kullanicilar.KullaniciTipleri.Yerli, s.KullaniciID }).Distinct().ToList();
                Dictionary<string, List<ComboModelInt>> dct = new Dictionary<string, List<ComboModelInt>>();
                dct.Add("", qTaslaklar.Select(s => new ComboModelInt { Value = s.KullaniciID, Caption = s.EMail }).ToList()); 
                var bdurumAds = db.BasvuruDurumlaris.Where(p => p.BasvuruDurumID == MezuniyetYayinKontrolDurumu.Taslak).ToList();
                TaslakCount = qTaslaklar.Count();
                var zmnB = db.MezuniyetSurecOtoMails.Where(p => p.MezuniyetSurecID == Bsurec.MezuniyetSurecID && p.MailSablonTipID.HasValue == false && p.ZamanTipID == ZamanTipID && p.Zaman == Zaman).FirstOrDefault();
                zmnB.Gonderildi = true;
                zmnB.GonderilenCount = TaslakCount;
                var GonderilecekTumMailler = dct.SelectMany(s => s.Value.Select(s2 => s2.Caption)).Distinct().ToList();
                zmnB.Gonderilenler = zmnB.Gonderilenler.IsNullOrWhiteSpace() ? string.Join(",", GonderilecekTumMailler) : zmnB.Gonderilenler + string.Join(",", GonderilecekTumMailler);
                if (TaslakCount > 0)
                    foreach (var itemMailD in dct)
                    {


                        string _DilKodu = itemMailD.Key;
                        string BasvuruDurumAdi = bdurumAds.First().BasvuruDurumAdi;
                        var mmmC = new mdlMailMainContent();
                        var enstitu = db.Enstitulers.Where(p => p.EnstituKod == Bsurec.EnstituKod).First();
                        mmmC.EnstituAdi = enstitu.EnstituAd;
                        mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                        mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png"; ;


                        var htmlCbilgi = new mailTableContent();
                        htmlCbilgi.AciklamaBasligi = "Mezuniyet Başvuru sürecinde yapmış olduğunuz başvurunuz taslak halindendir. Başvuru sürecinin bitimine " + kalanGun + "  gün kalmıştır. Eğer başvurunuzu " + kalanGun + " içerisinde onaylamazsanız başvurunuz geçersiz sayılacaktır.";
                        htmlCbilgi.GrupBasligi = "BAŞVURU BİLGİSİ";
                        htmlCbilgi.Detaylar.Add(new mailTableRow { Baslik = "Başvuru Durumu", Aciklama = BasvuruDurumAdi });
                        htmlCbilgi.Detaylar.Add(new mailTableRow { Baslik = "Başvuru Süreci", Aciklama = Bsurec.BaslangicTarihi.ToFormatDateAndTime() + " - " + Bsurec.BitisTarihi.ToFormatDateAndTime() });
                        htmlCbilgi.Detaylar.Add(new mailTableRow { Baslik = "Başvuruyu Onaylamanız İçin Kalan Süre", Aciklama = (kalanGun == 1 ? "Son" : kalanGun.ToString()) + " Gün" });
                        htmlCbilgi.Detaylar.Add(new mailTableRow { Baslik = "Sisteme Erişim Adresi", Aciklama = enstitu.SistemErisimAdresi });


                        var tableContent = Management.RenderPartialView("Ajax", "getMailTableContent", htmlCbilgi);
                        mmmC.Content = tableContent;
                        string htmlMail = Management.RenderPartialView("Ajax", "getMailContent", mmmC);
                        var EMailList = itemMailD.Value.Select(s => s.Caption).Distinct().Select(s => new MailSendList { EMail = s, ToOrBcc = false }).ToList();
                        // EMailList = new List<MailSendList> { new MailSendList { EMail = "irfansecer@gmail.com", ToOrBcc = true } };
                        var emailSend = MailManager.sendMailRetVal(Bsurec.EnstituKod, "Başvurunuz taslak halindedir.Lütfen başvuru süreci bitmeden başvurunuzu onaylayınız", htmlMail, EMailList, null);
                        if (emailSend == null)
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Bsurec.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = "Başvurunuz taslak halindedir.Lütfen başvuru süreci bitmeden başvurunuzu onaylayınız";
                            kModel.IslemYapanID = 1;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = "";
                            kModel.AciklamaHtml = htmlMail ?? "";
                            kModel.Gonderildi = true;
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
            return TaslakCount;
        }


    }
}