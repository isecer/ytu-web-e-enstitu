using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmTDOBasvuru : TDOBasvuru
    {
        public string AdSoyad { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
    }
}