using BiskaUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.MenuAndRoles
{
    public class SystemMenu : IMenu
    {
        //[MenuAttribute(MenuID = 60000, MenuAdi = "İntihal Kontrol", MenuCssClass = "fa fa-file-text-o", MenuUrl = "", DilCeviriYap = true, YetkisizErisim = true, SiraNo = 1)]
        //public const string IntihalKontrol = "İntihal Kontrol";


        [MenuAttribute(MenuID = 65000, MenuAdi = "Salon Rezervasyonu", MenuCssClass = "fa fa-file-text-o", MenuUrl = "", DilCeviriYap = true, YetkisizErisim = false, YetkiliEnstituler = "010,020", SiraNo = 3)]
        public const string SalonRezervasyonIslemleri = "Salon Rezervasyon İşlemleri";

        [MenuAttribute(MenuID = 75000, MenuAdi = "Belge Talep", MenuCssClass = "fa fa-file-text-o", MenuUrl = "", DilCeviriYap = true, YetkisizErisim = true, YetkiliEnstituler = "010", SiraNo = 5)]
        public const string BelgeTalepIslemleri = "Belge Talep";

        [MenuAttribute(MenuID = 77000, MenuAdi = "Talep İşlemleri", MenuCssClass = "fa fa-file-text-o", MenuUrl = "", DilCeviriYap = true, YetkisizErisim = true, YetkiliEnstituler = "010,020", SiraNo = 6)]
        public const string TalepIslemleri = "Talep İşlemleri";

        [MenuAttribute(MenuID = 80000, MenuAdi = "Lisansüstü Başvuru", MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", DilCeviriYap = true, YetkisizErisim = true, YetkiliEnstituler = "010,020", SiraNo = 7)]
        public const string BasvuruIslemleri = "Başvuru İşlemleri";

        [MenuAttribute(MenuID = 80500, MenuAdi = "YTU Yeni Mezun Başvuru", MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", DilCeviriYap = true, YetkisizErisim = true, YetkiliEnstituler = "010,020", SiraNo = 8)]
        public const string YydBasvuruIslemleri = "YYD Başvuru İşlemleri";

        [MenuAttribute(MenuID = 81000, MenuAdi = "Yatay Geçiş Başvuru", MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", DilCeviriYap = true, YetkisizErisim = true, YetkiliEnstituler = "010,020", SiraNo = 9)]
        public const string YgBasvuruIslemleri = "YG Başvuru İşlemleri";

        [MenuAttribute(MenuID = 82300, MenuAdi = "Tez Danışmanı Öneri", MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", DilCeviriYap = true, YetkisizErisim = true, YetkiliEnstituler = "010,020", SiraNo = 11)]
        public const string TdoIslemleri = "Tez danışmanı öneri İşlemleri";

        [MenuAttribute(MenuID = 83300, MenuAdi = "Tez İzleme İşlemleri", MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", DilCeviriYap = true, YetkisizErisim = true, YetkiliEnstituler = "010,020", SiraNo = 14)]
        public const string TiIslemleri = "Tez İzleme İşlemleri";

        [MenuAttribute(MenuID = 83500, MenuAdi = "Mezuniyet İşlemleri", MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", DilCeviriYap = true, YetkisizErisim = true, YetkiliEnstituler = "010,020", SiraNo = 16)]
        public const string MezuniyetIslemleri = "Mezuniyet İşlemleri";

        [MenuAttribute(MenuID = 84000, MenuAdi = "Rapor İşlemleri", MenuCssClass = "fa fa-bar-chart-o", MenuUrl = "", DilCeviriYap = false, YetkiliEnstituler = "010,020", SiraNo = 18)]
        public const string RaporIslemleri = "RaporIslemleri";

        [MenuAttribute(MenuID = 85000, MenuAdi = "Kullanıcı İşlemleri", MenuCssClass = "fa fa-group", MenuUrl = "", DilCeviriYap = false, YetkiliEnstituler = "010,020", SiraNo = 21)]
        public const string KullaniciIslemleri = "Kullanıcı İşlemleri";

        [MenuAttribute(MenuID = 90000, MenuAdi = "Tanımlamalar", MenuCssClass = "fa fa-gears", MenuUrl = "", DilCeviriYap = false, YetkiliEnstituler = "010,020", SiraNo = 24)]
        public const string Tanimlamalar = "Tanımlamalar";

        [MenuAttribute(MenuID = 100000, MenuAdi = "Sistem", MenuCssClass = "fa fa-desktop", MenuUrl = "", DilCeviriYap = false, YetkiliEnstituler = "010,020", SiraNo = 27)]
        public const string Sistem = "Sistem";

    }
    public class RoleNames : IRoleName, IMenu
    {

        [MenuAttribute(BagliMenuID = 65000, MenuAdi = "Rezervasyon Yap", MenuCssClass = "fa fa-file-text-o", MenuUrl = "SR/Index", DilCeviriYap = true, YetkisizErisim = false, SiraNo = 1)]
        [RoleAttribute(GorunurAdi = "Rezervasyon Yap", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrYap = "Rezervasyon Yap";
        [MenuAttribute(BagliMenuID = 65000, MenuAdi = "Gelen Rezervasyonlar", MenuCssClass = "fa fa-file-text-o", MenuUrl = "SRGelenTalepler/Index", DilCeviriYap = false, SiraNo = 2)]
        [RoleAttribute(GorunurAdi = "Gelen Rezervasyon Talepleri", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrGelenTalepler = "Gelen Rezervasyon Talepleri";
        [RoleAttribute(GorunurAdi = "Rezervasyon Talebi Düzelt", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrTalepDuzelt = "Rezervasyon Talebi Düzelt";
        [RoleAttribute(GorunurAdi = "Rezervasyon Talebi Sil", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrTalepSil = "Rezervasyon Talebi Sil";

        [MenuAttribute(BagliMenuID = 65000, MenuAdi = "Özel Tanımlar", MenuCssClass = "fa fa-list-alt", MenuUrl = "SROzelTanimlar/Index", DilCeviriYap = false, SiraNo = 3)]
        [RoleAttribute(GorunurAdi = "Özel Tanımlar", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrOzelTanimlar = "Özel Tanımlar";

        [MenuAttribute(BagliMenuID = 65000, MenuAdi = "Salon Bilgi", MenuCssClass = "fa fa-list-alt", MenuUrl = "SRSalonBilgi/Index", DilCeviriYap = false, SiraNo = 4)]
        [RoleAttribute(GorunurAdi = "Salon Bilgi", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrSalonBilgi = "Salon Bilgi";


        [MenuAttribute(BagliMenuID = 65000, MenuAdi = "Salon Tanımları", MenuCssClass = "fa fa-list-alt", MenuUrl = "SRSalonlar/Index", DilCeviriYap = false, SiraNo = 5)]
        [RoleAttribute(GorunurAdi = "Salon Tanımları", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrSalonlar = "Salon Rezervasyon Salonlar";

        [MenuAttribute(BagliMenuID = 65000, MenuAdi = "Talep Tipleri", MenuCssClass = "fa fa-list-alt", MenuUrl = "SRTalepTipleri/Index", DilCeviriYap = false, SiraNo = 6)]
        [RoleAttribute(GorunurAdi = "Talep Tipleri", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrTalepTipleri = "Talep Tipleri";

        [RoleAttribute(GorunurAdi = "Salon Tanımları Kayıt", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrSalonlarKayıt = "Salon Tanımları Kayıt";
        [RoleAttribute(GorunurAdi = "Salon Tanımları Sil", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrSalonlarSil = "Salon Tanımları Sil";

        [MenuAttribute(BagliMenuID = 65000, MenuAdi = "Salon Rezervasyon Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "SRAyarlar/Index", DilCeviriYap = false, SiraNo = 7)]
        [RoleAttribute(GorunurAdi = "Salon Rez. Ayarları", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrAyarları = "Salon Rezervasyon Ayarları";


        [MenuAttribute(BagliMenuID = 75000, MenuAdi = "Belge Talebi Yap", MenuCssClass = "fa fa-file-text-o", MenuUrl = "BelgeTalebi/Index", DilCeviriYap = true, YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        //[RoleAttribute(GorunurAdi = "Belge Talebi Yap", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTalebiYap = "Belge Talebi Yap";
        [MenuAttribute(BagliMenuID = 75000, MenuAdi = "Gelen Belge Talepleri", MenuCssClass = "fa fa-file-text-o", MenuUrl = "GelenBelgeTalepleri/Index", DilCeviriYap = false, SiraNo = 2)]
        [RoleAttribute(GorunurAdi = "Gelen Belge Talepleri", Kategori = "Belge Talep", Aciklama = "")]
        public const string GelenBelgeTalepleri = "Gelen Belge Talepleri";
        [RoleAttribute(GorunurAdi = "Belge Talebi Düzelt", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTalebiDuzelt = "Belge Talebi Düzelt";
        [RoleAttribute(GorunurAdi = "Belge Talebi Sil", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTalebiSil = "Belge Talebi Sil";

        [MenuAttribute(BagliMenuID = 75000, MenuAdi = "Belge Tip Detay", MenuCssClass = "fa fa-gear", MenuUrl = "BelgeTipDetay/Index", DilCeviriYap = false, SiraNo = 3)]
        [RoleAttribute(GorunurAdi = "Belge Tip Detay", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTipDetay = "Belge Tip Detay";
        [RoleAttribute(GorunurAdi = "Belge Tip Detay Kayıt", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTipDetayKayıt = "Belge Tip Detay Kayıt";
        [RoleAttribute(GorunurAdi = "Belge Tip Detay Sil", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTipDetaySil = "Belge Tip Detay Sil";

        [MenuAttribute(BagliMenuID = 75000, MenuAdi = "Belge Tipleri", MenuCssClass = "fa fa-list-alt", MenuUrl = "BelgeTipleri/Index", DilCeviriYap = false, SiraNo = 4)]
        [RoleAttribute(GorunurAdi = "Belge Tipleri", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTipleri = "Belge Tipleri";
        [RoleAttribute(GorunurAdi = "Belge Tipleri Kayıt", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTipleriKayıt = "Belge Tipleri Kayıt";
        [RoleAttribute(GorunurAdi = "Belge Tipleri Sil", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTipleriSil = "Belge Tipleri Sil";

        [MenuAttribute(BagliMenuID = 75000, MenuAdi = "Belge Talep Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "BelgeTalepAyar/Index", DilCeviriYap = false, SiraNo = 5)]
        [RoleAttribute(GorunurAdi = "Belge Talep Ayarları", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTalepAyarları = "Belge Talep Ayarları";

        [MenuAttribute(BagliMenuID = 77000, MenuAdi = "Talep Yap", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TalepYap/Index", DilCeviriYap = true, YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string TalepYap = "Talep Yap";
        [MenuAttribute(BagliMenuID = 77000, MenuAdi = "Gelen Talepler", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TalepGelenTalepler/Index", DilCeviriYap = false, SiraNo = 4)]
        [RoleAttribute(GorunurAdi = "Gelen Talepler", Kategori = "Talep İşlemleri", Aciklama = "")]
        public const string GelenTalepler = "Gelen Talepler";
        [RoleAttribute(GorunurAdi = "Gelen Talep Kayıt", Kategori = "Talep İşlemleri", Aciklama = "")]
        public const string GelenTalepKayit = "Gelen Talep Kayıt";
        [RoleAttribute(GorunurAdi = "Gelen Talep Sil", Kategori = "Talep İşlemleri", Aciklama = "")]
        public const string GelenTalepSil = "Gelen Talep Sil";

        [MenuAttribute(BagliMenuID = 77000, MenuAdi = "Talep Süreci", MenuCssClass = "fa fa-clock-o", MenuUrl = "TalepSureci/Index", DilCeviriYap = false, SiraNo = 7)]
        [RoleAttribute(GorunurAdi = "Talep Süreci", Kategori = "Talep İşlemleri", Aciklama = "")]
        public const string TalepSureci = "Talep Süreci"; 

        [MenuAttribute(BagliMenuID = 80000, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "Basvuru/Index", DilCeviriYap = true, YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 0)]
        //[RoleAttribute(GorunurAdi = "Başvuru", Kategori = "Başvuru İşlemleri", Aciklama = "")]
        public const string Basvuru = "Basvuru";
        [MenuAttribute(BagliMenuID = 80000, MenuAdi = "Dekont Giriş", MenuCssClass = "fa fa-file-text-o", MenuUrl = "DekontBilgi/Index", DilCeviriYap = true, YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        //[RoleAttribute(GorunurAdi = "Başvuru", Kategori = "Başvuru İşlemleri", Aciklama = "")]
        public const string DekontGiris = "Dekont Giriş";
        [MenuAttribute(BagliMenuID = 80000, MenuAdi = "Online Ödeme İşlemi", MenuCssClass = "fa fa-credit-card", MenuUrl = "OnlineOdeme/Index", DilCeviriYap = true, YetkisizErisim = true, YetkiliEnstituler = "010,020", AuthenticationControl = "authenticatedControl(this)", SiraNo = 2)]
        //[RoleAttribute(GorunurAdi = "Başvuru", Kategori = "Başvuru İşlemleri", Aciklama = "")]
        public const string OnlineOdeme = "OnlineOdeme";
        [MenuAttribute(BagliMenuID = 80000, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "GelenBasvurular/Index", DilCeviriYap = false, SiraNo = 3)]
        [RoleAttribute(GorunurAdi = "Gelen Başvurular", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string GelenBasvurular = "Gelen Başvurular";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular Kayıt", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string GelenBasvurularKayit = "Gelen Başvurular Kayıt";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular Sil", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string GelenBasvurularSil = "Gelen Başvurular Sil";
        [MenuAttribute(BagliMenuID = 80000, MenuAdi = "Başvuru Süreci", MenuCssClass = "fa fa-clock-o", MenuUrl = "BasvuruSureci/Index", DilCeviriYap = false, SiraNo = 4)]
        [RoleAttribute(GorunurAdi = "Başvuru Süreci", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string BasvuruSureci = "Başvuru Süreci Listesi";
        [RoleAttribute(GorunurAdi = "Başvuru Süreci Kayıt", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string BasvuruSureciKayit = "Başvuru Süreci Kayıt";
        [RoleAttribute(GorunurAdi = "Başvuru Süreci Öğrenci Kayıt", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string BasvuruSureciOgrenciKayit = "Başvuru Süreci Öğrenci Kayıt";
        [RoleAttribute(GorunurAdi = "Başvuru Süreci Sil", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string BasvuruSureciSil = "Başvuru Süreci Sil";
        [MenuAttribute(BagliMenuID = 80000, MenuAdi = "Kotalar", MenuCssClass = "fa fa-gear", MenuUrl = "Kotalar/Index", DilCeviriYap = false, SiraNo = 5)]
        [RoleAttribute(GorunurAdi = "Kotalar", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string Kotalar = "Kotalar";
        [MenuAttribute(BagliMenuID = 80000, MenuAdi = "Mülakat Süreci", MenuCssClass = "fa fa-clock-o", MenuUrl = "MulakatSureci/Index", DilCeviriYap = false, SiraNo = 6)]
        [RoleAttribute(GorunurAdi = "Mülakat Süreci", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string MulakatSureci = "Mülakat Süreci";
        [RoleAttribute(GorunurAdi = "Mülakat Süreci Kayıt", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string MulakatKayıt = "Mülakat Süreci Kayıt";
        [RoleAttribute(GorunurAdi = "Mülakat Süreci Sil", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string MulakatSil = "Mülakat Süreci Sil";


        [MenuAttribute(BagliMenuID = 80500, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "YYDBasvuru/Index", DilCeviriYap = true, YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 0)]
        public const string YydBasvuru = "YYDBasvuru";
        [MenuAttribute(BagliMenuID = 80500, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "YYDGelenBasvurular/Index", DilCeviriYap = false, SiraNo = 3)]
        [RoleAttribute(GorunurAdi = "Gelen Başvurular", Kategori = "YTU Yeni Mezun Başvuru", Aciklama = "")]
        public const string YydGelenBasvurular = "YYD Gelen Başvurular";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular Kayıt", Kategori = "YTU Yeni Mezun Başvuru", Aciklama = "")]
        public const string YydGelenBasvurularKayit = "YYDGelen Başvurular Kayıt";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular Sil", Kategori = "YTU Yeni Mezun Başvuru", Aciklama = "")]
        public const string YydGelenBasvurularSil = "YYD Gelen Başvurular Sil";
        [MenuAttribute(BagliMenuID = 80500, MenuAdi = "Başvuru Süreci", MenuCssClass = "fa fa-clock-o", MenuUrl = "YYDBasvuruSureci/Index", DilCeviriYap = false, SiraNo = 4)]
        [RoleAttribute(GorunurAdi = "Başvuru Süreci", Kategori = "YTU Yeni Mezun Başvuru", Aciklama = "")]
        public const string YydBasvuruSureci = "YYD Başvuru Süreci Listesi";
        [RoleAttribute(GorunurAdi = "Başvuru Süreci Kayıt", Kategori = "YTU Yeni Mezun Başvuru", Aciklama = "")]
        public const string YydBasvuruSureciKayit = "YYD Başvuru Süreci Kayıt";
        [RoleAttribute(GorunurAdi = "Başvuru Süreci Öğrenci Kayıt", Kategori = "YTU Yeni Mezun Başvuru", Aciklama = "")]
        public const string YydBasvuruSureciOgrenciKayit = "YYD Başvuru Süreci Öğrenci Kayıt";
        [RoleAttribute(GorunurAdi = "Başvuru Süreci Sil", Kategori = "YTU Yeni Mezun Başvuru", Aciklama = "")]
        public const string YydBasvuruSureciSil = "YYD Başvuru Süreci Sil";
        [MenuAttribute(BagliMenuID = 80500, MenuAdi = "Kotalar", MenuCssClass = "fa fa-gear", MenuUrl = "YYDKotalar/Index", DilCeviriYap = false, SiraNo = 5)]
        [RoleAttribute(GorunurAdi = "Kotalar", Kategori = "YTU Yeni Mezun Başvuru", Aciklama = "")]
        public const string YydKotalar = "YYD Kotalar";

        [MenuAttribute(BagliMenuID = 81000, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "YGBasvuru/Index", DilCeviriYap = true, YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 0)]
        public const string YgBasvuru = "YGBasvuru";
        [MenuAttribute(BagliMenuID = 81000, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "YGGelenBasvurular/Index", DilCeviriYap = false, SiraNo = 3)]
        [RoleAttribute(GorunurAdi = "Gelen Başvurular", Kategori = "Yatay Geçiş Başvuru", Aciklama = "")]
        public const string YgGelenBasvurular = "YG Gelen Başvurular";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular Kayıt", Kategori = "Yatay Geçiş Başvuru", Aciklama = "")]
        public const string YgGelenBasvurularKayit = "YGGelen Başvurular Kayıt";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular Sil", Kategori = "Yatay Geçiş Başvuru", Aciklama = "")]
        public const string YgGelenBasvurularSil = "YG Gelen Başvurular Sil";
        [MenuAttribute(BagliMenuID = 81000, MenuAdi = "Başvuru Süreci", MenuCssClass = "fa fa-clock-o", MenuUrl = "YGBasvuruSureci/Index", DilCeviriYap = false, SiraNo = 4)]
        [RoleAttribute(GorunurAdi = "Başvuru Süreci", Kategori = "Yatay Geçiş Başvuru", Aciklama = "")]
        public const string YgBasvuruSureci = "YG Başvuru Süreci Listesi";
        [RoleAttribute(GorunurAdi = "Başvuru Süreci Kayıt", Kategori = "Yatay Geçiş Başvuru", Aciklama = "")]
        public const string YgBasvuruSureciKayit = "YG Başvuru Süreci Kayıt";
        [RoleAttribute(GorunurAdi = "Başvuru Süreci Öğrenci Kayıt", Kategori = "Yatay Geçiş Başvuru", Aciklama = "")]
        public const string YgBasvuruSureciOgrenciKayit = "YG Başvuru Süreci Öğrenci Kayıt";
        [RoleAttribute(GorunurAdi = "Başvuru Süreci Sil", Kategori = "Yatay Geçiş Başvuru", Aciklama = "")]
        public const string YgBasvuruSureciSil = "YG Başvuru Süreci Sil";
        [MenuAttribute(BagliMenuID = 81000, MenuAdi = "Kotalar", MenuCssClass = "fa fa-gear", MenuUrl = "YGKotalar/Index", DilCeviriYap = false, SiraNo = 5)]
        [RoleAttribute(GorunurAdi = "Kotalar", Kategori = "Yatay Geçiş Başvuru", Aciklama = "")]
        public const string YgKotalar = "YG Kotalar";
        [MenuAttribute(BagliMenuID = 81000, MenuAdi = "Mülakat Süreci", MenuCssClass = "fa fa-clock-o", MenuUrl = "YGMulakatSureci/Index", DilCeviriYap = false, SiraNo = 6)]
        [RoleAttribute(GorunurAdi = "Mülakat Süreci", Kategori = "Yatay Geçiş Başvuru", Aciklama = "")]
        public const string YgMulakatSureci = "YG Mülakat Süreci";
        [RoleAttribute(GorunurAdi = "Mülakat Süreci Kayıt", Kategori = "Yatay Geçiş Başvuru", Aciklama = "")]
        public const string YgMulakatKayıt = "YG Mülakat Süreci Kayıt";
        [RoleAttribute(GorunurAdi = "Mülakat Süreci Sil", Kategori = "Yatay Geçiş Başvuru", Aciklama = "")]
        public const string YgMulakatSil = "YG Mülakat Süreci Sil";


        [MenuAttribute(BagliMenuID = 82300, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TDOBasvuru/Index", DilCeviriYap = true, YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string TdoBasvuru = "TDO Başvuru";
        [MenuAttribute(BagliMenuID = 82300, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TDOGelenBasvurular/Index", DilCeviriYap = false, SiraNo = 4)]
        [RoleAttribute(GorunurAdi = "Gelen Başvurular", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoGelenBasvuru = "TDO Gelen Başvurular";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular Kayıt", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoGelenBasvuruKayit = "TDO Gelen Başvurular Kayıt";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular Sil", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoGelenBasvuruSil = "TDO Gelen Başvurular Sil";
        [RoleAttribute(GorunurAdi = "TDO Form Düzeltme Yetkisi", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoFormOlusturmaYetkisi = "TDO Form Oluşturma Yetkisi";
        [RoleAttribute(GorunurAdi = "TDO Danışman Onay Yetkisi", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoDanismanOnayYetkisi = "TDO Danisman Onay Yetkisi";
        [RoleAttribute(GorunurAdi = "TDO EYK'ya Gönderim Yetkisi", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoeyKyaGonderimYetkisi = "TDO EYK'ya Gönderim Yetkisi";
        [RoleAttribute(GorunurAdi = "TDO EYK'da Onay Yetkisi", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoeyKdaOnayYetkisi = "TDO EYK'da Onay Yetkisi";
        [MenuAttribute(BagliMenuID = 82300, MenuAdi = "TDO Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "TDOAyarlar/Index", DilCeviriYap = false, SiraNo = 7)]
        [RoleAttribute(GorunurAdi = "TDO Ayarları", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoAyarlari = "TDO Ayarları";


        [MenuAttribute(BagliMenuID = 83300, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TIBasvuru/Index", DilCeviriYap = true, YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string TiBasvuru = "TI Başvuru";
        [MenuAttribute(BagliMenuID = 83300, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TIGelenBasvurular/Index", DilCeviriYap = false, SiraNo = 4)]
        [RoleAttribute(GorunurAdi = "Gelen Başvurular", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiGelenBasvuru = "TI Gelen Başvurular";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular Kayıt", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiGelenBasvuruKayit = "TI Gelen Başvurular Kayıt";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular Sil", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiGelenBasvuruSil = "TI Gelen Başvurular Sil";
        [RoleAttribute(GorunurAdi = "TIK Toplantı Talebi Yap", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiToplantiTalebiYap = "TI Toplantı Talebi Yap";
        [RoleAttribute(GorunurAdi = "TIK Tez Degerlendirme Yap", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiTezDegerlendirmeYap = "Tez İzleme Tez Degerlendirme Yap";
        [RoleAttribute(GorunurAdi = "TIK Tez Degerlendirme Düzeltme", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiTezDegerlendirmeDuzeltme = "Tez İzleme Tez Degerlendirme Düzeltme";
        [MenuAttribute(BagliMenuID = 83300, MenuAdi = "TIK Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "TIAyarlar/Index", DilCeviriYap = false, SiraNo = 7)]
        [RoleAttribute(GorunurAdi = "TIK Ayarları", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiAyarlari = "TI Ayarları";

        [MenuAttribute(BagliMenuID = 83500, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "Mezuniyet/Index", DilCeviriYap = true, YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        //[RoleAttribute(GorunurAdi = "Başvuru", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string MezuniyetBasvuru = "Mezuniyet Basvuru";
        [MenuAttribute(BagliMenuID = 83500, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "MezuniyetGelenBasvurular/Index", DilCeviriYap = false, SiraNo = 4)]
        [RoleAttribute(GorunurAdi = "Gelen Başvurular", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurular = "Mezuniyet Gelen Başvurular";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular J.Öneri.F Kayıt", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularJuriOneriFormuKayit = "Gelen Başvurular Juri Öneri Formu Kayıt";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular J.Öneri.F Onay", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularJuriOneriFormuOnay = "Gelen Başvurular Juri Öneri Formu Onay";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular J.Öneri.F Onay EYK'da", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularJuriOneriFormuEykOnay = "Gelen Başvurular Juri Öneri Formu EYK Onay";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular SR Talebi Yap", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularSrTalebiYap = "Gelen Başvurular SR Talebi Yap";

        [RoleAttribute(GorunurAdi = "Gelen Başvurular Tez Kontrol", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularTezKontrol = "Gelen Başvurular Tez Kontrol";

        [RoleAttribute(GorunurAdi = "Gelen Başvurular Tez Teslim Ek Süre", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularTtEkSure = "Gelen Başvurular Tez Teslim Ek Süre";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular Kayıt", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularKayit = "Mezuniyet Gelen Başvurular Kayıt";
        [RoleAttribute(GorunurAdi = "Gelen Başvurular Sil", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularSil = "Mezuniyet Gelen Başvurular Sil";

        [MenuAttribute(BagliMenuID = 83500, MenuAdi = "Mezuniyet Süreci", MenuCssClass = "fa fa-clock-o", MenuUrl = "MezuniyetSureci/Index", DilCeviriYap = false, SiraNo = 7)]
        [RoleAttribute(GorunurAdi = "Mezuniyet Süreci", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetSureci = "Mezuniyet Süreci";
        [RoleAttribute(GorunurAdi = "Mezuniyet Süreci Kayıt", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetSureciKayıt = "Mezuniyet Süreci Kayıt";
        [RoleAttribute(GorunurAdi = "Mezuniyet Süreci Sil", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetSureciSil = "Mezuniyet Süreci Sil";

        [MenuAttribute(BagliMenuID = 83500, MenuAdi = "Yönetmelikler", MenuCssClass = "fa fa-file-text-o", MenuUrl = "MezuniyetYonetmelikler/Index", DilCeviriYap = false, SiraNo = 8)]
        [RoleAttribute(GorunurAdi = "Yönetmelikler", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetYonetmelikler = "Mezuniyet Yönetmelikler";

        [MenuAttribute(BagliMenuID = 83500, MenuAdi = "Yayın Türleri", MenuCssClass = "fa fa-gear", MenuUrl = "MezuniyetYayinTurleri/Index", DilCeviriYap = false, SiraNo = 10)]
        [RoleAttribute(GorunurAdi = "Yayın Türleri", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetYayinTurleri = "Yayın Türleri";

        [MenuAttribute(BagliMenuID = 83500, MenuAdi = "Mezuniyet Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "MezuniyetAyarlar/Index", DilCeviriYap = false, SiraNo = 15)]
        [RoleAttribute(GorunurAdi = "Mezuniyet Ayarları", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetAyarları = "Mezuniyet Ayarları";



        [MenuAttribute(BagliMenuID = 84000, MenuAdi = "Lisansüstü Başvuru", MenuCssClass = "fa fa-bar-chart-o", MenuUrl = "RaporlarLUB/Index", DilCeviriYap = false, SiraNo = 5)]
        [RoleAttribute(GorunurAdi = "Lisansüstü Başvuru", Kategori = "Rapor İşlemleri", Aciklama = "")]
        public const string LisansustuBasvuruRapor = "Lisansüstü Başvuru";



        [MenuAttribute(BagliMenuID = 84000, MenuAdi = "Belge Talepleri", MenuCssClass = "fa fa-bar-chart-o", MenuUrl = "RaporlarBT/Index", DilCeviriYap = false, SiraNo = 10)]
        [RoleAttribute(GorunurAdi = "Belge Talepleri", Kategori = "Rapor İşlemleri", Aciklama = "")]
        public const string BelgeTalepleriRapor = "Belge Talepleri";

        [MenuAttribute(BagliMenuID = 84000, MenuAdi = "Anketler", MenuCssClass = "fa fa-bar-chart-o", MenuUrl = "RaporlarAnket/Index", DilCeviriYap = false, SiraNo = 15)]
        [RoleAttribute(GorunurAdi = "Anketler", Kategori = "Rapor İşlemleri", Aciklama = "")]
        public const string AnketlerRapor = "Anketler";


        [MenuAttribute(BagliMenuID = 85000, MenuAdi = "Kullanıcılar", MenuCssClass = "fa fa-list-alt", MenuUrl = "Kullanicilar/Index", DilCeviriYap = false, SiraNo = 1)]
        [RoleAttribute(GorunurAdi = "Kullanıcılar", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string Kullanicilar = "Kullanıcılar Listesi";
        [RoleAttribute(GorunurAdi = "Kullanıcılar İşlem Yetkileri", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullanicilarIslemYetkileri = "Kullanıcılar İşlem Yetkileri";
        [RoleAttribute(GorunurAdi = "Kullanıcılar Enstitü Yetkileri", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullanicilarEnstituYetkileri = "Kullanıcılar Enstitü Yetkileri";
        [RoleAttribute(GorunurAdi = "Kullanıcılar Program Yetkileri", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullanicilarProgramYetkileri = "Kullanıcılar Program Yetkileri";
        //[RoleAttribute(GorunurAdi = "Kullanıcılar Yetki Aktarımı", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        //public const string KullanicilarYetkiAktarimi = "Kullanıcılar Yetki Aktarımı";
        [RoleAttribute(GorunurAdi = "Kullanıcılar Kayıt", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullanicilarKayit = "Kullanıcılar Kayıt";
        [RoleAttribute(GorunurAdi = "Kullanıcı Adına Lisansütü Başvuru Yap", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullaniciAdinaBasvuruYap = "Kullanıcı Adına Başvuru Yap";
        [RoleAttribute(GorunurAdi = "Kullanıcı Adına Tez danışmanı önerisi Yap", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullaniciAdinaTezDanismanOnerisiYap = "Kullanıcı Adına Tez danışmanı önerisi Yap";
        [RoleAttribute(GorunurAdi = "Kullanıcı Adına Tez İzleme Başvurusu Yap", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullaniciAdinaTezIzlemeBasvurusuYap = "Kullanıcı Adına Tez İzleme Başvurusu Yap";
        [RoleAttribute(GorunurAdi = "Kullanıcı Adına Mezuniyet Başvurusu Yap", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullaniciAdinaMezuniyetBasvurusuYap = "Kullanıcı Adına Mezuniyet Başvurusu Yap";
        [RoleAttribute(GorunurAdi = "Kullanıcılar Sil", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullanicilarSil = "Kullanıcı Sil";
        [RoleAttribute(GorunurAdi = "Online Kullanıcıları Gör", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullanicilarOnlineKullanicilar = "Online Kullanıcıları Gör";


        [MenuAttribute(BagliMenuID = 85000, MenuAdi = "Yetki Grupları", MenuCssClass = "fa fa-list-alt", MenuUrl = "YetkiGruplari/Index", DilCeviriYap = false, SiraNo = 1)]
        [RoleAttribute(GorunurAdi = "Yetki Grupları", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string YetkiGruplari = "Yetki Grupları";
        [MenuAttribute(BagliMenuID = 85000, MenuAdi = "Birimler", MenuCssClass = "fa fa-list-alt", MenuUrl = "Birimler/Index", DilCeviriYap = false, SiraNo = 2)]
        [RoleAttribute(GorunurAdi = "Birimler", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string Birimler = "Birimler";
        [MenuAttribute(BagliMenuID = 85000, MenuAdi = "Ünvanlar", MenuCssClass = "fa fa-list-alt", MenuUrl = "Unvanlar/Index", DilCeviriYap = false, SiraNo = 3)]
        [RoleAttribute(GorunurAdi = "Ünvanlar", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string Unvanlar = "Ünvanlar";

        [MenuAttribute(BagliMenuID = 90000, MenuAdi = "Uyruklar", MenuCssClass = "fa fa-list-alt", MenuUrl = "Uyruklar/Index", DilCeviriYap = false, SiraNo = 1)]
        [RoleAttribute(GorunurAdi = "Uyruklar", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string Uyruklar = "Uyruklar";
        [MenuAttribute(BagliMenuID = 90000, MenuAdi = "Şehirler", MenuCssClass = "fa fa-list-alt", MenuUrl = "Sehirler/Index", DilCeviriYap = false, SiraNo = 4)]
        [RoleAttribute(GorunurAdi = "Şehirler", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string Sehirler = "Şehirler";
        [MenuAttribute(BagliMenuID = 90000, MenuAdi = "Üniversiteler", MenuCssClass = "fa fa-list-alt", MenuUrl = "Universiteler/Index", DilCeviriYap = false, SiraNo = 7)]
        [RoleAttribute(GorunurAdi = "Üniversiteler", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string Universiteler = "Üniversiteler";
        [MenuAttribute(BagliMenuID = 90000, MenuAdi = "Enstitüler", MenuCssClass = "fa fa-list-alt", MenuUrl = "Enstituler/Index", DilCeviriYap = false, SiraNo = 10)]
        [RoleAttribute(GorunurAdi = "Enstitüler", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string Enstituler = "Enstitüler";

        [MenuAttribute(BagliMenuID = 90000, MenuAdi = "Öğrenci Bölümleri", MenuCssClass = "fa fa-list-alt", MenuUrl = "OgrenciBolumleri/Index", DilCeviriYap = false, SiraNo = 13)]
        [RoleAttribute(GorunurAdi = "Öğrenci Bölümleri", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string OgrenciBolumleri = "Öğrenci Bölümleri";

        [MenuAttribute(BagliMenuID = 90000, MenuAdi = "Öğrenim Tipleri", MenuCssClass = "fa fa-list-alt", MenuUrl = "OgrenimTipleri/Index", DilCeviriYap = false, SiraNo = 16)]
        [RoleAttribute(GorunurAdi = "Öğrenim Tipleri", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string OgrenimTipleri = "Öğrenim Tipleri";

        [MenuAttribute(BagliMenuID = 90000, MenuAdi = "Anabilim Dalları", MenuCssClass = "fa fa-list-alt", MenuUrl = "Anabilimdallari/Index", DilCeviriYap = false, SiraNo = 19)]
        [RoleAttribute(GorunurAdi = "Anabilim Dalları", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string AnabilimDallari = "Anabilim Dalları";

        [MenuAttribute(BagliMenuID = 90000, MenuAdi = "Programlar", MenuCssClass = "fa fa-list-alt", MenuUrl = "Programlar/Index", DilCeviriYap = false, SiraNo = 22)]
        [RoleAttribute(GorunurAdi = "Programlar", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string Programlar = "Programlar";

        [MenuAttribute(BagliMenuID = 90000, MenuAdi = "Bölüm-Program Eşleştir", MenuCssClass = "fa fa-gear", MenuUrl = "BolumEslestir/Index", DilCeviriYap = false, SiraNo = 25)]
        [RoleAttribute(GorunurAdi = "Bölüm Eşleştir", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string BolumEslestir = "Bölüm Eşleştir";


        [MenuAttribute(BagliMenuID = 90000, MenuAdi = "Sınav Tipleri", MenuCssClass = "fa fa-gear", MenuUrl = "SinavTipleri/Index", DilCeviriYap = false, SiraNo = 28)]
        [RoleAttribute(GorunurAdi = "Sınav Tipleri", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string SinavTipleri = "Sınav Tipleri";  


        [MenuAttribute(BagliMenuID = 100000, MenuAdi = "Duyurular", MenuCssClass = "fa fa-bullhorn", MenuUrl = "Duyurular/Index", DilCeviriYap = false, SiraNo = 1)]
        [RoleAttribute(GorunurAdi = "Duyurular", Kategori = "Sistem", Aciklama = "")]
        public const string Duyurular = "Duyurular";

        [MenuAttribute(BagliMenuID = 100000, MenuAdi = "Gelen Mesajlar", MenuCssClass = "fa fa-envelope", MenuUrl = "Mesajlar/Index", DilCeviriYap = false, SiraNo = 4)]
        [RoleAttribute(GorunurAdi = "Gelen Mesajlar", Kategori = "Sistem", Aciklama = "")]
        public const string Mesajlar = "Gelen Mesajlar";
        [RoleAttribute(GorunurAdi = "Gelen Mesajlar Sil", Kategori = "Sistem", Aciklama = "")]
        public const string MesajlarSil = "Gelen Mesajlar Sil";

        [MenuAttribute(BagliMenuID = 100000, MenuAdi = "Mail İşlemleri", MenuCssClass = "fa fa-envelope", MenuUrl = "MailIslemleri/Index", DilCeviriYap = false, SiraNo = 7)]
        [RoleAttribute(GorunurAdi = "Mail İşlemleri", Kategori = "Sistem", Aciklama = "")]
        public const string MailIslemleri = "Mail İşlemleri";

        [RoleAttribute(GorunurAdi = "Mail Gönder", Kategori = "Sistem", Aciklama = "")]
        public const string MailGonder = "Mail Gönder";
        [MenuAttribute(BagliMenuID = 100000, MenuAdi = "Mail Şablonları", MenuCssClass = "fa fa-pencil", MenuUrl = "MailSablonlari/Index", DilCeviriYap = false, SiraNo = 10)]
        [RoleAttribute(GorunurAdi = "Mail Şablonları", Kategori = "Sistem", Aciklama = "")]
        public const string MailSablonlari = "Mail Şablonları";

        [MenuAttribute(BagliMenuID = 100000, MenuAdi = "Mail Şablonları (Sistem)", MenuCssClass = "fa fa-gear", MenuUrl = "MailSablonlariSistem/Index", DilCeviriYap = false, SiraNo = 13)]
        [RoleAttribute(GorunurAdi = "Mail Şablonları (Sistem)", Kategori = "Sistem", Aciklama = "")]
        public const string MailSablonlariSistem = "Mail Şablonları (Sistem)";


        [MenuAttribute(BagliMenuID = 100000, MenuAdi = "Mesaj Kategorileri", MenuCssClass = "fa fa-pencil", MenuUrl = "MesajKategorileri/Index", DilCeviriYap = false, SiraNo = 16)]
        [RoleAttribute(GorunurAdi = "Mesaj Kategorileri", Kategori = "Sistem", Aciklama = "")]
        public const string MesajlarKategorileri = "Mesaj Kategorileri";

        [MenuAttribute(BagliMenuID = 100000, MenuAdi = "Anketler", MenuCssClass = "fa fa-pencil", MenuUrl = "Anketler/Index", DilCeviriYap = false, SiraNo = 19)]
        [RoleAttribute(GorunurAdi = "Anketler", Kategori = "Sistem", Aciklama = "")]
        public const string Anketler = "Anketler";

        [RoleAttribute(GorunurAdi = "SSS Kayıt", Kategori = "Sistem", Aciklama = "")]
        public const string SssKayit = "SSSKayit";

        [MenuAttribute(BagliMenuID = 100000, MenuAdi = "Sistem Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "SistemAyarlari/Index", DilCeviriYap = false, SiraNo = 22)]
        [RoleAttribute(GorunurAdi = "Sistem Ayarları", Kategori = "Sistem", Aciklama = "")]
        public const string SistemAyarlari = "Sistem Ayarları";

        [MenuAttribute(BagliMenuID = 100000, MenuAdi = "Sistem Bilgilendirme", MenuCssClass = "fa fa-envelope", MenuUrl = "SistemBilgilendirme/Index", DilCeviriYap = false, SiraNo = 25)]
        [RoleAttribute(GorunurAdi = "Sistem Bilgilendirme", Kategori = "Sistem", Aciklama = "")]
        public const string SistemBilgilendirme = "Sistem Bilgilendirme";
        //[RoleAttribute(GorunurAdi = "Sistem Bilgilendirme Sil", Kategori = "Sistem", Aciklama = "")]
        //public const string SistemBilgilendirmeSil = "Sistem Bilgilendirme Sil";


    }
}