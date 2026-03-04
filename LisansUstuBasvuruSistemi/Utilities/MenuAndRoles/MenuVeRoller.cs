using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Utilities.MenuAndRoles
{
    public class SystemMenu : IMenu
    {

        [Menu(MenuID = 65000, MenuAdi = SalonRezervasyonMenuName, MenuCssClass = "fa fa-file-text-o", MenuUrl = "", YetkisizErisim = false, YetkiliEnstituler = "010,020", SiraNo = 3)]
        public const string SalonRezervasyonMenuName = "Salon Rezervasyonu";

        [Menu(MenuID = 75000, MenuAdi = BelgeTalepMenuName, MenuCssClass = "fa fa-file-text-o", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010", SiraNo = 5)]
        public const string BelgeTalepMenuName = "Belge Talep";

        [Menu(MenuID = 77000, MenuAdi = TalepIslemleriMenuName, MenuCssClass = "fa fa-file-text-o", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010,020,030", SiraNo = 6)]
        public const string TalepIslemleriMenuName = "Talep İşlemleri";

        [Menu(MenuID = 80000, MenuAdi = BasvuruIslemleriMenuName, MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", YetkisizErisim = false, YetkiliEnstituler = "010,020", SiraNo = 7)]
        public const string BasvuruIslemleriMenuName = "Lisansüstü Başvuru";

        [Menu(MenuID = 82300, MenuAdi = TdoIslemleriMenuName, MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010,020,030", SiraNo = 12)]
        public const string TdoIslemleriMenuName = "Tez Danışman/İkinci Danışman/Tez Konu Değişiklik Öneri";

        [Menu(MenuID = 82500, MenuAdi = YeterlikIslemleriMenuName, MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010,020,030", SiraNo = 13)]
        public const string YeterlikIslemleriMenuName = "Yeterlik İşlemleri";

        [Menu(MenuID = 83300, MenuAdi = TiIslemleriMenuName, MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010,020,030", SiraNo = 15)]
        public const string TiIslemleriMenuName = "Tez İzleme İşlemleri";

        [Menu(MenuID = 83500, MenuAdi = MezuniyetIslemleriMenuName, MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010,020,030", SiraNo = 16)]
        public const string MezuniyetIslemleriMenuName = "Mezuniyet İşlemleri";

        [Menu(MenuID = 83600, MenuAdi = DonemProjesiIslemleriMenuName, MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010,020,030", SiraNo = 16)]
        public const string DonemProjesiIslemleriMenuName = "Dönem Projesi İşlemleri";

        [Menu(MenuID = 83800, MenuAdi = KayitSilmeIslemleriMenuName, MenuCssClass = "fa fa-graduation-cap", MenuUrl = "", YetkisizErisim = true, YetkiliEnstituler = "010,020,030", SiraNo = 17)]
        public const string KayitSilmeIslemleriMenuName = "Kayıt Silme İşlemleri";


        [Menu(MenuID = 84000, MenuAdi = RaporIslemleriMenuName, MenuCssClass = "fa fa-bar-chart-o", MenuUrl = "", YetkiliEnstituler = "010,020,030", SiraNo = 18)]
        public const string RaporIslemleriMenuName = "Rapor İşlemleri";

        [Menu(MenuID = 85000, MenuAdi = KullaniciIslemleriMenuName, MenuCssClass = "fa fa-group", MenuUrl = "", YetkiliEnstituler = "010,020,030", SiraNo = 21)]
        public const string KullaniciIslemleriMenuName = "Kullanıcı İşlemleri";

        [Menu(MenuID = 90000, MenuAdi = TanimlamalarMenuName, MenuCssClass = "fa fa-gears", MenuUrl = "", YetkiliEnstituler = "010,020,030", SiraNo = 24)]
        public const string TanimlamalarMenuName = "Tanımlamalar";

        [Menu(MenuID = 100000, MenuAdi = SistemMenuName, MenuCssClass = "fa fa-desktop", MenuUrl = "", YetkiliEnstituler = "010,020,030", SiraNo = 27)]
        public const string SistemMenuName = "Sistem";


    }
    public class RoleNames : IRoleName, IMenu
    {

        [Menu(BagliMenuID = 65000, MenuAdi = "Gelen Rezervasyonlar", MenuCssClass = "fa fa-file-text-o", MenuUrl = "SRGelenTalepler/Index", SiraNo = 2)]
        [Role(GorunurAdi = "Gelen Rezervasyon Talepleri", Kategori = SystemMenu.SalonRezervasyonMenuName, Aciklama = "Bu yetki ile kişi gelen rezervasyonlar menüsünü ve bu menüden tüm Tez Sınavı,Tez İzleme Komite, Tez Savunma Önerisi toplantılarını görebilemektedir.")]
        public const string SrGelenTalepler = "Gelen Rezervasyon Talepleri";
        [Role(GorunurAdi = "Rezervasyon Talebini Onayla", Kategori = SystemMenu.SalonRezervasyonMenuName, Aciklama = "Bu yetki ile kişi gelen Rezervasyon Talepleri menüsünden yapılan rezervasyonları onaylayabilmektedir. ")]
        public const string SrTalepDuzelt = "Rezervasyon Talebi Düzelt";
        [Menu(BagliMenuID = 65000, MenuAdi = "Resmi Tatiller", MenuCssClass = "fa fa-list-alt", MenuUrl = "SROzelTanimlar/Index", SiraNo = 3)]
        [Role(GorunurAdi = "Resmi Tatiller", Kategori = SystemMenu.SalonRezervasyonMenuName, Aciklama = "Bu yetki ile Rezervasyon talepleri için Resmi Tatil tanımlamaları yapılabilmektedir.")]
        public const string SrOzelTanimlar = "Özel Tanımlar";
        [Menu(BagliMenuID = 65000, MenuAdi = "Salon Tanımları", MenuCssClass = "fa fa-list-alt", MenuUrl = "SRSalonlar/Index", SiraNo = 5)]
        [Role(GorunurAdi = "Salon Tanımları", Kategori = SystemMenu.SalonRezervasyonMenuName, Aciklama = "Bu yetki Salon Tanımları menüsüne ulaşım için gereklidir.")]
        public const string SrSalonlar = "Salon Rezervasyon Salonlar";
        [Role(GorunurAdi = "Salon Tanımları Kayıt", Kategori = SystemMenu.SalonRezervasyonMenuName, Aciklama = "Bu yetki Salon Tanımları menüsünede kayıt ve silme işlemleri için gereklidir.")]
        public const string SrSalonlarKayıt = "Salon Tanımları Kayıt";
        [Menu(BagliMenuID = 65000, MenuAdi = "Talep Tipleri", MenuCssClass = "fa fa-list-alt", MenuUrl = "SRTalepTipleri/Index", SiraNo = 6)]
        [Role(GorunurAdi = "Talep Tipleri", Kategori = SystemMenu.SalonRezervasyonMenuName, Aciklama = "Salon rezervasyon işleminin hangi işlem için gerçekleştiği ayırt eden türlerin tanımlanması yapılabilmektedir.")]
        public const string SrTalepTipleri = "Talep Tipleri";


        [Menu(BagliMenuID = 75000, MenuAdi = "Belge Talebi Yap", MenuCssClass = "fa fa-file-text-o", MenuUrl = "BelgeTalebi/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string BelgeTalebiYap = "Belge Talebi Yap";
        [Menu(BagliMenuID = 75000, MenuAdi = "Gelen Belge Talepleri", MenuCssClass = "fa fa-file-text-o", MenuUrl = "GelenBelgeTalepleri/Index", SiraNo = 2)]
        [Role(GorunurAdi = "Gelen Belge Talepleri", Kategori = SystemMenu.BelgeTalepMenuName, Aciklama = "Bu yetki öğrenciler tarafından yapılan belge talebi işlemlerinin gelen belge talebi menüsünde görülmesini sağlar.")]
        public const string GelenBelgeTalepleri = "Gelen Belge Talepleri";
        [Role(GorunurAdi = "Belge Talebi Düzelt", Kategori = SystemMenu.BelgeTalepMenuName, Aciklama = "Bu yetki öğrenciler tarafından yapılan belge talebi işlemlerinin gelen belge talebi menüsü üzerinden düzeltme işlemi yapılabilmesini sağlar.")]
        public const string BelgeTalebiDuzelt = "Belge Talebi Düzelt";
        [Role(GorunurAdi = "Belge Talebi Sil", Kategori = SystemMenu.BelgeTalepMenuName, Aciklama = "Bu yetki öğrenciler tarafından yapılan belge talebi işlemlerinin gelen belge talebi menüsü üzerinden silme işlemi yapılabilmesini sağlar.")]
        public const string BelgeTalebiSil = "Belge Talebi Sil";
        [Menu(BagliMenuID = 75000, MenuAdi = "Belge Tip Detay", MenuCssClass = "fa fa-gear", MenuUrl = "BelgeTipDetay/Index", SiraNo = 3)]
        [Role(GorunurAdi = "Belge Tip Detay", Kategori = SystemMenu.BelgeTalepMenuName, Aciklama = "Bu yetki Belge Tip Detay menüsüne erişim ve Kayıt/Düzeltme yetkisi sağlar.")]
        public const string BelgeTipDetay = "Belge Tip Detay";
        [Menu(BagliMenuID = 75000, MenuAdi = "Belge Tipleri", MenuCssClass = "fa fa-list-alt", MenuUrl = "BelgeTipleri/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Belge Tipleri", Kategori = SystemMenu.BelgeTalepMenuName, Aciklama = "Bu yetki Belge Tipleri menüsüne Erişim yetkisi sağlar.")]
        public const string BelgeTipleri = "Belge Tipleri";
        [Role(GorunurAdi = "Belge Tipleri Kayıt", Kategori = SystemMenu.BelgeTalepMenuName, Aciklama = "Bu yetki Belge Tipleri menüsünde Kayıt/Düzeltme yetkisi sağlar.")]
        public const string BelgeTipleriKayıt = "Belge Tipleri Kayıt";
        [Role(GorunurAdi = "Belge Tipleri Sil", Kategori = SystemMenu.BelgeTalepMenuName, Aciklama = "Bu yetki Belge Tipleri menüsünde Silme yetkisi sağlar.")]
        public const string BelgeTipleriSil = "Belge Tipleri Sil";
        [Menu(BagliMenuID = 75000, MenuAdi = "Belge Talep Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "BelgeTalepAyar/Index", SiraNo = 5)]
        [Role(GorunurAdi = "Belge Talep Ayarları", Kategori = SystemMenu.BelgeTalepMenuName, Aciklama = "Bu yetki Belge Talep Ayarları menüsüne Erişim ve Kayıt/Düzeltme yetkisi sağlar.")]
        public const string BelgeTalepAyarları = "Belge Talep Ayarları";

        [Menu(BagliMenuID = 77000, MenuAdi = "Talep Yap", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TalepYap/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string TalepYap = "Talep Yap";
        [Menu(BagliMenuID = 77000, MenuAdi = "Gelen Talepler", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TalepGelenTalepler/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Gelen Talepler", Kategori = SystemMenu.TalepIslemleriMenuName, Aciklama = "Bu yetki Gelen Talepler menüsüne Erişim yetkisi sağlar.")]
        public const string GelenTalepler = "Gelen Talepler";
        [Role(GorunurAdi = "Gelen Talep Kayıt", Kategori = SystemMenu.TalepIslemleriMenuName, Aciklama = "Bu yetki Gelen Talepler menüsünde Kayıt/Düzeltme sağlar.")]
        public const string GelenTalepKayit = "Gelen Talep Kayıt";
        [Role(GorunurAdi = "Gelen Talep Sil", Kategori = SystemMenu.TalepIslemleriMenuName, Aciklama = "Bu yetki Gelen Talepler menüsünde Silme yetkisi sağlar.")]
        public const string GelenTalepSil = "Gelen Talep Sil";
        [Menu(BagliMenuID = 77000, MenuAdi = "Talep Süreci", MenuCssClass = "fa fa-clock-o", MenuUrl = "TalepSureci/Index", SiraNo = 7)]
        [Role(GorunurAdi = "Talep Süreci", Kategori = SystemMenu.TalepIslemleriMenuName, Aciklama = "Bu yetki Talep Süreci menüsüne Erişim ve Kayıt/Düzeltme sağlar.")]
        public const string TalepSureci = "Talep Süreci";

        [Menu(BagliMenuID = 80000, MenuAdi = "Lisansüstü Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "GelenBasvurular/Index", SiraNo = 3)]
        [Role(GorunurAdi = "Lisansüstü Başvurular", Kategori = SystemMenu.BasvuruIslemleriMenuName, Aciklama = "")]
        public const string GelenBasvurular = "Gelen Başvurular";
        [Menu(BagliMenuID = 80000, MenuAdi = "Yatay Geçiş Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "YYDGelenBasvurular/Index", SiraNo = 6)]
        [Role(GorunurAdi = "Yatay Geçiş Başvurular", Kategori = SystemMenu.BasvuruIslemleriMenuName, Aciklama = "")]
        public const string YydGelenBasvurular = "YYD Gelen Başvurular";
        [Menu(BagliMenuID = 80000, MenuAdi = "Yeni Mezun Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "YGGelenBasvurular/Index", SiraNo = 10)]
        [Role(GorunurAdi = "Yeni Mezun Başvurular", Kategori = SystemMenu.BasvuruIslemleriMenuName, Aciklama = "")]
        public const string YgGelenBasvurular = "YG Gelen Başvurular";

        [Menu(BagliMenuID = 82500, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "Yeterlik/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string YeterlikBasvuru = "Yeterlik Basvuru";
        [Menu(BagliMenuID = 82500, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "YeterlikGelenBasvurular/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Gelen Başvurular", Kategori = SystemMenu.YeterlikIslemleriMenuName, Aciklama = "Bu yetki Gelen Yeterlik Başvuruları menüsüne Erişim yetkisi sağlar.", SiraNo = 7)]
        public const string YeterlikGelenBasvurular = "Yeterlik Gelen Başvurular";

        [Role(GorunurAdi = "Danışmanı Olunan Öğrenciler Görülsün", Kategori = SystemMenu.YeterlikIslemleriMenuName, Aciklama = "Kullanıcı sadece danışmanı olduğu öğrencilerin başvurularını görebilir.", SiraNo = 8)]
        public const string YeterlikDanismanYetkisi = "Yeterlik Danışman Yetkisi";
        [Role(GorunurAdi = "Yetkili Program Öğrencileri Görülsün ", Kategori = SystemMenu.YeterlikIslemleriMenuName, Aciklama = "Kullanıcı sadece yetkili olduğu programlardaki öğrencilerin başvurularını görebilir.", SiraNo = 9)]
        public const string YeterlikProgramYetkisi = "Yeterlik Program Yetkisi";
        [Role(GorunurAdi = "Tüm Başvurular ve Öğrenciler Görülsün", Kategori = SystemMenu.YeterlikIslemleriMenuName, Aciklama = "Kullanıcı tüm öğrencilerin başvurularını görebilir. Danışman ve Program yetkilerini de kapsar.", SiraNo = 10)]
        public const string YeterlikTumBasvurulariGormeYetkisi = "Yeterlik Tum Basvuruları Görme Yetkisi";

        [Role(GorunurAdi = "Gelen Başvurular Kayıt Yetkisi", Kategori = SystemMenu.YeterlikIslemleriMenuName, Aciklama = "Bu yetki Gelen Yeterlik Başvuruları menüsünde Kayıt/Düzeltme yetkisi sağlar.", SiraNo = 13)]
        public const string YeterlikGelenBasvurularKayit = "Yeterlik Gelen Başvurular Kayit";
        [Role(GorunurAdi = "Başvuru Onay Yetkisi", Kategori = SystemMenu.YeterlikIslemleriMenuName, Aciklama = "Bu yetki Gelen Yeterlik Başvuruları menüsünde Onay yetkisi sağlar.", SiraNo = 15)]
        public const string YeterlikBasvuruOnayYetkisi = "Yeterlik Başvuru Onay Yetkisi";
        [Role(GorunurAdi = "Abd/Jüri Onayı Düzeltme Yetkisi", Kategori = SystemMenu.YeterlikIslemleriMenuName, Aciklama = "Bu yetki Gelen Yeterlik Başvuruları menüsünde Abd veya Jüri üyelerinin değerlendirmelerinde değişiklik yapabilme yetkisi sağlar.", SiraNo = 17)]
        public const string YeterlikAbdJuriOnayDuzeltme = "Yeterlik Abd/Jüri Onay Düzeltme";


        [Menu(BagliMenuID = 82500, MenuAdi = "Yeterlik Süreci", MenuCssClass = "fa fa-clock-o", MenuUrl = "YeterlikSureci/Index", SiraNo = 7)]
        [Role(GorunurAdi = "Yeterlik Süreci", Kategori = SystemMenu.YeterlikIslemleriMenuName, Aciklama = "Bu yetki Yeterlik Süreci menüsüne Erişim yetkisi sağlar.")]
        public const string YeterlikSureci = "Yeterlik Süreci";
        [Role(GorunurAdi = "Yeterlik Süreci Kayıt", Kategori = SystemMenu.YeterlikIslemleriMenuName, Aciklama = "Bu yetki Yeterlik Süreci menüsünde Kayıt/Düzeltme yetkisi sağlar.")]
        public const string YeterlikSureciKayıt = "Yeterlik Süreci Kayıt";
        [Role(GorunurAdi = "Yeterlik Süreci Sil", Kategori = SystemMenu.YeterlikIslemleriMenuName, Aciklama = "Bu yetki Yeterlik Süreci menüsüne Silme yetkisi sağlar.")]
        public const string YeterlikSureciSil = "Yeterlik Süreci Sil";

        [Menu(BagliMenuID = 82300, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TDOBasvuru/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string TdoBasvuru = "TDO Başvuru";
        [Menu(BagliMenuID = 82300, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TDOGelenBasvurular/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Gelen Başvurular", Kategori = SystemMenu.TdoIslemleriMenuName, Aciklama = "Bu yetki Tez/Danışman Öneri Gelen Başvurular menüsüne Erişim yetkisi sağlar.", SiraNo = 1)]
        public const string TdoGelenBasvuru = "TDO Gelen Başvurular";
        [Role(GorunurAdi = "Gelen Başvurular Kayıt", Kategori = SystemMenu.TdoIslemleriMenuName, Aciklama = "Bu yetki Tez/Danışman Öneri Gelen Başvurular menüsüne Başvuru Düzeltme yetkisi sağlar.", SiraNo = 4)]
        public const string TdoGelenBasvuruKayit = "TDO Gelen Başvurular Kayıt";
        [Role(GorunurAdi = "Gelen Başvurular Sil", Kategori = SystemMenu.TdoIslemleriMenuName, Aciklama = "Bu yetki Tez/Danışman Öneri Gelen Başvurular menüsüne Başvuru Silme yetkisi sağlar.", SiraNo = 9)]
        public const string TdoGelenBasvuruSil = "TDO Gelen Başvurular Sil";
        [Role(GorunurAdi = "Form Düzeltme Yetkisi", Kategori = SystemMenu.TdoIslemleriMenuName, Aciklama = "Bu yetki TDO Form Oluşturma/Düzeltme yetkisini sağlar. Öğrenci haricinde bu yetkiye sahip olması gerekenlere verilir.", SiraNo = 14)]
        public const string TdoFormOlusturmaYetkisi = "TDO Form Oluşturma Yetkisi";
        [Role(GorunurAdi = "Danışman Onay Yetkisi", Kategori = SystemMenu.TdoIslemleriMenuName, Aciklama = "Bu yetki Tez Danışmanı, Tez Dili, Tez Başlığı Değişikliği İşlemi yapıldıktan sonra Danışman Onayı işlemi yapılabilmesi için verilmesi gereken bir yetkidir.", SiraNo = 19)]
        public const string TdoDanismanOnayYetkisi = "TDO Danisman Onay Yetkisi";
        [Role(GorunurAdi = "Eyk'ya Gönderim Yetkisi", Kategori = SystemMenu.TdoIslemleriMenuName, Aciklama = "Bu yetki Danışman Onayından sonra Eyk'ya Gönderim işlemi yapabilmeyi sağlayan yetkidir.", SiraNo = 24)]
        public const string TdoEykyaGonderimYetkisi = "TDO EYK ya Gönderim Yetkisi";
        [Role(GorunurAdi = "Eyk'ya Hazırlandı Yetkisi", Kategori = SystemMenu.TdoIslemleriMenuName, Aciklama = "Bu yetki Eyk'ya Gönderim yapıldıktan sonra Eyk'ya Hazırlık işlemi yapabilmeyi sağlayan yetkidir.", SiraNo = 29)]
        public const string TdoEykyaHazirlandiYetkisi = "TDO EYK ya Hazırlandı Yetkisi";
        [Role(GorunurAdi = "Eyk'da Onay Yetkisi", Kategori = SystemMenu.TdoIslemleriMenuName, Aciklama = "Bu yetki Eyk'ya Hazırlık yapıldıktan sonra Eyk'da Onay işlemi yapabilmeyi sağlayan yetkidir.", SiraNo = 34)]
        public const string TdoEykdaOnayYetkisi = "TDO EYK da Onay Yetkisi";
        [Menu(BagliMenuID = 82300, MenuAdi = "TDO Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "TDOAyarlar/Index", SiraNo = 7)]
        [Role(GorunurAdi = "Tez Danışman Öneri Ayarları", Kategori = SystemMenu.TdoIslemleriMenuName, Aciklama = "Bu yetki TDO Ayarları menüsüne Erişim ve Kayıt/Düzeltme yetkisi sağlar.", SiraNo = 39)]
        public const string TdoAyarlari = "TDO Ayarları";

        [Menu(BagliMenuID = 83300, MenuAdi = "Jüri Önerileri", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TiJuriOnerileriGb/Index", SiraNo = 3)]
        [Role(GorunurAdi = "Gelen Jüri Önerileri", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Jüri Önerileri menüsüne Erişim yetkisi sağlar.", SiraNo = 1)]
        public const string TiJuriOnerileriGb = "Tez İzleme Jüri Önerisileri";
        [Role(GorunurAdi = "Jüri Önerisini Öğrenci Adına Yap", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Jüri Öneri işlemini öğrenci adına yapabilmeyi sağlar.", SiraNo = 3)]
        public const string TiJuriOnerileriOgrenciAdina = "Jüri Önerisini Öğrenci Adına Yap";
        [Role(GorunurAdi = "Eyk'ya Gönderme Yetkisi", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Jüri Önerilerinde Danışman Onayından sonra Eyk'ya Gönderim işlemi yapabilmeyi sağlayan yetkidir.", SiraNo = 5)]
        public const string TiJuriOnerileriEykYaGonder = "Jüri Önerisi Eyk ya Gönderme Yetkisi";
        [Role(GorunurAdi = "Eyk'ya Hazırlandı Yetkisi", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Jüri Önerilerinde Eyk'ya Gönderim yapıldıktan sonra Eyk'ya Hazırlık işlemi yapabilmeyi sağlayan yetkidir.", SiraNo = 9)]
        public const string TiJuriOnerileriEykyaHazirlandiYetkisi = "Jüri Önerisi Eyk'ya Hazırlandı Yetkisi";
        [Role(GorunurAdi = "Eyk'da Onay Yetkisi", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Jüri Önerilerinde Eyk'ya Hazırlık yapıldıktan sonra Eyk'da Onay işlemi yapabilmeyi sağlayan yetkidir.", SiraNo = 14)]
        public const string TiJuriOnerileriEykDaOnay = "Jüri Önerisi Eyk da Onay Yetkisi";

        [Menu(BagliMenuID = 83300, MenuAdi = "Tez Öneri Giriş", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TosBasvuru/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 10)]
        public const string TosBasvuru = "Tos Basvuru";
        [Menu(BagliMenuID = 83300, MenuAdi = "Tez Öneri Başvuruları", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TosGelenBasvurular/Index", SiraNo = 11)]
        [Role(GorunurAdi = "Tez Öneri Başvuruları", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Tez Öneri Başvuruları menüsüne Erişim yetkisi sağlar.")]
        public const string TosGelenBasvuru = "Tos Gelen Başvurular";
        [Role(GorunurAdi = "Tez Öneri Başvuruları Kayıt", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Enstitü yetkililerine verilmelidir.")]
        public const string TosGelenBasvuruKayit = "Tos Gelen Başvurular Kayıt";
        [Role(GorunurAdi = "Tez Öneri Başvuruları Sil", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Enstitü yetkililerine verilmelidir.")]
        public const string TosGelenBasvuruSil = "Tos Gelen Başvurular Sil";
        [Role(GorunurAdi = "Tez Öneri Savunma Bitiş Tarih Düzeltme", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Öğrencilerin savunma başvurusu yapabileceği tarih aralığını belirleyebilmek için gereklidir.")]
        public const string TosSavunmaBitisTarihDuzeltme = "Tez Öneri Savunma Bitiş Tarih Düzeltme";
        [Role(GorunurAdi = "Tez Öneri Toplantı Talebi Yap", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Toplantı Talebi yapılabilmesini sağlayan yetkidir.")]
        public const string TosToplantiTalebiYap = "Tos Toplantı Talebi Yap";
        [Role(GorunurAdi = "Tez Öneri Degerlendirme Yap", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Savunma sınavından sonra değerlendirme işlemi yapılabilmesini sağlayan yetkidir.")]
        public const string TosDegerlendirmeYap = "Tez Öneri Savunma Degerlendirme Yap";
        [Role(GorunurAdi = "Tez Öneri Degerlendirme Düzeltme", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Savunma sınavından sonra değerlendirme işlemlerinin değiştirilebilmesini sağlayan yetkidir. Bu yetki ile toplantı tarihi günümüz tarihi öncesi olsa bile değerlendirme yapılabilir.")]
        public const string TosDegerlendirmeDuzeltme = "Tez Öneri Savunma Degerlendirme Düzeltme";

        [Menu(BagliMenuID = 83300, MenuAdi = "Ara Rapor Giriş", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TIBasvuru/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 15)]
        public const string TiBasvuru = "Ti Basvuru";
        [Menu(BagliMenuID = 83300, MenuAdi = "Ara Rapor Başvuruları", MenuCssClass = "fa fa-file-text-o", MenuUrl = "TIGelenBasvurular/Index", SiraNo = 17)]
        [Role(GorunurAdi = "Ara Rapor Başvuruları", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Ara Rapor Başvuruları menüsüne Erişim yetkisi sağlar.")]
        public const string TiGelenBasvuru = "TI Gelen Başvurular";
        [Role(GorunurAdi = "Ara Rapor Başvuruları Kayıt", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Ara Rapor Başvuruları Düzeltme/Silme veya yeni Ara Rapor oluşturma işlemi yapabilmeyi sağlayan yetkidir.")]
        public const string TiGelenBasvuruKayit = "TI Gelen Başvurular Kayıt";
        [Role(GorunurAdi = "Ara Rapor Toplantı Talebi Yap", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Toplantı Talebi yapılabilmesini sağlayan yetkidir.")]
        public const string TiToplantiTalebiYap = "TI Toplantı Talebi Yap";
        [Role(GorunurAdi = "Ara Rapor Degerlendirme Yap", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki yeni Ara Rapor oluşturma işlemi yapabilmeyi sağlayan yetkidir.")]
        public const string TiTezDegerlendirmeYap = "Tez İzleme Tez Degerlendirme Yap";
        [Role(GorunurAdi = "Ara Rapor Degerlendirme Düzeltme", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Ara raporlarda Düzeltme/Silme işlemi yapabilmeyi sağlayan yetkidir.")]
        public const string TiTezDegerlendirmeDuzeltme = "Tez İzleme Tez Degerlendirme Düzeltme";
        [Menu(BagliMenuID = 83300, MenuAdi = "Tez İzleme Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "TIAyarlar/Index", SiraNo = 30)]
        [Role(GorunurAdi = "Tez İzleme Ayarları", Kategori = SystemMenu.TiIslemleriMenuName, Aciklama = "Bu yetki Tez İzleme Ayarları menüsüne Erişim ve Kayıt işlemini yapabilmeyi sağlayan yetkidir.")]
        public const string TiAyarlari = "TI Ayarları";

        [Menu(BagliMenuID = 83500, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "Mezuniyet/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string MezuniyetBasvuru = "Mezuniyet Basvuru";
        [Menu(BagliMenuID = 83500, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "MezuniyetGelenBasvurular/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Gelen Başvurular", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Mezuniyet Gelen Başvurular menüsünü görmeyi sağlayan yetkidir.", SiraNo = 1)]
        public const string MezuniyetGelenBasvurular = "Mezuniyet Gelen Başvurular";
        [Role(GorunurAdi = "Başvuru Düzeltme Yetkisi", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Gelen Başvurular üzerinde düzeltme yapabilmeyi sağlayan yetkidir.", SiraNo = 5)]
        public const string MezuniyetGelenBasvurularKayit = "Mezuniyet Gelen Başvurular Kayıt";
        [Role(GorunurAdi = "Jüri Öneri Formu Oluşturma Yetkisi", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Jüri Öneri formunu oluşturabilmeyi sağlayan yetkidir.", SiraNo = 10)]
        public const string MezuniyetGelenBasvurularJuriOneriFormuKayit = "Gelen Başvurular Juri Öneri Formu Kayıt";
        [Role(GorunurAdi = "Jüri Önerisi Eyk'ya Gönderme Yetkisi", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Danışman tarafından onaylanan Jüri Öneri formunu Eyk'ya gönderebilmeyi sağlayan yetkidir.", SiraNo = 15)]
        public const string MezuniyetGelenBasvurularJuriOneriFormuOnay = "Gelen Başvurular Juri Öneri Formu Onay";
        [Role(GorunurAdi = "Jüri Önerisi Eyk'ya Hazırlandı Yetkisi", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Eyk'ya Gönderim yapıldıktan sonra Eyk'ya Hazırlık işlemi yapabilmeyi sağlayan yetkidir.", SiraNo = 20)]
        public const string MezuniyetGelenBasvurularEykyaHazirlandiYetkisi = "Gelen Başvurular Jüri Öneri Formu Eyk ya Hazırlandı Yetkisi";
        [Role(GorunurAdi = "Jüri Önerisi Eyk'da Onay Yetkisi", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Eyk'ya gönderilen Jüri Öneri formunu Eyk'da onaylamayı sağlayan yetkidir.", SiraNo = 25)]
        public const string MezuniyetGelenBasvurularJuriOneriFormuEykOnay = "Gelen Başvurular Juri Öneri Formu Eyk Onay";
        [Role(GorunurAdi = "Tez Sınavı Oluşturma", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Eyk'da onaylama işleminden sonra Rezervasyon işlemini yapabilmeyi sağlayan yetkidir.", SiraNo = 30)]
        public const string MezuniyetGelenBasvurularSrTalebiYap = "Gelen Başvurular SR Talebi Yap";
        [Role(GorunurAdi = "Tez Kontrol Yetkisi", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki öğrenci tarafından yüklenen tezlerin kontrol işlemini yapabilmeyi sağlayan yetkidir.", SiraNo = 35)]
        public const string MezuniyetGelenBasvurularTezKontrol = "Gelen Başvurular Tez Kontrol";
        [Role(GorunurAdi = "Tez Kontrol Yetkilisi Atama", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Öğrenci tarafından yüklenen tezlerin kontrolünü yapması gereken yetkiliyi belirlemeye yarayan yetkidir.", SiraNo = 40)]
        public const string MezuniyetGelenBasvurularTezKontrolYetkiliAtama = "Gelen Başvurular Tez Kontrol Yetkili Atama";
        [Role(GorunurAdi = "Tez Teslim Ek Süre Verme Yetkisi", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki tez teslimi için belirlenen süre kriterini değiştirebilmeyi sağlayan yetkidir.", SiraNo = 45)]
        public const string MezuniyetGelenBasvurularTtEkSure = "Gelen Başvurular Tez Teslim Ek Süre";
        [Menu(BagliMenuID = 83500, MenuAdi = "Mezuniyet Süreci", MenuCssClass = "fa fa-clock-o", MenuUrl = "MezuniyetSureci/Index", SiraNo = 7)]
        [Role(GorunurAdi = "Mezuniyet Süreci", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Mezunyet Süreci menüsünü görmeyi sağlayan yetkidir.", SiraNo = 50)]
        public const string MezuniyetSureci = "Mezuniyet Süreci";
        [Role(GorunurAdi = "Mezuniyet Süreci Kayıt", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Mezuniyet Süreçlerinde Düzeltme/Kayıt işlemlerini yapabilmeyi sağlayan yetkidir.", SiraNo = 55)]
        public const string MezuniyetSureciKayıt = "Mezuniyet Süreci Kayıt";
        [Role(GorunurAdi = "Mezuniyet Süreci Sil", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Mezuniyet Süreçlerinde Silme işlemlerini yapabilmeyi sağlayan yetkidir.", SiraNo = 60)]
        public const string MezuniyetSureciSil = "Mezuniyet Süreci Sil";
        [Menu(BagliMenuID = 83500, MenuAdi = "Yönetmelikler", MenuCssClass = "fa fa-file-text-o", MenuUrl = "MezuniyetYonetmelikler/Index", SiraNo = 8)]
        [Role(GorunurAdi = "Yönetmelikler", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Yönetmelikler menüsünü Görme/Düzeltme/Silme işlemlerini yapabilmeyi sağlayan yetkidir.", SiraNo = 65)]
        public const string MezuniyetYonetmelikler = "Mezuniyet Yönetmelikler";
        [Menu(BagliMenuID = 83500, MenuAdi = "Yayın Türleri", MenuCssClass = "fa fa-gear", MenuUrl = "MezuniyetYayinTurleri/Index", SiraNo = 10)]
        [Role(GorunurAdi = "Yayın Türleri", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Yayın Türleri menüsünü Görme/Düzeltme/Silme işlemlerini yapabilmeyi sağlayan yetkidir.", SiraNo = 70)]
        public const string MezuniyetYayinTurleri = "Yayın Türleri";
        [Menu(BagliMenuID = 83500, MenuAdi = "Mezuniyet Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "MezuniyetAyarlar/Index", SiraNo = 15)]
        [Role(GorunurAdi = "Mezuniyet Ayarları", Kategori = SystemMenu.MezuniyetIslemleriMenuName, Aciklama = "Bu yetki Mezuniyet Ayarları menüsünü Görme/Düzeltme işlemlerini yapabilmeyi sağlayan yetkidir.", SiraNo = 75)]
        public const string MezuniyetAyarları = "Mezuniyet Ayarları";



        [Menu(BagliMenuID = 83600, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "DpBasvuru/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string DonemProjesiBasvuru = "Dönem Projesi Basvuru";
        [Menu(BagliMenuID = 83600, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "DpGelenBasvurular/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Gelen Başvurular", Kategori = SystemMenu.DonemProjesiIslemleriMenuName, Aciklama = "Bu yetki Dönem Projesi Gelen Başvurular menüsüne Erişim yetkisi sağlar.", SiraNo = 2)]
        public const string DonemProjesiGelenBasvurular = "Dönem Projesi Gelen Başvurular";
        [Role(GorunurAdi = "Başvuru Yapma Yetkisi", Kategori = SystemMenu.DonemProjesiIslemleriMenuName, Aciklama = "Bu yetki Dönem Projesi başvurusunda bulunabilmeyi sağlayan yetkidir. Öğrenciler bu yetkiye otomatik olarak sahiptirler başvuru için bu yetkiyi vermeye gerek yoktur.", SiraNo = 8)]
        public const string DonemProjesiBasvuruYapmaYetkisi = "Dönem Projesi Başvuru Yapma Yetkisi";
        [Role(GorunurAdi = "Enstitü Başvuru Onay Yetkisi", Kategori = SystemMenu.DonemProjesiIslemleriMenuName, Aciklama = "Bu yetki Gelen Dönem Projesi Başvuruları menüsünde Enstitü Başvuru Onay yetkisi sağlar.", SiraNo = 6)]
        public const string DonemProjesiEnstituBasvuruOnayYetkisi = "Dönem Projesi Enstitü Başvuru Onay Yetkisi";
        [Role(GorunurAdi = "Danışman Başvuru Onay Yetkisi", Kategori = SystemMenu.DonemProjesiIslemleriMenuName, Aciklama = "Bu yetki Gelen Dönem Projesi Başvuruları menüsünde Danışman Başvuru Onay yetkisi sağlar.", SiraNo = 8)]
        public const string DonemProjesiDanismanBasvuruOnayYetkisi = "Dönem Projesi Danışman Başvuru Onay Yetkisi";
        [Role(GorunurAdi = "Sınav Jürisi Oluşturma Yetkisi", Kategori = SystemMenu.DonemProjesiIslemleriMenuName, Aciklama = "Bu yetki Dönem Projesi sınavı için jüri listesi oluşturabilmeyi sağlayan yetkidir.", SiraNo = 10)]
        public const string DonemProjesiJuriOlusturmaYetkisi = "Dönem Projesi Jüri Oluşturma Yetkisi";
        [Role(GorunurAdi = "Sınav Oluşturma Yetkisi", Kategori = SystemMenu.DonemProjesiIslemleriMenuName, Aciklama = "Bu yetki Dönem Projesi sınavı için sınav oluşturabilmeyi sağlayan yetkidir.", SiraNo = 12)]
        public const string DonemProjesiSinaviOlusturmaYetkisi = "Dönem Projesi Sınavı Oluşturma Yetkisi";
        [Role(GorunurAdi = "Jüri Değerlendirmesi Düzeltme Yetkisi", Kategori = SystemMenu.DonemProjesiIslemleriMenuName, Aciklama = "Bu yetki Dönem Projesi sınavından sonra değerlendirme işlemlerinin değiştirilebilmesini sağlayan yetkidir. Bu yetki ile toplantı tarihi günümüz tarihi öncesi olsa bile değerlendirme yapılabilir.", SiraNo = 18)]
        public const string DonemProjesiSinavDegerlendirmeDuzeltme = "Dönem Projesi Sınav Degerlendirme Düzeltme";
        [Role(GorunurAdi = "Eyk'ya Gönderme Yetkisi", Kategori = SystemMenu.DonemProjesiIslemleriMenuName, Aciklama = "Bu yetki Sınav tamamlandıktan sonra Eyk'ya Gönderim işlemi yapabilmeyi sağlayan yetkidir.", SiraNo = 14)]
        public const string DonemProjesiEykYaGonder = "Dönem Projesi Eyk'ya Gönderme Yetkisi";
        [Role(GorunurAdi = "Eyk'ya Hazırlandı Yetkisi", Kategori = SystemMenu.DonemProjesiIslemleriMenuName, Aciklama = "Bu yetki Eyk'ya Gönderim yapıldıktan sonra Eyk'ya Hazırlık işlemi yapabilmeyi sağlayan yetkidir.", SiraNo = 16)]
        public const string DonemProjesiEykYaHazirlandi = "Dönem Projesi Eyk'ya Hazırlandı Yetkisi";
        [Role(GorunurAdi = "Eyk'da Onay Yetkisi", Kategori = SystemMenu.DonemProjesiIslemleriMenuName, Aciklama = "Bu yetki Eyk'ya Hazırlık yapıldıktan sonra Eyk'da Onay işlemi yapabilmeyi sağlayan yetkidir.", SiraNo = 18)]
        public const string DonemProjesiEykDaOnay = "Dönem Projesi Eyk'da Onay Yetkisi";
        [Menu(BagliMenuID = 83600, MenuAdi = "Kriterden Muaf Öğrenciler", MenuCssClass = "fa fa-group", MenuUrl = "DpKriterdenMuafOgrenciler/Index", SiraNo = 13)]
        [Role(GorunurAdi = "Kriterden Muaf Öğrenciler", Kategori = SystemMenu.DonemProjesiIslemleriMenuName, Aciklama = "Bu yetki Dönem Projesi başvurularında Kriterden Muaf Öğrenciler menüsünü Görme/Ekleme/Silme işlemlerini yapabilmeyi sağlayan yetkidir.", SiraNo = 20)]
        public const string DonemProjesiKriterdenMuafOgrenciler = "Dönem Projesi Kriterden Muaf Öğrenciler";
        [Menu(BagliMenuID = 83600, MenuAdi = "Dönem Projesi Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "DpAyarlar/Index", SiraNo = 15)]
        [Role(GorunurAdi = "Dönem Projesi Ayarları", Kategori = SystemMenu.DonemProjesiIslemleriMenuName, Aciklama = "Bu yetki Dönem Projesi Ayarları menüsünü Görme/Düzeltme işlemlerini yapabilmeyi sağlayan yetkidir.", SiraNo = 22)]
        public const string DonemProjesiAyarları = "Dönem Projesi Ayarları";


        [Menu(BagliMenuID = 83800, MenuAdi = "Başvuru", MenuCssClass = "fa fa-file-text-o", MenuUrl = "KsBasvuru/Index", YetkisizErisim = true, AuthenticationControl = "authenticatedControl(this)", SiraNo = 1)]
        public const string KayitSilmeBasvuru = "Kayıt Silme Basvuru";
        [Menu(BagliMenuID = 83800, MenuAdi = "Gelen Başvurular", MenuCssClass = "fa fa-file-text-o", MenuUrl = "KsGelenBasvurular/Index", SiraNo = 2)]
        [Role(GorunurAdi = "Gelen Başvurular", Kategori = SystemMenu.KayitSilmeIslemleriMenuName, Aciklama = "Bu yetki Kayıt Silme Gelen Başvurular menüsüne Erişim yetkisi sağlar.", SiraNo = 1)]
        public const string KayitSilmeGelenBasvurular = "Kayıt Silme Gelen Başvurular";
        [Role(GorunurAdi = "Başvuru Düzeltme Yetkisi", Kategori = SystemMenu.KayitSilmeIslemleriMenuName, Aciklama = "Bu yetki Kayıt Silme başvurularında düzeltme yapabilmeyi sağlayan yetkidir.", SiraNo = 2)]
        public const string KayitSilmeBasvuruDuzeltmeYetkisi = "Kayıt Silme Başvuru Düzeltme Yetkisi";
        [Role(GorunurAdi = "Harç Birimi Başvuru Onay Yetkisi", Kategori = SystemMenu.KayitSilmeIslemleriMenuName, Aciklama = "Bu yetki Gelen Kayıt Silme Başvuruları menüsünde Harç Birimi Başvuru Onay yetkisi sağlar.", SiraNo = 3)]
        public const string KayitSilmeHarcBirimiBasvuruOnayYetkisi = "Kayıt Silme Harç Birimi Başvuru Onay Yetkisi";
        [Role(GorunurAdi = "Kütüphane Birimi Başvuru Onay Yetkisi", Kategori = SystemMenu.KayitSilmeIslemleriMenuName, Aciklama = "Bu yetki Gelen Kayıt Silme Başvuruları menüsünde Kütüphane Birimi Başvuru Onay yetkisi sağlar.", SiraNo = 4)]
        public const string KayitSilmeKutuphaneBirimiBasvuruOnayYetkisi = "Kayıt Silme Kütüphane Birimi Başvuru Onay Yetkisi";
        [Role(GorunurAdi = "Eyk'ya Gönderme Yetkisi", Kategori = SystemMenu.KayitSilmeIslemleriMenuName, Aciklama = "Bu yetki Sınav tamamlandıktan sonra Eyk'ya Gönderim işlemi yapabilmeyi sağlayan yetkidir.", SiraNo = 5)]
        public const string KayitSilmeEykYaGonder = "Kayıt Silme Eyk'ya Gönderme Yetkisi";
        [Role(GorunurAdi = "Eyk'ya Hazırlandı Yetkisi", Kategori = SystemMenu.KayitSilmeIslemleriMenuName, Aciklama = "Bu yetki Eyk'ya Gönderim yapıldıktan sonra Eyk'ya Hazırlık işlemi yapabilmeyi sağlayan yetkidir.", SiraNo = 6)]
        public const string KayitSilmeEykYaHazirlandi = "Kayıt Silme Eyk'ya Hazırlandı Yetkisi";
        [Role(GorunurAdi = "Eyk'da Onay Yetkisi", Kategori = SystemMenu.KayitSilmeIslemleriMenuName, Aciklama = "Bu yetki Eyk'ya Hazırlık yapıldıktan sonra Eyk'da Onay işlemi yapabilmeyi sağlayan yetkidir.", SiraNo = 7)]
        public const string KayitSilmeEykDaOnay = "Kayıt Silme Eyk'da Onay Yetkisi"; 
        [Menu(BagliMenuID = 83800, MenuAdi = "Kayıt Silme Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "KsAyarlar/Index", SiraNo = 3)]
        [Role(GorunurAdi = "Kayıt Silme Ayarları", Kategori = SystemMenu.KayitSilmeIslemleriMenuName, Aciklama = "Bu yetki Kayıt Silme Ayarları menüsünü Görme/Düzeltme işlemlerini yapabilmeyi sağlayan yetkidir.", SiraNo = 8)]
        public const string KayitSilmeAyarları = "Kayıt Silme Ayarları";




        [Menu(BagliMenuID = 84000, MenuAdi = "Lisansüstü Başvuru", MenuCssClass = "fa fa-bar-chart-o", MenuUrl = "RaporlarLUB/Index", SiraNo = 5)]
        [Role(GorunurAdi = "Lisansüstü Başvuru", Kategori = SystemMenu.RaporIslemleriMenuName, Aciklama = "Bu yetki Lisansüstü Başvuru raporunu görmeyi sağlayan yetkidir.")]
        public const string LisansustuBasvuruRapor = "Lisansüstü Başvuru";
        [Menu(BagliMenuID = 84000, MenuAdi = "Belge Talepleri", MenuCssClass = "fa fa-bar-chart-o", MenuUrl = "RaporlarBT/Index", SiraNo = 10)]
        [Role(GorunurAdi = "Belge Talepleri", Kategori = SystemMenu.RaporIslemleriMenuName, Aciklama = "Bu yetki Belge Talepleri raporunu görmeyi sağlayan yetkidir.")]
        public const string BelgeTalepleriRapor = "Belge Talepleri";
        [Menu(BagliMenuID = 84000, MenuAdi = "Anketler", MenuCssClass = "fa fa-bar-chart-o", MenuUrl = "RaporlarAnket/Index", SiraNo = 15)]
        [Role(GorunurAdi = "Anketler", Kategori = SystemMenu.RaporIslemleriMenuName, Aciklama = "Bu yetki Anketler raporunu görmeyi sağlayan yetkidir.")]
        public const string AnketlerRapor = "Anketler";

        [Menu(BagliMenuID = 85000, MenuAdi = "Kullanıcılar", MenuCssClass = "fa fa-list-alt", MenuUrl = "Kullanicilar/Index", SiraNo = 1)]
        [Role(GorunurAdi = "Kullanıcılar", Kategori = SystemMenu.KullaniciIslemleriMenuName, Aciklama = "Bu yetki Kullanıcılar menüsünü görmeyi sağlayan yetkidir.")]
        public const string Kullanicilar = "Kullanıcılar Listesi";
        [Role(GorunurAdi = "Kullanıcı Hesabına Geçme Yetkisi", Kategori = SystemMenu.KullaniciIslemleriMenuName, Aciklama = "Bu yetki bir kullanıcı detayından kullanıcının hesabına geçebilmeyi sağlayan yetkidir.")]
        public const string KullaniciHesabinaGecmeYetkisi = "Kullanıcı Hesabına Geçme Yetkisi";
        [Role(GorunurAdi = "Kullanıcılar İşlem Yetkileri", Kategori = SystemMenu.KullaniciIslemleriMenuName, Aciklama = "Bu yetki bir kullanıcının Yetki Grubunu belirleme ve gerekiyorsa ek yetki verme işlemini sağlayan yetkidir.")]
        public const string KullanicilarIslemYetkileri = "Kullanıcılar İşlem Yetkileri";
        [Role(GorunurAdi = "Kullanıcılar Enstitü Yetkileri", Kategori = SystemMenu.KullaniciIslemleriMenuName, Aciklama = "Bu yetki bir kullanıcının Enstitü yetkilerini belirlemeyi sağlayan yetkidir.")]
        public const string KullanicilarEnstituYetkileri = "Kullanıcılar Enstitü Yetkileri";
        [Role(GorunurAdi = "Kullanıcılar Program Yetkileri", Kategori = SystemMenu.KullaniciIslemleriMenuName, Aciklama = "Bu yetki bir kullanıcının Program yetkilerini düzenlemeyi sağlayan yetkidir.")]
        public const string KullanicilarProgramYetkileri = "Kullanıcılar Program Yetkileri";
        [Role(GorunurAdi = "Kullanıcılar Kayıt", Kategori = SystemMenu.KullaniciIslemleriMenuName, Aciklama = "Bu yetki yeni bir Kullanıcı oluşturma ya da güncelleme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string KullanicilarKayit = "Kullanıcılar Kayıt";
        [Role(GorunurAdi = "Kullanıcılar Sil", Kategori = SystemMenu.KullaniciIslemleriMenuName, Aciklama = "Bu yetki bir kullanıcıyı silebilmek için gerekli olan yetkidir.")]
        public const string KullanicilarSil = "Kullanıcı Sil";
        [Role(GorunurAdi = "Online Kullanıcıları Gör", Kategori = SystemMenu.KullaniciIslemleriMenuName, Aciklama = "Bu yetki online kullanıcıları görebilmek için gerekli olan yetkidir.")]
        public const string KullanicilarOnlineKullanicilar = "Online Kullanıcıları Gör";

        [Menu(BagliMenuID = 85000, MenuAdi = "Yetki Grupları", MenuCssClass = "fa fa-list-alt", MenuUrl = "YetkiGruplari/Index", SiraNo = 1)]
        [Role(GorunurAdi = "Yetki Grupları", Kategori = SystemMenu.KullaniciIslemleriMenuName, Aciklama = "Bu yetki Yetki Grupları menüsünü Görebilme/Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string YetkiGruplari = "Yetki Grupları";
        [Menu(BagliMenuID = 85000, MenuAdi = "Birimler", MenuCssClass = "fa fa-list-alt", MenuUrl = "Birimler/Index", SiraNo = 2)]
        [Role(GorunurAdi = "Birimler", Kategori = SystemMenu.KullaniciIslemleriMenuName, Aciklama = "Bu yetki Birimler menüsünü Görebilme/Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string Birimler = "Birimler";
        [Menu(BagliMenuID = 85000, MenuAdi = "ObsBirimler", MenuCssClass = "fa fa-list-alt", MenuUrl = "ObsBirimler/Index", SiraNo = 2)]
        [Role(GorunurAdi = "Obs Birimleri", Kategori = SystemMenu.KullaniciIslemleriMenuName, Aciklama = "Bu yetki OBS Birimlerini menüsünü Görebilmeyi sağlayan yetkidir.")]
        public const string ObsBirimler = "ObsBirimler";
        [Menu(BagliMenuID = 85000, MenuAdi = "Unvanlar", MenuCssClass = "fa fa-list-alt", MenuUrl = "Unvanlar/Index", SiraNo = 3)]
        [Role(GorunurAdi = "Unvanlar", Kategori = SystemMenu.KullaniciIslemleriMenuName, Aciklama = "Bu yetki Unvanlar menüsünü Görebilme/Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string Unvanlar = "Ünvanlar";
        [Menu(BagliMenuID = 90000, MenuAdi = "Uyruklar", MenuCssClass = "fa fa-list-alt", MenuUrl = "Uyruklar/Index", SiraNo = 1)]
        [Role(GorunurAdi = "Uyruklar", Kategori = SystemMenu.TanimlamalarMenuName, Aciklama = "Bu yetki Uyruklar menüsünü Görebilme/Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string Uyruklar = "Uyruklar";
        [Menu(BagliMenuID = 90000, MenuAdi = "Şehirler", MenuCssClass = "fa fa-list-alt", MenuUrl = "Sehirler/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Şehirler", Kategori = SystemMenu.TanimlamalarMenuName, Aciklama = "Bu yetki Şehirler menüsünü Görebilme/Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string Sehirler = "Şehirler";
        [Menu(BagliMenuID = 90000, MenuAdi = "Üniversiteler", MenuCssClass = "fa fa-list-alt", MenuUrl = "Universiteler/Index", SiraNo = 7)]
        [Role(GorunurAdi = "Üniversiteler", Kategori = SystemMenu.TanimlamalarMenuName, Aciklama = "Bu yetki Üniversiteler menüsünü Görebilme/Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string Universiteler = "Üniversiteler";
        [Menu(BagliMenuID = 90000, MenuAdi = "Enstitüler", MenuCssClass = "fa fa-home", MenuUrl = "Enstituler/Index", SiraNo = 10)]
        [Role(GorunurAdi = "Enstitüler", Kategori = SystemMenu.TanimlamalarMenuName, Aciklama = "Bu yetki Enstitüler menüsünü Görebilme/Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string Enstituler = "Enstitüler";
        [Menu(BagliMenuID = 90000, MenuAdi = "Öğrenci Bölümleri", MenuCssClass = "fa fa-list-alt", MenuUrl = "OgrenciBolumleri/Index", SiraNo = 13)]
        [Role(GorunurAdi = "Öğrenci Bölümleri", Kategori = SystemMenu.TanimlamalarMenuName, Aciklama = "Bu yetki Öğrenci Bölümleri menüsünü Görebilme/Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string OgrenciBolumleri = "Öğrenci Bölümleri";
        [Menu(BagliMenuID = 90000, MenuAdi = "Öğrenim Tipleri", MenuCssClass = "fa fa-list-alt", MenuUrl = "OgrenimTipleri/Index", SiraNo = 16)]
        [Role(GorunurAdi = "Öğrenim Tipleri", Kategori = SystemMenu.TanimlamalarMenuName, Aciklama = "Bu yetki Öğrenim Tipleri menüsünü Görebilme/Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string OgrenimTipleri = "Öğrenim Tipleri";
        [Menu(BagliMenuID = 90000, MenuAdi = "Anabilim Dalları", MenuCssClass = "fa fa-list-alt", MenuUrl = "Anabilimdallari/Index", SiraNo = 19)]
        [Role(GorunurAdi = "Anabilim Dalları", Kategori = SystemMenu.TanimlamalarMenuName, Aciklama = "Bu yetki Anabilim Dalları menüsünü Görebilme/Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string AnabilimDallari = "Anabilim Dalları";
        [Menu(BagliMenuID = 90000, MenuAdi = "Programlar", MenuCssClass = "fa fa-list-alt", MenuUrl = "Programlar/Index", SiraNo = 22)]
        [Role(GorunurAdi = "Programlar", Kategori = SystemMenu.TanimlamalarMenuName, Aciklama = "Bu yetki Programlar menüsünü Görebilme/Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string Programlar = "Programlar";
        [Menu(BagliMenuID = 90000, MenuAdi = "Sınav Tipleri", MenuCssClass = "fa fa-gear", MenuUrl = "SinavTipleri/Index", SiraNo = 28)]
        [Role(GorunurAdi = "Sınav Tipleri", Kategori = SystemMenu.TanimlamalarMenuName, Aciklama = "Bu yetki Sınav Tipleri menüsünü Görebilme/Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string SinavTipleri = "Sınav Tipleri";

        [Menu(BagliMenuID = 100000, MenuAdi = "Duyurular", MenuCssClass = "fa fa-bullhorn", MenuUrl = "Duyurular/Index", SiraNo = 1)]
        [Role(GorunurAdi = "Duyurular", Kategori = SystemMenu.SistemMenuName, Aciklama = "Bu yetki Duyurular menüsünü Görebilme/Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string Duyurular = "Duyurular";
        [Menu(BagliMenuID = 100000, MenuAdi = "Gelen Mesajlar", MenuCssClass = "fa fa-envelope", MenuUrl = "Mesajlar/Index", SiraNo = 4)]
        [Role(GorunurAdi = "Gelen Mesajlar", Kategori = SystemMenu.SistemMenuName, Aciklama = "Bu yetki Gelen Mesajlar menüsünü Görebilmeyi/Mesajları Cevaplayabilmeyi sağlayan yetkidir.")]
        public const string Mesajlar = "Gelen Mesajlar";
        [Role(GorunurAdi = "Gelen Mesajlar Sil", Kategori = SystemMenu.SistemMenuName, Aciklama = "Bu yetki Gelen Mesajlar menüsünde gelen mesajı Silebilmeyi sağlayan yetkidir.")]
        public const string MesajlarSil = "Gelen Mesajlar Sil";
        [Menu(BagliMenuID = 100000, MenuAdi = "Gönderilen Mailler", MenuCssClass = "fa fa-envelope", MenuUrl = "MailIslemleri/Index", SiraNo = 7)]
        [Role(GorunurAdi = "Mail İşlemleri", Kategori = SystemMenu.SistemMenuName, Aciklama = "Bu yetki Gönderilen Mailler menüsünü Görebilmeyi sağlayan yetkidir.")]
        public const string MailIslemleri = "Mail İşlemleri";
        [Role(GorunurAdi = "Mail Gönder", Kategori = SystemMenu.SistemMenuName, Aciklama = "")]
        public const string MailGonder = "Mail Gönder";
        [Menu(BagliMenuID = 100000, MenuAdi = "Mail Şablonları", MenuCssClass = "fa fa-pencil", MenuUrl = "MailSablonlari/Index", SiraNo = 10)]
        [Role(GorunurAdi = "Mail Şablonları", Kategori = SystemMenu.SistemMenuName, Aciklama = "Bu yetki Mail Şablonları menüsünü Görebilmeyi Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string MailSablonlari = "Mail Şablonları";
        [Menu(BagliMenuID = 100000, MenuAdi = "Mail Şablonları (Sistem)", MenuCssClass = "fa fa-gear", MenuUrl = "MailSablonlariSistem/Index", SiraNo = 13)]
        [Role(GorunurAdi = "Mail Şablonları (Sistem)", Kategori = SystemMenu.SistemMenuName, Aciklama = "Bu yetki Mail Şablonları Sistem menüsünü Görebilmeyi Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string MailSablonlariSistem = "Mail Şablonları (Sistem)";
        [Menu(BagliMenuID = 100000, MenuAdi = "Yazı Şablonları", MenuCssClass = "fa fa-gear", MenuUrl = "YaziSablonlari/Index", SiraNo = 14)]
        [Role(GorunurAdi = "Yazı Şablonları", Kategori = SystemMenu.SistemMenuName, Aciklama = "Bu yetki Yazı Şablonları  menüsünü Görebilmeyi Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string YaziSablonlari = "Yazi Şablonları";
        [Menu(BagliMenuID = 100000, MenuAdi = "Mesaj Kategorileri", MenuCssClass = "fa fa-pencil", MenuUrl = "MesajKategorileri/Index", SiraNo = 16)]
        [Role(GorunurAdi = "Mesaj Kategorileri", Kategori = SystemMenu.SistemMenuName, Aciklama = "Bu yetki Mesaj Kategorileri menüsünü Görebilmeyi Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string MesajlarKategorileri = "Mesaj Kategorileri";
        [Menu(BagliMenuID = 100000, MenuAdi = "Anketler", MenuCssClass = "fa fa-pencil", MenuUrl = "Anketler/Index", SiraNo = 19)]
        [Role(GorunurAdi = "Anketler", Kategori = SystemMenu.SistemMenuName, Aciklama = "Bu yetki Anketler menüsünü Görebilmeyi Kayıt/Silme işlemlerini yapabilmeyi sağlayan yetkidir.")]
        public const string Anketler = "Anketler";
        [Role(GorunurAdi = "SSS Kayıt", Kategori = SystemMenu.SistemMenuName, Aciklama = "Bu yetki Sıkça Sorulan Sorular bilgilerini Güncelleyebilmeyi sağlayan yetkidir.")]
        public const string SssKayit = "SSSKayit";
        [Menu(BagliMenuID = 100000, MenuAdi = "Sistem Ayarları", MenuCssClass = "fa fa-cogs", MenuUrl = "SistemAyarlari/Index", SiraNo = 22)]
        [Role(GorunurAdi = "Sistem Ayarları", Kategori = SystemMenu.SistemMenuName, Aciklama = "Bu yetki Sistem Ayarları menüsünü Görebilmeyi ve Güncelleyebilmeyi sağlayan yetkidir.")]
        public const string SistemAyarlari = "Sistem Ayarları";
        [Menu(BagliMenuID = 100000, MenuAdi = "Sistem Bilgilendirme", MenuCssClass = "fa fa-envelope", MenuUrl = "SistemBilgilendirme/Index", SiraNo = 25)]
        [Role(GorunurAdi = "Sistem Bilgilendirme", Kategori = SystemMenu.SistemMenuName, Aciklama = "Bu yetki Sistem Bilgilendirme menüsünü Görebilmeyi sağlayan yetkidir.")]
        public const string SistemBilgilendirme = "Sistem Bilgilendirme";


    }
}