using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class kmMezuniyetYonetmelik : MezuniyetYonetmelikleri
    {
        public string EnstituAdi { get; set; }
        public string OgretimYili { get; set; }
        public string OgretimYiliB { get; set; }
        public string TarihKriterAdi { get; set; }
        public string IslemYapan { get; set; }

        public List<string> _MezuniyetYayinTurID { get; set; }
        public List<string> _IsGecerli { get; set; }
        public List<string> _IsZorunlu { get; set; }
        public List<string> _GrupKodu { get; set; }
        public List<string> _IsVeOrVeya { get; set; }


        public IEnumerable<krMezuniyetYonetmelikOT> krMezuniyetYonetmelikOT { get; set; }
        public kmMezuniyetYonetmelik()
        {
            krMezuniyetYonetmelikOT = new List<krMezuniyetYonetmelikOT>();
            _MezuniyetYayinTurID = new List<string>();
            _IsGecerli = new List<string>();
            _IsZorunlu = new List<string>();
            _GrupKodu = new List<string>();
            _IsVeOrVeya = new List<string>();
        }
    }
    public class krMezuniyetYonetmelikOT : MezuniyetYonetmelikleriOT
    {
        public bool? Success { get; set; }
        public string EnstituKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string MezuniyetYayinTurAdi { get; set; }
    }
}