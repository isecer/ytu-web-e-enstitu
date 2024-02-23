using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmYeterlikSureciDto : PagerModel
    {
        public string EnstituKod { get; set; }
        public List<FrYeterlikSureci> FrYeterlikSurecis { get; set; }
    }

    public class FrYeterlikSureci : YeterlikSureci
    {
        public string EnstituAdi { get; set; }
        public string DonemAdi { get; set; }
        public string IslemYapan { get; set; }
    }
}