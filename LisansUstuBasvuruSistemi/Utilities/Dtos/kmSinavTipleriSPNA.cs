using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmSinavTipleriSpna : SinavTipleriOT_SNA
    {

        public List<int> NAOgrenimTipKod { get; set; }
        public List<bool> NAIngilizce { get; set; }
        public List<int> NAIsGecerli { get; set; }
        public List<int> NAIsIstensin { get; set; }
        public List<double?> NAMin { get; set; }
        public List<double?> NAMax { get; set; }
        public List<string> IPProgramKod { get; set; }

        public List<KrSinavTipleriOtNotAraliklari> KrSinavTipleriOtNotAraliklaris { get; set; }
        public KmSinavTipleriSpna()
        {
            IPProgramKod = new List<string>();
            KrSinavTipleriOtNotAraliklaris = new List<KrSinavTipleriOtNotAraliklari>();

        }
    }
    public class KrSinavTipleriOtNotAraliklari : SinavTipleriOTNotAraliklari
    {
        public string OgrenimTipAdi { get; set; }
        public bool? SuccessRow { get; set; }
        public List<string> PropName { get; set; }
        public List<string> ProgramKods { get; set; }
        public List<CmbStringDto> IstenmeyenProgramlar { get; set; }
        public KrSinavTipleriOtNotAraliklari()
        {
            PropName = new List<string>();
            ProgramKods = new List<string>();
            IstenmeyenProgramlar = new List<CmbStringDto>();
        }
    }
}