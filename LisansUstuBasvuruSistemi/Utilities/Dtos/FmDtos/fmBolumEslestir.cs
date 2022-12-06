using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmBolumEslestir : PagerOption
    {
        public string EnstituKod { get; set; }
        public string ProgramAdi { get; set; }
        public IEnumerable<frBolumEslestir> data { get; set; }

    }
    public class frBolumEslestir : frProgramlar
    {
        public List<string> OgrenciBolumAdlari { get; set; }
    }
}