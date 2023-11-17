using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmSrTalep : SRTalepleri
    { 
        public bool YetkisizErisim { get; set; }
        public string AdSoyad { get; set; }
        public string OgrenciNo { get; set; }
        public string SicilNo { get; set; }
        public List<string> JuriAdi { get; set; }
        public List<string> Telefon { get; set; }
        public List<string> Email { get; set; }
        public TimeSpan? BasSaat { get; set; }
        public TimeSpan? BitSaat { get; set; }
        public string MzRowID { get; set; }
        public bool IsSalonSecilsin { get; set; }
        public DateTime? SonSrTarihi { get; set; }
        public KmSrTalep()
        {
            JuriAdi = new List<string>();
            Telefon = new List<string>();
            Email = new List<string>();
            this.SRTaleplerJuris = new List<SRTaleplerJuri>();
        }
    }
}