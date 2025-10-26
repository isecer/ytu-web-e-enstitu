using System.Collections.Generic;

namespace LisansUstuBasvuruSistemi.WebServiceData.ObsRestData.Models
{
    public class ObsApiResponseDonem
    {
        public bool sonucDurum { get; set; }
        public string sonucAciklama { get; set; }

        public List<ObsServiceDonemDto> donem { get; set; }
        public List<FakulteItem> fakulte { get; set; }
        public List<BolumItem> bolum { get; set; }
        public List<ProgramItem> program { get; set; }
    }
}