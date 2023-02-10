using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class RprModelProgramDilBilgi
    {
        public string SinavAdi { get; set; }
        public List<KrSinavTipleriOtNotAraliklari> OgretimNotKriterleri { get; set; }
        public List<SinavNotlari> SinavNotlariList { get; set; }
        public List<SinavTiplerSubSinavAralik> SinavNotAralikList { get; set; }
        public RprModelProgramDilBilgi()
        {
            OgretimNotKriterleri = new List<KrSinavTipleriOtNotAraliklari>();
            SinavNotlariList = new List<SinavNotlari>();
            SinavNotAralikList = new List<SinavTiplerSubSinavAralik>();
        }
    }
}