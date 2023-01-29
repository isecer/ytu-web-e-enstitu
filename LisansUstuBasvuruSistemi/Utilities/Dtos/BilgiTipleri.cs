using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class BilgiTipleri
    {
        public List<BilgiRow> BilgiTip { get; set; }
        public BilgiTipleri()
        {

            var dct = new List<BilgiRow>();
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.Hata, BilgiTipAdi = "Hata", BilgiTipCls = "primary" });
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.Uyarı, BilgiTipAdi = "Uyarı", BilgiTipCls = "warning" });
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.Kritik, BilgiTipAdi = "Kritik Durum", BilgiTipCls = "danger" });
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.OnemsizHata, BilgiTipAdi = "Önemsiz Hata", BilgiTipCls = "default" });
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.Saldırı, BilgiTipAdi = "Saldırı", BilgiTipCls = "danger" });
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.LoginHatalari, BilgiTipAdi = "loginHatalari", BilgiTipCls = "info" });
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.Bilgi, BilgiTipAdi = "Bilgi", BilgiTipCls = "success" });
            BilgiTip = dct;
             
        } 
    }
    public class BilgiRow
    {
        public int BilgiTipID { get; set; }
        public string BilgiTipAdi { get; set; }
        public string BilgiTipCls { get; set; }
    }
}