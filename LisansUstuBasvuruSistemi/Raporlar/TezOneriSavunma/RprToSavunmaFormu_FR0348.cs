using System.Linq;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.TezOneriSavunma
{
    public partial class RprToSavunmaFormu_FR0348 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprToSavunmaFormu_FR0348(int id)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {


                var data = (from s in db.ToBasvuruSavunmas
                            join sr in db.SRTalepleris on s.ToBasvuruSavunmaID equals sr.ToBasvuruSavunmaID
                            join mb in db.ToBasvurus on s.ToBasvuruID equals mb.ToBasvuruID
                            join k in db.Kullanicilars on mb.KullaniciID equals k.KullaniciID
                            join e in db.Enstitulers on mb.EnstituKod equals e.EnstituKod
                            join prg in db.Programlars on mb.ProgramKod equals prg.ProgramKod
                            join abd in db.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                            where s.ToBasvuruSavunmaID == id
                            select new
                            {
                                k.OgrenciNo,
                                s.FormKodu,
                                mb.YeterlikSozluSinavTarihi,
                                AdSoyad = k.Ad + " " + k.Soyad,
                                e.EnstituAd,
                                abd.AnabilimDaliAdi,
                                prg.ProgramAdi,
                                OgrenciKayitDonemi = mb.KayitOgretimYiliBaslangic + " - " + (mb.KayitOgretimYiliBaslangic + 1) + " / " + (mb.KayitOgretimYiliDonemID == 1 ? "Güz" : "Bahar") + " (" + (mb.KayitOgretimYiliDonemID == 1 ? "Fall" : "Spring") + ")",
                                s.IsYokDrBursiyeriVar,
                                YokDrBursiyeri = s.IsYokDrBursiyeriVar ? "Evet (Yes)" : "Hayır (No)",
                                YokDrOncelikliAlan = s.IsYokDrBursiyeriVar ? s.YokDrOncelikliAlan : "",
                                TezDili = s.IsTezDiliTr ? "Türkçe (Turkish)" : "İngilizce (English)",
                                ToplantiSekli = sr.IsOnline ? "Çevrimiçi\r\n(Online)" : "Yüz yüze\r\n(Face-to-face)",
                                ToplantiTarihi = sr.Tarih,
                                ToplantiSaati = sr.BasSaat,
                                Toplantiyeri = sr.SalonAdi,
                                TezIzlemeRaporDonemi = s.DonemBaslangicYil + " - " + (s.DonemBaslangicYil + 1) + " / " + (s.DonemID == 1 ? "Güz" : "Bahar") + " (" + (s.DonemID == 1 ? "Fall" : "Spring") + ")",
                                KomiteCount = s.ToBasvuruSavunmaKomites.Count,
                                IsTezIzlemeRaporuAltAlanUygun = s.ToBasvuruSavunmaKomites.Any(p => p.IsTezDanismani && p.IsCalismaRaporuAltAlanUygun == true),
                                s.ToBasvuruSavunmaDurumID,
                                s.IsOyBirligiOrCoklugu,
                                Danisman = s.ToBasvuruSavunmaKomites.FirstOrDefault(p => p.IsTezDanismani),
                                TikUyesi1 = s.ToBasvuruSavunmaKomites.FirstOrDefault(p => p.TikNum == 1),
                                TikUyesi2 = s.ToBasvuruSavunmaKomites.FirstOrDefault(p => p.TikNum == 2),
                                s.YeniTezBaslikTr,
                                s.YeniTezBaslikEn,
                                urlAdd = e.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "TOSF_" + s.ToBasvuruSavunmaID + "_" + s.UniqueID
                            }).First();

                this.DisplayName = "FR-0348 DOKTORA TEZ ÖNERİ FORMU";
                cellFormKodu.Text = "Form Kodu: " + data.FormKodu;
                xrQRCode.ImageUrl = data.urlAdd;
                xrQRCode.Image = data.urlAdd.CreateQrCode();

                cellYeterlikBasariTarihi.Text = data.YeterlikSozluSinavTarihi.ToFormatDate();
                cellOgrenciNo.Text = data.OgrenciNo;
                cellOgrenciAdSoyad.Text = data.AdSoyad;
                cellOgrenciEnstituAdi.Text = data.EnstituAd;
                cellOgrenciAnabilimDaliAdi.Text = data.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = data.ProgramAdi;
                cellOgrenciKayitDonemi.Text = data.OgrenciKayitDonemi;
                cellYok200BursiyeriVarMi.Text = data.YokDrBursiyeri;
                cellYok200BursiyeriAltAlan.Text = data.YokDrOncelikliAlan;
                cellTezDili.Text = data.TezDili;
                cellToplantiSekli.Text = data.ToplantiSekli;
                cellToplantiTarihi.Text = data.ToplantiTarihi.ToLongDateString() + "\n\r" + $"{data.ToplantiSaati:hh\\:mm}";
                cellToplantiyeri.Text = data.Toplantiyeri;
                if (data.IsYokDrBursiyeriVar)
                {
                    cellTezYok2000BursAltAlanUyumu.Text = (data.IsTezIzlemeRaporuAltAlanUygun ? "UYGUN " : "UYGUN DEĞİL") + "\r\n(" + (data.IsTezIzlemeRaporuAltAlanUygun ? "COMPATIBLE " : "INCOMPATIBLE") + ")";
                }
                else cellTezYok2000BursAltAlanUyumu.Text = "";



                var strDurumAdiTr =
                    data.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.KabulEdildi
                        ? "KABUL EDİLDİ"
                        : (data.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.RetEdildi
                            ? "RET EDİLDİ"
                            : "DÜZELTME TALEP EDİLDİ");
                var strDurumAdiEn =
                    data.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.KabulEdildi
                        ? "ACCEPTED"
                        : (data.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.RetEdildi
                            ? "REJECTED"
                            : "REVISION");

                cellTezDegerlendirmeSonucu.Text = (data.IsOyBirligiOrCoklugu == true ? "OY BİRLİĞİ İLE " : "OY ÇOKLUĞU İLE ") + strDurumAdiTr +
                                                  "\r\n(" + (data.IsOyBirligiOrCoklugu == true ? "UNANIMOUSLY " : "BY MAJORITY ") + strDurumAdiEn + ")";


                cellYeniTezBaslikTr.Text = data.YeniTezBaslikTr;
                cellYeniTezBaslikEn.Text = data.YeniTezBaslikEn;

                cellDanismanUnvanAdSoyad.Text = data.Danisman.UnvanAdi + "\r\n" + data.Danisman.AdSoyad;
                cellDanismanAbdUniversiteAdi.Text = data.Danisman.AnabilimdaliAdi + " \r\n" + data.Danisman.UniversiteAdi;
                cellTik1UnvanAdSoyad.Text = data.TikUyesi1.UnvanAdi + "\r\n" + data.TikUyesi1.AdSoyad;
                cellTik1AbdUniversiteAdi.Text = data.TikUyesi1.AnabilimdaliAdi + " \r\n" + data.TikUyesi1.UniversiteAdi;
                cellTik2UnvanAdSoyad.Text = data.TikUyesi2.UnvanAdi + "\r\n " + data.TikUyesi2.AdSoyad;
                cellTik2AbdUniversiteAdi.Text = data.TikUyesi2.AnabilimdaliAdi + "\r\n" + data.TikUyesi2.UniversiteAdi;
                xRowYokBursAltAlan.Visible = data.IsYokDrBursiyeriVar;
                xrIsAltAlan.Visible = data.IsYokDrBursiyeriVar;
                xrAltAlanAdi.Visible = data.IsYokDrBursiyeriVar;




            }
        }

    }
}
