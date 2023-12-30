using System.Linq;
using BiskaUtil;
using DevExpress.Web.Internal.XmlProcessor;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.TezOneriSavunma
{
    public partial class RprToSavunmaFormuDetay_FR0348 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprToSavunmaFormuDetay_FR0348(int toBasvuruSavunmaId)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.ToBasvuruSavunmas
                            where s.ToBasvuruSavunmaID == toBasvuruSavunmaId
                            select new
                            {
                                s.IsYokDrBursiyeriVar,
                                s.FormKodu,
                                Danisman = s.ToBasvuruSavunmaKomites.FirstOrDefault(p => p.IsTezDanismani),
                                TikUyesi1 = s.ToBasvuruSavunmaKomites.FirstOrDefault(p => p.TikNum == 1),
                                TikUyesi2 = s.ToBasvuruSavunmaKomites.FirstOrDefault(p => p.TikNum == 2),
                                urlAdd = s.ToBasvuru.Enstituler.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "TOSF_" + s.ToBasvuruSavunmaID + "_" + s.UniqueID
                            }).First();

                this.DisplayName = "FR-0348 DOKTORA TEZ ÖNERİ FORMU DEĞERLENDİRME EKİ";

                xrTableFK.Text = "Form Kodu: " + data.FormKodu; 
                xrQRCode.ImageUrl = data.urlAdd;
                xrQRCode.Image = data.urlAdd.CreateQrCode();

                cellDanismanUnvanAdSoyad.Text = data.Danisman.UnvanAdi + "\r\n" + data.Danisman.AdSoyad;
                cellDanismanAbdUniversiteAdi.Text = data.Danisman.AnabilimdaliAdi + "\r\n" + data.Danisman.UniversiteAdi;
                cellDanismanTarihImza.Text = "";
                cellDanismanAlanUyum.Text = data.Danisman.IsCalismaRaporuAltAlanUygun == true ? "UYGUN (COMPATIBLE)" : "UYGUN DEĞİL (INCOMPATIBLE)";
                cellDanismanDegerlendirmeSonucu.Text =
                    data.Danisman.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.KabulEdildi
                        ? "KABUL (ACCEPTED)"
                        : (data.Danisman.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.RetEdildi
                            ? "RET (REJECTED)"
                            : "DÜZELTME (REVISION)"
                        );
                cellDanismanDegerlendirmeAciklama.Text = data.Danisman.Aciklama.IsNullOrWhiteSpace() ? "" : data.Danisman.Aciklama;
                cellDanismanTarihImza.Text = data.Danisman.DegerlendirmeIslemTarihi.ToFormatDateAndTime();

                RwAltAlanSoru.Visible = data.IsYokDrBursiyeriVar;
                if (!data.IsYokDrBursiyeriVar)
                {
                    cellSoru3.Text = cellSoru3.Text.Replace("2.", "1.");
                    cellSoru4.Text = cellSoru4.Text.Replace("3.", "2.");
                }

                cellTik1UnvanAdSoyad.Text = data.TikUyesi1.UnvanAdi + " \r\n" + data.TikUyesi1.AdSoyad;
                cellTik1AbdUniversiteAdi.Text = data.TikUyesi1.AnabilimdaliAdi + "\r\n" + data.TikUyesi1.UniversiteAdi;
                cellTik1TarihImza.Text = "";
                cellTik1DegerlendirmeSonucu.Text = data.TikUyesi1.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.KabulEdildi
                    ? "KABUL (ACCEPTED)"
                    : (data.TikUyesi1.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.RetEdildi
                        ? "RET (REJECTED)"
                        : "DÜZELTME (REVISION)"
                    );
                cellTik1DegerlendirmeAciklama.Text = data.TikUyesi1.Aciklama.IsNullOrWhiteSpace() ? "" : data.TikUyesi1.Aciklama;
                cellTik1TarihImza.Text = data.TikUyesi1.DegerlendirmeIslemTarihi.ToFormatDateAndTime();

                cellTik2UnvanAdSoyad.Text = data.TikUyesi2.UnvanAdi + "\r\n" + data.TikUyesi2.AdSoyad;
                cellTik2AbdUniversiteAdi.Text = data.TikUyesi2.AnabilimdaliAdi + "\r\n" + data.TikUyesi2.UniversiteAdi;
                cellTik2TarihImza.Text = "";
                cellTik2DegerlendirmeSonucu.Text = data.TikUyesi2.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.KabulEdildi
                    ? "KABUL (ACCEPTED)"
                    : (data.TikUyesi2.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.RetEdildi
                        ? "RET (REJECTED)"
                        : "DÜZELTME (REVISION)"
                    );
                cellTik2DegerlendirmeAciklama.Text = data.TikUyesi2.Aciklama.IsNullOrWhiteSpace() ? "" : data.TikUyesi2.Aciklama;
                cellTik2TarihImza.Text = data.TikUyesi2.DegerlendirmeIslemTarihi.ToFormatDateAndTime();
            }
        }

    }
}
