using System;
using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Raporlar.DonemProjesi
{
    public partial class RprDpSinavTutanakFormu_FR0366 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprDpSinavTutanakFormu_FR0366(int donemProjesiBasvuruId)
        {
            InitializeComponent();
            using (var entities = new LubsDbEntities())
            {
                var donemProjesiBasvuru = entities.DonemProjesiBasvurus.First(p => p.DonemProjesiBasvuruID == donemProjesiBasvuruId);
                var donemProjesi = donemProjesiBasvuru.DonemProjesi;
                var sinav = donemProjesiBasvuru.SRTalepleris.First();
                var ogrenci = donemProjesi.Kullanicilar;
                var enstL = donemProjesi.Enstituler;
                var prgL = donemProjesi.Programlar;
                var abdL = donemProjesi.Programlar.AnabilimDallari;
                cellOgrenciNo.Text = donemProjesi.OgrenciNo;
                cellOgrenciAdSoyad.Text = ogrenci.Ad + " " + ogrenci.Soyad;
                cellOgrenciEnstituAdi.Text = enstL.EnstituAd;
                cellOgrenciAnabilimDaliAdi.Text = abdL.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = prgL.ProgramAdi;
                cellOgrenciKayitDonemi.Text = donemProjesi.KayitOgretimYiliBaslangic + " - " + (donemProjesi.KayitOgretimYiliBaslangic + 1) + " / " + (donemProjesi.KayitOgretimYiliDonemID == 1 ? "Güz" : "Bahar") + " (" + (donemProjesi.KayitOgretimYiliDonemID == 1 ? "Fall" : "Spring") + ")";


                cellDonemProjesiBaslik.Text = donemProjesiBasvuru.ProjeBasligi;
                cellEnFazlaTekKaynakOrani.Text = donemProjesiBasvuru.TekKaynakOrani + " %";
                cellToplamKaynakOrani.Text = donemProjesiBasvuru.ToplamKaynakOrani + " %";
                if (sinav.IsOnline)
                {
                    cellToplantiYeri.Text = "Online";
                }
                else
                {
                    cellToplantiYeri.Text = sinav.SRSalonID.HasValue ? sinav.SRSalonlar.SalonAdi : sinav.SalonAdi;
                }
                cellToplantiTarihi.Text = sinav.Tarih.ToFormatDate();
                cellToplantiSaati.Text = $"{sinav.BasSaat:hh\\:mm}";


                var uyeler = donemProjesiBasvuru.DonemProjesiJurileris.ToList();

                var danisman = uyeler.First(p => p.IsTezDanismani);
                var juriler = uyeler.Where(p => !p.IsTezDanismani).ToList();
                var juri1 = juriler[0];
                var juri2 = juriler[1];

                cellProjeYurutucuUnvanAdSoyad.Text = danisman.UnvanAdi + " " + danisman.AdSoyad;
                cellProjeYurutucuAbdUniversiteAdi.Text = danisman.AnabilimdaliAdi + "\r\n" + GlobalSistemSetting.UniversiteAdi;


                cellJuriUyesi1AdSoyad.Text = juri1.UnvanAdi + " " + juri1.AdSoyad;
                cellJuriUyesi1AbdUniversiteAdi.Text = juri1.AnabilimdaliAdi + "\r\n" + GlobalSistemSetting.UniversiteAdi;

                cellJuriUyesi2AdSoyad.Text = juri2.UnvanAdi + " " + juri2.AdSoyad;
                cellJuriUyesi2AbdUniversiteAdi.Text = juri2.AnabilimdaliAdi + "\r\n" + GlobalSistemSetting.UniversiteAdi;

                DisplayName = (ogrenci.Ad + " " + ogrenci.Soyad) + " FR-0366 Dönem Projesi Sınavı Sonuç Tutanaği";

                chkOyBirligi.Checked = donemProjesiBasvuru.IsOyBirligiOrCoklugu == true;
                chkOyCoklugu.Checked = donemProjesiBasvuru.IsOyBirligiOrCoklugu == false;
                chkBasarili.Checked = donemProjesiBasvuru.DonemProjesiJuriOnayDurumID == DonemProjesiJuriOnayDurumEnum.Basarili;
                chkBasarisiz.Checked = donemProjesiBasvuru.DonemProjesiJuriOnayDurumID == DonemProjesiJuriOnayDurumEnum.Basarisiz;
                chkUzatma.Checked = donemProjesiBasvuru.DonemProjesiJuriOnayDurumID == DonemProjesiJuriOnayDurumEnum.BasarisizKatilmadi;

                cellFormKodu.Text = "Form Kodu: " + donemProjesiBasvuru.FormKodu;
                var qrUlr = enstL.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "DPSF_" + donemProjesiBasvuru.DonemProjesiBasvuruID + "_" + donemProjesiBasvuru.UniqueID;
                xrQRCode.ImageUrl = qrUlr;
                xrQRCode.Image = qrUlr.CreateQrCode();




            }
        }

    }
}
