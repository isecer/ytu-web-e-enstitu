using System.Linq;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.TezIzlemeJuriOneri
{
    public partial class RprTijOneriFormu_FR0306 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprTijOneriFormu_FR0306(int id)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {


                var data = (from s in db.TijBasvuruOneris
                            join mb in db.TijBasvurus on s.TijBasvuruID equals mb.TijBasvuruID
                            join k in db.Kullanicilars on mb.KullaniciID equals k.KullaniciID
                            join e in db.Enstitulers on mb.EnstituKod equals e.EnstituKod
                            join prg in db.Programlars on mb.ProgramKod equals prg.ProgramKod
                            join abd in db.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                            where s.TijBasvuruOneriID == id
                            select new
                            {
                                k.OgrenciNo,
                                s.FormKodu,
                                AdSoyad = k.Ad + " " + k.Soyad,
                                e.EnstituAd,
                                abd.AnabilimDaliAdi,
                                prg.ProgramAdi,
                                OgrenciKayitDonemi = mb.KayitOgretimYiliBaslangic + " - " + (mb.KayitOgretimYiliBaslangic + 1) + " / " + (mb.KayitOgretimYiliDonemID == 1 ? "Güz" : "Bahar") + " (" + (mb.KayitOgretimYiliDonemID == 1 ? "Fall" : "Spring") + ")",
                                s.IsTezDiliTr,
                                TezDiliText = s.IsTezDiliTr ? "Türkçe (Turkish)" : "İngilizce (English)",
                                s.TezBaslikTr,
                                s.TezBaslikEn,
                                Danisman = s.Kullanicilar,
                                Juriler = s.TijBasvuruOneriJurilers.ToList(),
                                urlAdd = e.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "TIJF_" + s.TijBasvuruOneriID + "_" + s.UniqueID
                            }).First();

                this.DisplayName = "FR-0306 DOKTORA TEZ İZLEME KOMİTE ÜYESİ ÖNERİ FORMU";

                cellOgrenciNo.Text = data.OgrenciNo;
                xrTableFK.Text = "Form Kodu: " + data.FormKodu;
                cellOgrenciAdSoyad.Text = data.AdSoyad;
                cellOgrenciEnstituAdi.Text = data.EnstituAd;
                cellOgrenciAnabilimDaliAdi.Text = data.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = data.ProgramAdi;
                cellOgrenciKayitDonemi.Text = data.OgrenciKayitDonemi;
                cellTezDili.Text = data.TezDiliText;
                cellTezBasligiTr.Text = data.TezBaslikTr;
                cellTezBasligiEn.Text = data.TezBaslikEn;

                rwTaahhut.Visible = !data.IsTezDiliTr;


                var juriTd = data.Juriler.First(f => f.IsTezDanismani);

                cellTdUnvan.Text = juriTd.UnvanAdi;
                cellTdAdSoyad.Text = juriTd.AdSoyad;
                cellTdUniversiteAdi.Text = juriTd.UniversiteAdi;
                cellTdAnabilimDaliAdi.Text = juriTd.AnabilimdaliAdi;

                var ytuIci1 = data.Juriler.First(f => f.IsYtuIciJuri && f.RowNum == 1);
                cellYtuIciJuri1Unvan.Text = ytuIci1.UnvanAdi;
                cellYtuIciJuri1AdSoyad.Text = ytuIci1.AdSoyad;
                cellYtuIciJuri1Universite.Text = ytuIci1.UniversiteAdi;
                cellYtuIciJuri1AnabilimDali.Text = ytuIci1.AnabilimdaliAdi;

                var ytuIci2 = data.Juriler.First(f => f.IsYtuIciJuri && f.RowNum == 2);
                cellYtuIciJuri2Unvan.Text = ytuIci2.UnvanAdi;
                cellYtuIciJuri2AdSoyad.Text = ytuIci2.AdSoyad;
                cellYtuIciJuri2Universite.Text = ytuIci2.UniversiteAdi;
                cellYtuIciJuri2AnabilimDali.Text = ytuIci2.AnabilimdaliAdi;

                var ytuIci3 = data.Juriler.FirstOrDefault(f => f.IsYtuIciJuri && f.RowNum == 3);
                if (ytuIci3 != null)
                {
                    cellYtuIciJuri3Unvan.Text = ytuIci3.UnvanAdi;
                    cellYtuIciJuri3AdSoyad.Text = ytuIci3.AdSoyad;
                    cellYtuIciJuri3Universite.Text = ytuIci3.UniversiteAdi;
                    cellYtuIciJuri3AnabilimDali.Text = ytuIci3.AnabilimdaliAdi;
                }

                var ytuDisi1 = data.Juriler.First(f => !f.IsYtuIciJuri && f.RowNum == 4);
                cellYtuDisiJuri1Unvan.Text = ytuDisi1.UnvanAdi;
                cellYtuDisiJuri1AdSoyad.Text = ytuDisi1.AdSoyad;
                cellYtuDisiJuri1Universite.Text = ytuDisi1.UniversiteAdi;
                cellYtuDisiJuri1AnabilimDali.Text = ytuDisi1.AnabilimdaliAdi;

                var ytuDisi2 = data.Juriler.First(f => !f.IsYtuIciJuri && f.RowNum == 5);
                cellYtuDisiJuri2Unvan.Text = ytuDisi2.UnvanAdi;
                cellYtuDisiJuri2AdSoyad.Text = ytuDisi2.AdSoyad;
                cellYtuDisiJuri2Universite.Text = ytuDisi2.UniversiteAdi;
                cellYtuDisiJuri2AnabilimDali.Text = ytuDisi2.AnabilimdaliAdi;

                var ytuDisi3 = data.Juriler.FirstOrDefault(f => !f.IsYtuIciJuri && f.RowNum == 6);
                if (ytuDisi3 != null)
                {
                    cellYtuDisiJuri3Unvan.Text = ytuDisi3.UnvanAdi;
                    cellYtuDisiJuri3AdSoyad.Text = ytuDisi3.AdSoyad;
                    cellYtuDisiJuri3Universite.Text = ytuDisi3.UniversiteAdi;
                    cellYtuDisiJuri3AnabilimDali.Text = ytuDisi3.AnabilimdaliAdi;
                }
                cellDanismanUnvanAdSoyad.Text = data.Danisman.Unvanlar.UnvanAdi + "\r\n" + data.Danisman.Ad + " " + data.Danisman.Soyad;

                cellFormKodu.Text = "Form Kodu: " + data.FormKodu;
                xrQRCode.ImageUrl = data.urlAdd;
                xrQRCode.Image = data.urlAdd.CreateQrCode();

            }
        }

    }
}
