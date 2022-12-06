using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using LisansUstuBasvuruSistemi.Utilities.Dtos.CmbDtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.RmDtos
{
    public class rprModelProgramList
    {
        public string AnabilimDaliAdi { get; set; }
        public string EnstituKod { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public string EgitimDili { get; set; }
        public List<rprModelProgramDilBilgi> ProgramDilBilgi { get; set; }
        public List<rprModelProgramAgnoKriterBilgi> AgnoKriterBilgi { get; set; }
        public List<CmbIntDto> EslestirilenBolumler { get; set; }
        public rprModelProgramList()
        {
            ProgramDilBilgi = new List<rprModelProgramDilBilgi>();
            AgnoKriterBilgi = new List<rprModelProgramAgnoKriterBilgi>();
            EslestirilenBolumler = new List<CmbIntDto>();
        }


    }
}