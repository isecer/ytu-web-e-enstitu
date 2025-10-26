using System.Collections.Generic;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.WebServiceData.ObsRestData.Models;

namespace LisansUstuBasvuruSistemi.WebServiceData.ObsRestData
{
    public class FmObsProgramDto : PagerModel
    {
        public string FakulteKod { get; set; }
        public string BolumId { get; set; }
        public string ProgramId { get; set; }

        public string FakulteAd { get; set; }
        public string BolumAd { get; set; }
        public string ProgramAd { get; set; }
        public string ProgramKod { get; set; }

        public IEnumerable<ObsProgramFullDto> Programlar { get; set; }
    }
}