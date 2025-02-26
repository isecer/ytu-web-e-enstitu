using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.WebServiceData.ObsRestData.Models
{
    public class ObsApiResponseDonem
    {
        public bool sonucDurum { get; set; }
        public string sonucAciklama { get; set; }
        public List<ObsServiceDonemDto> donem { get; set; }
    }

    public class ObsServiceDonemDto
    {
        public string ID { get; set; }
        public string TIP { get; set; }
        public string YIL { get; set; }
        public string AD { get; set; }
        public string AD_EN { get; set; }
        public string DonemId { get; set; }
    }
}