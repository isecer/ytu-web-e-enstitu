using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class mailSRjuriModel : SRTalepleri
    {
        public string DilKodu { get; set; }
        public string EgitimOgretimYili { get; set; }
        public string UniversiteAdi { get; set; }
        public string EnstituAdi { get; set; }
        public string AdSoyad { get; set; }
        public string ProgramAdi { get; set; }
        public string OgrenciAdi { get; set; }
        public string OgrenciNo { get; set; }
        public string Yer { get; set; }
        public string Tarih { get; set; }
        public string Saat { get; set; }
        public string WebAdresi { get; set; }
        public string Email { get; set; }
    }
}