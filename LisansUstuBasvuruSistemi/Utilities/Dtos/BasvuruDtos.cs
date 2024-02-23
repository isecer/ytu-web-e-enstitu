using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class IndexPageInfoDto
    {
        public bool SistemBasvuruyaAcik { get; set; }
        public bool SistemGirisSinavBilgiAcik { get; set; }
        public int? AktifSurecID { get; set; }
        public bool YtuOgrencisi { get; set; }
        public Enstituler Enstitü { get; set; }
        public BasvuruSurec BasvuruSurec { get; set; }
        public MezuniyetSureci MezuniyetSurec { get; set; }
        public Kullanicilar Kullanici { get; set; }
        public string DonemAdi { get; set; }

        public string OgrenimTipAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string OgrenciNo { get; set; }
        public string KayitDonemi { get; set; }

        public string BirimAdi { get; set; }
        public string UnvanAdi { get; set; }
        public string SicilNo { get; set; }

        public bool EnstituYetki { get; set; }
        public bool KullaniciTipYetki { get; set; }
        public string KullaniciTipYetkiYokMsj { get; set; }

    }
    public class BasvuruDurumDto : BasvuruDurumlari
    {
        public string DurumAdi { get; set; }
    }
    public class BasvuruTercihKontrolDto
    {

        public int SinavTipGrupID { get; set; }
        public int BasvuruSurecID { get; set; }
        public int SinavTipID { get; set; }
        public List<int> OgrenimTipKods { get; set; }
        public List<string> ProgramKods { get; set; }
        public List<bool> Ingilizces { get; set; }

        public BasvuruTercihKontrolDto()
        {
            OgrenimTipKods = new List<int>();
            ProgramKods = new List<string>();
            Ingilizces = new List<bool>();
        }

    }

    public class EntBegeKayitT
    {
        public string EnstituKod { get; set; }
        public int OgrenimTipKod { get; set; }
        public int MulakatSonucTipID { get; set; }
        public DateTime BaslangicTar { get; set; }
        public DateTime BitisTar { get; set; }
    }
    public class BasvuruDetayDto : Basvurular
    {
        public int SelectedTabIndex { get; set; }

        public bool IsYerli { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string ResimYolu { get; set; }
        public string AdSoyad { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public string BasvuruDurumAdi { get; set; }

        public string CinsiyetAdi { get; set; }
        public string UyrukAdi { get; set; }
        public string DogumYeriAdi { get; set; }
        public string NufusIlIlceAdi { get; set; }
        public string YasadigiSehirAdi { get; set; }


        public string LUniversiteAdi { get; set; }
        public string LBolumAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string LNotSistemi { get; set; }
        public string LegitimDilAdi { get; set; }
        public string YLUniversiteAdi { get; set; }
        public string YLBolumAdi { get; set; }
        public string YLNotSistemi { get; set; }
        public string YLegitimDilAdi { get; set; }
        public string DRUniversiteAdi { get; set; }
        public string DRBolumAdi { get; set; }
        public string DRNotSistemi { get; set; }
        public string DRegitimDilAdi { get; set; }

        public List<BasvuruTercihDto> Tercihlers { get; set; }
        public bool IsSecilenTercihVarAsil { get; set; }
        public bool IsSecilenTercihVarYedek { get; set; }
        public bool IsTurkceProgramVar { get; set; }
        public bool IsBelgeYuklemeVar { get; set; }
        public List<BasvuruSinavTipDto> Sinavlars { get; set; }

        public bool IsGonderilenMaillerVar { get; set; }
        public bool IsHesaplandi { get; set; }
        public bool IsKayitHakkiVar { get; set; }
        public bool IsBelgeYuklemeAktif { get; set; }
        public bool IsYedekCokluTercih { get; set; }

        public List<BasvuruBelgeDto> Belgelers { get; set; }

        public bool KayitIslemiGordu { get; set; }
        public bool IsSave { get; set; }
        public bool IsKayittaBelgeOnayiZorunlu { get; set; }

        public YokStudentControl YokStudentControl { get; set; }
        public BasvuruDetayDto()
        {
            Tercihlers = new List<BasvuruTercihDto>();
            Sinavlars = new List<BasvuruSinavTipDto>();
            Belgelers = new List<BasvuruBelgeDto>();
        }
    }
    public class YokStudentControl
    {
        public bool KayitVar { get; set; }
        public bool Hata { get; set; }
        public string Mesaj { get; set; }
        public List<string> AktifOgrenimListesi { get; set; }
        public YokStudentControl()
        {
            AktifOgrenimListesi = new List<string>();
        }
    }
    public class BasvuruTercihDto : BasvurularTercihleri
    {
        public bool IsSeciliBasvuruyaAitTercih { get; set; }
        public int MulakatSonucID { get; set; }
        public int MulakaSonucTipID { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public bool Ingilizce { get; set; }
        public string ProgramAdi { get; set; }
        public string EgitimDilAdi { get; set; }
        public string AlesTipAdi { get; set; }
        public string AlanTipAdi { get; set; }
        public int Kota { get; set; }
        public int? KayitDurumID { get; set; }
        public bool? KayıtOldu { get; set; }
        public bool IsBelgeYuklemeAktif { get; set; }
    }
    public class BasvuruSinavTipDto : SinavTipleri
    {
        public bool IsWebService { get; set; }
        public int? SinavDilID { get; set; }
        public string SinavDilAdi { get; set; }
        public bool IsTaahhutVar { get; set; }
        public string GrupAdi { get; set; }
        public int? Yil { get; set; }
        public string DonemAdi { get; set; }
        public DateTime? SinavTarihi { get; set; }
        public double? SinavSubPuani { get; set; }
        public double SinavPuani { get; set; }
        public SinavSonucAlesXmlModel AlesXmlModel { get; set; }
    }
    public class SinavSonucAlesXmlModel
    {
        public string TCK { get; set; }
        public string AD { get; set; }
        public string SOYAD { get; set; }
        public string ENGELDURUM { get; set; }
        public string SAY1_DOGRU { get; set; }
        public string SAY1_YANLIS { get; set; }
        public string SAY2_DOGRU { get; set; }
        public string SAY2_YANLIS { get; set; }
        public string SOZ1_DOGRU { get; set; }
        public string SOZ2_DOGRU { get; set; }
        public string SAY_PUAN { get; set; }
        public string SAY_BASARI { get; set; }
        public string SAY_TOPLAM_BASARI { get; set; }
        public string SOZ_PUAN { get; set; }
        public string SOZ_BASARI { get; set; }
        public string SOZ_TOPLAM_BASARI { get; set; }
        public string EA_PUAN { get; set; }
        public string EA_BASARI { get; set; }
        public string EA_TOPLAM_BASARI { get; set; }
        public string SGK { get; set; }

    }
    public class BasvuruDetaySecilenDto : Basvurular
    {

        public int BasvuruSurecTipID { get; set; }
        public bool IsSave { get; set; }
        public bool IsYerli { get; set; }
        public string EnstituAdi { get; set; }
        public int SelectedTabIndex { get; set; }
        public string LUniversiteAdi { get; set; }
        public string LBolumAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string LNotSistemi { get; set; }
        public string LegitimDilAdi { get; set; }
        public string YLUniversiteAdi { get; set; }
        public string YLBolumAdi { get; set; }
        public string YLNotSistemi { get; set; }
        public string YLegitimDilAdi { get; set; }
        public string DRUniversiteAdi { get; set; }
        public string DRBolumAdi { get; set; }
        public string DRNotSistemi { get; set; }
        public string DRegitimDilAdi { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string Cinsiyet { get; set; }
        public string UyrukAdi { get; set; }
        public string DogumYeriAdi { get; set; }
        public string YasadigiSehirAdi { get; set; }
        public string NufusIlIcleAdi { get; set; } 

        public BasvuruSinavBilgiGrupDto BasvurularSinavBilgi_A { get; set; }
        public BasvuruSinavBilgiGrupDto BasvurularSinavBilgi_D { get; set; }
        public BasvuruSinavBilgiGrupDto BasvurularSinavBilgi_T { get; set; }
        public List<basvuruTercihModel> BasvuruTercihleri { get; set; }

        public string BasvuruDurumAdi { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }


        public BasvuruDetaySecilenDto()
        {
            BasvurularSinavBilgi_A = new BasvuruSinavBilgiGrupDto();
            BasvurularSinavBilgi_D = new BasvuruSinavBilgiGrupDto();
            BasvurularSinavBilgi_T = new BasvuruSinavBilgiGrupDto();
            BasvuruTercihleri = new List<basvuruTercihModel>();

        }
    }

    public class basvuruTercihModel
    {

        public int BasvuruTercihID { get; set; }
        public int BasvuruID { get; set; }
        public Guid UniqueID { get; set; }
        public int SiraNo { get; set; }
        public bool YlBilgiIste { get; set; }
        public int AlanTipID { get; set; }
        public string AlanTipAdi { get; set; }
        public int OgrenimTipKod { get; set; }
        public string ProgramKod { get; set; }
        public bool IsSecilenTercih { get; set; }
        public bool IsAsilOrYedek { get; set; }
        public KontenjanProgramBilgiModel ProgramBilgileri = new KontenjanProgramBilgiModel();
    }
    public class BasvuruBelgeDto : BasvurularYuklenenBelgeler
    {
        public int SiraNo { get; set; }
        public string BasvuruBelgeTipAdi { get; set; }
        public int SinavTipKod { get; set; }
        public string SinavTipAdi { get; set; }
        public bool IsKayitSonrasiGetirilecek { get; set; }
        public string Not { get; set; }
    }
    public class BasvuruSinavBilgiGrupDto
    {
        public string DilKodu { get; set; }
        public string DurumColor { get; set; }
        public DateTime BasvuruTarihi { get; set; }
        public bool IsTurkceProgramVar { get; set; }
        public BasvurularSinavBilgi Sinav { get; set; }
        public List<int> SecilenAlesTipleri { get; set; }
        public SinavBilgiModel SinavDetay { get; set; }
    }
}