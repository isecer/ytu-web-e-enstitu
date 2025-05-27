using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Raporlar.KayitSilme
{
    public partial class RprKayitSilmeTalepFormu_FR_0000 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprKayitSilmeTalepFormu_FR_0000(int id)
        {
            InitializeComponent();
            using (var entities = new LubsDbEntities())
            {


                var data = (from mb in entities.KayitSilmeBasvurus
                            join enst in entities.Enstitulers on mb.EnstituKod equals enst.EnstituKod
                            join osl in entities.OgrenimTipleris on new { mb.OgrenimTipKod, enst.EnstituKod } equals new { osl.OgrenimTipKod, osl.EnstituKod }
                            join prg in entities.Programlars on mb.ProgramKod equals prg.ProgramKod
                            join abd in entities.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                            join ogrenci in entities.Kullanicilars on mb.KullaniciID equals ogrenci.KullaniciID
                            join danisman in entities.Kullanicilars on mb.TezDanismanID equals danisman.KullaniciID into defD
                            from dDanisman in defD.DefaultIfEmpty()
                            join harcYetkili in entities.Kullanicilars on mb.HarcBirimiOnayYapanID equals harcYetkili.KullaniciID
                            join kutuphaneYetkili in entities.Kullanicilars on mb.KutuphaneBirimiOnayYapanID equals kutuphaneYetkili.KullaniciID
                            join enstituYetkili in entities.Kullanicilars on mb.EYKYaGonderildiIslemYapanID equals enstituYetkili.KullaniciID
                            where mb.KayitSilmeBasvuruID == id
                            select new
                            {
                                mb.KayitSilmeBasvuruID,
                                mb.EnstituKod,
                                EnstituAdi = enst.EnstituAd,
                                DonemAdi = mb.OgretimYiliBaslangic + " / " + (mb.OgretimYiliBaslangic + 1) + " " + mb.Donemler.DonemAdi,
                                mb.OgrenciNo,
                                OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                                mb.BasvuruTarihi,
                                osl.OgrenimTipAdi,
                                abd.AnabilimDaliAdi,
                                prg.ProgramAdi,
                                mb.CepTel,
                                mb.EPostaAdresi,
                                DanismanAdSoyad = dDanisman != null ? dDanisman.Unvanlar.UnvanAdi + " " + dDanisman.Ad + " " + dDanisman.Soyad : "",
                                HarcOnayYapanAdSoyad = harcYetkili.Unvanlar.UnvanAdi + " " + harcYetkili.Ad + " " + harcYetkili.Soyad,
                                mb.HarcBirimiOnayIslemTarihi,
                                KutuphaneOnayYapanAdSoyad = kutuphaneYetkili.Unvanlar.UnvanAdi + " " + kutuphaneYetkili.Ad + " " + kutuphaneYetkili.Soyad,
                                mb.KutuphaneBirimiOnayIslemTarihi,
                                EnstituOnayYapanAdSoyad = enstituYetkili.Unvanlar.UnvanAdi + " " + enstituYetkili.Ad + " " + enstituYetkili.Soyad,
                                mb.EYKYaGonderildiIslemTarihi
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
                cellImzaEnstituTarih.Text = data.EYKYaGonderildiIslemTarihi.ToFormatDateAndTime();


            }
        }

    }
}
