using System;
using System.Collections.Generic;
using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.MailManager
{
    public class MailSenderTalep
    {
        public static MmMessage SendTopluBilgiMaili(List<int> talepGelenTalepIDs, string enstituKod, string aciklama = "")
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var entities = new LisansustuBasvuruSistemiEntities())
                {

                    var taleps = (from s in entities.TalepGelenTaleplers
                                  join ts in entities.TalepSurecleris on s.TalepSurecID equals ts.TalepSurecID
                                  join kul in entities.Kullanicilars on s.KullaniciID equals kul.KullaniciID
                                  join tt in entities.TalepTipleris on s.TalepTipID equals tt.TalepTipID
                                  join td in entities.TalepDurumlaris on s.TalepDurumID equals td.TalepDurumID
                                  join ags in entities.TalepArGorStatuleris on s.TalepArGorStatuID equals ags.TalepArGorStatuID into defAgs
                                  from ags in defAgs.DefaultIfEmpty()
                                  join ot in entities.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod into defO
                                  from ot in defO.DefaultIfEmpty()
                                  join otl in entities.OgrenimTipleris on ot.OgrenimTipID equals otl.OgrenimTipID into defOtl
                                  from otl in defOtl.DefaultIfEmpty()
                                  join prl in entities.Programlars on s.ProgramKod equals prl.ProgramKod into defprl
                                  from prl in defprl.DefaultIfEmpty()
                                  join abl in entities.AnabilimDallaris on new { AnabilimDaliID = (prl != null ? prl.AnabilimDaliID : (int?)null) } equals new { AnabilimDaliID = (int?)abl.AnabilimDaliID } into defabl
                                  from abl in defabl.DefaultIfEmpty()
                                  where talepGelenTalepIDs.Contains(s.TalepGelenTalepID)
                                  select new
                                  {
                                      s.TalepGelenTalepID,
                                      s.TalepSurecID,
                                      s.KullaniciID,
                                      kul.ResimAdi,
                                      kul.EMail,
                                      kul.YtuOgrencisi,
                                      s.TalepTipID,
                                      tt.TalepTipAdi,
                                      tt.TalepTipAciklama,
                                      s.TalepDurumID,
                                      s.TalepDurumAciklamasi,

                                      td.TalepDurumAdi,
                                      td.ClassName,
                                      td.Color,
                                      s.TalepTarihi,
                                      s.AdSoyad,
                                      s.OgrenciNo,
                                      s.OgrenimTipID,
                                      s.OgrenimTipKod,
                                      s.DoktoraTezOneriTarihi,
                                      otl.OgrenimTipAdi,
                                      abl.AnabilimDaliAdi,
                                      prl.ProgramAdi,
                                      s.IsYtuArGor,
                                      s.TalepArGorStatuID,
                                      ags.StatuAdi,
                                      s.IsDersYukuTamamlandi,
                                      s.IsHarcBorcuVar,
                                      s.IslemTarihi,
                                      s.IslemYapanID,
                                      s.IslemYapanIP,
                                  }).ToList();
                    foreach (var talep in taleps)
                    {
                        var htmlBigliRow = new List<MailTableRowDto>();
                        var contentBilgi = new MailTableContentDto();
                        htmlBigliRow.Add(new MailTableRowDto { Baslik = "Ad Soyad", Aciklama = talep.AdSoyad });
                        if (talep.YtuOgrencisi)
                        {
                            if (!talep.OgrenciNo.IsNullOrWhiteSpace()) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Öğrenci No", Aciklama = talep.OgrenciNo });
                            if (talep.OgrenimTipKod.HasValue) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Öğrenim Seviyesi", Aciklama = talep.OgrenimTipAdi });
                            if (!talep.ProgramAdi.IsNullOrWhiteSpace()) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Program", Aciklama = talep.ProgramAdi });
                        }

                        htmlBigliRow.Add(new MailTableRowDto { Baslik = "Talep Tipi", Aciklama = talep.TalepTipAdi });
                        string talepTipAciklama;
                        switch (talep.TalepTipID)
                        {
                            case TalepTipiEnum.LisansustuSureUzatmaTalebi:
                                talepTipAciklama = talep.OgrenimTipKod.IsDoktora() ?
                                    "Bu talep tipini seçecek öğrenciler, doktora tez önerisinden başarılı olmuş ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir. "
                                    :
                                    "Bu talep tipini seçecek öğrenciler Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşamasını tamamlamış ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir.";
                                break;
                            case TalepTipiEnum.Covid19KayitDondurmaTalebi:
                                talepTipAciklama = talep.OgrenimTipKod.IsDoktora() ?
                                    "Bu talep tipini seçecek olan öğrencilerimizden: doktora tez önerisinden başarılı olunmuş ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir. Aksi takdirde talebiniz kabul edilmeyecektir."
                                    :
                                    "Bu talep tipini seçecek olan öğrencilerimizden: YTÜ Lisansüstü Eğitim ve Öğretim Yönetmeliği Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşaması tamamlanmış ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir Aksi takdirde talebiniz kabul edilmeyecektir.";
                                break;
                            default:
                                talepTipAciklama = talep.TalepTipAciklama;
                                break;
                        }
                        if (!talepTipAciklama.IsNullOrWhiteSpace()) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Talep Tipi Açıklaması", Aciklama = talepTipAciklama });
                        htmlBigliRow.Add(new MailTableRowDto { Baslik = "Talep Tarihi", Aciklama = talep.TalepTarihi.ToFormatDateAndTime() });
                        htmlBigliRow.Add(new MailTableRowDto { Baslik = "Talep Durumu", Aciklama = talep.TalepDurumAdi });
                        if (talep.TalepDurumID == TalepDurumuEnum.Rededildi) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Red Açıklaması", Aciklama = talep.TalepDurumAciklamasi });
                        if (!aciklama.IsNullOrWhiteSpace()) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Not", Aciklama = aciklama });

                        contentBilgi.GrupBasligi = "'" + talep.TalepTipAdi + "' talebiniz " + talep.TalepDurumAdi;
                        contentBilgi.Detaylar = htmlBigliRow;

                        var mmmC = new MailMainContentDto();
                        var enstituAdi = entities.Enstitulers.First(p => p.EnstituKod == enstituKod).EnstituAd;
                        mmmC.EnstituAdi = enstituAdi;
                        mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                        var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(enstituKod);
                        var erisimAdresi = mailBilgi.SistemErisimAdresi;
                        var wurlAddr = erisimAdresi.Split('/').ToList();
                        if (erisimAdresi.Contains("//"))
                            erisimAdresi = wurlAddr[0] + "//" + wurlAddr.Skip(2).Take(1).First();
                        else
                            erisimAdresi = "http://" + wurlAddr.First();
                        mmmC.LogoPath = erisimAdresi + "/Content/assets/images/ytu_logo_tr.png";
                        var hcb = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", contentBilgi);
                        mmmC.Content = hcb;
                        mmmC.WebAdresi = mailBilgi.WebAdresi;
                        string htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);
                        var emailSend = MailManager.SendMail(mailBilgi.EnstituKod, "Talep İşleminiz Hk.", htmlMail, talep.EMail, null);

                        if (emailSend)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = mailBilgi.EnstituKod,
                                MesajID = null,
                                Konu = "Talep İşlemleri: " + talep.TalepTipAdi + "  (" + talep.AdSoyad + " [" + talep.TalepDurumAdi + "])",
                                Aciklama = "",
                                AciklamaHtml = htmlMail,
                                IslemYapanID = UserIdentity.Current.Id,
                                IslemTarihi = DateTime.Now,
                                IslemYapanIP = UserIdentity.Ip,
                                Gonderildi = true,
                                GonderilenMailKullanicilars = new List<GonderilenMailKullanicilar>()
                            };

                            kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = talep.EMail });
                            entities.GonderilenMaillers.Add(kModel);
                            entities.SaveChanges();
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Talep işlemi yapan öğrencilere toplu mail gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }

            return mmMessage;
        }
    }
}