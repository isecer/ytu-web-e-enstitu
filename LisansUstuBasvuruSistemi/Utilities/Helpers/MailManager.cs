using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{

    public class MailMainContentDto
    {
        public string LogoPath { get; set; }
        public string UniversiteAdi { get; set; }
        public string EnstituAdi { get; set; }
        public string Content { get; set; }

    }
    public class MailTableContentDto
    {
        public bool IsJuriBilgi { get; set; }
        public string AciklamaBasligi { get; set; }
        public string AciklamaDetayi { get; set; }
        public bool AciklamaTextAlingCenter { get; set; }
        public string GrupBasligi { get; set; }
        public int CaptTdWidth { get; set; }
        public List<MailTableRowDto> Detaylar { get; set; }
        public bool Success { get; set; }
        public MailTableContentDto()
        {
            CaptTdWidth = 200;
            Detaylar = new List<MailTableRowDto>();
            AciklamaTextAlingCenter = false;
        }

    }
    public class MailTableRowDto
    {
        public bool Colspan2 { get; set; }
        public int SiraNo { get; set; }
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
        public MailTableRowDto()
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
        public static Exception SendMailRetVal(string enstituKod, string konu, string icerik, string eMail, List<Attachment> attach, bool toOrBcc = true)
        {
            Exception exRet = null;

            try
            {
                MailManager.SendMail(enstituKod, konu, icerik, eMail, attach, toOrBcc);

            }
            catch (Exception ex)
            {
                exRet = ex;
            }

            return exRet;
        }
        public static Exception SendMailRetVal(string enstituKod, string konu, string icerik, List<MailSendList> eMails, List<Attachment> attach)
        {
            Exception exRet = null;

            try
            {
                MailManager.SendMail(enstituKod, konu, icerik, eMails, attach);

            }
            catch (Exception ex)
            {
                exRet = ex;
            }

            return exRet;
        }
        public static void SendMail(int gonderilenMailId, string konu, string icerik, List<MailSendList> eMails, List<Attachment> attachs)
        {

            #region sendMail
            var uid = UserIdentity.Current.Id;
            var uIp = UserIdentity.Ip;
            new System.Threading.Thread(() =>
            {
                try
                {
                    using (var dbb = new LisansustuBasvuruSistemiEntities())
                    {
                        var qeklenen = dbb.GonderilenMaillers.First(p => p.GonderilenMailID == gonderilenMailId);
                        try
                        {
                            MailManager.SendMail(qeklenen.EnstituKod, konu, icerik, eMails, attachs);
                            qeklenen.Gonderildi = true;
                            qeklenen.IslemTarihi = DateTime.Now;
                        }
                        catch (Exception ex)
                        {
                            qeklenen.HataMesaji = ex.ToExceptionMessage();
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderim işlemi yapılamadı! Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogType.Hata, uid, uIp);
                        }
                        dbb.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderim işlemi sırasında bir hata oluştu! Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogType.Hata, uid, uIp);
                }
            }).Start();

            #endregion
        }
        public static bool SendMail(string enstituKod, string konu, string icerik, string eMail, List<Attachment> attachs, bool toOrBcc = true)
        {
            return SendMail(enstituKod, konu, icerik,
                new List<MailSendList> { new MailSendList { EMail = eMail, ToOrBcc = toOrBcc } }, attachs);

        }
        public static bool SendMail(string enstituKod, string konu, string icerik, List<MailSendList> eMails, List<Attachment> attachs)
        {

            var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(enstituKod);
            var emailAdresi = mailBilgi.SmtpMailAdresi;
            var name = mailBilgi.SmtpKullaniciAdi;
            var sifre = mailBilgi.SmtpSifre;
            var port = mailBilgi.SmtpPortAdresi;
            var host = mailBilgi.SmtpHost;
            var ssl = mailBilgi.SmtpSSL;

            using (var ePosta = new MailMessage())
            {
                ePosta.From = new MailAddress(emailAdresi, name, System.Text.Encoding.UTF8);
                ePosta.IsBodyHtml = true;
                
                foreach (var item in eMails)
                {
                    //item.Value == true ? TO: CC;
                    if (!mailBilgi.TestEmailAddress.IsNullOrWhiteSpace()) item.EMail = mailBilgi.TestEmailAddress;
                    if (item.ToOrBcc) ePosta.To.Add(item.EMail);
                    else ePosta.Bcc.Add(item.EMail);
                }
                ePosta.Subject = konu;
                ePosta.Body = icerik;
                ePosta.BodyEncoding = System.Text.Encoding.UTF8;
                ePosta.Priority = MailPriority.High;
                if (attachs != null)
                    foreach (var item in attachs)
                        ePosta.Attachments.Add(item);
                using (var smtp = new SmtpClient())
                {
                    smtp.Credentials = new System.Net.NetworkCredential(emailAdresi, sifre);
                    smtp.Port = port.ToInt(587);
                    smtp.Host = host;
                    smtp.EnableSsl = ssl;
                    smtp.Timeout = 5 * 60 * 1000;
                    smtp.Send(ePosta);
                }

            }
            return true;


        }

    }



    public class MailContentDetailDto
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string HtmlContent { get; set; }
        public List<string> AddMailList { get; set; }
        public MailContentDetailDto()
        {
            AddMailList = new List<string>();
        }
    }
    public class MailReplaceParameterDto
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsLink { get; set; }
    }
    public static class SystemMails
    {
        public static MailContentDetailDto GetSystemMailContent(string enstituAdi, string sablonHtml, string sablonAdi, List<MailReplaceParameterDto> rpModel)
        {
            rpModel = rpModel ?? new List<MailReplaceParameterDto>();
            var model = new MailContentDetailDto
            {
                Title = sablonAdi,
                HtmlContent = sablonHtml
            };
            foreach (var itemRp in rpModel.Where(p => !p.Value.IsNullOrWhiteSpace()))
            {
                model.Title = model.Title.Replace("@" + itemRp.Key, (itemRp.IsLink ? "<a href='" + itemRp.Value + "' target='_blank'>" + itemRp.Value + "</a>" : itemRp.Value));
                model.HtmlContent = model.HtmlContent.Replace("@" + itemRp.Key, (itemRp.IsLink ? "<a href='" + itemRp.Value + "' target='_blank'>" + itemRp.Value + "</a>" : itemRp.Value));
            }
            model.Title = model.Title.Replace("{{", "{{_removeRw_");
            var titleStrList = model.Title.Split(new string[] { "{{", "}}" }, StringSplitOptions.None).ToList();

            foreach (var itemRp in rpModel.Where(p => p.Value.IsNullOrWhiteSpace()))
            {
                titleStrList = titleStrList.Where(p => (p.Contains("@" + itemRp.Key) && p.Contains("_removeRw_")) == false).ToList();
            }
            model.Title = string.Join("", titleStrList);


            model.HtmlContent = model.HtmlContent.Replace("{{", "{{_removeRw_");

            var contentStrList = model.HtmlContent.Split(new string[] { "{{", "}}" }, StringSplitOptions.None).ToList();

            foreach (var itemRp in rpModel.Where(p => p.Value.IsNullOrWhiteSpace()))
            {
                contentStrList = contentStrList.Where(p => (p.Contains("@" + itemRp.Key) && p.Contains("_removeRw_")) == false).ToList();
            }
            model.HtmlContent = string.Join("", contentStrList);

            var mmmC = new MailMainContentDto
            {
                UniversiteAdi = "Yıldız Teknik Üniversitesi",
                EnstituAdi = enstituAdi,
                LogoPath = "https://lisansustu.yildiz.edu.tr/Content/assets/images/ytu_logo_tr.png",
                Content = model.HtmlContent.Replace("_removeRw_", "")
            };
            model.HtmlContent = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);


            return model;

        }
    }



}