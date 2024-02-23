using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class krMulakatSonuc : MulakatSonuclari
    {

        public bool SuccessRow { get; set; }
        public int BasvuruSurecTipID { get; set; }
        public int KullaniID { get; set; }
        public string AdSoyad { get; set; }
        public string UniqueID { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string ProgramKod { get; set; }
        public int OgrenimTipKod { get; set; }
        public bool MulakatSurecineGirecek { get; set; }
        public string MulakatSonucTipAdi { get; set; }
        public BasvurularSinavBilgi Sinav { get; set; }
        public string DekontNo { get; set; }
        public DateTime? DekontTarihi { get; set; }

        public int? LOgrenimDurumID { get; set; }
        public string LOgrenimDurumAdi { get; set; }
        public int MezuniyetNotSistemi { get; set; }
        public double MezuniyetNotu { get; set; }


        public int? KayitOncelikSiraNo { get; set; }

        public int? SinavTipKod { get; set; }
        public string SinavAdi { get; set; }
        public double? SinavNotu { get; set; }

        public int JuriCount { get; set; }
        public int YerCount { get; set; }

        public double? MinAGNO { get; set; }

        public int AlanKota { get; set; }
        public int AlanKotaYedek { get; set; }

        public bool YaziliSinaviIstensin { get; set; }
        public bool SozluSinaviIstensin { get; set; }

        public bool ShowBilimselHazirlik { get; set; }
        public bool EnabledBilimselHazirlik { get; set; }
        public bool IsUcretliKayit { get; set; }
        public bool? IsDilTaahhutVar { get; set; }
        public bool IsAlesYerineDosyaNotuIstensin { get; set; }
        public bool Ingilizce { get; set; }
        public int KayittaIstenecekBelgeCount { get; set; }
        public int BasvurudaYuklenenBelgeCount { get; set; }
        public krMulakatSonuc()
        {
            SiraNo = 0;
            SuccessRow = false;
            SinavaGirmediY = false;
            SinavaGirmediS = false;
        }
    }
    public class krMulakatSonucPostModel
    {
        public int BasvuruSurecID { get; set; }
        public bool MulakatSurecineGirecek { get; set; }
        public int MulakatID { get; set; }
        public bool IsAlesYerineDosyaNotuIstensin { get; set; }
        public int AlanTipID { get; set; }
        public string ProgramKod { get; set; }
        public int OgrenimTipKod { get; set; }
        public List<int> BasvuruTercihID { get; set; }
        public List<bool?> SinavaGirmediY { get; set; }
        public List<double?> AlesNotuOrDosyaNotu { get; set; }
        public List<double?> YaziliNotu { get; set; }
        public List<bool?> SinavaGirmediS { get; set; }
        public List<double?> SozluNotu { get; set; }
        public List<bool?> BilimselHazirlikVar { get; set; }

        public krMulakatSonucPostModel()
        {
            BasvuruTercihID = new List<int>();
            SinavaGirmediY = new List<bool?>();
            YaziliNotu = new List<double?>();
            SinavaGirmediS = new List<bool?>();
            SozluNotu = new List<double?>();
        }
    }

 
    
    
}