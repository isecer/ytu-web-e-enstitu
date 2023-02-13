using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
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

namespace LisansUstuBasvuruSistemi.Business
{
    public static class MezuniyetBus
    {
        public static int? GetMezuniyetAktifSurecId(string enstituKod, int? mezuniyetSurecId = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var nowDate = DateTime.Now;
                var bf = db.MezuniyetSurecis.Where(p => (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif && (p.EnstituKod == enstituKod) && p.MezuniyetSurecID == (mezuniyetSurecId.HasValue ? mezuniyetSurecId.Value : p.MezuniyetSurecID));
                var qBf = bf.FirstOrDefault();
                int? id = null;
                if (qBf != null) id = qBf.MezuniyetSurecID;
                return id;
            }
        }
        public static MmMessage MezuniyetBasvurusuSilKontrol(int mezuniyetBasvurulariId)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
                var basvuru = db.MezuniyetBasvurularis.FirstOrDefault(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                if (basvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                }
                else
                {
                    if (!UserIdentity.Current.EnstituKods.Contains(basvuru.MezuniyetSureci.EnstituKod) && kayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                        var message = "Bu enstitüye ait mezuniyet başvurusu silmeye yetkili değilsiniz!\r\n Mezuniyet Başvuru ID: " + basvuru.MezuniyetBasvurulariID + " \r\n Mezuniyet Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Mezuniyet Başvuru Sil", LogType.Kritik);
                    }
                    else if (!GetMezuniyetAktifSurecId(basvuru.MezuniyetSureci.EnstituKod, basvuru.MezuniyetSurecID).HasValue && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait mezuniyet başvurusunu silmeye hakkınız yoktur! \r\n Silinmeye çalışılan Mezuniyet Başvuru ID:" + basvuru.MezuniyetBasvurulariID + " \r\n Mezuniyet Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi.ToString(), "Başvuru Sil", LogType.Saldırı);
                    }
                    else if (kayitYetki == false && basvuru.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumu.Taslak)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Taslak Harici Başvurular Silinemez.");
                    }
                }
            }
            return msg;
        }
        public static MmMessage MezuniyetSurecAktifKontrol(string enstituKod, int? kullaniciId, int? mezuniyetBasvurulariId = null)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
                if (mezuniyetBasvurulariId.HasValue)
                {
                    var basvuru = db.MezuniyetBasvurularis.FirstOrDefault(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId.Value);
                    if (basvuru == null)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                        if (kayitYetki == false) SistemBilgilendirmeBus.SistemBilgisiKaydet("Aranan başvuru sistemde bulunamadı! \r\n Çağrılan Mezuniyet Başvuru ID:" + mezuniyetBasvurulariId, "Mezuniyet Başvuru Düzelt", LogType.Uyarı);
                    }
                    else
                    {
                        if (basvuru.MezuniyetSureci.EnstituKod != enstituKod)
                        {
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Seçilen Mezuniyet başvurusu Enstitü kodu ile aktif Enstitü kodu uyuşmuyor! \r\n Çağrılan Mezuniyet Başvuru Enstitü Kod:" + basvuru.MezuniyetSureci.EnstituKod + " \r\n Aktif Enstitü Kod:" + enstituKod + " \r\n Çağrılan Mezuniyet Başvuru ID:" + basvuru.MezuniyetBasvurulariID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Mezuniyet Başvuru Düzelt", LogType.Uyarı);
                            enstituKod = basvuru.MezuniyetSureci.EnstituKod;
                        }
                        if (!UserIdentity.Current.EnstituKods.Contains(basvuru.MezuniyetSureci.EnstituKod) && kayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu Enstitü İçin Yetkili Değilsiniz.");
                            var message = "Bu enstitüye ait mezuniyet başvurusu güncellemeye yetkili değilsiniz!\r\n Mezuniyet Başvuru ID: " + basvuru.MezuniyetBasvurulariID + " \r\n Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Başvuru Düzelt", LogType.Saldırı);
                        }
                        else if (!GetMezuniyetAktifSurecId(enstituKod, basvuru.MezuniyetSurecID).HasValue && UserIdentity.Current.IsAdmin == false)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                        }
                        if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu İşlem için Yetkili Değilsiniz.");
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Mezuniyet başvurusu düzenlemeye hakkınız yoktur! \r\n Çağrılan Mezuniyet Başvuru ID:" + basvuru.MezuniyetBasvurulariID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Mezuniyet Başvuru Düzelt", LogType.Saldırı);
                        }
                        else if (kayitYetki == false && (basvuru.IsDanismanOnay == true))
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Danışman tarafından onaylanan başvurunuzda düzenleme işlemi yapamazsınız!");
                        }

                    }
                }
                else
                {
                    int? mezuniyetSurecId = GetMezuniyetAktifSurecId(enstituKod);
                    msg.IsSuccess = mezuniyetSurecId.HasValue;
                    if (kullaniciId.HasValue == false) kullaniciId = UserIdentity.Current.Id;
                    else if (kullaniciId != UserIdentity.Current.Id && RoleNames.KullaniciAdinaBasvuruYap.InRoleCurrent() == false && UserIdentity.Current.IsAdmin == false)
                    {
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya adına başvuru yapılmak isteniyor! \r\n Başvuru yapılmak istenen Kullanıcı ID:" + kullaniciId + " \r\n İşlem Yapan Kullanıcı ID:" + UserIdentity.Current.Id, "Mezuniyet Başvury Yap", LogType.Saldırı);
                        kullaniciId = UserIdentity.Current.Id;
                    }
                    var kul = db.Kullanicilars.First(p => p.KullaniciID == kullaniciId.Value);
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
                            if (kul.OgrenimDurumID != OgrenimDurum.HalenOğrenci)
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Mezuniyet Başvuru işlemini yapabilmeniz için profil kısmındaki öğrenim bilgilerinizde bulunan Öğrenim durumunuzun Halen öğrenci olarak seçilmesi gerekmektedir. (Not: özel öğrenciler bu sistem üzerinden başvuru yapamazlar.)");
                            }
                            else if (kul.KayitDonemID.HasValue == false)
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Kayıt Tarihi Bilginiz Eksik Başvuru Yapamazsınız");
                            }

                            var basvuruVar = db.MezuniyetBasvurularis.Any(p => p.MezuniyetSurecID == mezuniyetSurecId &&
                                                                               p.KullaniciID == kullaniciId);
                            if (basvuruVar)
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Bu mezuniyet süreci için başvurunuz bulunmaktadır tekrar başvuru yapamazsınız!");

                            }
                            else if (GetMezuniyetSureciOgrenimTipUygunMu(mezuniyetSurecId.Value, kullaniciId.Value) == false)
                            {
                                var otsAdi = db.OgrenimTipleris.First(p => p.OgrenimTipKod == kul.OgrenimTipKod).OgrenimTipAdi;
                                msg.IsSuccess = false;
                                msg.Messages.Add(otsAdi + " Öğrenim seviyesinde okuyan öğrenciler mezuniyet başvurusu yapamazlar");
                            }
                            else if ((kullaniciId != UserIdentity.Current.Id && RoleNames.KullaniciAdinaBasvuruYap.InRoleCurrent() == false) && kul.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans && (kul.KayitTarihi > Convert.ToDateTime("31-03-2016") && MezuniyetBus.GetCmbOkunanDonemList(mezuniyetSurecId.Value, kul.KayitYilBaslangic.Value, kul.KayitDonemID.Value).Count < 4))
                            {
                                var otsAdi = db.OgrenimTipleris.First(p => p.OgrenimTipKod == kul.OgrenimTipKod).OgrenimTipAdi;
                                msg.IsSuccess = false;
                                msg.Messages.Add(otsAdi + " öğrenim seviyesi okuyan öğrencilerin mezuniyet başvurusu için en az 4 dönem okumaları gerekmektedir.");
                            }
                            else
                            {
                                var basvuruKriterleri = db.MezuniyetSureciOgrenimTipKriterleris.First(p => p.MezuniyetSurecID == mezuniyetSurecId.Value && p.OgrenimTipKod == kul.OgrenimTipKod);
                                var basvuruSonDonemSecilecekDersKodlari = basvuruKriterleri.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari.Split(',').Where(p => !p.IsNullOrWhiteSpace()).ToList();
                                var ogrenciBilgi = Management.StudentControl(kul.TcKimlikNo);
                                var bkMsg = new List<string>();
                                if (basvuruSonDonemSecilecekDersKodlari.Any() && ogrenciBilgi.AktifDonemDers.DersKodNums.Count(p => basvuruSonDonemSecilecekDersKodlari.Any(a => a == p)) != basvuruSonDonemSecilecekDersKodlari.Count)
                                {
                                    bkMsg.Add(string.Join(", ", basvuruSonDonemSecilecekDersKodlari) + " kodlu derslere son dönemde kayıt yaptırmanız gerekmektedi.");
                                }
                                if (basvuruKriterleri.MBasvuruToplamKrediKriteri > ogrenciBilgi.AktifDonemDers.ToplamKredi)
                                {
                                    bkMsg.Add("Toplam Kredi sayınız " + basvuruKriterleri.MBasvuruToplamKrediKriteri + " krediden büyük ya da eşit olmalıdır. Mevcut Kredi: " + ogrenciBilgi.AktifDonemDers.ToplamKredi);

                                }
                                if (basvuruKriterleri.MBasvuruAGNOKriteri > ogrenciBilgi.AktifDonemDers.Agno)
                                {
                                    bkMsg.Add("Ortalamanız " + basvuruKriterleri.MBasvuruAGNOKriteri + " ortalamasından büyük ya da eşit olmalıdır. Mevcut Ortalama: " + ogrenciBilgi.AktifDonemDers.Agno.ToString("n2"));

                                }
                                if (basvuruKriterleri.MBasvuruAKTSKriteri > ogrenciBilgi.AktifDonemDers.ToplamAkts)
                                {
                                    bkMsg.Add("Akts toplamınız " + basvuruKriterleri.MBasvuruAKTSKriteri + " akts'den büyük ya da eşit olmalıdır. Mevcut Akts: " + ogrenciBilgi.AktifDonemDers.ToplamAkts);

                                }
                                if (bkMsg.Count > 0)
                                {
                                    var otsAdi = db.OgrenimTipleris.First(p => p.OgrenimTipKod == kul.OgrenimTipKod).OgrenimTipAdi;
                                    msg.Messages.Add(otsAdi + " mezuniyet başvurunuz aşağıdaki sebeplerden dolayı başlatılamadı.");
                                    msg.Messages.AddRange(bkMsg);
                                    msg.IsSuccess = false;
                                }
                            }
                        }
                        else
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Mezuniyet başvurusu yapabilmeniz için Profil bilginizi düzelterek YTU öğrencisi olduğunuzu belirtiniz.");
                        }
                    }
                }

            }
            return msg;

        }
        public static kmMezuniyetBasvuru GetMezuniyetBasvuruBilgi(int mezuniyetBasvurulariId)
        {
            var model = new kmMezuniyetBasvuru();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var basvuru = db.MezuniyetBasvurularis.Include("MezuniyetYayinKontrolDurumlari").FirstOrDefault(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                var kul = db.Kullanicilars.First(p => p.KullaniciID == basvuru.KullaniciID);

                #region BasvuruBilgi
                model.EnstituKod = basvuru.MezuniyetSureci.EnstituKod;
                model.MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID;
                model.MezuniyetSurecID = basvuru.MezuniyetSurecID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.MezuniyetYayinKontrolDurumID = basvuru.MezuniyetYayinKontrolDurumID;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;
                if (kul.KullaniciTipID != basvuru.KullaniciTipID)
                {
                    model.KullaniciTipID = kul.KullaniciTipID;
                    model.ResimAdi = kul.ResimAdi;
                    model.Ad = kul.Ad;
                    model.Soyad = kul.Soyad;
                    model.TcKimlikNo = kul.TcKimlikNo;
                    model.PasaportNo = kul.PasaportNo;


                }
                else
                {
                    model.KullaniciTipID = basvuru.KullaniciTipID;
                    model.ResimAdi = basvuru.ResimAdi;
                    model.Ad = basvuru.Ad;
                    model.Soyad = basvuru.Soyad;
                    model.TcKimlikNo = basvuru.TcKimlikNo;
                    model.PasaportNo = basvuru.PasaportNo;
                    model.UyrukKod = basvuru.UyrukKod;
                }
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = db.OgrenimTipleris.First(p => p.EnstituKod == model.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).OgrenimTipAdi;
                var progLng = basvuru.Programlar;
                model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = progLng.ProgramAdi;
                model.OgrenimDurumID = basvuru.OgrenimDurumID;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
                model.ProgramKod = basvuru.ProgramKod;
                model.KayitOgretimYiliBaslangic = basvuru.KayitOgretimYiliBaslangic;
                model.KayitOgretimYiliDonemID = basvuru.KayitOgretimYiliDonemID;
                model.KayitTarihi = basvuru.KayitTarihi;
                model.IsTezDiliTr = basvuru.IsTezDiliTr;
                model.TezBaslikTr = basvuru.TezBaslikTr;
                model.TezBaslikEn = basvuru.TezBaslikEn;
                model.TezDanismanAdi = basvuru.TezDanismanAdi;
                model.TezDanismanUnvani = basvuru.TezDanismanUnvani;
                model.TezEsDanismanAdi = basvuru.TezEsDanismanAdi;
                model.TezEsDanismanUnvani = basvuru.TezEsDanismanUnvani;
                model.TezEsDanismanEMail = basvuru.TezEsDanismanEMail;
                model.TezOzet = basvuru.TezOzet;
                model.OzetAnahtarKelimeler = basvuru.OzetAnahtarKelimeler;
                model.TezOzetHtml = basvuru.TezOzetHtml;
                model.TezAbstract = basvuru.TezAbstract;
                model.TezAbstractHtml = basvuru.TezAbstractHtml;
                model.AbstractAnahtarKelimeler = basvuru.AbstractAnahtarKelimeler;
                model.DanismanImzaliFormDosyaAdi = basvuru.DanismanImzaliFormDosyaAdi;
                model.DanismanImzaliFormDosyaYolu = basvuru.DanismanImzaliFormDosyaYolu;
                model.IslemTarihi = DateTime.Now;
                model.IslemYapanID = UserIdentity.Current.Id;
                model.IslemYapanIP = UserIdentity.Ip;
                #endregion

                var yayins = (from qs in db.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId)
                              join s in db.MezuniyetSureciYayinTurleris on new { qs.MezuniyetBasvurulari.MezuniyetSurecID, qs.MezuniyetYayinTurID } equals new { s.MezuniyetSurecID, s.MezuniyetYayinTurID }
                              join sd in db.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                              join yb in db.MezuniyetYayinBelgeTurleris on new { s.MezuniyetYayinBelgeTurID } equals new { MezuniyetYayinBelgeTurID = (int?)yb.MezuniyetYayinBelgeTurID } into defyb
                              from ybD in defyb.DefaultIfEmpty()
                              join klk in db.MezuniyetYayinLinkTurleris on new { s.KaynakMezuniyetYayinLinkTurID } equals new { KaynakMezuniyetYayinLinkTurID = (int?)klk.MezuniyetYayinLinkTurID } into defklk
                              from klkD in defklk.DefaultIfEmpty()
                              join ym in db.MezuniyetYayinMetinTurleris on new { s.MezuniyetYayinMetinTurID } equals new { MezuniyetYayinMetinTurID = (int?)ym.MezuniyetYayinMetinTurID } into defym
                              from ymD in defym.DefaultIfEmpty()
                              join kl in db.MezuniyetYayinLinkTurleris on new { s.YayinMezuniyetYayinLinkTurID } equals new { YayinMezuniyetYayinLinkTurID = (int?)kl.MezuniyetYayinLinkTurID } into defkl
                              from klD in defkl.DefaultIfEmpty()
                              join inx in db.MezuniyetYayinIndexTurleris on new { qs.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into definx
                              from inxD in definx.DefaultIfEmpty()
                              select new MezuniyetBasvurulariYayinDto
                              {
                                  Yayinlanmis = qs.Yayinlanmis,
                                  MezuniyetBasvurulariYayinID = qs.MezuniyetBasvurulariYayinID,
                                  YayinBasligi = qs.YayinBasligi,
                                  MezuniyetYayinTarih = qs.MezuniyetYayinTarih,
                                  MezuniyetYayinTarihZorunlu = s.TarihIstensin,
                                  MezuniyetYayinTurID = qs.MezuniyetYayinTurID,
                                  MezuniyetYayinTurAdi = sd.MezuniyetYayinTurAdi,
                                  MezuniyetYayinBelgeTurID = s.MezuniyetYayinBelgeTurID,
                                  MezuniyetYayinBelgeTurAdi = ybD != null ? ybD.BelgeTurAdi : "",
                                  MezuniyetYayinBelgeAdi = ybD != null ? qs.MezuniyetYayinBelgeAdi : "",
                                  MezuniyetYayinBelgeDosyaYolu = ybD != null ? qs.MezuniyetYayinBelgeDosyaYolu : "",
                                  MezuniyetYayinBelgeTurZorunlu = s.BelgeZorunlu,
                                  MezuniyetYayinKaynakLinkTurID = s.KaynakMezuniyetYayinLinkTurID,
                                  MezuniyetYayinKaynakLinkTurAdi = klkD != null ? klkD.LinkTurAdi : "",
                                  MezuniyetYayinKaynakLinkIsUrl = klkD != null ? klkD.IsUrl : false,
                                  MezuniyetYayinKaynakLinkTurZorunlu = s.KaynakLinkiZorunlu,
                                  MezuniyetYayinMetinTurID = s.MezuniyetYayinMetinTurID,
                                  MezuniyetYayinMetinTurAdi = ymD != null ? ymD.MetinTurAdi : "",
                                  MezuniyetYayinMetniBelgeAdi = ymD != null ? qs.MezuniyetYayinMetniBelgeAdi : "",
                                  MezuniyetYayinMetniBelgeYolu = qs.MezuniyetYayinMetniBelgeYolu,
                                  MezuniyetYayinMetinZorunlu = s.MetinZorunlu,
                                  MezuniyetYayinLinkTurID = s.YayinMezuniyetYayinLinkTurID,
                                  MezuniyetYayinLinkTurAdi = klD != null ? klD.LinkTurAdi : "",
                                  MezuniyetYayinLinkIsUrl = klD != null ? klD.IsUrl : false,
                                  MezuniyetYayinLinkiZorunlu = s.YayinLinkiZorunlu,
                                  MezuniyetYayinKaynakLinki = qs.MezuniyetYayinKaynakLinki,
                                  MezuniyetYayinLinki = qs.MezuniyetYayinLinki,
                                  MezuniyetYayinIndexTurZorunlu = s.YayinIndexTurIstensin,
                                  MezuniyetYayinIndexTurAdi = inxD != null ? inxD.IndexTurAdi : "",
                                  MezuniyetKabulEdilmisMakaleZorunlu = s.YayinKabulEdilmisMakaleIstensin,
                                  MezuniyetYayinKabulEdilmisMakaleAdi = qs.MezuniyetYayinKabulEdilmisMakaleAdi,
                                  MezuniyetYayinKabulEdilmisMakaleDosyaYolu = qs.MezuniyetYayinKabulEdilmisMakaleDosyaYolu,
                                  YayinDeatKurulusIstensin = s.YayinDeatKurulusIstensin,
                                  ProjeDeatKurulus = qs.ProjeDeatKurulus,
                                  YayinDergiAdiIstensin = s.YayinDergiAdiIstensin,
                                  DergiAdi = qs.DergiAdi,
                                  YayinMevcutDurumIstensin = s.YayinMevcutDurumIstensin,
                                  IsProjeTamamlandiOrDevamEdiyor = qs.IsProjeTamamlandiOrDevamEdiyor,
                                  YayinProjeEkibiIstensin = s.YayinProjeEkibiIstensin,
                                  ProjeEkibi = qs.ProjeEkibi,
                                  YayinProjeTurIstensin = s.YayinProjeTurIstensin,
                                  ProjeTurAdi = qs.MezuniyetYayinProjeTurID.HasValue ? qs.MezuniyetYayinProjeTurleri.ProjeTurAdi : "",
                                  YayinYazarlarIstensin = s.YayinYazarlarIstensin,
                                  YazarAdi = qs.YazarAdi,
                                  YayinYilCiltSayiIstensin = s.YayinYilCiltSayiIstensin,
                                  YilCiltSayiSS = qs.YilCiltSayiSS,
                                  IsTarihAraligiIstensin = s.IsTarihAraligiIstensin,
                                  TarihAraligi = qs.TarihAraligi


                              }).ToList();

                model.MezuniyetBasvuruYayinlari = yayins;
                if (basvuru.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.Onaylandi) model.Onaylandi = true;

            }
            return model;

        }
        public static MezuniyetBasvuruDetayDto GetMezuniyetBasvuruDetayBilgi(int mezuniyetBasvurulariId, int? mezuniyetBasvurulariYayinId = null, int? showDetayYayinId = null)
        {
            var model = new MezuniyetBasvuruDetayDto();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var basvuru = db.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);

                var bsurec = basvuru.MezuniyetSureci;
                var bSurecOtKriter = bsurec.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == basvuru.OgrenimTipKod);
                var enstitu = db.Enstitulers.First(p => p.EnstituKod == bsurec.EnstituKod);

                var eslesenDanisman = db.Kullanicilars.FirstOrDefault(p => p.KullaniciID == (basvuru.TezDanismanID ?? 0));
                if (eslesenDanisman != null)
                {
                    model.TezDanismanBilgiEslesen = eslesenDanisman.Unvanlar.UnvanAdi + " " + eslesenDanisman.Ad + " " + eslesenDanisman.Soyad;
                }
                else
                {
                    model.TezDanismanBilgiEslesen = "Sistemde eşleşen tez danışmanı bulunamadı.";
                }
                model.MezuniyetSinavDurumID = basvuru.MezuniyetSinavDurumID;
                model.RowID = basvuru.RowID;
                model.MezuniyetBasvurulariTezDosyalaris = basvuru.MezuniyetBasvurulariTezDosyalaris;
                var onayYapanIDs = model.MezuniyetBasvurulariTezDosyalaris.Where(p => p.OnayYapanID.HasValue).Select(s => s.OnayYapanID).ToList();
                var kuls = db.Kullanicilars.Where(p => onayYapanIDs.Contains(p.KullaniciID)).ToList();
                foreach (var item in model.MezuniyetBasvurulariTezDosyalaris.Where(p => p.OnayYapanID.HasValue))
                {
                    var kul = kuls.First(p => p.KullaniciID == item.IslemYapanID);
                    item.IslemYapanIP = kul.Ad + " " + kul.Soyad;

                }
                model.TezDanismanID = basvuru.TezDanismanID;
                model.IsAnketVar = bsurec.AnketID.HasValue;
                model.IsAnketDolduruldu = basvuru.AnketCevaplaris.Any();
                model.MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID;
                model.MezuniyetSurecID = basvuru.MezuniyetSurecID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.MezuniyetYayinKontrolDurumID = basvuru.MezuniyetYayinKontrolDurumID;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;
                model.KullaniciTipID = basvuru.KullaniciTipID;
                model.ResimAdi = basvuru.ResimAdi;
                model.Ad = basvuru.Ad;
                model.Soyad = basvuru.Soyad;
                model.TcKimlikNo = basvuru.TcKimlikNo;
                model.PasaportNo = basvuru.PasaportNo;
                model.UyrukKod = basvuru.UyrukKod;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = db.OgrenimTipleris.First(p => p.EnstituKod == bsurec.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).OgrenimTipAdi;
                var progLng = basvuru.Programlar;
                model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = progLng.ProgramAdi;
                model.OgrenimDurumID = basvuru.OgrenimDurumID;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
                model.ProgramKod = basvuru.ProgramKod;
                model.KayitOgretimYiliBaslangic = basvuru.KayitOgretimYiliBaslangic;
                model.KayitOgretimYiliDonemID = basvuru.KayitOgretimYiliDonemID;
                model.KayitTarihi = basvuru.KayitTarihi;
                model.IsTezDiliTr = basvuru.IsTezDiliTr;
                model.TezBaslikTr = basvuru.TezBaslikTr;
                model.TezBaslikEn = basvuru.TezBaslikEn;
                model.IsDanismanOnay = basvuru.IsDanismanOnay;
                model.DanismanOnayTarihi = basvuru.DanismanOnayTarihi;
                model.DanismanOnayAciklama = basvuru.DanismanOnayAciklama;
                model.TezDanismanUnvani = basvuru.TezDanismanUnvani;
                model.TezDanismanAdi = basvuru.TezDanismanUnvani + " " + basvuru.TezDanismanAdi;
                model.TezEsDanismanUnvani = basvuru.TezEsDanismanUnvani;
                if (!basvuru.TezEsDanismanUnvani.IsNullOrWhiteSpace())
                    model.TezEsDanismanAdi = basvuru.TezEsDanismanUnvani + " " + basvuru.TezEsDanismanAdi;
                model.TezEsDanismanEMail = basvuru.TezEsDanismanEMail;
                model.TezOzet = basvuru.TezOzet;
                model.OzetAnahtarKelimeler = basvuru.OzetAnahtarKelimeler;
                model.TezOzetHtml = basvuru.TezOzetHtml;
                model.TezAbstract = basvuru.TezAbstract;
                model.TezAbstractHtml = basvuru.TezAbstractHtml;
                model.AbstractAnahtarKelimeler = basvuru.AbstractAnahtarKelimeler;
                model.DanismanImzaliFormDosyaYolu = basvuru.DanismanImzaliFormDosyaYolu;
                model.DanismanImzaliFormDosyaAdi = basvuru.DanismanImzaliFormDosyaAdi;
                model.IsYerli = basvuru.KullaniciTipleri.Yerli;
                model.EnstituAdi = enstitu.EnstituAd;
                model.MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID;
                model.MezuniyetSureci = basvuru.MezuniyetSureci;
                model.MezuniyetSurecID = basvuru.MezuniyetSurecID;
                model.KullaniciID = basvuru.KullaniciID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.MezuniyetYayinKontrolDurumID = basvuru.MezuniyetYayinKontrolDurumID;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;

                model.IslemTarihi = basvuru.IslemTarihi;
                model.IslemYapanID = basvuru.IslemYapanID;
                model.IslemYapanIP = basvuru.IslemYapanIP;
                var nowDate = DateTime.Now;
                model.BasvuruSureciTarihi = bsurec.BaslangicYil + "/" + bsurec.BitisYil + " " + db.Donemlers.First(p => p.DonemID == bsurec.DonemID).DonemAdi + " (" + bsurec.BaslangicTarihi.ToDateString() + "-" + bsurec.BitisTarihi.ToDateString() + ")";
                model.sonucGirisSureciAktif = bsurec.BaslangicTarihi <= nowDate && bsurec.BitisTarihi >= nowDate;
                model.IsMezunOldu = basvuru.IsMezunOldu;
                model.MezuniyetTarihi = basvuru.MezuniyetTarihi;
                model.EYKTarihi = basvuru.EYKTarihi;
                model.TezTeslimSonTarih = basvuru.TezTeslimSonTarih;
                model.MezuniyetJuriOneriFormlaris = db.MezuniyetJuriOneriFormlaris.Include("MezuniyetJuriOneriFormuJurileris").Where(p => p.MezuniyetBasvurulariID == basvuru.MezuniyetBasvurulariID).ToList();


                foreach (var item in model.MezuniyetJuriOneriFormlaris.SelectMany(s => s.MezuniyetJuriOneriFormuJurileris).Where(p => p.UniversiteID.HasValue))
                {

                    item.UniversiteAdi = item.Universiteler.Ad;
                }


                model.EYKYaGonderildi = model.MezuniyetJuriOneriFormlaris.Select(s => s.EYKYaGonderildi).FirstOrDefault();
                model.EYKDaOnaylandi = model.MezuniyetJuriOneriFormlaris.Select(s => s.EYKDaOnaylandi).FirstOrDefault();
                var yayins = (from qs in db.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId)
                              join s in db.MezuniyetSureciYayinTurleris on new { qs.MezuniyetBasvurulari.MezuniyetSurecID, qs.MezuniyetYayinTurID } equals new { s.MezuniyetSurecID, s.MezuniyetYayinTurID }
                              join sd in db.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                              join yb in db.MezuniyetYayinBelgeTurleris on new { s.MezuniyetYayinBelgeTurID } equals new { MezuniyetYayinBelgeTurID = (int?)yb.MezuniyetYayinBelgeTurID } into defyb
                              from ybD in defyb.DefaultIfEmpty()
                              join klk in db.MezuniyetYayinLinkTurleris on new { s.KaynakMezuniyetYayinLinkTurID } equals new { KaynakMezuniyetYayinLinkTurID = (int?)klk.MezuniyetYayinLinkTurID } into defklk
                              from klkD in defklk.DefaultIfEmpty()
                              join ym in db.MezuniyetYayinMetinTurleris on new { s.MezuniyetYayinMetinTurID } equals new { MezuniyetYayinMetinTurID = (int?)ym.MezuniyetYayinMetinTurID } into defym
                              from ymD in defym.DefaultIfEmpty()
                              join kl in db.MezuniyetYayinLinkTurleris on new { s.YayinMezuniyetYayinLinkTurID } equals new { YayinMezuniyetYayinLinkTurID = (int?)kl.MezuniyetYayinLinkTurID } into defkl
                              from klD in defkl.DefaultIfEmpty()
                              join inx in db.MezuniyetYayinIndexTurleris on new { qs.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into definx
                              from inxD in definx.DefaultIfEmpty()
                              select new MezuniyetBasvurulariYayinDto
                              {
                                  MezuniyetYayinTurID = qs.MezuniyetYayinTurID,
                                  ShowDetayYayinID = showDetayYayinId,
                                  MezuniyetBasvurulariYayinID = qs.MezuniyetBasvurulariYayinID,
                                  MezuniyetBasvurulariID = qs.MezuniyetBasvurulariID,
                                  DanismanIsmiVar = qs.DanismanIsmiVar,
                                  TezIcerikUyumuVar = qs.TezIcerikUyumuVar,
                                  Onaylandi = qs.Onaylandi,
                                  RetAciklamasi = qs.RetAciklamasi,
                                  YayinBasligi = qs.YayinBasligi,
                                  Yayinlanmis = qs.Yayinlanmis,
                                  MezuniyetYayinTarih = qs.MezuniyetYayinTarih,
                                  MezuniyetYayinTarihZorunlu = s.TarihIstensin,
                                  MezuniyetYayinTurAdi = sd.MezuniyetYayinTurAdi,
                                  MezuniyetYayinBelgeTurID = s.MezuniyetYayinBelgeTurID,
                                  MezuniyetYayinBelgeTurAdi = ybD != null ? ybD.BelgeTurAdi : "",
                                  MezuniyetYayinBelgeAdi = ybD != null ? qs.MezuniyetYayinBelgeAdi : "",
                                  MezuniyetYayinBelgeDosyaYolu = ybD != null ? qs.MezuniyetYayinBelgeDosyaYolu : "",
                                  MezuniyetYayinBelgeTurZorunlu = s.BelgeZorunlu,
                                  MezuniyetYayinKaynakLinkTurID = s.KaynakMezuniyetYayinLinkTurID,
                                  MezuniyetYayinKaynakLinkTurAdi = klkD != null ? klkD.LinkTurAdi : "",
                                  MezuniyetYayinKaynakLinkIsUrl = klkD != null && klkD.IsUrl,
                                  MezuniyetYayinKaynakLinkTurZorunlu = s.KaynakLinkiZorunlu,
                                  MezuniyetYayinMetinTurID = s.MezuniyetYayinMetinTurID,
                                  MezuniyetYayinMetinTurAdi = ymD != null ? ymD.MetinTurAdi : "",
                                  MezuniyetYayinMetniBelgeAdi = ymD != null ? qs.MezuniyetYayinMetniBelgeAdi : "",
                                  MezuniyetYayinMetniBelgeYolu = qs.MezuniyetYayinMetniBelgeYolu,
                                  MezuniyetYayinMetinZorunlu = s.MetinZorunlu,
                                  MezuniyetYayinLinkTurID = s.YayinMezuniyetYayinLinkTurID,
                                  MezuniyetYayinLinkTurAdi = klD != null ? klD.LinkTurAdi : "",
                                  MezuniyetYayinLinkIsUrl = klD != null && klD.IsUrl,
                                  MezuniyetYayinLinkiZorunlu = s.YayinLinkiZorunlu,
                                  MezuniyetYayinKaynakLinki = qs.MezuniyetYayinKaynakLinki,
                                  MezuniyetYayinLinki = qs.MezuniyetYayinLinki,
                                  MezuniyetYayinIndexTurZorunlu = s.YayinIndexTurIstensin,
                                  MezuniyetYayinIndexTurAdi = inxD != null ? inxD.IndexTurAdi : "",
                                  MezuniyetYayinIndexTurID = qs.MezuniyetYayinIndexTurID,
                                  YayinIndexTurleri = db.MezuniyetYayinIndexTurleris.ToList(),
                                  MezuniyetKabulEdilmisMakaleZorunlu = s.YayinKabulEdilmisMakaleIstensin,
                                  MezuniyetYayinKabulEdilmisMakaleAdi = qs.MezuniyetYayinKabulEdilmisMakaleAdi,
                                  MezuniyetYayinKabulEdilmisMakaleDosyaYolu = qs.MezuniyetYayinKabulEdilmisMakaleDosyaYolu,
                                  YayinDeatKurulusIstensin = s.YayinDeatKurulusIstensin,
                                  ProjeDeatKurulus = qs.ProjeDeatKurulus,
                                  YayinDergiAdiIstensin = s.YayinDergiAdiIstensin,
                                  DergiAdi = qs.DergiAdi,
                                  YayinMevcutDurumIstensin = s.YayinMevcutDurumIstensin,
                                  IsProjeTamamlandiOrDevamEdiyor = qs.IsProjeTamamlandiOrDevamEdiyor,
                                  YayinProjeEkibiIstensin = s.YayinProjeEkibiIstensin,
                                  ProjeEkibi = qs.ProjeEkibi,
                                  YayinProjeTurIstensin = s.YayinProjeTurIstensin,
                                  MezuniyetYayinProjeTurID = qs.MezuniyetYayinProjeTurID,
                                  ProjeTurAdi = qs.MezuniyetYayinProjeTurID.HasValue ? qs.MezuniyetYayinProjeTurleri.ProjeTurAdi : "",
                                  YayinYazarlarIstensin = s.YayinYazarlarIstensin,
                                  YazarAdi = qs.YazarAdi,
                                  YayinYilCiltSayiIstensin = s.YayinYilCiltSayiIstensin,
                                  YilCiltSayiSS = qs.YilCiltSayiSS,
                                  IsTarihAraligiIstensin = s.IsTarihAraligiIstensin,
                                  TarihAraligi = qs.TarihAraligi,
                                  YayinYerBilgisiIstensin = s.YayinYerBilgisiIstensin,
                                  YerBilgisi = qs.YerBilgisi,
                                  YayinEtkinlikAdiIstensin = s.YayinEtkinlikAdiIstensin,
                                  EtkinlikAdi = qs.EtkinlikAdi


                              });

                if (mezuniyetBasvurulariYayinId.HasValue)
                {
                    model.SelectedYayin = yayins.First(p => p.MezuniyetBasvurulariYayinID == mezuniyetBasvurulariYayinId);
                }
                else
                {
                    model.YayinBilgileri = yayins.ToList();
                }

                #region SalonRezervasyonlari 
                model.MezuniyetSRModel.SalonRezervasyonlari = (from s in db.SRTalepleris
                                                               join tt in db.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID
                                                               join mb in db.MezuniyetBasvurularis on s.MezuniyetBasvurulariID equals mb.MezuniyetBasvurulariID
                                                               join sal in db.SRSalonlars on s.SRSalonID equals sal.SRSalonID into def1
                                                               from defSl in def1.DefaultIfEmpty()
                                                               join hg in db.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                                                               join d in db.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                                                               join sd in db.MezuniyetSinavDurumlaris on (s.MezuniyetSinavDurumID ?? MezuniyetSinavDurum.SonucGirilmedi) equals sd.MezuniyetSinavDurumID into def2
                                                               from defSd in def2.DefaultIfEmpty()
                                                               join sdj in db.MezuniyetSinavDurumlaris on (s.JuriSonucMezuniyetSinavDurumID ?? MezuniyetSinavDurum.SonucGirilmedi) equals sdj.MezuniyetSinavDurumID into def3
                                                               from defsdj in def3.DefaultIfEmpty()
                                                               let jof = mb.MezuniyetJuriOneriFormlaris.FirstOrDefault()
                                                               where s.MezuniyetBasvurulariID == basvuru.MezuniyetBasvurulariID
                                                               select new FrTalepler
                                                               {
                                                                   SRTalepID = s.SRTalepID,
                                                                   UniqueID = s.UniqueID,
                                                                   TalepYapanID = s.TalepYapanID,
                                                                   TalepTipAdi = tt.TalepTipAdi,
                                                                   SRTalepTipID = s.SRTalepTipID,
                                                                   SRSalonID = s.SRSalonID,
                                                                   SalonAdi = s.SRSalonID.HasValue ? defSl.SalonAdi : s.SalonAdi,
                                                                   DegerlendirmeSonucMailTarihi = s.DegerlendirmeSonucMailTarihi,

                                                                   Tarih = s.Tarih,
                                                                   HaftaGunID = s.HaftaGunID,
                                                                   HaftaGunAdi = hg.HaftaGunAdi,
                                                                   BasSaat = s.BasSaat,
                                                                   BitSaat = s.BitSaat,
                                                                   MezuniyetSinavDurumID = s.MezuniyetSinavDurumID,
                                                                   MezuniyetSinavDurumIslemTarihi = s.MezuniyetSinavDurumIslemTarihi,
                                                                   MezuniyetSinavDurumIslemYapanID = s.MezuniyetSinavDurumIslemYapanID,
                                                                   SDurumAdi = defSd != null ? defSd.MezuniyetSinavDurumAdi : "",
                                                                   SDurumListeAdi = defSd != null ? defSd.MezuniyetSinavDurumAdi : "",
                                                                   SClassName = defSd != null ? defSd.ClassName : "",
                                                                   SColor = defSd != null ? defSd.Color : "",
                                                                   SRDurumID = s.SRDurumID,
                                                                   DurumAdi = d.DurumAdi,
                                                                   DurumListeAdi = d.DurumAdi,
                                                                   ClassName = d.ClassName,
                                                                   Color = d.Color,
                                                                   SRDurumAciklamasi = s.SRDurumAciklamasi,
                                                                   JuriSonucMezuniyetSinavDurumID = s.JuriSonucMezuniyetSinavDurumID,
                                                                   IsOyBirligiOrCouklugu = s.IsOyBirligiOrCouklugu,
                                                                   RSBaslatildiMailGonderimTarihi = s.RSBaslatildiMailGonderimTarihi,
                                                                   JuriSonucMezuniyetSinavDurumAdi = defsdj.MezuniyetSinavDurumAdi,
                                                                   IslemTarihi = s.IslemTarihi,
                                                                   IslemYapanID = s.IslemYapanID,
                                                                   IslemYapanIP = s.IslemYapanIP,
                                                                   IsTezBasligiDegisti = s.IsTezBasligiDegisti,
                                                                   IsTezDiliTr = mb.IsTezDiliTr == true,
                                                                   TezBaslikTr = jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikTr : mb.TezBaslikTr,
                                                                   TezBaslikEn = jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikEn : mb.TezBaslikEn,
                                                                   YeniTezBaslikTr = s.YeniTezBaslikTr,
                                                                   YeniTezBaslikEn = s.YeniTezBaslikEn,
                                                                   SRTaleplerJuris = s.SRTaleplerJuris.ToList(),
                                                                   IsSonSRTalebi = !mb.SRTalepleris.Any(a => a.SRTalepID > s.SRTalepID),
                                                                   // IslemYetkisiVar = !mb.SRTalepleris.Any(a => a.SRTalepID > s.SRTalepID) && (!mb.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true)),
                                                                   SRTalepleriBezCiltFormus = s.SRTalepleriBezCiltFormus,
                                                                   UzatmaSonSRTarih = s.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma ? (EntityFunctions.AddDays(s.Tarih, bSurecOtKriter.MBSinavUzatmaSuresiGun).Value) : DateTime.Now,
                                                                   TeslimSonTarih = model.TezTeslimSonTarih ?? EntityFunctions.AddDays(s.Tarih, bSurecOtKriter.MBTezTeslimSuresiGun).Value,
                                                                   IsOgrenciUzatmaSonrasiOnay = s.IsOgrenciUzatmaSonrasiOnay,
                                                                   OgrenciOnayTarihi = s.OgrenciOnayTarihi,
                                                                   IsDanismanUzatmaSonrasiOnay = s.IsDanismanUzatmaSonrasiOnay,
                                                                   DanismanOnayTarihi = s.DanismanOnayTarihi,
                                                                   DanismanUzatmaSonrasiOnayAciklama = s.DanismanUzatmaSonrasiOnayAciklama,
                                                                   IsYokDrBursiyeriVar = s.IsYokDrBursiyeriVar,
                                                                   YokDrOncelikliAlan = s.YokDrOncelikliAlan

                                                               }).OrderByDescending(o => o.SRTalepID).ToList();
                foreach (var item in model.MezuniyetSRModel.SalonRezervasyonlari)
                {

                    item.SrDurumSelectList.SRDurumID = new SelectList(SrTalepleriBus.GetCmbSrDurumListe(false), "Value", "Caption", item.SRDurumID);
                    item.SrDurumSelectList.MezuniyetSinavDurumID = new SelectList(MezuniyetBus.GetCmbMzSinavDurumListe(), "Value", "Caption", item.MezuniyetSinavDurumID);

                }
                model.MezuniyetDurumSelectList.IsMezunOldu = new SelectList(MezuniyetBus.GetCmbMezuniyetDurum(), "Value", "Caption", model.IsMezunOldu);
                model.MezuniyetSRModel.EykIlkSrMaxTarih = model.EYKTarihi.HasValue ? (model.TezTeslimSonTarih ?? model.EYKTarihi.Value.AddDays(bSurecOtKriter.MBTezTeslimSuresiGun)) : (DateTime?)null;
                model.MezuniyetSRModel.IsSrEykSureAsimi = model.EYKTarihi.HasValue && model.MezuniyetSRModel.EykIlkSrMaxTarih < DateTime.Now;


                #endregion

                if (model.IsAnketDolduruldu == false)
                {
                    if (bsurec.AnketID.HasValue)
                    {

                        if (!db.AnketCevaplaris.Any(a => a.MezuniyetBasvurulariID == mezuniyetBasvurulariId))
                        {

                            var anketSorulari = (from bsa in db.Ankets.Where(p => p.AnketID == bsurec.AnketID)
                                                 join aso in db.AnketSorus on bsa.AnketID equals aso.AnketID
                                                 join sb in db.AnketCevaplaris.Where(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId && p.Basvurular.KullaniciID == basvuru.KullaniciID) on aso.AnketSoruID equals sb.AnketSoruID into def1
                                                 from sbc in def1.DefaultIfEmpty()
                                                 select new
                                                 {
                                                     aso.AnketSoruID,
                                                     AnketSoruSecenekID = sbc != null ? sbc.AnketSoruSecenekID : (int?)null,
                                                     aso.IsTabloVeriGirisi,
                                                     aso.IsTabloVeriMaxSatir,
                                                     Aciklama = sbc != null ? sbc.EkAciklama : "",
                                                     aso.SiraNo,
                                                     aso.SoruAdi,
                                                     Secenekler = aso.AnketSoruSeceneks.Select(ss => new
                                                     {
                                                         ss.AnketSoruSecenekID,
                                                         ss.AnketSoruID,
                                                         ss.SiraNo,
                                                         ss.IsYaziOrSayi,
                                                         ss.IsEkAciklamaGir,
                                                         ss.SecenekAdi
                                                     }).OrderBy(o => o.SiraNo).ToList()


                                                 }).OrderBy(o => o.SiraNo).ToList();
                            var modelAnk = new KmAnketlerCevap
                            {
                                RowID = basvuru.RowID.ToString(),
                                AnketTipID = 4,
                                BasvuruSurecID = bsurec.MezuniyetSurecID,
                                AnketID = bsurec.AnketID.Value,
                                JsonStringData = anketSorulari.ToJsonText()
                            };
                            foreach (var item in anketSorulari)
                            {
                                modelAnk.AnketCevapModel.Add(new AnketCevapDto
                                {
                                    SecilenAnketSoruSecenekID = item.AnketSoruSecenekID,
                                    SoruBilgi = new FrAnketDetayDto { AnketSoruID = item.AnketSoruID, SoruAdi = item.SoruAdi, SiraNo = item.SiraNo, Aciklama = item.Aciklama, IsTabloVeriGirisi = item.IsTabloVeriGirisi, IsTabloVeriMaxSatir = item.IsTabloVeriMaxSatir },
                                    SoruSecenek = item.Secenekler.Select(s => new FrAnketSecenekDetayDto { AnketSoruSecenekID = s.AnketSoruSecenekID, SiraNo = s.SiraNo, IsEkAciklamaGir = s.IsEkAciklamaGir, SecenekAdi = s.SecenekAdi }).ToList(),
                                    SelectListSoruSecenek = new SelectList(item.Secenekler.ToList(), "AnketSoruSecenekID", "SecenekAdi", item.AnketSoruSecenekID)
                                });
                            }

                            model.AnketView = ViewRenderHelper.RenderPartialView("Ajax", "getAnket", modelAnk);
                        }
                    }
                }

                var bdurum = basvuru.MezuniyetYayinKontrolDurumlari;
                model.MezuniyetYayinKontrolDurumAdi = bdurum.MezuniyetYayinKontrolDurumAdi;
                model.DurumClassName = bdurum.ClassName;
                model.DurumColor = bdurum.Color;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;
            }
            return model;

        }
        public static MmMessage TezKontrol(kmMezuniyetBasvuru kModel)
        {
            var mmMessage = new MmMessage();
            if (!kModel.IsTezDiliTr.HasValue)
            {
                mmMessage.Messages.Add("Tez dilini seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsTezDiliTr" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "IsTezDiliTr" });
            if (kModel.TezBaslikTr.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Başlığını Türkçe Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezBaslikTr" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezBaslikTr" });
            if (kModel.TezBaslikEn.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Başlığını İngilizce Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezBaslikEn" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezBaslikEn" });

            if (kModel.TezDanismanUnvani.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Danışman Unvanı Seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezDanismanUnvani" });
            }
            if (kModel.TezDanismanAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Danışman Adı Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezDanismanAdi" });
            }
            if (!kModel.TezDanismanAdi.IsNullOrWhiteSpace() && !kModel.TezDanismanUnvani.IsNullOrWhiteSpace())
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezDanismanAdi" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezDanismanUnvani" });
            }


            if (!kModel.TezEsDanismanAdi.IsNullOrWhiteSpace() || !kModel.TezEsDanismanUnvani.IsNullOrWhiteSpace() || !kModel.TezEsDanismanEMail.IsNullOrWhiteSpace())
            {
                if (kModel.TezEsDanismanUnvani.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Tez Eş Danışman Unvanı Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezEsDanismanUnvani" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezEsDanismanUnvani" });

                if (kModel.TezEsDanismanAdi.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Tez Eş Danışman Adı Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezEsDanismanAdi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezEsDanismanAdi" });
                if (kModel.TezEsDanismanEMail.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Eş Danışman E-Posta Bilgisini Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezEsDanismanEMail" });

                }
                else if (kModel.TezEsDanismanEMail.ToIsValidEmail())
                {
                    mmMessage.Messages.Add("Lütfen E-Posta Adres Tekrarını Uygun Formatta Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezEsDanismanEMail" });

                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezEsDanismanEMail" });
            }
            else
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "TezEsDanismanAdi" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "TezEsDanismanUnvani" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "TezEsDanismanEMail" });
            }
            if (kModel.TezOzet.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Özetini Türkçe Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezOzet" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezOzet" });
            if (kModel.OzetAnahtarKelimeler.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Özeti Anahtar Kelimelerini Türkçe Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OzetAnahtarKelimeler" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OzetAnahtarKelimeler" });
            if (kModel.TezAbstract.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Özetini İngilizce Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezAbstract" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezAbstract" });
            if (kModel.AbstractAnahtarKelimeler.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Tez Özeti Anahtar Kelimelerini İngilizce Olarak Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AbstractAnahtarKelimeler" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AbstractAnahtarKelimeler" });

            return mmMessage;
        }
        public static bool GetMezuniyetSureciOgrenimTipUygunMu(int mezuniyetSurecId, int kullaniciId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kul = db.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
                return db.MezuniyetSureciYonetmelikleris.Any(p => p.MezuniyetSurecID == mezuniyetSurecId && p.MezuniyetSureciYonetmelikleriOTs.Any(a => a.OgrenimTipKod == kul.OgrenimTipKod && a.IsGecerli));
            }
        }
        public static MezuniyetSureciYonetmelikleri GetMezuniyetAktifYonetmelik(int mezuniyetSurecId, int kullaniciId, int? mezuniyetBasvurulariId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                decimal baslangic = 0;
                if (mezuniyetBasvurulariId > 0)
                {
                    var mBasvuru = db.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                    baslangic = Convert.ToDecimal(mBasvuru.KayitOgretimYiliBaslangic + "," + mBasvuru.KayitOgretimYiliDonemID.Value);
                }
                else
                {
                    var kul = db.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
                    baslangic = Convert.ToDecimal(kul.KayitYilBaslangic + "," + kul.KayitDonemID.Value);
                }
                var kriter = db.MezuniyetSureciYonetmelikleris.Include("MezuniyetSureciYonetmelikleriOTs").Where(p => p.MezuniyetSurecID == mezuniyetSurecId).ToList().First(f =>
                    f.TarihKriterID == TarihKriterSecim.SecilenTarihVeOncesi ?
                        (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) >= baslangic)
                        :
                        (f.TarihKriterID == TarihKriterSecim.SecilenTarihVeSonrasi ?
                            (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) <= baslangic)
                            :
                            (
                                (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) <= baslangic && Convert.ToDecimal(f.BaslangicYilB + "," + f.DonemIDB) >= baslangic)
                            )
                        )

                );
                return kriter;
            }
        }
        public static List<MezuniyetSureciYonetmelikleriOT> GetMezuniyetAktifOgrenimTipiYayinBilgileri(int mezuniyetSurecId, int kullaniciId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kul = db.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
                var baslangic = Convert.ToDecimal(kul.KayitYilBaslangic + "," + kul.KayitDonemID.Value);
                var kriter = db.MezuniyetSureciYonetmelikleris.Where(p => p.MezuniyetSurecID == mezuniyetSurecId).ToList().First(f =>
                    f.TarihKriterID == TarihKriterSecim.SecilenTarihVeOncesi ?
                        (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) >= baslangic)
                        :
                        (f.TarihKriterID == TarihKriterSecim.SecilenTarihVeSonrasi ?
                            (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) <= baslangic)
                            :
                            (
                                (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) <= baslangic && Convert.ToDecimal(f.BaslangicYilB + "," + f.DonemIDB) >= baslangic)
                            )
                        )

                );
                var ots = kriter.MezuniyetSureciYonetmelikleriOTs.Where(p => p.OgrenimTipKod == kul.OgrenimTipKod).ToList();
                return ots;
            }
        }
        public static MmMessage YayinKontrol(kmMezuniyetBasvuru kModel)
        {
            var mmMessage = new MmMessage();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                //var kriterSecim=db.MezuniyetSureciYonetmelikleris.Where(p=>p.BaslangicYil)
                var kriter = GetMezuniyetAktifYonetmelik(kModel.MezuniyetSurecID, kModel.KullaniciID, kModel.MezuniyetBasvurulariID);
                var yturAds = db.MezuniyetYayinTurleris.ToList();
                var kul = db.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);
                var kriterDetay = (from s in kriter.MezuniyetSureciYonetmelikleriOTs.Where(p => p.OgrenimTipKod == kul.OgrenimTipKod).ToList()
                                   join yta in yturAds on s.MezuniyetYayinTurID equals yta.MezuniyetYayinTurID
                                   group new { s.MezuniyetYayinTurID, yta.MezuniyetYayinTurAdi, s.OgrenimTipKod, s.IsGecerli, s.IsZorunlu, s.GrupKodu, s.IsVeOrVeya } by new { IsZorunlu = s.IsZorunlu, IsGrup = s.GrupKodu.IsNullOrWhiteSpace() == false, s.GrupKodu } into g1
                                   select new
                                   {
                                       g1.Key.IsGrup,
                                       g1.Key.GrupKodu,
                                       g1.Key.IsZorunlu,
                                       data = g1.ToList()
                                   }).ToList();


                var qYbaslik = kModel._YayinBasligi.Select((s, inx) => new { YayinBasligi = s, Index = inx }).ToList();
                var qYtarih = kModel._MezuniyetYayinTarih.Select((s, inx) => new { MezuniyetYayinTarih = s, Index = inx }).ToList();
                var qMytID = kModel._MezuniyetYayinTurID.Select((s, inx) => new { MezuniyetYayinTurID = s, Index = inx }).ToList();
                var qMybelge = kModel._MezuniyetYayinBelgesiAdi.Select((s, inx) => new { MezuniyetYayinBelgesiAdi = s, Index = inx }).ToList();
                var qMkLink = kModel._MezuniyetYayinKaynakLinki.Select((s, inx) => new { MezuniyetYayinKaynakLinki = s, Index = inx }).ToList();
                var qMbelge = kModel._YayinMetniBelgesiAdi.Select((s, inx) => new { YayinMetniBelgesiAdi = s, Index = inx }).ToList();
                var qMyLink = kModel._MezuniyetYayinLinki.Select((s, inx) => new { MezuniyetYayinLinki = s, Index = inx }).ToList();
                var qIndex = kModel._MezuniyetYayinIndexTurID.Select((s, inx) => new { MezuniyetYayinIndexTurID = s, Index = inx }).ToList();

                var qYayins = (from b in qYbaslik
                               join myt in qMytID on b.Index equals myt.Index
                               join yt in qYtarih on b.Index equals yt.Index
                               join yta in yturAds on myt.MezuniyetYayinTurID equals yta.MezuniyetYayinTurID
                               join myb in qMybelge on b.Index equals myb.Index
                               join mkl in qMkLink on b.Index equals mkl.Index
                               join mb in qMbelge on b.Index equals mb.Index
                               join myl in qMyLink on b.Index equals myl.Index
                               join mI in qIndex on b.Index equals mI.Index
                               select new
                               {
                                   b.Index,
                                   myt.MezuniyetYayinTurID,
                                   yta.MezuniyetYayinTurAdi,
                                   yt.MezuniyetYayinTarih,
                                   myb.MezuniyetYayinBelgesiAdi,
                                   mkl.MezuniyetYayinKaynakLinki,
                                   mb.YayinMetniBelgesiAdi,
                                   myl.MezuniyetYayinLinki,
                                   mI.MezuniyetYayinIndexTurID
                               }).ToList();
                if (kriterDetay.Any(p => p.IsZorunlu) == false && qYbaslik.Count > 0)
                {
                    mmMessage.Messages.Add("Mezuniyet başvurunuz için herhangi bir yayın bilgisi istenmemektedir. Yayın bilgisi ekleyemezsiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTurID" });
                }
                else
                {
                    foreach (var item in kriterDetay)
                    {
                        if (item.IsZorunlu)
                        {
                            if (item.IsGrup)
                            {
                                var contains = item.data.Select(s => new { s.MezuniyetYayinTurID, s.MezuniyetYayinTurAdi }).ToList();

                                if (!qYayins.Any(a => contains.Any(a2 => a2.MezuniyetYayinTurID == a.MezuniyetYayinTurID)) && kModel.YayinBilgisi == null)
                                {
                                    mmMessage.Messages.Add(string.Join(", ", contains.Select(s => s.MezuniyetYayinTurAdi)) + " Yayın Türlerinden birinin eklenmesi gerekmektedir.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTurID" });
                                }
                                else if (qYayins.Count(a => contains.Any(a2 => a2.MezuniyetYayinTurID == a.MezuniyetYayinTurID)) > 1)
                                {
                                    mmMessage.Messages.Add(string.Join(", ", contains.Select(s => s.MezuniyetYayinTurAdi)) + " Yayın Türlerinden sadece biri eklenebilir.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTurID" });
                                }
                            }
                            else
                            {
                                foreach (var item2 in item.data)
                                {
                                    if (qYayins.All(a => item2.MezuniyetYayinTurID != a.MezuniyetYayinTurID) && kModel.YayinBilgisi == null)
                                    {

                                        mmMessage.Messages.Add(item2.MezuniyetYayinTurAdi + " Yayın türünün eklenmesi gerekmektedir.");
                                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTurID" });
                                    }

                                    else if (qYayins.Count(a => item2.MezuniyetYayinTurID == a.MezuniyetYayinTurID) > 1)
                                    {
                                        mmMessage.Messages.Add(item2.MezuniyetYayinTurAdi + " Yayın Türünden sadece 1 adet eklenebilir.");
                                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTurID" });
                                    }
                                }

                            }
                        }
                        else
                        {
                            foreach (var itemY in qYayins)
                            {
                                if (item.data.Any(a => a.MezuniyetYayinTurID == itemY.MezuniyetYayinTurID && a.IsZorunlu == false))
                                {
                                    mmMessage.Messages.Add(itemY.MezuniyetYayinTurAdi + " Yayın kabul edilmemektedir. Bu Yayın bilgisini eklenemez.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTurID" });
                                }
                            }
                        }
                    }
                }

            }
            return mmMessage;
        }
        public static kmMezuniyetSureciOgrenimTipModel GetMezuniyetOgrenimTipKriterleri(string enstituKod, int mezuniyetSurecId)
        {
            var model = new kmMezuniyetSureciOgrenimTipModel();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                model.OgrenimTipKriterList = (from o in db.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod && p.IsMezuniyetBasvurusuYapabilir && p.IsAktif)
                                              join s in db.MezuniyetSureciOgrenimTipKriterleris on new
                                              {
                                                  o.OgrenimTipKod,
                                                  MezuniyetSurecID = mezuniyetSurecId
                                              }
                                                 equals new
                                                 {
                                                     s.OgrenimTipKod,
                                                     s.MezuniyetSurecID
                                                 }
                                                 into def1
                                              from defS in def1.DefaultIfEmpty()
                                              join ot in db.OgrenimTipleris on o.OgrenimTipID equals ot.OgrenimTipID
                                              where o.IsAktif
                                              select new kmMezuniyetSureciOgrenimTipKriterleri
                                              {

                                                  MezuniyetSureciOgrenimTipKriterID = defS != null ? defS.MezuniyetSureciOgrenimTipKriterID : 0,
                                                  OrjinalVeri = defS == null,
                                                  OgrenimTipKod = o.OgrenimTipKod,
                                                  OgrenimTipID = o.OgrenimTipID,
                                                  MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = defS != null ? defS.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari : o.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari,
                                                  MBasvuruToplamKrediKriteri = defS != null ? defS.MBasvuruToplamKrediKriteri : o.MBasvuruToplamKrediKriteri.Value,
                                                  MBasvuruAGNOKriteri = defS != null ? defS.MBasvuruAGNOKriteri : o.MBasvuruAGNOKriteri.Value,
                                                  MBasvuruAKTSKriteri = defS != null ? defS.MBasvuruAKTSKriteri : o.MBasvuruAKTSKriteri.Value,
                                                  MBSinavUzatmaSuresiGun = defS != null ? defS.MBSinavUzatmaSuresiGun : o.MBSinavUzatmaSuresiGun.Value,
                                                  MBTezTeslimSuresiGun = defS != null ? defS.MBTezTeslimSuresiGun : o.MBTezTeslimSuresiGun.Value,
                                                  MBSRTalebiKacGunSonraAlabilir = defS != null ? defS.MBSRTalebiKacGunSonraAlabilir : o.MBSRTalebiKacGunSonraAlabilir.Value,
                                                  OgrenimTipAdi = ot.OgrenimTipAdi
                                              }).ToList();
            }
            return model;
        }
        public static MezuniyetBasvurulariYayinDto GetYayinBilgisi(int mezuniyetSurecId, int mezuniyetYayinTurId)
        {
            MezuniyetBasvurulariYayinDto mdl;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                mdl = (from s in db.MezuniyetSureciYayinTurleris.Where(p => p.MezuniyetSurecID == mezuniyetSurecId && p.MezuniyetYayinTurID == mezuniyetYayinTurId)
                       join sd in db.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                       join yb in db.MezuniyetYayinBelgeTurleris on new { s.MezuniyetYayinBelgeTurID } equals new { MezuniyetYayinBelgeTurID = (int?)yb.MezuniyetYayinBelgeTurID } into defyb
                       from ybD in defyb.DefaultIfEmpty()
                       join klk in db.MezuniyetYayinLinkTurleris on new { s.KaynakMezuniyetYayinLinkTurID } equals new { KaynakMezuniyetYayinLinkTurID = (int?)klk.MezuniyetYayinLinkTurID } into defklk
                       from klkD in defklk.DefaultIfEmpty()
                       join ym in db.MezuniyetYayinMetinTurleris on new { s.MezuniyetYayinMetinTurID } equals new { MezuniyetYayinMetinTurID = (int?)ym.MezuniyetYayinMetinTurID } into defym
                       from ymD in defym.DefaultIfEmpty()
                       join kl in db.MezuniyetYayinLinkTurleris on new { s.YayinMezuniyetYayinLinkTurID } equals new { YayinMezuniyetYayinLinkTurID = (int?)kl.MezuniyetYayinLinkTurID } into defkl
                       from klD in defkl.DefaultIfEmpty()
                       join inx in db.MezuniyetYayinIndexTurleris on new { s.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into definx
                       from inxD in definx.DefaultIfEmpty()
                       select new MezuniyetBasvurulariYayinDto
                       {
                           MezuniyetYayinTurID = s.MezuniyetYayinTurID,
                           MezuniyetYayinTurAdi = sd.MezuniyetYayinTurAdi,
                           MezuniyetYayinTarihZorunlu = s.TarihIstensin,
                           MezuniyetYayinBelgeTurID = s.MezuniyetYayinBelgeTurID,
                           MezuniyetYayinBelgeTurAdi = ybD != null ? ybD.BelgeTurAdi : "",
                           MezuniyetYayinBelgeTurZorunlu = s.BelgeZorunlu,
                           MezuniyetYayinKaynakLinkTurID = s.KaynakMezuniyetYayinLinkTurID,
                           MezuniyetYayinKaynakLinkTurAdi = klkD != null ? klkD.LinkTurAdi : "",
                           MezuniyetYayinKaynakLinkIsUrl = klD != null && klD.IsUrl,
                           MezuniyetYayinKaynakLinkTurZorunlu = s.KaynakLinkiZorunlu,
                           MezuniyetYayinMetinTurID = s.MezuniyetYayinMetinTurID,
                           MezuniyetYayinMetinTurAdi = ymD != null ? ymD.MetinTurAdi : "",
                           MezuniyetYayinMetinZorunlu = s.MetinZorunlu,
                           MezuniyetYayinLinkTurID = s.YayinMezuniyetYayinLinkTurID,
                           MezuniyetYayinLinkTurAdi = klD != null ? klD.LinkTurAdi : "",
                           MezuniyetYayinLinkIsUrl = klD != null && klD.IsUrl,
                           MezuniyetYayinLinkiZorunlu = s.YayinLinkiZorunlu,
                           MezuniyetYayinIndexTurZorunlu = s.YayinIndexTurIstensin,
                           MezuniyetKabulEdilmisMakaleZorunlu = s.YayinKabulEdilmisMakaleIstensin,
                           YayinDeatKurulusIstensin = s.YayinDeatKurulusIstensin,
                           YayinDergiAdiIstensin = s.YayinDergiAdiIstensin,
                           YayinMevcutDurumIstensin = s.YayinMevcutDurumIstensin,
                           YayinProjeEkibiIstensin = s.YayinProjeEkibiIstensin,
                           YayinProjeTurIstensin = s.YayinProjeTurIstensin,
                           YayinYazarlarIstensin = s.YayinYazarlarIstensin,
                           YayinYilCiltSayiIstensin = s.YayinYilCiltSayiIstensin,
                           IsTarihAraligiIstensin = s.IsTarihAraligiIstensin,
                           YayinEtkinlikAdiIstensin = s.YayinEtkinlikAdiIstensin,
                           YayinYerBilgisiIstensin = s.YayinYerBilgisiIstensin,
                       }).First();
                mdl.YayinIndexTurleri = db.MezuniyetYayinIndexTurleris.ToList();
                mdl.MezuniyetYayinProjeTurleris = db.MezuniyetYayinProjeTurleris.ToList();
            }
            return mdl;
        }
        public static List<MezuniyetYayinKontrolDurumlari> GetMezuniyetYayinDurumListe(List<int> selectedBDurumId = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.MezuniyetYayinKontrolDurumlaris.Where(p => p.IsAktif);
                if (selectedBDurumId != null) qdata = qdata.Where(p => selectedBDurumId.Contains(p.MezuniyetYayinKontrolDurumID)).OrderBy(o => o.MezuniyetYayinKontrolDurumID);
                var data = qdata.ToList();
                return data;

            }

        }
        public static bool IsMakaleYayinDurumIsteniyor(this int yayinTurId)
        {
            return new List<int> { 5, 4 }.Contains(yayinTurId);
        }
        public static IHtmlString ToMezuniyetDurum(this FrMezuniyetBasvurulari model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Mezuniyet", "BasvuruDurumView");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetayBasvuru(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t1_Basvuru");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetayEykSureci(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t2_EYKSureci");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetaySinavSureci(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t3_SinavSureci");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetayTezKontrolSureci(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t4_TezKontrolSureci");
            return pagerString;
        }
        public static IHtmlString ToMezuniyetDetayMezuniyetSureci(this MezuniyetBasvuruDetayDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t5_MezuniyetSureci");
            return pagerString;
        }
        public static string ToMezuniyetJuriUnvanAdi(this string unvanAdi)
        {
            unvanAdi = unvanAdi.Trim().ToLower().Replace("  ", ".").Replace(". ", ".").Replace(" .", ".").Replace(" ", ".");
            var profUnvan = new List<string> { "PROFESÖR".ToLower(), "PROFESÖR.DR".ToLower(), "PROF.DR.".ToLower(), "Prof.".ToLower() };
            var docUnvan = new List<string> { "DOÇENT".ToLower(), "DOÇENT.DR".ToLower(), "Doç.".ToLower() };
            var ogUyeUnvan = new List<string> { "DR.ÖĞR.ÜYE".ToLower(), "DR.ÖĞR.ÜYESİ".ToLower(), "DR.ÖĞRETİM.ÜYE".ToLower(), "DR.ÖĞRETİM.ÜYESİ".ToLower() };
            if (profUnvan.Any(a => a.Contains(unvanAdi))) return "PROF.DR.";
            else if (docUnvan.Any(a => a.Contains(unvanAdi))) return "DOÇ.DR.";
            else if (ogUyeUnvan.Any(a => a.Contains(unvanAdi))) return "DR.ÖĞR.ÜYE.";
            else return unvanAdi.ToUpper();
        }
        public static bool ToJoFormSuccessRow(this string juriTipAdi, bool tezDiliTr, bool adSoyadSuccess, bool unvanAdiSuccess, bool eMailSuccess, bool universiteIdSuccess, bool uzmanlikAlaniSuccess, bool bilimselCalismalarAnahtarSozcuklerSuccess, bool dilSinavAdiSuccess, bool dilPuaniSuccess)
        {
            var retVal = false;
            if (new List<string> { "YtuIciJuri4", "YtuDisiJuri4" }.Contains(juriTipAdi))
            {
                if (tezDiliTr)
                {
                    retVal = (
                                (
                                    adSoyadSuccess &&
                                    unvanAdiSuccess &&
                                    eMailSuccess &&
                                    universiteIdSuccess &&
                                    uzmanlikAlaniSuccess &&
                                    bilimselCalismalarAnahtarSozcuklerSuccess
                                 )
                                 ||
                                 (
                                     !adSoyadSuccess &&
                                     !unvanAdiSuccess &&
                                     !eMailSuccess &&
                                     !universiteIdSuccess &&
                                     !uzmanlikAlaniSuccess &&
                                     !bilimselCalismalarAnahtarSozcuklerSuccess

                                 )
                            );
                }
                else
                {
                    retVal = (
                               (
                                   adSoyadSuccess &&
                                   unvanAdiSuccess &&
                                   eMailSuccess &&
                                   universiteIdSuccess &&
                                   uzmanlikAlaniSuccess &&
                                   bilimselCalismalarAnahtarSozcuklerSuccess &&
                                   dilSinavAdiSuccess &&
                                   dilPuaniSuccess
                                )
                                ||
                                (
                                    !adSoyadSuccess &&
                                    !unvanAdiSuccess &&
                                    !eMailSuccess &&
                                    !universiteIdSuccess &&
                                    !uzmanlikAlaniSuccess &&
                                    !bilimselCalismalarAnahtarSozcuklerSuccess &&
                                    !dilSinavAdiSuccess &&
                                    !dilPuaniSuccess
                                )
                           );
                }

            }
            else
            {
                if (tezDiliTr)
                {
                    retVal = (
                                  adSoyadSuccess &&
                                  unvanAdiSuccess &&
                                  eMailSuccess &&
                                  universiteIdSuccess &&
                                  uzmanlikAlaniSuccess &&
                                  bilimselCalismalarAnahtarSozcuklerSuccess
                              );
                }
                else
                {
                    retVal = (
                                 adSoyadSuccess &&
                                 unvanAdiSuccess &&
                                 eMailSuccess &&
                                 universiteIdSuccess &&
                                 uzmanlikAlaniSuccess &&
                                 bilimselCalismalarAnahtarSozcuklerSuccess &&
                                 dilSinavAdiSuccess &&
                                 dilPuaniSuccess
                             );
                }
            }
            return retVal;
        }
        public static List<CmbIntDto> GetCmbMezuniyetSurecleri(string enstituKod, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.MezuniyetSurecis.Where(p => p.EnstituKod == enstituKod)
                            join d in db.Donemlers on s.DonemID equals d.DonemID
                            orderby s.BaslangicTarihi descending
                            select new
                            {
                                s.MezuniyetSurecID,
                                s.BaslangicYil,
                                s.BitisYil,
                                s.SiraNo,
                                d.DonemAdi,
                                s.BaslangicTarihi,
                                s.BitisTarihi
                            }).ToList();
                foreach (var item in data)
                {
                    lst.Add(new CmbIntDto { Value = item.MezuniyetSurecID, Caption = (item.BaslangicYil + "/" + item.BitisYil + " " + item.DonemAdi + " " + item.SiraNo + " (" + item.BaslangicTarihi.ToDateString() + " - " + item.BitisTarihi.ToDateString() + ")") });
                }
            }
            return lst;
        }
        public static List<CmbIntDto> GetCmbMezuniyetSurecGroup(string enstituKod, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.MezuniyetSurecis.Where(p => p.EnstituKod == enstituKod)
                            join d in db.Donemlers on s.DonemID equals d.DonemID
                            select new
                            {
                                s.DonemID,
                                s.BaslangicYil,
                                s.BitisYil,
                                d.DonemAdi
                            }).Distinct().OrderByDescending(o => o.BaslangicYil).ToList();
                foreach (var item in data)
                {
                    lst.Add(new CmbIntDto { Value = (item.BaslangicYil + "" + item.DonemID).ToInt().Value, Caption = (item.BaslangicYil + "/" + (item.BitisYil) + " " + item.DonemAdi) });
                }
            }
            return lst;
        }
        public static List<CmbStringDto> GetCmbMezuniyetKayitDonemleri(string enstituKod, int? mezuniyetSurecId = null, bool bosSecimVar = false)
        {
            var lst = new List<CmbStringDto>();
            if (bosSecimVar) lst.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var qData = (from s in db.MezuniyetSurecis.Where(p => p.EnstituKod == enstituKod)
                             join bsv in db.MezuniyetBasvurularis on s.MezuniyetSurecID equals bsv.MezuniyetSurecID
                             join d in db.Donemlers on bsv.KayitOgretimYiliDonemID equals d.DonemID
                             orderby s.BaslangicTarihi descending
                             select new
                             {
                                 s.MezuniyetSurecID,
                                 bsv.KayitOgretimYiliBaslangic,
                                 bsv.KayitOgretimYiliDonemID,
                                 d.DonemAdi
                             }).AsQueryable();
                if (mezuniyetSurecId.HasValue) qData = qData.Where(p => p.MezuniyetSurecID == mezuniyetSurecId.Value);
                var dataDst = qData.Select(s => new { s.KayitOgretimYiliBaslangic, s.KayitOgretimYiliDonemID, s.DonemAdi }).Distinct().OrderByDescending(o => o.KayitOgretimYiliBaslangic).ThenBy(t => t.KayitOgretimYiliDonemID).ToList();
                foreach (var item in dataDst)
                {
                    lst.Add(new CmbStringDto { Value = item.KayitOgretimYiliBaslangic + "_" + item.KayitOgretimYiliDonemID, Caption = (item.KayitOgretimYiliBaslangic + "/" + (item.KayitOgretimYiliBaslangic + 1) + " " + item.DonemAdi) });
                }
            }
            return lst;
        }
        public static List<CmbMultyTypeDto> GetCmbOkunanDonemList(int mezuniyetSurecId, int basYil, int donemId)
        {
            var kdonems = new List<CmbMultyTypeDto>();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var surec = db.MezuniyetSurecis.First(p => p.MezuniyetSurecID == mezuniyetSurecId);

                for (int i = basYil; i <= surec.BaslangicYil; i++)
                {
                    for (int r = 1; r <= 2; r++)
                    {
                        bool add = true;
                        if (i == basYil)
                        {
                            if (donemId == 2 && r == 1) add = false;
                        }
                        else if (i == surec.BaslangicYil)
                        {
                            if (surec.DonemID == 1 && r == 2) add = false;
                        }
                        if (add) kdonems.Add(new CmbMultyTypeDto { Inx = i, Value = r });
                    }
                }
            }
            return kdonems;
        }
        public static List<CmbStringDto> GetCmbMezuniyetJofUnvanlar(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            var unvanList = new List<string> { "PROF.DR.", "DOÇ.DR.", "DR.ÖĞR.ÜYE." };
            foreach (var item in unvanList)
            {
                dct.Add(new CmbStringDto { Value = item, Caption = item });
            }
            return dct;

        }
        public static List<CmbIntDto> GetCmbMezuniyetYayinDurum(bool bosSecimVar = false, bool tumu = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinKontrolDurumlaris.Where(p => p.IsAktif && (tumu || p.BasvuranGorsun)).OrderBy(o => o.MezuniyetYayinKontrolDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinKontrolDurumID, Caption = item.MezuniyetYayinKontrolDurumAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> GetCmbMzSinavDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetSinavDurumlaris.ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetSinavDurumID, Caption = item.MezuniyetSinavDurumAdi });
                }
            }
            return dct;
        }
        public static List<CmbBoolDto> GetCmbTeslimFormDurumu(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Teslim Formu Oluşturuldu" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Teslim Formu Oluşturulmadı" });

            return dct;
        }
        public static List<CmbBoolDto> GetCmbMezuniyetDurum(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = null, Caption = "Sonuç Girilmedi" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Mezun Oldu" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Mezun Olamadı" });

            return dct;
        }
        public static List<CmbIntDto> GetCmbMezuniyetDurumId(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = -1, Caption = "" });
            dct.Add(new CmbIntDto { Value = null, Caption = "Sonuç Girilmedi" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Mezun Oldu" });
            dct.Add(new CmbIntDto { Value = 0, Caption = "Mezun Olamadı" });
            return dct;
        }
        public static List<CmbIntDto> GetCmbMezuniyetSurecYayinTurleri(int mezuniyetSurecId, int kullaniciId, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kriter = MezuniyetBus.GetMezuniyetAktifOgrenimTipiYayinBilgileri(mezuniyetSurecId, kullaniciId);
                var mezuniyetYayinTurIDs = kriter.Where(p => p.IsGecerli).Select(s => s.MezuniyetYayinTurID).Distinct().ToList();

                var qdata = db.MezuniyetYayinTurleris.AsQueryable();
                if (mezuniyetYayinTurIDs.Count > 0) qdata = qdata.Where(p => mezuniyetYayinTurIDs.Contains(p.MezuniyetYayinTurID));
                var data = qdata.OrderBy(o => o.MezuniyetYayinTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinTurID, Caption = item.MezuniyetYayinTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> GetCmbMezuniyetYayinDurumListe(bool bosSecimVar = false, bool tumu = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinKontrolDurumlaris.Where(p => (tumu || p.BasvuranGorsun)).OrderBy(o => o.MezuniyetYayinKontrolDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinKontrolDurumID, Caption = item.MezuniyetYayinKontrolDurumAdi });
                }
            }

            return dct;

        }
        public static List<CmbIntDto> GetCmbMezuniyetYayinBelgeTurleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinBelgeTurleris.OrderBy(o => o.BelgeTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinBelgeTurID, Caption = item.BelgeTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> GetCmbMezuniyetYayinLinkTurleri(bool isKaynakOrYayin, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinLinkTurleris.Where(p => p.IsKaynakOrYayin == isKaynakOrYayin).OrderBy(o => o.LinkTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinLinkTurID, Caption = item.LinkTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> GetCmbMezuniyetYayinMetinTurleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinMetinTurleris.OrderBy(o => o.MetinTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinMetinTurID, Caption = item.MetinTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> GetCmbJuriOneriFormuDurumu(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            dct.Add(new CmbIntDto { Value = 0, Caption = "Form oluşturulmadı" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Form oluşturuldu" });
            dct.Add(new CmbIntDto { Value = 2, Caption = "Eyk'Ya Gonderimi Onaylandi" });
            dct.Add(new CmbIntDto { Value = 3, Caption = "Eyk'Ya Gonderimi Onaylanmadi" });
            dct.Add(new CmbIntDto { Value = 4, Caption = "Eyk'Da Onaylandı" });
            dct.Add(new CmbIntDto { Value = 5, Caption = "Eyk'Da Onaylanmadı" });

            return dct;

        }
        public static List<CmbIntDto> GetCmbTezDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            dct.Add(new CmbIntDto { Value = 2, Caption = "İşlem Bekleyenler" });
            dct.Add(new CmbIntDto { Value = 0, Caption = "Düzeltme Talep Edildi" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Onaylananlar" });

            return dct;

        }
        public static MmMessage SendMailMezuniyetDegerlendirmeLink(int srTalepId, Guid? uniqueId = null, bool isLinkOrSonuc = false, bool isYeniLink = true, string eMail = "")
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {

                    var srTalep = db.SRTalepleris.First(p => p.SRTalepID == srTalepId);
                    var qJuriler = srTalep.SRTaleplerJuris.AsQueryable();
                    qJuriler = uniqueId.HasValue ? qJuriler.Where(p => p.UniqueID == uniqueId.Value) : qJuriler.Where(p => p.JuriTipAdi != "TezDanismani");
                    var juriler = qJuriler.ToList();
                    var mb = srTalep.MezuniyetBasvurulari;
                    var mModel = new List<SablonMailModel>();

                    var enstitu = mb.MezuniyetSureci.Enstituler;

                    var abdL = mb.Programlar.AnabilimDallari;
                    var prgL = mb.Programlar;
                    var jof = mb.MezuniyetJuriOneriFormlaris.First();



                    if (isLinkOrSonuc)
                    {

                        foreach (var item in juriler)
                        {
                            if (isYeniLink) item.UniqueID = Guid.NewGuid();
                            mModel.Add(new SablonMailModel
                            {
                                UniqueID = item.UniqueID,

                                UnvanAdi = item.UnvanAdi,
                                AdSoyad = item.JuriAdi,
                                EMails = new List<MailSendList> { new MailSendList { EMail = (eMail.IsNullOrWhiteSpace() ? item.Email : eMail), ToOrBcc = true } },
                                MailSablonTipID = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipi.Mez_SinavDegerlendirmeDavetGonderimJuriDR : MailSablonTipi.Mez_SinavDegerlendirmeDavetGonderimJuriYL,
                                JuriTipAdi = item.JuriTipAdi,
                            });
                        }
                    }
                    else
                    {
                        var danisman = db.Kullanicilars.First(p => p.KullaniciID == mb.TezDanismanID);
                        var srDanisman = srTalep.SRTaleplerJuris.First(p => p.JuriTipAdi == "TezDanismani");
                        mModel.Add(new SablonMailModel
                        {
                            UniqueID = null,
                            UnvanAdi = danisman.Unvanlar.UnvanAdi,
                            AdSoyad = danisman.Ad + " " + danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = !srDanisman.Email.IsNullOrWhiteSpace() ? srDanisman.Email : danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipi.Mez_SinavSonucBilgiGonderimDanismanDR : MailSablonTipi.Mez_SinavSonucBilgiGonderimDanismanYL,
                            JuriTipAdi = "",
                        });
                        var ogrenci = db.Kullanicilars.First(p => p.KullaniciID == mb.KullaniciID);
                        mModel.Add(new SablonMailModel
                        {
                            UniqueID = null,

                            UnvanAdi = "",
                            AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipi.Mez_SinavSonucBilgiGonderimOgrenciDR : MailSablonTipi.Mez_SinavSonucBilgiGonderimOgrenciYL,
                            JuriTipAdi = "",
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
                        if (!isLinkOrSonuc)
                        {
                            var ids = new List<int?>() { srTalepId };
                            var ekler = Management.exportRaporPdf(RaporTipleri.MezuniyetTezSinavSonucFormu, ids);
                            gonderilenMailEkleri.AddRange(ekler.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }));
                            item.Attachments.AddRange(ekler);
                        }
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
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailMezuniyetDegerlendirmeLink", LogType.Uyarı);
                        }
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (isLinkOrSonuc)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@JuriAdSoyad"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "JuriAdSoyad", Value = item.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@JuriUnvanAdi"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "JuriUnvanAdi", Value = item.UnvanAdi });
                        }
                        else
                        {
                            if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = mb.TezDanismanUnvani });
                            if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = mb.TezDanismanAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = mb.Ad + " " + mb.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = mb.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SinavTarihi", Value = srTalep.Tarih.ToLongDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@SinavSaati"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SinavSaati", Value = $"{srTalep.BasSaat:hh\\:mm}" });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/Mezuniyet/GSinavDegerlendir?UniqueID=" + item.UniqueID, IsLink = true });
                        }

                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = enstitu.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = mCOntent.Title
                        };
                        if (snded)
                        {
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
                            if (isLinkOrSonuc)
                            {
                                var juri = juriler.FirstOrDefault(p => p.UniqueID == item.UniqueID);

                                juri.DegerlendirmeIslemTarihi = null;
                                juri.DegerlendirmeIslemYapanIP = null;
                                juri.DegerlendirmeYapanID = null;
                                juri.MezuniyetSinavDurumID = null;
                                juri.Aciklama = null;
                                juri.IsLinkGonderildi = true;
                                juri.LinkGonderimTarihi = DateTime.Now;
                                juri.LinkGonderenID = UserIdentity.Current.Id;
                                db.SaveChanges();
                                LogIslemleri.LogEkle("SRTaleplerJuri", IslemTipi.Update, juri.ToJson());
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
                message = "Tez Sınavı değerlendirmesi için Jüri üyelerine değerlendirme davetiye linki mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailMezuniyetDegerlendirmeLink \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailMezuniyetSinavYerBilgisi(int srTalepId, bool isOnaylandi)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var talep = db.SRTalepleris.First(p => p.SRTalepID == srTalepId);

                    var mb = talep.MezuniyetBasvurulari;
                    if (!mb.MezuniyetJuriOneriFormlaris.Any())
                        return new MmMessage()
                        {
                            Messages = new List<string>
                                { "Rezervasyona ait mezuniyet başvurusu bulunamadığı öğrenci ve jüri üyelerine için mail gönderilemedi!" }
                        };
                    var juriOneriFormu = mb.MezuniyetJuriOneriFormlaris.First();


                    var enstitu = mb.MezuniyetSureci.Enstituler;

                    var mModel = new List<SablonMailModel>();
                    var juriSablonTipId = 0;
                    var ogrenciSablonTipId = 0;
                    if (isOnaylandi)
                    {
                        juriSablonTipId = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipi.Mez_SinavYerBilgisiGonderimJuriDoktora : MailSablonTipi.Mez_SinavYerBilgisiGonderimJuriYL;
                        ogrenciSablonTipId = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipi.Mez_SinavYerBilgisiGonderimOgrenciDoktora : MailSablonTipi.Mez_SinavYerBilgisiGonderimOgrenciYL;
                    }
                    else
                    {
                        juriSablonTipId = MailSablonTipi.Mez_SinavYerBilgisiOnaylanmadi;
                        ogrenciSablonTipId = MailSablonTipi.Mez_SinavYerBilgisiOnaylanmadi;
                    }


                    var juriler = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek.HasValue).ToList();

                    mModel.Add(new SablonMailModel
                    {
                        AdSoyad = mb.Ad + " " + mb.Soyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = mb.Kullanicilar.EMail, ToOrBcc = true } },
                        MailSablonTipID = ogrenciSablonTipId,
                    });
                    var danisman = juriler.First(p => p.JuriTipAdi == "TezDanismani");
                    if (!isOnaylandi)
                    {
                        mModel.Add(new SablonMailModel
                        {

                            AdSoyad = danisman.UnvanAdi + " " + danisman.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = juriSablonTipId,

                        });
                        if (!mb.TezEsDanismanEMail.IsNullOrWhiteSpace())
                            mModel.Add(new SablonMailModel
                            {

                                AdSoyad = mb.TezEsDanismanUnvani + " " + mb.TezEsDanismanAdi,
                                EMails = new List<MailSendList> { new MailSendList { EMail = mb.TezEsDanismanEMail, ToOrBcc = true } },
                                MailSablonTipID = juriSablonTipId,

                            });
                    }
                    else
                    {
                        foreach (var item in juriler.Where(p => p.IsAsilOrYedek == true))
                        {


                            mModel.Add(new SablonMailModel
                            {

                                AdSoyad = item.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                                MailSablonTipID = juriSablonTipId,
                                JuriTipAdi = item.JuriTipAdi,
                                UnvanAdi = item.UnvanAdi
                            });

                            if (item.JuriTipAdi == "TezDanismani" && !mb.TezEsDanismanEMail.IsNullOrWhiteSpace())
                            {
                                mModel.Add(new SablonMailModel
                                {

                                    AdSoyad = mb.TezEsDanismanAdi,
                                    EMails = new List<MailSendList> { new MailSendList { EMail = mb.TezEsDanismanEMail, ToOrBcc = true } },
                                    MailSablonTipID = juriSablonTipId,
                                    JuriTipAdi = item.JuriTipAdi,
                                    UnvanAdi = mb.TezEsDanismanUnvani
                                });
                            }
                        }


                    }


                    foreach (var item in mModel)
                    {

                        var abdL = mb.Programlar.AnabilimDallari;
                        var prgL = mb.Programlar;
                        item.Sablon = db.MailSablonlaris.First(p => p.MailSablonTipID == item.MailSablonTipID && p.EnstituKod == enstitu.EnstituKod);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailMezuniyetSinavYerBilgisi", LogType.Uyarı);
                        }


                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EYKTarihi", Value = mb.EYKTarihi.Value.ToDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = mb.Ad + " " + mb.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = mb.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SinavTarihi", Value = talep.Tarih.ToLongDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@SinavSaati"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SinavSaati", Value = $"{talep.BasSaat:hh\\:mm}" + "-" + $"{talep.BitSaat:hh\\:mm}" });
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            var sinavYerAdi = talep.SRSalonID.HasValue ? talep.SRSalonlar.SalonAdi : talep.SalonAdi;
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SinavYeri", Value = sinavYerAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@IptalAciklamasi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "IptalAciklamasi", Value = talep.SRDurumAciklamasi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanBilgi", Value = danisman.UnvanAdi + " " + danisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUni"))
                        {
                            var universiteAdi = danisman.UniversiteID.HasValue ? danisman.Universiteler.Ad : danisman.UniversiteAdi;
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUni", Value = universiteAdi });
                        }
                        foreach (var itemAsil in juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).Select((s, inx) => new { s, inx = inx + 1 }))
                        {

                            if (item.SablonParametreleri.Any(a => a == "@AsilBilgi" + itemAsil.inx))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AsilBilgi" + itemAsil.inx, Value = itemAsil.s.UnvanAdi + " " + itemAsil.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@AsilBilgiUni" + itemAsil.inx))
                            {
                                var universiteAdi = itemAsil.s.UniversiteID.HasValue ? itemAsil.s.Universiteler.Ad : itemAsil.s.UniversiteAdi;
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AsilBilgiUni" + itemAsil.inx, Value = universiteAdi });
                            }
                        }
                        foreach (var itemYedek in juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == false).Select((s, inx) => new { s, inx = inx + 1 }))
                        {
                            if (item.SablonParametreleri.Any(a => a == "@YedekBilgi" + itemYedek.inx))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YedekBilgi" + itemYedek.inx, Value = itemYedek.s.UnvanAdi + " " + itemYedek.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@YedekBilgiUni" + itemYedek.inx))
                            {
                                var universiteAdi = itemYedek.s.UniversiteID.HasValue ? itemYedek.s.Universiteler.Ad : itemYedek.s.UniversiteAdi;
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YedekBilgiUni" + itemYedek.inx, Value = universiteAdi });
                            }
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
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            kModel.GonderilenMailEkleris = gonderilenMEkleris;
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }
                    var message = "'" + talep.Kullanicilar.Ad + " " + talep.Kullanicilar.Soyad + "'  kullanıcısının yapmış olduğu salon rezervasyonuna ait " + juriler.Count + " adet jüriye mail olarak gönderildi!";
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Salon rezervasyonuna ait jürilere mail gönderilirken bir hata oluştu! \r\nSRTalepID:" + srTalepId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailMezuniyetSinavYerBilgisi \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailMezuniyetSinavSonucu(int srTalepId, int mezuniyetSinavDurumId)
        {
            var mmMessage = new MmMessage();
            try
            {
                var SablonTipID = 0;
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var talep = db.SRTalepleris.First(p => p.SRTalepID == srTalepId);

                    var mb = talep.MezuniyetBasvurulari;
                    var mbOtipKriter = talep.MezuniyetBasvurulari.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == mb.OgrenimTipKod);
                    var juriOneriFormu = mb.MezuniyetJuriOneriFormlaris.First();


                    var enstitu = mb.MezuniyetSureci.Enstituler;

                    var mModel = new List<SablonMailModel>();
                    if (mezuniyetSinavDurumId == MezuniyetSinavDurum.Basarili)
                    {
                        SablonTipID = mb.OgrenimTipKod.IsDoktora() ? MailSablonTipi.Mez_SinavSonucuBasariliBilgisiGonderimDoktora : MailSablonTipi.Mez_SinavSonucuBasariliBilgisiGonderimYL;
                    }
                    else if (mezuniyetSinavDurumId == MezuniyetSinavDurum.Uzatma)
                    {
                        SablonTipID = MailSablonTipi.Mez_SinavSonucuUzatmaBilgisiGonderim;
                    }


                    var tezDanismani = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TezDanismani");

                    var mezuniyetMailModel = new SablonMailModel
                    {

                        AdSoyad = mb.Ad + " " + mb.Soyad,
                        EMails = new List<MailSendList> {
                                        new MailSendList {EMail= mb.Kullanicilar.EMail,
                                                           ToOrBcc = true
                                                         }
                                                    },
                        MailSablonTipID = SablonTipID,
                        Attachments = mezuniyetSinavDurumId == MezuniyetSinavDurum.Basarili ? Management.exportRaporPdf(RaporTipleri.MezuniyetTezDuzeltmeVeJuriUyelerineTezTeslimTutanagi, new List<int?> { srTalepId }) : new List<System.Net.Mail.Attachment>()
                    };


                    mezuniyetMailModel.EMails.Add(new MailSendList
                    {
                        EMail = tezDanismani.EMail,
                        ToOrBcc = false
                    });
                    if (!mb.TezEsDanismanEMail.IsNullOrWhiteSpace()) mezuniyetMailModel.EMails.Add(new MailSendList { EMail = mb.TezEsDanismanEMail, ToOrBcc = false });



                    mModel.Add(mezuniyetMailModel);


                    foreach (var item in mModel)
                    {


                        var abdL = mb.Programlar.AnabilimDallari;
                        var prgL = mb.Programlar;
                        item.Sablon = db.MailSablonlaris.First(p => p.MailSablonTipID == item.MailSablonTipID && p.EnstituKod == enstitu.EnstituKod);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));


                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var FExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(FExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailMezuniyetSinavSonucu", LogType.Uyarı);
                        }

                        var paramereDegerleri = new List<MailReplaceParameterDto>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });

                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = item.AdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SinavTarihi", Value = talep.Tarih.ToString("dd-MM-yyyy") });
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            var sinavYerAdi = talep.SRSalonID.HasValue ? talep.SRSalonlar.SalonAdi : talep.SalonAdi;
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SinavYeri", Value = sinavYerAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@UzatmaTarihi"))
                        {
                            if (talep.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma)
                            {
                                var uzatmaTarihi = talep.Tarih.AddDays(mbOtipKriter.MBSinavUzatmaSuresiGun).ToString("dd-MM-yyyy");
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "UzatmaTarihi", Value = uzatmaTarihi });
                            }
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
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            kModel.GonderilenMailEkleris = gonderilenMEkleris;
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }
                    var message = "'" + talep.Kullanicilar.Ad + " " + talep.Kullanicilar.Soyad + "'  öğrencisinin tez sınav sonucu bilgisi mail olarak gönderildi!";
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez sınav sonucu bilgisi mail olarak gönderilirken bir hata oluştu! \r\nSRTalepID:" + srTalepId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailMezuniyetSinavSonucu \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailMezuniyetTezSablonKontrol(int mezuniyetBasvurulariTezDosyaId, int sablonTipId, string aciklama = null)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var talep = db.MezuniyetBasvurulariTezDosyalaris.First(p => p.MezuniyetBasvurulariTezDosyaID == mezuniyetBasvurulariTezDosyaId);
                    var mezuniyetBasvuru = talep.MezuniyetBasvurulari;
                    var srTalep = mezuniyetBasvuru.SRTalepleris.First(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili);


                    var enstitu = mezuniyetBasvuru.MezuniyetSureci.Enstituler;

                    var mModel = new List<SablonMailModel>();

                    var mezuniyetMailModel = new SablonMailModel
                    {
                        AdSoyad = mezuniyetBasvuru.Ad + " " + mezuniyetBasvuru.Soyad,
                        EMails = new List<MailSendList> {
                                        new MailSendList {EMail= mezuniyetBasvuru.Kullanicilar.EMail,
                                                           ToOrBcc = true
                                                         }
                                                    },
                        MailSablonTipID = sablonTipId,
                        Attachments = sablonTipId == MailSablonTipi.Mez_TezKontrolTezDosyasiBasarili ? Management.exportRaporPdf(RaporTipleri.MezuniyetTezKontrolFormu, new List<int?> { mezuniyetBasvurulariTezDosyaId }) : new List<System.Net.Mail.Attachment>()
                    };

                    mModel.Add(mezuniyetMailModel);


                    foreach (var item in mModel)
                    {


                        item.Sablon = db.MailSablonlaris.First(p => p.MailSablonTipID == item.MailSablonTipID && p.EnstituKod == enstitu.EnstituKod);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));


                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var FExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(FExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailMezuniyetSinavSonucu", LogType.Uyarı);
                        }

                        var paramereDegerleri = new List<MailReplaceParameterDto>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });

                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@SRTarihi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "SRTarihi", Value = srTalep.Tarih.ToShortDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@Aciklama"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Aciklama", Value = aciklama });
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
                                Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")"
                            };
                            if (sablonTipId == MailSablonTipi.Mez_TezKontrolTezDosyasiYuklendi || UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated) kModel.IslemYapanID = 1;
                            else kModel.IslemYapanID = UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            kModel.GonderilenMailEkleris = gonderilenMEkleris;
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }
                    mmMessage.IsSuccess = true;
                    if (sablonTipId == MailSablonTipi.Mez_TezKontrolTezDosyasiBasarili)
                        mmMessage.Messages.Add("'" + mezuniyetBasvuru.Kullanicilar.Ad + " " + mezuniyetBasvuru.Kullanicilar.Soyad + "'  öğrencisine ait tez şablon dosyası kontrolü başarılı olduğu bilgisi mail olarak gönderildi!");
                    else if (sablonTipId == MailSablonTipi.Mez_TezKontrolTezDosyasiOnaylanmadi)
                        mmMessage.Messages.Add("'" + mezuniyetBasvuru.Kullanicilar.Ad + " " + mezuniyetBasvuru.Kullanicilar.Soyad + "'  öğrencisine ait tez şablon dosyası kontrolü onaylanmadığı bilgisi mail olarak gönderildi!");
                    else if (sablonTipId == MailSablonTipi.Mez_TezKontrolTezDosyasiYuklendi)
                        mmMessage.Messages.Add("'" + mezuniyetBasvuru.Kullanicilar.Ad + " " + mezuniyetBasvuru.Kullanicilar.Soyad + "'  öğrencisine ait tez şablon dosyası yüklendi bilgisi mail olarak gönderildi!");
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var msg = "";
                if (sablonTipId == MailSablonTipi.Mez_TezKontrolTezDosyasiBasarili)
                    msg = "Tez şablon dosyası kontrolü başarılı olduğu bilgisi mail olarak gönderilirken bir hata oluştu! \r\nMezuniyetBasvurulariTezDosyaID:" + mezuniyetBasvurulariTezDosyaId;
                else if (sablonTipId == MailSablonTipi.Mez_TezKontrolTezDosyasiOnaylanmadi)
                    msg = "Tez şablon dosyası kontrolü onaylanmadığı bilgisii mail olarak gönderilirken bir hata oluştu! \r\nMezuniyetBasvurulariTezDosyaID:" + mezuniyetBasvurulariTezDosyaId;
                else if (sablonTipId == MailSablonTipi.Mez_TezKontrolTezDosyasiYuklendi)
                    msg = "Tez şablon dosyası yüklendi bilgisi mail olarak gönderilirken bir hata oluştu! \r\nMezuniyetBasvurulariTezDosyaID:" + mezuniyetBasvurulariTezDosyaId;

                SistemBilgilendirmeBus.SistemBilgisiKaydet(msg + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailMezuniyetTezSablonKontrol \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                mmMessage.Messages.Add(msg + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
    }
}