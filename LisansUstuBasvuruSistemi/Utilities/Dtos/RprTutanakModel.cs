using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class RprTutanakModel
    {
        public bool IsDoktoraOrYL { get; set; }
        public string TutanakAdi { get; set; }
        public string Sayi { get; set; }
        public string Aciklama { get; set; }
        public bool? IsNewOrEdit { get; set; }
        public List<RprTutanakRowModel> DetayData { get; set; }
        public RprTutanakModel()
        {
            DetayData = new List<RprTutanakRowModel>();
        }
    }
    public class RprTutanakRowModel
    {
        public string OgrenciBilgi { get; set; }
        public string DanismanAdSoyad { get; set; }
        public string DanismanUni { get; set; }
        public string TikUyesi { get; set; }
        public string TikUyesiUni { get; set; }
        public string TikUyesi2 { get; set; }
        public string TikUyesi2Uni { get; set; }
        public string AsilUye { get; set; }
        public string AsilUyeUni { get; set; }
        public string AsilUye2 { get; set; }
        public string AsilUye2Uni { get; set; }
        public string YedekUye { get; set; }
        public string YedekUyeUni { get; set; }
        public string YedekUye2 { get; set; }
        public string YedekUye2Uni { get; set; }
        public string TezKonusu { get; set; }

    }
}