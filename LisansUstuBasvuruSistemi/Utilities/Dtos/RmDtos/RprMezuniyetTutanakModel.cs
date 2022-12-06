using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.RmDtos
{
    public class RprMezuniyetTutanakModel
    {
        public string Konu { get; set; }
        public string Aciklama1 { get; set; }
        public string Aciklama2 { get; set; }

        public List<RprMezuniyetTutanakRowModel> Data { get; set; }

        public RprMezuniyetTutanakModel()
        {
            Data = new List<RprMezuniyetTutanakRowModel>();
        }
    }
    public class RprMezuniyetTutanakRowModel
    {
        public string OgrenciBilgi { get; set; }
        public string DanismanAdSoyad { get; set; }
        public string TezKonusu { get; set; }
        public string SavunmaTarihi { get; set; }
        public string TezTeslimTarihi { get; set; }
    }
}