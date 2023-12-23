using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Web;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Raporlar.Mezuniyet;
using LisansUstuBasvuruSistemi.Raporlar.TezDanismanOneri;
using LisansUstuBasvuruSistemi.Raporlar.TezIzleme;
using LisansUstuBasvuruSistemi.Raporlar.TezIzlemeJuriOneri;
using LisansUstuBasvuruSistemi.Raporlar.TezOneriSavunma;
using LisansUstuBasvuruSistemi.Raporlar.Yeterlik;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Utilities.MailManager
{
    public class MailReportAttachment
    {
        public static List<Attachment> GetMezuniyetBasvuruRaporuAttachments(int mezuniyetBasvurulariId)
        {
            var rpr = new RprMezuniyetYayinSartiOnayiFormu(mezuniyetBasvurulariId);
            rpr.CreateDocument();
            rpr.DisplayName = "MezuniyetBasvuruFormu_" + Guid.NewGuid().ToString().Substring(0, 5) + ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };

        }
        public static List<Attachment> GetMezuniyetTezTeslimFormuAttachments(int mezuniyetBasvurulariId, bool ilkTezlimOrIkinciTeslim)
        {
            var rpr = new RprMezuniyetTezTeslimFormu_FR0338(mezuniyetBasvurulariId, ilkTezlimOrIkinciTeslim);
            rpr.CreateDocument();
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };

        }

        public static List<Attachment> GetMezuniyetTezDuzeltmeVeJuriUyelerineTezTeslimTutanagiAttachments(int srTalepId)
        {
            var rpr = new RprMezuniyetTezDuzeltmeJuriUyelerineCiltliTezTeslimTutanagi_FR0329_FR0325(srTalepId);
            rpr.CreateDocument();
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }
        public static List<Attachment> GetMezuniyetTezSinavSonucFormuAttachments(int srTalepId, bool showTutanakDetay)
        {
            var rpr = new RprTezSinavSonucTutanagi_FR0342_FR0377(srTalepId);
            rpr.CreateDocument();

            if (showTutanakDetay)
            {
                var rpr2 = new RprTezSinavSonucTutanagi_Detay(srTalepId);
                rpr2.CreateDocument();
                rpr.Pages.AddRange(rpr2.Pages);
            }

            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }
        public static List<Attachment> GetMezuniyetJuriUyelerineTezTeslimFormuAttachments(int mezuniyetJuriOneriFormId)
        {
            var rpr = new RprJuriUyelerineTezTeslimFormu_FR0341_FR0302(mezuniyetJuriOneriFormId);
            rpr.CreateDocument();
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }
        public static List<Attachment> GetMezuniyetTezdenUretilenYayinlariDegerlendirmeFormuAttachments(int mezuniyetJuriOneriFormId, int? mezuniyetJuriOneriFormuJuriId)
        {
            var rpr = new RprMezuniyetTezdenUretilenYayinlariDegerlendirmeFormu_FR0304(mezuniyetJuriOneriFormId, mezuniyetJuriOneriFormuJuriId);
            rpr.CreateDocument();
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }
        public static List<Attachment> GetMezuniyetDoktoraTezDegerlendirmeFormuAttachments(int mezuniyetJuriOneriFormId, int? mezuniyetJuriOneriFormuJuriId)
        {
            var rpr = new RprMezuniyetTezDegerlendirmeFormu_FR0303(mezuniyetJuriOneriFormId, mezuniyetJuriOneriFormuJuriId);
            rpr.CreateDocument();
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }
        public static List<Attachment> GetMezuniyetTezKontrolFormuAttachments(Guid? tezDosyalariRowId, int? mezuniyetBasvurulariTezDosyaId)
        {
            var rpr = new RprMezuniyetTezKontrolFormu(tezDosyalariRowId, mezuniyetBasvurulariTezDosyaId);
            rpr.CreateDocument();
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }

        public static List<Attachment> GetTezIzlemeDegerlendirmeFormuAttachments(int tiBasvuruAraRaporId, bool showDegerlendirmeDetay)
        {
            var rpr = new RprTiDegerlendirmeFormu_FR0307(tiBasvuruAraRaporId);
            rpr.CreateDocument();
            if (showDegerlendirmeDetay)
            {
                var rpr2 = new RprTiDegerlendirmeFormuDetay_FR0307(tiBasvuruAraRaporId);
                rpr2.CreateDocument();
                rpr2.DisplayName += ".pdf";
                rpr.Pages.AddRange(rpr2.Pages);
            }
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }
        public static List<Attachment> GetTezOneriSavunmaFormuAttachments(int toBasvuruSavunmaId, bool showSavunmaDetay)
        {
            var rpr = new RprToSavunmaFormu_FR0348(toBasvuruSavunmaId);
            rpr.CreateDocument();
            if (showSavunmaDetay)
            {
                var rpr2 = new RprToSavunmaFormuDetay_FR0348(toBasvuruSavunmaId);
                rpr2.CreateDocument();
                rpr2.DisplayName += ".pdf";
                rpr.Pages.AddRange(rpr2.Pages);
            }
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }
        public static List<Attachment> GetTezDanismanOneriFormuAttachments(int tdoBasvuruDanismanId)
        {
            var rpr = new RprTezDanismaniOneriFormu_FR0347(tdoBasvuruDanismanId);
            rpr.CreateDocument();
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }
        public static List<Attachment> GetTezDanismanDegisiklikFormuAttachments(int tdoBasvuruDanismanId)
        {
            var rpr = new RprTezDanismaniDegisiklikFormu_FR0308(tdoBasvuruDanismanId);
            rpr.CreateDocument();
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }
        public static List<Attachment> GetTezEsDanismanOneriFormuAttachments(int tdoBasvuruEsDanismanId)
        {
            var rpr = new RprTezEsDanismaniOneriFormu_FR0320(tdoBasvuruEsDanismanId);
            rpr.CreateDocument();
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }
        public static List<Attachment> GetYeterlikDoktoraSinavSonucFormuAttachments(int yeterlikBasvuruId)
        {
            var rpr = new RprDrYeterlikSinavDegerlendirmeFormu_FR1227(yeterlikBasvuruId);
            rpr.CreateDocument();
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }
        public static List<Attachment> GetTezIzlemeJuriOneriFormuAttachments(int tijBasvuruOneriId)
        {
            var rpr = new RprDrYeterlikSinavDegerlendirmeFormu_FR1227(tijBasvuruOneriId);
            rpr.CreateDocument();
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }
        public static List<Attachment> GetTezIzlemeJuriDegisiklikFormuAttachments(int tijBasvuruOneriId)
        {
            var rpr = new RprTijDegisiklikFormu_FR1460(tijBasvuruOneriId);
            rpr.CreateDocument();
            rpr.DisplayName += ".pdf";
            rpr.ExportOptions.Pdf.Compressed = true;
            var memoryStream = new MemoryStream();
            rpr.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var attachment = new Attachment(memoryStream, rpr.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;
            return new List<Attachment> { attachment };
        }

        public static List<Attachment> CopyAttachments(List<Attachment> originalAttachments)
        {
            // Orijinal Attachment dizisini kopyala
            List<Attachment> copiedAttachments = new List<Attachment>();
            foreach (var originalAttachment in originalAttachments)
            {
                var copiedMemoryStream = new MemoryStream();
                originalAttachment.ContentStream.CopyTo(copiedMemoryStream);
                copiedMemoryStream.Seek(0, SeekOrigin.Begin);
                var copiedAttachment = new Attachment(copiedMemoryStream, originalAttachment.Name, originalAttachment.ContentType.MediaType);
                copiedAttachments.Add(copiedAttachment);
            }

            return copiedAttachments;
        }

        
    }
}