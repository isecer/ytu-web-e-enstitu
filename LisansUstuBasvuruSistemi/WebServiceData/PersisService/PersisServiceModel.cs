using System.Collections.Generic;

namespace LisansUstuBasvuruSistemi.WebServiceData.PersisService
{
    public class PersisServiceModel
    {
        public List<Table> Table { get; set; }
    }
    public class Table
    {
        public string BOLUMADI { get; set; }
        public string ADSOYAD { get; set; }
        public string AKADEMIKUNVAN { get; set; }
        public string KADROUNVAN { get; set; }
        public string KURUMMAIL { get; set; }
    }
}