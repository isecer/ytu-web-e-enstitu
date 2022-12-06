using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.RmDtos
{
    public class RprModelProgramDilBilgi
    {
        public string SinavAdi { get; set; }
        public List<krSinavTipleriOTNotAraliklari> OgretimNotKriterleri { get; set; }
        public List<SinavNotlari> SinavNotlariList { get; set; }
        public List<SinavTiplerSubSinavAralik> SinavNotAralikList { get; set; }
        public RprModelProgramDilBilgi()
        {
            OgretimNotKriterleri = new List<krSinavTipleriOTNotAraliklari>();
            SinavNotlariList = new List<SinavNotlari>();
            SinavNotAralikList = new List<SinavTiplerSubSinavAralik>();
        }
    }
}