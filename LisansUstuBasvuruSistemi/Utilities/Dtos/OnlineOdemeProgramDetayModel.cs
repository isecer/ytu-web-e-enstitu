using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class OnlineOdemeProgramDetayModel
    {
        public int BasvuruTercihID { get; set; }
        public string EnstituKod { get; set; }
        public int BasvuruSurecID { get; set; }
        public string SurecAdi { get; set; }
        public int BasvuruID { get; set; }
        public int DonemBaslangicYil { get; set; }
        public int DonemID { get; set; }
        public DateTime? SurecBaslangicTarihi { get; set; }
        public DateTime? SurecBitisTarihi { get; set; }
        public string Aciklama { get; set; }
        public string AciklamaSelectedLng { get; set; }
        public int KullaniciID { get; set; }
        public bool YtuOgrencisi { get; set; }
        public bool IsYerliOgrenci { get; set; }
        public string OgrenciNo { get; set; }
        public string TcKimlikNo { get; set; }
        public string AdSoyad { get; set; }
        public string EMail { get; set; }
        public string CepTel { get; set; }
        public Guid UniqueID { get; set; }
        public int AlanTipID { get; set; }
        public int AlanKota { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public int AlesTipID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public bool Ucretli { get; set; }
        public double? Ucret { get; set; }
        public int OdemeDonemNo { get; set; }
        public bool IsDekontOrSanalPos { get; set; }
        public bool IsYokOgrenciKaydiVar { get; set; }
        public bool YokOgrenciKontroluYap { get; set; }
        public double? IstenecekKatkiPayiTutari { get; set; }
        public bool? IsOgrenimUcretiOrKatkiPayi { get; set; }
        public bool YokOgrenciKontrolHataVar { get; set; }
        public bool IsOdemeVar { get; set; }
        public bool IsOdemeIslemiAcik { get; set; }
        public List<string> AktifOgrenimListesi { get; set; }
        public double? OdenecekUcret { get; set; }
        public DateTime? OdemeBaslangicTarihi { get; set; }
        public DateTime? OdemeBitisTarihi { get; set; }
        public bool KayitOldu { get; set; }
        public MulakatSonuclari MulakatSonuclari { get; set; }
        public Basvurular BasvuruBilgi { get; set; }
        public bool IsBelgeYuklemesiVar { get; set; }
        public bool IsKayittaBelgeOnayiZorunlu { get; set; }
        public List<int> IstenenBasvuruBelgeTipID { get; set; }
        public List<BasvurularYuklenenBelgeler> BasvurularYuklenenBelgeler { get; set; }
        public bool IsBelgeDialogYuklemeShow { get; set; }
        public bool IsBelgeDialogYuklemeClose { get; set; }


        public List<BasvurularTercihleriKayitOdemeleri> OdemeListesi { get; set; }
        public List<string> BelgeKontrolMessages { get; set; }
        public OnlineOdemeProgramDetayModel()
        {
            BelgeKontrolMessages = new List<string>();
            IstenenBasvuruBelgeTipID = new List<int>();
            AktifOgrenimListesi = new List<string>();
            OdemeListesi = new List<BasvurularTercihleriKayitOdemeleri>();
        }
    }
}