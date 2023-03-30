using System.Drawing;
using System.Linq;
using DevExpress.XtraPrinting;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using DevExpress.XtraReports.UI;

namespace LisansUstuBasvuruSistemi.Raporlar.Yeterlik
{
    public partial class RprDrYeterlikSinavDegerlendirmeFormu_FR1227 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprDrYeterlikSinavDegerlendirmeFormu_FR1227(int id)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                this.DisplayName = "FR-1227 DOKTORA YETERLİK SINAVI DEĞERLENDİRME FORMU";

                var q = (from s in db.YeterlikBasvurus.Where(p => p.YeterlikBasvuruID == id)
                         join bs in db.YeterlikSurecis on s.YeterlikSurecID equals bs.YeterlikSurecID
                         join ogrenci in db.Kullanicilars on s.KullaniciID equals ogrenci.KullaniciID
                         join e in db.Enstitulers on s.YeterlikSureci.EnstituKod equals e.EnstituKod
                         join prg in db.Programlars on s.ProgramKod equals prg.ProgramKod
                         join abd in db.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                         join ot in db.OgrenimTipleris on s.OgrenimTipID equals ot.OgrenimTipID
                         select new
                         {
                             s.BasvuruTarihi,
                             urlAdd = e.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "YETB_" + s.UniqueID,
                             s.UniqueID,
                             s.OgrenciNo,
                             AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                             e.EnstituAd,
                             abd.AnabilimDaliAdi,
                             prg.ProgramAdi,
                             ot.OgrenimTipAdi,
                             OgrenciKayitDonemi = bs.BaslangicYil + " - " + (bs.BitisYil) + " / " + (bs.DonemID == 1 ? "Güz" : "Bahar") + " (" + (bs.DonemID == 1 ? "Fall" : "Spring") + ")",
                             s.YeterlikSurecID,
                             s.KullaniciID,
                             s.OgrenimTipID,
                             s.ProgramKod,
                             s.OkuduguDonemNo,
                             s.YsBasToplamKrediKriteri,
                             s.YsBasEtikNotKriteri,
                             s.YsBasSeminerNotKriteri,
                             s.KayitYil,
                             s.KayitDonemID,
                             s.KayitTarihi,
                             s.TezDanismanID,
                             s.IsEnstituOnaylandi,
                             s.EnstituOnayTarihi,
                             s.EnstituOnayAciklama,
                             s.IsAbdKomitesiJuriyiOnayladi,
                             s.YaziliSinavTarihi,
                             s.YaziliSinavYeri,
                             s.IsYaziliSinavinaKatildi,
                             s.YaziliSinaviNotu,
                             s.IsYaziliSinavBasarili,
                             s.IsSozluSinavOnline,
                             s.SozluSinavTarihi,
                             s.SozluSinavYeri,
                             s.IsSozluSinavinaKatildi,
                             s.SozluSinaviOrtalamaNotu,
                             s.IsSozluSinavBasarili,
                             s.GenelBasariNotu,
                             s.IsGenelSonucBasarili,
                             Juris = s.YeterlikBasvuruJuriUyeleris.Where(p => p.IsSecilenJuri).OrderBy(o => o.IsYtuIciOrDisi).ToList(),
                             OgrenimTipKriter = bs.YeterlikSurecOgrenimTipleris.FirstOrDefault(f => f.OgrenimTipID == s.OgrenimTipID)
                         }).First();



                this.xrQRCode.ImageUrl = q.urlAdd;
                this.xrQRCode.Image = q.urlAdd.CreateQrCode(360, 360);
                this.cellOgrenciNo.Text = q.OgrenciNo;
                this.cellOgrenciAdSoyad.Text = q.AdSoyad;
                this.cellOgrenciEnstituAdi.Text = q.EnstituAd;
                this.cellOgrenciAnabilimDaliAdi.Text = q.AnabilimDaliAdi;
                this.cellOgrenciProgramAdi.Text = q.ProgramAdi;
                this.cellOgrenciOgrenimSeviyesi.Text = q.OgrenimTipAdi;
                this.cellOgrenciKayitDonemi.Text = q.OgrenciKayitDonemi;

                var danisman = q.Juris.First(p => p.JuriTipAdi == "TezDanismani");
                this.cellDanismanAdSoyad.Text = danisman.UnvanAdi + " " + danisman.AdSoyad;
                this.cellDanismanAnabilimDaliAdi.Text = danisman.AnabilimDaliAdi;
                this.cellYaziliSinavTarih.Text = q.YaziliSinavTarihi.ToFormatDateAndTime();
                this.cellYaziliSinavYeri.Text = q.YaziliSinavYeri;
                if (q.IsYaziliSinavinaKatildi == true)
                {
                    this.cellYaziliYuzlukNotTr.Text = "100 üzerinden: " + q.YaziliSinaviNotu;
                    this.cellYaziliYuzlukNotEn.Text = "Out of 100: " + q.YaziliSinaviNotu;
                }
                else
                {
                    this.cellYaziliYuzlukNotTr.Text = "Sınava Katılmadı";
                }
                this.cellYaziliAltBaslikSinavNotuTr.Text = this.cellYaziliAltBaslikSinavNotuTr.Text.Replace("@sinavNotu", q.OgrenimTipKriter.YaziliGecerNot.Value.ToString("n0"));
                this.cellYaziliAltBaslikSinavNotuEn.Text = this.cellYaziliAltBaslikSinavNotuEn.Text.Replace("@sinavNotu", q.OgrenimTipKriter.YaziliGecerNot.Value.ToString("n0"));
                this.chkYaziliBasarili.Checked = q.IsYaziliSinavBasarili == true;
                this.chkYaziliBasarisiz.Checked = !this.chkYaziliBasarili.Checked;

                if (q.IsYaziliSinavinaKatildi == true && q.IsYaziliSinavBasarili==true)
                {

                    this.cellSozluSinavTarih.Text = q.SozluSinavTarihi.ToFormatDateAndTime();
                    this.cellSozluSinavYeri.Text = q.SozluSinavYeri;
                    if (q.IsSozluSinavinaKatildi == true)
                    {
                        this.cellSozluYuzlukNotTr.Text = "100 üzerinden: " + q.SozluSinaviOrtalamaNotu;
                        this.cellSozluYuzlukNotEn.Text = "Out of 100: " + q.SozluSinaviOrtalamaNotu;
                    }
                    else
                    {
                        this.cellSozluYuzlukNotTr.Text = "Sınava Katılmadı";
                    }

                    this.chkSozluSinavOnline.Checked = q.IsSozluSinavOnline == true;
                    this.chkSozluSinavYuzYuze.Checked = !chkSozluSinavOnline.Checked;
                    this.chkSozluBasarili.Checked = q.IsSozluSinavBasarili == true;
                    this.chkSozluBasarisiz.Checked = !this.chkSozluBasarili.Checked; 
                } 
                this.cellSozluAltBaslikSinavNotuTr.Text =
                    this.cellSozluAltBaslikSinavNotuTr.Text.Replace("@sinavNotu",
                        q.OgrenimTipKriter.SozluGecerNot.Value.ToString("n0"));
                this.cellSozluAltBaslikSinavNotuEn.Text =
                    this.cellSozluAltBaslikSinavNotuEn.Text.Replace("@sinavNotu",
                        q.OgrenimTipKriter.SozluGecerNot.Value.ToString("n0"));
             
                if (q.IsSozluSinavinaKatildi == true)
                { 
                    this.cellSonucYuzlukNotTr.Text = "100 üzerinden: " + q.GenelBasariNotu;
                    this.cellSonucYuzlukNotEn.Text = "Out of 100: " + q.GenelBasariNotu;

                }
                


                this.cellSonucAltBaslikSinavNotuTr.Text = this.cellSonucAltBaslikSinavNotuTr.Text.Replace("@sinavNotu", q.OgrenimTipKriter.OrtalamaGecerNot.Value.ToString("n0"));
                this.cellSonucAltBaslikSinavNotuEn.Text = this.cellSonucAltBaslikSinavNotuEn.Text.Replace("@sinavNotu", q.OgrenimTipKriter.OrtalamaGecerNot.Value.ToString("n0"));
                this.chkGenelBasarili.Checked = q.IsGenelSonucBasarili == true;
                this.chkGenelBasarisiz.Checked = !this.chkGenelBasarili.Checked;

                foreach (var item in q.Juris.Select((s, inx) => new { s, inx }))
                {
                    tblJuris.Rows[item.inx + 1].Cells[0].Text = item.s.UnvanAdi + "\r\n" + item.s.AdSoyad;
                    tblJuris.Rows[item.inx + 1].Cells[1].Text = item.s.AnabilimDaliAdi + "\r\n" + item.s.UniversiteAdi;
                    tblJuris.Rows[item.inx + 1].Cells[2].Text = "";
                }




            }
        }


    }
}