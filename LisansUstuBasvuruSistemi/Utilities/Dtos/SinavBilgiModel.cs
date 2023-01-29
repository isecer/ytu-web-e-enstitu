using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class SinavBilgiModel : BasvuruSurecSinavTipleri
    {
        public List<SinavDilleri> BsSinavDilleri { get; set; }
        public string SinavDilleriStr { get; set; }
        public List<krSinavTipleriDonems> SinavTipleriDonems { get; set; }
        public string SinavAdi { get; set; }
        public BasvurularSinavBilgi BasvuruSinavData { get; set; }
        public bool IsTurkceProgramVar { get; set; }
        public bool IsEgitimDiliTurkce { get; set; }
        public string MinNotAdi { get; set; }
        public string MaxNotAdi { get; set; }
        public SinavBilgiModel()
        {
            BsSinavDilleri = new List<SinavDilleri>();
            SinavTipleriDonems = new List<krSinavTipleriDonems>();
        }

    }
    
}