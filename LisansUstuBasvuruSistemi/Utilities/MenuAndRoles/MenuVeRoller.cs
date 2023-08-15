using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Utilities.MenuAndRoles
{
    public class SystemMenu : IMenu
    {
        //[MenuAttribute(MenuID = 60000, MenuAdi = "İntihal Kontrol", MenuCssClass = "fa fa-file-text-o", MenuUrl = "",  YetkisizErisim = true, SiraNo = 1)]
        //public const string IntihalKontrol = "İntihal Kontrol";


        [Menu(MenuID = 65000, MenuAdi = "Salon Rezervasyonu", MenuCssClass = "fa fa-file-text-o", MenuUrl = "", YetkisizErisim = false, YetkiliEnstituler = "010,020", SiraNo = 3)]
        public const string SalonRezervasyonIslemleri = "Salon Rezervasyon İşlemleri";

        [Menu(MenuID = 75000, MenuAdi = "Belge Talep", MenuCssClass = "fa fa-file-text-o", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010", SiraNo = 5)]
        public const string BelgeTalepIslemleri = "Belge Talep";

        [Menu(MenuID = 77000, MenuAdi = "Talep İşlemleri", MenuCssClass = "fa fa-file-text-o", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010,020,030", SiraNo = 6)]
        public const string TalepIslemleri = "Talep İşlemleri";

        [Menu(MenuID = 80000, MenuAdi = "Lisansüstü Başvuru", MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", YetkisizErisim = false, YetkiliEnstituler = "010,020", SiraNo = 7)]
        public const string BasvuruIslemleri = "Başvuru İşlemleri";

       

        [Menu(MenuID = 82300, MenuAdi = "Tez Danışmanı Öneri", MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010,020,030", SiraNo = 12)]
        public const string TdoIslemleri = "Tez danışmanı öneri İşlemleri";

        [Menu(MenuID = 82500, MenuAdi = "Yeterlik İşlemleri", MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010,020,030", SiraNo = 13)]
        public const string YeterlikIslemleri = "Yeterlik İşlemleri";


        [Menu(MenuID = 83300, MenuAdi = "Tez İzleme İşlemleri", MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010,020,030", SiraNo = 15)]
        public const string TiIslemleri = "Tez İzleme İşlemleri";

        [Menu(MenuID = 83500, MenuAdi = "Mezuniyet İşlemleri", MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010,020,030", SiraNo = 16)]
        public const string MezuniyetIslemleri = "Mezuniyet İşlemleri";

        [Menu(MenuID = 84000, MenuAdi = "Rapor İşlemleri", MenuCssClass = "fa fa-bar-chart-o", MenuUrl = "", YetkiliEnstituler = "010,020,030", SiraNo = 18)]
        public const string RaporIslemleri = "RaporIslemleri";

        [Menu(MenuID = 85000, MenuAdi = "Kullanıcı İşlemleri", MenuCssClass = "fa fa-group", MenuUrl = "", YetkiliEnstituler = "010,020,030", SiraNo = 21)]
        public const string KullaniciIslemleri = "Kullanıcı İşlemleri";

        [Menu(MenuID = 90000, MenuAdi = "Tanımlamalar", MenuCssClass = "fa fa-gears", MenuUrl = "", YetkiliEnstituler = "010,020,030", SiraNo = 24)]
        public const string Tanimlamalar = "Tanımlamalar";

        [Menu(MenuID = 100000, MenuAdi = "Sistem", MenuCssClass = "fa fa-desktop", MenuUrl = "", YetkiliEnstituler = "010,020,030", SiraNo = 27)]
        public const string Sistem = "Sistem";

    }
    public class RoleNames : IRoleName, IMenu
    {

        [Menu(BagliMenuID = 65000, MenuAdi = "Rezervasyon Yap", MenuCssClass = "fa fa-file-text-o", MenuUrl = "SR/Index", YetkisizErisim = false, SiraNo = 1)]
        [Role(GorunurAdi = "Rezervasyon Yap", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrYap = "Rezervasyon Yap";
        [Menu(BagliMenuID = 65000, MenuAdi = "Gelen Rezervasyonlar", MenuCssClass = "fa fa-file-text-o", MenuUrl = "SRGelenTalepler/Index", SiraNo = 2)]
        [Role(GorunurAdi = "Gelen Rezervasyon Talepleri", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrGelenTalepler = "Gelen Rezervasyon Talepleri";
        [Role(GorunurAdi = "Rezervasyon Talebi Düzelt", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrTalepDuzelt = "Rezervasyon Talebi Düzelt";
        [Role(GorunurAdi = "Rezervasyon Talebi Sil", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrTalepSil = "Rezervasyon Talebi Sil";

        [Menu(BagliMenuID = 65000, MenuAdi = "Özel Tanımlar", MenuCssClass = "fa fa-list-alt", MenuUrl = "SROzelTanimlar/Index", SiraNo = 3)]
        [Role(GorunurAdi = "Özel Tanımlar", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrOzelTanimlar = "Özel Tanımlar";

        [Menu(BagliMenuID = 65000, MenuAdi = "Salon Bilgi", MenuCssClass = "fa fa-list-alt", MenuUrl = "SRSalonBilgi/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Salon Bilgi", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrSalonBilgi = "Salon Bilgi";


        [Menu(BagliMenuID = 65000, MenuAdi = "Salon Tanımları", MenuCssClass = "fa fa-list-alt", MenuUrl = "SRSalonlar/Index", SiraNo = 5)]
        [Role(GorunurAdi = "Salon Tanımları", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrSalonlar = "Salon Rezervasyon Salonlar";

        [Menu(BagliMenuID = 65000, MenuAdi = "Talep Tipleri", MenuCssClass = "fa fa-list-alt", MenuUrl = "SRTalepTipleri/Index", SiraNo = 6)]
        [Role(GorunurAdi = "Talep Tipleri", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrTalepTipleri = "Talep Tipleri";

        [Role(GorunurAdi = "Salon Tanımları Kayıt", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrSalonlarKayıt = "Salon Tanımları Kayıt";
        [Role(GorunurAdi = "Salon Tanımları Sil", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrSalonlarSil = "Salon Tanımları Sil";

        [Menu(BagliMenuID = 65000, MenuAdi = "Salon Rezervasyon Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "SRAyarlar/Index", SiraNo = 7)]
        [Role(GorunurAdi = "Salon Rez. Ayarları", Kategori = "Salon Rezervasyonu", Aciklama = "")]
        public const string SrAyarları = "Salon Rezervasyon Ayarları";


        [Menu(BagliMenuID = 75000, MenuAdi = "Belge Talebi Yap", MenuCssClass = "fa fa-file-text-o", MenuUrl = "BelgeTalebi/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        //[RoleAttribute(GorunurAdi = "Belge Talebi Yap", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTalebiYap = "Belge Talebi Yap";
        [Menu(BagliMenuID = 75000, MenuAdi = "Gelen Belge Talepleri", MenuCssClass = "fa fa-file-text-o", MenuUrl = "GelenBelgeTalepleri/Index", SiraNo = 2)]
        [Role(GorunurAdi = "Gelen Belge Talepleri", Kategori = "Belge Talep", Aciklama = "")]
        public const string GelenBelgeTalepleri = "Gelen Belge Talepleri";
        [Role(GorunurAdi = "Belge Talebi Düzelt", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTalebiDuzelt = "Belge Talebi Düzelt";
        [Role(GorunurAdi = "Belge Talebi Sil", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTalebiSil = "Belge Talebi Sil";

        [Menu(BagliMenuID = 75000, MenuAdi = "Belge Tip Detay", MenuCssClass = "fa fa-gear", MenuUrl = "BelgeTipDetay/Index", SiraNo = 3)]
        [Role(GorunurAdi = "Belge Tip Detay", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTipDetay = "Belge Tip Detay";
        [Role(GorunurAdi = "Belge Tip Detay Kayıt", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTipDetayKayıt = "Belge Tip Detay Kayıt";
        [Role(GorunurAdi = "Belge Tip Detay Sil", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTipDetaySil = "Belge Tip Detay Sil";

        [Menu(BagliMenuID = 75000, MenuAdi = "Belge Tipleri", MenuCssClass = "fa fa-list-alt", MenuUrl = "BelgeTipleri/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Belge Tipleri", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTipleri = "Belge Tipleri";
        [Role(GorunurAdi = "Belge Tipleri Kayıt", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTipleriKayıt = "Belge Tipleri Kayıt";
        [Role(GorunurAdi = "Belge Tipleri Sil", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTipleriSil = "Belge Tipleri Sil";

        [Menu(BagliMenuID = 75000, MenuAdi = "Belge Talep Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "BelgeTalepAyar/Index", SiraNo = 5)]
        [Role(GorunurAdi = "Belge Talep Ayarları", Kategori = "Belge Talep", Aciklama = "")]
        public const string BelgeTalepAyarları = "Belge Talep Ayarları";

        [Menu(BagliMenuID = 77000, MenuAdi = "Talep Yap", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TalepYap/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string TalepYap = "Talep Yap";
        [Menu(BagliMenuID = 77000, MenuAdi = "Gelen Talepler", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TalepGelenTalepler/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Gelen Talepler", Kategori = "Talep İşlemleri", Aciklama = "")]
        public const string GelenTalepler = "Gelen Talepler";
        [Role(GorunurAdi = "Gelen Talep Kayıt", Kategori = "Talep İşlemleri", Aciklama = "")]
        public const string GelenTalepKayit = "Gelen Talep Kayıt";
        [Role(GorunurAdi = "Gelen Talep Sil", Kategori = "Talep İşlemleri", Aciklama = "")]
        public const string GelenTalepSil = "Gelen Talep Sil";

        [Menu(BagliMenuID = 77000, MenuAdi = "Talep Süreci", MenuCssClass = "fa fa-clock-o", MenuUrl = "TalepSureci/Index", SiraNo = 7)]
        [Role(GorunurAdi = "Talep Süreci", Kategori = "Talep İşlemleri", Aciklama = "")]
        public const string TalepSureci = "Talep Süreci";

        [Menu(BagliMenuID = 80000, MenuAdi = "Lisansüstü Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "GelenBasvurular/Index", SiraNo = 3)]
        [Role(GorunurAdi = "Lisansüstü Başvurular", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string GelenBasvurular = "Gelen Başvurular";  
        [Menu(BagliMenuID = 80000, MenuAdi = "Yatay Geçiş Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "YYDGelenBasvurular/Index", SiraNo = 6)]
        [Role(GorunurAdi = "Yatay Geçiş Başvurular", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string YydGelenBasvurular = "YYD Gelen Başvurular"; 
        [Menu(BagliMenuID = 80000, MenuAdi = "Yeni Mezun Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "YGGelenBasvurular/Index", SiraNo = 10)]
        [Role(GorunurAdi = "Yeni Mezun Başvurular", Kategori = "Lisansüstü Başvuru", Aciklama = "")]
        public const string YgGelenBasvurular = "YG Gelen Başvurular"; 


        [Menu(BagliMenuID = 82500, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "Yeterlik/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string YeterlikBasvuru = "Yeterlik Basvuru";
        [Menu(BagliMenuID = 82500, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "YeterlikGelenBasvurular/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Gelen Başvurular", Kategori = "Yeterlik İşlemleri", Aciklama = "")]
        public const string YeterlikGelenBasvurular = "Yeterlik Gelen Başvurular";
        [Role(GorunurAdi = "Gelen Başvurular Kayıt Yetkisi", Kategori = "Yeterlik İşlemleri", Aciklama = "")]
        public const string YeterlikGelenBasvurularKayit = "Yeterlik Gelen Başvurular Kayit";
        [Role(GorunurAdi = "Başvuru Onay Yetkisi", Kategori = "Yeterlik İşlemleri", Aciklama = "")]
        public const string YeterlikBasvuruOnayYetkisi = "Yeterlik Başvuru Onay Yetkisi";
        [Role(GorunurAdi = "Abd/Jüri Onayı Düzeltme Yetkisi", Kategori = "Yeterlik İşlemleri", Aciklama = "")]
        public const string YeterlikAbdJuriOnayDuzeltme = "Yeterlik Abd/Jüri Onay Düzeltme";


        [Menu(BagliMenuID = 82500, MenuAdi = "Yeterlik Süreci", MenuCssClass = "fa fa-clock-o", MenuUrl = "YeterlikSureci/Index", SiraNo = 7)]
        [Role(GorunurAdi = "Yeterlik Süreci", Kategori = "Yeterlik İşlemleri", Aciklama = "")]
        public const string YeterlikSureci = "Yeterlik Süreci";
        [Role(GorunurAdi = "Yeterlik Süreci Kayıt", Kategori = "Yeterlik İşlemleri", Aciklama = "")]
        public const string YeterlikSureciKayıt = "Yeterlik Süreci Kayıt";
        [Role(GorunurAdi = "Yeterlik Süreci Sil", Kategori = "Yeterlik İşlemleri", Aciklama = "")]
        public const string YeterlikSureciSil = "Yeterlik Süreci Sil";


        [Menu(BagliMenuID = 82300, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TDOBasvuru/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string TdoBasvuru = "TDO Başvuru";
        [Menu(BagliMenuID = 82300, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TDOGelenBasvurular/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Gelen Başvurular", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoGelenBasvuru = "TDO Gelen Başvurular";
        [Role(GorunurAdi = "Gelen Başvurular Kayıt", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoGelenBasvuruKayit = "TDO Gelen Başvurular Kayıt";
        [Role(GorunurAdi = "Gelen Başvurular Sil", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoGelenBasvuruSil = "TDO Gelen Başvurular Sil";
        [Role(GorunurAdi = "TDO Form Düzeltme Yetkisi", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoFormOlusturmaYetkisi = "TDO Form Oluşturma Yetkisi";
        [Role(GorunurAdi = "TDO Danışman Onay Yetkisi", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoDanismanOnayYetkisi = "TDO Danisman Onay Yetkisi";
        [Role(GorunurAdi = "TDO EYK'ya Gönderim Yetkisi", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoeyKyaGonderimYetkisi = "TDO EYK'ya Gönderim Yetkisi";
        [Role(GorunurAdi = "TDO EYK'da Onay Yetkisi", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoeyKdaOnayYetkisi = "TDO EYK'da Onay Yetkisi";
        [Menu(BagliMenuID = 82300, MenuAdi = "TDO Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "TDOAyarlar/Index", SiraNo = 7)]
        [Role(GorunurAdi = "TDO Ayarları", Kategori = "Tez Danışmanı Öneri", Aciklama = "")]
        public const string TdoAyarlari = "TDO Ayarları";


        [Menu(BagliMenuID = 83300, MenuAdi = "Jüri Önerisi", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TiJuriOneri/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string TiJuriOneriBasvuru = "Tij Önerisi";
        [Menu(BagliMenuID = 83300, MenuAdi = "Gelen Jüri Önerileri", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TiJuriOnerileriGb/Index", SiraNo = 3)]
        [Role(GorunurAdi = "Gelen Jüri Önerileri", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiJuriOnerileriGb = "Tez İzleme Jüri Önerisileri";
        [Role(GorunurAdi = "Jüri Önerileri Kayıt", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiJuriOnerileriKayit = "Tez İzleme Önerisi Başvuru Kayıt";
        [Role(GorunurAdi = "Jüri Önerisi Silme Yetkisi", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiJuriOnerileriSil = "Tez İzleme Önerisi Silme Yetkisi";
        [Role(GorunurAdi = "Jüri Önerisi Eyk ya Gönderme Yetkisi", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiJuriOnerileriEykYaGonder = "Jüri Önerisi Eyk ya Gönderme Yetkisi";
        [Role(GorunurAdi = "Jüri Önerisi Eyk da Onay Yetkisi", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiJuriOnerileriEykDaOnay = "Jüri Önerisi Eyk da Onay Yetkisi";
        [Role(GorunurAdi = "Jüri Önerisini Öğrenci Adına Yap", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiJuriOnerileriOgrenciAdina = "Jüri Önerisini Öğrenci Adına Yap";


        [Menu(BagliMenuID = 83300, MenuAdi = "Ara Rapor Giriş", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TIBasvuru/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 15)]
        public const string TiBasvuru = "Ti Basvuru";
        [Menu(BagliMenuID = 83300, MenuAdi = "Ara Rapor Başvuruları", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TIGelenBasvurular/Index", SiraNo = 17)]
        [Role(GorunurAdi = "Ara Rapor Başvuruları", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiGelenBasvuru = "TI Gelen Başvurular";
        [Role(GorunurAdi = "Ara Rapor Başvuruları Kayıt", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiGelenBasvuruKayit = "TI Gelen Başvurular Kayıt";
        [Role(GorunurAdi = "Ara Rapor Başvuruları Sil", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiGelenBasvuruSil = "TI Gelen Başvurular Sil";
        [Role(GorunurAdi = "Ara Rapor Toplantı Talebi Yap", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiToplantiTalebiYap = "TI Toplantı Talebi Yap";
        [Role(GorunurAdi = "Ara Rapor Degerlendirme Yap", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiTezDegerlendirmeYap = "Tez İzleme Tez Degerlendirme Yap";
        [Role(GorunurAdi = "Ara Rapor Degerlendirme Düzeltme", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiTezDegerlendirmeDuzeltme = "Tez İzleme Tez Degerlendirme Düzeltme";
        [Menu(BagliMenuID = 83300, MenuAdi = "TIK Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "TIAyarlar/Index", SiraNo = 30)]
        [Role(GorunurAdi = "Tez İzleme Ayarları", Kategori = "Tez İzleme İşlemleri", Aciklama = "")]
        public const string TiAyarlari = "TI Ayarları";





        [Menu(BagliMenuID = 83500, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "Mezuniyet/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string MezuniyetBasvuru = "Mezuniyet Basvuru";
        [Menu(BagliMenuID = 83500, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "MezuniyetGelenBasvurular/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Gelen Başvurular", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurular = "Mezuniyet Gelen Başvurular";
        [Role(GorunurAdi = "Gelen Başvurular J.Öneri.F Kayıt", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularJuriOneriFormuKayit = "Gelen Başvurular Juri Öneri Formu Kayıt";
        [Role(GorunurAdi = "Gelen Başvurular J.Öneri.F Onay", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularJuriOneriFormuOnay = "Gelen Başvurular Juri Öneri Formu Onay";
        [Role(GorunurAdi = "Gelen Başvurular J.Öneri.F Onay EYK'da", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularJuriOneriFormuEykOnay = "Gelen Başvurular Juri Öneri Formu EYK Onay";
        [Role(GorunurAdi = "Gelen Başvurular SR Talebi Yap", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularSrTalebiYap = "Gelen Başvurular SR Talebi Yap";

        [Role(GorunurAdi = "Gelen Başvurular Tez Kontrol", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularTezKontrol = "Gelen Başvurular Tez Kontrol";

        [Role(GorunurAdi = "Gelen Başvurular Tez Teslim Ek Süre", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularTtEkSure = "Gelen Başvurular Tez Teslim Ek Süre";
        [Role(GorunurAdi = "Gelen Başvurular Kayıt", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularKayit = "Mezuniyet Gelen Başvurular Kayıt";
        [Role(GorunurAdi = "Gelen Başvurular Sil", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetGelenBasvurularSil = "Mezuniyet Gelen Başvurular Sil";

        [Menu(BagliMenuID = 83500, MenuAdi = "Mezuniyet Süreci", MenuCssClass = "fa fa-clock-o", MenuUrl = "MezuniyetSureci/Index", SiraNo = 7)]
        [Role(GorunurAdi = "Mezuniyet Süreci", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetSureci = "Mezuniyet Süreci";
        [Role(GorunurAdi = "Mezuniyet Süreci Kayıt", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetSureciKayıt = "Mezuniyet Süreci Kayıt";
        [Role(GorunurAdi = "Mezuniyet Süreci Sil", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetSureciSil = "Mezuniyet Süreci Sil";

        [Menu(BagliMenuID = 83500, MenuAdi = "Yönetmelikler", MenuCssClass = "fa fa-file-text-o", MenuUrl = "MezuniyetYonetmelikler/Index", SiraNo = 8)]
        [Role(GorunurAdi = "Yönetmelikler", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetYonetmelikler = "Mezuniyet Yönetmelikler";

        [Menu(BagliMenuID = 83500, MenuAdi = "Yayın Türleri", MenuCssClass = "fa fa-gear", MenuUrl = "MezuniyetYayinTurleri/Index", SiraNo = 10)]
        [Role(GorunurAdi = "Yayın Türleri", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetYayinTurleri = "Yayın Türleri";

        [Menu(BagliMenuID = 83500, MenuAdi = "Mezuniyet Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "MezuniyetAyarlar/Index", SiraNo = 15)]
        [Role(GorunurAdi = "Mezuniyet Ayarları", Kategori = "Mezuniyet İşlemleri", Aciklama = "")]
        public const string MezuniyetAyarları = "Mezuniyet Ayarları";



        [Menu(BagliMenuID = 84000, MenuAdi = "Lisansüstü Başvuru", MenuCssClass = "fa fa-bar-chart-o", MenuUrl = "RaporlarLUB/Index", SiraNo = 5)]
        [Role(GorunurAdi = "Lisansüstü Başvuru", Kategori = "Rapor İşlemleri", Aciklama = "")]
        public const string LisansustuBasvuruRapor = "Lisansüstü Başvuru";



        [Menu(BagliMenuID = 84000, MenuAdi = "Belge Talepleri", MenuCssClass = "fa fa-bar-chart-o", MenuUrl = "RaporlarBT/Index", SiraNo = 10)]
        [Role(GorunurAdi = "Belge Talepleri", Kategori = "Rapor İşlemleri", Aciklama = "")]
        public const string BelgeTalepleriRapor = "Belge Talepleri";

        [Menu(BagliMenuID = 84000, MenuAdi = "Anketler", MenuCssClass = "fa fa-bar-chart-o", MenuUrl = "RaporlarAnket/Index", SiraNo = 15)]
        [Role(GorunurAdi = "Anketler", Kategori = "Rapor İşlemleri", Aciklama = "")]
        public const string AnketlerRapor = "Anketler";


        [Menu(BagliMenuID = 85000, MenuAdi = "Kullanıcılar", MenuCssClass = "fa fa-list-alt", MenuUrl = "Kullanicilar/Index", SiraNo = 1)]
        [Role(GorunurAdi = "Kullanıcılar", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string Kullanicilar = "Kullanıcılar Listesi";
        [Role(GorunurAdi = "Kullanıcılar İşlem Yetkileri", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullanicilarIslemYetkileri = "Kullanıcılar İşlem Yetkileri";
        [Role(GorunurAdi = "Kullanıcılar Enstitü Yetkileri", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullanicilarEnstituYetkileri = "Kullanıcılar Enstitü Yetkileri";
        [Role(GorunurAdi = "Kullanıcılar Program Yetkileri", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullanicilarProgramYetkileri = "Kullanıcılar Program Yetkileri"; 
        [Role(GorunurAdi = "Kullanıcılar Kayıt", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullanicilarKayit = "Kullanıcılar Kayıt"; 
        [Role(GorunurAdi = "Kullanıcılar Sil", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullanicilarSil = "Kullanıcı Sil";
        [Role(GorunurAdi = "Online Kullanıcıları Gör", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string KullanicilarOnlineKullanicilar = "Online Kullanıcıları Gör";


        [Menu(BagliMenuID = 85000, MenuAdi = "Yetki Grupları", MenuCssClass = "fa fa-list-alt", MenuUrl = "YetkiGruplari/Index", SiraNo = 1)]
        [Role(GorunurAdi = "Yetki Grupları", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string YetkiGruplari = "Yetki Grupları";
        [Menu(BagliMenuID = 85000, MenuAdi = "Birimler", MenuCssClass = "fa fa-list-alt", MenuUrl = "Birimler/Index", SiraNo = 2)]
        [Role(GorunurAdi = "Birimler", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string Birimler = "Birimler";
        [Menu(BagliMenuID = 85000, MenuAdi = "Ünvanlar", MenuCssClass = "fa fa-list-alt", MenuUrl = "Unvanlar/Index", SiraNo = 3)]
        [Role(GorunurAdi = "Ünvanlar", Kategori = "Kullanıcı İşlemleri", Aciklama = "")]
        public const string Unvanlar = "Ünvanlar";

        [Menu(BagliMenuID = 90000, MenuAdi = "Uyruklar", MenuCssClass = "fa fa-list-alt", MenuUrl = "Uyruklar/Index", SiraNo = 1)]
        [Role(GorunurAdi = "Uyruklar", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string Uyruklar = "Uyruklar";
        [Menu(BagliMenuID = 90000, MenuAdi = "Şehirler", MenuCssClass = "fa fa-list-alt", MenuUrl = "Sehirler/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Şehirler", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string Sehirler = "Şehirler";
        [Menu(BagliMenuID = 90000, MenuAdi = "Üniversiteler", MenuCssClass = "fa fa-list-alt", MenuUrl = "Universiteler/Index", SiraNo = 7)]
        [Role(GorunurAdi = "Üniversiteler", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string Universiteler = "Üniversiteler";
        [Menu(BagliMenuID = 90000, MenuAdi = "Enstitüler", MenuCssClass = "fa fa-list-alt", MenuUrl = "Enstituler/Index", SiraNo = 10)]
        [Role(GorunurAdi = "Enstitüler", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string Enstituler = "Enstitüler";

        [Menu(BagliMenuID = 90000, MenuAdi = "Öğrenci Bölümleri", MenuCssClass = "fa fa-list-alt", MenuUrl = "OgrenciBolumleri/Index", SiraNo = 13)]
        [Role(GorunurAdi = "Öğrenci Bölümleri", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string OgrenciBolumleri = "Öğrenci Bölümleri";

        [Menu(BagliMenuID = 90000, MenuAdi = "Öğrenim Tipleri", MenuCssClass = "fa fa-list-alt", MenuUrl = "OgrenimTipleri/Index", SiraNo = 16)]
        [Role(GorunurAdi = "Öğrenim Tipleri", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string OgrenimTipleri = "Öğrenim Tipleri";

        [Menu(BagliMenuID = 90000, MenuAdi = "Anabilim Dalları", MenuCssClass = "fa fa-list-alt", MenuUrl = "Anabilimdallari/Index", SiraNo = 19)]
        [Role(GorunurAdi = "Anabilim Dalları", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string AnabilimDallari = "Anabilim Dalları";

        [Menu(BagliMenuID = 90000, MenuAdi = "Programlar", MenuCssClass = "fa fa-list-alt", MenuUrl = "Programlar/Index", SiraNo = 22)]
        [Role(GorunurAdi = "Programlar", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string Programlar = "Programlar";

       

        [Menu(BagliMenuID = 90000, MenuAdi = "Sınav Tipleri", MenuCssClass = "fa fa-gear", MenuUrl = "SinavTipleri/Index", SiraNo = 28)]
        [Role(GorunurAdi = "Sınav Tipleri", Kategori = "Tanımlamalar", Aciklama = "")]
        public const string SinavTipleri = "Sınav Tipleri";


        [Menu(BagliMenuID = 100000, MenuAdi = "Duyurular", MenuCssClass = "fa fa-bullhorn", MenuUrl = "Duyurular/Index", SiraNo = 1)]
        [Role(GorunurAdi = "Duyurular", Kategori = "Sistem", Aciklama = "")]
        public const string Duyurular = "Duyurular";

        [Menu(BagliMenuID = 100000, MenuAdi = "Gelen Mesajlar", MenuCssClass = "fa fa-envelope", MenuUrl = "Mesajlar/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Gelen Mesajlar", Kategori = "Sistem", Aciklama = "")]
        public const string Mesajlar = "Gelen Mesajlar";
        [Role(GorunurAdi = "Gelen Mesajlar Sil", Kategori = "Sistem", Aciklama = "")]
        public const string MesajlarSil = "Gelen Mesajlar Sil";

        [Menu(BagliMenuID = 100000, MenuAdi = "Mail İşlemleri", MenuCssClass = "fa fa-envelope", MenuUrl = "MailIslemleri/Index", SiraNo = 7)]
        [Role(GorunurAdi = "Mail İşlemleri", Kategori = "Sistem", Aciklama = "")]
        public const string MailIslemleri = "Mail İşlemleri";

        [Role(GorunurAdi = "Mail Gönder", Kategori = "Sistem", Aciklama = "")]
        public const string MailGonder = "Mail Gönder";
        [Menu(BagliMenuID = 100000, MenuAdi = "Mail Şablonları", MenuCssClass = "fa fa-pencil", MenuUrl = "MailSablonlari/Index", SiraNo = 10)]
        [Role(GorunurAdi = "Mail Şablonları", Kategori = "Sistem", Aciklama = "")]
        public const string MailSablonlari = "Mail Şablonları";

        [Menu(BagliMenuID = 100000, MenuAdi = "Mail Şablonları (Sistem)", MenuCssClass = "fa fa-gear", MenuUrl = "MailSablonlariSistem/Index", SiraNo = 13)]
        [Role(GorunurAdi = "Mail Şablonları (Sistem)", Kategori = "Sistem", Aciklama = "")]
        public const string MailSablonlariSistem = "Mail Şablonları (Sistem)";


        [Menu(BagliMenuID = 100000, MenuAdi = "Mesaj Kategorileri", MenuCssClass = "fa fa-pencil", MenuUrl = "MesajKategorileri/Index", SiraNo = 16)]
        [Role(GorunurAdi = "Mesaj Kategorileri", Kategori = "Sistem", Aciklama = "")]
        public const string MesajlarKategorileri = "Mesaj Kategorileri";

        [Menu(BagliMenuID = 100000, MenuAdi = "Anketler", MenuCssClass = "fa fa-pencil", MenuUrl = "Anketler/Index", SiraNo = 19)]
        [Role(GorunurAdi = "Anketler", Kategori = "Sistem", Aciklama = "")]
        public const string Anketler = "Anketler";

        [Role(GorunurAdi = "SSS Kayıt", Kategori = "Sistem", Aciklama = "")]
        public const string SssKayit = "SSSKayit";

        [Menu(BagliMenuID = 100000, MenuAdi = "Sistem Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "SistemAyarlari/Index", SiraNo = 22)]
        [Role(GorunurAdi = "Sistem Ayarları", Kategori = "Sistem", Aciklama = "")]
        public const string SistemAyarlari = "Sistem Ayarları";

        [Menu(BagliMenuID = 100000, MenuAdi = "Sistem Bilgilendirme", MenuCssClass = "fa fa-envelope", MenuUrl = "SistemBilgilendirme/Index", SiraNo = 25)]
        [Role(GorunurAdi = "Sistem Bilgilendirme", Kategori = "Sistem", Aciklama = "")]
        public const string SistemBilgilendirme = "Sistem Bilgilendirme"; 


    }
}