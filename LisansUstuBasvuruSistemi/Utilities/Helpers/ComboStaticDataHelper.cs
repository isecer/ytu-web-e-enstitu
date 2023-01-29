using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class ComboStaticDataHelper
    {
        public static List<CmbIntDto> CmbCardBonusType()
        {
            var mdl = new List<CmbIntDto>();
            mdl.Add(new CmbIntDto { Value = null, Caption = "" });
            mdl.Add(new CmbIntDto { Value = 1, Caption = "Bonus Kart Özelliği Var" });
            mdl.Add(new CmbIntDto { Value = 0, Caption = "Bonus Kart Özelliği Yok" });
            return mdl;
        }
        public static List<CmbIntDto> CmbCardMaximumType()
        {
            var mdl = new List<CmbIntDto>();
            mdl.Add(new CmbIntDto { Value = null, Caption = "" });
            mdl.Add(new CmbIntDto { Value = 1, Caption = "Var" });
            mdl.Add(new CmbIntDto { Value = 0, Caption = "Yok" });
            return mdl;
        }
        public static List<CmbIntDto> CmbTaksitList()
        {
            var mdl = new List<CmbIntDto>();
            mdl.Add(new CmbIntDto { Value = null, Caption = "Taksit İstemiyorum" });
            mdl.Add(new CmbIntDto { Value = 5, Caption = "5 Taksit" });
            return mdl;
        }
    }
}