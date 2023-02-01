using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class SablonMailModel
    {
        public int MailSablonTipID { get; set; }
        public MailSablonlari Sablon { get; set; }
        public List<string> SablonParametreleri { get; set; }
        public List<MailSendList> EMails { get; set; }
        public List<System.Net.Mail.Attachment> Attachments { get; set; }
        public Guid? UniqueID { get; set; }
        public string AdSoyad { get; set; }
        public string UniversiteAdi { get; set; }
        public string UnvanAdi { get; set; }
        public string ProgramAdi { get; set; }
        public bool IsAsilOrYedek { get; set; }
        public string JuriTipAdi { get; set; }
        public int? MezuniyetJuriOneriFormuJuriID { get; set; }
        public int? TIBasvuruAraRaporKomiteID { get; set; }

        public SablonMailModel()
        {
            EMails = new List<MailSendList>();
            SablonParametreleri = new List<string>();
            Attachments = new List<System.Net.Mail.Attachment>();
        }
    }
}