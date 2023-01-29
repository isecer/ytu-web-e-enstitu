using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class PersisWsDataModel
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