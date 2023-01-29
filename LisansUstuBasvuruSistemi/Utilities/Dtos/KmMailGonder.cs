using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

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
        public int? BasvuruSurecID { get; set; }
        public bool IsBasvuruSonuc { get; set; }
        public string Alici { get; set; }
        public bool IsTopluMail { get; set; }
        public string SecilenTopluAlicilar { get; set; }
        public string BasvuruRowID { get; set; }
        public bool IsBolumOrOgrenci { get; set; }
        public bool IsToOrBCC { get; set; }
        public List<int> MulakatSonucTipIDs { get; set; }
        public List<int?> KayitDurumIDs { get; set; }
        public List<string> ProgramKods { get; set; }
        public List<int> OgrenimTipKods { get; set; }

        public List<string> SecilenAlicilars { get; set; }
        public List<CmbStringDto> EMails { get; set; }

        public KmMailGonder()
        {
            Aciklama = "";
            AciklamaHtml = "";
            SecilenTopluAlicilar = "";
            IsBolumOrOgrenci = false;
            ProgramKods = new List<string>();
            OgrenimTipKods = new List<int>();
            SecilenAlicilars = new List<string>();
            KayitDurumIDs = new List<int?>();
            EMails = new List<CmbStringDto>();
            MulakatSonucTipIDs = new List<int>();
        }
    }
}