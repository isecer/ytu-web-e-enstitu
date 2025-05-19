using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmYeterlikBasvuruDto : PagerModel
    {
        public bool? IsKomiteOrJuri { get; set; }
        public Guid? IsDegerlendirme { get; set; }
        public int? YeterlikBasvuruID { get; set; }

        public int? AktifYeterlikSurecId { get; set; }
        public bool IsAktifSurecBasvuruVar { get; set; }
        public int? BasvuruDurumID { get; set; }

        public string EnstituAdi { get; set; }
        public string DonemAdi { get; set; }
        public string AdSoyad { get; set; }
        public string OgrenciNo { get; set; }
        public bool IsYtuOgrencisi { get; set; }
        public bool IsEnstituYetki { get; set; }
        public bool IsOgrenimSeviyeYetki { get; set; }
        public string OgrenimTipAdis { get; set; }
        public int? YeterlikSurecID { get; set; }
        public int? OgrenimTipID { get; set; }
        public int? AnabilimDaliID { get; set; }
        public bool? IsDanismaniOlunanOgrenciler { get; set; }

        public List<FrYeterlikBasvuruDto> Data { get; set; }
    }

    public class FrYeterlikBasvuruDto : YeterlikBasvuru
    {
        public Guid? UserKey { get; set; }
        public string AdSoyad { get; set; }
        public string TcKimlikNo { get; set; }
        public string EMail { get; set; }
        public string CepTel { get; set; }
        public string ResimAdi { get; set; }
        public int OgrenimTipKod { get; set; }
        public int AnabilimDaliID { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string DonemAdi { get; set; }
        public string TezDanismanAdi { get; set; }
        public string TezDanismanEmail { get; set; }
        public string TezDanismanCepTel { get; set; }
        public bool IsJuriOlusturuldu { get; set; }
        public List<int> OnayYapmayanKomiteIds { get; set; }
        public List<string> OnayYapmayanJuriEmails { get; set; }
        public bool IsYaziliSinaviYapildi { get; set; }
        public bool IsSozluSinaviYapildi { get; set; }
        public bool IsDanismaniOlunanOgrenci { get; set; }
        public YeterlikBasvuru BirOncekiBasvuru { get; set; }
    }

}