using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading;
using BiskaUtil;
using HtmlAgilityPack;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.MailManager
{
    #region  MailDtos 
    public class EmailTemplateModel
    {
        public string CurrentMessage { get; set; }
        public string PreviousMessage { get; set; }
        public string ReplyUrl { get; set; }
    }
    public class MailMainContentDto
    {
        public string LogoPath { get; set; }
        public string UniversiteAdi { get; set; }
        public string EnstituAdi { get; set; }
        public string WebAdresi { get; set; }
        public string SistemErisimAdresi { get; set; }
        public string Content { get; set; }

    }
    public class MailTableContentDto
    {
        public bool IsJuriBilgi { get; set; }
        public string AciklamaBasligi { get; set; }
        public string AciklamaDetayi { get; set; }
        public bool AciklamaTextAlingCenter { get; set; } = false;
        public string GrupBasligi { get; set; }
        public int CaptTdWidth { get; set; } = 200;
        public List<MailTableRowDto> Detaylar { get; set; } = new List<MailTableRowDto>();
        public bool Success { get; set; }
    }
    public class MailTableRowDto
    {
        public bool Colspan2 { get; set; } = false;
        public int SiraNo { get; set; }
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
    }
    public class MailSendList
    {
        public int? KullaniciId { get; set; }
        public string EMail { get; set; }
        public bool ToOrBcc { get; set; }
    }

    public class MailContentDetailDto
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string HtmlContent { get; set; }
        public List<string> AddMailList { get; set; } = new List<string>();
    }
    public class MailParameterDto
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsLink { get; set; }
    }
    #endregion
    public static class MailManager
    {
        public static long AttachmentMaxFileSize => (25 * 1024 * 1024); //25Default MB 
        public static MailContentDetailDto CreateMailContentDetailModel(SablonMailModel mailItem)
        {
            var model = new MailContentDetailDto
            {

                Title = mailItem.Sablon.SablonAdi,
                HtmlContent = mailItem.Sablon.SablonHtml
            };



            model.Title = model.Title.Replace("{{", "{{_removeRw_");
            var titleStrList = model.Title.Split(new[] { "{{", "}}" }, StringSplitOptions.None).ToList();

            foreach (var itemRp in mailItem.MailParameterDtos.Where(p => p.Value.IsNullOrWhiteSpace()))
            {
                titleStrList = titleStrList.Where(p => (p.Contains("@" + itemRp.Key) && p.Contains("_removeRw_")) == false).ToList();
            }
            model.Title = string.Join("", titleStrList);

            model.HtmlContent = model.HtmlContent.Replace("{{", "{{_removeRw_");

            var contentStrList = model.HtmlContent.Split(new[] { "{{", "}}" }, StringSplitOptions.None).ToList();

            foreach (var itemRp in mailItem.MailParameterDtos.Where(p => p.Value.IsNullOrWhiteSpace()))
            {
                contentStrList = contentStrList.Where(p => (p.Contains("@" + itemRp.Key) && p.Contains("_removeRw_")) == false).ToList();
            }
            model.HtmlContent = string.Join("", contentStrList);
            foreach (var itemRp in mailItem.MailParameterDtos)
            {
                itemRp.Value = itemRp.Value ?? "";
                model.Title = model.Title.Replace("@" + itemRp.Key, (itemRp.IsLink ? "<a href='" + itemRp.Value + "' target='_blank'>" + itemRp.Value + "</a>" : itemRp.Value));
                model.HtmlContent = model.HtmlContent.Replace("@" + itemRp.Key, (itemRp.IsLink ? "<a href='" + itemRp.Value + "' target='_blank'>" + itemRp.Value + "</a>" : itemRp.Value));
            }
            var mmmC = new MailMainContentDto
            {
                UniversiteAdi = "Yıldız Teknik Üniversitesi",
                EnstituAdi = mailItem.EnstituAdi,
                LogoPath = "https://lisansustu.yildiz.edu.tr/Content/assets/images/ytu_logo_tr.png",
                Content = model.HtmlContent.Replace("_removeRw_", ""),
                WebAdresi = mailItem.WebAdresi,
                SistemErisimAdresi = mailItem.SistemErisimAdresi
            };
            model.HtmlContent = ViewRenderHelper.RenderPartialView("Ajax", "GetMailContent", mmmC);

            if (mailItem.SablonEkleri.Any())
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(model.HtmlContent);

                // Belirli bir elementin id bilgisine göre seçim
                var targetElement = doc.DocumentNode.SelectSingleNode("//td[@id='mSendContent']");

                // Element bulunursa ve içine eklenecek HTML'i belirle
                if (targetElement == null) return model;
                var yeniHtml = "<br/><div><strong><u>İlgili Ekler:</u></strong>";
                foreach (var itemEk in mailItem.SablonEkleri)
                { 
                    yeniHtml += "</br><a href='" + itemEk.EkDosyaYolu.CustomUrlContentMail(mailItem.SistemErisimAdresi) + "' target='_blank'>" + itemEk.EkAdi + "<a>";
                }
                yeniHtml += "</div></br>";

                // Yeni HTML'i ekleyerek hedef elementi güncelle
                doc.CreateTextNode(yeniHtml);
                targetElement.InnerHtml += yeniHtml;
                model.HtmlContent = doc.DocumentNode.OuterHtml;
            }

            return model;

        }
        public static Exception SendMailRetVal(string enstituKod, string konu, string icerik, string eMail, List<Attachment> attach, bool toOrBcc = true)
        {
            Exception exRet = null;

            try
            {
                SendMail(enstituKod, konu, icerik, eMail, attach, toOrBcc);

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
                SendMail(enstituKod, konu, icerik, eMails, attach);

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
            new Thread(() =>
            {
                try
                {
                    using (var entities = new LubsDbEntities())
                    {
                        var qeklenen = entities.GonderilenMaillers.First(p => p.GonderilenMailID == gonderilenMailId);
                        try
                        {
                            SendMail(qeklenen.EnstituKod, konu, icerik, eMails, attachs);
                            qeklenen.Gonderildi = true;
                            qeklenen.IslemTarihi = DateTime.Now;
                        }
                        catch (Exception ex)
                        {
                            qeklenen.HataMesaji = ex.ToExceptionMessage();
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderim işlemi yapılamadı! Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata, uid, uIp);
                        }
                        entities.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderim işlemi sırasında bir hata oluştu! Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata, uid, uIp);
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

            if (attachs != null && attachs.Any()) attachs = MailReportAttachment.CopyAttachments(attachs);
            var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(enstituKod);
            var emailAdresi = mailBilgi.SmtpMailAdresi;
            var name = mailBilgi.SmtpKullaniciAdi;
            var sifre = mailBilgi.SmtpSifre;
            var port = mailBilgi.SmtpPortAdresi;
            var host = mailBilgi.SmtpHost;
            var ssl = mailBilgi.SmtpSSL;

            using (var ePosta = new MailMessage())
            {
                ePosta.From = new MailAddress(emailAdresi, name, Encoding.UTF8);
                ePosta.IsBodyHtml = true;

                foreach (var item in eMails)
                {
                    if (!mailBilgi.TestEmailAddress.IsNullOrWhiteSpace()) item.EMail = mailBilgi.TestEmailAddress;
                    if (item.ToOrBcc) ePosta.To.Add(item.EMail);
                    else ePosta.Bcc.Add(item.EMail);
                }
                ePosta.Subject = konu;
                ePosta.Body = icerik;
                ePosta.BodyEncoding = Encoding.UTF8;
                ePosta.Priority = MailPriority.High;
                if (attachs != null)
                    foreach (var item in attachs)
                        ePosta.Attachments.Add(item);
                using (var smtp = new SmtpClient())
                {
                    smtp.Credentials = new NetworkCredential(emailAdresi, sifre);
                    smtp.Port = port.ToInt(587);
                    smtp.Host = host;
                    smtp.EnableSsl = ssl;
                    smtp.Timeout = 5 * 60 * 1000;
                    smtp.Send(ePosta);
                }

            }
            return true;


        }


        public static Attachment GetFileToAttachment(this FileAttachmentInfo fileAttachmentInfo)
        {


            var fullPath = fileAttachmentInfo.FilePath.FileBaseFullPath();
            if (File.Exists(fullPath))
            {
                fileAttachmentInfo.FileName = fileAttachmentInfo.FileName.IsNullOrWhiteSpace() ? Path.GetFileName(fullPath) : fileAttachmentInfo.FileName.ToSetNameFileExtension(Path.GetExtension(fullPath));
                return new Attachment(new MemoryStream(File.ReadAllBytes(fullPath)), fileAttachmentInfo.FileName, MediaTypeNames.Application.Octet);
            }
            SistemBilgilendirmeBus.SistemBilgisiKaydet("Dosya eki sistemde bulunamadı! <br/>Dosya Yolu:" + fullPath, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Uyarı, null, "::");
            return null;
        }
        public static List<Attachment> GetFileToAttachments(this List<FileAttachmentInfo> fileAttachmentInfos)
        {
            var attachments = new List<Attachment>();
            foreach (var fileAttachmentInfo in fileAttachmentInfos)
            {
                var attach = fileAttachmentInfo.GetFileToAttachment();
                if (attach != null) attachments.Add(attach);
            }
            return attachments;
        }
        public static List<MailSendList> ToSplitEmailSendList(this string emailsString, bool toOrBcc = false)
        {
            if (emailsString.IsNullOrWhiteSpace()) return new List<MailSendList>();
            return emailsString.Split(',').Where(p => !p.IsNullOrWhiteSpace())
                .Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = toOrBcc }).ToList();
        }

        //public static List<MailSablonlariEkleri> GetFileSizeControl(this List<MailSablonlariEkleri> files,
        //    List<Attachment> attachments)
        //{ 
        //    var fileSizes = files.Select((file, inx) => new { inx, file, fileSize = file.EkDosyaYolu.GetFileSize() })
        //        .OrderBy(o => o.fileSize).ToList();

        //    var attachmentsSize = attachments.Sum(s => s.ContentStream.Length);
        //    //Boyutuna göre sıralanmış dosyaları indexe göre önceki dosya boyutları ile ve attahc olarak eklenen doysa boyutu ile topla
        //    var fileMaxSizeSort = fileSizes.Select(s => new
        //    {
        //        s.file,
        //        s.fileSize,
        //        beforeIndexSumSize = attachmentsSize + fileSizes.Where(p => p.inx <= s.inx).Sum(sm => sm.fileSize)
        //    }).ToList();
        //    //Sıralamaya göre maximum dosya gönderme boyutu aşmayan dosyaları al
        //    return fileMaxSizeSort.Where(p => p.beforeIndexSumSize <= AttachmentMaxFileSize).Select(s => s.file).ToList();
        //}

    }







}