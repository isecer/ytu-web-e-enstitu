using System;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    [Obsolete("Kullanılmıyor")]
    public class MyLoggingService : DevExpress.XtraReports.Web.Native.ClientControls.Services.ILoggingService
    {
        public void Error(string message)
        {
          //  Management.SistemBilgisiKaydet("ReportViewerError: " + message, "Global.asax", BilgiTipi.Hata);
        }

        public void Info(string message)
        {

           // Management.SistemBilgisiKaydet("ReportViewerInfo: " + message, "Global.asax", BilgiTipi.Bilgi);
        }
    }
     
}