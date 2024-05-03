using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Raporlar.DonemProjesi
{
    public partial class RprDpSinavTutanakFormuDetay_FR0366 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprDpSinavTutanakFormuDetay_FR0366(int donemProjesiBasvuruId)
        {
            InitializeComponent();

            using (var entities = new LubsDbEntities())
            {
                var data = (from s in entities.DonemProjesiBasvurus
                            where s.DonemProjesiBasvuruID == donemProjesiBasvuruId
                            select new
                            {
                                s.FormKodu,
                                Juriler = s.DonemProjesiJurileris.ToList(),
                                urlAdd = s.DonemProjesi.Enstituler.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "DPSF_" + s.DonemProjesiBasvuruID + "_" + s.UniqueID
                            }).First();

                this.DisplayName = "FR-0366 DÖNEM PROJESİ SINAVI DEĞERLENDİRME EKİ";

                xrTableFK.Text = "Form Kodu: " + data.FormKodu;
                xrQRCode.ImageUrl = data.urlAdd;
                xrQRCode.Image = data.urlAdd.CreateQrCode();

                var danisman = data.Juriler.FirstOrDefault(p => p.IsTezDanismani);
                var juriUyesi1 = data.Juriler.Where(p => !p.IsTezDanismani).OrderBy(o => o.DonemProjesiJuriID).ToList()[0];
                var juriUyesi2 = data.Juriler.Where(p => !p.IsTezDanismani).OrderBy(o => o.DonemProjesiJuriID).ToList()[1];


                cellYurutucuUnvanAdSoyad.Text = danisman.UnvanAdi + "\r\n" + danisman.AdSoyad;
                cellYurutucuAbdUniversiteAdi.Text = danisman.AnabilimdaliAdi + "\r\n" + GlobalSistemSetting.UniversiteAdi;
                cellYurutucuDegerlendirmeSonucu.Text =
                  danisman.DonemProjesiJuriOnayDurumID == DonemProjesiJuriOnayDurumEnum.Basarili
                      ? "BAŞARILI (SUCCESSFUL)"
                      : (danisman.DonemProjesiJuriOnayDurumID == DonemProjesiJuriOnayDurumEnum.Basarisiz
                          ? "BAŞARISIZ (UNSUCCESSFUL)"
                          : "BAŞARISIZ KATILMADI (UNSUCCESSFUL)"
                      );
                cellYurutucuDegerlendirmeAciklama.Text = danisman.Aciklama.IsNullOrWhiteSpace() ? "" : danisman.Aciklama;
                cellYurutucuTarihImza.Text = danisman.DegerlendirmeIslemTarihi.ToFormatDateAndTime() + "\r\nTarihinde Elektronik Olarak Onaylandı";

                cellJuri1UnvanAdSoyad.Text = juriUyesi1.UnvanAdi + " \r\n" + juriUyesi1.AdSoyad;
                cellJuri1AbdUniversiteAdi.Text = juriUyesi1.AnabilimdaliAdi + "\r\n" + GlobalSistemSetting.UniversiteAdi;
                cellJuri1DegerlendirmeSonucu.Text = juriUyesi1.DonemProjesiJuriOnayDurumID == DonemProjesiJuriOnayDurumEnum.Basarili
                    ? "BAŞARILI (SUCCESSFUL)"
                    : (juriUyesi1.DonemProjesiJuriOnayDurumID == DonemProjesiJuriOnayDurumEnum.Basarisiz
                       ? "BAŞARISIZ (UNSUCCESSFUL)"
                       : "BAŞARISIZ KATILMADI (UNSUCCESSFUL)"
                   );
                cellJuri1DegerlendirmeAciklama.Text = juriUyesi1.Aciklama.IsNullOrWhiteSpace() ? "" : juriUyesi1.Aciklama;
                cellJuri1TarihImza.Text = juriUyesi1.DegerlendirmeIslemTarihi.ToFormatDateAndTime() + "\r\nTarihinde Elektronik Olarak Onaylandı";

                cellJuri2UnvanAdSoyad.Text = juriUyesi2.UnvanAdi + "\r\n" + juriUyesi2.AdSoyad;
                cellJuri2AbdUniversiteAdi.Text = juriUyesi2.AnabilimdaliAdi + "\r\n" + GlobalSistemSetting.UniversiteAdi;
                cellJuri2DegerlendirmeSonucu.Text = juriUyesi2.DonemProjesiJuriOnayDurumID == DonemProjesiJuriOnayDurumEnum.Basarili
                    ? "BAŞARILI (SUCCESSFUL)"
                    : (juriUyesi2.DonemProjesiJuriOnayDurumID == DonemProjesiJuriOnayDurumEnum.Basarisiz
                        ? "BAŞARISIZ (UNSUCCESSFUL)"
                        : "BAŞARISIZ KATILMADI (UNSUCCESSFUL)"
                    );
                cellJuri2DegerlendirmeAciklama.Text = juriUyesi2.Aciklama.IsNullOrWhiteSpace() ? "" : juriUyesi2.Aciklama;
                cellJuri2TarihImza.Text = juriUyesi2.DegerlendirmeIslemTarihi.ToFormatDateAndTime() + "\r\nTarihinde Elektronik Olarak Onaylandı";
            }
        }

    }
}
