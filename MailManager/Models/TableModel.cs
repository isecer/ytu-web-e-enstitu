using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailManager.Models
{
    public class TableModel
    {
        public bool IsJuriBilgi { get; set; }
        public string AciklamaBasligi { get; set; }
        public string AciklamaDetayi { get; set; }
        public bool AciklamaTextAlingCenter { get; set; }
        public string GrupBasligi { get; set; }
        public int CaptTdWidth { get; set; }
        public List<TableRowModel> Detaylar { get; set; }
        public bool Success { get; set; }
        public TableModel()
        {
            CaptTdWidth = 200;
            Detaylar = new List<TableRowModel>();
            AciklamaTextAlingCenter = false;
        }
    }
    public class TableRowModel
    {
        public bool Colspan2 { get; set; }
        public int SiraNo { get; set; }
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
        public TableRowModel()
        {
            Colspan2 = false;
        }
    }
}
