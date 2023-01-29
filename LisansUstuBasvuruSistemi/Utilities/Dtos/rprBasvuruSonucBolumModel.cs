using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class rprBasvuruSonucBolumModel
    {
        public string ProgramAdi { get; set; }
        public string BolumAdi { get; set; }
        public List<rprBasvuruSonucModel> ProgramB { get; set; }
        public List<krMulakatDetay> MulakatDetayB { get; set; }
        public List<rwMulakatJuri> MulakatJuriB { get; set; }
        public List<rwMulakatJuri> k2 { get; set; }
        public rprBasvuruSonucBolumModel()
        {
            k2 = new List<rwMulakatJuri>();
        }
    }
}