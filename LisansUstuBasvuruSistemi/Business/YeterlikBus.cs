using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class YeterlikBus
    {
        private static readonly List<string> NotDegerleri = new List<string>
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
                                          YsBasSeminerNotKriteri = defYo?.YsBasSeminerNotKriteri
                                      }
                    ).ToList();
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
                    else if (basvuru.IsOnaylandi.HasValue)
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
                        else if (basvuru.IsOnaylandi.HasValue)
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
                    else
                    {
                        if (kul.YtuOgrencisi)
                        {
                            var ogrenimTipAdi = db.OgrenimTipleris.First(p => p.OgrenimTipKod == kul.OgrenimTipKod).OgrenimTipAdi;

                            if (kul.OgrenimDurumID != OgrenimDurum.HalenOğrenci)
                            { 
                                errorMessage.Add("Yeterlik Başvuru işlemini yapabilmeniz için profil kısmındaki öğrenim bilgilerinizde bulunan Öğrenim durumunuzun Halen öğrenci olarak seçilmesi gerekmektedir. (Not: özel öğrenciler bu sistem üzerinden başvuru yapamazlar.)");
                            }
                            else if (kul.KayitDonemID.HasValue == false)
                            { 
                                errorMessage.Add("Kayıt Tarihi Bilginiz Eksik Başvuru Yapamazsınız");
                            }

                            var basvuruVar = db.YeterlikBasvurus.Any(p => p.YeterlikSurecID == yeterlikSurecId && p.KullaniciID == (kayitYetki ? p.KullaniciID : UserIdentity.Current.Id));
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
                                var ogrenciBilgi = Management.StudentControl(kul.TcKimlikNo);
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
                            errorMessage.Add("Yeterlik başvurusu yapabilmeniz için Profil bilginizi düzelterek YTU öğrencisi olduğunuzu belirtiniz.");
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
        public static void SendMailYeterlikOnay(List<int> yeterlikBasvuruIds, bool enstituOnay)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var basvurus = db.YeterlikBasvurus.Where(p => yeterlikBasvuruIds.Contains(p.YeterlikBasvuruID)).ToList();
                foreach (var basvuru in basvurus)
                {
                    var htmlBigliRow = new List<MailTableRowDto>();
                    var contentBilgi = new MailTableContentDto();
                    htmlBigliRow.Add(new MailTableRowDto { Baslik = "Ad Soyad", Aciklama = basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad });
                    if (basvuru.Kullanicilar.YtuOgrencisi)
                    {
                        htmlBigliRow.Add(new MailTableRowDto { Baslik = "Öğrenci No", Aciklama = basvuru.OgrenciNo });
                        htmlBigliRow.Add(new MailTableRowDto { Baslik = "Öğrenim Seviyesi", Aciklama = basvuru.OgrenimTipleri.OgrenimTipAdi });
                        htmlBigliRow.Add(new MailTableRowDto { Baslik = "Program", Aciklama = basvuru.Programlar.ProgramAdi });
                    }

                    htmlBigliRow.Add(new MailTableRowDto { Baslik = "Başvuru Tarihi", Aciklama = basvuru.BasvuruTarihi.ToFormatDateAndTime() });
                    htmlBigliRow.Add(new MailTableRowDto { Baslik = "Başvuru Enstitu Onay Durumu", Aciklama = (enstituOnay ? "Başvurunuz Enstitu Tarafından Onaylandı." : "Başvurunuz Enstitü Tarafından İptal Edildi.") });
                    if (!enstituOnay) htmlBigliRow.Add(new MailTableRowDto { Baslik = "İptal Açıklaması", Aciklama = basvuru.OnayAciklama });

                    contentBilgi.GrupBasligi = "Yeterlik başvurusu detayı";
                    contentBilgi.Detaylar = htmlBigliRow;

                    var mmmC = new MailMainContentDto();
                    var enstituAdi = basvuru.YeterlikSureci.Enstituler.EnstituAd;
                    mmmC.EnstituAdi = enstituAdi;
                    mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                    var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(basvuru.YeterlikSureci.EnstituKod);
                    var erisimAdresi = mailBilgi.SistemErisimAdresi;
                    var wurlAddr = erisimAdresi.Split('/').ToList();
                    if (erisimAdresi.Contains("//"))
                        erisimAdresi = wurlAddr[0] + "//" + wurlAddr.Skip(2).Take(1).First();
                    else
                        erisimAdresi = "http://" + wurlAddr.First();
                    mmmC.LogoPath = erisimAdresi + "/Content/assets/images/ytu_logo_tr.png";
                    var hcb = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", contentBilgi);
                    mmmC.Content = hcb;
                    string htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);
                    var emailSend = MailManager.SendMail(mailBilgi.EnstituKod, "Yeterlik Başvurunuz Hk.", htmlMail, basvuru.Kullanicilar.EMail, null);

                    if (emailSend)
                    {
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = mailBilgi.EnstituKod,
                            MesajID = null,
                            Konu = "Yeterlik Başvurusu Onay İşlemi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " [" + (enstituOnay ? "Onaylandı." : "İptal Edildi.") + "])",
                            Aciklama = "",
                            AciklamaHtml = htmlMail,
                            IslemYapanID = UserIdentity.Current.Id,
                            IslemTarihi = DateTime.Now,
                            IslemYapanIP = UserIdentity.Ip,
                            Gonderildi = true,
                            GonderilenMailKullanicilars = new List<GonderilenMailKullanicilar>()
                        };

                        kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = basvuru.Kullanicilar.EMail });
                        db.GonderilenMaillers.Add(kModel);
                        db.SaveChanges();
                    }
                }

            }
        }

        public static IHtmlString ToYeterlikDurum(this FrYeterlikBasvuruDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Yeterlik", "BasvuruDurumView");
            return pagerString;
        }
    }
}