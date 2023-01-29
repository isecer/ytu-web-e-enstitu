using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class AnketCevapDto
    {
        public int? SecilenAnketSoruSecenekID { get; set; }
        public frAnketDetay SoruBilgi { get; set; }
        public List<frAnketSecenekDetay> SoruSecenek { get; set; }
        public SelectList SelectListSoruSecenek { get; set; }
    }
}