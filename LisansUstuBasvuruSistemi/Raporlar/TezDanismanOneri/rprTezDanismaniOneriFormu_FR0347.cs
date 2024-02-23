using System.Drawing;
using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.TezDanismanOneri
{
    public partial class RprTezDanismaniOneriFormu_FR0347 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprTezDanismaniOneriFormu_FR0347(int tdoBasvuruDanismanId)
        {
            InitializeComponent();

            using (var  entities = new LubsDbEntities())
            {
                this.DisplayName = "FR-0347 TEZ DANIŞMANI ÖNERİ FORMU";

                var q = (from s in entities.TDOBasvuruDanismen.Where(p => p.TDOBasvuruDanismanID == tdoBasvuruDanismanId)
                         join b in entities.TDOBasvurus on s.TDOBasvuruID equals b.TDOBasvuruID
                         join k in entities.Kullanicilars on b.KullaniciID equals k.KullaniciID
                         join e in entities.Enstitulers on b.EnstituKod equals e.EnstituKod 
                         join prg in entities.Programlars on b.ProgramKod equals prg.ProgramKod
                         join abd in entities.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                         join ot in entities.OgrenimTipleris on new { b.OgrenimTipKod, b.EnstituKod } equals new { ot.OgrenimTipKod, ot.EnstituKod } 
                         select new
                         {
                             s.BasvuruTarihi,
                             s.FormKodu,
                             urlAdd = e.SistemErisimAdresi  + "/DosyaKontrol/Index?Kod=" + "TDOF_" + s.TDOBasvuruDanismanID + "_" + s.UniqueID,
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
                             s.TDUnvanAdi,
                             s.TDAdSoyad,
                             s.TDAnabilimDaliAdi,
                             s.TDProgramAdi,
                             s.TDTezSayisiDR,
                             s.TDTezSayisiYL,
                             s.TDOgrenciSayisiDR,
                             s.TDOgrenciSayisiYL,
                             s.SinavAdi,
                             s.SinavYili,
                             s.SinavPuani, 
                             s.DanismanOnayTarihi

                         }).First();



                this.cellFormKodu.Text = "Form Kodu: " + q.FormKodu;
                this.xrQRCode.ImageUrl = q.urlAdd;
                this.xrQRCode.Image = q.urlAdd.CreateQrCode();
                this.cellOgrenciNo.Text = q.OgrenciNo;
                this.cellOgrenciAdSoyad.Text = q.AdSoyad;
                this.cellOgrenciEnstituAdi.Text = q.EnstituAd;
                this.cellOgrenciAnabilimDaliAdi.Text = q.AnabilimDaliAdi;
                this.cellOgrenciProgramAdi.Text = q.ProgramAdi;
                this.cellOgrenciOgrenimSeviyesi.Text = q.OgrenimTipAdi;

                this.cellOgrenciKayitDonemi.Text = q.OgrenciKayitDonemi;
                this.cellTezDili.Text = q.IsTezDiliTr ? "Türkçe (Turkish)" : "İngilizce (English)";
                this.cellTezBaslikTr.Text = q.TezBaslikTr;
                this.cellTezBaslikEn.Text = q.TezBaslikEn;
                this.cellDanismanUnvan.Text = q.TDUnvanAdi;
                this.cellDanismanAdSoyad.Text = q.TDAdSoyad;
                this.cellDanismanAnabilimDaliAdi.Text = q.TDAnabilimDaliAdi;
                this.cellDanismanProgramAdi.Text = q.TDProgramAdi;
                this.cellBasariIleTamamlanmisTezSayisiDR.Text = q.TDTezSayisiDR.ToString();
                this.cellBasariIleTamamlanmisTezSayisiYL.Text = q.TDTezSayisiYL.ToString();
                this.cellUzerineKayitliOgrenciSayisiDR.Text = q.TDOgrenciSayisiDR.ToString();
                this.cellUzerineKayitliOgrenciSayisiYL.Text = q.TDOgrenciSayisiYL.ToString();
                this.cellOgrenciAdSoyadImza.Text = q.AdSoyad;
                if (!q.IsTezDiliTr)
                {
                    this.cellOgrenciYabanciDilBilgi.Text = q.SinavAdi + " / " + q.SinavYili;
                    this.cellOgrenciYabanciDilPuan.Text = q.SinavPuani.ToString(); 
                }
                else
                {
                    this.xrTable17.Visible = false;
                    this.xrTable18.Visible = false;
                    float num2 = this.xrTable17.HeightF + this.xrTable18.HeightF;
                    PointF tf = new PointF
                    {
                        X = this.xrTblTahhutCapt.LocationF.X,
                        Y = this.xrTblTahhutCapt.LocationF.Y - num2
                    };
                    this.xrTblTahhutCapt.LocationF = tf;
                    tf = new PointF
                    {
                        X = this.xrTableImzaCapt.LocationF.X,
                        Y = this.xrTableImzaCapt.LocationF.Y - num2
                    };
                    this.xrTableImzaCapt.LocationF = tf;
                    tf = new PointF
                    {
                        X = this.xrCaptImzalar.LocationF.X,
                        Y = this.xrCaptImzalar.LocationF.Y - num2
                    };
                    this.xrCaptImzalar.LocationF = tf;
                    tf = new PointF
                    {
                        X = this.lblEkBilgi.LocationF.X,
                        Y = this.lblEkBilgi.LocationF.Y - num2
                    };
                    this.lblEkBilgi.LocationF = tf;
                    tf = new PointF
                    {
                        X = this.lblNot.LocationF.X,
                        Y = this.lblNot.LocationF.Y - num2
                    };
                    this.lblNot.LocationF = tf;
                    tf = new PointF
                    {
                        X = this.xrRichText1.LocationF.X,
                        Y = this.xrRichText1.LocationF.Y - num2
                    };
                    this.xrRichText1.LocationF = tf;
                    tf = new PointF
                    {
                        X = this.xrRichKaraBigi.LocationF.X,
                        Y = this.xrRichKaraBigi.LocationF.Y - num2
                    };
                    this.xrRichKaraBigi.LocationF = tf;
                }
                this.cellOgrenciTarihImza.Text = q.BasvuruTarihi.ToFormatDateAndTime();
                this.cellDanismanAdSoyadImza.Text = q.TDAdSoyad;
                this.cellDanismanTarihImza.Text = q.DanismanOnayTarihi.ToFormatDateAndTime();


            }
        }


    }
}