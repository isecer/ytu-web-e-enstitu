using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Business
{
    public class YeterlikBus
    {
        private static Dictionary<int, string> NotDegerleri = new Dictionary<int, string>
        {
            { 1, "CC" },
            { 2, "CB" },
            { 3, "BB" },
            { 4, "BA" },
            { 5, "AA" },
        };
        public static bool IsNotBuyukEsit(string not1, string not2)
        {
            var not1Key = NotDegerleri.FirstOrDefault(p => p.Value == not1);
            var not2Key = NotDegerleri.FirstOrDefault(p => p.Value == not2);
            return not1Key.Key <= not2Key.Key;
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

                var ogrenimtipData = (from o in ogrenimTipleri
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
                                          YsBasToplamKrediKriteri = defYo?.YsBasToplamKrediKriteri,
                                          YsBasEtikNotKriteri = defYo?.YsBasEtikNotKriteri,
                                          YsBasSeminerNotKriteri = defYo?.YsBasSeminerNotKriteri

                                      }
                    ).ToList();
                return ogrenimtipData;

            }
        }
        public static void SiraNoVer()
        {
            using (var _context = new LisansustuBasvuruSistemiEntities())
            {
                var surecs = (from s in _context.YeterlikSurecis
                              group new { s.YeterlikSurecID, s.BaslangicYil, s.BitisYil, s.BaslangicTarihi, s.BitisTarihi } by
                                  new { s.BaslangicYil, s.BitisYil, s.DonemID }
                    into g1
                              select new
                              {
                                  g1.Key.BaslangicYil,
                                  g1.Key.DonemID,
                                  data = g1.OrderBy(o => o.BaslangicTarihi).ToList()
                              }).ToList();
                foreach (var item in surecs)
                {
                    int inx = 1;
                    foreach (var item2 in item.data)
                    {
                        var src = _context.YeterlikSurecis.First(p => p.YeterlikSurecID == item2.YeterlikSurecID);
                        src.SiraNo = inx;
                        inx++;
                    }
                }

                _context.SaveChanges();
            }
        }
        public static MmMessage YeterlikBasvurusuSilKontrol(int yeterlikBasvurulariId)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kayitYetki = RoleNames.YeterlikGelenBasvurularKayit.InRoleCurrent();
                var basvuru = db.YeterlikBasvurus.FirstOrDefault(p => p.YeterlikBasvuruID == yeterlikBasvurulariId);
                if (basvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Silinmek istenen başvuru sistemde bulunamadı.");
                }
                else
                {
                    if (!UserIdentity.Current.EnstituKods.Contains(basvuru.YeterlikSureci.EnstituKod) && kayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                        var message = "Bu enstitüye ait Yeterlik başvurusu silmeye yetkili değilsiniz!\r\n Yeterlik Başvuru ID: " + basvuru.YeterlikBasvuruID + " \r\n Yeterlik Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Yeterlik Başvuru Sil", LogType.Kritik);
                    }
                    else if (!GetYeterlikAktifSurecId(basvuru.YeterlikSureci.EnstituKod, basvuru.YeterlikSurecID).HasValue && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Yeterlik başvurusunu silmeye hakkınız yoktur! \r\n Silinmeye çalışılan Yeterlik Başvuru ID:" + basvuru.YeterlikBasvuruID + " \r\n Yeterlik Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi.ToString(), "Başvuru Sil", LogType.Saldırı);
                    } 
                }
            }
            return msg;
        }
        public static MmMessage YeterlikBasvuruKontrol(string enstituKod, int? yeterlikBasvuruId = null)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kayitYetki = RoleNames.YeterlikGelenBasvurularKayit.InRoleCurrent();
                if (yeterlikBasvuruId > 0)
                {
                    var basvuru = db.YeterlikBasvurus.FirstOrDefault(p => p.YeterlikBasvuruID == yeterlikBasvuruId.Value);
                    if (basvuru == null)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                        if (kayitYetki == false) SistemBilgilendirmeBus.SistemBilgisiKaydet("Aranan başvuru sistemde bulunamadı! \r\n Çağrılan Yeterlik Başvuru ID:" + yeterlikBasvuruId, "Yeterlik Başvuru Düzelt", LogType.Uyarı);
                    }
                    else
                    {
                        if (basvuru.YeterlikSureci.EnstituKod != enstituKod)
                        {
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Seçilen Yeterlik başvurusu Enstitü kodu ile aktif Enstitü kodu uyuşmuyor! \r\n Çağrılan Yeterlik Başvuru Enstitü Kod:" + basvuru.YeterlikSureci.EnstituKod + " \r\n Aktif Enstitü Kod:" + enstituKod + " \r\n Çağrılan Yeterlik Başvuru ID:" + basvuru.YeterlikBasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Yeterlik Başvuru Düzelt", LogType.Uyarı);
                            enstituKod = basvuru.YeterlikSureci.EnstituKod;
                        }
                        if (!UserIdentity.Current.EnstituKods.Contains(basvuru.YeterlikSureci.EnstituKod) && kayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu Enstitü İçin Yetkili Değilsiniz.");
                            var message = "Bu enstitüye ait Yeterlik başvurusu güncellemeye yetkili değilsiniz!\r\n Yeterlik Başvuru ID: " + basvuru.YeterlikBasvuruID + " \r\n Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Başvuru Düzelt", LogType.Saldırı);
                        }
                        else if (!GetYeterlikAktifSurecId(enstituKod, basvuru.YeterlikSurecID).HasValue && UserIdentity.Current.IsAdmin == false)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                        }
                        if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu İşlem için Yetkili Değilsiniz.");
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Yeterlik başvurusu düzenlemeye hakkınız yoktur! \r\n Çağrılan Yeterlik Başvuru ID:" + basvuru.YeterlikBasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Yeterlik Başvuru Düzelt", LogType.Saldırı);
                        }


                    }
                }
                else
                {
                    int? yeterlikSurecId = GetYeterlikAktifSurecId(enstituKod);
                    msg.IsSuccess = yeterlikSurecId.HasValue;
                    var kul = db.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
                    if (msg.IsSuccess == false)
                    {
                        msg.Messages.Add("Başvuru Süreci Kapalı");
                    }
                    else if (!(kul.KullaniciTipleri.BasvuruYapabilir))
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Kullanıcı Hesap Türünüz için Başvuru İşlemleri Kapalıdır.");
                    }
                    else
                    {
                        if (kul.YtuOgrencisi)
                        {
                            var ogrenimTipAdi = db.OgrenimTipleris.First(p => p.OgrenimTipKod == kul.OgrenimTipKod).OgrenimTipAdi;

                            if (kul.OgrenimDurumID != OgrenimDurum.HalenOğrenci)
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Yeterlik Başvuru işlemini yapabilmeniz için profil kısmındaki öğrenim bilgilerinizde bulunan Öğrenim durumunuzun Halen öğrenci olarak seçilmesi gerekmektedir. (Not: özel öğrenciler bu sistem üzerinden başvuru yapamazlar.)");
                            }
                            else if (kul.KayitDonemID.HasValue == false)
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Kayıt Tarihi Bilginiz Eksik Başvuru Yapamazsınız");
                            }

                            var basvuruVar = db.YeterlikBasvurus.Any(p => p.YeterlikSurecID == yeterlikSurecId && p.KullaniciID == (kayitYetki ? p.KullaniciID : UserIdentity.Current.Id));
                            if (basvuruVar)
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Bu Yeterlik süreci için başvurunuz bulunmaktadır tekrar başvuru yapamazsınız!");

                            }
                            else if (db.YeterlikSurecOgrenimTipleris.Any(a => a.YeterlikSurecID == yeterlikSurecId.Value && a.OgrenimTipKod != kul.OgrenimTipKod) == false)
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add(ogrenimTipAdi + " Öğrenim seviyesinde okuyan öğrenciler Yeterlik başvurusu yapamazlar");
                            }
                            else
                            {
                                var basvuruKriterleri = db.YeterlikSurecOgrenimTipleris.First(p => p.YeterlikSurecID == yeterlikSurecId.Value && p.OgrenimTipKod == kul.OgrenimTipKod);

                                var ogrenciBilgi = Management.StudentControl(kul.TcKimlikNo);
                                var controlMessage = new List<string>();
                                if (!IsNotBuyukEsit(basvuruKriterleri.YsBasEtikNotKriteri, ogrenciBilgi.AktifDonemDers.EtikDersNotu))
                                {
                                    controlMessage.Add("Etik dersi için ders notu " + basvuruKriterleri.YsBasEtikNotKriteri + " veya daha üstü bir not almanız gerekmektedir.");
                                }
                                if (!IsNotBuyukEsit(basvuruKriterleri.YsBasSeminerNotKriteri, ogrenciBilgi.AktifDonemDers.SeminerDersNotu))
                                {
                                    controlMessage.Add("Seminer dersi için ders notu " + basvuruKriterleri.YsBasSeminerNotKriteri + " veya daha üstü bir not almanız gerekmektedir.");
                                }
                                if (basvuruKriterleri.YsBasToplamKrediKriteri > ogrenciBilgi.AktifDonemDers.ToplamKredi)
                                {
                                    controlMessage.Add("Toplam Kredi sayınız " + basvuruKriterleri.YsBasToplamKrediKriteri + " krediden büyük ya da eşit olmalıdır. Mevcut Kredi: " + ogrenciBilgi.AktifDonemDers.ToplamKredi);
                                }
                                if (controlMessage.Count > 0)
                                {
                                    msg.Messages.Add(ogrenimTipAdi + " Yeterlik başvurunuz aşağıdaki sebeplerden dolayı başlatılamadı.");
                                    msg.Messages.AddRange(controlMessage);
                                    msg.IsSuccess = false;
                                }
                            }
                        }
                        else
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Yeterlik başvurusu yapabilmeniz için Profil bilginizi düzelterek YTU öğrencisi olduğunuzu belirtiniz.");
                        }
                    }
                }

            }
            return msg;

        }
    }
}