using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Raporlar.DonemProjesi;
using LisansUstuBasvuruSistemi.Raporlar.Mezuniyet;
using LisansUstuBasvuruSistemi.Raporlar.TezDanismanOneri;
using LisansUstuBasvuruSistemi.Raporlar.TezIzleme;
using LisansUstuBasvuruSistemi.Raporlar.TezIzlemeJuriOneri;
using LisansUstuBasvuruSistemi.Raporlar.TezOneriSavunma;
using LisansUstuBasvuruSistemi.Raporlar.Yeterlik;

namespace LisansUstuBasvuruSistemi.Utilities.MailManager
{
    public class MailReportAttachment
    {
        public static List<Attachment> GetMezuniyetBasvuruRaporuAttachments(int mezuniyetBasvurulariId)
        {
            return CreateReportToMailAttachment(new RprMezuniyetYayinSartiOnayiFormu(mezuniyetBasvurulariId));
        }
        public static List<Attachment> GetMezuniyetTezTeslimFormuAttachments(int mezuniyetBasvurulariId, bool ilkTezlimOrIkinciTeslim)
        {
            return CreateReportToMailAttachment(new RprMezuniyetTezTeslimFormu_FR0338(mezuniyetBasvurulariId, ilkTezlimOrIkinciTeslim));
        }
        public static List<Attachment> GetMezuniyetTezDuzeltmeVeJuriUyelerineTezTeslimTutanagiAttachments(int srTalepId)
        {
            return CreateReportToMailAttachment(new RprMezuniyetTezDuzeltmeJuriUyelerineCiltliTezTeslimTutanagi_FR0329_FR0325(srTalepId));
        }
        public static List<Attachment> GetMezuniyetTezSinavSonucFormuAttachments(int srTalepId, bool showTutanakDetay)
        {
            if (showTutanakDetay)
            {
                return CreateReportToMailAttachment(new RprTezSinavSonucTutanagi_FR0342_FR0377(srTalepId),
                    new RprTezSinavSonucTutanagi_Detay(srTalepId));
            }
            return CreateReportToMailAttachment(new RprTezSinavSonucTutanagi_FR0342_FR0377(srTalepId));
        }
        public static List<Attachment> GetMezuniyetJuriUyelerineTezTeslimFormuAttachments(int mezuniyetJuriOneriFormId)
        {
            return CreateReportToMailAttachment(new RprJuriUyelerineTezTeslimFormu_FR0341_FR0302(mezuniyetJuriOneriFormId));
        }
        public static List<Attachment> GetMezuniyetTezdenUretilenYayinlariDegerlendirmeFormuAttachments(int mezuniyetJuriOneriFormId, int? mezuniyetJuriOneriFormuJuriId)
        {
            return CreateReportToMailAttachment(new RprMezuniyetTezdenUretilenYayinlariDegerlendirmeFormu_FR0304(mezuniyetJuriOneriFormId, mezuniyetJuriOneriFormuJuriId));
        }
        public static List<Attachment> GetMezuniyetDoktoraTezDegerlendirmeFormuAttachments(int mezuniyetJuriOneriFormId, int? mezuniyetJuriOneriFormuJuriId)
        {
            return CreateReportToMailAttachment(new RprMezuniyetTezDegerlendirmeFormu_FR0303(mezuniyetJuriOneriFormId, mezuniyetJuriOneriFormuJuriId));
        }
        public static List<Attachment> GetMezuniyetTezKontrolFormuAttachments(Guid? tezDosyalariRowId, int? mezuniyetBasvurulariTezDosyaId)
        {
            return CreateReportToMailAttachment(new RprMezuniyetTezKontrolFormu(tezDosyalariRowId, mezuniyetBasvurulariTezDosyaId));
        }
        public static List<Attachment> GetTezIzlemeDegerlendirmeFormuAttachments(int tiBasvuruAraRaporId, bool showDegerlendirmeDetay)
        {
            if (showDegerlendirmeDetay)
            {
                return CreateReportToMailAttachment(new RprTiDegerlendirmeFormu_FR0307(tiBasvuruAraRaporId),
                    new RprTiDegerlendirmeFormuDetay_FR0307(tiBasvuruAraRaporId));
            }
            return CreateReportToMailAttachment(new RprTiDegerlendirmeFormu_FR0307(tiBasvuruAraRaporId));
        }
        public static List<Attachment> GetTezOneriSavunmaFormuAttachments(int toBasvuruSavunmaId, bool showSavunmaDetay)
        {
            if (showSavunmaDetay)
            {
                return CreateReportToMailAttachment(new RprToSavunmaFormu_FR0348(toBasvuruSavunmaId),
                    new RprToSavunmaFormuDetay_FR0348(toBasvuruSavunmaId));
            }
            return CreateReportToMailAttachment(new RprToSavunmaFormu_FR0348(toBasvuruSavunmaId));
        }
        public static List<Attachment> GetTezDanismanOneriFormuAttachments(int tdoBasvuruDanismanId)
        {
            return CreateReportToMailAttachment(new RprTezDanismaniOneriFormu_FR0347(tdoBasvuruDanismanId));
        }
        public static List<Attachment> GetTezDanismanDegisiklikFormuAttachments(int tdoBasvuruDanismanId)
        {
            return CreateReportToMailAttachment(new RprTezDanismaniDegisiklikFormu_FR0308(tdoBasvuruDanismanId));
        }
        public static List<Attachment> GetTezEsDanismanOneriFormuAttachments(int tdoBasvuruEsDanismanId)
        {
            return CreateReportToMailAttachment(new RprTezEsDanismaniOneriFormu_FR0320(tdoBasvuruEsDanismanId));
        }
        public static List<Attachment> GetYeterlikDoktoraSinavSonucFormuAttachments(int yeterlikBasvuruId)
        {
            return CreateReportToMailAttachment(new RprDrYeterlikSinavDegerlendirmeFormu_FR1227(yeterlikBasvuruId));
        }
        public static List<Attachment> GetTezIzlemeJuriOneriFormuAttachments(int tijBasvuruOneriId)
        {
            return CreateReportToMailAttachment(new RprDrYeterlikSinavDegerlendirmeFormu_FR1227(tijBasvuruOneriId));
        }
        public static List<Attachment> GetTezIzlemeJuriDegisiklikFormuAttachments(int tijBasvuruOneriId)
        {
            return CreateReportToMailAttachment(new RprTijDegisiklikFormu_FR1460(tijBasvuruOneriId));
        }
        public static List<Attachment> GetDpSinavTutanagiAttachments(int donemProjesiBasvuruId, bool showTutanakDetay)
        {
            if (showTutanakDetay)
            {
                return CreateReportToMailAttachment(new RprDpSinavTutanakFormu_FR0366(donemProjesiBasvuruId),
                    new RprDpSinavTutanakFormuDetay_FR0366(donemProjesiBasvuruId));
            }
            return CreateReportToMailAttachment(new RprDpSinavTutanakFormu_FR0366(donemProjesiBasvuruId));
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

        private static List<Attachment> CreateReportToMailAttachment(params XtraReport[] reports)
        {
            if (reports == null || reports.Length == 0)
            {
                throw new ArgumentException("En az bir rapor belirtilmelidir.", nameof(reports));
            }
            var masterReport = reports[0];
            masterReport.CreateDocument();
            for (var i = 1; i < reports.Length; i++)
            {
                var subReport = reports[i];
                subReport.CreateDocument();
                masterReport.Pages.AddRange(subReport.Pages);
            }
            masterReport.DisplayName += ".pdf";
            masterReport.ExportOptions.Pdf.Compressed = true;

            var memoryStream = new MemoryStream();
            masterReport.ExportToPdf(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var attachment = new Attachment(memoryStream, masterReport.DisplayName, "application/pdf");
            attachment.ContentDisposition.ModificationDate = DateTime.Now;

            return new List<Attachment> { attachment };
        }
    }
}