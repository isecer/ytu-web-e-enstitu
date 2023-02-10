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
            else
            {
                var kodArr = kod.Split('_').ToList();
                var rowId = new Guid(kodArr[2]);
                var tid = kodArr[1].ToInt();
                if (kodArr[0] == "MBB")
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
                }
                else if (kodArr[0] == "MBBBC")
                {
                    var srTalepleriBezCiltFormu = _entities.SRTalepleriBezCiltFormus.FirstOrDefault(p => p.RowID == rowId && p.SRTalepleriBezCiltFormID == tid);
                    if (srTalepleriBezCiltFormu != null)
                    {
                        RprMezuniyetCiltliTezTeslimFormu_FR1243 rpr = new RprMezuniyetCiltliTezTeslimFormu_FR1243(srTalepleriBezCiltFormu.SRTalepleriBezCiltFormID);
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        rprX = rpr;
                    }
                }
                else if (kodArr[0] == "MZTTF")
                {
                    var mb = _entities.MezuniyetBasvurularis.FirstOrDefault(p => p.TezTeslimUniqueID == rowId && p.MezuniyetBasvurulariID == tid);
                    if (mb != null)
                    {

                        RprMezuniyetTezTeslimFormu_FR0338 rpr = new RprMezuniyetTezTeslimFormu_FR0338(mb.RowID, kodArr[3].ToInt().Value == 1);
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        rprX = rpr;
                    }
                }
                else if (kodArr[0] == "MZTSS")
                {
                    var srTalep = _entities.SRTalepleris.FirstOrDefault(p => p.UniqueID == rowId);
                    if (srTalep != null)
                    {

                        RprTezSinavSonucTutanagi_FR0342_FR0377 rpr = new RprTezSinavSonucTutanagi_FR0342_FR0377(srTalep.UniqueID.Value);
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        rprX = rpr;
                    }
                }
                else if (kodArr[0] == "MBTDO")
                {
                    var mezuniyetBasvurulariTezDosyasi = _entities.MezuniyetBasvurulariTezDosyalaris.FirstOrDefault(p => p.RowID == rowId && p.IsOnaylandiOrDuzeltme == true);
                    if (mezuniyetBasvurulariTezDosyasi != null)
                    {
                        RprMezuniyetTezKontrolFormu rpr = new RprMezuniyetTezKontrolFormu(rowId, null);
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        rprX = rpr;
                    }
                }

                else if (kodArr[0] == "TIDF")
                {
                    var tiAraRaporBasvurusu = _entities.TIBasvuruAraRapors.FirstOrDefault(p => p.UniqueID == rowId);
                    if (tiAraRaporBasvurusu != null)
                    {
                        var rpr = new RprTiDegerlendirmeFormu_FR0307(tiAraRaporBasvurusu.TIBasvuruAraRaporID);
                        rpr.CreateDocument();
                        rpr.DisplayName = rpr.DisplayName + ".pdf";
                        rprX = rpr;
                    }
                }
                else if (kodArr[0] == "TDOF")
                {
                    var tdoBasvuruDanisman = _entities.TDOBasvuruDanismen.FirstOrDefault(p => p.UniqueID == rowId);
                    if (tdoBasvuruDanisman != null)
                    {
                        if (tdoBasvuruDanisman.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniOnerisi)
                        {
                            var rpr = new RprTezDanismaniOneriFormu_FR0347(tdoBasvuruDanisman.TDOBasvuruDanismanID);
                            rpr.CreateDocument();
                            rpr.DisplayName = rpr.DisplayName + ".pdf";
                            rprX = rpr;
                        }
                        else
                        {
                            var rpr = new RprTezDanismaniDegisiklikFormu_FR0308(tdoBasvuruDanisman.TDOBasvuruDanismanID);
                            rpr.CreateDocument();
                            rpr.DisplayName = rpr.DisplayName + ".pdf";
                            rprX = rpr;

                        }
                    }
                }
                else if (kodArr[0] == "TDOEF")
                {
                    var tdoBasvuruEsDanisman = _entities.TDOBasvuruEsDanismen.FirstOrDefault(p => p.UniqueID == rowId);
                    if (tdoBasvuruEsDanisman != null)
                    {
                        var rpr = new RprTezEsDanismaniOneriFormu_FR0320(tdoBasvuruEsDanisman.TDOBasvuruEsDanismanID);
                        rpr.CreateDocument();
                        rpr.DisplayName = rpr.DisplayName + ".pdf";
                        rprX = rpr;
                    }
                }
            }

            return View(rprX);
        }
    }
}