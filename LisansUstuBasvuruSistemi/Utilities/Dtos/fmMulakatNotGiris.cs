using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmMulakatNotGiris
    {

        public int SinavBilgisiGirilenProgramCount { get; set; }
        public int YerJuriBilgisiGirisProgramCount { get; set; }
        public int ToplamBasvuru { get; set; }
        public int ToplamMGiris { get; set; }
        public int AIToplamBasvuru { get; set; }
        public int ADToplamBasvuru { get; set; }
        public int AIMToplamBasvuru { get; set; }
        public int ADMToplamBasvuru { get; set; }
        public int AIMNotGirildiCount { get; set; }
        public int ADMNotGirildiCount { get; set; }
        public int YerJuriBilgisiGirisCount { get; set; }
        public bool SinavYerBilgisiMailiGonderildi { get; set; }
        public DateTime? SinavYerBilgisiMailiGonderimTarihi { get; set; }
        public List<frMulakatNotGirisDetay> MulakatNotGirisDetay { get; set; }
    }
    public class frMulakatNotGirisDetay
    {
        public bool MulakatSurecineGirecek { get; set; }
        public bool IsAlesYerineDosyaNotuIstensin { get; set; }
        public int? MulakatID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public int AlanIciKota { get; set; }
        public int AlanDisiKota { get; set; }
        public bool YerJuriBilgisiGirildi { get; set; }
        public bool SinavNotGirisiYapildi { get; set; }
        public int ToplamBasvuru { get; set; }
        public int ToplamMGiris { get; set; }
        public int AIToplamBasvuru { get; set; }
        public int ADToplamBasvuru { get; set; }
        public int AIMToplamBasvuru { get; set; }
        public int ADMToplamBasvuru { get; set; }
        public int AIMNotGirildiCount { get; set; }
        public int ADMNotGirildiCount { get; set; }

        public bool Ucretli { get; set; }
        public double? Ucret { get; set; }
        public string KullaniciTipAdi { get; set; }

    }
}