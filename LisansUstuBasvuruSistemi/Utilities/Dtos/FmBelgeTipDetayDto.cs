using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmBelgeTipDetayDto : PagerModel
    {
        public string BelgeTipAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrBelgeTipDetayDto> BelgeTipDetayDtos { get; set; }

    }
    public class FrBelgeTipDetayDto : BelgeTipDetay
    {
        public List<string> BelgeTipAdi { get; set; }
        public List<BtSaatShowModel> Saatler { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string IslemYapan { get; set; }

    }
}