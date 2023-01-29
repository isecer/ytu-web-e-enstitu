using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class UrlInfoModel
    {
        public string Root { get; set; }
        public string FakeRoot { get; set; }
        public string DefaultUri { get; set; }
        public string AbsolutePath { get; set; }
        public string EnstituKisaAd { get; set; }
        public string LastPath { get; set; }
        public string Query { get; set; }
    }
}