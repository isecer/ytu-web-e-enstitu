using System;
using System.Collections.Generic;

namespace LisansUstuBasvuruSistemi.WebServiceData.ObsService.ObsDtos
{
    public class ObsStudentDto
    {
        public bool KayitVar { get; set; }
        public bool Hata { get; set; }
        public string HataMsj { get; set; }

        public string TcKimlikNo { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public bool IsErkek { get; set; }
        public string EPosta { get; set; }
        public string CepTel { get; set; }

        public List<ObsStudentOgrenimDto> AktifOgrenimler { get; set; } = new List<ObsStudentOgrenimDto>();
    }

    public class ObsStudentOgrenimDto
    {
        public int? BaslangicYil { get; set; }
        public int? BitisYil { get; set; }
        public int? DonemId { get; set; }
        public DateTime? KayitTarihi { get; set; }
        public string KayitNedeni { get; set; }
        public string OgrenciNo { get; set; }
        public string EnstituKod { get; set; }
        public string EnstituAd { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimSeviyeAdi { get; set; }
        public string AnabilimDaliId { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public int ProgramId { get; set; }
        public string ProgramAdi { get; set; }
        public int OkuduguDonemNo { get; set; }


        public ObsOgrenciTezInfoDto TezInfo { get; set; }

        public ObsStudentDanismanInfo DanismanInfo { get; set; }

        public ObsStudentDersGenelInfoDto AktifDonemDersGenelInfo { get; set; } = new ObsStudentDersGenelInfoDto();
        public List<ObsStudentDersNotDto> DersNotlari { get; set; } = new List<ObsStudentDersNotDto>();

        public List<ObsStudentYeterlikDto> YeterlikInfos { get; set; }=new List<ObsStudentYeterlikDto>();

    }

    public class ObsStudentDanismanInfo
    {
        public string TcKimlikNo { get; set; }
        public string SicilNo { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public int UnvanId { get; set; }
        public string UnvanAdi { get; set; }
        public string EPosta { get; set; }
        public string Ceptel { get; set; }
        public int YlOgrenciSayisiDanismanlik { get; set; }
        public int YlOgrenciSayisiMezunOlan { get; set; }
        public int DrOgrenciSayisiDanismanlik { get; set; }
        public int DrOgrenciSayisiMezunOlan { get; set; }
    }

    public class ObsOgrenciTezInfoDto
    {
        public int TezId { get; set; }
        public bool IsTezDiliTr { get; set; }
        public string TezBasligi { get; set; }
        public string TezBasligiEn { get; set; }
        public int TezIzlemeSayisi { get; set; }
        public ObsStudentTezIzlemeDto SonTezIzlemeInfo { get; set; } = new ObsStudentTezIzlemeDto();
    }
    public class ObsStudentDersGenelInfoDto
    {
        public int ToplamKredi { get; set; }
        public int ToplamAkts { get; set; }
        public double Agno { get; set; }
        public string EtikDersNotu { get; set; }
        public string SeminerDersNotu { get; set; }
        public int ZorunluDersSayisi { get; set; }
        public int AbdDersSayisi { get; set; }
        public List<string> DersKodus { get; set; } = new List<string>();
        public List<string> DersKodNums { get; set; } = new List<string>();
    }
    public class ObsStudentDersNotDto
    {
        public string DonemId { get; set; }
        public string DonemAd { get; set; }
        public string HocaTc { get; set; }
        public string HocaUnvan { get; set; }
        public string HocaAdi { get; set; }
        public string DersKoduNum { get; set; }
        public string DersKodu { get; set; }
        public string DersAdi { get; set; }
        public string DersNotu { get; set; }
        public string NotDeger { get; set; }
    }

    public class ObsStudentTezIzlemeDto
    {
        public int Id { get; set; }
        public string Durum { get; set; }
        public string Yer { get; set; }
        public DateTime Tarih { get; set; }
        public int Sira { get; set; }
        public string DonemAdi { get; set; }
        public List<ObsStudentTezIzlemeJuriDto> TezIzlemeJurileri { get; set; } = new List<ObsStudentTezIzlemeJuriDto>();
    }
    public class ObsStudentTezIzlemeJuriDto
    { 
        public string AdSoyad { get; set; }
        public string UnvanKod { get; set; }
        public string UnvanAdi { get; set; }
        public string UniversiteAdi { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string CepTel { get; set; }
        public string EPosta { get; set; }
    }
    public class ObsStudentYeterlikDto
    { 
        public DateTime? YaziliSinavTarihi { get; set; }
        public string YaziliSinavYeri { get; set; }
        public string YaziliSinavDurumu { get; set; }
        public DateTime? SozluSinavTarihi { get; set; }
        public string SozluSinavDurumu { get; set; }
        public string SozluSinavYeri{ get; set; }
        public string GenelSinavDurum { get; set; } 
    }
}
