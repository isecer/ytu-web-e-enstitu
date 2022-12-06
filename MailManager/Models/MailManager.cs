using MailManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace LisansUstuBasvuruSistemi.Models
{

    public class MailContent
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string HtmlContent { get; set; }
        public List<string> AddMailList { get; set; }
        public MailContent(string EnstituAdi, string SablonHtml, string SablonAdi, List<MailReplaceParameterModel> RPModel = null)
        {
            RPModel = RPModel ?? new List<MailReplaceParameterModel>();

            this.Title = SablonAdi;
            this.HtmlContent = SablonHtml;
            foreach (var itemRp in RPModel.Where(p => p.Value != null && p.Value.Trim() != ""))
            {
                this.Title = this.Title.Replace("@" + itemRp.Key, (itemRp.IsLink ? "<a href='" + itemRp.Value + "' target='_blank'>" + itemRp.Value + "</a>" : itemRp.Value));
                this.HtmlContent = this.HtmlContent.Replace("@" + itemRp.Key, (itemRp.IsLink ? "<a href='" + itemRp.Value + "' target='_blank'>" + itemRp.Value + "</a>" : itemRp.Value));
            }
            this.Title = this.Title.Replace("{{", "{{_removeRw_");
            var titleStrList = this.Title.Split(new string[] { "{{", "}}" }, StringSplitOptions.None).ToList();

            foreach (var itemRp in RPModel.Where(p => p.Value == null || p.Value.Trim() == ""))
            {
                titleStrList = titleStrList.Where(p => (p.Contains("@" + itemRp.Key) && p.Contains("_removeRw_")) == false).ToList();
            }
            this.Title = string.Join("", titleStrList);


            this.HtmlContent = this.HtmlContent.Replace("{{", "{{_removeRw_");

            var contentStrList = this.HtmlContent.Split(new string[] { "{{", "}}" }, StringSplitOptions.None).ToList();

            foreach (var itemRp in RPModel.Where(p => p.Value == null || p.Value.Trim() == ""))
            {
                contentStrList = contentStrList.Where(p => (p.Contains("@" + itemRp.Key) && p.Contains("_removeRw_")) == false).ToList();
            }
            this.HtmlContent = string.Join("", contentStrList);

            var mmmC = new MainContentModel();
            mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
            mmmC.EnstituAdi = EnstituAdi;
            mmmC.LogoPath = "https://lisansustu.yildiz.edu.tr/Content/assets/images/ytu_logo_tr.png";
            mmmC.Content = this.HtmlContent.Replace("_removeRw_", "");
            this.HtmlContent = "";// Management.RenderPartialView("Ajax", "getMailContent", mmmC); 

        }
        public MailContent()
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
  



}