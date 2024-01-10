using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class LogTypeData
    {
        public List<LogTypeItem> LogTipiData { get; set; }
        public LogTypeData()
        {

            var dct = new List<LogTypeItem>();
            dct.Add(new LogTypeItem { BilgiTipID = BilgiTipiEnum.Hata, BilgiTipAdi = "Hata", BilgiTipCls = "primary" });
            dct.Add(new LogTypeItem { BilgiTipID = BilgiTipiEnum.Uyarı, BilgiTipAdi = "Uyarı", BilgiTipCls = "warning" });
            dct.Add(new LogTypeItem { BilgiTipID = BilgiTipiEnum.Kritik, BilgiTipAdi = "Kritik Durum", BilgiTipCls = "danger" });
            dct.Add(new LogTypeItem { BilgiTipID = BilgiTipiEnum.OnemsizHata, BilgiTipAdi = "Önemsiz Hata", BilgiTipCls = "default" });
            dct.Add(new LogTypeItem { BilgiTipID = BilgiTipiEnum.Saldırı, BilgiTipAdi = "Saldırı", BilgiTipCls = "danger" });
            dct.Add(new LogTypeItem { BilgiTipID = BilgiTipiEnum.LoginHatalari, BilgiTipAdi = "loginHatalari", BilgiTipCls = "info" });
            dct.Add(new LogTypeItem { BilgiTipID = BilgiTipiEnum.Bilgi, BilgiTipAdi = "Bilgi", BilgiTipCls = "success" });
            LogTipiData = dct;
             
        } 
    }
    public class LogTypeItem
    {
        public int BilgiTipID { get; set; }
        public string BilgiTipAdi { get; set; }
        public string BilgiTipCls { get; set; }
    }
}