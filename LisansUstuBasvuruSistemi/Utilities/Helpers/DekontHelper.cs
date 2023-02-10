using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class DekontHelper
    {
        public static string DekontOdemeIsle(string SiparisNo, DateTime DekontTarih, string Ucret, string taksit, string CardNo)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var msg = "";
                var DekontBilgi = db.BasvurularTercihleriKayitOdemeleris.Where(p => p.DekontNo == SiparisNo).FirstOrDefault();
                if (DekontBilgi != null)
                {

                    if (!DekontBilgi.IsOdendi)
                    {
                        var kul = DekontBilgi.BasvurularTercihleri.Basvurular.Kullanicilar;
                        var tercihPrg = DekontBilgi.BasvurularTercihleri.Programlar;
                        DekontBilgi.Ucret = Ucret.ToDouble().Value;
                        DekontBilgi.DekontTarih = DekontTarih;
                        DekontBilgi.IsOdendi = true;
                        DekontBilgi.IslemYapanIP = UserIdentity.Ip;
                        DekontBilgi.IslemTarihi = DateTime.Now;
                        if (UserIdentity.Current != null && UserIdentity.Current.IsAuthenticated)
                        {
                            DekontBilgi.IslemYapanID = UserIdentity.Current.Id;


                        }
                        db.SaveChanges();

                        if (DekontBilgi.DonemNo > 1 && DekontBilgi.BasvurularTercihleri.MulakatSonuclaris.Any(a => a.KayitDurumID.HasValue && a.KayitDurumlari.IsKayitOldu == true))
                        {
                            string msj = "";
                            var IsSuccess = true;//SetPaymentInfoGsis(kul.OgrenciNo, Ucret, SiparisNo, DekontTarih, out msj);

                            if (!IsSuccess)
                            {
                                SistemBilgilendirmeBus.SistemBilgisiKaydet("Sanal pos ödeme sonrası dekont bilgisi işlenemedi! (" + kul.Ad + " " + kul.Soyad + ") " + msj, "Management/DekontOdemeIsle", LogType.Kritik);
                            }

                        }


                        #region SendMail
                        var Enstituler = DekontBilgi.BasvurularTercihleri.Basvurular.BasvuruSurec.Enstituler;
                        string EnstituKod = Enstituler.EnstituKod;
                        var Sablon = db.MailSablonlaris.Where(p => p.MailSablonTipleri.SistemMaili && p.MailSablonTipID == MailSablonTipi.LisansustuSanalPosOdemeBilgisi).First();
                        var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(EnstituKod);
                        var mmmC = new MailMainContentDto();

                        mmmC.EnstituAdi = db.Enstitulers.Where(p => p.EnstituKod == EnstituKod).First().EnstituAd;
                        var _ea = mailBilgi.SistemErisimAdresi;
                        var WurlAddr = _ea.Split('/').ToList();
                        if (_ea.Contains("//"))
                            _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
                        else
                            _ea = "http://" + WurlAddr.First();
                        mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png";
                        mmmC.UniversiteAdi = "Yıldız Tekni Üniversitesi";

                        var contentHtml = Sablon.SablonHtml;
                        #region replaces 
                        var OdemeIslemAdi = "";

                        if (DekontBilgi.BasvurularTercihleri.IsOgrenimUcretiOrKatkiPayi.Value)
                        {
                            OdemeIslemAdi = DekontBilgi.DonemNo + ". Dönem Öğrenim Ücreti Ödemesi";
                        }
                        else
                        {

                            OdemeIslemAdi = "Katkı Payı Ücreti Ödemesi";
                        }
                        var Bsurec = DekontBilgi.BasvurularTercihleri.Basvurular.BasvuruSurec;
                        var KayitDonemi = Bsurec.BaslangicYil + "/" + Bsurec.BitisYil + " " + Bsurec.Donemler.DonemAdi;
                        var TaksitBilgisi = "";
                        if (taksit.IsNullOrWhiteSpace())
                        {
                            TaksitBilgisi = "Taksit Yok";
                        }
                        else
                        {
                            TaksitBilgisi = taksit + " Taksit";
                        }
                        contentHtml = contentHtml.Replace("@AdSoyad", kul.Ad + " " + kul.Soyad);
                        contentHtml = contentHtml.Replace("@OdemeIslemAdi", OdemeIslemAdi);
                        contentHtml = contentHtml.Replace("@KayitDonemi", KayitDonemi);
                        contentHtml = contentHtml.Replace("@ProgramAdi", tercihPrg.ProgramAdi);
                        contentHtml = contentHtml.Replace("@KartNo", CardNo);
                        contentHtml = contentHtml.Replace("@DekontNo", SiparisNo);
                        contentHtml = contentHtml.Replace("@DekontTarihi", DekontBilgi.DekontTarih.ToString("dd.MM.yyyy"));
                        contentHtml = contentHtml.Replace("@KartNo", CardNo);
                        contentHtml = contentHtml.Replace("@Ucret", Ucret);
                        contentHtml = contentHtml.Replace("@Taksit", TaksitBilgisi);
                        contentHtml = contentHtml.Replace("@EnstituAdi", mmmC.EnstituAdi);
                        var webadresLink = "<a href='" + mailBilgi.WebAdresi + "' target='_blank'>" + mailBilgi.WebAdresi + "</a>";
                        contentHtml = contentHtml.Replace("@WebAdresi", webadresLink);
                        mmmC.Content = contentHtml;
                        #endregion
                        msg = contentHtml;
                        // msg = "Sayın " + kul.Ad + " " + kul.Soyad + " <br/>Lisansüstü başvuru sistemi üzerinden <b>" + CardNo + "</b> numaralı kartınızla <b>" + DekontBilgi.DekontTarih.ToString("dd.MM.yyyy") + "</b> tarihinde yapmış olduğunuz <b>" + tercihPrg.ProgramAdi + "</b> Program kaydı için gereken <b>" + Ucret + " TL</b> Ücretli Sanal Pos ödeme işleminiz <b>" + (taksit.IsNullOrWhiteSpace() ? "Taksitsiz" : taksit + " Taksit") + "</b> olarak <b>" + SiparisNo + "</b> Sipariş Numarası ile başarılı bir şekilde gerçekleşmiştir.";

                        var EMailList = new List<MailSendList> { new MailSendList { EMail = kul.EMail, ToOrBcc = true } };
                        mmmC.Content = contentHtml;
                        string htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);
                        var Attachments = new List<System.Net.Mail.Attachment>();
                        if (Sablon.GonderilecekEkEpostalar.IsNullOrWhiteSpace() == false)
                        {
                            EMailList.AddRange(Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        }

                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var FExtension = Path.GetExtension(ekTamYol);
                                Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(FExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailMezuniyetSinavYerBilgisi", LogType.Uyarı);
                        }
                        var sndMail = MailManager.SendMail(EnstituKod, Sablon.SablonAdi, htmlMail, EMailList, Attachments);

                        if (sndMail)
                        {

                            var mailList = new List<GonderilenMailKullanicilar>();
                            foreach (var item in EMailList)
                            {
                                mailList.Add(new GonderilenMailKullanicilar
                                {
                                    Email = item.EMail,
                                });
                            }

                            var kModel = new GonderilenMailler();
                            kModel.EnstituKod = EnstituKod;
                            kModel.MesajID = null;
                            kModel.Tarih = DateTime.Now;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Gonderildi = true;
                            kModel.Konu = Enstituler.EnstituKisaAd + " SANAL POS ÖDEME İŞLEMİ (" + kul.Ad + " " + kul.Soyad + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = "";
                            kModel.AciklamaHtml = htmlMail ?? "";
                            kModel.GonderilenMailKullanicilars = mailList;
                            kModel.GonderilenMailEkleris = gonderilenMEkleris;
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }
                    #endregion
                }
                else
                {
                    msg = "Ödeme İşleminden Sonra Sipariş Numarası Hiçbir Program Tercihi İle Eşleşmedi!";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet("Ödeme İşleminden Sonra Sipariş Numarası Hiçbir Program Tercihi İle Eşleşmedi! <br/>Sipariş No: " + SiparisNo, "Management/DekontOdemeIsle", LogType.Kritik);
                }
                return msg;
            }

        }
        public static string DekontNoUret(int KayitYilBas, int DonemNo, int OdemeDonemNo, string ProgramKod, string EnstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var EnstituKodB = EnstituKod == EnstituKodlari.FenBilimleri ? "F" : "S";
                string DekontNo = DekontNoBirlestir(EnstituKodB, KayitYilBas, DonemNo, OdemeDonemNo, ProgramKod);

                while (db.BasvurularTercihleriKayitOdemeleris.Where(p => p.DekontNo != null && p.DekontNo != "" && p.DekontNo == DekontNo).Any())
                {
                    DekontNo = DekontNoBirlestir(EnstituKodB, KayitYilBas, DonemNo, OdemeDonemNo, ProgramKod);
                }
                return DekontNo;
            }
        }
        public static string DekontNoBirlestir(string EnstituKodB, int KayitYilBas, int DonemNo, int OdemeDonemNo, string ProgramKod)
        {
            var rndID = Guid.NewGuid().ToString().ToUpper().Substring(0, 6);
            return EnstituKodB + "-" + KayitYilBas + "/" + DonemNo + "-" + OdemeDonemNo + '-' + ProgramKod + "-" + rndID;

        }
    }
}