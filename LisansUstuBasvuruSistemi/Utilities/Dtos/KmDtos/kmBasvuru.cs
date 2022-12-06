using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.KmDtos
{
    public class kmBasvuru : Basvurular
    {
        public bool Onaylandi { get; set; }
        public bool sbmtForm { get; set; }
        public int StepNo { get; set; }
        public int? SetSelectedStep { get; set; }
        public int TercihSayisi { get; set; }
        public bool ODurumIstensin { get; set; }
        public bool KotaValid { get; set; }
        public bool AlesIstensinmi { get; set; }
        public bool LEgitimDiliIstensinMi { get; set; }
        public bool YLEgitimDiliIstensinMi { get; set; }
        public bool TomerIstensinmi { get; set; }
        public bool DilIstensinmi { get; set; }
        public bool IsYerli { get; set; }
        public bool YLDurum { get; set; }
        public int OgrenimTipKod { get; set; }
        public string ProgramKod { get; set; }
        public string EnstituKod { get; set; }
        public string KullaniciTipAdi { get; set; }
        public List<BasvurularSinavBilgi> BasvurularSinavBilgi { get; set; }
        public BasvurularSinavBilgi BasvurularSinavBilgi_A { get; set; }
        public BasvurularSinavBilgi BasvurularSinavBilgi_D { get; set; }
        public BasvurularSinavBilgi BasvurularSinavBilgi_T { get; set; }
        public List<string> _UniqueID { get; set; }
        public List<int> _tSiraNo { get; set; }
        public List<int> _OgrenimTipKod { get; set; }
        public List<string> _ProgramKod { get; set; }
        public List<bool> _Ingilizce { get; set; }
        public List<bool> _YLBilgiIste { get; set; }
        public List<int> _AlanTipID { get; set; }

        public string DonemAdi { get; set; }

        public List<basvuruTercihModel> BasvuruTercihleri { get; set; }
        public kmBasvuru()
        {
            _UniqueID = new List<string>();
            _tSiraNo = new List<int>();
            _OgrenimTipKod = new List<int>();
            _ProgramKod = new List<string>();
            _Ingilizce = new List<bool>();
            _YLBilgiIste = new List<bool>();
            _AlanTipID = new List<int>();
            BasvuruTercihleri = new List<basvuruTercihModel>();
            BasvurularSinavBilgi = new List<BasvurularSinavBilgi>();
        }
    }
    public class basvuruTercihModel : BasvurularTercihleri
    {
        public string AlesTipAdi { get; set; }
        public string AlanTipAdi { get; set; }
        public bool Ingilizce { get; set; }
        public bool YlBilgiIste { get; set; }
        public bool IsLagnoOrYlAgnoAlinsin { get; set; }
        public bool? IsAsilOrYedek { get; set; }
        public kontenjanProgramBilgiModel ProgramBilgileri { get; set; }
    }
}