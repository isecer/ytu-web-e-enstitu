using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.KayitSilme
{
    public partial class RprKayitSilmeTalepFormu_FR_0000 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprKayitSilmeTalepFormu_FR_0000(int id)
        {
            InitializeComponent();
            using (var entities = new LubsDbEntities())
            {


                var data = (from ks in entities.KayitSilmeBasvurus
                            join enst in entities.Enstitulers on ks.EnstituKod equals enst.EnstituKod
                            join osl in entities.OgrenimTipleris on new { ks.OgrenimTipKod, enst.EnstituKod } equals new { osl.OgrenimTipKod, osl.EnstituKod }
                            join prg in entities.Programlars on ks.ProgramKod equals prg.ProgramKod
                            join abd in entities.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                            join ogrenci in entities.Kullanicilars on ks.KullaniciID equals ogrenci.KullaniciID
                            join danisman in entities.Kullanicilars on ks.TezDanismanID equals danisman.KullaniciID into defD
                            from dDanisman in defD.DefaultIfEmpty()
                            join harcYetkili in entities.Kullanicilars on ks.HarcBirimiOnayYapanID equals harcYetkili.KullaniciID
                            join kutuphaneYetkili in entities.Kullanicilars on ks.KutuphaneBirimiOnayYapanID equals kutuphaneYetkili.KullaniciID
                            join enstituYetkili in entities.Kullanicilars on ks.OnayMakaminaGonderildiIslemYapanID equals enstituYetkili.KullaniciID
                            where ks.KayitSilmeBasvuruID == id
                            select new
                            {
                                ks.KayitSilmeBasvuruID,
                                ks.EnstituKod,
                                ks.UniqueID,
                                EnstituAdi = enst.EnstituAd,
                                DonemAdi = ks.OgretimYiliBaslangic + " / " + (ks.OgretimYiliBaslangic + 1) + " " + ks.Donemler.DonemAdi,
                                ks.OgrenciNo,
                                OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                                ks.BasvuruTarihi,
                                osl.OgrenimTipAdi,
                                abd.AnabilimDaliAdi,
                                prg.ProgramAdi,
                                ks.CepTel,
                                ks.EPostaAdresi,
                                DanismanAdSoyad = dDanisman != null ? dDanisman.Unvanlar.UnvanAdi + " " + dDanisman.Ad + " " + dDanisman.Soyad : "",
                                HarcOnayYapanAdSoyad = harcYetkili.Unvanlar.UnvanAdi + " " + harcYetkili.Ad + " " + harcYetkili.Soyad,
                                ks.HarcBirimiOnayIslemTarihi,
                                KutuphaneOnayYapanAdSoyad = kutuphaneYetkili.Unvanlar.UnvanAdi + " " + kutuphaneYetkili.Ad + " " + kutuphaneYetkili.Soyad,
                                ks.KutuphaneBirimiOnayIslemTarihi,
                                EnstituOnayYapanAdSoyad = enstituYetkili.Unvanlar.UnvanAdi + " " + enstituYetkili.Ad + " " + enstituYetkili.Soyad,
                                ks.OnayMakaminaGonderildiIslemTarihi,
                                enst.SistemErisimAdresi,
                                urlAdd = enst.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "KSTF_" + ks.KayitSilmeBasvuruID + "_" + ks.UniqueID
                            }).First();


                this.DisplayName = data.OgrenciAdSoyad + " FR-0000 Kayıt Silme Talep Formu";
                cellDonemAdi.Text = data.DonemAdi;
                cellOgrenciNo.Text = data.OgrenciNo;
                cellOgrenciAdSoyad.Text = data.OgrenciAdSoyad;
                cellOgrenciEnstituAdi.Text = data.EnstituAdi;
                cellOgrenciAnabilimDaliAdi.Text = data.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = data.ProgramAdi;
                cellOgrenciOgrenimSeviyesi.Text = data.OgrenimTipAdi;
                cellCepTel.Text = data.CepTel;
                cellEPosta.Text = data.EPostaAdresi;
                cellTezDanismani.Text = data.DanismanAdSoyad;

                cellImzaOgrenciAdSoyad.Text = data.OgrenciAdSoyad;
                cellImzaOgrenciTarih.Text = data.BasvuruTarihi.ToFormatDateAndTime();

                cellImzaHarcAdSoyad.Text = data.HarcOnayYapanAdSoyad;
                cellImzaHarcTarih.Text = data.HarcBirimiOnayIslemTarihi.ToFormatDateAndTime();

                cellImzaKutuphaneAdSoyad.Text = data.KutuphaneOnayYapanAdSoyad;
                cellImzaKutuphaneTarih.Text = data.KutuphaneBirimiOnayIslemTarihi.ToFormatDateAndTime();

                cellImzaEnstituAdSoyad.Text = data.EnstituOnayYapanAdSoyad;
                cellImzaEnstituTarih.Text = data.OnayMakaminaGonderildiIslemTarihi.ToFormatDateAndTime();


                cellFormKodu.Text = "Form Kodu: " + data.UniqueID.ToString().Substring(0, 8).ToUpper();
                xrQRCode.ImageUrl = data.urlAdd;
                xrQRCode.Image = data.urlAdd.CreateQrCode();
            }
        }

    }
}
