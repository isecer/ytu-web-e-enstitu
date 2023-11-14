using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Raporlar;
using LisansUstuBasvuruSistemi.Raporlar.Mezuniyet;
using LisansUstuBasvuruSistemi.Raporlar.TezDanismanOneri;
using LisansUstuBasvuruSistemi.Raporlar.TezIzleme;
using LisansUstuBasvuruSistemi.Raporlar.TezOneriSavunma;
using LisansUstuBasvuruSistemi.Raporlar.Yeterlik;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Controllers
{
    public class DosyaKontrolController : Controller
    {
        // GET: DosyaKontrol
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();


        public ActionResult Index(string kod)
        {
            if (kod.IsNullOrWhiteSpace()) return RedirectToAction("Index", "Home");
            ViewBag.Kod = kod;
            return View();
        }

        public ActionResult Reports(string kod)
        {
            DevExpress.XtraReports.UI.XtraReport rprX = null;
            if (kod.IsNullOrWhiteSpace()) return RedirectToAction("Index", "Home");
            var kodArr = kod.Split('_').ToList();
            var rowId = new Guid(kodArr[2]);
            var tid = kodArr[1].ToInt();
            switch (kodArr[0])
            {
                case "MBB":
                {
                    var mezuniyetBasvurusu = _entities.MezuniyetBasvurularis.FirstOrDefault(p => p.RowID == rowId);
                    if (mezuniyetBasvurusu != null)
                    {
                        RprMezuniyetYayinSartiOnayiFormu rpr = new RprMezuniyetYayinSartiOnayiFormu(mezuniyetBasvurusu.MezuniyetBasvurulariID);
                        rpr.DisplayName = "Mezuniyet Başvuru Bilgisi";
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        rprX = rpr;
                    }

                    break;
                }
                case "MBBBC":
                {
                    var srTalepleriBezCiltFormu = _entities.MezuniyetBasvurulariTezTeslimFormlaris.FirstOrDefault(p => p.RowID == rowId && p.MezuniyetBasvurulariTezTeslimFormID == tid);
                    if (srTalepleriBezCiltFormu != null)
                    {
                        RprMezuniyetCiltliTezTeslimFormu_FR1243 rpr = new RprMezuniyetCiltliTezTeslimFormu_FR1243(srTalepleriBezCiltFormu.MezuniyetBasvurulariTezTeslimFormID);
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        rprX = rpr;
                    }

                    break;
                }
                case "MZTTF":
                {
                    var mb = _entities.MezuniyetBasvurularis.FirstOrDefault(p => p.TezTeslimUniqueID == rowId && p.MezuniyetBasvurulariID == tid);
                    if (mb != null)
                    {

                        RprMezuniyetTezTeslimFormu_FR0338 rpr = new RprMezuniyetTezTeslimFormu_FR0338(mb.RowID, kodArr[3].ToInt(0) == 1);
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        rprX = rpr;
                    }

                    break;
                }
                case "MZTSS":
                {
                    var srTalepAny = _entities.SRTalepleris.Any(p => p.UniqueID == rowId);
                    if (srTalepAny)
                    {

                        RprTezSinavSonucTutanagi_FR0342_FR0377 rpr = new RprTezSinavSonucTutanagi_FR0342_FR0377(rowId);
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        rprX = rpr;
                    }

                    break;
                }
                case "MBTDO":
                {
                    var mezuniyetBasvurulariTezDosyasiAyn = _entities.MezuniyetBasvurulariTezDosyalaris.Any(p => p.RowID == rowId && p.IsOnaylandiOrDuzeltme == true);
                    if (mezuniyetBasvurulariTezDosyasiAyn)
                    {
                        RprMezuniyetTezKontrolFormu rpr = new RprMezuniyetTezKontrolFormu(rowId, null);
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        rprX = rpr;
                    }

                    break;
                }
                case "TIDF":
                {
                    var tiAraRaporBasvurusu = _entities.TIBasvuruAraRapors.FirstOrDefault(p => p.UniqueID == rowId);
                    if (tiAraRaporBasvurusu != null)
                    {
                        var rpr = new RprTiDegerlendirmeFormu_FR0307(tiAraRaporBasvurusu.TIBasvuruAraRaporID);
                        rpr.CreateDocument();
                        rpr.DisplayName += ".pdf";
                        rprX = rpr;
                    }

                    break;
                }
                case "TOSF":
                {
                    var toBasvuruSavunmas = _entities.ToBasvuruSavunmas.FirstOrDefault(p => p.UniqueID == rowId);
                    if (toBasvuruSavunmas != null)
                    {
                        var rpr = new RprToSavunmaFormu_FR0348(toBasvuruSavunmas.ToBasvuruSavunmaID);
                        rpr.CreateDocument();
                        rpr.DisplayName += ".pdf";
                        rprX = rpr;
                    }

                    break;
                }
                case "TDOF":
                {
                    var tdoBasvuruDanisman = _entities.TDOBasvuruDanismen.FirstOrDefault(p => p.UniqueID == rowId);
                    if (tdoBasvuruDanisman != null)
                    {
                        if (tdoBasvuruDanisman.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezDanismaniOnerisi)
                        {
                            var rpr = new RprTezDanismaniOneriFormu_FR0347(tdoBasvuruDanisman.TDOBasvuruDanismanID);
                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            rprX = rpr;
                        }
                        else
                        {
                            var rpr = new RprTezDanismaniDegisiklikFormu_FR0308(tdoBasvuruDanisman.TDOBasvuruDanismanID);
                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            rprX = rpr;

                        }
                    }

                    break;
                }
                case "TDOEF":
                {
                    var tdoBasvuruEsDanisman = _entities.TDOBasvuruEsDanismen.FirstOrDefault(p => p.UniqueID == rowId);
                    if (tdoBasvuruEsDanisman != null)
                    {
                        var rpr = new RprTezEsDanismaniOneriFormu_FR0320(tdoBasvuruEsDanisman.TDOBasvuruEsDanismanID);
                        rpr.CreateDocument();
                        rpr.DisplayName += ".pdf";
                        rprX = rpr;
                    }

                    break;
                }
                case "YETB":
                {
                    var yeterlikBasvuru = _entities.YeterlikBasvurus.FirstOrDefault(p => p.UniqueID == rowId);
                    if (yeterlikBasvuru != null)
                    {
                        var rpr = new RprDrYeterlikSinavDegerlendirmeFormu_FR1227(yeterlikBasvuru.YeterlikBasvuruID);
                        rpr.CreateDocument();
                        rpr.DisplayName += ".pdf";
                        rprX = rpr;
                    }

                    break;
                }
            }

            return View(rprX);
        }
    }
}