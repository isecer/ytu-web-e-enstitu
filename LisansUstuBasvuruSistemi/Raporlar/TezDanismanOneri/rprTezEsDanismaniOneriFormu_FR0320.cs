using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.TezDanismanOneri
{
    public partial class RprTezEsDanismaniOneriFormu_FR0320 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprTezEsDanismaniOneriFormu_FR0320(int id)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                this.DisplayName = "FR-0320 TEZ EŞ DANIŞMANI ÖNERİ FORMU";

                var qData = (from s in db.TDOBasvuruDanismen
                             join ed in db.TDOBasvuruEsDanismen.Where(p => p.TDOBasvuruEsDanismanID == id) on s.TDOBasvuruDanismanID equals ed.TDOBasvuruDanismanID
                             join b in db.TDOBasvurus on s.TDOBasvuruID equals b.TDOBasvuruID
                             join k in db.Kullanicilars on b.KullaniciID equals k.KullaniciID
                             join e in db.Enstitulers on b.EnstituKod equals e.EnstituKod
                             join prg in db.Programlars on b.ProgramKod equals prg.ProgramKod
                             join abd in db.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                             join ot in db.OgrenimTipleris on new { b.EnstituKod, b.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                             join dn in db.Donemlers on b.KayitOgretimYiliDonemID equals dn.DonemID
                             select new
                             {
                                 b.OgrenciNo,
                                 AdSoyad = b.Ad + " " + b.Soyad,
                                 e.EnstituAd,
                                 prg.ProgramAdi,
                                 abd.AnabilimDaliAdi,
                                 ot.OgrenimTipAdi,
                                 b.KayitOgretimYiliBaslangic,
                                 dn.DonemAdi,
                                 s.TDODanismanTalepTipID,
                                 s.TDAdSoyad,
                                 s.TDUnvanAdi,
                                 s.TDAnabilimDaliAdi,
                                 s.TDProgramAdi,
                                 EdTDAdSoyad = ed.TDAdSoyad,
                                 EdTDUnvanAdi = ed.TDUnvanAdi,
                                 EdTDAnabilimDaliAdi = ed.TDAnabilimDaliAdi,
                                 EdTDProgramAdi = ed.TDProgramAdi,
                                 s.SinavAdi,
                                 s.SinavYili,
                                 s.SinavPuani, 
                                 ed.BasvuruTarihi,
                                 ed.IsDegisiklikTalebi,
                                 EdAdSoyad = ed.AdSoyad,
                                 EdUniversiteAdi = ed.UniversiteAdi,
                                 EdUnvanAdi = ed.UnvanAdi,
                                 EdProgramAdi = ed.ProgramAdi,
                                 EdAnabilimDaliAdi = ed.AnabilimDaliAdi,
                                 ed.Gerekce,
                                 s.FormKodu,
                                 urlAdd = e.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "TDOEF_" + ed.TDOBasvuruEsDanismanID + "_" + ed.UniqueID,
                             }).FirstOrDefault();

                cellFormKodu.Text = "Form Kodu: " + qData.FormKodu;
                xrQRCode.ImageUrl = qData.urlAdd;
                xrQRCode.Image = qData.urlAdd.CreateQrCode();

                cellOgrenciNo.Text = qData.OgrenciNo;
                cellOgrenciAdSoyad.Text = qData.AdSoyad;
                cellOgrenciEnstituAdi.Text = qData.EnstituAd;
                cellOgrenciAnabilimDaliAdi.Text = qData.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = qData.ProgramAdi;
                cellOgrenciOgrenimSeviyesi.Text = qData.OgrenimTipAdi;
                cellOgrenciKayitDonemi.Text = qData.KayitOgretimYiliBaslangic + "/" + (qData.KayitOgretimYiliBaslangic + 1) + " " + qData.DonemAdi;
                cellAtamaDurum.Text = qData.IsDegisiklikTalebi ? "Değişiklik Talebi" : "Yeni Atama";


                if (qData.EdTDAdSoyad.IsNullOrWhiteSpace())
                {
                    cellDanismanAdSoyad.Text = qData.TDUnvanAdi + " " + qData.TDAdSoyad;
                    cellDanismanAnabilimDaliAdi.Text = qData.TDAnabilimDaliAdi;
                    cellDanismanProgramAdi.Text = qData.TDProgramAdi;

                }
                else
                {
                    cellDanismanAdSoyad.Text = qData.EdTDUnvanAdi + " " + qData.EdTDAdSoyad;
                    cellDanismanAnabilimDaliAdi.Text = qData.EdTDAnabilimDaliAdi;
                    cellDanismanProgramAdi.Text = qData.EdTDProgramAdi;
                }

                cellEsDanismanAdSoyad.Text = qData.EdUnvanAdi + " " + qData.EdAdSoyad;
                cellEsDanismanAnabilimDaliAdi.Text = qData.EdAnabilimDaliAdi;
                cellEsDanismanProgramAdi.Text = qData.ProgramAdi;
                cellEsDanismanUniversiteAdi.Text = qData.EdUniversiteAdi;

                cellEsGerekce.Text = qData.Gerekce;



                cellDanismanAdSoyadImza.Text = qData.TDAdSoyad;
                cellDanismanTarihImza.Text = qData.BasvuruTarihi.ToFormatDateAndTime();


            }
        }

    }
}
