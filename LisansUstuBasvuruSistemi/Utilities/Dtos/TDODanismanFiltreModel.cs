using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class TdoDanismanFiltreModel
    {
        public string FormKodu { get; set; }
        public string RaporDonemID { get; set; }
        public string DanismanAdSoyad { get; set; }
        public int TDODanismanTalepTipID { get; set; }
        public bool? VarolanDanismanOnayladi { get; set; }
        public bool? DanismanOnayladi { get; set; }
        public bool? EYKYaGonderildi { get; set; }
        public bool? EYKDaOnaylandi { get; set; }
        public string Es_FormKodu { get; set; }
        public string Es_DanismanAdSoyad { get; set; }
        public bool? Es_EYKYaGonderildi { get; set; }
        public bool? Es_EYKDaOnaylandi { get; set; }
    }
    public class TdoBasvuruDurumSortDto
    {
        public bool? IsOnayOrRed { get; set; }
        public string DurumAciklama { get; set; }
        public string DurumClass => IsOnayOrRed.HasValue ? (IsOnayOrRed.Value ? "fa fa-thumbs-o-up" : "fa fa-thumbs-o-down") : "fa fa-clock-o";
        public string DurumColor => IsOnayOrRed.HasValue ? (IsOnayOrRed.Value ? "green" : "maroon") : "";

    }
}