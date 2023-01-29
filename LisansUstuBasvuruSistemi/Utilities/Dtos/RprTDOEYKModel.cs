using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class RprTDOEYKModel
    {
        public string OgrenciNo { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public string OgrenciAnabilimdaliProgram { get; set; }
        public string YL_DR { get; set; }
        public string DanismanAdSoyad { get; set; }
        public string DanismanAnabilimDali { get; set; }
        public string TezBaslikTr { get; set; }
        public string TezBaslikEn { get; set; }
        public string TezDili { get; set; }
        public int DanismanYukYlDrSayi { get; set; }
        public int MezunSayisi { get; set; }
    }
}