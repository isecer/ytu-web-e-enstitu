using System;
using System.Collections.Generic;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmSistemBilgilendirme : PagerOption
    {
        public byte? BilgiTipi { get; set; }
        public string Kategori { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public DateTime? IslemZamani { get; set; }
        public string IpAdresi { get; set; }
        public string AdSoyad { get; set; }

        public IEnumerable<FrSistemBilgilendirme> FrSistemBilgilendirmes { get; set; }

    }
    public class FrSistemBilgilendirme
    {
        public int SistemBilgiID { get; set; }
        public byte? BilgiTipi { get; set; }
        public string Kategori { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public DateTime? IslemZamani { get; set; }
        public int? IslemYapanID { get; set; }
        public string IpAdresi { get; set; }
        public string AdSoyad { get; set; }
        public string KullaniciAdi { get; set; }
    }
}