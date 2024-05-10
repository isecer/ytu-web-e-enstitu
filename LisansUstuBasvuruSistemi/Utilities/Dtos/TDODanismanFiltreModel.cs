using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
   
    public class TdoBasvuruDurumSortDto
    {
        public bool? IsOnayOrRed { get; set; }
        public string DurumAciklama { get; set; }
        public string DurumClass => IsOnayOrRed.HasValue ? (IsOnayOrRed.Value ? "fa fa-thumbs-o-up" : "fa fa-thumbs-o-down") : "fa fa-clock-o";
        public string DurumColor => IsOnayOrRed.HasValue ? (IsOnayOrRed.Value ? "green" : "maroon") : "";

    }
}