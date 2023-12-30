using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmMezuniyetYonetmelik : MezuniyetYonetmelikleri
    {
        public string EnstituAdi { get; set; }
        public string OgretimYili { get; set; }
        public string OgretimYiliB { get; set; }
        public string TarihKriterAdi { get; set; }
        public string IslemYapan { get; set; }

        public List<int>OgrenimTipKods{ get; set; } = new List<int>();
        public List<int> MezuniyetYayinTurIds { get; set; } = new List<int>();
        public List<bool?> IsGecerlis { get; set; } = new List<bool?>();
        public List<bool?> IsZorunlus { get; set; } = new List<bool?>();
        public List<string> GrupKodus { get; set; } = new List<string>();
        public List<bool?> IsVeOrVeyas { get; set; } = new List<bool?>();


        public IEnumerable<KrMezuniyetYonetmelikOt> KrMezuniyetYonetmelikOt { get; set; } = new List<KrMezuniyetYonetmelikOt>();
    }
    public class KrMezuniyetYonetmelikOt : MezuniyetYonetmelikleriOT
    {
        public bool? Success { get; set; }
        public string EnstituKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string MezuniyetYayinTurAdi { get; set; }
    }
}