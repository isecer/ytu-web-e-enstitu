using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MailKullaniciBilgi
    {

        public bool Checked { get; set; }
        public int KullaniciID { get; set; }
        public string AdSoyad { get; set; }
        public string BirimAdi { get; set; }
        public string Email { get; set; }

    }
    public class KmMailGonder : GonderilenMailler
    {  
        public string Alici { get; set; }
        public string BccAlici { get; set; }
        public bool IsTopluMail { get; set; }
        public string SecilenTopluAlicilar { get; set; }  
        
        public List<string> SecilenAlicilars { get; set; }
        public List<CmbStringDto> EMails { get; set; }

        public KmMailGonder()
        {
            Aciklama = "";
            AciklamaHtml = "";
            SecilenTopluAlicilar = ""; 
            SecilenAlicilars = new List<string>(); 
            EMails = new List<CmbStringDto>(); 
        }
    }
}