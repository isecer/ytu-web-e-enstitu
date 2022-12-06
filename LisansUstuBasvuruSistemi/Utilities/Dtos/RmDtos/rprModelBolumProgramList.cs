using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.RmDtos
{
    public class rprModelBolumProgramList
    {
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string ProgramKodu { get; set; }
        public string ProgramAdi { get; set; }
        public string EgitimDili { get; set; }
    }
}