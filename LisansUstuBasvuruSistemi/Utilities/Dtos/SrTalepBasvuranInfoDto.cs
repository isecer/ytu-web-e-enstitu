using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class SrTalepBasvuranInfoDto
    {
        public string AdSoyad { get; set; }
        public string OgrenciNo { get; set; }
        public string ProgramAdi { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public bool ShowSaveButon { get; set; }
    }
}