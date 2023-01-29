using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;

using BiskaUtil;
using System.Threading;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Models
{

    public class mdlMailMainContent
    { 
        public string LogoPath { get; set; }
        public string UniversiteAdi { get; set; }
        public string EnstituAdi { get; set; }
        public string Content { get; set; }

    }
    public class mailTableContent
    {
        public bool IsJuriBilgi { get; set; } 
        public string AciklamaBasligi { get; set; }
        public string AciklamaDetayi { get; set; }
        public bool AciklamaTextAlingCenter { get; set; }
        public string GrupBasligi { get; set; }
        public int CaptTdWidth { get; set; }
        public List<mailTableRow> Detaylar { get; set; }
        public bool Success { get; set; }
        public mailTableContent()
        {
            CaptTdWidth = 200;
            Detaylar = new List<mailTableRow>(); 
            AciklamaTextAlingCenter = false;
        }

    }
    public class mailTableRow
    {
        public bool Colspan2 { get; set; }
        public int SiraNo { get; set; }
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
        public mailTableRow()
        {
            Colspan2 = false;
        }
    }
    public class MailSendList
    {
        public string EMail { get; set; }
        public bool ToOrBcc { get; set; }
    }
    public static class MailManager
    {
        public static Exception sendMailRetVal(string EnstituKod, string Konu, string Icerik, string EMail, List<Attachment> attach, bool ToOrBCC = true)
        {
            Exception exRet = null;

            try
            {
                MailManager.sendMail(EnstituKod, Konu, Icerik, EMail, attach, ToOrBCC);

            }
            catch (Exception ex)
            {
                exRet = ex;
            }

            return exRet;
        }
        public static Exception sendMailRetVal(string EnstituKod, string Konu, string Icerik, List<MailSendList> EMails, List<Attachment> attach)
        {
            Exception exRet = null;

            try
            {
                MailManager.sendMail(EnstituKod, Konu, Icerik, EMails, attach);

            }
            catch (Exception ex)
            {
                exRet = ex;
            }

            return exRet;
        }
        public static void sendMail(int GonderilenMailID, string Konu, string Icerik, List<MailSendList> EMails, List<Attachment> Attachs)
        {

            #region sendMail
            var uid = UserIdentity.Current.Id;
            var uIp = UserIdentity.Ip;
            new System.Threading.Thread(() =>
            {
                var UserID = uid;
                var Ip = uIp;
                try
                {
                    using (var dbb = new LisansustuBasvuruSistemiEntities())
                    {
                        var qeklenen = dbb.GonderilenMaillers.Where(p => p.GonderilenMailID == GonderilenMailID).First();
                        try
                        {
                            MailManager.sendMail(qeklenen.EnstituKod, Konu, Icerik, EMails, Attachs);
                            qeklenen.Gonderildi = true;
                            qeklenen.IslemTarihi = DateTime.Now;
                        }
                        catch (Exception ex)
                        {
                            qeklenen.HataMesaji = ex.ToExceptionMessage();
                            Management.SistemBilgisiKaydet("Mail gönderim işlemi yapılamadı! Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogType.Hata, uid, uIp);
                        }
                        dbb.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Management.SistemBilgisiKaydet("Mail gönderim işlemi sırasında bir hata oluştu! Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogType.Hata, uid, uIp);
                }
            }).Start();

            #endregion
        }
        public static bool sendMail(string EnstituKod, string Konu, string Icerik, string EMail, List<Attachment> Attachs, bool ToOrBcc = true)
        {
            var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(EnstituKod);
            var EmailAdresi = mailBilgi.SmtpMailAdresi;
            var Name = mailBilgi.SmtpKullaniciAdi;
            var Sifre = mailBilgi.SmtpSifre;
            var Port = mailBilgi.SmtpPortAdresi;
            var Host = mailBilgi.SmtpHost;
            var SSL = mailBilgi.SmtpSSL;

            using (var ePosta = new MailMessage())
            {
                ePosta.From = new MailAddress(EmailAdresi, Name, System.Text.Encoding.UTF8);
                ePosta.IsBodyHtml = true;
                ePosta.To.Add(EMail);

                ePosta.Subject = Konu;
                ePosta.Body = Icerik;
                ePosta.BodyEncoding = System.Text.Encoding.UTF8;
                ePosta.Priority = MailPriority.High;


                if (Attachs != null)
                    foreach (var item in Attachs)
                        ePosta.Attachments.Add(item);
                using (var smtp = new SmtpClient())
                {
                    smtp.Credentials = new System.Net.NetworkCredential(EmailAdresi, Sifre);
                    smtp.Port = Port.ToInt().Value;
                    smtp.Host = Host;
                    smtp.EnableSsl = SSL;
                    smtp.Timeout = 5 * 60 * 1000;
                    smtp.Send(ePosta);
                }
            }
            return true;



        }
        public static bool sendMail(string EnstituKod, string Konu, string Icerik, List<MailSendList> EMails, List<Attachment> Attachs)
        {
             
            var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(EnstituKod);
            var EmailAdresi = mailBilgi.SmtpMailAdresi;
            var Name = mailBilgi.SmtpKullaniciAdi;
            var Sifre = mailBilgi.SmtpSifre;
            var Port = mailBilgi.SmtpPortAdresi;
            var Host = mailBilgi.SmtpHost;
            var SSL = mailBilgi.SmtpSSL;

            using (var ePosta = new MailMessage())
            {
                ePosta.From = new MailAddress(EmailAdresi, Name, System.Text.Encoding.UTF8);
                ePosta.IsBodyHtml = true;
                foreach (var item in EMails)
                {
                    //item.Value == true ? TO: CC;
                    if (item.ToOrBcc) ePosta.To.Add(item.EMail);
                    else ePosta.Bcc.Add(item.EMail);
                }
                ePosta.Subject = Konu;
                ePosta.Body = Icerik;  
                ePosta.BodyEncoding = System.Text.Encoding.UTF8;
                ePosta.Priority = MailPriority.High;
                if (Attachs != null)
                    foreach (var item in Attachs)
                        ePosta.Attachments.Add(item);
                using (var smtp = new SmtpClient())
                {
                    smtp.Credentials = new System.Net.NetworkCredential(EmailAdresi, Sifre);
                    smtp.Port = Port.ToInt().Value;
                    smtp.Host = Host;
                    smtp.EnableSsl = SSL;
                    smtp.Timeout = 5 * 60 * 1000;
                    smtp.Send(ePosta);
                }

            }
            return true;


        }

    }



    public class MailContentDetail
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string HtmlContent { get; set; }
        public List<string> AddMailList { get; set; }
        public MailContentDetail()
        {
            AddMailList = new List<string>();
        }
    }
    public class MailReplaceParameterModel
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsLink { get; set; }
    }
    public static class SystemMails
    {
        public static MailContentDetail GetSystemMailContent(string EnstituAdi, string SablonHtml, string SablonAdi,  List<MailReplaceParameterModel> RPModel)
        {
            RPModel = RPModel ?? new List<MailReplaceParameterModel>();
            var model = new MailContentDetail();
            model.Title = SablonAdi;
            model.HtmlContent = SablonHtml;
            foreach (var itemRp in RPModel.Where(p => !p.Value.IsNullOrWhiteSpace()))
            {
                model.Title=model.Title.Replace("@" + itemRp.Key, (itemRp.IsLink ? "<a href='" + itemRp.Value + "' target='_blank'>" + itemRp.Value + "</a>" : itemRp.Value));
                model.HtmlContent = model.HtmlContent.Replace("@" + itemRp.Key, (itemRp.IsLink ? "<a href='" + itemRp.Value + "' target='_blank'>" + itemRp.Value + "</a>" : itemRp.Value));
            }
            model.Title = model.Title.Replace("{{", "{{_removeRw_");
            var titleStrList = model.Title.Split(new string[] { "{{", "}}" }, StringSplitOptions.None).ToList();

            foreach (var itemRp in RPModel.Where(p => p.Value.IsNullOrWhiteSpace()))
            {
                titleStrList = titleStrList.Where(p => (p.Contains("@" + itemRp.Key) && p.Contains("_removeRw_")) == false).ToList();
            }
            model.Title = string.Join("", titleStrList);


            model.HtmlContent = model.HtmlContent.Replace("{{", "{{_removeRw_"); 

            var contentStrList = model.HtmlContent.Split(new string[] { "{{", "}}" }, StringSplitOptions.None).ToList();

            foreach (var itemRp in RPModel.Where(p => p.Value.IsNullOrWhiteSpace()))
            {
                contentStrList = contentStrList.Where(p => (p.Contains("@" + itemRp.Key) && p.Contains("_removeRw_")) == false).ToList();
            }
            model.HtmlContent = string.Join("", contentStrList);

            var mmmC = new mdlMailMainContent(); 
            mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
            mmmC.EnstituAdi = EnstituAdi;
            mmmC.LogoPath = "https://lisansustu.yildiz.edu.tr/Content/assets/images/ytu_logo_tr.png";
            mmmC.Content = model.HtmlContent.Replace("_removeRw_", "");
            model.HtmlContent = Management.RenderPartialView("Ajax", "getMailContent", mmmC);


            return model;

        }
    }



}