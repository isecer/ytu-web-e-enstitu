using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{
    public partial class RprMezuniyetCiltliTezTeslimFormu_FR1243 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprMezuniyetCiltliTezTeslimFormu_FR1243(int id)
        {
            InitializeComponent();

            using (var  entities = new LubsDbEntities())
            {


                var data = (from s in entities.MezuniyetBasvurulariTezTeslimFormlaris 
                            join mb in entities.MezuniyetBasvurularis on s.MezuniyetBasvurulariID equals mb.MezuniyetBasvurulariID
                            join ms in entities.MezuniyetSurecis on mb.MezuniyetSurecID equals ms.MezuniyetSurecID
                            join dnm in entities.Donemlers on mb.KayitOgretimYiliDonemID equals dnm.DonemID
                            join enst in entities.Enstitulers on ms.EnstituKod equals enst.EnstituKod
                            join osl in entities.OgrenimTipleris on new { mb.OgrenimTipKod, enst.EnstituKod } equals new { osl.OgrenimTipKod, osl.EnstituKod }
                            join prg in entities.Programlars on mb.ProgramKod equals prg.ProgramKod
                            join abd in entities.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                            where s.MezuniyetBasvurulariTezTeslimFormID == id
                            select new
                            {
                                s.MezuniyetBasvurulariTezTeslimFormID,
                                mb.MezuniyetBasvurulariID,
                                ms.EnstituKod,
                                enst.SistemErisimAdresi,
                                mb.OgrenciNo,
                                s.RowID,
                                AdSoyad = mb.Ad + " " + mb.Soyad,
                                DonemAdi = mb.KayitOgretimYiliBaslangic + " / " + (mb.KayitOgretimYiliBaslangic + 1) + " " + dnm.DonemAdi,
                                EnstituAdi = enst.EnstituAd,
                                osl.OgrenimTipAdi,
                                abd.AnabilimDaliAdi,
                                prg.ProgramAdi,
                                OgrenciKayitDonemi = mb.KayitOgretimYiliBaslangic + " - " + (mb.KayitOgretimYiliBaslangic + 1) + " / " + (mb.KayitOgretimYiliDonemID == 1 ? "Güz" : "Bahar") + " (" + (mb.KayitOgretimYiliDonemID == 1 ? "Fall" : "Spring") + ")",

                                s.IsTezDiliTr,
                                s.TezDili,
                                s.TezBaslikTr,
                                s.TezBaslikEn,
                                mb.TezTeslimSonTarih,
                                urlAdd = enst.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MBBBC_" + s.MezuniyetBasvurulariTezTeslimFormID + "_" + s.RowID
                            }).First();

                var sonSr = entities.SRTalepleris.First(f =>
                    f.MezuniyetBasvurulariID==data.MezuniyetBasvurulariID && f.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili);
                this.DisplayName = data.AdSoyad + " FR-1243 Lisansüstü Ciltli Tez Teslim Formu";
                cellOgrenciNo.Text = data.OgrenciNo;
                cellOgrenciAdSoyad.Text = data.AdSoyad;
                cellOgrenciEnstituAdi.Text = data.EnstituAdi;
                cellOgrenciAnabilimDaliAdi.Text = data.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = data.ProgramAdi;
                cellOgrenciOgrenimSeviyesi.Text = data.OgrenimTipAdi;
                cellOgrenciKayitDonemi.Text = data.OgrenciKayitDonemi;

                var mezuniyetBasvuru = sonSr.MezuniyetBasvurulari;
                var joForm = mezuniyetBasvuru.MezuniyetJuriOneriFormlaris.First(); 
                var tezBasligiDegisenSinav = mezuniyetBasvuru.SRTalepleris.FirstOrDefault(p =>
                     p.SRDurumID == SrTalepDurumEnum.Onaylandı && p.IsTezBasligiDegisti == true);



                var tezBaslikTr = "";
                var tezBaslikEn = "";
                 
                if (tezBasligiDegisenSinav != null)
                {
                    tezBaslikTr = tezBasligiDegisenSinav.YeniTezBaslikTr;
                    tezBaslikEn = tezBasligiDegisenSinav.YeniTezBaslikEn;
                }
                else if (joForm.IsTezBasligiDegisti == true)
                {
                    tezBaslikTr = joForm.YeniTezBaslikTr;
                    tezBaslikEn = joForm.YeniTezBaslikEn;
                }
                else
                {
                    tezBaslikTr = mezuniyetBasvuru.TezBaslikTr;
                    tezBaslikEn = mezuniyetBasvuru.TezBaslikEn;
                }

             


                cellTezDili.Text = data.IsTezDiliTr ? "Türkçe (Turkish)" : "İngilizce (English)";
                cellTezBaslikTr.Text = tezBaslikTr;
                cellTezBaslikEn.Text = tezBaslikEn;
                CellTezSavunmaSinavTarihi.Text = sonSr.Tarih.ToFormatDate();

                cellImzaOgrenciAdSoyad.Text = data.AdSoyad;

                cellFormKodu.Text = "Form Kodu: " + data.RowID.ToString().Substring(0, 8).ToUpper();
                var qrUlr = data.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MBBBC_" + data.MezuniyetBasvurulariTezTeslimFormID + "_" + data.RowID;
                xrQRCode.ImageUrl = qrUlr;
                xrQRCode.Image = qrUlr.CreateQrCode();

            }
        }

    }
}
