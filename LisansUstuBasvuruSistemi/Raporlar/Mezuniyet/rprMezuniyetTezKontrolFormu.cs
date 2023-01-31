using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;
using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Raporlar
{

    public partial class rprMezuniyetTezKontrolFormu : DevExpress.XtraReports.UI.XtraReport
    {
        public rprMezuniyetTezKontrolFormu(Guid? RowID, int? MezuniyetBasvurulariTezDosyaID)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var MezuniyetBasvurulariTezDosyasi = db.MezuniyetBasvurulariTezDosyalaris.Where(p => p.RowID == (RowID ?? p.RowID) && p.MezuniyetBasvurulariTezDosyaID == (MezuniyetBasvurulariTezDosyaID ?? p.MezuniyetBasvurulariTezDosyaID)).First();
                var basvuru = MezuniyetBasvurulariTezDosyasi.MezuniyetBasvurulari;
                var enstituLng = basvuru.MezuniyetSureci.Enstituler;
                lblEnstituAdi.Text = enstituLng.EnstituAd;
                var onaylayan = db.Kullanicilars.Where(p => p.KullaniciID == MezuniyetBasvurulariTezDosyasi.OnayYapanID).First();
                cell_OnaylayanKisi.Text = onaylayan.Ad + " " + onaylayan.Soyad;
                cellOnayTarihi.Text = MezuniyetBasvurulariTezDosyasi.OnayTarihi.ToFormatDateAndTime(); 
                var KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.Where(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).First().DonemAdi + " - " + basvuru.KayitTarihi.ToDateString();
                lngLbl_AkademikTarih.Text = "Eğitim Öğretim Yılı";
                cell_AkademikYil.Text = basvuru.MezuniyetSureci.BaslangicYil + "-" + basvuru.MezuniyetSureci.BitisYil + " " + db.Donemlers.Where(p => p.DonemID == basvuru.MezuniyetSureci.DonemID).First().DonemAdi;
                lblKayitTarihi.Text = "Kayıt Tarihi";
                cell_KayitTarihi.Text = KayitDonemi;
                Lbl_AdSoyad.Text = "Ad Soyad";
                cell_AdiSoyadi.Text = basvuru.Ad + " " + basvuru.Soyad;
                lbl_AnabilimdaliProg.Text = "Anabilim Dalı / Program";
                cell_AnabilimdaliProg.Text = basvuru.Programlar.AnabilimDallari.AnabilimDaliAdi + " / " + basvuru.Programlar.ProgramAdi;
                lbl_OgrenciNo.Text = "Öğrenci No";
                cell_OgrenciNo.Text = basvuru.OgrenciNo;
                lbl_OgrenimTipi.Text = "Öğrenim Seviyesi";
                cell_OgrenimTipi.Text = db.OgrenimTipleris.First(p => p.OgrenimTipKod == basvuru.OgrenimTipKod && p.EnstituKod == basvuru.MezuniyetSureci.EnstituKod).OgrenimTipAdi;

                var urlAdd = enstituLng.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MBTDO_" + MezuniyetBasvurulariTezDosyasi.MezuniyetBasvurulariID + "_" + MezuniyetBasvurulariTezDosyasi.RowID.ToString();
                xrQRCode.ImageUrl = urlAdd;
                xrQRCode.Image = urlAdd.CreateQrCode();
                this.DisplayName = (basvuru.Ad + " " + basvuru.Soyad) + " Mezuniyet Tez Kontrol Formu";
            }

        }

    }
}
