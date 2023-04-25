using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class kmBasvuruSurec : BasvuruSurec
    {
        public string OgretimYili { get; set; }

        public List<int> gID { get; set; }
        public List<string> BasvuruSurecOtoMailID { get; set; }
        public List<string> ZamanTipID { get; set; }
        public List<string> Zaman { get; set; }

        public List<int> OgrenimTipKod { get; set; }
        public List<int> OgrenimTipKods { get; set; }
        public List<int> Kota { get; set; }
        public List<double> BasariNotOrtalamasi { get; set; }
        public List<string> SeciliOgrenimTipKod { get; set; }
        public List<int> BasvuruSurecOgrenimTipID { get; set; }
        public List<string> MulakatSurecineGirecek { get; set; }
        public List<string> AlanIciBilimselHazirlik { get; set; }
        public List<string> AlanDisiBilimselHazirlik { get; set; }


        public List<int> MulakatSinavTurID { get; set; }
        public List<int> MulakatSinavTurIDSecilen { get; set; }
        public List<int?> YuzdeOran { get; set; }
        public List<MulakatSturModel> MulakatSTurModel { get; set; }

        public kmBasvuruSurecOgrenimTipModel OgrenimTipModel { get; set; }

        public List<DateTime?> AsilBasTar { get; set; }
        public List<DateTime?> AsilBitTar { get; set; }
        public List<DateTime?> YedekBasTar { get; set; }
        public List<DateTime?> YedekBitTar { get; set; }


        public kmBasvuruSurec()
        {
            gID = new List<int>();
            BasvuruSurecOtoMailID = new List<string>();
            ZamanTipID = new List<string>();
            Zaman = new List<string>();
            OgrenimTipKod = new List<int>();
            OgrenimTipKods = new List<int>();
            Kota = new List<int>();
            BasariNotOrtalamasi = new List<double>();
            BasvuruSurecOgrenimTipID = new List<int>();
            MulakatSurecineGirecek = new List<string>();
            AlanIciBilimselHazirlik = new List<string>();
            AlanDisiBilimselHazirlik = new List<string>();
            MulakatSinavTurID = new List<int>();
            MulakatSinavTurIDSecilen = new List<int>();
            YuzdeOran = new List<int?>();


            OgrenimTipModel = new kmBasvuruSurecOgrenimTipModel();

            AsilBasTar = new List<DateTime?>();
            AsilBitTar = new List<DateTime?>();
            YedekBasTar = new List<DateTime?>();
            YedekBitTar = new List<DateTime?>();
        }
    }
    public class kmBasvuruSurecOgrenimTipModel
    {
        public bool IsBelgeYuklemeVar { get; set; }
        public string EnstituKod { get; set; }
        public int BasvuruSurecID { get; set; }
        public List<CmbIntDto> EnstituOgrenimTipleri = new List<CmbIntDto>();
        public IEnumerable<CheckObject<KrOgrenimTip>> OgrenimTipleriDataList { get; set; }
    }
    public class MulakatSturModel : MulakatSinavTurleri
    {
        public bool? Success { get; set; }
        public int IndexNo { get; set; }
        public string SinavTurAdi { get; set; }
        public int? YuzdeOran { get; set; }
        public bool Zorunlu { get; set; }
    }

}