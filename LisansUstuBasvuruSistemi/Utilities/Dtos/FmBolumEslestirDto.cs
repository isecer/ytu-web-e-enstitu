using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmBolumEslestirDto : PagerOption
    {
        public string EnstituKod { get; set; }
        public string ProgramAdi { get; set; }
        public IEnumerable<FrBolumEslestirDto> BolumEslestirDtos { get; set; }

    }
    public class FrBolumEslestirDto : frProgramlar
    {
        public List<string> OgrenciBolumAdlari { get; set; }
    }
}