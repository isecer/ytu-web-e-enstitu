using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;
using BiskaUtil;
using System.Linq;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprMezuniyetCiltliTezTeslimFormu_FR1243 : DevExpress.XtraReports.UI.XtraReport
    {
        public rprMezuniyetCiltliTezTeslimFormu_FR1243(int ID)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {


                var data = (from s in db.SRTalepleriBezCiltFormus
                            join sr in db.SRTalepleris on s.SRTalepID equals sr.SRTalepID
                            join mb in db.MezuniyetBasvurularis on sr.MezuniyetBasvurulariID equals mb.MezuniyetBasvurulariID
                            join ms in db.MezuniyetSurecis on mb.MezuniyetSurecID equals ms.MezuniyetSurecID
                            join dnm in db.Donemlers on mb.KayitOgretimYiliDonemID equals dnm.DonemID
                            join enst in db.Enstitulers on ms.EnstituKod equals enst.EnstituKod
                            join osl in db.OgrenimTipleris on new { mb.OgrenimTipKod, enst.EnstituKod } equals new { osl.OgrenimTipKod, osl.EnstituKod }
                            join prg in db.Programlars on mb.ProgramKod equals prg.ProgramKod
                            join abd in db.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                            where s.SRTalepleriBezCiltFormID == ID
                            select new
                            {
                                s.SRTalepleriBezCiltFormID,
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
                                sr.Tarih,
                                mb.TezTeslimSonTarih,
                                urlAdd = enst.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MBBBC_" + s.SRTalepleriBezCiltFormID + "_" + s.RowID
                            }).First();

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
                CellTezSavunmaSinavTarihi.Text = data.Tarih.ToFormatDate();

                cellImzaOgrenciAdSoyad.Text = data.AdSoyad;

                cellFormKodu.Text = "Form Kodu: " + data.RowID.ToString().Substring(0, 8).ToUpper();
                var qrUlr = data.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MBBBC_" + data.SRTalepleriBezCiltFormID + "_" + data.RowID;
                xrQRCode.ImageUrl = qrUlr;
                xrQRCode.Image = qrUlr.CreateQrCode();

            }
        }

    }
}
