using System;
using System.Collections.Generic;

namespace LisansUstuBasvuruSistemi.Models.YokApi
{

 

    public class _1
    {
        public string DersGosterimKodu { get; set; }
        public string DersAd { get; set; }
        public double Sonuc { get; set; }
        public string Aciklama { get; set; }
    }

    public class YDSonuclar
    {
        public List<_1> _1 { get; set; }
    }
    public class YDSinavlar
    {
        public int ID { get; set; }
        public string Ad { get; set; }
        public string Tarih { get; set; }
        public DateTime? SinavTarihi { get; set; }
        public string DersAd { get; set; }
        public double? Sonuc { get; set; }
        public string Aciklama { get; set; }
    }
}