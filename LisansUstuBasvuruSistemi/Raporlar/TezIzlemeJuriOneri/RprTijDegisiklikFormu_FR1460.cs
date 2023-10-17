using System.Linq;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.TezIzlemeJuriOneri
{
    public partial class RprTijDegisiklikFormu_FR1460 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprTijDegisiklikFormu_FR1460(int id)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {


                var data = (from s in db.TijBasvuruOneris
                            join mb in db.TijBasvurus on s.TijBasvuruID equals mb.TijBasvuruID
                            join k in db.Kullanicilars on mb.KullaniciID equals k.KullaniciID
                            join e in db.Enstitulers on mb.EnstituKod equals e.EnstituKod
                            join prg in db.Programlars on mb.ProgramKod equals prg.ProgramKod
                            join abd in db.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                            where s.TijBasvuruOneriID == id
                            select new
                            {
                                mb.KullaniciID,
                                s.TijFormTipID,
                                s.TijDegisiklikTipID,
                                k.OgrenciNo,
                                s.FormKodu,
                                AdSoyad = k.Ad + " " + k.Soyad,
                                e.EnstituAd,
                                abd.AnabilimDaliAdi,
                                prg.ProgramAdi,
                                OgrenciKayitDonemi = mb.KayitOgretimYiliBaslangic + " - " + (mb.KayitOgretimYiliBaslangic + 1) + " / " + (mb.KayitOgretimYiliDonemID == 1 ? "Güz" : "Bahar") + " (" + (mb.KayitOgretimYiliDonemID == 1 ? "Fall" : "Spring") + ")",
                                s.IsTezDiliTr,
                                TezDiliText = s.IsTezDiliTr ? "Türkçe (Turkish)" : "İngilizce (English)",
                                s.TezBaslikTr,
                                s.TezBaslikEn,
                                Danisman = s.Kullanicilar,
                                s.DanismanOnayTarihi,
                                Juriler = s.TijBasvuruOneriJurilers.ToList(),
                                urlAdd = e.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "TIJF_" + s.TijBasvuruOneriID + "_" + s.UniqueID
                            }).First();

                this.DisplayName = "FR-1460 DOKTORA TEZ İZLEME KOMİTE ÜYESİ DEĞİŞİKLİK ÖNERİ FORMU";

                cellOgrenciNo.Text = data.OgrenciNo;
                xrTableFK.Text = "Form Kodu: " + data.FormKodu;
                cellOgrenciAdSoyad.Text = data.AdSoyad;
                cellOgrenciEnstituAdi.Text = data.EnstituAd;
                cellOgrenciAnabilimDaliAdi.Text = data.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = data.ProgramAdi;
                cellOgrenciKayitDonemi.Text = data.OgrenciKayitDonemi;
                cellTezDili.Text = data.TezDiliText;
                cellTezBasligiTr.Text = data.TezBaslikTr;
                cellTezBasligiEn.Text = data.TezBaslikEn;

                rwTaahhut.Visible = !data.IsTezDiliTr;


                var oncekibasvuru = db.TijBasvuruOneris.Where(p => p.TijBasvuruOneriID != id &&
                    p.TijBasvuru.KullaniciID == data.KullaniciID &&
                    (p.IsObsData || p.EYKDaOnaylandi == true)).OrderByDescending(o => o.TijBasvuruOneriID).First();
                var oncekiJuriler = data.Juriler.Where(p => !p.IsYeniOrOnceki).ToList();


                var oncekiTd = db.Kullanicilars.First(f => f.KullaniciID == oncekibasvuru.TezDanismanID);
                var varolanTikler = oncekiJuriler.Where(f => !f.IsTezDanismani).ToList();

                cellMevcutUyeTdUnvanAdSoyad.Text = oncekiTd.Unvanlar.UnvanAdi + " " + oncekiTd.Ad+" "+oncekiTd.Soyad;
                cellVarolanTik1TrCapt.Text = varolanTikler[0].IsYtuIciJuri ? "YTÜ TİK Üyesi" : "YTU DIŞI TİK Üyesi ";
                cellVarolanTik1EnCapt.Text = varolanTikler[0].IsYtuIciJuri ? "YTU TMC Member" : "Non-YTU TMC Member";

                cellVarolanTik2TrCapt.Text = varolanTikler[1].IsYtuIciJuri ? "YTÜ TİK Üyesi" : "YTU DIŞI TİK Üyesi ";
                cellVarolanTik2EnCapt.Text = varolanTikler[1].IsYtuIciJuri ? "YTU TMC Member" : "Non-YTU TMC Member";

                cellVarolanTik1UnvanAdSoyad.Text = varolanTikler[0].UnvanAdi + " " + varolanTikler[0].AdSoyad;
                cellVarolanTik2UnvanAdSoyad.Text = varolanTikler[1].UnvanAdi + " " + varolanTikler[1].AdSoyad;


                var yeniJuriler = data.Juriler.Where(p => p.IsYeniOrOnceki).ToList();
                if (data.TijDegisiklikTipID == TijDegisiklikTipi.YtuIciDegisiklik ||
                    data.TijDegisiklikTipID == TijDegisiklikTipi.YtuIciVeDisiDegisiklik)
                {
                    ChkMevcutUyeYtuIciEvet.Checked = true;
                    var ytuIci1 = yeniJuriler.FirstOrDefault(f => f.IsYtuIciJuri && f.RowNum == 1);
                    if (ytuIci1 != null)
                    {
                        cellYtuIciJuri1Unvan.Text = ytuIci1.UnvanAdi;
                        cellYtuIciJuri1AdSoyad.Text = ytuIci1.AdSoyad;
                        cellYtuIciJuri1Universite.Text = ytuIci1.UniversiteAdi;
                        cellYtuIciJuri1AnabilimDali.Text = ytuIci1.AnabilimdaliAdi;
                    }

                    var ytuIci2 = data.Juriler.FirstOrDefault(f => f.IsYtuIciJuri && f.RowNum == 2);
                    if (ytuIci2 != null)
                    {
                        cellYtuIciJuri2Unvan.Text = ytuIci2.UnvanAdi;
                        cellYtuIciJuri2AdSoyad.Text = ytuIci2.AdSoyad;
                        cellYtuIciJuri2Universite.Text = ytuIci2.UniversiteAdi;
                        cellYtuIciJuri2AnabilimDali.Text = ytuIci2.AnabilimdaliAdi;
                    }

                    var ytuIci3 = data.Juriler.FirstOrDefault(f => f.IsYtuIciJuri && f.RowNum == 3);
                    if (ytuIci3 != null)
                    {
                        cellYtuIciJuri3Unvan.Text = ytuIci3.UnvanAdi;
                        cellYtuIciJuri3AdSoyad.Text = ytuIci3.AdSoyad;
                        cellYtuIciJuri3Universite.Text = ytuIci3.UniversiteAdi;
                        cellYtuIciJuri3AnabilimDali.Text = ytuIci3.AnabilimdaliAdi;
                    }

                }
                else ChkMevcutUyeYtuIciHayir.Checked = true;

                if (data.TijDegisiklikTipID == TijDegisiklikTipi.YtuDisiDegisiklik ||
                    data.TijDegisiklikTipID == TijDegisiklikTipi.YtuIciVeDisiDegisiklik)
                {
                    ChkMevcutUyeYtuDisiEvet.Checked = true;
                    var ytuDisi1 = data.Juriler.FirstOrDefault(f => !f.IsYtuIciJuri && f.RowNum == 4);
                    if (ytuDisi1 != null)
                    {
                        cellYtuDisiJuri1Unvan.Text = ytuDisi1.UnvanAdi;
                        cellYtuDisiJuri1AdSoyad.Text = ytuDisi1.AdSoyad;
                        cellYtuDisiJuri1Universite.Text = ytuDisi1.UniversiteAdi;
                        cellYtuDisiJuri1AnabilimDali.Text = ytuDisi1.AnabilimdaliAdi;
                    }


                    var ytuDisi2 = data.Juriler.FirstOrDefault(f => !f.IsYtuIciJuri && f.RowNum == 5);
                    if (ytuDisi2 != null)
                    {
                        cellYtuDisiJuri2Unvan.Text = ytuDisi2.UnvanAdi;
                        cellYtuDisiJuri2AdSoyad.Text = ytuDisi2.AdSoyad;
                        cellYtuDisiJuri2Universite.Text = ytuDisi2.UniversiteAdi;
                        cellYtuDisiJuri2AnabilimDali.Text = ytuDisi2.AnabilimdaliAdi;

                    }

                    var ytuDisi3 = data.Juriler.FirstOrDefault(f => !f.IsYtuIciJuri && f.RowNum == 6);
                    if (ytuDisi3 != null)
                    {
                        cellYtuDisiJuri3Unvan.Text = ytuDisi3.UnvanAdi;
                        cellYtuDisiJuri3AdSoyad.Text = ytuDisi3.AdSoyad;
                        cellYtuDisiJuri3Universite.Text = ytuDisi3.UniversiteAdi;
                        cellYtuDisiJuri3AnabilimDali.Text = ytuDisi3.AnabilimdaliAdi;
                    }
                }
                else ChkMevcutUyeYtuDisiHayir.Checked = true;





                chkGerekceDiger.Checked = data.TijFormTipID == TijFormTipi.Diger;
                chkGerekceTezDanismanDegisikligi.Checked = data.TijFormTipID == TijFormTipi.DanismanDegisikligi;
                chkGerekceTezKonuDegisikligi.Checked = data.TijFormTipID == TijFormTipi.TezKonusuDegisikligi;
                chkGerekceDanismanTezKonuDegisikligi.Checked = data.TijFormTipID == TijFormTipi.DanismanVeTezKonusuDegisikligi;

                cellDanismanUnvanAdSoyad.Text = data.Danisman.Unvanlar.UnvanAdi + "\r\n" + data.Danisman.Ad + " " + data.Danisman.Soyad;
                cellDanismanImza.Text = data.DanismanOnayTarihi.ToFormatDate() + " " + "Tarihinde Danışman tarafından elektronik olarak onaylanmıştır";
                cellFormKodu.Text = "Form Kodu: " + data.FormKodu;
                xrQRCode.ImageUrl = data.urlAdd;
                xrQRCode.Image = data.urlAdd.CreateQrCode();

            }
        }

    }
}
