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
        public string OgrenciNo { get; set; }
        public string OgrenciBilgi { get; set; }
        public string DanismanAdSoyad { get; set; }
        public string TezDili { get; set; }
        public string TezBaslikTr { get; set; }
        public string TezBaslikEn { get; set; }
        public string YeniTezDanismaniAdSoyad { get; set; }
        public string YeniTezDili { get; set; }
        public string YeniTezBaslikTr { get; set; }
        public string YeniTezBaslikEn { get; set; }
        public string EYKYaHazirlandiAciklamasi { get; set; }
    }

    
}