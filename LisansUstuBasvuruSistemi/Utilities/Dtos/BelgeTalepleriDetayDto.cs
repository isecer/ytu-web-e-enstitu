using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class BelgeTalepleriDetayDto : BelgeTalepleri
    {
        public int? KullaniciID { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string ResimAdi { get; set; }
        public string ClassName { get; set; }
        public string Color { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string DonemAdi { get; set; }
        public string DurumAdi { get; set; }
        public string DurumListeAdi { get; set; }
        public string BelgeTipAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public int SeciliDonemdeVerilenMiktar { get; set; }
        public int SeciliDonemdehenuzVerilmeyenMiktar { get; set; }
        public int DonemdeAlinabilecekToplamMiktar { get; set; }
        public bool DonemlikKotaVar { get; set; }
        public bool Edit { get; set; }
    }
}