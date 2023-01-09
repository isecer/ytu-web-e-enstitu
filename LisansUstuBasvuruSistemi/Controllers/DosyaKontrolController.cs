using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Raporlar;

namespace LisansUstuBasvuruSistemi.Controllers
{
    public class DosyaKontrolController : Controller
    {
        // GET: DosyaKontrol
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();


        public ActionResult Index(string Kod)
        {
            if (Kod.IsNullOrWhiteSpace()) return RedirectToAction("Index", "Home");
            ViewBag.Kod = Kod;
            return View();
        }

        public ActionResult Reports(string Kod)
        {
            DevExpress.XtraReports.UI.XtraReport RprX = null;
            if (Kod.IsNullOrWhiteSpace()) return RedirectToAction("Index", "Home");
            else
            {
                var KodArr = Kod.Split('_').ToList();
                var RowID = new Guid(KodArr[2]);
                var TID = KodArr[1].ToInt();
                if (KodArr[0] == "MBB")
                {

                    var MezuniyetBasvurusu = db.MezuniyetBasvurularis.Where(p => p.RowID == RowID).FirstOrDefault();
                    if (MezuniyetBasvurusu != null)
                    {
                        rprMezuniyetYayinSartiOnayiFormu rpr = new rprMezuniyetYayinSartiOnayiFormu(MezuniyetBasvurusu.MezuniyetBasvurulariID);
                        rpr.DisplayName = "Mezuniyet Başvuru Bilgisi";
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        RprX = rpr;
                    }
                }
                else if (KodArr[0] == "MBBBC")
                {
                    var SRTalepleriBezCiltFormu = db.SRTalepleriBezCiltFormus.Where(p => p.RowID == RowID && p.SRTalepleriBezCiltFormID == TID).FirstOrDefault();
                    if (SRTalepleriBezCiltFormu != null)
                    {
                        rprMezuniyetCiltliTezTeslimFormu_FR1243 rpr = new rprMezuniyetCiltliTezTeslimFormu_FR1243(SRTalepleriBezCiltFormu.SRTalepleriBezCiltFormID);
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        RprX = rpr;
                    }
                }
                else if (KodArr[0] == "MZTTF")
                {
                    var MB = db.MezuniyetBasvurularis.Where(p => p.TezTeslimUniqueID == RowID && p.MezuniyetBasvurulariID == TID).FirstOrDefault();
                    if (MB != null)
                    {

                        rprMezuniyetTezTeslimFormu_FR0338 rpr = new rprMezuniyetTezTeslimFormu_FR0338(MB.RowID, KodArr[3].ToInt().Value == 1);
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        RprX = rpr;
                    }
                }
                else if (KodArr[0] == "MZTSS")
                {
                    var SrTalep = db.SRTalepleris.Where(p => p.UniqueID == RowID).FirstOrDefault();
                    if (SrTalep != null)
                    {

                        rprTezSinavSonucTutanagi_FR0342_FR0377 rpr = new rprTezSinavSonucTutanagi_FR0342_FR0377(SrTalep.UniqueID.Value);
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        RprX = rpr;
                    }
                }
                else if (KodArr[0] == "MBTDO")
                {
                    var MezuniyetBasvurulariTezDosyasi = db.MezuniyetBasvurulariTezDosyalaris.Where(p => p.RowID == RowID && p.IsOnaylandiOrDuzeltme == true).FirstOrDefault();
                    if (MezuniyetBasvurulariTezDosyasi != null)
                    {
                        rprMezuniyetTezKontrolFormu rpr = new rprMezuniyetTezKontrolFormu(RowID, null);
                        rpr.PrintingSystem.ContinuousPageNumbering = true;
                        rpr.ExportOptions.Xlsx.ExportMode = DevExpress.XtraPrinting.XlsxExportMode.SingleFile;
                        RprX = rpr;
                    }
                }

                else if (KodArr[0] == "TIDF")
                {
                    var TIAraRaporBasvurusu = db.TIBasvuruAraRapors.Where(p => p.UniqueID == RowID).FirstOrDefault();
                    if (TIAraRaporBasvurusu != null)
                    {
                        var rpr = new rprTIDegerlendirmeFormu_FR0307(TIAraRaporBasvurusu.TIBasvuruAraRaporID);
                        rpr.CreateDocument();
                        rpr.DisplayName = rpr.DisplayName + ".pdf";
                        RprX = rpr;
                    }
                }
                else if (KodArr[0] == "TDOF")
                {
                    var TDOBasvuruDanisman = db.TDOBasvuruDanismen.Where(p => p.UniqueID == RowID).FirstOrDefault();
                    if (TDOBasvuruDanisman != null)
                    {
                        if (TDOBasvuruDanisman.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniOnerisi)
                        {
                            var rpr = new rprTezDanismaniOneriFormu_FR0347(TDOBasvuruDanisman.TDOBasvuruDanismanID);
                            rpr.CreateDocument();
                            rpr.DisplayName = rpr.DisplayName + ".pdf";
                            RprX = rpr;
                        }
                        else
                        {
                            var rpr = new rprTezDanismaniDegisiklikFormu_FR0308(TDOBasvuruDanisman.TDOBasvuruDanismanID);
                            rpr.CreateDocument();
                            rpr.DisplayName = rpr.DisplayName + ".pdf";
                            RprX = rpr;

                        }
                    }
                }
                else if (KodArr[0] == "TDOEF")
                {
                    var TDOBasvuruEsDanisman = db.TDOBasvuruEsDanismen.Where(p => p.UniqueID == RowID).FirstOrDefault();
                    if (TDOBasvuruEsDanisman != null)
                    {
                        var rpr = new rprTezEsDanismaniOneriFormu_FR0320(TDOBasvuruEsDanisman.TDOBasvuruDanismanID);
                        rpr.CreateDocument();
                        rpr.DisplayName = rpr.DisplayName + ".pdf";
                        RprX = rpr;
                    }
                }
            }

            return View(RprX);
        }
    }
}