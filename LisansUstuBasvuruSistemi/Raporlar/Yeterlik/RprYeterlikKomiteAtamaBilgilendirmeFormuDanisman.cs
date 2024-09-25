using Entities.Entities;
using System.Linq;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Raporlar.Yeterlik
{
    public partial class RprYeterlikKomiteAtamaBilgilendirmeFormuDanisman : DevExpress.XtraReports.UI.XtraReport
    {
        public RprYeterlikKomiteAtamaBilgilendirmeFormuDanisman(int yeterlikBasvuruId)
        {
            InitializeComponent();
            this.DisplayName = "Komite Atama Gerekliliği Bilgi Formu";
            using (var entities = new LubsDbEntities())
            {
                var yeterlikBasvuru = entities.YeterlikBasvurus.First(f => f.YeterlikBasvuruID == yeterlikBasvuruId);
                var enstitu = yeterlikBasvuru.YeterlikSureci.Enstituler;
                var anabilimDaliAdi = yeterlikBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi.IlkHarfiBuyut();
                var programAdi = yeterlikBasvuru.Programlar.ProgramAdi.IlkHarfiBuyut();
                var ogrenciNo = yeterlikBasvuru.OgrenciNo;
                var ogrenciAdSoyad = yeterlikBasvuru.Kullanicilar.Ad.IlkHarfiBuyut() + " " + yeterlikBasvuru.Kullanicilar.Soyad.ToUpper();
                var isEnstituMudurOrVekil = !enstitu.EnstituMudurVekilId.HasValue;
                var mudurId = isEnstituMudurOrVekil ? enstitu.EnstituMudurId : enstitu.EnstituMudurVekilId;
                var mudurAdi = "";
                if (mudurId.HasValue)
                {
                    var mudur = entities.Kullanicilars.First(f => f.KullaniciID == mudurId); 
                    mudurAdi = (mudur.Unvanlar.UnvanAdi + " " + mudur.Ad).IlkHarfiBuyut() + " " + mudur.Soyad.ToUpper();
                }
                var teslimSonTarih = yeterlikBasvuru.SozluSinavTarihi.Value.AddMonths(1).ToFormatDate();
                lblKonu.Text = "Komite Atama Gerekliliği Hk.";
                lblEnstituBaslik.Text = enstitu.EnstituAd.ToUpper();
                var rtfText = richContent.Rtf;
                var danisman = entities.Kullanicilars.First(f => f.KullaniciID == yeterlikBasvuru.TezDanismanID);
                var danismanAdi = (danisman.Unvanlar.UnvanAdi + " " + danisman.Ad).IlkHarfiBuyut() + " " + danisman.Soyad.ToUpper();
                rtfText = rtfText.Replace("@Baslik", "Sayın " + danismanAdi);
                rtfText = rtfText.Replace("@YeterlikSozluSinavTarihi", yeterlikBasvuru.SozluSinavTarihi.ToFormatDate());
                rtfText = rtfText.Replace("@OgrenciAnabilimDali", anabilimDaliAdi);
                rtfText = rtfText.Replace("@OgrenciProgrami", programAdi);
                rtfText = rtfText.Replace("@OgrenciNo", ogrenciNo);
                rtfText = rtfText.Replace("@OgrenciAdSoyad", ogrenciAdSoyad);
                rtfText = rtfText.Replace("@EnstituMuduruTitle", isEnstituMudurOrVekil ? "Enstitü Müdürü" : "Enstitü Müdür V.");
                rtfText = rtfText.Replace("@EnstituMuduru", mudurAdi);
                rtfText = rtfText.Replace("@TeslimSonTarih", teslimSonTarih);
                richContent.ResetTextAlignment();
                richContent.Rtf = rtfText;

                cellAdres.Text = enstitu.Adres;
                cellTel.Text = enstitu.Tel;
                cellEnstituAdi.Text = enstitu.EnstituAd;
                cellWebAdresi.Text = enstitu.WebAdresi;
                cellEposta.Text = enstitu.EPosta;


            }

        }

    }
}
