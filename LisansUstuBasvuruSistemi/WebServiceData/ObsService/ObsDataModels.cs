using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Ws_ObsService;

namespace LisansUstuBasvuruSistemi.Models.ObsService
{
    public class StudentControl
    {
        public bool KayitVar { get; set; }
        public bool Hata { get; set; }
        public bool IsDanismanHesabiBulunamadi { get; set; }
        public string HataMsj { get; set; }
        public int? BaslangicYil { get; set; }
        public int? BitisYil { get; set; }
        public int? DonemID { get; set; }
        public int OkuduguDonemNo { get; set; }
        public DateTime? KayitTarihi { get; set; }


        public StudentDersModel AktifDonemDers = new StudentDersModel();
        public Ogrenci OgrenciInfo { get; set; }
        public bool IsTezDiliTr { get; set; }
        public OgrenciTez OgrenciTez { get; set; }

        public int AraRaporMaxNo => this.SonTezIzlemeBilgileri.TEZ_IZL_SIRA.ToEmptyStringToZero() + 1;
        public int? AktifDanismanID { get; set; }
        public TezIzlemeBilgileri SonTezIzlemeBilgileri { get; set; }
        public List<TezIzlJuriBilgileri> TezIzlJuriBilgileri { get; set; }
        public List<OgrenciYeter> OgrenciYeters { get; set; }
        public Personel DanismanInfo { get; set; }
    }


    public class StudentDersModel
    {
        public StudentDersModel()
        {
            DersKodNums = new List<string>();
            DersKodus = new List<string>();
        }
        public int ToplamKredi { get; set; }
        public int ToplamAkts { get; set; }
        public double Agno { get; set; }
        public string EtikDersNotu { get; set; }
        public string SeminerDersNotu { get; set; }
        public int ZorunluDersSayisi { get; set; }
        public int AbdDersSayisi { get; set; }
        public List<string> DersKodus { get; set; }
        public List<string> DersKodNums { get; set; }
    }

    public class ObsOgrenciSorgulaModel
    {
        public string Tc { get; set; }
        public Ogrenci Ogrenci { get; set; }
        public OgrenciDersNot OgrenciDersNot { get; set; }
        public OgrenciTez OgrenciTez { get; set; }
        public List<TezIzlJuriBilgileri> OgrenciTezJuri { get; set; }
        public List<OgrenciYeter> OgrenciYeters { get; set; }
    }



}