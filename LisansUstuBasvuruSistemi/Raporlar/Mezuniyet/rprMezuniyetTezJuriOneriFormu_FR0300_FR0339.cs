using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{
    public partial class RprMezuniyetTezJuriOneriFormu_FR0300_FR0339 : DevExpress.XtraReports.UI.XtraReport
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public RprMezuniyetTezJuriOneriFormu_FR0300_FR0339(int mezuniyetBasvurulariId)
        {
            InitializeComponent();



            var mBasvuru = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
            bool isDrOrYl = mBasvuru.OgrenimTipKod.IsDoktora();
            var mezuniyetJuriOneriFormu = mBasvuru.MezuniyetJuriOneriFormlaris.FirstOrDefault();

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
                var program = mBasvuru.Programlar;
                var abdAdi = program.AnabilimDallari;
                txtAnabilimdaliProgramAdi.Text = abdAdi.AnabilimDaliAdi + " - " + program.ProgramAdi;
                txtAdSoyad.Text = mBasvuru.Ad + " " + mBasvuru.Soyad;
                txtNumara.Text = mBasvuru.OgrenciNo;
                chkIsTurkce.Checked = mBasvuru.IsTezDiliTr == true;
                chkIsEnglish.Checked = mBasvuru.IsTezDiliTr == false;

                cellTezBaslikTr.Text = mBasvuru.TezBaslikTr;
                cellTezBaslikEn.Text = mBasvuru.TezBaslikEn;

                cellYeniTezBaslikTr.Text = mezuniyetJuriOneriFormu.YeniTezBaslikTr;
                cellYeniTezBaslikEn.Text = mezuniyetJuriOneriFormu.YeniTezBaslikEn;


                TbRowYeniTB.Visible = !mezuniyetJuriOneriFormu.YeniTezBaslikTr.IsNullOrWhiteSpace();
                TbRowYeniTBCeviri.Visible = !mezuniyetJuriOneriFormu.YeniTezBaslikEn.IsNullOrWhiteSpace();
                if (mBasvuru.MezuniyetSureci.EnstituKod == EnstituKodlariEnum.FenBilimleri)
                {
                    txtMudurlukAdiTr.Text = "FEN BİLİMLERİ ENSTİTÜSÜ MÜDÜRLÜĞÜNE,";
                    txtMudurlukAdiEn.Text = "THE GRADUATE SCHOOL OF NATURAL and APPLIED SCIENCE";
                }
                else if (mBasvuru.MezuniyetSureci.EnstituKod == EnstituKodlariEnum.SosyalBilimleri)
                {
                    txtMudurlukAdiTr.Text = "SOSYAL BİLİMLERİ ENSTİTÜSÜ MÜDÜRLÜĞÜNE,";
                    txtMudurlukAdiEn.Text = "THE GRADUATE SCHOOL OF SOCIAL SCIENCE";
                }
                if (mBasvuru.MezuniyetSureci.EnstituKod == EnstituKodlariEnum.TemizEnerjiTeknolojileri)
                {
                    txtMudurlukAdiTr.Text = "TEMİZ ENERJİ TEKNOLOJİLERİ ENSTİTÜSÜ MÜDÜRLÜĞÜNE,";
                    txtMudurlukAdiEn.Text = "TEMİZ ENERJİ TEKNOLOJİLERİ ENSTİTÜSÜ";
                }

                List<string> RowIDs;
                if (isDrOrYl)
                {
                    RowIDs = new List<string> { "TezDanismani", "TikUyesi1", "TikUyesi2", "YtuIciJuri1", "YtuIciJuri2", "YtuIciJuri3", "YtuIciJuri4", "YtuDisiJuri1", "YtuDisiJuri2", "YtuDisiJuri3", "YtuDisiJuri4" };
                    TbRowTik1.Visible = true;
                    TbRowTik2.Visible = true;

                    lblFormNo.Text = "(Form No: FR-0300; Revizyon Tarihi: 29.03.2017; Revizyon No: 07)";
                    txtFormAdiEn.Text = "DOCTORAL DISSERTATION COMMITTEE PROPOSPAL FORM";
                    txtFormAdiTr.Text = "DOKTORA TEZ JÜRİ ÖNERİ FORMU";
                    txtYUiciAciklama.Text = "Yıldız Teknik Üniversitesi İçinden Jüri Adayı Önerileri (Tik Haricinde)";
                    txtYUiciAciklamaEn.Text = "YTÜ Faculty - Committee Member Propospals (Different than TMC)";
                    txtYUDisiAciklama.Text = "Yıldız Teknik Üniversitesi Dışından Jüri Adayı Önerileri (Tik Haricinde)";
                    txtYUDisiAciklamaEn.Text = "non-YTÜ Faculty - Committee Member Propospals (Different than TMC)";

                    cellAcklama1.Text = "Yukarıda adı yazılı doktora öğrencisinin, sınavını yapmak üzere oluşturulacak jüri önerimiz aşağıda belirtilmektedir.\r\nGereği için bilgilerinize arz ederim. Saygılarımla.";
                    CellAcklama1En.Text = "The proposal of the committee that will be formed to test the above mentioned doctoral student is stated below. \r\nI respectfully submit for your consideration.";
                    this.DisplayName = (mBasvuru.Ad + " " + mBasvuru.Soyad) + " FR-0300 Doktora Tez Jüri Öneri Formu";

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
                    txtYUiciAciklamaEn.Text = "YTÜ Faculty - Committee Member Propospals (Obligatorily from the Department)";
                    txtYUDisiAciklama.Text = "Yıldız Teknik Üniversitesi Dışından Jüri Adayı Önerileri";
                    txtYUDisiAciklamaEn.Text = "non-YTÜ Faculty - Committee Member Propospals";

                    cellAcklama1.Text = "Yukarıda adı yazılı yüksek lisans öğrencisinin, sınavını yapmak üzere oluşturulacak jüri önerimiz aşağıda belirtilmektedir. \r\nGereği için bilgilerinize arz ederim. Saygılarımla.";
                    CellAcklama1En.Text = "The proposal of the committee that will be formed to test the above-mentioned master’s student is stated below. \r\nI respectfully submit for your consideration.";
                    this.DisplayName = (mBasvuru.Ad + " " + mBasvuru.Soyad) + " FR-0339 Yüksek Lisans Tez Jüri Öneri Formu";
                }

                foreach (var item in RowIDs)
                {
                    var itmData = mezuniyetJuriOneriFormu.MezuniyetJuriOneriFormuJurileris.FirstOrDefault(p => p.JuriTipAdi == item);
                    if (itmData != null)
                    {

                        if (itmData.UniversiteID.HasValue) itmData.UniversiteAdi = itmData.Universiteler.KisaAd;

                        var unvanAdi = cells.FirstOrDefault(p => p.Name == "txt" + item + "UnvanAdi");
                        if (unvanAdi != null) unvanAdi.Text = itmData.UnvanAdi;

                        var adSoyad = cells.FirstOrDefault(p => p.Name == "txt" + item + "AdSoyad");
                        if (adSoyad != null) adSoyad.Text = itmData.AdSoyad;
                        var eMail = cells.FirstOrDefault(p => p.Name == "txt" + item + "EMail");
                        if (eMail != null) eMail.Text = itmData.EMail;
                        var universiteAdi = cells.FirstOrDefault(p => p.Name == "txt" + item + "UniversiteAdi");
                        if (universiteAdi != null) universiteAdi.Text = itmData.UniversiteAdi;
                        var anabilimdaliProgramAdi = cells.FirstOrDefault(p => p.Name == "txt" + item + "AnabilimdaliProgramAdi");
                        if (anabilimdaliProgramAdi != null) anabilimdaliProgramAdi.Text = itmData.AnabilimdaliProgramAdi;
                        var uzmanlikAlani = cells.FirstOrDefault(p => p.Name == "txt" + item + "UzmanlikAlani");
                        if (uzmanlikAlani != null) uzmanlikAlani.Text = itmData.UzmanlikAlani;
                        var bilimselCalismalarAnahtarSozcukler = cells.FirstOrDefault(p => p.Name == "txt" + item + "BilimselCalismalarAnahtarSozcukler");
                        if (bilimselCalismalarAnahtarSozcukler != null) bilimselCalismalarAnahtarSozcukler.Text = itmData.BilimselCalismalarAnahtarSozcukler;
                        var dilSinavAdi = cells.FirstOrDefault(p => p.Name == "txt" + item + "DilSinavAdi");
                        if (dilSinavAdi != null) dilSinavAdi.Text = itmData.DilSinavAdi;
                        var dilPuani = cells.FirstOrDefault(p => p.Name == "txt" + item + "DilPuani");
                        if (dilPuani != null) dilPuani.Text = itmData.DilPuani;

                    }
                }
            }


        }

    }
}
