using System;
using System.Linq;
using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{

    public partial class RprMezuniyetTezKontrolFormu : DevExpress.XtraReports.UI.XtraReport
    {
        public RprMezuniyetTezKontrolFormu(Guid? tezDosyalariRowId, int? mezuniyetBasvurulariTezDosyaId)
        {
            InitializeComponent();

            using (var entities = new LubsDbEntities())
            {
                var mezuniyetBasvurulariTezDosyasi = entities.MezuniyetBasvurulariTezDosyalaris.First(p => p.RowID == (tezDosyalariRowId ?? p.RowID) && p.MezuniyetBasvurulariTezDosyaID == (mezuniyetBasvurulariTezDosyaId ?? p.MezuniyetBasvurulariTezDosyaID));
                var basvuru = mezuniyetBasvurulariTezDosyasi.MezuniyetBasvurulari;
                var enstituLng = basvuru.MezuniyetSureci.Enstituler;
                lblEnstituAdi.Text = enstituLng.EnstituAd;
                var onaylayan = entities.Kullanicilars.First(p => p.KullaniciID == mezuniyetBasvurulariTezDosyasi.OnayYapanID);
                cell_OnaylayanKisi.Text = onaylayan.Ad + " " + onaylayan.Soyad;
                cellOnayTarihi.Text = mezuniyetBasvurulariTezDosyasi.OnayTarihi.ToFormatDateAndTime();
                var kayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + entities.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi + " - " + basvuru.KayitTarihi.ToFormatDate();
                lngLbl_AkademikTarih.Text = "Eğitim Öğretim Yılı";
                cell_AkademikYil.Text = basvuru.MezuniyetSureci.BaslangicYil + "-" + basvuru.MezuniyetSureci.BitisYil + " " + entities.Donemlers.First(p => p.DonemID == basvuru.MezuniyetSureci.DonemID).DonemAdi;
                lblKayitTarihi.Text = "Kayıt Tarihi";
                cell_KayitTarihi.Text = kayitDonemi;
                Lbl_AdSoyad.Text = "Ad Soyad";
                cell_AdiSoyadi.Text = basvuru.Ad + " " + basvuru.Soyad;
                lbl_AnabilimdaliProg.Text = "Anabilim Dalı / Program";
                cell_AnabilimdaliProg.Text = basvuru.Programlar.AnabilimDallari.AnabilimDaliAdi + " / " + basvuru.Programlar.ProgramAdi;
                lbl_OgrenciNo.Text = "Öğrenci No";
                cell_OgrenciNo.Text = basvuru.OgrenciNo;
                lbl_OgrenimTipi.Text = "Öğrenim Seviyesi";
                cell_OgrenimTipi.Text = entities.OgrenimTipleris.First(p => p.OgrenimTipKod == basvuru.OgrenimTipKod && p.EnstituKod == basvuru.MezuniyetSureci.EnstituKod).OgrenimTipAdi;

                var urlAdd = enstituLng.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MBTDO_" + mezuniyetBasvurulariTezDosyasi.MezuniyetBasvurulariID + "_" + mezuniyetBasvurulariTezDosyasi.RowID.ToString();
                xrQRCode.ImageUrl = urlAdd;
                xrQRCode.Image = urlAdd.CreateQrCode();
                this.DisplayName = (basvuru.Ad + " " + basvuru.Soyad) + " Mezuniyet Tez Kontrol Formu";
            }

        }

    }
}
