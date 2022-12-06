using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmAnketler : PagerOption
    {
        public string EnstituKod { get; set; }
        public string AnketAdi { get; set; }
        public IEnumerable<frAnketler> Data { get; set; }
    }

    public class frAnketler : Anket
    {
        public string EnstituAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public int SoruSayisi { get; set; }
    }
    public class frAnketDetay : AnketSoru
    {
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public int SecenekSayisi { get; set; }
        public string Aciklama { get; set; }
        public string CevapHtml { get; set; }
        public List<frAnketSecenekDetay> frAnketSecenekDetay { get; set; }

        public List<AnketSeceneklerDetay> SecenekDetay { get; set; }
        public List<AnketTableDetay> TableDetay { get; set; }
        public frAnketDetay()
        {
            SecenekDetay = new List<AnketSeceneklerDetay>();
            TableDetay = new List<AnketTableDetay>();
            frAnketSecenekDetay = new List<frAnketSecenekDetay>();
        }
    }

    public class frAnketSecenekDetay : AnketSoruSecenek
    {
        public int Count { get; set; }

    }
    public class AnketTableDetay
    {
        public string SiraNo { get; set; }
        public string TabloVeri1 { get; set; }
        public string TabloVeri2 { get; set; }
        public string TabloVeri3 { get; set; }
        public string TabloVeri4 { get; set; }
    }
}