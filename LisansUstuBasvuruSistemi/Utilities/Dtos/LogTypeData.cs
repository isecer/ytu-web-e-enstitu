using System.Collections.Generic;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class LogTypeData
    {
        public List<LogTypeItem> LogTipiData { get; set; }
        public LogTypeData()
        {

            var dct = new List<LogTypeItem>
            {
                new LogTypeItem { BilgiTipId = BilgiTipiEnum.Hata, BilgiTipAdi = "Hata", BilgiTipCls = "primary" },
                new LogTypeItem { BilgiTipId = BilgiTipiEnum.Uyarı, BilgiTipAdi = "Uyarı", BilgiTipCls = "warning" },
                new LogTypeItem { BilgiTipId = BilgiTipiEnum.Kritik, BilgiTipAdi = "Kritik Durum", BilgiTipCls = "danger" },
                new LogTypeItem { BilgiTipId = BilgiTipiEnum.OnemsizHata, BilgiTipAdi = "Önemsiz Hata", BilgiTipCls = "default" },
                new LogTypeItem { BilgiTipId = BilgiTipiEnum.Saldırı, BilgiTipAdi = "Saldırı", BilgiTipCls = "danger" },
                new LogTypeItem { BilgiTipId = BilgiTipiEnum.LoginHatalari, BilgiTipAdi = "loginHatalari", BilgiTipCls = "info" },
                new LogTypeItem { BilgiTipId = BilgiTipiEnum.Bilgi, BilgiTipAdi = "Bilgi", BilgiTipCls = "success" }
            };
            LogTipiData = dct;
             
        } 
    }
    public class LogTypeItem
    {
        public int BilgiTipId { get; set; }
        public string BilgiTipAdi { get; set; }
        public string BilgiTipCls { get; set; }
    }
}