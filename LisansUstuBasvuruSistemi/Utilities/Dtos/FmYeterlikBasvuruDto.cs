using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmYeterlikBasvuruDto : PagerOption
    {
        public int? AktifYeterlikSurecId { get; set; }
        public string EnstituAdi { get; set; }
        public string DonemAdi { get; set; }
        public string AdSoyad { get; set; }
        public string OgrenciNo { get; set; }
        public bool IsYtuOgrencisi { get; set; }
        public bool IsEnstituYetki { get; set; }

        public List<FrYeterlikBasvuruDto> Data { get; set; }
    }

    public class FrYeterlikBasvuruDto : YeterlikBasvuru
    {
        public string AdSoyad { get; set; }
        public string ResimAdi { get; set; } 
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string DonemAdi { get; set; }
    }
     
}