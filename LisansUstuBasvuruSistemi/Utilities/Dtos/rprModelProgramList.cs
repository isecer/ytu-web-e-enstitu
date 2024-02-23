using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class RprModelProgramList
    {
        public string AnabilimDaliAdi { get; set; }
        public string EnstituKod { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public string EgitimDili { get; set; }
        public List<rprModelProgramDilBilgi> ProgramDilBilgi { get; set; }
        public List<rprModelProgramAgnoKriterBilgi> AgnoKriterBilgi { get; set; }
        public List<CmbIntDto> EslestirilenBolumler { get; set; }
        public RprModelProgramList()
        {
            ProgramDilBilgi = new List<rprModelProgramDilBilgi>();
            AgnoKriterBilgi = new List<rprModelProgramAgnoKriterBilgi>();
            EslestirilenBolumler = new List<CmbIntDto>();
        }


    }
    public class rprModelProgramDilBilgi
    {
        public string SinavAdi { get; set; }
        public List<KrSinavTipleriOtNotAraliklari> OgretimNotKriterleri { get; set; }
        public List<SinavNotlari> SinavNotlariList { get; set; }
        public List<SinavTiplerSubSinavAralik> SinavNotAralikList { get; set; }
        public rprModelProgramDilBilgi()
        {
            OgretimNotKriterleri = new List<KrSinavTipleriOtNotAraliklari>();
            SinavNotlariList = new List<SinavNotlari>();
            SinavNotAralikList = new List<SinavTiplerSubSinavAralik>();
        }
    }
    public class rprModelProgramAgnoKriterBilgi
    {
        public string AgnoAlimTipi { get; set; }
        public List<CmbStringDto> OgretimKriterleri { get; set; }
        public rprModelProgramAgnoKriterBilgi()
        {
            OgretimKriterleri = new List<CmbStringDto>();
        }
    }
}