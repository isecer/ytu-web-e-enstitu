using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class RprBasvuruSonucBolumModel
    {
        public string ProgramAdi { get; set; }
        public string BolumAdi { get; set; }
        public List<RprBasvuruSonucModel> ProgramB { get; set; }
        public List<KrMulakatDetay> MulakatDetayB { get; set; }
        public List<RwMulakatJuri> MulakatJuriB { get; set; }
        public List<RwMulakatJuri> k2 { get; set; }
        public RprBasvuruSonucBolumModel()
        {
            k2 = new List<RwMulakatJuri>();
        }
    }
}