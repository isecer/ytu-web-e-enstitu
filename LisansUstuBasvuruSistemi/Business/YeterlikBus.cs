using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

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


        public static bool IsHarfNotuBuyukEsit(string notKriteri, string ogrenciNotu)
        {
            var notKriteriIndex = NotDegerleri.IndexOf(notKriteri);
            var ogrenciNotuIndex = NotDegerleri.IndexOf(ogrenciNotu);
            var success = notKriteriIndex <= ogrenciNotuIndex;
            if (!success)
            {
                //geçmiş öğrenciler için özel kontrol G notu geçerli
                success = ogrenciNotu == "G";
            }
            return success;
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
                        var message = "Bu enstitüye ait Yeterlik başvurusu silmeye yetkili değilsiniz!\r\n Yeterlik Başvuru ID: " + basvuru.YeterlikBasvuruID + " \r\n Yeterlik Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi;
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Yeterlik Başvuru Sil", LogTipiEnum.Kritik);
                    }
                    else if (!GetYeterlikAktifSurecId(basvuru.YeterlikSureci.EnstituKod, basvuru.YeterlikSurecID).HasValue && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Yeterlik başvurusunu silmeye hakkınız yoktur! \r\n Silinmeye çalışılan Yeterlik Başvuru ID:" + basvuru.YeterlikBasvuruID + " \r\n Yeterlik Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi, "Başvuru Sil", LogTipiEnum.Saldırı);
                    }
                    else if (basvuru.IsEnstituOnaylandi.HasValue)
                    {
                        msg.Messages.Add("Enstitü tarafından işlem gören bir başvuru silinemez!");
                    }
                }
            }
            msg.IsSuccess = !msg.Messages.Any();
            msg.MessageType = msg.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Information;
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
                            var message = "Bu enstitüye ait Yeterlik başvurusu güncellemeye yetkili değilsiniz!\r\n Yeterlik Başvuru ID: " + basvuru.YeterlikBasvuruID + " \r\n Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi;
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Başvuru Düzelt", LogTipiEnum.Saldırı);
                        }
                        else if (!GetYeterlikAktifSurecId(enstituKod, basvuru.YeterlikSurecID).HasValue && UserIdentity.Current.IsAdmin == false)
                        {
                            errorMessage.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                        }
                        if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            errorMessage.Add("Bu İşlem için Yetkili Değilsiniz.");
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Yeterlik başvurusu düzenlemeye hakkınız yoktur! \r\n Çağrılan Yeterlik Başvuru ID:" + basvuru.YeterlikBasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Yeterlik Başvuru Düzelt", LogTipiEnum.Saldırı);
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

                            if (kul.OgrenimDurumID != OgrenimDurumEnum.HalenOğrenci)
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
                                var ogrenciBilgi = KullanicilarBus.OgrenciKontrol(kul.TcKimlikNo);
                                var controlMessage = new List<string>();
                                var basvuruKriterleri = db.YeterlikSurecOgrenimTipleris.FirstOrDefault(p => p.YeterlikSurecID == yeterlikSurecId.Value && p.OgrenimTipKod == kul.OgrenimTipKod);
                                if (basvuruKriterleri == null)
                                {
                                    errorMessage.Add("Okuduğunuz öğrenim seviyesi yeterlik başvuru yapmak için uygun değildir.");
                                }
                                else
                                {
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
                    lst.Add(new CmbIntDto { Value = item.YeterlikSurecID, Caption = (item.BaslangicYil + "/" + item.BitisYil + " " + item.DonemAdi + " (" + item.BaslangicTarihi.ToFormatDate() + " - " + item.BitisTarihi.ToFormatDate() + ")") });
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
            return MailSenderYeterlik.SendMailBasvuruOnayi(basvuruUniqueId);
        }
        public static MmMessage SendMailKomiteDegerlendirmeLink(Guid yeterlikBasvuruUniqueId, Guid? komiteUniqueId)
        {
            return MailSenderYeterlik.SendMailKomiteDegerlendirmeLink(yeterlikBasvuruUniqueId, komiteUniqueId);
        }
        public static MmMessage SendMailKomiteDegerlendirmeSonuc(Guid yeterlikBasvuruUniqueId)
        {
            return MailSenderYeterlik.SendMailKomiteDegerlendirmeSonuc(yeterlikBasvuruUniqueId);
        }
        public static MmMessage SendMailSinavBilgi(Guid basvuruUniqueId, bool isYaziliOrSozlu = true, Guid? uniqueId = null)
        {
            return MailSenderYeterlik.SendMailSinavBilgi(basvuruUniqueId, isYaziliOrSozlu, uniqueId);
        }
        public static MmMessage SendMailSinavJuriLink(Guid basvuruUniqueId, bool isYaziliOrSozlu = true, Guid? uniqueId = null)
        {
            return MailSenderYeterlik.SendMailSinavJuriLink(basvuruUniqueId, isYaziliOrSozlu, uniqueId);
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