using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{

    public class KmDonemProjesiDto : DonemProjesi
    {
        public Guid? UniqueID { get; set; }
        public string AdSoyad { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
    }
    public class DonemProjesiJuriFormuKayitDto : DonemProjesiBasvuru
    {
        public int KullaniciId { get; set; }
        public string OgrenciAnabilimdaliProgramAdi { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public List<int> RowNum { get; set; }
        public List<bool> IsTezDanismani { get; set; } = new List<bool>();
        public List<string> AdSoyad { get; set; } = new List<string>();
        public List<string> UnvanAdi { get; set; } = new List<string>();
        public List<string> EMail { get; set; } = new List<string>();
        public List<string> AnabilimdaliAdi { get; set; } = new List<string>();

        public SelectList SListUnvanAdi { get; set; }
        public List<KrDonemProjesiJurileri> JoFormJuriList { get; set; } = new List<KrDonemProjesiJurileri>();
    }
    public class KrDonemProjesiJurileri : DonemProjesiJurileri
    {
        public SelectList SlistUnvanAdi { get; set; }
    }
    public class DpBasvuruDetayDto : DonemProjesi
    {
        public bool GelenBasvuru { get; set; }
        public string EnstituAdi { get; set; }
        public string ResimAdi { get; set; }
        public string TcKimlikNo { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public Guid? TezDanismaniUserKey { get; set; }
        public string TezDanismanBilgiEslesen { get; set; }

        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public Guid? DegerlendirenUniqueID { get; set; }
        public List<DonemProjesiBasvuruDto> DonemProjesiBasvurus { get; set; } 
    }
    public class DonemProjesiBasvuruDto : DonemProjesiBasvuru
    {
        public bool IsSonBasvuru { get; set; }
        public string DonemProjesiDurumAdi { get; set; }
        public string EnstituOnayDurumAdi { get; set; }
        public string JuriOnayDurumAdi { get; set; }
        public string DonemAdi { get; set; }
        public Guid? TezDanismaniUserKey { get; set; }
        public string DanismanAdi { get; set; }
        public FrTalepler SRModel { get; set; }
        public DpBasvuruDurumDto DonemProjesiDurumDto { get; set; }
    }

    public class DpBasvuruDurumDto
    {
        public int DonemProjesiID { get; set; }
        public int DonemProjesiBasvuruID { get; set; }
        public int BasvuruYil { get; set; }
        public string BasvuruDonemAdi { get; set; } 
        public DateTime BasvuruTarihi { get; set; }
        public int BasvuruDonemID { get; set; }
        public int DonemProjesiDurumID { get; set; }
        public int? DonemProjesiEnstituOnayDurumID { get; set; }
        public string EnstituOnayDurumAdi { get; set; }
        public string EnstituOnayAciklama { get; set; }
        public int TezDanismanID { get; set; }
        public bool? IsDanismanOnay { get; set; }

        public string DanismanOnayAciklama { get; set; }
        public bool IsDegerlendirmeBasladi { get; set; }
        public bool? IsOyBirligiOrCoklugu { get; set; }
        public int? DonemProjesiJuriOnayDurumID { get; set; }
        public string DonemProjesiJuriOnayDurumAdi { get; set; }
        public bool? EYKYaGonderildi { get; set; }
        public string EYKYaGonderimDurumAciklamasi { get; set; }
        public bool? EYKYaHazirlandi { get; set; }
        public bool? EYKDaOnaylandi { get; set; }
        public string EYKDaOnaylanmadiDurumAciklamasi { get; set; }
        public DateTime? EykTarihi { get; set; }
        public bool IsJuriOlusturuldu { get; set; }
        public bool IsSrTalebiYapildi { get; set; }
 
    }

    public class DpJuriTutanakModel
    {
 
        public string TutanakAdi { get; set; }
        public string Sayi { get; set; }
        public string Aciklama { get; set; }
        public List<RprDpjuriTutanakRowModel> DetayData { get; set; }
        public DpJuriTutanakModel()
        {
            DetayData = new List<RprDpjuriTutanakRowModel>();
        }
    }
    public class RprDpjuriTutanakRowModel
    { 
        public string OgrenciBilgi { get; set; }
        public string DanismanAdSoyad { get; set; }
        public string DanismanUni { get; set; }
        public string Uye1 { get; set; }
        public string Uye1Uni { get; set; }
        public string Uye2 { get; set; }
        public string Uye2Uni { get; set; } 
        public string ProjeBasligi { get; set; }

    }
}