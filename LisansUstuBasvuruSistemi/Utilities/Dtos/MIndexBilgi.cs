using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MIndexBilgi
    {
        public int Toplam { get; set; }
        public int Aktif { get; set; }
        public int Pasif { get; set; }
        public List<mxRowModel> ListB { get; set; }
        public MIndexBilgi()
        {
            ListB = new List<mxRowModel>();
        }
    }
    public class mxRowModel
    {
        public int ID { get; set; }
        public string Key { get; set; }
        public string ClassName { get; set; }
        public string Color { get; set; }
        public int Toplam { get; set; }
        public int KayitOlan { get; set; }

    }
}