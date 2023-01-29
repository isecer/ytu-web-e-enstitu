using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class kmKotalar : Kotalar
    {
        public int? AlanIciKota { get; set; }
        public int? AlanDisiKota { get; set; }
        public double? MinAles { get; set; }
        public double? MinAGNO { get; set; }
    }
}