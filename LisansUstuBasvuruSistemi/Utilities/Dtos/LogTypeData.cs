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
            dct.Add(new LogTypeItem { BilgiTipID = LogType.Hata, BilgiTipAdi = "Hata", BilgiTipCls = "primary" });
            dct.Add(new LogTypeItem { BilgiTipID = LogType.Uyarı, BilgiTipAdi = "Uyarı", BilgiTipCls = "warning" });
            dct.Add(new LogTypeItem { BilgiTipID = LogType.Kritik, BilgiTipAdi = "Kritik Durum", BilgiTipCls = "danger" });
            dct.Add(new LogTypeItem { BilgiTipID = LogType.OnemsizHata, BilgiTipAdi = "Önemsiz Hata", BilgiTipCls = "default" });
            dct.Add(new LogTypeItem { BilgiTipID = LogType.Saldırı, BilgiTipAdi = "Saldırı", BilgiTipCls = "danger" });
            dct.Add(new LogTypeItem { BilgiTipID = LogType.LoginHatalari, BilgiTipAdi = "loginHatalari", BilgiTipCls = "info" });
            dct.Add(new LogTypeItem { BilgiTipID = LogType.Bilgi, BilgiTipAdi = "Bilgi", BilgiTipCls = "success" });
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