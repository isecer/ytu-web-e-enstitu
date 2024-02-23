using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmTiBasvuru : TIBasvuru
    {
        public string OgrenimTipAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
    }
}