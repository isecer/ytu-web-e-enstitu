using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class YeterlikBus
    {
        public static List<string> NotDegerleri = new List<string>
        {
            "F0",
            "FF",
            "DD",
            "DC",
            "CC",
            "CB",
            "BB",
            "BA",
            "AA"
        };

        public static bool IsHarfNotuBuyukEsit(string not1, string not2)
        {
            var not1Inx = NotDegerleri.IndexOf(not1);
            var not2Inx = NotDegerleri.IndexOf(not2);
            return not1Inx <= not2Inx;
        }
        public static int? GetYeterlikAktifSurecId(string enstituKod, int? yeterlikSurecId = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var nowDate = DateTime.Now;
                var aktifSurec = db.YeterlikSurecis.FirstOrDefault(p => (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif && (p.EnstituKod == enstituKod) && p.YeterlikSurecID == (yeterlikSurecId ?? p.YeterlikSurecID));
                return aktifSurec?.YeterlikSurecID;
            }
        }

        public static List<KmYeterlikSureciOgrenimTipKriterleri> GetOgrenimTipKriterleri(string enstituKod, int? yeterlikSurecId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var yeterlikSurecOgrenimTipleri = db.YeterlikSurecOgrenimTipleris.Where(p => p.YeterlikSureci.EnstituKod == enstituKod && p.YeterlikSurecID == (yeterlikSurecId ?? p.YeterlikSurecID)).ToList();
                var ogrenimTipleri = db.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod && p.IsMezuniyetBasvurusuYapabilir && p.IsAktif).ToList();
                var ogrenimtipData = (from o in ogrenimTipleri.Where(p => p.OgrenimTipKod.IsDoktora())
                                      join yo in yeterlikSurecOgrenimTipleri on o.OgrenimTipID equals yo.OgrenimTipID into defYod
                                      from defYo in defYod.DefaultIfEmpty()
                                      select new KmYeterlikSureciOgrenimTipKriterleri
                                      {
                                          YeterlikSurecOgrenimTipID = defYo?.YeterlikSurecOgrenimTipID ?? 0,
                                          IsSelected = defYo != null,
                                          YeterlikSurecID = yeterlikSurecId ?? 0,
                                          OgrenimTipAdi = o.OgrenimTipAdi,
                                          OgrenimTipKod = o.OgrenimTipKod,
                                          OgrenimTipID = o.OgrenimTipID,
                                          YsMaxBasvuruDonemNo = defYo?.YsMaxBasvuruDonemNo,
                                          YsBasToplamKrediKriteri = defYo?.YsBasToplamKrediKriteri,
                                          YsBasEtikNotKriteri = defYo?.YsBasEtikNotKriteri,
                                          YsBasSeminerNotKriteri = defYo?.YsBasSeminerNotKriteri,
                                          YaziliYuzde = defYo?.YaziliYuzde,
                                          SozluYuzde = defYo?.SozluYuzde,
                                          YaziliGecerNot = defYo?.YaziliGecerNot,
                                          SozluGecerNot = defYo?.SozluGecerNot,
                                          OrtalamaGecerNot = defYo?.OrtalamaGecerNot
                                      }
                    ).ToList();
                foreach (var item in ogrenimtipData)
                {
                    item.SlistEtikNots = new SelectList(NotDegerleri, item.YsBasEtikNotKriteri);
                    item.SlistSeminerNots = new SelectList(NotDegerleri, item.YsBasSeminerNotKriteri);
                }
                return ogrenimtipData;

            }
        }

        public static MmMessage YeterlikBasvurusuSilKontrol(int yeterlikBasvurulariId)
        {
            var msg = new MmMessage();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kayitYetki = RoleNames.YeterlikGelenBasvurularKayit.InRoleCurrent();
                var basvuru = db.YeterlikBasvurus.FirstOrDefault(p => p.YeterlikBasvuruID == yeterlikBasvurulariId);
                if (basvuru == null)
                {
                    msg.Messages.Add("Silinmek istenen başvuru sistemde bulunamadı.");
                }
                else
                {
                    if (!UserIdentity.Current.EnstituKods.Contains(basvuru.YeterlikSureci.EnstituKod) && kayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                        var message = "Bu enstitüye ait Yeterlik başvurusu silmeye yetkili değilsiniz!\r\n Yeterlik Başvuru ID: " + basvuru.YeterlikBasvuruID + " \r\n Yeterlik Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Yeterlik Başvuru Sil", LogType.Kritik);
                    }
                    else if (!GetYeterlikAktifSurecId(basvuru.YeterlikSureci.EnstituKod, basvuru.YeterlikSurecID).HasValue && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Yeterlik başvurusunu silmeye hakkınız yoktur! \r\n Silinmeye çalışılan Yeterlik Başvuru ID:" + basvuru.YeterlikBasvuruID + " \r\n Yeterlik Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi.ToString(), "Başvuru Sil", LogType.Saldırı);
                    }
                    else if (basvuru.IsEnstituOnaylandi.HasValue)
                    {
                        msg.Messages.Add("Enstitü tarafından işlem gören bir başvuru silinemez!");
                    }
                }
            }
            msg.IsSuccess = !msg.Messages.Any();
            msg.MessageType = msg.IsSuccess ? Msgtype.Success : Msgtype.Information;
            return msg;
        }
        public static List<string> YeterlikBasvuruKontrol(string enstituKod, Guid? uniqueId)
        {
            var errorMessage = new List<string>();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kayitYetki = RoleNames.YeterlikGelenBasvurularKayit.InRoleCurrent();
                if (uniqueId.HasValue)
                {
                    var basvuru = db.YeterlikBasvurus.FirstOrDefault(p => p.UniqueID == uniqueId.Value);
                    if (basvuru == null)
                    {
                        errorMessage.Add("Aranan başvuru sistemde bulunamadı.");
                    }
                    else
                    {
                        if (!UserIdentity.Current.EnstituKods.Contains(basvuru.YeterlikSureci.EnstituKod) && kayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            errorMessage.Add("Bu Enstitü İçin Yetkili Değilsiniz.");
                            var message = "Bu enstitüye ait Yeterlik başvurusu güncellemeye yetkili değilsiniz!\r\n Yeterlik Başvuru ID: " + basvuru.YeterlikBasvuruID + " \r\n Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Başvuru Düzelt", LogType.Saldırı);
                        }
                        else if (!GetYeterlikAktifSurecId(enstituKod, basvuru.YeterlikSurecID).HasValue && UserIdentity.Current.IsAdmin == false)
                        {
                            errorMessage.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                        }
                        if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            errorMessage.Add("Bu İşlem için Yetkili Değilsiniz.");
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Yeterlik başvurusu düzenlemeye hakkınız yoktur! \r\n Çağrılan Yeterlik Başvuru ID:" + basvuru.YeterlikBasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Yeterlik Başvuru Düzelt", LogType.Saldırı);
                        }
                        else if (basvuru.IsEnstituOnaylandi.HasValue)
                        {
                            errorMessage.Add("Başvuru enstitü tarafından işlem gördüğü düzenlenemez!");
                        }

                    }
                }
                else
                {
                    var yeterlikSurecId = GetYeterlikAktifSurecId(enstituKod);
                    var kul = db.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
                    if (!yeterlikSurecId.HasValue)
                    {
                        errorMessage.Add("Başvuru Süreci Kapalı");
                    }
                    else if (!(kul.KullaniciTipleri.BasvuruYapabilir))
                    {
                        errorMessage.Add("Kullanıcı Hesap Türünüz için Başvuru İşlemleri Kapalıdır.");
                    }
                    else if (enstituKod != kul.EnstituKod)
                    {
                        var enstitu = db.Enstitulers.First(p => p.EnstituKod == kul.EnstituKod);
                        errorMessage.Add("Kullanıcı hesbınızın kayıtlı olduğu enstitü ile başvuru yaptığınız entistü uyuşmamaktadır. Enstitünüz: " + enstitu.EnstituAd + " olarak gözükmektedir.");
                    }
                    else
                    {
                        if (kul.YtuOgrencisi)
                        {
                            var ogrenimTipAdi = db.OgrenimTipleris.First(p => p.OgrenimTipKod == kul.OgrenimTipKod).OgrenimTipAdi;

                            if (kul.OgrenimDurumID != OgrenimDurum.HalenOğrenci)
                            {
                                errorMessage.Add("Yeterlik Başvuru işlemini yapabilmeniz için profil kısmındaki öğrenim bilgilerinizde bulunan Öğrenim durumunuzun Halen öğrenci olarak seçilmesi gerekmektedir. (Not: özel öğrenciler bu sistem üzerinden başvuru yapamazlar.)");
                            }
                            var basvuruVar = db.YeterlikBasvurus.Any(p => p.IsEnstituOnaylandi != false && p.YeterlikSurecID == yeterlikSurecId && p.KullaniciID == (kayitYetki ? p.KullaniciID : UserIdentity.Current.Id));
                            if (basvuruVar)
                            {
                                errorMessage.Add("Bu Yeterlik süreci için başvurunuz bulunmaktadır tekrar başvuru yapamazsınız!");

                            }
                            else if (db.YeterlikSurecOgrenimTipleris.Any(a => a.YeterlikSurecID == yeterlikSurecId.Value && a.OgrenimTipKod != kul.OgrenimTipKod) == false)
                            {
                                errorMessage.Add(ogrenimTipAdi + " Öğrenim seviyesinde okuyan öğrenciler Yeterlik başvurusu yapamazlar");
                            }
                            else if (!db.YeterlikSureciKriterMuafOgrencilers.Any(a => a.YeterlikSurecID == yeterlikSurecId.Value && a.KullaniciID == kul.KullaniciID))
                            {
                                var basvuruKriterleri = db.YeterlikSurecOgrenimTipleris.FirstOrDefault(p => p.YeterlikSurecID == yeterlikSurecId.Value && p.OgrenimTipKod == kul.OgrenimTipKod);
                                if (basvuruKriterleri == null)
                                {
                                    errorMessage.Add("Okuduğunuz öğrenim seviyesi yeterlik başvuru yapmak için uygun değildir.");
                                }
                               
                                var ogrenciBilgi = KullanicilarBus.OgrenciKontrol(kul.TcKimlikNo);
                                var controlMessage = new List<string>();
                                if (basvuruKriterleri.YsMaxBasvuruDonemNo.HasValue && basvuruKriterleri.YsMaxBasvuruDonemNo < ogrenciBilgi.OkuduguDonemNo)
                                {
                                    controlMessage.Add("Aktif okuduğunuz dönem " + basvuruKriterleri.YsMaxBasvuruDonemNo + ".dönem veya daha altı olması gerekmektedir.");
                                }
                                if (!basvuruKriterleri.YsBasEtikNotKriteri.IsNullOrWhiteSpace() && !IsHarfNotuBuyukEsit(basvuruKriterleri.YsBasEtikNotKriteri, ogrenciBilgi.AktifDonemDers.EtikDersNotu))
                                {
                                    controlMessage.Add("Etik dersi için ders notu " + basvuruKriterleri.YsBasEtikNotKriteri + " veya daha üstü bir not almanız gerekmektedir.");
                                }
                                if (!basvuruKriterleri.YsBasSeminerNotKriteri.IsNullOrWhiteSpace() && !IsHarfNotuBuyukEsit(basvuruKriterleri.YsBasSeminerNotKriteri, ogrenciBilgi.AktifDonemDers.SeminerDersNotu))
                                {
                                    controlMessage.Add("Seminer dersi için ders notu " + basvuruKriterleri.YsBasSeminerNotKriteri + " veya daha üstü bir not almanız gerekmektedir.");
                                }
                                if (basvuruKriterleri.YsBasToplamKrediKriteri.HasValue && basvuruKriterleri.YsBasToplamKrediKriteri > ogrenciBilgi.AktifDonemDers.ToplamKredi)
                                {
                                    controlMessage.Add("Toplam Kredi sayınız " + basvuruKriterleri.YsBasToplamKrediKriteri + " krediden büyük ya da eşit olmalıdır. Mevcut Kredi: " + ogrenciBilgi.AktifDonemDers.ToplamKredi);
                                }
                                if (controlMessage.Count > 0)
                                {
                                    errorMessage.Add(ogrenimTipAdi + " Yeterlik başvurunuz aşağıdaki sebeplerden dolayı başlatılamadı.");
                                    errorMessage.AddRange(controlMessage);
                                }
                            }
                        }
                        else
                        {
                            errorMessage.Add("Yeterlik başvurusu yapabilmeniz için Hesap bilginizi düzelterek YTÜ öğrencisi olduğunuzu belirtiniz.");
                        }
                    }
                }

            }
            return errorMessage;

        }
        public static List<CmbIntDto> GetCmbYeterlikSurecleri(string enstituKod, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.YeterlikSurecis.Where(p => p.EnstituKod == enstituKod)
                            join d in db.Donemlers on s.DonemID equals d.DonemID
                            orderby s.BaslangicTarihi descending
                            select new
                            {
                                s.YeterlikSurecID,
                                s.BaslangicYil,
                                s.BitisYil,
                                d.DonemAdi,
                                s.BaslangicTarihi,
                                s.BitisTarihi
                            }).ToList();
                foreach (var item in data)
                {
                    lst.Add(new CmbIntDto { Value = item.YeterlikSurecID, Caption = (item.BaslangicYil + "/" + item.BitisYil + " " + item.DonemAdi + " (" + item.BaslangicTarihi.ToDateString() + " - " + item.BitisTarihi.ToDateString() + ")") });
                }
            }
            return lst;
        }
        public static List<CmbIntDto> GetCmbFilterYeterlikAnabilimDallari(string enstituKod, int? basvuruSurecId, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var yeterliAnabilimDaliIds = db.YeterlikBasvurus
                    .Where(p => p.YeterlikSureci.EnstituKod == enstituKod &&
                                p.YeterlikSurecID == (basvuruSurecId ?? p.YeterlikSurecID)).Select(s => s.Programlar.AnabilimDaliID).Distinct().ToList();

                var anabilimDallaris = db.AnabilimDallaris.Where(p => yeterliAnabilimDaliIds.Contains(p.AnabilimDaliID))
                    .Select(s => new { s.AnabilimDaliID, s.AnabilimDaliAdi }).OrderBy(o => o.AnabilimDaliAdi).ToList();

                foreach (var item in anabilimDallaris)
                {
                    lst.Add(new CmbIntDto { Value = item.AnabilimDaliID, Caption = item.AnabilimDaliAdi });
                }
            }
            return lst;
        }

     
        public static List<CmbIntDto> GetCmbBasvuruDurumu(bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            lst.Add(new CmbIntDto { Value = YeterlikBasvuruFilterEnum.IslemGormeyenler, Caption = "İşlem Görmeyenler" });
            lst.Add(new CmbIntDto { Value = YeterlikBasvuruFilterEnum.Onaylananlar, Caption = "Onaylananlar" });
            lst.Add(new CmbIntDto { Value = YeterlikBasvuruFilterEnum.IptalEdilenler, Caption = "İptal Edilenler" });
            lst.Add(new CmbIntDto { Value = YeterlikBasvuruFilterEnum.JuriOlusturulmayanlar, Caption = "Jüri Oluşturulmayanlar" });
            lst.Add(new CmbIntDto { Value = YeterlikBasvuruFilterEnum.KomiteOnayiBekleyenler, Caption = "ABD Komite Onayı Bekleyenler" });
            lst.Add(new CmbIntDto { Value = YeterlikBasvuruFilterEnum.KomiteOnayiTamamlananlar, Caption = "ABD Komite Onayı Tamamlananlar" });
            lst.Add(new CmbIntDto { Value = YeterlikBasvuruFilterEnum.SinavSureciniBaslatilmayanlar, Caption = "Sınav Süreci Başlatılmayanlar" });
            lst.Add(new CmbIntDto { Value = YeterlikBasvuruFilterEnum.SinavSurecindeOlanlar, Caption = "Sınav Sürecinde Olanlar" });
            lst.Add(new CmbIntDto { Value = YeterlikBasvuruFilterEnum.BasariliOlanlar, Caption = "Başarılı Olanlar" });
            lst.Add(new CmbIntDto { Value = YeterlikBasvuruFilterEnum.BasarisizOlanlar, Caption = "Başarısız Olanlar" });
            return lst;
        }
        public static List<CmbStringDto> GetCmbJuriYedekList(Guid juriUniqueId, bool bosSecimVar = false)
        {
            var lst = new List<CmbStringDto>();
            if (bosSecimVar) lst.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var juri = db.YeterlikBasvuruJuriUyeleris.First(f => f.UniqueID == juriUniqueId);
                var juriUyeleri = db.YeterlikBasvuruJuriUyeleris.Where(p => p.YeterlikBasvuruID == juri.YeterlikBasvuruID && !p.IsSecilenJuri && p.IsYtuIciOrDisi == juri.IsYtuIciOrDisi && p.UniqueID != juriUniqueId).ToList();
                var cmbData = juriUyeleri.Select(s => new CmbStringDto
                {
                    Value = s.UniqueID.ToString(),
                    Caption = s.UnvanAdi + " " + s.AdSoyad
                }).ToList();
                lst.AddRange(cmbData);
            }
            return lst;
        }
        public static List<CmbStringDto> GetCmbKomiteDegisiklikList(Guid juriUniqueId, bool bosSecimVar = false)
        {
            var lst = new List<CmbStringDto>();
            if (bosSecimVar) lst.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var komite = db.YeterlikBasvuruKomitelers.First(f => f.UniqueID == juriUniqueId);
                var komiteler = komite.YeterlikBasvuru.YeterlikBasvuruKomitelers;
                var anabilimDali = komite.YeterlikBasvuru.Programlar.AnabilimDallari;
                var haricKomiteKullaniciIds = komiteler.Select(s => s.KullaniciID).ToList();
                var anabilimDaliYeniKomiteler = anabilimDali.AnabilimDaliYeterlikKomiteUyeleris.Where(p => !haricKomiteKullaniciIds.Contains(p.KullaniciID)).ToList();

                var cmbData = anabilimDaliYeniKomiteler.Select(s => new CmbStringDto
                {
                    Value = s.KullaniciID.ToString(),
                    Caption = s.Kullanicilar.Unvanlar.UnvanAdi + " " + s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad
                }).ToList();
                lst.AddRange(cmbData);
            }
            return lst;
        }
        public static MmMessage SendMailBasvuruOnayi(Guid basvuruUniqueId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var basvuru = db.YeterlikBasvurus.First(p => p.UniqueID == basvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = db.Kullanicilars.Find(basvuru.TezDanismanID);
                    var surec = basvuru.YeterlikSureci;
                    var enstitu = basvuru.YeterlikSureci.Enstituler;
                    var anabilimDali = basvuru.Programlar.AnabilimDallari;
                    var program = basvuru.Programlar;



                    var mModel = new List<SablonMailModel>
                    {
                        new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID =basvuru.IsEnstituOnaylandi.Value? MailSablonTipi.Yeterlik_BasvuruOnaylandiOgrenciye:MailSablonTipi.Yeterlik_BasvuruRetEdildiOgrenciye,
                        }
                    };
                    if (basvuru.IsEnstituOnaylandi == true)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.Yeterlik_BasvuruOnaylandiDanismana
                        });
                    }
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        var gonderilenMailEkleri = new List<GonderilenMailEkleri>();

                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMailEkleri.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailTIToplantiBilgisi", LogType.Uyarı);
                        }

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (basvuru.IsEnstituOnaylandi == false && item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "RetAciklamasi", Value = basvuru.EnstituOnayAciklama });

                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstitu.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = mCOntent.Title
                            };
                            if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGk in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                            }
                            foreach (var itemMe in gonderilenMailEkleri)
                            {
                                kModel.GonderilenMailEkleris.Add(itemMe);
                            }
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "";
                //message = isLinkOrSonuc ? "Yeterlik Jüri üyeleri onayı için Komite üyelerine onay davet linki mail olarak gönderilirken bir hata oluştu!" : "Yeterlik Jüri üyeleri onayı  davet linki  Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
                //SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTIDegerlendirmeLink \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                ////mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailKomiteDegerlendirmeLink(Guid yeterlikBasvuruUniqueId, Guid? komiteUniqueId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var basvuru = db.YeterlikBasvurus.First(p => p.UniqueID == yeterlikBasvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = db.Kullanicilars.Find(basvuru.TezDanismanID);
                    var surec = basvuru.YeterlikSureci;
                    var enstitu = basvuru.YeterlikSureci.Enstituler;
                    var anabilimDali = basvuru.Programlar.AnabilimDallari;
                    var program = basvuru.Programlar;
                    var komiteler = basvuru.YeterlikBasvuruKomitelers.Where(p => p.UniqueID == (komiteUniqueId ?? p.UniqueID)).ToList();
                    foreach (var item in komiteler)
                    {
                        item.UniqueID = Guid.NewGuid();
                    }
                    var mModel = new List<SablonMailModel>();
                    foreach (var item in komiteler)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Komite Üyesi",
                            UniqueID = item.UniqueID,
                            UnvanAdi = item.Kullanicilar.Unvanlar.UnvanAdi,
                            AdSoyad = item.Kullanicilar.Ad + " " + item.Kullanicilar.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.Yeterlik_JuriUyeleriTanimlandiKomiteyeLink,
                        });
                    }
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {
                        var komite = komiteler.FirstOrDefault(p => p.UniqueID == item.UniqueID);
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        var gonderilenMailEkleri = new List<GonderilenMailEkleri>();

                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMailEkleri.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailTIToplantiBilgisi", LogType.Uyarı);
                        }
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@KomiteUyesiAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "KomiteUyesiAdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@KomiteUyesiUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "KomiteUyesiUnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/Yeterlik/Index?isKomiteOrJuri=true&isDegerlendirme=" + item.UniqueID, IsLink = true });

                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OncekiMailTarihi", Value = komite.LinkGonderimTarihi?.ToFormatDateAndTime() });

                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstitu.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = mCOntent.Title
                            };
                            if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGk in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                            }
                            foreach (var itemMe in gonderilenMailEkleri)
                            {
                                kModel.GonderilenMailEkleris.Add(itemMe);
                            }
                            komite.DegerlendirmeIslemTarihi = null;
                            komite.DegerlendirmeIslemYapanIP = null;
                            komite.DegerlendirmeYapanID = null;
                            komite.IsJuriOnaylandi = null;
                            komite.IsLinkGonderildi = true;
                            komite.LinkGonderimTarihi = DateTime.Now;
                            komite.LinkGonderenID = UserIdentity.Current.Id;
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                            LogIslemleri.LogEkle("YeterlikBasvuruKomiteler", IslemTipi.Update, komite.ToJson());
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Yeterlik Jüri üyeleri onayı için Komite üyelerine onay davet linki mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogType.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailKomiteDegerlendirmeSonuc(Guid yeterlikBasvuruUniqueId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var basvuru = db.YeterlikBasvurus.First(p => p.UniqueID == yeterlikBasvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = db.Kullanicilars.Find(basvuru.TezDanismanID);
                    var surec = basvuru.YeterlikSureci;
                    var enstitu = basvuru.YeterlikSureci.Enstituler;
                    var anabilimDali = basvuru.Programlar.AnabilimDallari;
                    var program = basvuru.Programlar;
                    var mModel = new List<SablonMailModel>();
                    if (basvuru.IsEnstituOnaylandi == true)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.Yeterlik_KomiteDegerlendimreyiTamamladiDanismana
                        });
                    }
                    //var komiteler = basvuru.YeterlikBasvuruKomitelers.ToList();
                    //foreach (var item in komiteler)
                    //{
                    //    item.UniqueID = Guid.NewGuid();
                    //}
                    //foreach (var item in komiteler)
                    //{
                    //    mModel.Add(new SablonMailModel
                    //    {
                    //        JuriTipAdi = "Komite Üyesi",
                    //        UniqueID = item.UniqueID,
                    //        UnvanAdi = item.Kullanicilar.Unvanlar.UnvanAdi,
                    //        AdSoyad = item.Kullanicilar.Ad + " " + item.Kullanicilar.Soyad,
                    //        EMails = new List<MailSendList> { new MailSendList { EMail = item.Kullanicilar.EMail, ToOrBcc = true } },
                    //        MailSablonTipID = MailSablonTipi.Yeterlik_JuriUyeleriTanimlandiKomiteyeLink,
                    //    });
                    //}
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        var gonderilenMailEkleri = new List<GonderilenMailEkleri>();

                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMailEkleri.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailTIToplantiBilgisi", LogType.Uyarı);
                        }
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });

                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstitu.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = mCOntent.Title
                            };
                            if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGk in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                            }
                            foreach (var itemMe in gonderilenMailEkleri)
                            {
                                kModel.GonderilenMailEkleris.Add(itemMe);
                            }

                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Yeterlik ABD komitesi Jüri üyeleri onayını tamamladıktan sonra mail gönderilirken hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogType.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }

            return mmMessage;
        }
        public static MmMessage SendMailSinavBilgi(Guid basvuruUniqueId, bool isYaziliOrSozlu = true, Guid? uniqueId = null)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var basvuru = db.YeterlikBasvurus.First(p => p.UniqueID == basvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = db.Kullanicilars.Find(basvuru.TezDanismanID);
                    var surec = basvuru.YeterlikSureci;
                    var enstitu = basvuru.YeterlikSureci.Enstituler;
                    var anabilimDali = basvuru.Programlar.AnabilimDallari;
                    var program = basvuru.Programlar;
                    var juriler = basvuru.YeterlikBasvuruJuriUyeleris.Where(p => p.IsSecilenJuri && p.UniqueID == (uniqueId ?? p.UniqueID)).ToList();



                    var ogrenciMailSablonTipId = 0;
                    var danismanMailSablonTipId = 0;
                    var juriMailSablonTipId = 0;
                    if (isYaziliOrSozlu)
                    {
                        if (!basvuru.IsYaziliSinavinaKatildi.HasValue)
                        {
                            ogrenciMailSablonTipId = MailSablonTipi.Yeterlik_YaziliSinavTalebiYapildiOgrenciye;
                            danismanMailSablonTipId = MailSablonTipi.Yeterlik_YaziliSinavTalebiYapildiDanismana;
                            juriMailSablonTipId = MailSablonTipi.Yeterlik_YaziliSinavTalebiYapildiJurilere;
                        }
                        else
                        {
                            if (basvuru.IsYaziliSinavinaKatildi.Value)
                            {

                                ogrenciMailSablonTipId = basvuru.IsYaziliSinavBasarili.Value
                                    ? MailSablonTipi.Yeterlik_YaziliSinavBasariliGirisiYapidliOgrenciye
                                    : MailSablonTipi.Yeterlik_YaziliSinavBasarisizOnayYapildiOgrenciye;
                                danismanMailSablonTipId = basvuru.IsYaziliSinavBasarili.Value
                                    ? MailSablonTipi.Yeterlik_YaziliSinavBasariliGirisiYapidliDanismana
                                    : MailSablonTipi.Yeterlik_YaziliSinavBasarisizOnayYapildiDanismana;
                                if (basvuru.IsYaziliSinavBasarili.Value) juriMailSablonTipId = MailSablonTipi.Yeterlik_YaziliSinavBasariliGirisiYapidliJurilere;
                            }
                            else
                            {
                                ogrenciMailSablonTipId =
                                    MailSablonTipi.Yeterlik_YaziliSinavKatilmadiGirisiYapildiOgrenciye;
                                danismanMailSablonTipId =
                                    MailSablonTipi.Yeterlik_YaziliSinavKatilmadiGirisiYapildiDanismana;
                            }
                        }
                    }
                    else
                    {
                        if (!basvuru.IsSozluSinavinaKatildi.HasValue)
                        {
                            ogrenciMailSablonTipId = MailSablonTipi.Yeterlik_SozluSinavTalebiYapildiOgrenciye;
                            danismanMailSablonTipId = MailSablonTipi.Yeterlik_SozluSinavTalebiYapildiDanismana;
                            juriMailSablonTipId = MailSablonTipi.Yeterlik_SozluSinavTalebiYapildiJurilere;
                        }
                        else
                        {


                            if (basvuru.IsSozluSinavinaKatildi.Value)
                            {
                                if (juriler.All(a => a.IsSonucOnaylandi.HasValue))
                                {
                                    ogrenciMailSablonTipId = basvuru.IsGenelSonucBasarili.Value ? MailSablonTipi.Yeterlik_GenelSinavSonucuBasariliOgrenciye : MailSablonTipi.Yeterlik_GenelSinavSonucuBasarisizOgrenciye;
                                    danismanMailSablonTipId = basvuru.IsGenelSonucBasarili.Value ? MailSablonTipi.Yeterlik_GenelSinavSonucuBasariliDanismana : MailSablonTipi.Yeterlik_GenelSinavSonucuBasarisizDanismana;
                                    juriMailSablonTipId = basvuru.IsGenelSonucBasarili.Value ? MailSablonTipi.Yeterlik_GenelSinavSonucuBasariliJurilere : MailSablonTipi.Yeterlik_GenelSinavSonucuBasarisizJurilere;

                                }
                            }
                            else
                            {
                                ogrenciMailSablonTipId = MailSablonTipi.Yeterlik_SozluSinavKatilmadiGirisiYapildiOgrenciye;
                                danismanMailSablonTipId = MailSablonTipi.Yeterlik_SozluSinavKatilmadiGirisiYapildiDanismana;
                            }
                        }
                    }

                    var mModel = new List<SablonMailModel>();
                    if (!uniqueId.HasValue)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList>
                                { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = ogrenciMailSablonTipId
                        });
                        mModel.Add(
                            new SablonMailModel
                            {

                                JuriTipAdi = "Danışman",
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList>
                                    { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = danismanMailSablonTipId,
                                Attachments = basvuru.IsGenelSonucBasarili.HasValue ?
                                    Management.exportRaporPdf(RaporTipleri.YeterlikDoktoraSinavSonucFormu, new List<int?> { basvuru.YeterlikBasvuruID }) : new List<Attachment>()
                            }
                        );
                        juriler = juriler.Where(p => p.JuriTipAdi != "TezDanismani").ToList();
                    }

                    foreach (var item in juriler)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = item.JuriTipAdi,
                            UniqueID = item.UniqueID,
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                            MailSablonTipID = juriMailSablonTipId
                        });
                    }


                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {

                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        var gonderilenMailEkleri = item.Attachments.Select(s => new GonderilenMailEkleri { EkAdi = s.Name }).ToList();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {

                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMailEkleri.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailTIToplantiBilgisi", LogType.Uyarı);
                        }

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@JuriUyesiAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "JuriUyesiAdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@JuriUyesiUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "JuriUyesiUnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto
                            {
                                Key = "SinavTarihi",
                                Value = isYaziliOrSozlu ?
                                basvuru.YaziliSinavTarihi.ToFormatDateAndTime()
                                : basvuru.SozluSinavTarihi.ToFormatDateAndTime()
                            });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto
                            {
                                Key = "SinavYeri",
                                Value = isYaziliOrSozlu ?
                                basvuru.YaziliSinavYeri
                                : basvuru.SozluSinavYeri
                            });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavSekli") && basvuru.IsSozluSinavOnline.HasValue)
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SinavSekli", Value = basvuru.IsSozluSinavOnline == true ? "Online" : "Yüz Yüze" });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavNotu"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SinavNotu", Value = basvuru.YaziliSinaviNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@YaziliNotu"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YaziliNotu", Value = basvuru.YaziliSinaviNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SozluNotuOrtalama"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SozluNotuOrtalama", Value = basvuru.SozluSinaviOrtalamaNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@GenelOrtalama"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "GenelOrtalama", Value = basvuru.GenelBasariNotu.ToString() });
                        }
                        if (item.UniqueID.HasValue && item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/Yeterlik/Index?isKomiteOrJuri=false&isDegerlendirme=" + item.UniqueID, IsLink = true });
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstitu.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = mCOntent.Title
                            };
                            if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGk in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                            }
                            foreach (var itemMe in gonderilenMailEkleri)
                            {
                                kModel.GonderilenMailEkleris.Add(itemMe);
                            }
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "";
                //message = isLinkOrSonuc ? "Yeterlik Jüri üyeleri onayı için Komite üyelerine onay davet linki mail olarak gönderilirken bir hata oluştu!" : "Yeterlik Jüri üyeleri onayı  davet linki  Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
                //SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTIDegerlendirmeLink \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                ////mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailSinavJuriLink(Guid basvuruUniqueId, bool isYaziliOrSozlu = true, Guid? uniqueId = null)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var basvuru = db.YeterlikBasvurus.First(p => p.UniqueID == basvuruUniqueId);
                    var ogrenci = basvuru.Kullanicilar;
                    var danisman = db.Kullanicilars.Find(basvuru.TezDanismanID);
                    var surec = basvuru.YeterlikSureci;
                    var enstitu = basvuru.YeterlikSureci.Enstituler;
                    var anabilimDali = basvuru.Programlar.AnabilimDallari;
                    var program = basvuru.Programlar;
                    var juriler = basvuru.YeterlikBasvuruJuriUyeleris.Where(p => p.IsSecilenJuri && p.UniqueID == (uniqueId ?? p.UniqueID)).ToList();


                    foreach (var item in juriler)
                    {
                        item.UniqueID = Guid.NewGuid();
                    }

                    var juriMailSablonTipId = 0;
                    if (isYaziliOrSozlu)
                    {
                        if (basvuru.IsYaziliSinavinaKatildi == false)
                        {
                            juriMailSablonTipId = MailSablonTipi.Yeterlik_YaziliSinavKatilmadiGirisiYapildiJurilereLink;
                        }
                        else if (basvuru.IsYaziliSinavinaKatildi == true && basvuru.IsYaziliSinavBasarili == false)
                        {
                            juriMailSablonTipId = MailSablonTipi.Yeterlik_YaziliSinavBasarisizGirisiYapildiJurilereLink;
                        }

                    }
                    else
                    {
                        if (basvuru.IsSozluSinavinaKatildi.HasValue)
                        {
                            foreach (var item in juriler)
                            {
                                item.UniqueID = Guid.NewGuid();
                            }
                            juriMailSablonTipId = basvuru.IsSozluSinavinaKatildi.Value ? MailSablonTipi.Yeterlik_SozluNotGirisJurilereLink : MailSablonTipi.Yeterlik_SozluSinavKatilmadiGirisiYapildiJurilereLink;
                        }
                    }

                    var mModel = new List<SablonMailModel>();


                    foreach (var item in juriler)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = item.JuriTipAdi,
                            UniqueID = item.UniqueID,
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                            MailSablonTipID = juriMailSablonTipId
                        });
                    }
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {

                        var juri = juriler.FirstOrDefault(p => p.UniqueID == item.UniqueID);
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        var gonderilenMailEkleri = new List<GonderilenMailEkleri>();

                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMailEkleri.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailTIToplantiBilgisi", LogType.Uyarı);
                        }

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DonemAdi", Value = surec.BaslangicYil + "/" + surec.BitisYil + " " + surec.Donemler.DonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = basvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = ogrenci.Ad + " " + ogrenci.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = anabilimDali.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = program.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@JuriUyesiAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "JuriUyesiAdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@JuriUyesiUnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "JuriUyesiUnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto
                            {
                                Key = "SinavTarihi",
                                Value = isYaziliOrSozlu ?
                                    basvuru.YaziliSinavTarihi.ToFormatDateAndTime()
                                    : basvuru.SozluSinavTarihi.ToFormatDateAndTime()
                            });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto
                            {
                                Key = "SinavYeri",
                                Value = isYaziliOrSozlu ?
                                    basvuru.YaziliSinavYeri
                                    : basvuru.SozluSinavYeri
                            });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavSekli") && basvuru.IsSozluSinavOnline.HasValue)
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SinavSekli", Value = basvuru.IsSozluSinavOnline == true ? "Online" : "Yüz Yüze" });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SinavNotu"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SinavNotu", Value = basvuru.YaziliSinaviNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@YaziliNotu"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YaziliNotu", Value = basvuru.YaziliSinaviNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@SozluNotuOrtalama"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SozluNotuOrtalama", Value = basvuru.SozluSinaviOrtalamaNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@GenelOrtalama"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "GenelOrtalama", Value = basvuru.GenelBasariNotu.ToString() });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OncekiMailTarihi", Value = juri.LinkGonderimTarihi?.ToFormatDateAndTime() });

                        if (item.UniqueID.HasValue && item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/Yeterlik/Index?isKomiteOrJuri=false&isDegerlendirme=" + item.UniqueID, IsLink = true });
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstitu.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = mCOntent.Title
                            };
                            if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGk in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                            }
                            foreach (var itemMe in gonderilenMailEkleri)
                            {
                                kModel.GonderilenMailEkleris.Add(itemMe);
                            }

                            juri.IsSonucOnaylandi = null;
                            juri.SozluNotu = null;
                            juri.DegerlendirmeTarihi = null;
                            juri.LinkGonderimTarihi = DateTime.Now;
                            juri.LinkGonderenID = UserIdentity.Current.Id;
                            juri.IsLinkGonderildi = true;
                            basvuru.GenelBasariNotu = null;
                            basvuru.IsGenelSonucBasarili = null;



                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "";
                //message = isLinkOrSonuc ? "Yeterlik Jüri üyeleri onayı için Komite üyelerine onay davet linki mail olarak gönderilirken bir hata oluştu!" : "Yeterlik Jüri üyeleri onayı  davet linki  Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
                //SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTIDegerlendirmeLink \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                ////mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }


        public static IHtmlString ToYeterlikDurum(this FrYeterlikBasvuruDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Yeterlik", "BasvuruDurumView");
            return pagerString;
        }
        public static IHtmlString ToEnstituBasvuruOnayView(this DmYeterlikDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Yeterlik", "EnstituBasvuruOnayView");
            return pagerString;
        }
        public static IHtmlString ToJuriUyeleriListView(this DmYeterlikDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Yeterlik", "JuriUyeleriListView");
            return pagerString;
        }
        public static IHtmlString ToKomiteOnayDurumView(this DmYeterlikKomite model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Yeterlik", "KomiteOnayDurumView");
            return pagerString;
        }
        public static IHtmlString ToKomiteDegerlendirmeListView(this DmYeterlikDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Yeterlik", "KomiteDegerlendirmeListView");
            return pagerString;
        }
        public static IHtmlString ToSinavView(this DmYeterlikDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Yeterlik", "SinavView");
            return pagerString;
        }
        public static IHtmlString ToYaziliSinavView(this DmYeterlikDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Yeterlik", "YaziliSinavView");
            return pagerString;
        }
        public static IHtmlString ToSozluSinavView(this DmYeterlikDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Yeterlik", "SozluSinavView");
            return pagerString;
        }
        public static IHtmlString ToJuriDegerlendirmeListView(this DmYeterlikDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Yeterlik", "JuriDegerlendirmeListView");
            return pagerString;
        }
        public static IHtmlString ToJuriOnayDurumView(this YeterlikBasvuruJuriUyeleri model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Yeterlik", "JuriOnayDurumView");
            return pagerString;
        }





    }
}