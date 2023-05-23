using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class RprTdoEykDto
    {
        public string Title { get; set; } 
        public int SiraNo { get; set; }
        public bool IsDoktora { get; set; }
        public int DegisiklikTipID { get; set; }
        public int TDODanismanTalepTipID { get; set; }
        public string Aciklama { get; set; }
        public string OgrenciBilgi { get; set; }
        public string VarolanDanismanAdSoyad { get; set; }
        public string YeniTezDanismaniAdSoyad { get; set; }
        public string VarolanTezDili { get; set; }
        public string YeniTezDili { get; set; }
        public string VarolanTezBaslikTr { get; set; }
        public string VarolanTezBaslikEn { get; set; }
        public string YeniTezBaslikTr { get; set; }
        public string YeniTezBaslikEn { get; set; }
    }

    
}