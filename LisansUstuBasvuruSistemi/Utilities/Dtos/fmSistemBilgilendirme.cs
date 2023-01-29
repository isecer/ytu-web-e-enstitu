using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmSistemBilgilendirme : PagerOption
    {
        public Nullable<byte> BilgiTipi { get; set; }
        public string Kategori { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public Nullable<System.DateTime> IslemZamani { get; set; }
        public string IpAdresi { get; set; }
        public string AdSoyad { get; set; }

        public IEnumerable<frSistemBilgilendirme> data { get; set; }

    }
    public class frSistemBilgilendirme
    {
        public int SistemBilgiID { get; set; }
        public Nullable<byte> BilgiTipi { get; set; }
        public string Kategori { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public Nullable<System.DateTime> IslemZamani { get; set; }
        public int? IslemYapanID { get; set; }
        public string IpAdresi { get; set; }
        public string AdSoyad { get; set; }
        public string KullaniciAdi { get; set; }
    }
}