using System.Linq;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{
    public partial class RprMezuniyetCiltliTezTeslimFormu_FR1243 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprMezuniyetCiltliTezTeslimFormu_FR1243(int id)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {


                var data = (from s in db.MezuniyetBasvurulariTezTeslimFormlaris 
                            join mb in db.MezuniyetBasvurularis on s.MezuniyetBasvurulariID equals mb.MezuniyetBasvurulariID
                            join ms in db.MezuniyetSurecis on mb.MezuniyetSurecID equals ms.MezuniyetSurecID
                            join dnm in db.Donemlers on mb.KayitOgretimYiliDonemID equals dnm.DonemID
                            join enst in db.Enstitulers on ms.EnstituKod equals enst.EnstituKod
                            join osl in db.OgrenimTipleris on new { mb.OgrenimTipKod, enst.EnstituKod } equals new { osl.OgrenimTipKod, osl.EnstituKod }
                            join prg in db.Programlars on mb.ProgramKod equals prg.ProgramKod
                            join abd in db.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                            where s.MezuniyetBasvurulariTezTeslimFormID == id
                            select new
                            {
                                s.MezuniyetBasvurulariTezTeslimFormID,
                                mb.MezuniyetBasvurulariID,
                                ms.EnstituKod,
                                enst.SistemErisimAdresi,
                                mb.OgrenciNo,
                                s.RowID,
                                AdSoyad = mb.Ad + " " + mb.Soyad,
                                DonemAdi = mb.KayitOgretimYiliBaslangic + " / " + (mb.KayitOgretimYiliBaslangic + 1) + " " + dnm.DonemAdi,
                                EnstituAdi = enst.EnstituAd,
                                osl.OgrenimTipAdi,
                                abd.AnabilimDaliAdi,
                                prg.ProgramAdi,
                                OgrenciKayitDonemi = mb.KayitOgretimYiliBaslangic + " - " + (mb.KayitOgretimYiliBaslangic + 1) + " / " + (mb.KayitOgretimYiliDonemID == 1 ? "Güz" : "Bahar") + " (" + (mb.KayitOgretimYiliDonemID == 1 ? "Fall" : "Spring") + ")",

                                s.IsTezDiliTr,
                                s.TezDili,
                                s.TezBaslikTr,
                                s.TezBaslikEn,
                                mb.TezTeslimSonTarih,
                                urlAdd = enst.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MBBBC_" + s.MezuniyetBasvurulariTezTeslimFormID + "_" + s.RowID
                            }).First();

                var sonSr = db.SRTalepleris.First(f =>
                    f.MezuniyetBasvurulariID==data.MezuniyetBasvurulariID && f.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili);
                this.DisplayName = data.AdSoyad + " FR-1243 Lisansüstü Ciltli Tez Teslim Formu";
                cellOgrenciNo.Text = data.OgrenciNo;
                cellOgrenciAdSoyad.Text = data.AdSoyad;
                cellOgrenciEnstituAdi.Text = data.EnstituAdi;
                cellOgrenciAnabilimDaliAdi.Text = data.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = data.ProgramAdi;
                cellOgrenciOgrenimSeviyesi.Text = data.OgrenimTipAdi;
                cellOgrenciKayitDonemi.Text = data.OgrenciKayitDonemi;
                cellTezDili.Text = data.IsTezDiliTr ? "Türkçe (Turkish)" : "İngilizce (English)";
                cellTezBaslikTr.Text = data.TezBaslikTr;
                cellTezBaslikEn.Text = data.TezBaslikEn;
                CellTezSavunmaSinavTarihi.Text = sonSr.Tarih.ToFormatDate();

                cellImzaOgrenciAdSoyad.Text = data.AdSoyad;

                cellFormKodu.Text = "Form Kodu: " + data.RowID.ToString().Substring(0, 8).ToUpper();
                var qrUlr = data.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MBBBC_" + data.MezuniyetBasvurulariTezTeslimFormID + "_" + data.RowID;
                xrQRCode.ImageUrl = qrUlr;
                xrQRCode.Image = qrUlr.CreateQrCode();

            }
        }

    }
}
