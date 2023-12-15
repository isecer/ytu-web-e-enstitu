using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public  class KmSrTalep : SRTalepleri
    { 
        public bool YetkisizErisim { get; set; }
        public string AdSoyad { get; set; }
        public string OgrenciNo { get; set; }
        public List<string> JuriAdi { get; set; }
        public List<string> Telefon { get; set; }
        public List<string> Email { get; set; }
        public new TimeSpan? BasSaat { get; set; }
        public new TimeSpan? BitSaat { get; set; }
        public string MzRowId { get; set; }
        public bool IsSalonSecilsin { get; set; }

        public KmSrTalep()
        {
            JuriAdi = new List<string>();
            Telefon = new List<string>();
            Email = new List<string>();
            SRTaleplerJuris = new List<SRTaleplerJuri>();
        }
    }
}