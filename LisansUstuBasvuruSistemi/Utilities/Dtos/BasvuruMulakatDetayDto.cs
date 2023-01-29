using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class BasvuruMulakatDetayDto : Mulakat
    {
        public int SelectedTabIndex { get; set; }
        public int BaslangicYil { get; set; }
        public int BitisYil { get; set; }
        public string DonemAdi { get; set; }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public bool MulakatSinavNotGirisiAktif { get; set; }
        public DateTime? SonucGirisBaslangicTarihi { get; set; }
        public DateTime? SonucGirisBitisTarihi { get; set; }
        public DateTime? AGNOGirisBaslangicTarihi { get; set; }
        public DateTime? AGNOGirisBitisTarihi { get; set; }
        public string BasvuruSurecAdi { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public bool IsUcretliKayit { get; set; }
        public bool Ucretli { get; set; }
        public double? Ucret { get; set; }
        public string ProgramAdi { get; set; }
        public bool MulakatSurecineGirecek { get; set; }
        public bool AlanIciBilimselHazirlik { get; set; }
        public bool AlanDisiBilimselHazirlik { get; set; }
        public string OgrenimTipAdi { get; set; }
        public double BasariNotOrtalamasi { get; set; }
        public List<krMulakatDetay> MulakatDetay { get; set; }
        public List<krMulakatSonuc> MulakatSonuc { get; set; }
        public bool OrtakKota { get; set; }
        public int? OrtakKotaSayisi { get; set; }
        public int AlanIciKota { get; set; }
        public int AlanDisiKota { get; set; }
        public int AlanIciEkKota { get; set; }
        public int AlanDisiEkKota { get; set; }
        public bool YaziliNotuIstensin { get; set; }
        public bool SozluNotuIstensin { get; set; }
        public bool SonucHesaplandi { get; set; }
        public bool IsAlesYerineDosyaNotuIstensin { get; set; }
        public bool IsBelgeYuklemeVar { get; set; }
        public BasvuruMulakatDetayDto()
        {
            SonucHesaplandi = false;
            MulakatDetay = new List<krMulakatDetay>();
            MulakatSonuc = new List<krMulakatSonuc>();
        }
    } 
   
}