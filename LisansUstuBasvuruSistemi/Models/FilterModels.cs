using BiskaUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Utilities.Dtos.CmbDtos;
using LisansUstuBasvuruSistemi.Ws_ObsService;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Models.FilterModel
{
    
    public class CheckObjectX<T> where T : class
    {
        public bool? Checked { get; set; }
        public bool Disabled { get; set; }
        public T Value { get; set; }
    }
    public class URoles
    {

        public int? YetkiGrupID { get; set; }
        public string YetkiGrupAdi { get; set; }
        public List<Roller> EklenenRoller { get; set; }
        public List<Roller> YetkiGrupRolleri { get; set; }
        public List<Roller> TumRoller { get; set; }
        public URoles()
        {
            EklenenRoller = new List<Roller>();
            YetkiGrupRolleri = new List<Roller>();
            TumRoller = new List<Roller>();
        }
    }
    public class ChkListModel
    {
        public string PanelTitle { get; set; }
        public string TableID { get; set; }
        public string InputName { get; set; }
        public IEnumerable<CheckObject<ChkListDataModel>> Data { get; set; }
        public bool AllDataChecked
        {
            get
            {

                return Data.Any() && Data.Select(s => s.Value).Count() == Data.Where(p => p.Checked == true).Select(s => s.Value).Count();

            }
        }
        public ChkListModel(string InputName = "")
        {
            this.InputName = InputName;
            var ID = Guid.NewGuid().ToString().Substr(0, 4);
            TableID = ID;
        }
    }
    public class ChkListDataModel
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string Caption { get; set; }
        public string Detail { get; set; }
    }


    public class fmKullanicilar : PagerOption
    {
        public bool Expand { get; set; }
        public int KullaniciID { get; set; }
        public int? YetkiGrupID { get; set; }
        public string EnstituKod { get; set; }
        public int? OgrenimTipKod { get; set; }
        public int? SehirKod { get; set; }
        public string ProgramKod { get; set; }
        public int? OgrenimDurumID { get; set; }
        public int? CinsiyetID { get; set; }
        public int? BirimID { get; set; }
        public int? KullaniciTipID { get; set; }
        public int? Cinsiyet { get; set; }
        public string KullaniciAdi { get; set; }
        public string AdSoyad { get; set; }
        public string EMail { get; set; }
        public string Telefon { get; set; }
        public bool? IsActiveDirectoryUser { get; set; }
        public bool? IsAktif { get; set; }
        public bool? IsAdmin { get; set; }
        public string Aciklama { get; set; }
        public IEnumerable<frKullanicilar> data { get; set; }
        public fmKullanicilar()
        {
            data = new frKullanicilar[0];
        }
    }
    public class frKullanicilar : Kullanicilar
    {
        public bool KtipBasvuruYapabilir { get; set; }
        public string EnstituAdi { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string YetkiGrupAdi { get; set; }
    }
    public class fmYetkiGruplari : PagerOption
    {
        public int YetkiGrupID { get; set; }
        public string YetkiGrupAdi { get; set; }
        public IEnumerable<frYetkiGruplari> Data { get; set; }
    }
    public class frYetkiGruplari
    {
        public int YetkiGrupID { get; set; }
        public string YetkiGrupAdi { get; set; }
        public int YetkiSayisi { get; set; }
        public int FbeYetkiliSayisi { get; set; }
        public int SbeYetkiliSayisi { get; set; }
    }
    public class fmUnvanlar : PagerOption
    {
        public int? UnvanSiraNo { get; set; }
        public string UnvanAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<Unvanlar> data { get; set; }

    }
    public class fmBirimler : PagerOption
    {
        public string BirimKod { get; set; }
        public string BirimAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<Birimler> data { get; set; }

    }
    public class fmSistemBilgilendirme : PagerOption
    {
        public Nullable<byte> BilgiTipi { get; set; }
        public string Kategori { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public Nullable<System.DateTime> IslemZamani { get; set; }
        public string IpAdresi { get; set; }
        public string AdSoyad { get; set; }

        public IEnumerable<frSistemBilgilendirme> data { get; set; }

    }
    public class frSistemBilgilendirme
    {
        public int SistemBilgiID { get; set; }
        public Nullable<byte> BilgiTipi { get; set; }
        public string Kategori { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public Nullable<System.DateTime> IslemZamani { get; set; }
        public int? IslemYapanID { get; set; }
        public string IpAdresi { get; set; }
        public string AdSoyad { get; set; }
        public string KullaniciAdi { get; set; }
    }
    public class fmDuyurular : PagerOption
    {
        public string EnstituKod { get; set; }
        public string Baslik { get; set; }
        public DateTime? Tarih { get; set; }
        public string Aciklama { get; set; }
        public bool? IsAktif { get; set; }
        public string DuyuruYapan { get; set; }
        public IEnumerable<frDuyurular> Data { get; set; }
    }

    public class frDuyurular : Duyurular
    {
        public string EnstituAdi { get; set; }
        public string DuyuruYapan { get; set; }
        public int EkSayisi { get; set; }
        public List<DuyuruEkleri> Ekler { get; set; }
    }
    public class fmMailSablonlari : PagerOption
    {
        public string EnstituKod { get; set; }
        public int? MailSablonTipID { get; set; }
        public string SablonAdi { get; set; }
        public DateTime? Tarih { get; set; }
        public string Sablon { get; set; }
        public bool? IsAktif { get; set; }
        public string DuyuruYapan { get; set; }
        public IEnumerable<frMailSablonlari> Data { get; set; }
    }

    public class frMailSablonlari : MailSablonlari
    {
        public string EnstituAdi { get; set; }
        public string SablonTipAdi { get; set; }
        public string Parametreler { get; set; }
        public string IslemYapan { get; set; }
        public int EkSayisi { get; set; }
    }


    public class fmAnketler : PagerOption
    {
        public string EnstituKod { get; set; }
        public string AnketAdi { get; set; }
        public IEnumerable<frAnketler> Data { get; set; }
    }

    public class frAnketler : Anket
    {
        public string EnstituAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public int SoruSayisi { get; set; }
    }
    public class frAnketDetay : AnketSoru
    {
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public int SecenekSayisi { get; set; }
        public string Aciklama { get; set; }
        public string CevapHtml { get; set; }
        public List<frAnketSecenekDetay> frAnketSecenekDetay { get; set; }

        public List<AnketSeceneklerDetay> SecenekDetay { get; set; }
        public List<AnketTableDetay> TableDetay { get; set; }
        public frAnketDetay()
        {
            SecenekDetay = new List<AnketSeceneklerDetay>();
            TableDetay = new List<AnketTableDetay>();
            frAnketSecenekDetay = new List<frAnketSecenekDetay>();
        }
    }
    public class frAnketSecenekDetay : AnketSoruSecenek
    {
        public int Count { get; set; }

    }

    public class AnketSeceneklerDetay
    {
        public int SiraNo { get; set; }
        public string SecenekAdi { get; set; }
        public Dictionary<int, string> EkAciklama { get; set; }
        public int Count { get; set; }
    }
    public class AnketTableDetay
    {
        public string SiraNo { get; set; }
        public string TabloVeri1 { get; set; }
        public string TabloVeri2 { get; set; }
        public string TabloVeri3 { get; set; }
        public string TabloVeri4 { get; set; }
    }
    public class fmMesajKategorileri : PagerOption
    {
        public string EnstituKod { get; set; }
        public string KategoriAdi { get; set; }
        public string KategoriAciklamasi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frMesajKategorileri> Data { get; set; }
    }
    public class frMesajKategorileri : MesajKategorileri
    {
        public string EnstituAd { get; set; }
        public string IslemYapan { get; set; }
    }


    public class AnketTabloVeriGirisModel
    {
        public string TabloVeri1 { get; set; }
        public string TabloVeri2 { get; set; }
        public string TabloVeri3 { get; set; }
        public string TabloVeri4 { get; set; }
        public string TabloVeri5 { get; set; }
        public bool InsertTablerRow { get; set; }
    }
    public class AnketPostGroupModel
    {
        public int inx { get; set; }
        public int AnketID { get; set; }
        public int AnketSoruID { get; set; }
        public bool IsTabloVeriGirisi { get; set; }
        public int? IsTabloVeriMaxSatir { get; set; }
        public int SecenekCount { get; set; }
        public int? AnketSoruSecenekID { get; set; }
        public string AnketSoruSecenekAciklama { get; set; }
        public bool SoruCevabiYanlis { get; set; }
        public bool IsEkAciklamaGir { get; set; }

        public List<AnketTabloVeriGirisModel> TabloVerileri { get; set; }
        public AnketPostGroupModel()
        {
            TabloVerileri = new List<AnketTabloVeriGirisModel>();
        }
    }
    public class AnketCevapModel
    {
        public int? SecilenAnketSoruSecenekID { get; set; }
        public frAnketDetay SoruBilgi { get; set; }
        public List<frAnketSecenekDetay> SoruSecenek { get; set; }
        public SelectList SelectListSoruSecenek { get; set; }
    }
    public class kmAnketlerCevap
    {
        public int AnketTipID { get; set; }
        public string RowID { get; set; }
        public int BasvuruSurecID { get; set; }
        public int AnketID { get; set; }
        public string JsonStringData { get; set; }
        public List<AnketCevapModel> AnketCevapModel { get; set; }
        public List<int> AnketSoruID { get; set; }
        public List<int?> AnketSoruSecenekID { get; set; }
        public List<string> TabloVeri1 { get; set; }
        public List<string> TabloVeri2 { get; set; }
        public List<string> TabloVeri3 { get; set; }
        public List<string> TabloVeri4 { get; set; }
        public List<string> TabloVeri5 { get; set; }
        public List<string> AnketSoruSecenekAciklama { get; set; }
        public kmAnketlerCevap()
        {
            AnketCevapModel = new List<AnketCevapModel>();
            AnketSoruID = new List<int>();
            AnketSoruSecenekID = new List<int?>();
            TabloVeri1 = new List<string>();
            TabloVeri2 = new List<string>();
            TabloVeri3 = new List<string>();
            TabloVeri4 = new List<string>();
            TabloVeri5 = new List<string>();
            AnketSoruSecenekAciklama = new List<string>();
        }
    }



    public class fmMesajlar : PagerOption
    {
        public bool Expand { get; set; }
        public string EnstituKod { get; set; }
        public int? MesajKategoriID { get; set; }
        public string Konu { get; set; }
        public DateTime? Tarih { get; set; }
        public string Aciklama { get; set; }
        public bool? IsAktif { get; set; }
        public bool? IsDosyaEkDurum { get; set; }
        public string AdSoyad { get; set; }
        public int? MesajYili { get; set; }
        public IEnumerable<frMesajlar> Data { get; set; }
    }

    public class frMesajlar : Mesajlar
    {
        public int GrupNo { get; set; }
        public string GidenGelen { get; set; }
        public string EnstituAdi { get; set; }
        public string KategoriAdi { get; set; }
        public string ResimAdi { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string OgrenciNo { get; set; }
        public string KayitDonemAdi { get; set; }
        public DateTime? KayitTarihi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }

        public int EkSayisi { get; set; }
        public List<SubMessages> SubMesajList { get; set; }

    }
    public class SubMessages
    {
        public int KullaniciID { get; set; }
        public string EMail { get; set; }
        public DateTime Tarih { get; set; }
        public string ResimYolu { get; set; }
        public string AdSoyad { get; set; }
        public int MesajID { get; set; }
        public string Icerik { get; set; }
        public string IslemYapanIP { get; set; }
        public List<MesajEkleri> Ekler { get; set; }
        public List<GonderilenMailKullanicilar> Gonderilenler { get; set; }

    }
    public class fmBasvuruSureci : PagerOption
    {
        public string EnstituKod { get; set; }
        public IEnumerable<frBasvuruSureci> Data { get; set; }
    }
    public class frBasvuruSureci : BasvuruSurec
    {
        public bool Hesaplandi { get; set; }
        public string EnstituAdi { get; set; }
        public string Kota_BasvuruSurecKontrolTipAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public string DonemAdi { get; set; }
        public int OTCount { get; set; }
        public List<CmbIntDto> CmbOgrenimTipBilgi { get; set; }
    }



    public class bsUrecDetay : BasvuruSurec
    {
        public int SelectedTabIndex { get; set; }
        public bool IsDelete { get; set; }
        public string EnstituAdi { get; set; }
        public string Kota_BasvuruSurecKontrolTipAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public string DonemAdi { get; set; }
        public MIndexBilgi ToplamBasvuruBilgisi { get; set; }
        public List<mulakatSturModel> MulakatSTurModel { get; set; }
        public List<krOgrenimTip> OgrenimTipleriLst { get; set; }
        public List<frKotalar> ProgramKotaLst { get; set; }
        public List<krSinavTipleri> SinavTipleri { get; set; }
        public fmMulakatNotGiris MulakatBilgi { get; set; }
        public fmMulakatSonuc MulakatSonucu { get; set; }
        public List<frAnketDetay> AnketDetay { get; set; }
        public int ToplamOnaylananBasvuru { get; set; }
        public List<CmbIntDto> CmbOgrenimTipBilgi { get; set; }
        public bsUrecDetay()
        {
            CmbOgrenimTipBilgi = new List<CmbIntDto>();
            MulakatSonucu = new fmMulakatSonuc();
            MulakatBilgi = new fmMulakatNotGiris();
            AnketDetay = new List<frAnketDetay>();
        }
    }
    public class msUrecDetay : MezuniyetSureci
    {
        public int SelectedTabIndex { get; set; }
        public bool IsDelete { get; set; }
        public string EnstituAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public string DonemAdi { get; set; }
        public MIndexBilgi ToplamBasvuruBilgisi { get; set; }
        public msUrecDetay()
        {
        }
    }
    public class krSinavTipleriDonems : SinavTipleriDonem
    {
        public string DonemAdi { get; set; }
    }
    public class krSinavTipleri : BasvuruSurecSinavTipleri
    {
        public string EnstituAd { get; set; }
        public string SinavTipGrupAdi { get; set; }
        public string SinavAdi { get; set; }
        public string IslemYapan { get; set; }
        public List<krSinavTipleriDonems> SinavTipleriDonems { get; set; }
        public List<krSinavTipleriOTNotAraliklari> SinavTipleriOTNotAraliklariList { get; set; }
        public List<frBsSinavTipleriSPA> frSinavTipleriSPA { get; set; }
        public krSinavTipleri()
        {
            frSinavTipleriSPA = new List<frBsSinavTipleriSPA>();
        }
    }
    public class krProgramBilgi : BasvuruSurecKotalar
    {
        public string OgrenimTipAdi { get; set; }
        public string BolumKod { get; set; }
        public string BolumAdi { get; set; }
        public string ProgramAdi { get; set; }
        public List<string> BasvurabilecekOgrenciTipleri { get; set; }
        public krProgramBilgi()
        {
            BasvurabilecekOgrenciTipleri = new List<string>();
        }
    }
    public class fmBasvurular : PagerOption
    {
        public bool Expand { get; set; }
        public int? BelgeDetailBasvuruID { get; set; }
        public int? UyrukKod { get; set; }
        public string EnstituKod { get; set; }
        public int? BasvuruSurecID { get; set; }
        public int? KullaniciID { get; set; }
        public int? KullaniciTipID { get; set; }
        public string AdSoyad { get; set; }
        public int? ToplamBasvurulanProgram { get; set; }
        public int? BasvuruDurumID { get; set; }
        public int? MulakatSonucTipID { get; set; }
        public int? OgrenimTipKod { get; set; }
        public string ProgramKod { get; set; }
        public string AnabilimDaliKod { get; set; }
        public int? SinavTipKod { get; set; }
        public int? CinsiyetID { get; set; }
        public int? LOgrenimDurumID { get; set; }
        public bool? IsTaahhutVar { get; set; }

        public IEnumerable<frBasvurular> Data { get; set; }
    }
    public class frBasvurular : Basvurular
    {
        public string EnstituKod { get; set; }
        public string EnstituAdi { get; set; }
        public string BasvuruSurecAdi { get; set; }
        public string TcPasaPortNo { get; set; }
        public DateTime BasTar { get; set; }
        public DateTime BitTar { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string AdSoyad { get; set; }
        public int TercihSayisi { get; set; }
        public string BasvuruDurumAdi { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public bool IsNotDuzelt { get; set; }
        public bool KayitliTercihVar { get; set; }

    }

    public class fmMezuniyetBasvurulari : PagerOption
    {
        public int? SMezuniyetBID { get; set; }
        public int? STabID { get; set; }
        public bool IsSinavDegerlendirme { get; set; }
        public bool Expand { get; set; }
        public Guid? RowID { get; set; }
        public int? KullaniciID { get; set; }
        public int? UyrukKod { get; set; }
        public string EnstituKod { get; set; }
        public int? MezuniyetSurecID { get; set; }
        public int? MezuniyetSureci { get; set; }
        public string KayitDonemi { get; set; }
        public int? KullaniciTipID { get; set; }
        public int? JuriOneriFormuDurumuID { get; set; }
        public string AdSoyad { get; set; }
        public int? ToplamBasvurulanProgram { get; set; }
        public int? MezuniyetYayinKontrolDurumID { get; set; }
        public int? OgrenimTipKod { get; set; }
        public string ProgramKod { get; set; }
        public string AnabilimDaliKod { get; set; }
        public int? SRDurumID { get; set; }
        public int? MezuniyetSinavDurumID { get; set; }
        public int? TDDurumID { get; set; }
        public bool? TeslimFormDurumu { get; set; }
        public int? MezuniyetDurumID { get; set; }
        public DateTime? MBaslangicTarihi { get; set; }
        public DateTime? MBitisTarihi { get; set; }

        public IEnumerable<frMezuniyetBasvurulari> Data { get; set; }
    }
    public class frMezuniyetBasvurulari : MezuniyetBasvurulari
    {
        public string EnstituKod { get; set; }
        public string EnstituAdi { get; set; }
        public string MezuniyetSurecAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string TcPasaPortNo { get; set; }
        public int SurecBaslangicYil { get; set; }
        public int DonemID { get; set; }
        public DateTime BasTar { get; set; }
        public DateTime BitTar { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string AdSoyad { get; set; }
        public string EMail { get; set; }
        public string CepTel { get; set; }
        public DateTime? GsisKayitTarihi { get; set; }
        public string MezuniyetYayinKontrolDurumAdi { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public string MezuniyetSinavDurumAdi { get; set; }
        public string SDurumClassName { get; set; }
        public string SDurumColor { get; set; }
        public bool IsNotDuzelt { get; set; }
        public bool TeslimFormDurumu { get; set; }
        public int? SRDurumID { get; set; }
        public string IslemYapan { get; set; }
        public int UzatmaSuresiGun { get; set; }
        public int MezuniyetSuresiGun { get; set; }
        public SRTalepleri SrTalebi { get; set; }
        public int MyProperty { get; set; }
        public bool? IsOnaylandiOrDuzeltme { get; set; }
        public MezuniyetBasvurulariTezDosyalari MezuniyetBasvurulariTezDosyasi { get; set; }
        public string FormNo { get; set; }
        public MezuniyetJuriOneriFormlari MezuniyetJuriOneriFormu { get; set; }
        public List<int> MBYayinTurIDs { get; set; }

    }
    public class fmMailGonderme : PagerOption
    {
        public string EnstituKod { get; set; }
        public string Konu { get; set; }
        public DateTime? Tarih { get; set; }
        public string Aciklama { get; set; }
        public string MailGonderen { get; set; }
        public IEnumerable<frMailGonderme> Data { get; set; }
    }
    public class frMailGonderme : GonderilenMailler
    {
        public string EnstituAdi { get; set; }
        public string MailGonderen { get; set; }
        public int EkSayisi { get; set; }
        public int KisiSayisi { get; set; }

    }

    public class MailKullaniciBilgi
    {

        public bool Checked { get; set; }
        public int KullaniciID { get; set; }
        public string AdSoyad { get; set; }
        public string BirimAdi { get; set; }
        public string Email { get; set; }

    }
    public class KmMailGonder : GonderilenMailler
    {
        public int? BasvuruSurecID { get; set; }
        public bool IsBasvuruSonuc { get; set; }
        public string Alici { get; set; }
        public bool IsTopluMail { get; set; }
        public string SecilenTopluAlicilar { get; set; }
        public string BasvuruRowID { get; set; }
        public bool IsBolumOrOgrenci { get; set; }
        public bool IsToOrBCC { get; set; }
        public List<int> MulakatSonucTipIDs { get; set; }
        public List<int?> KayitDurumIDs { get; set; }
        public List<string> ProgramKods { get; set; }
        public List<int> OgrenimTipKods { get; set; }

        public List<string> SecilenAlicilars { get; set; }
        public List<CmbStringDto> EMails { get; set; }

        public KmMailGonder()
        {
            Aciklama = "";
            AciklamaHtml = "";
            SecilenTopluAlicilar = "";
            IsBolumOrOgrenci = false;
            ProgramKods = new List<string>();
            OgrenimTipKods = new List<int>();
            SecilenAlicilars = new List<string>();
            KayitDurumIDs = new List<int?>();
            EMails = new List<CmbStringDto>();
            MulakatSonucTipIDs = new List<int>();
        }
    }

    public class fmSehirler : PagerOption
    {
        public int? SehirKod { get; set; }
        public string Ad { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<Sehirler> data { get; set; }

    }
    public class fmUniversiteler : PagerOption
    {
        public int? UniversiteID { get; set; }
        public string KisaAd { get; set; }
        public string Ad { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<Universiteler> data { get; set; }
    }
    public class fmUyruklar : PagerOption
    {
        public int? UyrukKod { get; set; }
        public string KisaAd { get; set; }
        public string Ad { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<Uyruklar> data { get; set; }
    }
    public class fmEnstituler : PagerOption
    {
        public string EnstituKod { get; set; }
        public string EnstituAd { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frEnstituler> data { get; set; }

    }
    public class frEnstituler : Enstituler
    {
        public string IslemYapan { get; set; }
    }
    public class fmAnabilimDallari : PagerOption
    {
        public string EnstituKod { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frAnabilimDallari> data { get; set; }

    }
    public class frAnabilimDallari : AnabilimDallari
    {
        public string EnstituAd { get; set; }
        public string IslemYapan { get; set; }

    }
    public class fmAlesTipleri : PagerOption
    {
        public string AlesTipAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frAlesTipleri> data { get; set; }

    }
    public class frAlesTipleri : AlesTipleri
    {
        public string IslemYapan { get; set; }

    }
    public class fmOgrenciBolumleri : PagerOption
    {
        public string EnstituKod { get; set; }
        public int? OgrenciBolumID { get; set; }
        public string BolumAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frOgrenciBolumleri> data { get; set; }

    }
    public class frOgrenciBolumleri : OgrenciBolumleri
    {
        public string EnstituAd { get; set; }

        public string IslemYapan { get; set; }
    }
    public class fmProgramlar : PagerOption
    {
        public bool Expand { get; set; }
        public string EnstituKod { get; set; }
        public string ProgramKod { get; set; }
        public int? BolumID { get; set; }
        public int? AlesTipID { get; set; }
        public int? KullaniciTipID { get; set; }
        public bool? Ucretli { get; set; }
        public string ProgramAdi { get; set; }
        public bool? AlesNotuYuksekOlanAlinsin { get; set; }
        public bool? LYLHerhangiBirindeGecenAlanIci { get; set; }
        public bool? ProgramSecimiEkBilgi { get; set; }
        public bool? IsAlandisiBolumKisitlamasi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frProgramlar> data { get; set; }

    }
    public class frProgramlar : Programlar
    {
        public string EnstituKod { get; set; }
        public string EnstituAd { get; set; }
        public string AnabilimDaliAdi { get; set; }

        public string AgnoAlimTipAdi { get; set; }
        public string AlesTipAdi { get; set; }
        public string IslemYapan { get; set; }
        public List<string> AlandisiBolumKisitListesi { get; set; }
        public string KullaniciTipAdi { get; set; }
        public List<string> SecilenAlesTipleri { get; set; }

    }




    public class fmSinavTipleri : PagerOption
    {
        public string EnstituKod { get; set; }
        public string SinavAdi { get; set; }
        public int? SinavTipGrupID { get; set; }
        public bool? WebService { get; set; }
        public bool? OzelTarih { get; set; }
        public bool? OzelNot { get; set; }
        public bool? KusuratVar { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frSinavTipleri> data { get; set; }

    }
    public class frSinavTipleri : SinavTipleri
    {
        public int SelectedTabIndex { get; set; }
        public string EnstituAd { get; set; }
        public string SinavTipGrupAdi { get; set; }
        public string WsSinavCekimTipAdi { get; set; }
        public string IslemYapan { get; set; }

        public List<krSinavTipleriOTNotAraliklari> SinavTipleriOTNotAraliklariList { get; set; }
        public List<krSinavTipleriDonems> krSinavTipleriDonems { get; set; }

        public List<frSinavTipleriSPA> frSinavTipleriSPA { get; set; }

        public frSinavTipleri()
        {
            frSinavTipleriSPA = new List<frSinavTipleriSPA>();
        }
    }
    public class frSinavTipleriSPA : SinavTipleriOT_SNA
    {
        public List<krSinavTipleriOTNotAraliklari> SinavTipleriOTNotAraliklariList { get; set; }
        public List<krSinavTipleriDonems> krSinavTipleriDonems { get; set; }

    }
    public class frBsSinavTipleriSPA : BasvuruSurecSinavTipleriOT_SNA
    {
        public List<krSinavTipleriOTNotAraliklari> SinavTipleriOTNotAraliklariList { get; set; }
    }
    public class fmBolumEslestir : PagerOption
    {
        public string EnstituKod { get; set; }
        public string ProgramAdi { get; set; }
        public IEnumerable<frBolumEslestir> data { get; set; }

    }
    public class frBolumEslestir : frProgramlar
    {
        public List<string> OgrenciBolumAdlari { get; set; }
    }
    public class fmKotalar : PagerOption
    {
        public bool? MulakatSurecineGirecek { get; set; }
        public bool? IsAlesYerineDosyaNotuIstensin { get; set; }
        public string EnstituKod { get; set; }
        public string ProgramAdi { get; set; }
        public int? OgrenimTipKod { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frKotalar> data { get; set; }

    }
    public class frKotalar : frProgramlar
    {
        public int KotaID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public bool MulakatSurecineGirecek { get; set; }
        public bool OrtakKota { get; set; }
        public int? OrtakKotaSayisi { get; set; }
        public int AlanIciKota { get; set; }
        public int AlanDisiKota { get; set; }
        public double? MinAles { get; set; }
        public double? MinAgno { get; set; }
        public object UnAdi { get; internal set; }
        public bool IsAlesYerineDosyaNotuIstensin { get; set; }

        public int AlanTipID { get; set; }
        public bool? KayitOldu { get; set; }
        public int MulakatSonucTipID { get; set; }
    }
    public class FmOgrenimTipleri : PagerOption
    {
        public string EnstituKod { get; set; }
        public int? OgrenimTipKod { get; set; }
        public string OgrenimTipAd { get; set; }
        public string GrupAd { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrOgrenimTipleri> data { get; set; }
    }
    public class FrOgrenimTipleri : OgrenimTipleri
    {
        public string EnstituAd { get; set; }
        public string IslemYapan { get; set; }
        public List<string> BasvurulabilecekDigerOgrenimTipleri { get; set; }

    }
    public class FmYabanciDilSinavTip : PagerOption
    {
        public int? YabanciDilSinavTipKod { get; set; }
        public string Ad { get; set; }
        public string AdEng { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrYabanciDilSinavTip> data { get; set; }

    }
    public class FrYabanciDilSinavTip : SinavDilleri
    {
        public string IslemYapan { get; set; }

    }






    public class kmKotalar : Kotalar
    {
        public int? AlanIciKota { get; set; }
        public int? AlanDisiKota { get; set; }
        public double? MinAles { get; set; }
        public double? MinAGNO { get; set; }
    }
    public class kmBolumEslestir
    {
        public string EnstituKod { get; set; }
        public string EnstituAd { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public List<int> OgrenciBolumID { get; set; }

    }
    public class KmSinavTipleri : SinavTipleri
    {
        public List<int> SinavDilIDs { get; set; }

        public List<int> SinavTarihleriID { get; set; }
        public List<DateTime> SinavTarihi { get; set; }

        public List<int> SinavNotlariID { get; set; }
        public List<string> SinavNotAdi { get; set; }
        public List<double> SinavNotDeger { get; set; }


        public List<int> SubSinavAralikID { get; set; }
        public List<string> SubSinavAralikAdi { get; set; }
        public List<double> SubSinavMin { get; set; }
        public List<double> SubSinavMax { get; set; }
        public List<bool> SubNotDonusum { get; set; }
        public List<string> SubNotDonusumFormulu { get; set; }

        public List<int> SinavTipDonemID { get; set; }
        public List<int> Yil { get; set; }
        public List<string> WsDonemKod { get; set; }
        public List<bool> IsTaahhutVar { get; set; }

        public List<int> NAOgrenimTipKod { get; set; }
        public List<bool> NAIngilizce { get; set; }
        public List<int> NAIsGecerli { get; set; }
        public List<int> NAIsIstensin { get; set; }
        public List<double?> NAMin { get; set; }
        public List<double?> NAMax { get; set; }
        public List<string> IPProgramKod { get; set; }

        public KmSinavTipleri()
        {
            IPProgramKod = new List<string>();
            SinavDilIDs = new List<int>();
            IsTaahhutVar = new List<bool>();


            SinavTipDonemID = new List<int>();
            Yil = new List<int>();
            WsDonemKod = new List<string>();

            SinavTarihleriID = new List<int>();
            SinavTarihi = new List<DateTime>();
            SinavNotlariID = new List<int>();
            SinavNotAdi = new List<string>();
            SinavNotDeger = new List<double>();

            SubSinavAralikID = new List<int>();
            SubSinavAralikAdi = new List<string>();
            SubSinavMin = new List<double>();
            SubSinavMax = new List<double>();
            SubNotDonusum = new List<bool>();
            SubNotDonusumFormulu = new List<string>();
        }
    }
    public class kmSinavTipleriSPNA : SinavTipleriOT_SNA
    {

        public List<int> NAOgrenimTipKod { get; set; }
        public List<bool> NAIngilizce { get; set; }
        public List<int> NAIsGecerli { get; set; }
        public List<int> NAIsIstensin { get; set; }
        public List<double?> NAMin { get; set; }
        public List<double?> NAMax { get; set; }
        public List<string> IPProgramKod { get; set; }

        public List<krSinavTipleriOTNotAraliklari> SinavTipleriOTNotAraliklari { get; set; }
        public kmSinavTipleriSPNA()
        {
            IPProgramKod = new List<string>();
            SinavTipleriOTNotAraliklari = new List<krSinavTipleriOTNotAraliklari>();

        }
    }
    public class krSinavTipleriOTNotAraliklari : SinavTipleriOTNotAraliklari
    {
        public string OgrenimTipAdi { get; set; }
        public bool? SuccessRow { get; set; }
        public List<string> PropName { get; set; }
        public List<string> ProgramKods { get; set; }
        public List<CmbStringDto> IstenmeyenProgramlar { get; set; }
        public krSinavTipleriOTNotAraliklari()
        {
            PropName = new List<string>();
            ProgramKods = new List<string>();
            IstenmeyenProgramlar = new List<CmbStringDto>();
        }
    }


    public class CevrilenNotModel
    {
        public double Not1Lik { get; set; }
        public double Not4Luk { get; set; }
        public double Not5Lik { get; set; }
        public double Not100Luk { get; set; }
    }

    public class kmBasvuru : Basvurular
    {
        public bool Onaylandi { get; set; }
        public bool sbmtForm { get; set; }
        public int StepNo { get; set; }
        public int? SetSelectedStep { get; set; }
        public int TercihSayisi { get; set; }
        public bool ODurumIstensin { get; set; }
        public bool KotaValid { get; set; }
        public bool AlesIstensinmi { get; set; }
        public bool LEgitimDiliIstensinMi { get; set; }
        public bool YLEgitimDiliIstensinMi { get; set; }
        public bool TomerIstensinmi { get; set; }
        public bool DilIstensinmi { get; set; }
        public bool IsYerli { get; set; }
        public bool YLDurum { get; set; }
        public int OgrenimTipKod { get; set; }
        public string ProgramKod { get; set; }
        public string EnstituKod { get; set; }
        public string KullaniciTipAdi { get; set; }
        public List<BasvurularSinavBilgi> BasvurularSinavBilgi { get; set; }
        public BasvurularSinavBilgi BasvurularSinavBilgi_A { get; set; }
        public BasvurularSinavBilgi BasvurularSinavBilgi_D { get; set; }
        public BasvurularSinavBilgi BasvurularSinavBilgi_T { get; set; }
        public List<string> _UniqueID { get; set; }
        public List<int> _tSiraNo { get; set; }
        public List<int> _OgrenimTipKod { get; set; }
        public List<string> _ProgramKod { get; set; }
        public List<bool> _Ingilizce { get; set; }
        public List<bool> _YLBilgiIste { get; set; }
        public List<int> _AlanTipID { get; set; }

        public string DonemAdi { get; set; }

        public List<basvuruTercihModel> BasvuruTercihleri { get; set; }
        public kmBasvuru()
        {
            _UniqueID = new List<string>();
            _tSiraNo = new List<int>();
            _OgrenimTipKod = new List<int>();
            _ProgramKod = new List<string>();
            _Ingilizce = new List<bool>();
            _YLBilgiIste = new List<bool>();
            _AlanTipID = new List<int>();
            BasvuruTercihleri = new List<basvuruTercihModel>();
            BasvurularSinavBilgi = new List<BasvurularSinavBilgi>();
        }
    }
    public class tercihSTKontrolModel
    {

        public int SinavTipGrupID { get; set; }
        public int BasvuruSurecID { get; set; }
        public int SinavTipID { get; set; }
        public List<int> OgrenimTipKods { get; set; }
        public List<string> ProgramKods { get; set; }
        public List<bool> Ingilizces { get; set; }

        public tercihSTKontrolModel()
        {
            OgrenimTipKods = new List<int>();
            ProgramKods = new List<string>();
            Ingilizces = new List<bool>();
        }

    }
    public class basvuruTercihModel : BasvurularTercihleri
    {
        public string AlesTipAdi { get; set; }
        public string AlanTipAdi { get; set; }
        public bool Ingilizce { get; set; }
        public bool YlBilgiIste { get; set; }
        public bool IsLagnoOrYlAgnoAlinsin { get; set; }
        public bool? IsAsilOrYedek { get; set; }
        public kontenjanProgramBilgiModel ProgramBilgileri { get; set; }
    }
    public class basvuruDetayModel : Basvurular
    {

        public int BasvuruSurecTipID { get; set; }
        public bool IsSave { get; set; }
        public bool IsYerli { get; set; }
        public string EnstituAdi { get; set; }
        public int SelectedTabIndex { get; set; }
        public string LUniversiteAdi { get; set; }
        public string LBolumAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string LNotSistemi { get; set; }
        public string LegitimDilAdi { get; set; }
        public string YLUniversiteAdi { get; set; }
        public string YLBolumAdi { get; set; }
        public string YLNotSistemi { get; set; }
        public string YLegitimDilAdi { get; set; }
        public string DRUniversiteAdi { get; set; }
        public string DRBolumAdi { get; set; }
        public string DRNotSistemi { get; set; }
        public string DRegitimDilAdi { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string Cinsiyet { get; set; }
        public string UyrukAdi { get; set; }
        public string DogumYeriAdi { get; set; }
        public string YasadigiSehirAdi { get; set; }
        public string NufusIlIcleAdi { get; set; }
        public bool IsLAgnoOrYLAgnoAlinsin { get; set; }

        public sinavBilgiGrupModel BasvurularSinavBilgi_A { get; set; }
        public sinavBilgiGrupModel BasvurularSinavBilgi_D { get; set; }
        public sinavBilgiGrupModel BasvurularSinavBilgi_T { get; set; }
        public List<basvuruTercihModel> BasvuruTercihleri { get; set; }

        public string BasvuruDurumAdi { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }


        public basvuruDetayModel()
        {
            BasvurularSinavBilgi_A = new sinavBilgiGrupModel();
            BasvurularSinavBilgi_D = new sinavBilgiGrupModel();
            BasvurularSinavBilgi_T = new sinavBilgiGrupModel();
            BasvuruTercihleri = new List<basvuruTercihModel>();

        }
    }

    public class BasvuruBelgeModel : BasvurularYuklenenBelgeler
    {
        public int SiraNo { get; set; }
        public string BasvuruBelgeTipAdi { get; set; }
        public int SinavTipKod { get; set; }
        public string SinavTipAdi { get; set; }
        public bool IsKayitSonrasiGetirilecek { get; set; }
        public string Not { get; set; }
    }
    public class sinavBilgiGrupModel
    {
        public string DilKodu { get; set; }
        public string DurumColor { get; set; }
        public DateTime BasvuruTarihi { get; set; }
        public bool IsTurkceProgramVar { get; set; }
        public BasvurularSinavBilgi Sinav { get; set; }
        public List<int> SecilenAlesTipleri { get; set; }
        public SinavBilgiModel SinavDetay { get; set; }
    }
    public class BasvuruDetayModelTDO : TDOBasvuru
    {
        public bool GelenBasvuru { get; set; }
        public string EnstituAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public string BasvuruKayitSureciTarihi { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string TezDanismanBilgiEslesen { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public Guid? DegerlendirenUniqueID { get; set; }
        public bool TdoBasvurusuYapabilir { get; set; }
        public bool IsYeniDanismanOneriOrDegisiklik { get; set; }
        public List<TDOBasvuruDanismanModel> TDOBasvuruDanismanList { get; set; }
    }
    public class TDOBasvuruDanismanModel : TDOBasvuruDanisman
    {
        public string DonemAdi { get; set; }
        public string AdSoyad { get; set; }
        public bool IsDuzeltSilYapabilir { get; set; }
        public string VarolanDanismanAd { get; set; }
        public string TalepTipAdi { get; set; }
        public bool VarolanDanismanGozuksun { get; set; }
        public bool VarolanDanismanOnayIslemiAcik { get; set; } 
        public bool YeniDanismanOnayIslemiAcik { get; set; }
        public bool IsYeniTezBasligiGozuksun { get; set; }
        public bool TdoEsBasvurusuYapabilir { get; set; }
        public bool IsYeniEsDanismanOneriOrDegisiklik { get; set; }
        public TDOBasvuruEsDanisman EsDanismanBilgi { get; set; }
    }
    public class BasvuruDetayModelTI : TIBasvuru
    {
        public bool GelenBasvuru { get; set; }
        public string EnstituAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public string BasvuruKayitSureciTarihi { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string TezDanismanBilgiEslesen { get; set; }

        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public Guid? DegerlendirenUniqueID { get; set; }
        public List<TIBasvuruAraRaporModel> TIBasvuruAraRaporList { get; set; }
    }
    public class TIBasvuruAraRaporModel : TIBasvuruAraRapor
    {
        public string DonemAdi { get; set; }
        public string TIBasvuruAraaRaporDurumAdi { get; set; }
        public frTalepler SRModel { get; set; }
    }
    public class basvuruDetayModelMezuniyet : MezuniyetBasvurulari
    {
        public IHtmlString BasvuruDurumHtml { get; set; }
        public bool IsDelete { get; set; }
        public bool GelenBasvuru { get; set; }
        public bool sonucGirisSureciAktif { get; set; }
        public bool IsYerli { get; set; }
        public string EnstituAdi { get; set; }
        public int SelectedTabIndex { get; set; }

        public string OgrenimDurumAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public string BasvuruSureciTarihi { get; set; }
        public string BasvuruKayitSureciTarihi { get; set; }
        public string KullaniciTipAdi { get; set; }

        public List<YayinBilgiModel> YayinBilgileri { get; set; }

        public string MezuniyetYayinKontrolDurumAdi { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public bool IsAnketVar { get; set; }
        public bool IsAnketDolduruldu { get; set; }
        public string AnketView { get; set; }
        public bool? EYKYaGonderildi { get; set; }
        public bool? EYKDaOnaylandi { get; set; }

        public string TezDanismanBilgiEslesen { get; set; }
        public YayinBilgiModel SelectedYayin { get; set; }


        public MezuniyetSRModel MezuniyetSRModel { get; set; }

        public SrDurumSelectList MezuniyetDurumSelectList = new SrDurumSelectList();

        public SelectList SMezuniyetYayinKontrolDurum { get; set; }
        public SelectList SEYKYaGonderildi { get; set; }
        public SelectList SEYKDaOnaylandi { get; set; }
        public SelectList SIsAsilOryedek { get; set; }



        public basvuruDetayModelMezuniyet()
        {
            YayinBilgileri = new List<YayinBilgiModel>();
            MezuniyetSRModel = new MezuniyetSRModel();


        }
    }

    public class MezuniyetSRModel
    {
        public bool IsSrEykSureAsimi { get; set; }
        public DateTime? EykIlkSrMaxTarih { get; set; }
        public List<frTalepler> SalonRezervasyonlari { get; set; }
    }
    public class mulakatSturModel : MulakatSinavTurleri
    {
        public bool? Success { get; set; }
        public int IndexNo { get; set; }
        public string SinavTurAdi { get; set; }
        public int? YuzdeOran { get; set; }
        public bool Zorunlu { get; set; }
    }
    public class kmBasvuruSurec : BasvuruSurec
    {
        public string OgretimYili { get; set; }

        public List<int> gID { get; set; }
        public List<string> BasvuruSurecOtoMailID { get; set; }
        public List<string> ZamanTipID { get; set; }
        public List<string> Zaman { get; set; }

        public List<int> OgrenimTipKod { get; set; }
        public List<int> OgrenimTipKods { get; set; }
        public List<int> Kota { get; set; }
        public List<double> BasariNotOrtalamasi { get; set; }
        public List<string> SeciliOgrenimTipKod { get; set; }
        public List<int> BasvuruSurecOgrenimTipID { get; set; }
        public List<string> MulakatSurecineGirecek { get; set; }
        public List<string> AlanIciBilimselHazirlik { get; set; }
        public List<string> AlanDisiBilimselHazirlik { get; set; }


        public List<int> MulakatSinavTurID { get; set; }
        public List<int> MulakatSinavTurIDSecilen { get; set; }
        public List<int?> YuzdeOran { get; set; }
        public List<mulakatSturModel> MulakatSTurModel { get; set; }

        public kmBasvuruSurecOgrenimTipModel OgrenimTipModel { get; set; }

        public List<DateTime?> AsilBasTar { get; set; }
        public List<DateTime?> AsilBitTar { get; set; }
        public List<DateTime?> YedekBasTar { get; set; }
        public List<DateTime?> YedekBitTar { get; set; }


        public kmBasvuruSurec()
        {
            gID = new List<int>();
            BasvuruSurecOtoMailID = new List<string>();
            ZamanTipID = new List<string>();
            Zaman = new List<string>();
            OgrenimTipKod = new List<int>();
            OgrenimTipKods = new List<int>();
            Kota = new List<int>();
            BasariNotOrtalamasi = new List<double>();
            BasvuruSurecOgrenimTipID = new List<int>();
            MulakatSurecineGirecek = new List<string>();
            AlanIciBilimselHazirlik = new List<string>();
            AlanDisiBilimselHazirlik = new List<string>();
            MulakatSinavTurID = new List<int>();
            MulakatSinavTurIDSecilen = new List<int>();
            YuzdeOran = new List<int?>();


            OgrenimTipModel = new kmBasvuruSurecOgrenimTipModel();

            AsilBasTar = new List<DateTime?>();
            AsilBitTar = new List<DateTime?>();
            YedekBasTar = new List<DateTime?>();
            YedekBitTar = new List<DateTime?>();
        }
    }
    public class kmBasvuruSurecOgrenimTipModel
    {
        public bool IsBelgeYuklemeVar { get; set; }
        public string EnstituKod { get; set; }
        public int BasvuruSurecID { get; set; }
        public List<CmbIntDto> EnstituOgrenimTipleri = new List<CmbIntDto>();
        public IEnumerable<CheckObject<krOgrenimTip>> OgrenimTipleriDataList { get; set; }
    }
    public class kmBsOtoMail : BasvuruSurecOtoMail
    {
        public int gID { get; set; }
        public bool Checked { get; set; }
        public string ZamanTipAdi { get; set; }

    }


    public class kmMzOtoMail : MezuniyetSurecOtoMail
    {
        public int gID { get; set; }
        public bool Checked { get; set; }
        public string ZamanTipAdi { get; set; }
        public string Aciklama { get; set; }

    }

    public class krOgrenimTip : BasvuruSurecOgrenimTipleri
    {
        public string EnstituKod { get; set; }
        public bool? Success { get; set; }
        public bool OrjinalVeri { get; set; }
        public bool OTipiniAyir { get; set; }
        public string GrupAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public List<int> SecilenBSOTIDs { get; set; }

        public krOgrenimTip()
        {
            SecilenBSOTIDs = new List<int>();

        }
    }




    public class kotaKontrolModel
    {
        public string RowClass { get; set; }
        public int AlanTipID { get; set; }
        public bool AlanIci { get; set; }
        public string AlanTipAdi { get; set; }
        public string AlanTipKisaAdi { get; set; }
        public string ProgramKod { get; set; }
        public int Kota { get; set; }
        public int AlesTipID { get; set; }
        public string AlesTipAdi { get; set; }
        public string AlesTipKisaAdi { get; set; }
        public bool AyniProgramBasvurusu { get; set; }
        public bool AlanDisiProgramKisitlamasiVar { get; set; }
        public string AlanDisiProgramKisitlamasiMsg { get; set; }
        public List<string> AlertInputNames { get; set; }
    }

    public class basvuruDurumModel : BasvuruDurumlari
    {
        public string DurumAdi { get; set; }
    }

    public class mezuniyetYayinKontrolDurumModel : MezuniyetYayinKontrolDurumlari
    {
        public string DurumAdi { get; set; }
    }
    public class fmBelgeTalepleri : PagerOption
    {
        public bool Expand { get; set; }
        public string DilKodu { get; set; }
        public int? OgrenimDurumID { get; set; }
        public int? BelgeID { get; set; }
        public int? OgrenimTipKod { get; set; }
        public int? BelgeDurumID { get; set; }
        public int? BelgeTipID { get; set; }
        public string OgretimYili { get; set; }
        public string AranacakKelime { get; set; }
        public string ProgramKod { get; set; }
        public string BuGunkuKayitlar { get; set; }
        public IEnumerable<frBelgeTalepleri> Data { get; set; }
    }
    public class frBelgeTalepleri : BelgeTalepleri
    {
        public int? KullaniciID { get; set; }
        public string ResimAdi { get; set; }
        public string ClassName { get; set; }
        public string Color { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string DonemAdi { get; set; }
        public string DurumAdi { get; set; }
        public string DurumListeAdi { get; set; }
        public string BelgeTipAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
    }


    public class fmTalep : PagerOption
    {
        public bool Expand { get; set; }
        public int? TalepSurecID { get; set; }
        public int? KullaniciTipID { get; set; }
        public int? TalepTipID { get; set; }
        public int? TalepDurumID { get; set; }
        public int? OgrenimTipKod { get; set; }
        public string AranacakKelime { get; set; }
        public string ProgramKod { get; set; }
        public bool? IsTezOnerisiYapildi { get; set; }
        public bool? IsDersYukuTamamlandi { get; set; }
        public IEnumerable<frTalep> Data { get; set; }
    }
    public class frTalep : TalepGelenTalepler
    {
        public bool YtuOgrencisi { get; set; }
        public bool IsbelgeYuklemesiVar { get; set; }
        public string TalepTipAdi { get; set; }
        public string ResimAdi { get; set; }
        public string ClassName { get; set; }
        public string Color { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabiliDaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string StatuAdi { get; set; }
        public string DurumListeAdi { get; set; }
        public string TalepTipAciklama { get; set; }
        public string TaahhutAciklama { get; set; }
    }
    public class fmTalepSurec : PagerOption
    {
        public bool? IsAktif { get; set; }
        public string EnstituKod { get; set; }
        public IEnumerable<frTalepSurec> Data { get; set; }
    }
    public class frTalepSurec : TalepSurecleri
    {
        public string EnstituAdi { get; set; }
        public string IslemYapan { get; set; }
        public bool AktifSurec { get; set; }
    }

    public class BelgeTalepleriDetaymodel : BelgeTalepleri
    {
        public int? KullaniciID { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string ResimAdi { get; set; }
        public string ClassName { get; set; }
        public string Color { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string DonemAdi { get; set; }
        public string DurumAdi { get; set; }
        public string DurumListeAdi { get; set; }
        public string BelgeTipAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public int SeciliDonemdeVerilenMiktar { get; set; }
        public int SeciliDonemdehenuzVerilmeyenMiktar { get; set; }
        public int DonemdeAlinabilecekToplamMiktar { get; set; }
        public bool DonemlikKotaVar { get; set; }
        public bool Edit { get; set; }
    }

    public class fmBelgeTipleri : PagerOption
    {
        public string BelgeTipAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frBelgeTipleri> data { get; set; }

    }
    public class frBelgeTipleri : BelgeTipleri
    {
        public string IslemYapan { get; set; }

    }

    public class fmSRTalepTipleri : PagerOption
    {
        public string TalepTipAdi { get; set; }
        public bool? IsTezSinavi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frSRTalepTipleri> data { get; set; }

    }
    public class frSRTalepTipleri : SRTalepTipleri
    {

        public bool IsTezSinavi { get; set; }
        public int? MaxCevaplanmamisTalep { get; set; }
        public int? IstenenJuriSayisiDR { get; set; }
        public int? IstenenJuriSayisiYL { get; set; }
        public bool IsAktif { get; set; }
        public DateTime IslemTarihi { get; set; }
        public int IslemYapanID { get; set; }
        public string IslemYapanIP { get; set; }
        public string IslemYapan { get; set; }
        public List<int> TalepTipAktifAyIds { get; set; }
        public List<int> KullaniciTipIDs { get; set; }

    }

    public class kmSalonlar : SRSalonlar
    {


        public List<string> HaftaGunleri { get; set; }
        public List<TimeSpan> BasSaat { get; set; }
        public List<TimeSpan> BitSaat { get; set; }


        public List<SRSaatlerMDL> Saatler { get; set; }
        public object TeslimBitisSaat { get; internal set; }

        public kmSalonlar()
        {
            Saatler = new List<SRSaatlerMDL>();
        }
    }
    public class SRSaatlerMDL : SRSaatler
    {
        public List<CmbIntDto> GunNos { get; set; }
    }
    public class SRSaatKontrolModel
    {
        public string HaftaGunleri { get; set; }
        public List<int> GHaftaGunleri { get; set; }
        public TimeSpan? BasSaat { get; set; }
        public TimeSpan? BitSaat { get; set; }

        public List<string> HaftaGunleriList { get; set; }
        public List<TimeSpan> BasSaatList { get; set; }
        public List<TimeSpan> BitSaatList { get; set; }
    }
    public class fmBelgeTipDetay : PagerOption
    {
        public string BelgeTipAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frBelgeTipDetay> data { get; set; }

    }
    public class frBelgeTipDetay : BelgeTipDetay
    {
        public List<string> BelgeTipAdi { get; set; }
        public List<BTSaatShowModel> Saatler { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string IslemYapan { get; set; }

    }

    public class fmMulakatSinavTurleri : PagerOption
    {
        public string MulakatSinavTurAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frMulakatSinavTurleri> data { get; set; }

    }
    public class frMulakatSinavTurleri : MulakatSinavTurleri
    {
        public string IslemYapan { get; set; }

    }
    public class fmSinavSonuclari : PagerOption
    {
        public string AdSoyad { get; set; }
        public int? SinavTipKod { get; set; }
        public int? SinavDilID { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frSinavSonuclari> data { get; set; }

    }
    public class frSinavSonuclari : SinavSonuclari
    {
        public string AdSoyad { get; set; }
        public string SinavTipAdi { get; set; }
        public string SinavDilAdi { get; set; }
        public string IslemYapan { get; set; }

    }
    public class kmMulakat : Mulakat
    {
        public List<int> MulakatDetayID { get; set; }
        public List<int> KampusID { get; set; }
        public List<int> MulakatSinavTurID { get; set; }
        public List<int> YuzdeOran { get; set; }
        public List<DateTime> SinavTarihi { get; set; }
        public List<string> YerAdi { get; set; }

        public List<int> MulakatJuriID { get; set; }
        public List<bool> IsAsil { get; set; }
        public List<int> SiraNo { get; set; }
        public List<string> JuriAdi { get; set; }

        public List<krMulakatDetay> MulakatDetayi { get; set; }
        public kmMulakat()
        {
            MulakatDetayID = new List<int>();
            KampusID = new List<int>();
            MulakatSinavTurID = new List<int>();
            YuzdeOran = new List<int>();
            SinavTarihi = new List<DateTime>();
            YerAdi = new List<string>();

            MulakatJuriID = new List<int>();
            IsAsil = new List<bool>();
            SiraNo = new List<int>();
            JuriAdi = new List<string>();

            MulakatDetayi = new List<krMulakatDetay>();
        }
    }
    public class krMulakatDetay : MulakatDetay
    {
        public int KullaniID { get; set; }
        public string DilKodu { get; set; }
        public string KampusAdi { get; set; }
        public string MulakatSinavTurAdi { get; set; }
        public string YuzdeOranStr { get; set; }
        public int? YuzdeOran { get; set; }
    }

    public class fmMulakatSureci : PagerOption
    {
        public string ProgramAdi { get; set; }
        public string ProgramKod { get; set; }
        public int? OgrenimTipKod { get; set; }
        public int? BasvuruSurecID { get; set; }
        public IEnumerable<frMulakatSureci> Data { get; set; }
    }
    public class frMulakatSureci : Mulakat
    {
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public DateTime? SonucGirisBaslangicTarihi { get; set; }
        public DateTime? SonucGirisBitisTarihi { get; set; }
        public string BasvuruSurecAdi { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string IslemYapan { get; set; }
    }

    public class bsMulakatDetay : Mulakat
    {
        public int SelectedTabIndex { get; set; }
        public int BaslangicYil { get; set; }
        public int BitisYil { get; set; }
        public string DonemAdi { get; set; }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public bool MulakatSinavNotGirisiAktif { get; set; }
        public DateTime? SonucGirisBaslangicTarihi { get; set; }
        public DateTime? SonucGirisBitisTarihi { get; set; }
        public DateTime? AGNOGirisBaslangicTarihi { get; set; }
        public DateTime? AGNOGirisBitisTarihi { get; set; }
        public string BasvuruSurecAdi { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public bool IsUcretliKayit { get; set; }
        public bool Ucretli { get; set; }
        public double? Ucret { get; set; }
        public string ProgramAdi { get; set; }
        public bool MulakatSurecineGirecek { get; set; }
        public bool AlanIciBilimselHazirlik { get; set; }
        public bool AlanDisiBilimselHazirlik { get; set; }
        public string OgrenimTipAdi { get; set; }
        public double BasariNotOrtalamasi { get; set; }
        public List<krMulakatDetay> MulakatDetay { get; set; }
        public List<krMulakatSonuc> MulakatSonuc { get; set; }
        public bool OrtakKota { get; set; }
        public int? OrtakKotaSayisi { get; set; }
        public int AlanIciKota { get; set; }
        public int AlanDisiKota { get; set; }
        public int AlanIciEkKota { get; set; }
        public int AlanDisiEkKota { get; set; }
        public bool YaziliNotuIstensin { get; set; }
        public bool SozluNotuIstensin { get; set; }
        public bool SonucHesaplandi { get; set; }
        public bool IsAlesYerineDosyaNotuIstensin { get; set; }
        public bool IsBelgeYuklemeVar { get; set; }
        public bsMulakatDetay()
        {
            SonucHesaplandi = false;
            MulakatDetay = new List<krMulakatDetay>();
            MulakatSonuc = new List<krMulakatSonuc>();
        }
    }
    public class krMulakatSonuc : MulakatSonuclari
    {

        public bool SuccessRow { get; set; }
        public int BasvuruSurecTipID { get; set; }
        public int KullaniID { get; set; }
        public string AdSoyad { get; set; }
        public string UniqueID { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string ProgramKod { get; set; }
        public int OgrenimTipKod { get; set; }
        public bool MulakatSurecineGirecek { get; set; }
        public string MulakatSonucTipAdi { get; set; }
        public BasvurularSinavBilgi Sinav { get; set; }
        public string DekontNo { get; set; }
        public DateTime? DekontTarihi { get; set; }

        public int? LOgrenimDurumID { get; set; }
        public string LOgrenimDurumAdi { get; set; }
        public int MezuniyetNotSistemi { get; set; }
        public double MezuniyetNotu { get; set; }


        public int? KayitOncelikSiraNo { get; set; }

        public int? SinavTipKod { get; set; }
        public string SinavAdi { get; set; }
        public double? SinavNotu { get; set; }

        public int JuriCount { get; set; }
        public int YerCount { get; set; }

        public double? MinAGNO { get; set; }

        public int AlanKota { get; set; }
        public int AlanKotaYedek { get; set; }

        public bool YaziliSinaviIstensin { get; set; }
        public bool SozluSinaviIstensin { get; set; }

        public bool ShowBilimselHazirlik { get; set; }
        public bool EnabledBilimselHazirlik { get; set; }
        public bool IsUcretliKayit { get; set; }
        public bool? IsDilTaahhutVar { get; set; }
        public bool IsAlesYerineDosyaNotuIstensin { get; set; }
        public bool Ingilizce { get; set; }
        public int KayittaIstenecekBelgeCount { get; set; }
        public int BasvurudaYuklenenBelgeCount { get; set; }
        public krMulakatSonuc()
        {
            SiraNo = 0;
            SuccessRow = false;
            SinavaGirmediY = false;
            SinavaGirmediS = false;
        }
    }
    public class krMulakatSonucPostModel
    {
        public int BasvuruSurecID { get; set; }
        public bool MulakatSurecineGirecek { get; set; }
        public int MulakatID { get; set; }
        public bool IsAlesYerineDosyaNotuIstensin { get; set; }
        public int AlanTipID { get; set; }
        public string ProgramKod { get; set; }
        public int OgrenimTipKod { get; set; }
        public List<int> BasvuruTercihID { get; set; }
        public List<bool?> SinavaGirmediY { get; set; }
        public List<double?> AlesNotuOrDosyaNotu { get; set; }
        public List<double?> YaziliNotu { get; set; }
        public List<bool?> SinavaGirmediS { get; set; }
        public List<double?> SozluNotu { get; set; }
        public List<bool?> BilimselHazirlikVar { get; set; }

        public krMulakatSonucPostModel()
        {
            BasvuruTercihID = new List<int>();
            SinavaGirmediY = new List<bool?>();
            YaziliNotu = new List<double?>();
            SinavaGirmediS = new List<bool?>();
            SozluNotu = new List<double?>();
        }
    }

    public class fmMsonucOranModel
    {
        public bool MulakatSurecineGirecek { get; set; }
        public int AlanTipID { get; set; }
        public int Toplam { get; set; }
        public double ToplamYuzde { get; set; }
        public int Kota { get; set; }
        public double KotaYuzde { get; set; }
        public int KayitCount { get; set; }
        public double KayitYuzde { get; set; }
        public int AsilCount { get; set; }
        public double AsilYuzde { get; set; }
        public int YedekCount { get; set; }
        public double YedekYuzde { get; set; }
        public int KazanamayanCount { get; set; }
        public double KazanamayanYuzde { get; set; }
    }
    public class fmMulakatSonuc
    {
        public int ToplamTercihCount { get; set; }
        public int HesaplananTercihCount { get; set; }
        public int ToplamProgramCount { get; set; }
        public int HesaplananProgramCount { get; set; }
        public bool HesaplamaYapildi { get; set; }
        public bool TumProgramlarHesaplandi { get; set; }
        public int MulakatSurecineGirecekToplamBasvuru { get; set; }
        public int MulakatSurecineGirmeyecekToplamBasvuru { get; set; }
        public List<fmMsonucOranModel> OranModel { get; set; }
        public List<frMulakatSonucDetay> MulakatSonucDetay { get; set; }
        public fmMulakatSonuc()
        {
            MulakatSonucDetay = new List<frMulakatSonucDetay>();
            OranModel = new List<fmMsonucOranModel>();
            OranModel.Add(new fmMsonucOranModel { MulakatSurecineGirecek = true, AlanTipID = AlanTipi.AlanIci });
            OranModel.Add(new fmMsonucOranModel { MulakatSurecineGirecek = true, AlanTipID = AlanTipi.AlanDisi });
            OranModel.Add(new fmMsonucOranModel { MulakatSurecineGirecek = false, AlanTipID = AlanTipi.AlanIci });
            OranModel.Add(new fmMsonucOranModel { MulakatSurecineGirecek = false, AlanTipID = AlanTipi.AlanDisi });
        }
    }
    public class frMulakatSonucDetay
    {
        public int? MulakatID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public bool MulakatSurecineGirecek { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public int AIKota { get; set; }
        public int ADKota { get; set; }
        public int ToplamBasvuru { get; set; }
        public int AIKayitCount { get; set; }
        public int AIAsilCount { get; set; }
        public int AIYedekCount { get; set; }
        public int AIKazanamayanCount { get; set; }
        public int AISinavaGirmeyenCount { get; set; }
        public int ADKayitCount { get; set; }
        public int ADAsilCount { get; set; }
        public int ADYedekCount { get; set; }
        public int ADKazanamayanCount { get; set; }
        public int ADSinavaGirmeyenCount { get; set; }
        public bool Ucretli { get; set; }
        public double? Ucret { get; set; }
        public string KullaniciTipAdi { get; set; }

    }

    public class raporOtipModel
    {
        public string OgrenimTipAdi { get; set; }
        public double GBNO { get; set; }
        public int TaslakCount { get; set; }
        public int OnaylananCount { get; set; }
        public int IptalEdilenCount { get; set; }
        public int KayitCount { get; set; }
    }
    public class raporLUBModel
    {
        public string EnstituAdi { get; set; }
        public string AkademikYil { get; set; }
        public string SurecTarihi { get; set; }
        public int ToplamTercihSayisi { get; set; }
        public IEnumerable<raporOtipModel> OgrenimTipleri { get; set; }
        public fmMsonucOranModel AIToplamModel { get; set; }
        public fmMsonucOranModel ADToplamModel { get; set; }
        public IEnumerable<frMulakatSonucDetay> BasvuruSonuclari { get; set; }
    }
    public class raporBTSayisalModel
    {
        public int Yil { get; set; }
        public int Ay { get; set; }
        public int BelgeTipID { get; set; }
        public string BelgeTipAdi { get; set; }
        public int Toplam { get; set; }
        public int TalepEdilen { get; set; }
        public int Verilen { get; set; }
        public int Kapatilan { get; set; }
        public int IptalEdilen { get; set; }
    }
    public class raporBTModel
    {
        public string EnstituAdi { get; set; }
        public string SurecTarihi { get; set; }
        public IEnumerable<raporBTSayisalModel> YilaGoreToplam { get; set; }
        public IEnumerable<raporBTSayisalModel> DetayliToplam { get; set; }
    }
    public class raporMezuniyetBasvuruFormModel
    {
        public string YayinTurAdi { get; set; }
        public string DanismanIsmiVarMi { get; set; }
        public string TezIcerigiIleUygunMu { get; set; }
        public string Index { get; set; }
        public string Aciklama { get; set; }
    }
    public class fmMulakatNotGiris
    {

        public int SinavBilgisiGirilenProgramCount { get; set; }
        public int YerJuriBilgisiGirisProgramCount { get; set; }
        public int ToplamBasvuru { get; set; }
        public int ToplamMGiris { get; set; }
        public int AIToplamBasvuru { get; set; }
        public int ADToplamBasvuru { get; set; }
        public int AIMToplamBasvuru { get; set; }
        public int ADMToplamBasvuru { get; set; }
        public int AIMNotGirildiCount { get; set; }
        public int ADMNotGirildiCount { get; set; }
        public int YerJuriBilgisiGirisCount { get; set; }
        public bool SinavYerBilgisiMailiGonderildi { get; set; }
        public DateTime? SinavYerBilgisiMailiGonderimTarihi { get; set; }
        public List<frMulakatNotGirisDetay> MulakatNotGirisDetay { get; set; }
    }
    public class frMulakatNotGirisDetay
    {
        public bool MulakatSurecineGirecek { get; set; }
        public bool IsAlesYerineDosyaNotuIstensin { get; set; }
        public int? MulakatID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public int AlanIciKota { get; set; }
        public int AlanDisiKota { get; set; }
        public bool YerJuriBilgisiGirildi { get; set; }
        public bool SinavNotGirisiYapildi { get; set; }
        public int ToplamBasvuru { get; set; }
        public int ToplamMGiris { get; set; }
        public int AIToplamBasvuru { get; set; }
        public int ADToplamBasvuru { get; set; }
        public int AIMToplamBasvuru { get; set; }
        public int ADMToplamBasvuru { get; set; }
        public int AIMNotGirildiCount { get; set; }
        public int ADMNotGirildiCount { get; set; }

        public bool Ucretli { get; set; }
        public double? Ucret { get; set; }
        public string KullaniciTipAdi { get; set; }

    }

    public class rprBasvuruSonucModel
    {
        public int SiraNo { get; set; }
        public Guid RowID { get; set; }
        public int? PSiraNo { get; set; }
        public string EnstituKod { get; set; }
        public string WsXmlData { get; set; }
        public bool AlesNotuYuksekOlanAlinsin { get; set; }
        public int BasvuruSurecID { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public bool Ingilizce { get; set; }
        public string ProgramGrupAdi { get; set; }
        public int AlesTipID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public int AlanTipID { get; set; }
        public string AlanTipAdi { get; set; }
        public int Kota { get; set; }
        public int EkKota { get; set; }
        public bool? KayitOldu { get; set; }



        public int TercihID { get; set; }
        public string AdSoyad { get; set; }
        public string Telefon { get; set; }
        public string EMail { get; set; }
        public double? AlesNotu { get; set; }
        public double? GirisSinavNotu { get; set; }
        public double? Agno { get; set; }
        public double? YaziliNotu { get; set; }
        public double? SozluNotu { get; set; }
        public double? GenelBasariNotu { get; set; }
        public int TercihNo { get; set; }
        public int MulakatSonucTipID { get; set; }
        public string MulakatSonucTipAdi { get; set; }

        public int? MulakatID { get; set; }
        public bool? SinavaGirmediY { get; set; }
        public bool? SinavaGirmediS { get; set; }
    }

    public class rprBasvuruSonucBolumModel
    {
        public string ProgramAdi { get; set; }
        public string BolumAdi { get; set; }
        public List<rprBasvuruSonucModel> ProgramB { get; set; }
        public List<krMulakatDetay> MulakatDetayB { get; set; }
        public List<rwMulakatJuri> MulakatJuriB { get; set; }
        public List<rwMulakatJuri> k2 { get; set; }
        public rprBasvuruSonucBolumModel()
        {
            k2 = new List<rwMulakatJuri>();
        }
    }
    public class rwMulakatJuri : MulakatJuri
    {
        public string AsilYedek { get; set; }
    }
    public class mailMulakatSinavYerJuriBilgi
    {
        public int MulakatID { get; set; }
        public int BasvuruSurecID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public List<krMulakatDetay> MulakatDetayB { get; set; }
        public List<MulakatJuri> MulakatJuriB { get; set; }
        public List<CmbIntDto> GonderilecekMails { get; set; }
    }

    public class mailMulakatSinavYerJuriBilgiBolum
    {
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public List<mailMulakatSinavYerJuriBilgiBolumDetay> detay { get; set; }
        public List<string> GonderilecekMails { get; set; }
        public mailMulakatSinavYerJuriBilgiBolum()
        {
            detay = new List<mailMulakatSinavYerJuriBilgiBolumDetay>();
        }
    }
    public class mailMulakatSinavYerJuriBilgiBolumDetay
    {
        public bool EksikBilgiSinavYerBilgi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public int AlaniciBasvuranCount { get; set; }
        public int AlandisiBasvuranCount { get; set; }
        public int AlaniciKota { get; set; }
        public int AlandisiKota { get; set; }
    }
    public class mailBsonucModel : rprBasvuruSonucModel
    {
        public int KullaniciID { get; set; }
        public int BasvuruID { get; set; }
        public string EgitimOgretimYili { get; set; }
        public string EnstituAdi { get; set; }
        public string WebAdresi { get; set; }
        public string Link { get; set; }
        public string Email { get; set; }
    }
    public class BTSaatShowModel : BelgeTipDetaySaatler
    {
        public List<CmbIntDto> GunNos { get; set; }
    }
    public class BTSaatKontrolModel
    {
        public string HaftaGunleri { get; set; }
        public List<int> GHaftaGunleri { get; set; }
        public TimeSpan? TalepBaslangicSaat { get; set; }
        public TimeSpan? TalepBitisSaat { get; set; }
        public int? EklenecekGun { get; set; }
        public TimeSpan? TeslimBaslangicSaat { get; set; }
        public TimeSpan? TeslimBitisSaat { get; set; }

        public List<string> HaftaGunleriList { get; set; }
        public List<TimeSpan> TalepBaslangicSaatList { get; set; }
        public List<TimeSpan> TalepBitisSaatList { get; set; }
    }

    public class BelgeTipDetayKayitModel : BelgeTipDetay
    {
        public List<int> BelgeTipID { get; set; }
        public List<string> HaftaGunleri { get; set; }
        public List<TimeSpan> TalepBaslangicSaat { get; set; }
        public List<TimeSpan> TalepBitisSaat { get; set; }
        public List<int> EklenecekGun { get; set; }
        public List<TimeSpan> TeslimBaslangicSaat { get; set; }
        public List<TimeSpan> TeslimBitisSaat { get; set; }


        public List<BTSaatShowModel> Saatler { get; set; }
        public List<int> SeciliBelgeTipler { get; set; }
    }
    public class fmSalonlar : PagerOption
    {
        public string EnstituKod { get; set; }
        public string SalonAdi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frSalonlar> data { get; set; }

    }
    public class frSalonlar : SRSalonlar
    {
        public string EnstituAdi { get; set; }
        public string IslemYapan { get; set; }
        public List<SRSaatlerMDL> Saatler { get; set; }
        public List<SRSalonTalepTipleri> SRSalonTalepTipleri { get; set; }

    }
    public class fmTalepler : PagerOption
    {
        public string EnstituKod { get; set; }
        public int? SRTalepTipID { get; set; }
        public int? SRSalonID { get; set; }
        public int? HaftaGunID { get; set; }
        public TimeSpan? BasSaat { get; set; }
        public TimeSpan? BitSaat { get; set; }
        public int? SRDurumID { get; set; }
        public string Aciklama { get; set; }
        public IEnumerable<frTalepler> data { get; set; }

    }
    public class frTalepler : SRTalepleri
    {
        public string EnstituAdi { get; set; }
        public string OgrenciNo { get; set; }
        public string SicilNo { get; set; }
        public string TalepYapan { get; set; }
        public string ResimAdi { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string TalepTipAdi { get; set; }
        public bool IsTezSinavi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string HaftaGunAdi { get; set; }
        public string SDurumAdi { get; set; }
        public string SDurumListeAdi { get; set; }
        public string SClassName { get; set; }
        public string SColor { get; set; }
        public string DurumAdi { get; set; }
        public string DurumListeAdi { get; set; }
        public string ClassName { get; set; }
        public string Color { get; set; }
        public string IslemYapan { get; set; }
        public bool IsTezDiliTr { get; set; }
        public string TezBaslikTr { get; set; }
        public string TezBaslikEn { get; set; }
        public bool IsSonSRTalebi { get; set; }
        public bool IsOncedenUzatmaAlindi { get; set; }
        public SrDurumSelectList SrDurumSelectList = new SrDurumSelectList();
        public List<SRTaleplerJuri> JuriBilgi { get; set; }
        public DateTime UzatmaSonSRTarih { get; set; }
        public DateTime TeslimSonTarih { get; set; }

        public string JuriSonucMezuniyetSinavDurumAdi { get; set; }

    }
    public class SrDurumSelectList
    {
        public SelectList SRDurumID { get; set; }
        public SelectList MezuniyetSinavDurumID { get; set; }
        public SelectList IsMezunOldu { get; set; }
    }
    public class fmSalonBilgi : PagerOption
    {
        public string EnstituKod { get; set; }
        public int? SRTalepTipID { get; set; }
        public List<int> SRSalonID { get; set; }
        public List<int> HaftaGunID { get; set; }
        public DateTime? BasTarih { get; set; }
        public DateTime? BitTarih { get; set; }
        public TimeSpan? BasSaat { get; set; }
        public TimeSpan? BitSaat { get; set; }
        public bool? IsDolu { get; set; }
        public IEnumerable<frSalonBilgi> data { get; set; }

        public fmSalonBilgi()
        {
            SRSalonID = new List<int>();
            HaftaGunID = new List<int>();
        }

    }
    public class frSalonBilgi
    {
        public string EnstituKod { get; set; }
        public string EnstituAdi { get; set; }
        public bool IsOzelTanim { get; set; }
        public int KayitID { get; set; }
        public DateTime Tarih { get; set; }
        public int HaftaGunID { get; set; }
        public System.TimeSpan BasSaat { get; set; }
        public System.TimeSpan BitSaat { get; set; }
        public string HaftaGunAdi { get; set; }
        public int SRSalonID { get; set; }
        public string SalonAdi { get; set; }
        public string ClassName { get; set; }
        public string Color { get; set; }
        public int? GTID { get; set; }
        public int? OTID { get; set; }
        public bool RemoveRow { get; set; }
    }




    public class kmSRTalep : SRTalepleri
    {
        public bool YetkisizErisim { get; set; }
        public string AdSoyad { get; set; }
        public string OgrenciNo { get; set; }
        public string SicilNo { get; set; }
        public List<string> JuriAdi { get; set; }
        public List<string> Telefon { get; set; }
        public List<string> Email { get; set; }
        public TimeSpan? BasSaat { get; set; }
        public TimeSpan? BitSaat { get; set; }
        public string MzRowID { get; set; }
        public bool IsSalonSecilsin { get; set; }
        public DateTime? SonSrTarihi { get; set; }
        public kmSRTalep()
        {
            JuriAdi = new List<string>();
            Telefon = new List<string>();
            Email = new List<string>();
            this.SRTaleplerJuris = new List<SRTaleplerJuri>();
        }
    }

    public class SRSalonSaatler : SRSaatler
    {
        public string HaftaGunAdi { get; set; }
        public int SalonDurumID { get; set; }
        public bool Checked { get; set; }
        public string SalonDurumAdi { get; set; }
        public string Color { get; set; }
        public bool Disabled { get; set; }
        public string Aciklama { get; set; }
    }
    public class SRSalonSaatlerModel
    {
        public bool IsPopupFrame { get; set; }
        public string DilKodu { get; set; }
        public int SRSalonID { get; set; }
        public string SRSalonAdi { get; set; }
        public bool HaftaGunundeSaatlerVar { get; set; }
        public DateTime Tarih { get; set; }
        public int HaftaGunID { get; set; }
        public string HaftaGunAdi { get; set; }
        public int BosSaatSayisi { get; set; }
        public int DoluSaatSayisi { get; set; }
        public string GenelAciklama { get; set; }
        public List<SRSalonSaatler> Data { get; set; }
        public string MzRowID { get; set; }
        public SRSalonSaatlerModel()
        {
            Data = new List<SRSalonSaatler>();
        }
    }
    public class mailSRjuriModel : SRTalepleri
    {
        public string DilKodu { get; set; }
        public string EgitimOgretimYili { get; set; }
        public string UniversiteAdi { get; set; }
        public string EnstituAdi { get; set; }
        public string AdSoyad { get; set; }
        public string ProgramAdi { get; set; }
        public string OgrenciAdi { get; set; }
        public string OgrenciNo { get; set; }
        public string Yer { get; set; }
        public string Tarih { get; set; }
        public string Saat { get; set; }
        public string WebAdresi { get; set; }
        public string Email { get; set; }
    }
    public class fmOzelTanimlar : PagerOption
    {
        public string EnstituKod { get; set; }
        public int? SROzelTanimTipID { get; set; }
        public string Aciklama { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frOzelTanimlar> data { get; set; }

    }
    public class frOzelTanimlar : SROzelTanimlar
    {
        public string EnstituAdi { get; set; }
        public string SROzelTanimTipAdi { get; set; }
        public string TalepTipAdi { get; set; }
        public string SalonAdi { get; set; }
        public string AyAdi { get; set; }
        public string IslemYapan { get; set; }
    }
    public class rprModelKazananList
    {
        public string ProgramKodu { get; set; }
        public string ProgramAdi { get; set; }
        public string OgrenciNo { get; set; }
        public string AdSoyad { get; set; }
        public string ResimAdi { get; set; }
    }

    public class rprModelBolumProgramList
    {
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string ProgramKodu { get; set; }
        public string ProgramAdi { get; set; }
        public string EgitimDili { get; set; }
    }
    public class rprModelProgramList
    {
        public string AnabilimDaliAdi { get; set; }
        public string EnstituKod { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public string EgitimDili { get; set; }
        public List<rprModelProgramDilBilgi> ProgramDilBilgi { get; set; }
        public List<rprModelProgramAgnoKriterBilgi> AgnoKriterBilgi { get; set; }
        public List<CmbIntDto> EslestirilenBolumler { get; set; }
        public rprModelProgramList()
        {
            ProgramDilBilgi = new List<rprModelProgramDilBilgi>();
            AgnoKriterBilgi = new List<rprModelProgramAgnoKriterBilgi>();
            EslestirilenBolumler = new List<CmbIntDto>();
        }


    }


    public class rprMezuniyetTezDegerlendirmeYayinBilgi
    {
        public string YayinTurAdi { get; set; }
        public string YayinBasligi { get; set; }
        public string DoiNumarasi { get; set; }
        public string IndexBilgisi { get; set; }
    }
    public class rprModelProgramAgnoKriterBilgi
    {
        public string AgnoAlimTipi { get; set; }
        public List<CmbStringDto> OgretimKriterleri { get; set; }
        public rprModelProgramAgnoKriterBilgi()
        {
            OgretimKriterleri = new List<CmbStringDto>();
        }
    }
    public class rprModelProgramDilBilgi
    {
        public string SinavAdi { get; set; }
        public List<krSinavTipleriOTNotAraliklari> OgretimNotKriterleri { get; set; }
        public List<SinavNotlari> SinavNotlariList { get; set; }
        public List<SinavTiplerSubSinavAralik> SinavNotAralikList { get; set; }
        public rprModelProgramDilBilgi()
        {
            OgretimNotKriterleri = new List<krSinavTipleriOTNotAraliklari>();
            SinavNotlariList = new List<SinavNotlari>();
            SinavNotAralikList = new List<SinavTiplerSubSinavAralik>();
        }
    }


    public class mailListModel
    {
        public string id { get; set; }
        public string AdSoyad { get; set; }
        public string text { get; set; }
        public string Images { get; set; }
    }

    public class fmTDOBasvuru : PagerOption
    {
        public int? TDOBasvuruID { get; set; }
        public int? KullaniciID { get; set; }
        public string Kod { get; set; }
        public bool Expand { get; set; }
        public int? UyrukKod { get; set; }
        public string EnstituKod { get; set; }
        public string KayitDonemi { get; set; }
        public int? KullaniciTipID { get; set; }
        public string AdSoyad { get; set; }
        public int? OgrenimTipKod { get; set; }
        public string ProgramKod { get; set; }
        public string AnabilimDaliKod { get; set; }
        public DateTime? TIBaslangicTarihi { get; set; }
        public DateTime? TIBitisTarihi { get; set; }
        public bool AktifOgrenimIcinBasvuruVar { get; set; }
        public string AktifDonemID { get; set; }
        public string DonemID { get; set; }
        public int? AktifDurumID { get; set; }
        public int? DurumID { get; set; }
        public int? AktifEsDurumID { get; set; }
        public int? EsDurumID { get; set; }
        public IEnumerable<frTDOBasvuru> Data { get; set; }
    }
    public class frTDOBasvuru : TDOBasvuru
    {
        public int? TezDanismanID { get; set; }
        public string EnstituAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string TcPasaPortNo { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string AdSoyad { get; set; }
        public string EMail { get; set; }
        public string CepTel { get; set; }
        public DateTime? GsisKayitTarihi { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public string AktifDonemID { get; set; }
        public string AktifDonemAdi { get; set; } 
        public int? VarolanTezDanismanID { get; set; }
        public bool? VarolanDanismanOnayladi { get; set; }
        public bool? DanismanOnayladi { get; set; }
        public bool? EYKYaGonderildi { get; set; }
        public DateTime? EYKYaGonderildiIslemTarihi { get; set; }
        public DateTime? EYKYaGonderildiIslemTarihiES { get; set; }
        public bool? EYKDaOnaylandi { get; set; }
        public bool EsDanismanOnerisiVar { get; set; }
        public bool? Es_EYKYaGonderildi { get; set; }
        public bool? Es_EYKDaOnaylandi { get; set; }
        public int Sira { get; set; }
        public DateTime RowDate { get; set; }
        public List<TDODanismanFiltreModel> TDODanismanDetayModels { get; set; }
    }
    public class KmTDOBasvuruDanisman : TDOBasvuruDanisman
    {
        public bool? isCopy { get; set; }
        public bool? IsTezDiliTr { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public SelectList SListTDoDanismanTalepTip { get; set; }
        public SelectList SListSinav { get; set; }
        public SelectList SListSinavNot { get; set; }
        public SelectList SListTDAnabilimDali { get; set; }
        public SelectList SListTDProgram { get; set; }
        public SelectList SListTDSinav { get; set; }
        public SelectList SListTDSinavNot { get; set; }
    }

    public class KmTDOBasvuru : TDOBasvuru
    {
        public string OgrenimTipAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
    }
    public class TDODanismanFiltreModel
    {
        public string FormKodu { get; set; }
        public string RaporDonemID { get; set; }
        public string DanismanAdSoyad { get; set; }
        public int TDODanismanTalepTipID { get; set; }
        public bool? VarolanDanismanOnayladi { get; set; }
        public bool? DanismanOnayladi { get; set; }
        public bool? EYKYaGonderildi { get; set; }
        public bool? EYKDaOnaylandi { get; set; }
        public string Es_FormKodu { get; set; }
        public string Es_DanismanAdSoyad { get; set; }
        public bool? Es_EYKYaGonderildi { get; set; }
        public bool? Es_EYKDaOnaylandi { get; set; }
    }
    public class fmTIBasvuru : PagerOption
    {
        public int? TIBasvuruID { get; set; }
        public int? KullaniciID { get; set; }
        public string Kod { get; set; }
        public Guid? IsDegerlendirme { get; set; }
        public bool Expand { get; set; }
        public int? UyrukKod { get; set; }
        public string EnstituKod { get; set; }
        public string KayitDonemi { get; set; }
        public int? KullaniciTipID { get; set; }
        public string AdSoyad { get; set; }
        public int? OgrenimTipKod { get; set; }
        public string ProgramKod { get; set; }
        public string AnabilimDaliKod { get; set; }
        public int? TIDurumID { get; set; }
        public DateTime? TIBaslangicTarihi { get; set; }
        public DateTime? TIBitisTarihi { get; set; }
        public bool AktifOgrenimIcinBasvuruVar { get; set; }
        public string AktifTIAraRaporDonemID { get; set; }
        public string TIAraRaporDonemID { get; set; }
        public int? AktifTIAraRaporRaporDurumID { get; set; }
        public int? TIAraRaporRaporDurumID { get; set; }
        public int? AktifAraRaporSayisi { get; set; }
        public int? TIAraRaporSayisi { get; set; }

        public IEnumerable<frTIBasvuru> Data { get; set; }
    }
    public class frTIBasvuru : TIBasvuru
    {
        public string EnstituAdi { get; set; }
        public string MezuniyetSurecAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string TcPasaPortNo { get; set; }
        public int SurecBaslangicYil { get; set; }
        public int DonemID { get; set; }
        public string DonemAdi { get; set; }
        public DateTime BasTar { get; set; }
        public DateTime BitTar { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string AdSoyad { get; set; }
        public string EMail { get; set; }
        public string CepTel { get; set; }
        public DateTime? GsisKayitTarihi { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public string IslemYapan { get; set; }
        public string FormNo { get; set; }

        public int? TIAraRaporRaporDurumID { get; set; }
        public string TIAraRaporRaporDurumAdi { get; set; }
        public string TIAraRaporAktifDonemAdi { get; set; }
        public int? AraRaporSayisi { get; set; }
        public bool? IsOyBirligiOrCouklugu { get; set; }
        public bool? IsBasariliOrBasarisiz { get; set; }
        public List<TIAraraporFiltreModel> tIAraraporFiltreModels { get; set; }
        public string TIAraRaporAktifDonemID { get; set; }
        public int? TIAraRaporTezDanismanID { get; set; } 

    }
    public class TIAraraporFiltreModel
    {
        public string FormKodu { get; set; }
        public string RaporDonemID { get; set; }
        public int AraRaporSayisi { get; set; }
        public int TezDanismanID { get; set; }
        public string DanismanAdSoyad { get; set; }
        public int TIBasvuruAraRaporDurumID { get; set; }
        public List<string> KomiteUyeleri { get; set; }
    }

    public class KmTIBasvuru : TIBasvuru
    {
        public string OgrenimTipAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
    }

    public class TIAraRaporFormuModel : TIBasvuruAraRapor
    {
        public string DilKodu { get; set; }
        public string OgrenciAnabilimdaliProgramAdi { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public bool? IsYokDrBursiyeriVar { get; set; }
        public SelectList SListAraRaporSayisi { get; set; }
        public int SelectedTabID { get; set; }
        public List<int> TabID { get; set; }
        public List<string> AnaTabAdi { get; set; }
        public List<string> JuriTipAdi { get; set; }
        public List<string> AdSoyad { get; set; }
        public List<string> UnvanAdi { get; set; }
        public List<string> EMail { get; set; }
        public List<int?> UniversiteID { get; set; }
        public List<string> AnabilimdaliProgramAdi { get; set; }
        public List<string> DilSinavAdi { get; set; }
        public List<string> IsDilSinaviOrUniversite { get; set; }
        public List<string> DilPuani { get; set; }
        public List<string> SinavTarihi { get; set; }

        public SelectList SListUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
        public List<KrTIBasvuruAraRaporKomite> KomiteList { get; set; }

        public HttpPostedFileBase Dosya { get; set; }

        public TIAraRaporFormuModel()
        {
            AnaTabAdi = new List<string>();
            JuriTipAdi = new List<string>();
            AdSoyad = new List<string>();
            UnvanAdi = new List<string>();
            EMail = new List<string>();
            UniversiteID = new List<int?>();
            AnabilimdaliProgramAdi = new List<string>();
            DilSinavAdi = new List<string>();
            DilPuani = new List<string>();
            SinavTarihi = new List<string>();
            KomiteList = new List<KrTIBasvuruAraRaporKomite>();
        }
    }
    public class KrTIBasvuruAraRaporKomite : TIBasvuruAraRaporKomite
    {

        public SelectList SlistUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
    }
    public class kmMezuniyetBasvuru : MezuniyetBasvurulari
    {
        public int SelectedTabIndex { get; set; }
        public bool Onaylandi { get; set; }
        public bool sbmtForm { get; set; }
        public int StepNo { get; set; }
        public int? SetSelectedStep { get; set; }
        public bool IsYerli { get; set; }
        public string EnstituKod { get; set; }
        public string KullaniciTipAdi { get; set; }
        public int YayinSayisi { get; set; }
        public string DanismanImzaliFormDosyaAdi2 { get; set; }
        public HttpPostedFileBase DanismanImzaliFormDosya { get; set; }
        public List<int> _MezuniyetBasvurulariYayinID { get; set; }
        public List<bool?> _Yayinlanmis { get; set; }
        public List<DateTime?> _MezuniyetYayinTarih { get; set; }
        public List<int> _MezuniyetYayinTurID { get; set; }
        public List<string> _YayinBasligi { get; set; }
        public List<HttpPostedFileBase> _MezuniyetYayinBelgesi { get; set; }
        public List<string> _MezuniyetYayinBelgesiAdi { get; set; }
        public List<string> _MezuniyetYayinKaynakLinki { get; set; }
        public List<HttpPostedFileBase> _YayinMetniBelgesi { get; set; }
        public List<string> _YayinMetniBelgesiAdi { get; set; }
        public List<string> _MezuniyetYayinLinki { get; set; }
        public List<int?> _MezuniyetYayinIndexTurID { get; set; }
        public List<string> _MezuniyetYayinKabulEdilmisMakaleAdi { get; set; }
        public List<HttpPostedFileBase> _MezuniyetYayinKabulEdilmisMakaleBelgesi { get; set; }

        public List<string> _YazarAdi { get; set; }
        public List<string> _DergiAdi { get; set; }
        public List<string> _YilCiltSayiSS { get; set; }
        public List<int?> _MezuniyetYayinProjeTurID { get; set; }
        public List<bool?> _IsProjeTamamlandiOrDevamEdiyor { get; set; }
        public List<string> _ProjeEkibi { get; set; }
        public List<string> _ProjeDeatKurulus { get; set; }
        public List<string> _TarihAraligi { get; set; }
        public List<string> _EtkinlikAdi { get; set; }
        public List<string> _YerBilgisi { get; set; }

        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public string DonemAdi { get; set; }

        public YayinBilgiModel YayinBilgisi { get; set; }

        public List<YayinBilgiModel> MezuniyetBasvuruYayinlari { get; set; }
        public kmMezuniyetBasvuru()
        {
            _MezuniyetBasvurulariYayinID = new List<int>();
            _Yayinlanmis = new List<bool?>();
            _MezuniyetYayinTarih = new List<DateTime?>();
            MezuniyetBasvuruYayinlari = new List<YayinBilgiModel>();
            _MezuniyetYayinTurID = new List<int>();
            _YayinBasligi = new List<string>();
            _MezuniyetYayinBelgesi = new List<HttpPostedFileBase>();
            _MezuniyetYayinBelgesiAdi = new List<string>();
            _MezuniyetYayinKaynakLinki = new List<string>();
            _YayinMetniBelgesi = new List<HttpPostedFileBase>();
            _YayinMetniBelgesiAdi = new List<string>();
            _MezuniyetYayinLinki = new List<string>();
            _MezuniyetYayinIndexTurID = new List<int?>();
            _MezuniyetYayinKabulEdilmisMakaleAdi = new List<string>();
            _MezuniyetYayinKabulEdilmisMakaleBelgesi = new List<HttpPostedFileBase>();

            _YazarAdi = new List<string>();
            _DergiAdi = new List<string>();
            _YilCiltSayiSS = new List<string>();
            _MezuniyetYayinProjeTurID = new List<int?>();
            _IsProjeTamamlandiOrDevamEdiyor = new List<bool?>();
            _ProjeEkibi = new List<string>();
            _ProjeDeatKurulus = new List<string>();
            _TarihAraligi = new List<string>();
            _EtkinlikAdi = new List<string>();
            _YerBilgisi = new List<string>();
        }
    }

    public class fmMezuniyetSureci : PagerOption
    {
        public string EnstituKod { get; set; }
        public IEnumerable<frMezuniyetSureci> Data { get; set; }
    }
    public class frMezuniyetSureci : MezuniyetSureci
    {
        public bool Hesaplandi { get; set; }
        public string EnstituAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public string DonemAdi { get; set; }
        public int OTCount { get; set; }
    }
    public class fmMezuniyetYonetmelikler : PagerOption
    {
        public string EnstituKod { get; set; }
        public int? TarihKriterID { get; set; }
        public IEnumerable<frMezuniyetYonetmelikler> Data { get; set; }
    }
    public class frMezuniyetYonetmelikler : MezuniyetYonetmelikleri
    {
        public string EnstituAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public string TarihKriterAdi { get; set; }
        public string DonemAdi { get; set; }
        public string DonemAdiB { get; set; }

        public List<krMezuniyetYonetmelikOT> MezuniyetYonetmelikData { get; set; }
        public frMezuniyetYonetmelikler()
        {
            MezuniyetYonetmelikData = new List<krMezuniyetYonetmelikOT>();
        }
    }
    public class fmMezuniyetYayinTurleri : PagerOption
    {
        public bool? IsAktif { get; set; }
        public bool Expand { get; set; }
        public string MezuniyetYayinTurAdi { get; set; }
        public IEnumerable<frMezuniyetYayinTurleri> data { get; set; }

    }
    public class frMezuniyetYayinTurleri : MezuniyetYayinTurleri
    {
        public string EnstituKod { get; set; }
        public string EnstituAd { get; set; }
        public string BelgeTurAdi { get; set; }
        public string KaynakLinkTurAdi { get; set; }
        public string MetinTurAdi { get; set; }
        public string YayinLinkTurAdi { get; set; }
        public string IslemYapan { get; set; }




    }
    public class kmMezuniyetSureci : MezuniyetSureci
    {
        public string OgretimYili { get; set; }
        public List<int> gID { get; set; }
        public List<string> MezuniyetSurecOtoMailID { get; set; }
        public List<string> ZamanTipID { get; set; }
        public List<string> Zaman { get; set; }
        public List<string> MailSablonTipID { get; set; }



        public List<int?> MezuniyetSureciOgrenimTipKriterID { get; set; }
        public List<int?> OgrenimTipID { get; set; }
        public List<int?> OgrenimTipKod { get; set; }
        public List<string> MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari { get; set; }
        public List<int?> MBasvuruToplamKrediKriteri { get; set; }
        public List<double?> MBasvuruAGNOKriteri { get; set; }
        public List<int?> MBasvuruAKTSKriteri { get; set; }
        public List<int?> MBSinavUzatmaSuresiGun { get; set; }
        public List<int?> MBTezTeslimSuresiGun { get; set; }
        public List<int?> MBSRTalebiKacGunSonraAlabilir { get; set; }


        public kmMezuniyetSureciOgrenimTipModel OgrenimTipModel { get; set; }
        public kmMezuniyetSureci()
        {
            gID = new List<int>();
            MezuniyetSurecOtoMailID = new List<string>();
            ZamanTipID = new List<string>();
            Zaman = new List<string>();
            MailSablonTipID = new List<string>();
            OgrenimTipModel = new kmMezuniyetSureciOgrenimTipModel();
            MezuniyetSureciOgrenimTipKriterID = new List<int?>();
            OgrenimTipID = new List<int?>();
            OgrenimTipKod = new List<int?>();
            MBasvuruToplamKrediKriteri = new List<int?>();
            MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = new List<string>();
            MBasvuruAGNOKriteri = new List<double?>();
            MBasvuruAKTSKriteri = new List<int?>();
            MBSinavUzatmaSuresiGun = new List<int?>();
            MBTezTeslimSuresiGun = new List<int?>();
            MBSRTalebiKacGunSonraAlabilir = new List<int?>();

        }
    }
    public class kmMezuniyetSureciOgrenimTipModel
    {
        public List<kmMezuniyetSureciOgrenimTipKriterleri> OgrenimTipKriterList { get; set; }
    }
    public class kmMezuniyetSureciOgrenimTipKriterleri : MezuniyetSureciOgrenimTipKriterleri
    {
        public int? SelectedOgrenimTipID { get; set; }
        public bool OrjinalVeri { get; set; }
        public string OgrenimTipAdi { get; set; }
    }

    public class kmMezuniyetYonetmelik : MezuniyetYonetmelikleri
    {
        public string EnstituAdi { get; set; }
        public string OgretimYili { get; set; }
        public string OgretimYiliB { get; set; }
        public string TarihKriterAdi { get; set; }
        public string IslemYapan { get; set; }

        public List<string> _MezuniyetYayinTurID { get; set; }
        public List<string> _IsGecerli { get; set; }
        public List<string> _IsZorunlu { get; set; }
        public List<string> _GrupKodu { get; set; }
        public List<string> _IsVeOrVeya { get; set; }


        public IEnumerable<krMezuniyetYonetmelikOT> krMezuniyetYonetmelikOT { get; set; }
        public kmMezuniyetYonetmelik()
        {
            krMezuniyetYonetmelikOT = new List<krMezuniyetYonetmelikOT>();
            _MezuniyetYayinTurID = new List<string>();
            _IsGecerli = new List<string>();
            _IsZorunlu = new List<string>();
            _GrupKodu = new List<string>();
            _IsVeOrVeya = new List<string>();
        }
    }
    public class krMezuniyetYonetmelikOT : MezuniyetYonetmelikleriOT
    {
        public bool? Success { get; set; }
        public string EnstituKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string MezuniyetYayinTurAdi { get; set; }
    }

    public class OnlineOdemeProgramDetayModel
    {
        public int BasvuruTercihID { get; set; }
        public string EnstituKod { get; set; }
        public int BasvuruSurecID { get; set; }
        public string SurecAdi { get; set; }
        public int BasvuruID { get; set; }
        public int DonemBaslangicYil { get; set; }
        public int DonemID { get; set; }
        public DateTime? SurecBaslangicTarihi { get; set; }
        public DateTime? SurecBitisTarihi { get; set; }
        public string Aciklama { get; set; }
        public string AciklamaSelectedLng { get; set; }
        public int KullaniciID { get; set; }
        public bool YtuOgrencisi { get; set; }
        public bool IsYerliOgrenci { get; set; }
        public string OgrenciNo { get; set; }
        public string TcKimlikNo { get; set; }
        public string AdSoyad { get; set; }
        public string EMail { get; set; }
        public string CepTel { get; set; }
        public Guid UniqueID { get; set; }
        public int AlanTipID { get; set; }
        public int AlanKota { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public int AlesTipID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public bool Ucretli { get; set; }
        public double? Ucret { get; set; }
        public int OdemeDonemNo { get; set; }
        public bool IsDekontOrSanalPos { get; set; }
        public bool IsYokOgrenciKaydiVar { get; set; }
        public bool YokOgrenciKontroluYap { get; set; }
        public double? IstenecekKatkiPayiTutari { get; set; }
        public bool? IsOgrenimUcretiOrKatkiPayi { get; set; }
        public bool YokOgrenciKontrolHataVar { get; set; }
        public bool IsOdemeVar { get; set; }
        public bool IsOdemeIslemiAcik { get; set; }
        public List<string> AktifOgrenimListesi { get; set; }
        public double? OdenecekUcret { get; set; }
        public DateTime? OdemeBaslangicTarihi { get; set; }
        public DateTime? OdemeBitisTarihi { get; set; }
        public bool KayitOldu { get; set; }
        public MulakatSonuclari MulakatSonuclari { get; set; }
        public Basvurular BasvuruBilgi { get; set; }
        public bool IsBelgeYuklemesiVar { get; set; }
        public bool IsKayittaBelgeOnayiZorunlu { get; set; }
        public List<int> IstenenBasvuruBelgeTipID { get; set; }
        public List<BasvurularYuklenenBelgeler> BasvurularYuklenenBelgeler { get; set; }
        public bool IsBelgeDialogYuklemeShow { get; set; }
        public bool IsBelgeDialogYuklemeClose { get; set; }


        public List<BasvurularTercihleriKayitOdemeleri> OdemeListesi { get; set; }
        public List<string> BelgeKontrolMessages { get; set; }
        public OnlineOdemeProgramDetayModel()
        {
            BelgeKontrolMessages = new List<string>();
            IstenenBasvuruBelgeTipID = new List<int>();
            AktifOgrenimListesi = new List<string>();
            OdemeListesi = new List<BasvurularTercihleriKayitOdemeleri>();
        }
    }


    public class MezuniyetJuriOneriFormuModel : MezuniyetJuriOneriFormlari
    {
        public bool IsDoktoraOrYL { get; set; }
        public string OgrenciAnabilimdaliProgramAdi { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public bool IsTezDiliTr { get; set; }
        public string TezBaslikTr { get; set; }
        public string TezBaslikEn { get; set; }
        public Kullanicilar Danisman { get; set; }
        public List<string> AnaTabAdi { get; set; }
        public List<string> DetayTabAdi { get; set; }
        public List<string> JuriTipAdi { get; set; }
        public List<string> AdSoyad { get; set; }
        public List<string> UnvanAdi { get; set; }
        public List<string> EMail { get; set; }
        public List<int?> UniversiteID { get; set; }
        public List<string> AnabilimdaliProgramAdi { get; set; }
        public List<string> UzmanlikAlani { get; set; }
        public List<string> BilimselCalismalarAnahtarSozcukler { get; set; }
        public List<string> DilSinavAdi { get; set; }
        public List<string> DilPuani { get; set; }

        public SelectList SListUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
        public List<KrMezuniyetJuriOneriFormuJurileri> JoFormJuriList { get; set; }


        public MezuniyetJuriOneriFormuModel()
        {
           AnaTabAdi = new List<string>();
            DetayTabAdi = new List<string>();
            JuriTipAdi = new List<string>();
            AdSoyad = new List<string>();
            UnvanAdi = new List<string>();
            EMail = new List<string>();
            UniversiteID = new List<int?>();
            AnabilimdaliProgramAdi = new List<string>();
            UzmanlikAlani = new List<string>();
            BilimselCalismalarAnahtarSozcukler = new List<string>();
            DilSinavAdi = new List<string>();
            DilPuani = new List<string>();
            JoFormJuriList = new List<KrMezuniyetJuriOneriFormuJurileri>();
        }
    }

    public class KrMezuniyetJuriOneriFormuJurileri : MezuniyetJuriOneriFormuJurileri
    {

        public SelectList SlistUnvanAdi { get; set; }
        public SelectList SListUniversiteID { get; set; }
    }
    public class SablonMailModel
    {
        public int MailSablonTipID { get; set; }
        public MailSablonlari Sablon { get; set; }
        public List<string> SablonParametreleri { get; set; }
        public List<MailSendList> EMails { get; set; }
        public List<System.Net.Mail.Attachment> Attachments { get; set; }
        public Guid? UniqueID { get; set; }
        public string AdSoyad { get; set; }
        public string UniversiteAdi { get; set; }
        public string UnvanAdi { get; set; }
        public string ProgramAdi { get; set; }
        public bool IsAsilOrYedek { get; set; }
        public string JuriTipAdi { get; set; }
        public int? MezuniyetJuriOneriFormuJuriID { get; set; }
        public int? TIBasvuruAraRaporKomiteID { get; set; }

        public SablonMailModel()
        {
            EMails = new List<MailSendList>();
            SablonParametreleri = new List<string>();
            Attachments = new List<System.Net.Mail.Attachment>();
        }
    }
    public class ExportAttachPdfModel
    {
        public int RaporTipID { get; set; }
        public int DataID { get; set; }

    }

    public class Table
    {
        public string BOLUMADI { get; set; }
        public string ADSOYAD { get; set; }
        public string AKADEMIKUNVAN { get; set; }
        public string KADROUNVAN { get; set; }
        public string KURUMMAIL { get; set; }
    }

    public class PersisWsDataModel
    {
        public List<Table> Table { get; set; }
    }

    public class RprTutanakModel
    {
        public bool IsDoktoraOrYL { get; set; }
        public string TutanakAdi { get; set; }
        public string Sayi { get; set; }
        public string Aciklama { get; set; }

        public List<RprTutanakRowModel> DetayData { get; set; }
        public RprTutanakModel()
        {
            DetayData = new List<RprTutanakRowModel>();
        }
    }
    public class RprTutanakRowModel
    {
        public string OgrenciBilgi { get; set; }
        public string DanismanAdSoyad { get; set; }
        public string DanismanUni { get; set; }
        public string TikUyesi { get; set; }
        public string TikUyesiUni { get; set; }
        public string TikUyesi2 { get; set; }
        public string TikUyesi2Uni { get; set; }
        public string AsilUye { get; set; }
        public string AsilUyeUni { get; set; }
        public string AsilUye2 { get; set; }
        public string AsilUye2Uni { get; set; }
        public string YedekUye { get; set; }
        public string YedekUyeUni { get; set; }
        public string YedekUye2 { get; set; }
        public string YedekUye2Uni { get; set; }
        public string TezKonusu { get; set; }

    }

    public class RprMezuniyetTutanakModel
    {
        public string Konu { get; set; }
        public string Aciklama1 { get; set; }
        public string Aciklama2 { get; set; }

        public List<RprMezuniyetTutanakRowModel> Data { get; set; }

        public RprMezuniyetTutanakModel()
        {
            Data = new List<RprMezuniyetTutanakRowModel>();
        }
    }
    public class RprMezuniyetTutanakRowModel
    {
        public string OgrenciBilgi { get; set; }
        public string DanismanAdSoyad { get; set; }
        public string TezKonusu { get; set; }
        public string SavunmaTarihi { get; set; }
        public string TezTeslimTarihi { get; set; }
    }

    public class MmMessage
    {
        public bool IsDialog { get; set; }
        public string DialogID { get; set; }
        public bool IsCloseDialog { get; set; }
        public bool IsSuccess { get; set; }
        public Msgtype MessageType { get; set; }

        public string Title { get; set; }
        public string ReturnUrl { get; set; }
        public int ReturnUrlTimeOut { get; set; }
        public int SiraNo { get; set; }
        public List<string> Messages { get; set; }
        public List<MrMessage> MessagesDialog { get; set; }
        public MmMessage()
        {
            MessageType = Msgtype.Nothing;
            Messages = new List<string>();
            MessagesDialog = new List<MrMessage>();
            ReturnUrlTimeOut = 400;
        }

    }
    public class MrMesajBilgi
    {

        public int MesajlarID { get; set; }
        public int KullaniciID { get; set; }
        public DateTime Tarih { get; set; }
        public string Konu { get; set; }
        public string Aciklama { get; set; }

    }
    public class MesajBilgi
    {
        public int Count { get; set; }
        public List<MrMesajBilgi> Mesajlar { get; set; }
    }

    public class BilgiTipleri
    {
        public List<BilgiRow> BilgiTip { get; set; }
        public BilgiTipleri()
        {

            var dct = new List<BilgiRow>();
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.Hata, BilgiTipAdi = "Hata", BilgiTipCls = "primary" });
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.Uyarı, BilgiTipAdi = "Uyarı", BilgiTipCls = "warning" });
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.Kritik, BilgiTipAdi = "Kritik Durum", BilgiTipCls = "danger" });
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.OnemsizHata, BilgiTipAdi = "Önemsiz Hata", BilgiTipCls = "default" });
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.Saldırı, BilgiTipAdi = "Saldırı", BilgiTipCls = "danger" });
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.LoginHatalari, BilgiTipAdi = "loginHatalari", BilgiTipCls = "info" });
            dct.Add(new BilgiRow { BilgiTipID = BilgiTipi.Bilgi, BilgiTipAdi = "Bilgi", BilgiTipCls = "success" });
            BilgiTip = dct;



        }


    }
    public class BilgiRow
    {
        public int BilgiTipID { get; set; }
        public string BilgiTipAdi { get; set; }
        public string BilgiTipCls { get; set; }
    }

    public class AjaxLoginModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
        public string ReturnUrl { get; set; }
        public string NewGuid { get; set; }
        public string NewSrc { get; set; }
    }
    public class MIndexBilgi
    {
        public int Toplam { get; set; }
        public int Aktif { get; set; }
        public int Pasif { get; set; }
        public List<mxRowModel> ListB { get; set; }
        public MIndexBilgi()
        {
            ListB = new List<mxRowModel>();
        }
    }
    public class mxRowModel
    {
        public int ID { get; set; }
        public string Key { get; set; }
        public string ClassName { get; set; }
        public string Color { get; set; }
        public int Toplam { get; set; }
        public int KayitOlan { get; set; }

    }

    public class kulaniciProgramYetkiModel
    {
        public int KullaniciProgramID { get; set; }
        public bool YetkiVar { get; set; }
        public string EnstituKisaAd { get; set; }
        public string EnstituKod { get; set; }
        public string EnstituAdi { get; set; }

        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }

    }



    public class ekAciklamaContent
    {
        public string Baslik { get; set; }
        public List<CmbStringDto> Detay { get; set; }
        public ekAciklamaContent()
        {
            Detay = new List<CmbStringDto>();
        }
    }

    public class MrMessage
    {

        public string DialogID { get; set; }
        public bool IsSucces { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string PropertyName { get; set; }
        public bool AddIcon { get; set; }
        public string HtmlData { get; set; }
        public List<int> ReturnIds { get; set; }
        public Msgtype MessageType { get; set; }
        public MrMessage()
        {
            AddIcon = true;
            MessageType = Msgtype.Nothing;
        }
    }


    public class TikAraRaporJuriBilgi
    {

    }
    public class YokStudentControl
    {
        public bool KayitVar { get; set; }
        public bool Hata { get; set; }
        public string Mesaj { get; set; }
        public List<string> AktifOgrenimListesi { get; set; }
        public YokStudentControl()
        {
            AktifOgrenimListesi = new List<string>();
        }
    }
    public class UrlInfoModel
    {
        public string Root { get; set; }
        public string FakeRoot { get; set; }
        public string DefaultUri { get; set; }
        public string AbsolutePath { get; set; }
        public string EnstituKisaAd { get; set; }
        public string LastPath { get; set; }
        public string Query { get; set; }
    }

    public class BasvuruBilgiModel
    {
        public bool SistemBasvuruyaAcik { get; set; }
        public bool SistemGirisSinavBilgiAcik { get; set; }
        public int? AktifSurecID { get; set; }
        public bool YtuOgrencisi { get; set; }
        public Enstituler Enstitü { get; set; }
        public BasvuruSurec BasvuruSurec { get; set; }
        public MezuniyetSureci MezuniyetSurec { get; set; }
        public Kullanicilar Kullanici { get; set; }
        public string DonemAdi { get; set; }

        public string OgrenimTipAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string OgrenciNo { get; set; }
        public string KayitDonemi { get; set; }

        public string BirimAdi { get; set; }
        public string UnvanAdi { get; set; }
        public string SicilNo { get; set; }

        public bool EnstituYetki { get; set; }
        public bool KullaniciTipYetki { get; set; }
        public string KullaniciTipYetkiYokMsj { get; set; }

    }
    public class YayinBilgiModel : MezuniyetBasvurulariYayin
    {
        public int? ShowDetayYayinID { get; set; }
        public bool DegerlendirmeAktif { get; set; }
        public bool DegerlendirmeKolonu { get; set; }
        public bool sonucGirisSureciAktif { get; set; }
        public int? RowNum { get; set; }
        public bool IsDataShow { get; set; }
        public string guID { get; set; }
        public int MezuniyetSurecID { get; set; }
        public string MezuniyetYayinTurAdi { get; set; }
        public bool MezuniyetYayinTarihZorunlu { get; set; }
        public string MezuniyetYayinBelgeTurAdi { get; set; }
        public bool MezuniyetYayinBelgeTurZorunlu { get; set; }
        public string MezuniyetYayinKaynakLinkTurAdi { get; set; }
        public bool MezuniyetYayinKaynakLinkIsUrl { get; set; }
        public bool MezuniyetYayinKaynakLinkTurZorunlu { get; set; }
        public string MezuniyetYayinMetinTurAdi { get; set; }
        public bool MezuniyetYayinMetinZorunlu { get; set; }
        public string MezuniyetYayinLinkTurAdi { get; set; }
        public bool MezuniyetYayinLinkiZorunlu { get; set; }
        public bool MezuniyetYayinLinkIsUrl { get; set; }
        public bool MezuniyetYayinIndexTurZorunlu { get; set; }
        public string MezuniyetYayinIndexTurAdi { get; set; }
        public bool MezuniyetKabulEdilmisMakaleZorunlu { get; set; }

        public bool YayinYazarlarIstensin { get; set; }
        public bool YayinDergiAdiIstensin { get; set; }
        public bool YayinYilCiltSayiIstensin { get; set; }
        public bool YayinProjeTurIstensin { get; set; }
        public bool YayinProjeEkibiIstensin { get; set; }
        public bool YayinMevcutDurumIstensin { get; set; }
        public bool YayinDeatKurulusIstensin { get; set; }
        public bool IsTarihAraligiIstensin { get; set; }
        public bool YayinEtkinlikAdiIstensin { get; set; }
        public bool YayinYerBilgisiIstensin { get; set; }

        public string ProjeTurAdi { get; set; }

        public List<MezuniyetYayinIndexTurleri> YayinIndexTurleri { get; set; }

        public List<MezuniyetYayinProjeTurleri> MezuniyetYayinProjeTurleris { get; set; }


        public YayinBilgiModel()
        {
            guID = Guid.NewGuid().ToString().Substring(0, 8);
            YayinIndexTurleri = new List<Models.MezuniyetYayinIndexTurleri>();
            MezuniyetYayinProjeTurleris = new List<Models.MezuniyetYayinProjeTurleri>();
        }

    }
    public class MulakatBilgiModel : BasvuruBilgiModel
    {
        public List<string> Programlar { get; set; }
        //public basvuru AktifBasvuru { get; set; }
        public MulakatBilgiModel()
        {
            Programlar = new List<string>();
        }
    }
    public class SinavBilgiModel : BasvuruSurecSinavTipleri
    {
        public List<SinavDilleri> BsSinavDilleri { get; set; }
        public string SinavDilleriStr { get; set; }
        public List<krSinavTipleriDonems> SinavTipleriDonems { get; set; }
        public string SinavAdi { get; set; }
        public BasvurularSinavBilgi BasvuruSinavData { get; set; }
        public bool IsTurkceProgramVar { get; set; }
        public bool IsEgitimDiliTurkce { get; set; }
        public string MinNotAdi { get; set; }
        public string MaxNotAdi { get; set; }
        public SinavBilgiModel()
        {
            BsSinavDilleri = new List<SinavDilleri>();
            SinavTipleriDonems = new List<krSinavTipleriDonems>();
        }

    }

    public class SinavSonucBilgiModel
    {
        public string EnstituKod { get; set; }
        public int SinavTipGrupID { get; set; }
        public int WsSinavCekimTipID { get; set; }
        public int? AlesTipID { get; set; }
        public int Durum { get; set; }
        public string SinavKodu { get; set; }
        public string SinavAdi { get; set; }
        public string TCKimlikNo { get; set; }
        public string SinavYili { get; set; }
        public string SinavDonemi { get; set; }
        public DateTime? AciklanmaTarihi { get; set; }
        public Double Puan { get; set; }
        public string WsXmlData { get; set; }
        public int? SinavDilID { get; set; }
        public SinavSonucDilXmlModel jSonValDilSinavi { get; set; }
        public SinavSonucAlesXmlModel jSonValAlesSinavi { get; set; }
        public List<int> SecilenAlesTipleri { get; set; }
        public int? WsSonucID { get; set; }
        public List<CmbIntDto> WsSinavSonucList { get; set; }
        public bool IsSinavSonucuVar { get; set; }
        public bool ShowIsTaahhutVar { get; set; }
        public bool IsTaahhutVar { get; set; }
        public string Aciklama { get; set; }
        public string MinNotAdi { get; set; }
        public SinavSonucBilgiModel()
        {
            WsSinavSonucList = new List<CmbIntDto>();
        }
    }
    public class SinavSonucDilXmlModel
    {
        public string TCK { get; set; }
        public string AD { get; set; }
        public string SOYAD { get; set; }
        public string ENGELDURUM { get; set; }
        public string DIL { get; set; }
        public string DILADI { get; set; }
        public string DIL_ADI { get; set; }
        public string DOGRU_SAY { get; set; }
        public string YANLIS_SAY { get; set; }
        public string PUAN { get; set; }
        public string DUZEY { get; set; }
        public string ASAYISI { get; set; }
        public string BSAYISI { get; set; }
        public string CSAYISI { get; set; }
        public string DSAYISI { get; set; }
        public string ESAYISI { get; set; }
        public string FSAYISI { get; set; }
        public string TOPLAMSAYI { get; set; }
        public object KURUMKOD { get; set; }
        public object KURUMAD { get; set; }
        public object YERKOD { get; set; }
        public object ILAD { get; set; }
        public object ILCEAD { get; set; }
        public string SGK { get; set; }

    }
    public class SinavSonucAlesXmlModel
    {
        public string TCK { get; set; }
        public string AD { get; set; }
        public string SOYAD { get; set; }
        public string ENGELDURUM { get; set; }
        public string SAY1_DOGRU { get; set; }
        public string SAY1_YANLIS { get; set; }
        public string SAY2_DOGRU { get; set; }
        public string SAY2_YANLIS { get; set; }
        public string SOZ1_DOGRU { get; set; }
        public string SOZ2_DOGRU { get; set; }
        public string SAY_PUAN { get; set; }
        public string SAY_BASARI { get; set; }
        public string SAY_TOPLAM_BASARI { get; set; }
        public string SOZ_PUAN { get; set; }
        public string SOZ_BASARI { get; set; }
        public string SOZ_TOPLAM_BASARI { get; set; }
        public string EA_PUAN { get; set; }
        public string EA_BASARI { get; set; }
        public string EA_TOPLAM_BASARI { get; set; }
        public string SGK { get; set; }

    }

    public class kontenjanBilgiModel
    {
        public string EnstituAdi { get; set; }
        public string EnstituKod { get; set; }
        public int OgrenimTipKod { get; set; }
        public bool GrupGoster { get; set; }
        public string GrupKodu { get; set; }
        public bool LEgitimBilgisiIste { get; set; }
        public bool YLEgitimBilgisiIste { get; set; }
        public int BasvuruSurecKontrolTipID { get; set; }
        public bool FarkliOgrenimTipiEklenemez { get; set; }
        public string FarkliOgrenimTipEklenemezAds { get; set; }
        public bool FarkliOgrenimTipiEklenemezAyniBasvuruda { get; set; }
        public bool IsAktif { get; set; }
        public string GrupAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public int KontenjanBulunanBolumSayisi { get; set; }
        public int KontenjanBulunanProgramSayisi { get; set; }
        public int ToplamKota { get; set; }
        public int ToplamKalanKota { get; set; }
        public int Kota { get; set; }
        public int KalanKota { get; set; }
        public List<kontenjanBilgiModel> OBOgrenimTipleri { get; set; }

    }
    public class kontenjanProgramBilgiModel : Programlar
    {
        public int BasvuruSurecID { get; set; }
        public int BasvuruID { get; set; }
        public int KullaniciID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string AlesTipAdi { get; set; }
        public int AlanTipID { get; set; }
        public bool OrtakKota { get; set; }
        public int? OrtakKotaSayisi { get; set; }
        public int AlanIciKota { get; set; }
        public int AlanDisiKota { get; set; }
        public double? MinAles { get; set; }
        public double? MinAgno { get; set; }
        public string MinAgnoAciklama { get; set; }

        public bool Kazandi { get; set; }
        public bool KayitEdildi { get; set; }

        public string UniqueID { get; set; }

        public kontenjanProgramBilgiModel()
        {
            Kazandi = false;
            KayitEdildi = false;
        }
    }
    public class TercihRowModel
    {
        public bool IsNewRow { get; set; }
        public string UniqueID { get; set; }
        public int SiraNo { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public int AlesTipID { get; set; }
        public string AlesTipAdi { get; set; }
        public bool Ingilizce { get; set; }
        public string MinAgnoAciklama { get; set; }
        public int AlanTipID { get; set; }
        public string AlanTipAdi { get; set; }
        public bool OrtakKota { get; set; }
        public int Kota { get; set; }
        public bool YLEgitimBilgisiIste { get; set; }
        public string MezuniyetBelgesiYolu { get; set; }
        public string MezuniyetBelgesiAdi { get; set; }
    }
    public class programKullaniciModel
    {
        public string KullaniciTipAdi { get; set; }
        public string ProgramKod { get; set; }
        public int KullaniciTipID { get; set; }
        public bool Checked { get; set; }

    }
    public class TarihAralikModel
    {
        public int BaslangicYil { get; set; }
        public int BitisYil { get { return (this.BaslangicYil > 0 ? this.BaslangicYil + 1 : 0); } }
        public int DonemID { get; set; }
        public string DonemAdi { get; set; }
        public string DonemAdiEn { get; set; }
        public string DonemAdiLong { get { return (this.BaslangicYil + " - " + (this.BaslangicYil + 1) + " " + this.DonemAdi); } }
        public string DonemAdiEnLong { get { return (this.BaslangicYil + " - " + (this.BaslangicYil + 1) + " " + this.DonemAdiEn); } }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
    }
    public class EOyilBilgi
    {

        public int BaslangicYili { get; set; }
        public int BitisYili { get; set; }

        public int Donem { get; set; }
        public string DonemAdi { get; set; }
    }
    //public class CmbStringDto
    //{
    //    public string Value { get; set; }
    //    public string Caption { get; set; }
    //}
    //public class CmbBoolDto
    //{
    //    public bool? Value { get; set; }
    //    public string Caption { get; set; }
    //}
    //public class CmbBoolDatetimeDto
    //{
    //    public bool? Value { get; set; }
    //    public DateTime? Caption { get; set; }
    //}
    //public class CmbIntDto
    //{
    //    public int? Value { get; set; }
    //    public string Caption { get; set; }
    //}
    //public class CmbDoubleDto
    //{
    //    public double? Value { get; set; }
    //    public string Caption { get; set; }
    //}
    //public class CmbMultyTypeDto
    //{
    //    public int Inx { get; set; }
    //    public int Key { get; set; }
    //    public int Value { get; set; }
    //    public string ValueS { get; set; }
    //    public string ValueS2 { get; set; }
    //    public string ValueS3 { get; set; }
    //    public bool ValueB { get; set; }
    //    public bool ValueB2 { get; set; }
    //    public double ValueDouble { get; set; }
    //    public double ValueDouble2 { get; set; }
    //}
    //public class PagerIndexDto
    //{
    //    public int StartRowIndex { get; set; }
    //    public int PageIndex { get; set; }
    //}

    public class KmDekontBilgi
    {
        public int KullaniciID { get; set; }
        public string UniqueID { get; set; }
        public string EnstituKod { get; set; }
        public string EnstituAdi { get; set; }
        public string AdSoyad { get; set; }
        public int KullaniciTipID { get; set; }
        public string TcPasaportNo { get; set; }
        public bool KullaniciAktif { get; set; }
        public bool GirisAktif { get; set; }

        public kontenjanProgramBilgiModel ProgramBilgi { get; set; }

    }

    public class BasvuruDetayModel : Basvurular
    {
        public int SelectedTabIndex { get; set; }

        public bool IsYerli { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string ResimYolu { get; set; }
        public string AdSoyad { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public string BasvuruDurumAdi { get; set; }

        public string CinsiyetAdi { get; set; }
        public string UyrukAdi { get; set; }
        public string DogumYeriAdi { get; set; }
        public string NufusIlIlceAdi { get; set; }
        public string YasadigiSehirAdi { get; set; }


        public string LUniversiteAdi { get; set; }
        public string LBolumAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string LNotSistemi { get; set; }
        public string LegitimDilAdi { get; set; }
        public string YLUniversiteAdi { get; set; }
        public string YLBolumAdi { get; set; }
        public string YLNotSistemi { get; set; }
        public string YLegitimDilAdi { get; set; }
        public string DRUniversiteAdi { get; set; }
        public string DRBolumAdi { get; set; }
        public string DRNotSistemi { get; set; }
        public string DRegitimDilAdi { get; set; }

        public List<FrTercihler> Tercihlers { get; set; }
        public bool IsSecilenTercihVarAsil { get; set; }
        public bool IsSecilenTercihVarYedek { get; set; }
        public bool IsTurkceProgramVar { get; set; }
        public bool IsBelgeYuklemeVar { get; set; }
        public List<FrSinavlar> Sinavlars { get; set; }

        public bool IsGonderilenMaillerVar { get; set; }
        public bool IsHesaplandi { get; set; }
        public bool IsKayitHakkiVar { get; set; }
        public bool IsBelgeYuklemeAktif { get; set; }
        public bool IsYedekCokluTercih { get; set; }

        public List<BasvuruBelgeModel> Belgelers { get; set; }

        public bool KayitIslemiGordu { get; set; }
        public bool IsSave { get; set; }
        public bool IsKayittaBelgeOnayiZorunlu { get; set; }

        public YokStudentControl YokStudentControl { get; set; }
        public BasvuruDetayModel()
        {
            Tercihlers = new List<FrTercihler>();
            Sinavlars = new List<FrSinavlar>();
            Belgelers = new List<BasvuruBelgeModel>();
        }
    }
    public class EntBegeKayitT
    {
        public string EnstituKod { get; set; }
        public int OgrenimTipKod { get; set; }
        public int MulakatSonucTipID { get; set; }
        public DateTime BaslangicTar { get; set; }
        public DateTime BitisTar { get; set; }
    }
    public class FrTercihler : BasvurularTercihleri
    {
        public bool IsSeciliBasvuruyaAitTercih { get; set; }
        public int MulakatSonucID { get; set; }
        public int MulakaSonucTipID { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public bool Ingilizce { get; set; }
        public string ProgramAdi { get; set; }
        public string EgitimDilAdi { get; set; }
        public string AlesTipAdi { get; set; }
        public string AlanTipAdi { get; set; }
        public int Kota { get; set; }
        public int? KayitDurumID { get; set; }
        public bool? KayıtOldu { get; set; }
        public bool IsBelgeYuklemeAktif { get; set; }
    }
    public class FrSinavlar : SinavTipleri
    {
        public string EnstituKod { get; set; }
        public bool IsWebService { get; set; }
        public int SinavTipGrupID { get; set; }
        public int? SinavDilID { get; set; }
        public string SinavDilAdi { get; set; }
        public bool IsTaahhutVar { get; set; }
        public bool GIsTaahhutVar { get; set; }
        public string GrupAdi { get; set; }
        public int? TarihGirisMaxGecmisYil { get; set; }
        public int? Yil { get; set; }
        public string DonemAdi { get; set; }
        public DateTime? SinavTarihi { get; set; }
        public double? SinavSubPuani { get; set; }
        public double SinavPuani { get; set; }
        public SinavSonucAlesXmlModel AlesXmlModel { get; set; }
    }

    public class RprTDOEYKModel
    {
        public string OgrenciNo { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public string OgrenciAnabilimdaliProgram { get; set; }
        public string YL_DR { get; set; }
        public string DanismanAdSoyad { get; set; }
        public string DanismanAnabilimDali { get; set; }
        public string TezBaslikTr { get; set; }
        public string TezBaslikEn { get; set; }
        public string TezDili { get; set; }
        public int DanismanYukYlDrSayi { get; set; }
        public int MezunSayisi { get; set; }
    }

}