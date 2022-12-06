using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.KmDtos
{
    public class kmKotalar : Kotalar
    {
        public int? AlanIciKota { get; set; }
        public int? AlanDisiKota { get; set; }
        public double? MinAles { get; set; }
        public double? MinAGNO { get; set; }
    }
}