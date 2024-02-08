using System.Linq;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using System.Drawing;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Raporlar.TezDanismanOneri
{
    public partial class RprTezDanismaniDegisiklikFormu_FR0308 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprTezDanismaniDegisiklikFormu_FR0308(int tdoBasvuruDanismanId)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                DisplayName = "FR-0308 TEZ DANIŞMANI KONU DİL DEĞİŞİKLİK FORMU";

                var basvuru = db.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId);


                var q = (from s in db.TDOBasvuruDanismen.Where(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId)
                         join b in db.TDOBasvurus on s.TDOBasvuruID equals b.TDOBasvuruID
                         join k in db.Kullanicilars on b.KullaniciID equals k.KullaniciID
                         join e in db.Enstitulers on b.EnstituKod equals e.EnstituKod
                         join prg in db.Programlars on b.ProgramKod equals prg.ProgramKod
                         join abd in db.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                         join ot in db.OgrenimTipleris on new { b.OgrenimTipKod, b.EnstituKod } equals new { ot.OgrenimTipKod, ot.EnstituKod }
                         select new
                         {
                             s.TDODanismanTalepTipID,
                             b.OgrenimTipKod,
                             s.BasvuruTarihi,
                             s.FormKodu,
                             urlAdd = e.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "TDOF_" + s.TDOBasvuruDanismanID + "_" + s.UniqueID,
                             s.UniqueID,
                             b.OgrenciNo,
                             AdSoyad = k.Ad + " " + k.Soyad,
                             e.EnstituAd,
                             abd.AnabilimDaliAdi,
                             prg.ProgramAdi,
                             ot.OgrenimTipAdi,
                             OgrenciKayitDonemi = b.KayitOgretimYiliBaslangic + " - " + (b.KayitOgretimYiliBaslangic + 1) + " / " + (b.KayitOgretimYiliDonemID == 1 ? "Güz" : "Bahar") + " (" + (b.KayitOgretimYiliDonemID == 1 ? "Fall" : "Spring") + ")",
                             s.IsTezDiliTr,
                             s.TezBaslikTr,
                             s.TezBaslikEn,
                             s.IsYeniTezDiliTr,
                             s.YeniTezBaslikTr,
                             s.YeniTezBaslikEn,
                             s.TDUnvanAdi,
                             s.TDAdSoyad,
                             s.TDAnabilimDaliAdi,
                             s.TDProgramAdi,
                             s.TDTezSayisiDR,
                             s.TDTezSayisiYL,
                             s.TDOgrenciSayisiDR,
                             s.TDOgrenciSayisiYL,
                             s.VarolanTDAdSoyad,
                             s.VarolanTDUnvanAdi,
                             s.VarolanTDAnabilimDaliAdi,
                             s.VarolanTDProgramAdi,
                             s.SinavAdi,
                             s.SinavYili,
                             s.SinavPuani,
                             s.DanismanOnayTarihi,
                             s.VarolanDanismanOnayTarihi

                         }).First();



                cellFormKodu.Text = "Form Kodu: " + q.FormKodu;
                xrQRCode.ImageUrl = q.urlAdd;
                xrQRCode.Image = q.urlAdd.CreateQrCode();
                cellOgrenciNo.Text = q.OgrenciNo;
                cellOgrenciAdSoyad.Text = q.AdSoyad;
                cellOgrenciEnstituAdi.Text = q.EnstituAd;
                cellOgrenciAnabilimDaliAdi.Text = q.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = q.ProgramAdi;
                cellOgrenciOgrenimSeviyesi.Text = q.OgrenimTipAdi;
                cellImzaOgrenciTarih.Text = q.BasvuruTarihi.ToFormatDate();
                cellImzaOgrenciOnay.Text = "Elektronik olarak onaylanmıştır.";

                cellOgrenciKayitDonemi.Text = q.OgrenciKayitDonemi;
                cellTezDili.Text = q.IsTezDiliTr ? "Türkçe (Turkish)" : "İngilizce (English)";
                cellTezBaslikTr.Text = q.TezBaslikTr;
                cellTezBaslikEn.Text = q.TezBaslikEn;


                if (q.IsYeniTezDiliTr.HasValue ? q.IsYeniTezDiliTr == false : !q.IsTezDiliTr)
                {
                    dgYabanciDilBilgi.Visible = true;
                    this.cellOgrenciYabanciDilBilgi.Text = q.SinavAdi + " / " + q.SinavYili;
                    this.cellOgrenciYabanciDilPuan.Text = (q.SinavPuani ?? "");
                }



                chkDanismanDegisecekEvet.Checked = q.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezDanismaniDegisikligi || q.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi;
                chkDanismanDegisecekHayir.Checked = !chkDanismanDegisecekEvet.Checked;
                if (chkDanismanDegisecekEvet.Checked)
                {
                    cellDanismanUnvan.Text = q.TDUnvanAdi;
                    cellDanismanAdSoyad.Text = q.TDAdSoyad;
                    cellDanismanAnabilimDaliAdi.Text = q.TDAnabilimDaliAdi;
                    cellDanismanProgramAdi.Text = q.TDProgramAdi;
                    cellBasariIleTamamlanmisTezSayisiDR.Text = q.TDTezSayisiDR.ToString();
                    cellBasariIleTamamlanmisTezSayisiYL.Text = q.TDTezSayisiYL.ToString();
                    cellUzerineKayitliOgrenciSayisiDR.Text = q.TDOgrenciSayisiDR.ToString();
                    cellUzerineKayitliOgrenciSayisiYL.Text = q.TDOgrenciSayisiYL.ToString();

                    CellMevcutDanismanAd.Text = q.VarolanTDUnvanAdi + " " + q.VarolanTDAdSoyad;
                    cellMevcutDanismanAnabilimDali.Text = q.VarolanTDAnabilimDaliAdi;
                    cellMevcutDanismanProgram.Text = q.VarolanTDProgramAdi;

                    cellImzaMevcutDanismanAdSoyad.Text = q.VarolanTDUnvanAdi + " " + q.VarolanTDAdSoyad;
                    cellImzaOnerilenDanismanAdSoyad.Text = q.TDUnvanAdi + " " + q.TDAdSoyad;

                    cellImzaMevcutDanismanTarih.Text = q.DanismanOnayTarihi.ToFormatDate();
                    cellImzaMevcutDanismanOnay.Text = "Elektronik olarak onaylanmıştır.";
                    cellImzaOnerilenDanismanTarih.Text = q.DanismanOnayTarihi.ToFormatDate();
                    cellImzaOnerilenDanismanOnay.Text = "Elektronik olarak onaylanmıştır.";

                   
                }
                else
                {
                    CellMevcutDanismanAd.Text = q.TDUnvanAdi + " " + q.TDAdSoyad;
                    cellMevcutDanismanAnabilimDali.Text = q.TDAnabilimDaliAdi;
                    cellMevcutDanismanProgram.Text = q.TDProgramAdi; 
                    cellImzaMevcutDanismanAdSoyad.Text = q.TDUnvanAdi + " " + q.TDAdSoyad;
                    cellImzaMevcutDanismanTarih.Text = q.DanismanOnayTarihi.ToFormatDate();
                    cellImzaMevcutDanismanOnay.Text = "Elektronik olarak onaylanmıştır.";
                }
                chkTezBasligiDegisecekEvet.Checked = q.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezBasligiDegisikligi || q.TDODanismanTalepTipID == TdoDanismanTalepTipEnum.TezDanismaniVeBaslikDegisikligi;
                chkTezBasligiDegisecekHayir.Checked = !chkTezBasligiDegisecekEvet.Checked;
                if (chkTezBasligiDegisecekEvet.Checked)
                {
                    cellYeniTezBaslikTr.Text = q.YeniTezBaslikTr;
                    cellYeniTezBaslikEn.Text = q.YeniTezBaslikEn;
                }
                chkTezDiliDegisecekEvet.Checked = q.IsYeniTezDiliTr.HasValue;
                chkTezDiliDegisecekHayir.Checked = !chkTezDiliDegisecekEvet.Checked;
                if (chkTezDiliDegisecekEvet.Checked)
                {
                    cellYeniTezDili.Text = q.IsYeniTezDiliTr == true ? "Türkçe (Turkish)" : "İngilizce (English)";
                }
                detGrupDanismanDegisiklik.Visible = chkDanismanDegisecekEvet.Checked;
                rwOnerilenTdBaslikEn.Visible = detGrupDanismanDegisiklik.Visible;
                rwOnerilenTdBaslikTr.Visible = detGrupDanismanDegisiklik.Visible;
                rwOnerilenTdDetayBaslik.Visible = detGrupDanismanDegisiklik.Visible;
                rwOnerilenTdDetayBilgileri.Visible = detGrupDanismanDegisiklik.Visible;
                detGrupTezBaslikDegisiklik.Visible = chkTezBasligiDegisecekEvet.Checked;
                detGrupTezDiliDr.Visible = chkTezDiliDegisecekEvet.Checked;
                rwTezOneriTarihDr.Visible = q.OgrenimTipKod.IsDoktora();
                rwTezOneriYapildiDr.Visible = q.OgrenimTipKod.IsDoktora();
                rwTezSayisiDr.Visible = q.OgrenimTipKod.IsDoktora();



                cellImzaOgrenciAdSoyad.Text = q.AdSoyad;



            }
        }


    }
}