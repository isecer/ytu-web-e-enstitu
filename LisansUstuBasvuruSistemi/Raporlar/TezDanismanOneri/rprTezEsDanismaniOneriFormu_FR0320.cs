using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.TezDanismanOneri
{
    public partial class RprTezEsDanismaniOneriFormu_FR0320 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprTezEsDanismaniOneriFormu_FR0320(int tdoBasvuruEsDanismanId)
        {
            InitializeComponent();

            using (var  entities = new LubsDbEntities())
            {
                this.DisplayName = "FR-0320 TEZ EŞ DANIŞMANI ÖNERİ FORMU";

                var qData = (from s in entities.TDOBasvuruDanismen
                             join ed in entities.TDOBasvuruEsDanismen.Where(p => p.TDOBasvuruEsDanismanID == tdoBasvuruEsDanismanId) on s.TDOBasvuruDanismanID equals ed.TDOBasvuruDanismanID
                             join b in entities.TDOBasvurus on s.TDOBasvuruID equals b.TDOBasvuruID
                             join k in entities.Kullanicilars on b.KullaniciID equals k.KullaniciID
                             join e in entities.Enstitulers on b.EnstituKod equals e.EnstituKod
                             join prg in entities.Programlars on b.ProgramKod equals prg.ProgramKod
                             join abd in entities.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                             join ot in entities.OgrenimTipleris on new { b.EnstituKod, b.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                             join dn in entities.Donemlers on b.KayitOgretimYiliDonemID equals dn.DonemID
                             select new
                             {
                                 b.OgrenciNo,
                                 AdSoyad = k.Ad + " " + k.Soyad,
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
                cellEsDanismanProgramAdi.Text = qData.EdProgramAdi;
                cellEsDanismanUniversiteAdi.Text = qData.EdUniversiteAdi;

                cellEsGerekce.Text = qData.Gerekce;



                cellDanismanAdSoyadImza.Text = qData.TDAdSoyad;
                cellDanismanTarihImza.Text = qData.BasvuruTarihi.ToFormatDateAndTime();


            }
        }

    }
}
