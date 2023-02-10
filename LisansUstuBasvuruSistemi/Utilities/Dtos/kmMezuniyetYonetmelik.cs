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

        public List<string> MezuniyetYayinTurIDs { get; set; }
        public List<string> IsGecerlis { get; set; }
        public List<string> IsZorunlus { get; set; }
        public List<string> GrupKodus { get; set; }
        public List<string> IsVeOrVeyas { get; set; }


        public IEnumerable<KrMezuniyetYonetmelikOt> KrMezuniyetYonetmelikOt { get; set; }
        public KmMezuniyetYonetmelik()
        {
            KrMezuniyetYonetmelikOt = new List<KrMezuniyetYonetmelikOt>();
            MezuniyetYayinTurIDs = new List<string>();
            IsGecerlis = new List<string>();
            IsZorunlus = new List<string>();
            GrupKodus = new List<string>();
            IsVeOrVeyas = new List<string>();
        }
    }
    public class KrMezuniyetYonetmelikOt : MezuniyetYonetmelikleriOT
    {
        public bool? Success { get; set; }
        public string EnstituKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string MezuniyetYayinTurAdi { get; set; }
    }
}