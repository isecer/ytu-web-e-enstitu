using System.Linq;
using DevExpress.XtraReports.UI;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.TezIzleme
{
    public partial class RprTiDegerlendirmeFormu_FR0307 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprTiDegerlendirmeFormu_FR0307(int tiBasvuruAraRaporId)
        {
            InitializeComponent();

            using (var entities = new LubsDbEntities())
            {


                var data = (from s in entities.TIBasvuruAraRapors
                            join sr in entities.SRTalepleris on s.TIBasvuruAraRaporID equals sr.TIBasvuruAraRaporID
                            join mb in entities.TIBasvurus on s.TIBasvuruID equals mb.TIBasvuruID
                            join k in entities.Kullanicilars on mb.KullaniciID equals k.KullaniciID
                            join e in entities.Enstitulers on mb.EnstituKod equals e.EnstituKod
                            join prg in entities.Programlars on mb.ProgramKod equals prg.ProgramKod
                            join abd in entities.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                            where s.TIBasvuruAraRaporID == tiBasvuruAraRaporId
                            select new
                            {
                                k.OgrenciNo,
                                s.FormKodu,
                                AdSoyad = k.Ad + " " + k.Soyad,
                                e.EnstituAd,
                                abd.AnabilimDaliAdi,
                                prg.ProgramAdi,
                                OgrenciKayitDonemi = mb.KayitOgretimYiliBaslangic + " - " + (mb.KayitOgretimYiliBaslangic + 1) + " / " + (mb.KayitOgretimYiliDonemID == 1 ? "Güz" : "Bahar") + " (" + (mb.KayitOgretimYiliDonemID == 1 ? "Fall" : "Spring") + ")",
                                s.IsYokDrBursiyeriVar,
                                YokDrBursiyeri = s.IsYokDrBursiyeriVar ? "Evet (Yes)" : "Hayır (No)",
                                YokDrOncelikliAlan = s.IsYokDrBursiyeriVar ? s.YokDrOncelikliAlan : "",
                                TezDili = s.IsTezDiliTr ? "Türkçe (Turkish)" : "İngilizce (English)",
                                s.TezBaslikTr,
                                s.TezBaslikEn,
                                ToplantiSekli = sr.IsOnline ? "Çevrimiçi\r\n(Online)" : "Yüz yüze\r\n(Face-to-face)",
                                ToplantiTarihi = sr.Tarih,
                                ToplantiSaati = sr.BasSaat,
                                Toplantiyeri = sr.SalonAdi,
                                TezIzlemeRaporDonemi = s.DonemBaslangicYil + " - " + (s.DonemBaslangicYil + 1) + " / " + (s.DonemID == 1 ? "Güz" : "Bahar") + " (" + (s.DonemID == 1 ? "Fall" : "Spring") + ")",
                                s.AraRaporSayisi,
                                KomiteCount = s.TIBasvuruAraRaporKomites.Count,
                                IsTezIzlemeRaporuTezOnerisiUygunCount = s.TIBasvuruAraRaporKomites.Count(p => p.IsTezIzlemeRaporuTezOnerisiUygun == true),
                                IsTezIzlemeRaporuTezOnerisiUygunDegilCount = s.TIBasvuruAraRaporKomites.Count(p => p.IsTezIzlemeRaporuTezOnerisiUygun == false),
                                IsTezIzlemeRaporuAltAlanUygun = s.TIBasvuruAraRaporKomites.Any(p => p.JuriTipAdi == "TezDanismani" && p.IsTezIzlemeRaporuAltAlanUygun == true),
                                IsBasariliCount = s.TIBasvuruAraRaporKomites.Count(p => p.IsBasarili == true),
                                IsBasariliDegilCount = s.TIBasvuruAraRaporKomites.Count(p => p.IsBasarili == false),
                                Danisman = s.TIBasvuruAraRaporKomites.FirstOrDefault(p => p.JuriTipAdi == "TezDanismani"),
                                TikUyesi1 = s.TIBasvuruAraRaporKomites.FirstOrDefault(p => p.JuriTipAdi == "TikUyesi1"),
                                TikUyesi2 = s.TIBasvuruAraRaporKomites.FirstOrDefault(p => p.JuriTipAdi == "TikUyesi2"),
                                s.IsTezBasligiDegisti,
                                YeniTezBaslikTr = s.IsTezBasligiDegisti ? s.YeniTezBaslikTr : "",
                                YeniTezBaslikEn = s.IsTezBasligiDegisti ? s.YeniTezBaslikEn : "",
                                urlAdd = e.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "TIDF_" + s.TIBasvuruAraRaporID + "_" + s.UniqueID
                            }).First();

                this.DisplayName = "FR-0307 DOKTORA TEZ İZLEME RAPORU FORMU";

                cellOgrenciNo.Text = data.OgrenciNo;
                xrTableFK.Text = "Form Kodu: " + data.FormKodu;
                cellOgrenciAdSoyad.Text = data.AdSoyad;
                cellOgrenciEnstituAdi.Text = data.EnstituAd;
                cellOgrenciAnabilimDaliAdi.Text = data.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = data.ProgramAdi;
                cellOgrenciKayitDonemi.Text = data.OgrenciKayitDonemi;
                cellYok200BursiyeriVarMi.Text = data.YokDrBursiyeri;
                cellYok200BursiyeriAltAlan.Text = data.YokDrOncelikliAlan;
                cellTezDili.Text = data.TezDili;
                cellTezBasligiTr.Text = data.TezBaslikTr;
                cellTezBasligiEn.Text = data.TezBaslikEn;
                cellToplantiSekli.Text = data.ToplantiSekli;
                cellToplantiTarihi.Text = data.ToplantiTarihi.ToLongDateString() + "\n\r" + $"{data.ToplantiSaati:hh\\:mm}";
                cellToplantiyeri.Text = data.Toplantiyeri;
                cellTezIzlemeRaporDonemAdi.Text = data.TezIzlemeRaporDonemi;
                cellTezIzlemeRaporSayisi.Text = data.AraRaporSayisi.ToString();
                bool tezOnerisiUygun = data.IsTezIzlemeRaporuTezOnerisiUygunCount > data.IsTezIzlemeRaporuTezOnerisiUygunDegilCount;
                bool tezOnerisiUygunOyBirligiOrCoklugu = (tezOnerisiUygun ? data.IsTezIzlemeRaporuTezOnerisiUygunCount : data.IsTezIzlemeRaporuTezOnerisiUygunDegilCount) == data.KomiteCount;
                cellTezOnerisiUyumu.Text = (tezOnerisiUygunOyBirligiOrCoklugu ? "OY BİRLİĞİ İLE " : "OY ÇOKLUĞU İLE ") + (tezOnerisiUygun ? "UYGUN " : "UYGUN DEĞİL") + "\r\n(" + (tezOnerisiUygunOyBirligiOrCoklugu ? "UNANIMOUSLY " : "BY MAJORITY ") + (tezOnerisiUygun ? "COMPATIBLE " : "INCOMPATIBLE") + ")";
                if (data.IsYokDrBursiyeriVar)
                {
                    cellTezYok2000BursAltAlanUyumu.Text = (data.IsTezIzlemeRaporuAltAlanUygun ? "UYGUN " : "UYGUN DEĞİL") + "\r\n(" + (data.IsTezIzlemeRaporuAltAlanUygun ? "COMPATIBLE " : "INCOMPATIBLE") + ")";
                }
                else cellTezYok2000BursAltAlanUyumu.Text = "";
                bool tezDegerlendirmeBasarili = data.IsBasariliCount > data.IsBasariliDegilCount;
                bool tezDegerlendirmeBasariliOyBirligiOrCoklugu = (tezDegerlendirmeBasarili ? data.IsBasariliCount : data.IsBasariliDegilCount) == data.KomiteCount;
                cellTezDegerlendirmeSonucu.Text = (tezDegerlendirmeBasariliOyBirligiOrCoklugu ? "OY BİRLİĞİ İLE " : "OY ÇOKLUĞU İLE ") + (tezDegerlendirmeBasarili ? "BAŞARILI " : "BAŞARISIZ") + "\r\n(" + (tezDegerlendirmeBasariliOyBirligiOrCoklugu ? "UNANIMOUSLY " : "BY MAJORITY ") + (tezDegerlendirmeBasarili ? "SUCCESSFUL " : "UNSUCCESSFUL") + ")";

                cellYeniTezBaslikTr.Text = data.YeniTezBaslikTr;
                cellYeniTezBaslikEn.Text = data.YeniTezBaslikEn;
                cellDanismanUnvanAdSoyad.Text = data.Danisman.UnvanAdi + "\r\n" + data.Danisman.AdSoyad;
                cellDanismanAbdUniversiteAdi.Text = data.Danisman.AnabilimdaliProgramAdi + " \r\n" + data.Danisman.UniversiteAdi;
                cellTik1UnvanAdSoyad.Text = data.TikUyesi1.UnvanAdi + "\r\n" + data.TikUyesi1.AdSoyad;
                cellTik1AbdUniversiteAdi.Text = data.TikUyesi1.AnabilimdaliProgramAdi + " \r\n" + data.TikUyesi1.UniversiteAdi;
                cellTik2UnvanAdSoyad.Text = data.TikUyesi2.UnvanAdi + "\r\n " + data.TikUyesi2.AdSoyad;
                cellTik2AbdUniversiteAdi.Text = data.TikUyesi2.AnabilimdaliProgramAdi + "\r\n" + data.TikUyesi2.UniversiteAdi;
                xRowYokBursAltAlan.Visible = data.IsYokDrBursiyeriVar;
                xrIsAltAlan.Visible = data.IsYokDrBursiyeriVar;
                xrAltAlanAdi.Visible = data.IsYokDrBursiyeriVar;
                sbantTezBasligiDegisim.Visible = data.IsTezBasligiDegisti;
                cellOnay.Text = "G. ONAY";


                detReportSinav.Visible = false;


                cellFormKodu.Text = "Form Kodu: " + data.FormKodu;
                xrQRCode.ImageUrl = data.urlAdd;
                xrQRCode.Image = data.urlAdd.CreateQrCode();

            }
        }

    }
}
