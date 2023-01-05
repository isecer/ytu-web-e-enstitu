using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.FilterModel;
using BiskaUtil;
using System.Linq;
using System.Collections.Generic;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprMezuniyetTezJuriOneriFormu_FR0300_FR0339 : DevExpress.XtraReports.UI.XtraReport
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public rprMezuniyetTezJuriOneriFormu_FR0300_FR0339(int MezuniyetBasvurulariID)
        {
            InitializeComponent();



            var MBasvuru = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == MezuniyetBasvurulariID).First();
            bool IsDrOrYL = MBasvuru.OgrenimTipKod == OgrenimTipi.Doktra;
            var mezuniyetJuriOneriFormu = MBasvuru.MezuniyetJuriOneriFormlaris.FirstOrDefault();

            var cells = new List<XRTableCell>();
            var xrTable = new List<XRTable> { tableYtuIciJuri, tableYtuDisiJuri, tableDilSinav1, tableDilSinav2, tableDilSinav3, tableDilSinav4, tableDilSinav5, tableDilSinav6, tableDilSinav7, tableDilSinav8, tableDilSinav9, tableDilSinav10, tableDilSinav11 };
            foreach (var item in xrTable)
            {
                foreach (var itemR in item.Rows)
                {
                    var Rw = (XRTableRow)itemR;
                    foreach (var itemC in Rw.Cells)
                    {
                        var cel = (XRTableCell)itemC;
                        cells.Add(cel);
                    }
                }
            }
            if (mezuniyetJuriOneriFormu != null)
            {
                txtFormKodu.Text = "Form Kodu: " + mezuniyetJuriOneriFormu.UniqueID;
                var Program = MBasvuru.Programlar;
                var AbdAdi = Program.AnabilimDallari;
                txtAnabilimdaliProgramAdi.Text = AbdAdi.AnabilimDaliAdi + " - " + Program.ProgramAdi;
                txtAdSoyad.Text = MBasvuru.Ad + " " + MBasvuru.Soyad;
                txtNumara.Text = MBasvuru.OgrenciNo;
                chkIsTurkce.Checked = MBasvuru.IsTezDiliTr == true;
                chkIsEnglish.Checked = MBasvuru.IsTezDiliTr == false;

                cellTezBaslikTr.Text = MBasvuru.TezBaslikTr;
                cellTezBaslikEn.Text = MBasvuru.TezBaslikEn;

                cellYeniTezBaslikTr.Text = mezuniyetJuriOneriFormu.YeniTezBaslikTr;
                cellYeniTezBaslikEn.Text = mezuniyetJuriOneriFormu.YeniTezBaslikEn;


                TbRowYeniTB.Visible = !mezuniyetJuriOneriFormu.YeniTezBaslikTr.IsNullOrWhiteSpace();
                TbRowYeniTBCeviri.Visible = !mezuniyetJuriOneriFormu.YeniTezBaslikEn.IsNullOrWhiteSpace();
                if (MBasvuru.MezuniyetSureci.EnstituKod == EnstituKodlari.FenBilimleri)
                {
                    txtMudurlukAdiTr.Text = "FEN BİLİMLERİ ENSTİTÜSÜ MÜDÜRLÜĞÜNE,";
                    txtMudurlukAdiEn.Text = "THE GRADUATE SCHOOL OF NATURAL and APPLIED SCIENCE";
                }
                else
                {
                    txtMudurlukAdiTr.Text = "SOSYAL BİLİMLERİ ENSTİTÜSÜ MÜDÜRLÜĞÜNE,";
                    txtMudurlukAdiEn.Text = "THE GRADUATE SCHOOL OF SOCIAL SCIENCE";
                }

                var RowIDs = new List<string>();
                if (IsDrOrYL)
                {
                    RowIDs = new List<string> { "TezDanismani", "TikUyesi1", "TikUyesi2", "YtuIciJuri1", "YtuIciJuri2", "YtuIciJuri3", "YtuIciJuri4", "YtuDisiJuri1", "YtuDisiJuri2", "YtuDisiJuri3", "YtuDisiJuri4" };
                    TbRowTik1.Visible = true;
                    TbRowTik2.Visible = true;

                    lblFormNo.Text = "(Form No: FR-0300; Revizyon Tarihi: 29.03.2017; Revizyon No: 07)";
                    txtFormAdiEn.Text = "DOCTORAL DISSERTATION COMMITTEE PROPOSPAL FORM";
                    txtFormAdiTr.Text = "DOKTORA TEZ JÜRİ ÖNERİ FORMU";
                    txtYUiciAciklama.Text = "Yıldız Teknik Üniversitesi İçinden Jüri Adayı Önerileri (Tik Haricinde)";
                    txtYUiciAciklamaEn.Text = "YTU Faculty - Committee Member Propospals (Different than TMC)";
                    txtYUDisiAciklama.Text = "Yıldız Teknik Üniversitesi Dışından Jüri Adayı Önerileri (Tik Haricinde)";
                    txtYUDisiAciklamaEn.Text = "non-YTU Faculty - Committee Member Propospals (Different than TMC)";
                   
                    cellAcklama1.Text = "Yukarıda adı yazılı doktora öğrencisinin, sınavını yapmak üzere oluşturulacak jüri önerimiz aşağıda belirtilmektedir.\r\nGereği için bilgilerinize arz ederim. Saygılarımla.";
                    CellAcklama1En.Text = "The proposal of the committee that will be formed to test the above mentioned doctoral student is stated below. \r\nI respectfully submit for your consideration.";
                    this.DisplayName = (MBasvuru.Ad + " " + MBasvuru.Soyad) + " FR-0300 Doktora Tez Jüri Öneri Formu";

                }
                else
                {
                    RowIDs = new List<string> { "TezDanismani", "YtuIciJuri1", "YtuIciJuri2", "YtuIciJuri3", "YtuIciJuri4", "YtuDisiJuri1", "YtuDisiJuri2", "YtuDisiJuri3", "YtuDisiJuri4" };
                    TbRowTik1.Visible = false;
                    TbRowTik2.Visible = false;
                    lblFormNo.Text = "(Form No: FR-0339; Revizyon Tarihi: 15.05.2018; Revizyon No: 07)";
                    txtFormAdiEn.Text = "MASTER'S THESIS COMMITTEE PROPOSPAL FORM";
                    txtFormAdiTr.Text = "YÜKSEK LİSANS TEZ JÜRİ ÖNERİ FORMU";
                    txtYUiciAciklama.Text = "Yıldız Teknik Üniversitesi İçinden Jüri Adayı Önerileri (Anabilim Dalı İçerisinden Olmalı)";
                    txtYUiciAciklamaEn.Text = "YTU Faculty - Committee Member Propospals (Obligatorily from the Department)";
                    txtYUDisiAciklama.Text = "Yıldız Teknik Üniversitesi Dışından Jüri Adayı Önerileri";
                    txtYUDisiAciklamaEn.Text = "non-YTU Faculty - Committee Member Propospals";
                   
                    cellAcklama1.Text = "Yukarıda adı yazılı yüksek lisans öğrencisinin, sınavını yapmak üzere oluşturulacak jüri önerimiz aşağıda belirtilmektedir. \r\nGereği için bilgilerinize arz ederim. Saygılarımla.";
                    CellAcklama1En.Text = "The proposal of the committee that will be formed to test the above-mentioned master’s student is stated below. \r\nI respectfully submit for your consideration.";
                    this.DisplayName = (MBasvuru.Ad + " " + MBasvuru.Soyad) + " FR-0339 Yüksek Lisans Tez Jüri Öneri Formu";
                }

                foreach (var item in RowIDs)
                {
                    var itmData = mezuniyetJuriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi == item).FirstOrDefault();
                    if (itmData != null)
                    {

                        if (itmData.UniversiteID.HasValue) itmData.UniversiteAdi = itmData.Universiteler.KisaAd;

                        var UnvanAdi = cells.Where(p => p.Name == "txt" + item + "UnvanAdi").FirstOrDefault();
                        if (UnvanAdi != null) UnvanAdi.Text = itmData.UnvanAdi;

                        var AdSoyad = cells.Where(p => p.Name == "txt" + item + "AdSoyad").FirstOrDefault();
                        if (AdSoyad != null) AdSoyad.Text = itmData.AdSoyad;
                        var EMail = cells.Where(p => p.Name == "txt" + item + "EMail").FirstOrDefault();
                        if (EMail != null) EMail.Text = itmData.EMail;
                        var UniversiteAdi = cells.Where(p => p.Name == "txt" + item + "UniversiteAdi").FirstOrDefault();
                        if (UniversiteAdi != null) UniversiteAdi.Text = itmData.UniversiteAdi;
                        var AnabilimdaliProgramAdi = cells.Where(p => p.Name == "txt" + item + "AnabilimdaliProgramAdi").FirstOrDefault();
                        if (AnabilimdaliProgramAdi != null) AnabilimdaliProgramAdi.Text = itmData.AnabilimdaliProgramAdi;
                        var UzmanlikAlani = cells.Where(p => p.Name == "txt" + item + "UzmanlikAlani").FirstOrDefault();
                        if (UzmanlikAlani != null) UzmanlikAlani.Text = itmData.UzmanlikAlani;
                        var BilimselCalismalarAnahtarSozcukler = cells.Where(p => p.Name == "txt" + item + "BilimselCalismalarAnahtarSozcukler").FirstOrDefault();
                        if (BilimselCalismalarAnahtarSozcukler != null) BilimselCalismalarAnahtarSozcukler.Text = itmData.BilimselCalismalarAnahtarSozcukler;
                        var DilSinavAdi = cells.Where(p => p.Name == "txt" + item + "DilSinavAdi").FirstOrDefault();
                        if (DilSinavAdi != null) DilSinavAdi.Text = itmData.DilSinavAdi;
                        var DilPuani = cells.Where(p => p.Name == "txt" + item + "DilPuani").FirstOrDefault();
                        if (DilPuani != null) DilPuani.Text = itmData.DilPuani;

                    }
                }
            }


        }

    }
}
