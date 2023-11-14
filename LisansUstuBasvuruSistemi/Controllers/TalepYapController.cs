using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize]
    public class TalepYapController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            return Index(new FmTalep() { PageSize = 10 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmTalep model, string ekd)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var kullanici = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);

            var bbModel = new IndexPageInfoDto
            {
                Kullanici = kullanici
            };
            var talepSurecId = Management.GetAktifTalepSurecId(enstituKod);
            bbModel.SistemBasvuruyaAcik = talepSurecId.HasValue;
            bbModel.EnstituYetki = UserIdentity.Current.SeciliEnstituKodu.Contains(enstituKod) || UserIdentity.Current.SeciliEnstituKodu == enstituKod;
            bbModel.Enstitü = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod);
            if (bbModel.SistemBasvuruyaAcik)
            {
                var surec = _entities.TalepSurecleris.First(p => p.TalepSurecID == talepSurecId.Value);
                bbModel.DonemAdi = surec.BaslangicTarihi.ToString("yyyy-MM-dd HH:mm") + " / " +
                                   surec.BitisTarihi.ToString("yyyy-MM-dd HH:mm");
            }
            bbModel.YtuOgrencisi = kullanici.YtuOgrencisi;
            bbModel.KullaniciTipYetki = true;
            if (kullanici.KayitDonemID.HasValue == false && kullanici.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci && kullanici.KayitDonemID.HasValue == false)
            {
                var kullKayitB = KullanicilarBus.OgrenciBilgisiGuncelleObs(kullanici.KullaniciID);
                kullanici = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
            }
            if (kullanici.YtuOgrencisi)
            {

                var otb = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kullanici.OgrenimTipKod);
                bbModel.KayitDonemi = kullanici.KayitYilBaslangic + "/" + (kullanici.KayitYilBaslangic + 1) + " " +
                                      _entities.Donemlers.FirstOrDefault(p => p.DonemID == kullanici.KayitDonemID.Value)?.DonemAdi;
                bbModel.OgrenimDurumAdi = kullanici.OgrenimDurumlari.OgrenimDurumAdi;
                bbModel.OgrenimTipAdi = otb.OgrenimTipAdi;
                bbModel.AnabilimdaliAdi = kullanici.Programlar.AnabilimDallari.AnabilimDaliAdi;
                bbModel.ProgramAdi = kullanici.Programlar.ProgramAdi;
                bbModel.OgrenciNo = kullanici.OgrenciNo;

                if (kullanici.Programlar.AnabilimDallari.EnstituKod != enstituKod)
                {

                    var gelenEnstituKisaAd = EnstituBus.Enstitulers.First(p => p.EnstituKod==enstituKod).EnstituKisaAd.ToLower();
                    var gidilecekEnstituKisaAd = EnstituBus.Enstitulers.First(p => p.EnstituKod == kullanici.Programlar.AnabilimDallari.EnstituKod).EnstituKisaAd.ToLower();  
                    var urlStr = Url.Action("Index", "TalepYap")?.Replace(gelenEnstituKisaAd, gidilecekEnstituKisaAd);
                    return Redirect(urlStr);
                }
            }


            ViewBag.bModel = bbModel;

            #region data
            var q = from s in _entities.TalepGelenTaleplers
                    join ts in _entities.TalepSurecleris on s.TalepSurecID equals ts.TalepSurecID
                    join kul in _entities.Kullanicilars on s.KullaniciID equals kul.KullaniciID
                    join tt in _entities.TalepTipleris on s.TalepTipID equals tt.TalepTipID
                    join td in _entities.TalepDurumlaris on s.TalepDurumID equals td.TalepDurumID
                    join ags in _entities.TalepArGorStatuleris on s.TalepArGorStatuID equals ags.TalepArGorStatuID into defAgs
                    from Ags in defAgs.DefaultIfEmpty()
                    join ot in _entities.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod into defO
                    from Ot in defO.DefaultIfEmpty()
                    join otl in _entities.OgrenimTipleris on Ot.OgrenimTipID equals otl.OgrenimTipID into defOtl
                    from Otl in defOtl.DefaultIfEmpty()
                    join prl in _entities.Programlars on s.ProgramKod equals prl.ProgramKod into defprl
                    from Prl in defprl.DefaultIfEmpty()
                    join abl in _entities.AnabilimDallaris on new { AnabilimDaliID = (Prl != null ? Prl.AnabilimDaliID : (int?)null) } equals new { AnabilimDaliID = (int?)abl.AnabilimDaliID } into defabl
                    from Abl in defabl.DefaultIfEmpty()
                    where s.KullaniciID == kullanici.KullaniciID
                    select new
                    {
                        s.TalepGelenTalepID,
                        s.TalepSurecID,
                        tt.IsBelgeYuklemeVar,
                        tt.TalepTipAciklama,
                        s.IsTaahut,
                        tt.TaahhutAciklamasi,
                        s.KullaniciID,
                        kul.ResimAdi,
                        YtuOgrencisi = s.ProgramKod != null,
                        s.TalepTipID,
                        tt.TalepTipAdi,
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
                        s.IsTezOnerisiYapildi,
                        s.DoktoraTezOneriTarihi,
                        Otl.OgrenimTipAdi,
                        Abl.AnabilimDaliAdi,
                        Prl.ProgramAdi,
                        s.IsYtuArGor,
                        s.TalepArGorStatuID,
                        Ags.StatuAdi,
                        s.IsDersYukuTamamlandi,
                        s.IsHarcBorcuVar,
                        s.IslemTarihi,
                        s.IslemYapanID,
                        s.IslemYapanIP,
                        s.TalepGelenTalepBelgeleris
                    }; 
            q = model.Sort.IsNullOrWhiteSpace() == false ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.TalepTarihi); 
            model.RowCount = q.Count(); 
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount
            };
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList().Select(item => new FrTalep()
            {
                TalepGelenTalepID = item.TalepGelenTalepID,
                IsbelgeYuklemesiVar = item.IsBelgeYuklemeVar,
                YtuOgrencisi = item.YtuOgrencisi,
                TalepDurumID = item.TalepDurumID,
                TalepTipAciklama = item.TalepTipID == TalepTipiEnum.LisansustuSureUzatmaTalebi ?
                                (item.OgrenimTipKod.IsDoktora() ?
                                    "Bu talep tipini seçecek öğrenciler, doktora tez önerisinden başarılı olmuş ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir. "
                                    :
                                    "Bu talep tipini seçecek öğrenciler Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşamasını tamamlamış ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir.")
                                 :
                                 item.TalepTipID == TalepTipiEnum.Covid19KayitDondurmaTalebi ?
                                 (item.OgrenimTipKod.IsDoktora() ?
                                    "Bu talep tipini seçecek olan öğrencilerimizden: doktora tez önerisinden başarılı olunmuş ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir. Aksi takdirde talebiniz kabul edilmeyecektir."
                                    :
                                    "Bu talep tipini seçecek olan öğrencilerimizden: YTÜ Lisansüstü Eğitim ve Öğretim Yönetmeliği Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşaması tamamlanmış ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir Aksi takdirde talebiniz kabul edilmeyecektir."
                                    ) : item.TalepTipAciklama,
                TaahhutAciklama = item.TaahhutAciklamasi,
                IsTaahut = item.IsTaahut,
                TalepDurumAciklamasi = item.TalepDurumAciklamasi,
                TalepSurecID = item.TalepSurecID,
                ClassName = item.ClassName,
                Color = item.Color,
                TalepTipID = item.TalepTipID,
                TalepTipAdi = item.TalepTipAdi,
                DurumListeAdi = item.TalepDurumAdi,
                KullaniciID = item.KullaniciID,
                ResimAdi = item.ResimAdi,
                TalepTarihi = item.TalepTarihi,
                AdSoyad = item.AdSoyad,
                OgrenciNo = item.OgrenciNo,
                OgrenimTipKod = item.OgrenimTipKod,
                OgrenimTipAdi = item.OgrenimTipAdi,
                IsTezOnerisiYapildi = item.IsTezOnerisiYapildi,
                DoktoraTezOneriTarihi = item.DoktoraTezOneriTarihi,
                AnabiliDaliAdi = item.AnabilimDaliAdi,
                ProgramAdi = item.ProgramAdi,
                IsYtuArGor = item.IsYtuArGor ?? false,
                TalepArGorStatuID = item.TalepArGorStatuID,
                StatuAdi = item.StatuAdi,
                IsDersYukuTamamlandi = item.IsDersYukuTamamlandi ?? false,
                IsHarcBorcuVar = item.IsHarcBorcuVar ?? false,
                IslemTarihi = item.IslemTarihi,
                IslemYapanID = item.IslemYapanID,
                IslemYapanIP = item.IslemYapanIP,
                TalepGelenTalepBelgeleris = item.TalepGelenTalepBelgeleris

            }).AsEnumerable();

            ViewBag.IndexModel = indexModel;

            #endregion 
            return View(model);
        }

        public ActionResult TalepYap(int? talepGelenTalepId, string ekd)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var talep = new TalepGelenTalepler();
            var mmMessage = new MmMessage();
            var kayitYetki = RoleNames.GelenTalepKayit.InRoleCurrent();

            if (talepGelenTalepId.HasValue)
            {
                talep = _entities.TalepGelenTaleplers.First(p => p.TalepGelenTalepID == talepGelenTalepId.Value && p.KullaniciID == (kayitYetki ? p.KullaniciID : UserIdentity.Current.Id));
            }
            else
            {
                talep.TalepSurecID = Management.GetAktifTalepSurecId(enstituKod) ?? 0;
                var kul = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
                talep.KullaniciID = kul.KullaniciID;
                talep.AdSoyad = kul.Ad + " " + kul.Soyad;
                if (kul.YtuOgrencisi)
                {
                    talep.OgrenciNo = kul.OgrenciNo;
                    var ot = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kul.OgrenimTipKod);
                    talep.OgrenimTipID = ot.OgrenimTipID;
                    talep.OgrenimTipKod = ot.OgrenimTipKod;
                    talep.ProgramKod = kul.ProgramKod;
                }
            }

            ViewBag.MmMessage = mmMessage;
            ViewBag.TalepArGorStatuID = new SelectList(TaleplerBus.GetCmbArGorStatuleri(true), "Value", "Caption", talep.TalepArGorStatuID);
            ViewBag.TalepTipID = new SelectList(TaleplerBus.GetCmbTalepTipleriSurec(talep.TalepSurecID, talep.TalepTipID, true), "Value", "Caption", talep.TalepTipID);


            return View(talep);
        }

        [HttpPost]
        public ActionResult TalepYap(TalepGelenTalepler kModel, HttpPostedFileBase dosya, string dosyaAdi, HttpPostedFileBase dosyaDanismanOnay, string dosyaAdiDanismanOnay, string ekd)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var mmMessage = new MmMessage
            {
                Title = ""
            };
            var kayitYetki = RoleNames.GelenTalepKayit.InRoleCurrent();
            #region kontrol
            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.KullaniciID);

            if (kModel.TalepGelenTalepID <= 0)
            {
                kModel.TalepSurecID = Management.GetAktifTalepSurecId(enstituKod) ?? 0;

                if (kModel.TalepSurecID <= 0)
                {
                    mmMessage.Messages.Add("Sistem talep işlemlerine kapalıdır.");
                }
                else kModel.TalepSurecID = kModel.TalepSurecID;
            }
            else
            {
                var talepSurec = TaleplerBus.GetTalepSurec(kModel.TalepSurecID);
                if (!kayitYetki && !talepSurec.AktifSurec)
                {
                    mmMessage.Messages.Add("Sistem talep işlemlerine kapalıdır.");
                }
            }

            if (kModel.TalepGelenTalepID > 0 && !kayitYetki)
            {
                var talep = _entities.TalepGelenTaleplers.Find(kModel.TalepGelenTalepID);
                if (talep.TalepDurumID > TalepDurumuEnum.TalepYapildi)
                {
                    mmMessage.Messages.Add("Enstitü tarafından işlem gören talepler düzeltilemez.");
                }
            }
            if (mmMessage.Messages.Count == 0)
                if (kModel.TalepTipID <= 0)
                {
                    mmMessage.Messages.Add("Talep Tipi seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TalepTipID" });
                }
                else
                {
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TalepTipID" });
                    var talepTipi = _entities.TalepTipleris.First(p => p.TalepTipID == kModel.TalepTipID);
                    if (talepTipi.IsAktifOgrenciKontroluYapilsin)
                    {
                        if (!kul.YtuOgrencisi)
                        {
                            KullanicilarBus.OgrenciBilgisiGuncelleObs(kul.KullaniciID);
                            mmMessage.Messages.Add(talepTipi.TalepTipAdi + " başvurusu için Aktif YTÜ öğrencisi olunması gerekmektedir. Kullanıcı hesap bilgilerinizi düzeltip YTÜ öğrencisi olduğunuzu belirtiniz.");
                        }
                    }

                }


            if (kul.YtuOgrencisi)
            {
                if (kModel.TalepTipID == TalepTipiEnum.LisansustuSureUzatmaTalebi)
                {
                    if (kModel.IsHarcBorcuVar == true)
                    {
                        mmMessage.Messages.Add("Talep işleminin yapılabilmesi ödenmeyen harç borcunuzun bulunmaması gerekmektedir.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsHarcBorcuVar" });

                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "IsHarcBorcuVar" });
                }
                if (mmMessage.Messages.Count == 0)
                {
                    if (kModel.IsYtuArGor == true)
                    {
                        if (!kModel.TalepArGorStatuID.HasValue)
                        {
                            mmMessage.Messages.Add("Araştırma Görevlisi Statüsü seçiniz.");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TalepArGorStatuID" });

                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TalepArGorStatuID" });
                    }

                    if (kModel.OgrenimTipKod.IsDoktora())
                    {
                        if (kModel.TalepTipID == TalepTipiEnum.LisansustuSureUzatmaTalebi || kModel.TalepTipID == TalepTipiEnum.Covid19KayitDondurmaTalebi)
                        {
                            if (kModel.TalepTipID == TalepTipiEnum.LisansustuSureUzatmaTalebi)
                            {
                                if (kModel.IsTezOnerisiYapildi != true)
                                {
                                    mmMessage.Messages.Add("Başvuru Yapılabilmesi İçin Tez Önerisinin Verilmiş Olması Gerekmektedir.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsTezOnerisiYapildi" });
                                }
                                else
                                {
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "IsTezOnerisiYapildi" });
                                    if (!kModel.DoktoraTezOneriTarihi.HasValue)
                                    {
                                        mmMessage.Messages.Add("Doktora Tez Öneri Tarihi bilgisini giriniz.");
                                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "DoktoraTezOneriTarihi" });
                                    }
                                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "DoktoraTezOneriTarihi" });
                                    if (dosyaDanismanOnay == null && dosyaAdiDanismanOnay.IsNullOrWhiteSpace())
                                    {
                                        mmMessage.Messages.Add("Danışman İmzalı Onay Belgesi Yükleyiniz.");
                                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "DosyaDanismanOnay" });
                                    }
                                    else if (dosyaDanismanOnay != null)
                                    {
                                        if (dosyaDanismanOnay.FileName.Split('.').Last().ToLower() != "pdf")
                                        {
                                            mmMessage.Messages.Add("Yükleyeceğiniz te öneri belgesi pdf türünde olmalıdır.");
                                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "DosyaDanismanOnay" });
                                        }
                                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "DosyaDanismanOnay" });
                                    }
                                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "DosyaDanismanOnay" });
                                }


                            }
                            else
                            {
                                if (kModel.IsTezOnerisiYapildi == true)
                                {
                                    if (!kModel.DoktoraTezOneriTarihi.HasValue)
                                    {
                                        mmMessage.Messages.Add("Doktora Tez Öneri Tarihi bilgisini giriniz.");
                                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "DoktoraTezOneriTarihi" });

                                    }
                                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "DoktoraTezOneriTarihi" });
                                    if (dosyaDanismanOnay == null && dosyaAdiDanismanOnay.IsNullOrWhiteSpace())
                                    {
                                        mmMessage.Messages.Add("Danışman İmzalı Onay Belgesi Yükleyiniz.");
                                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "DosyaDanismanOnay" });
                                    }
                                    else if (dosyaDanismanOnay != null)
                                    {
                                        if (dosyaDanismanOnay.FileName.Split('.').Last().ToLower() != "pdf")
                                        {
                                            mmMessage.Messages.Add("Yükleyeceğiniz te öneri belgesi pdf türünde olmalıdır.");
                                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "DosyaDanismanOnay" });
                                        }
                                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "DosyaDanismanOnay" });
                                    }
                                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "DosyaDanismanOnay" });
                                }
                            }
                        }

                    }
                    else
                    {
                        if (kModel.TalepTipID == TalepTipiEnum.LisansustuSureUzatmaTalebi || kModel.TalepTipID == TalepTipiEnum.Covid19KayitDondurmaTalebi)
                        {
                            if (kModel.TalepTipID == TalepTipiEnum.LisansustuSureUzatmaTalebi)
                            {
                                if (kModel.IsDersYukuTamamlandi != true)
                                {
                                    mmMessage.Messages.Add("Talep işleminin yapılabilmesi için ders yükünün tamamlanması gerekmektedir.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsDersYukuTamamlandi" });
                                }
                                else
                                {
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "DosyaDanismanOnay" });
                                    if (kModel.IsDersYukuTamamlandi == true && dosyaDanismanOnay == null && dosyaAdiDanismanOnay.IsNullOrWhiteSpace())
                                    {
                                        mmMessage.Messages.Add("Danışman İmzalı Onay Belgesi Yükleyiniz.");
                                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "DosyaDanismanOnay" });
                                    }
                                    else if (dosyaDanismanOnay != null)
                                    {
                                        if (dosyaDanismanOnay.FileName.Split('.').Last().ToLower() != "pdf")
                                        {
                                            mmMessage.Messages.Add("Yükleyeceğiniz ders yükü belgesi pdf türünde olmalıdır.");
                                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "DosyaDanismanOnay" });
                                        }
                                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "DosyaDanismanOnay" });
                                    }
                                }
                            }
                            else
                            {
                                if (kModel.IsDersYukuTamamlandi == true)
                                {
                                    if (dosyaDanismanOnay == null && dosyaAdiDanismanOnay.IsNullOrWhiteSpace())
                                    {
                                        mmMessage.Messages.Add("Danışman İmzalı Onay Belgesi Yükleyiniz.");
                                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "DosyaDanismanOnay" });
                                    }
                                    else if (dosyaDanismanOnay != null)
                                    {
                                        if (dosyaDanismanOnay.FileName.Split('.').Last().ToLower() != "pdf")
                                        {
                                            mmMessage.Messages.Add("Yükleyeceğiniz ders yükü belgesi pdf türünde olmalıdır.");
                                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "DosyaDanismanOnay" });
                                        }
                                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "DosyaDanismanOnay" });
                                    }
                                }

                            }
                        }


                    }
                }

            }




            if (mmMessage.Messages.Count == 0)
            {
                if (kul.TalepGelenTaleplers.Any(a =>
                    a.TalepSurecID == kModel.TalepSurecID && (a.TalepTipID == kModel.TalepTipID || a.TalepTipleri.BirlikteBasvurulamayacakTalepTipID == kModel.TalepTipID) && a.TalepGelenTalepID != kModel.TalepGelenTalepID))
                {
                    mmMessage.Messages.Add("Bu talep alım sürecinde zaten bir talebiniz bulunmaktadır. Bu talep tipinde yeni talep başvurusu yapamazsınız.");
                }
            }
            var talepTip = new TalepSureciTalepTipleri();
            if (mmMessage.Messages.Count == 0)
            {


                talepTip = _entities.TalepSureciTalepTipleris.First(p => p.TalepSurecID == kModel.TalepSurecID && p.TalepTipID == kModel.TalepTipID);
                if (talepTip.IsBelgeYuklemeVar)
                {
                    if (dosya == null && dosyaAdi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("Belge seçiniz.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Dosya" });

                    }
                    else if (dosya != null)
                    {
                        if (dosya.FileName.Split('.').Last().ToLower() != "pdf")
                        {
                            mmMessage.Messages.Add("Yükleyeceğiniz belge pdf türünde olmalıdır.");
                            mmMessage.MessagesDialog.Add(new MrMessage
                            {
                                MessageType = MsgTypeEnum.Warning,
                                PropertyName = "Dosya"
                            });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "Dosya" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "Dosya" });

                }

                if (talepTip.IsTaahhutIsteniyor)
                {
                    if (kModel.IsTaahut != true)
                    {

                        mmMessage.Messages.Add("Talep işleminin yapılabilmesi taahhüt onayı verilmesi gerekmektedir.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsTaahut" });

                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "IsTaahut" });

                }
                else kModel.IsTaahut = null;
            }
            #endregion

            if (mmMessage.Messages.Count == 0)
            {
                var yeniKayit = kModel.TalepGelenTalepID <= 0;

                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.KullaniciID = kul.KullaniciID;
                kModel.TalepTipID = kModel.TalepTipID;
                kModel.AdSoyad = kul.Ad + " " + kul.Soyad;

                if (kModel.IsTezOnerisiYapildi != true && kModel.IsDersYukuTamamlandi != true)
                {
                    dosyaDanismanOnay = null;
                }
                if (kModel.IsTezOnerisiYapildi != true) kModel.DoktoraTezOneriTarihi = null;
                if (kul.YtuOgrencisi)
                {
                    kModel.OgrenciNo = kul.OgrenciNo;
                    var ot = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kul.OgrenimTipKod);
                    kModel.OgrenimTipID = ot.OgrenimTipID;
                    kModel.OgrenimTipKod = ot.OgrenimTipKod;
                    kModel.ProgramKod = kul.ProgramKod;
                }
                kModel.DoktoraTezOneriTarihi = kModel.OgrenimTipKod.IsDoktora() ? kModel.DoktoraTezOneriTarihi : null;

                TalepGelenTalepler talep;
                if (yeniKayit)
                {
                    kModel.TalepTarihi = DateTime.Now;
                    kModel.TalepDurumID = TalepDurumuEnum.TalepYapildi;//talep edildi
                    kModel.TalepTarihi = DateTime.Now;


                    talep = _entities.TalepGelenTaleplers.Add(kModel);
                    _entities.SaveChanges();
                    BilgiMaili(new List<int> { talep.TalepGelenTalepID }, enstituKod);

                }
                else
                {
                    talep = _entities.TalepGelenTaleplers.First(p => p.TalepGelenTalepID == kModel.TalepGelenTalepID && p.KullaniciID == (kayitYetki ? p.KullaniciID : UserIdentity.Current.Id));
                    talep.TalepTipID = kModel.TalepTipID;
                    talep.AdSoyad = kModel.AdSoyad;
                    talep.OgrenciNo = kModel.OgrenciNo;
                    talep.OgrenimTipID = kModel.OgrenimTipID;
                    talep.OgrenimTipKod = kModel.OgrenimTipKod;
                    talep.IsTezOnerisiYapildi = kModel.IsTezOnerisiYapildi;
                    talep.DoktoraTezOneriTarihi = kModel.DoktoraTezOneriTarihi;
                    talep.IsDersYukuTamamlandi = kModel.IsDersYukuTamamlandi;
                    talep.ProgramKod = kModel.ProgramKod;
                    talep.IsYtuArGor = kModel.IsYtuArGor;
                    talep.IsHarcBorcuVar = kModel.IsHarcBorcuVar;
                    talep.IsDersYukuTamamlandi = kModel.IsDersYukuTamamlandi;
                    talep.IsYtuArGor = kModel.IsYtuArGor;
                    talep.TalepArGorStatuID = kModel.TalepArGorStatuID;
                    talep.IsTaahut = kModel.IsTaahut;
                    talep.IslemTarihi = DateTime.Now;
                    talep.IslemYapanIP = kModel.IslemYapanIP;
                    talep.IslemYapanID = UserIdentity.Current.Id;
                    _entities.SaveChanges();
                }
                if (talepTip.IsBelgeYuklemeVar && dosya != null)
                {
                    string dosyaYolu = "/TalepDosyalari/TT_" + kModel.TalepTipID + "_" + talep.TalepGelenTalepID + "_" + dosya.FileName.ToFileNameAddGuid();
                    var sfilename = Server.MapPath("~" + dosyaYolu);
                    dosya.SaveAs(sfilename);

                    _entities.TalepGelenTalepBelgeleris.RemoveRange(talep.TalepGelenTalepBelgeleris).Where(p => p.IsDanismanOnayDosyasi == false);
                    foreach (var belge in talep.TalepGelenTalepBelgeleris)
                    {
                        var path = Server.MapPath("~" + belge.DosyaYolu);
                        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                    }


                    talep.TalepGelenTalepBelgeleris.Add(new TalepGelenTalepBelgeleri()
                    {

                        DosyaAdi = dosya.FileName.GetFileName().ReplaceSpecialCharacter(),
                        DosyaYolu = dosyaYolu

                    });
                    _entities.SaveChanges();
                }

                if ((kModel.TalepTipID == TalepTipiEnum.LisansustuSureUzatmaTalebi || kModel.TalepTipID == TalepTipiEnum.Covid19KayitDondurmaTalebi))
                {
                    var dosyalar = new Dictionary<TalepGelenTalepBelgeleri, HttpPostedFileBase>();
                    if (kModel.TalepTipID == TalepTipiEnum.Covid19KayitDondurmaTalebi && dosyaDanismanOnay != null)
                    {
                        dosyalar.Add(new TalepGelenTalepBelgeleri
                        {
                            IsDanismanOnayDosyasi = true,
                            DosyaAdi = dosyaDanismanOnay.FileName.ReplaceSpecialCharacter(),
                            DosyaYolu = "/TalepDosyalari/TT_" + kModel.TalepTipID + "_" + talep.TalepGelenTalepID + "_" + dosyaDanismanOnay.FileName.ToFileNameAddGuid()
                        }, dosyaDanismanOnay);
                    }

                    foreach (var itemD in dosyalar)
                    {
                        var sfilename = Server.MapPath("~" + itemD.Key.DosyaYolu);
                        itemD.Value.SaveAs(sfilename);
                        var qSilinecekler = talep.TalepGelenTalepBelgeleris.AsQueryable();
                        if (itemD.Key.IsDanismanOnayDosyasi == true) qSilinecekler = qSilinecekler.Where(p => p.IsDanismanOnayDosyasi);
                        var silinecekler = qSilinecekler.ToList();
                        foreach (var belge in silinecekler)
                        {
                            var path = Server.MapPath("~" + belge.DosyaYolu);
                            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                        }
                        _entities.TalepGelenTalepBelgeleris.RemoveRange(silinecekler);


                        talep.TalepGelenTalepBelgeleris.Add(new TalepGelenTalepBelgeleri()
                        {

                            DosyaAdi = itemD.Key.DosyaAdi,
                            DosyaYolu = itemD.Key.DosyaYolu,
                            IsDanismanOnayDosyasi = itemD.Key.IsDanismanOnayDosyasi,

                        });
                        _entities.SaveChanges();
                    }

                }
                LogIslemleri.LogEkle("TalepGelenTalepler", yeniKayit ? LogCrudType.Insert : LogCrudType.Update, talep.ToJson());
                mmMessage.IsSuccess = true;
                mmMessage.MessageType = MsgTypeEnum.Success;
                return mmMessage.ToJsonResult();
            }
            else
            {
                mmMessage.IsSuccess = false;
                mmMessage.MessageType = MsgTypeEnum.Warning;
                return mmMessage.ToJsonResult();
            }
        }

        void BilgiMaili(List<int> talepGelenTalepIDs, string enstituKod, string aciklama = "")
        {


            var taleps = (from s in _entities.TalepGelenTaleplers
                          join ts in _entities.TalepSurecleris on s.TalepSurecID equals ts.TalepSurecID
                          join kul in _entities.Kullanicilars on s.KullaniciID equals kul.KullaniciID
                          join tt in _entities.TalepTipleris on s.TalepTipID equals tt.TalepTipID
                          join td in _entities.TalepDurumlaris on s.TalepDurumID equals td.TalepDurumID
                          join ags in _entities.TalepArGorStatuleris on s.TalepArGorStatuID equals ags.TalepArGorStatuID into defAgs
                          from Ags in defAgs.DefaultIfEmpty()
                          join ot in _entities.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod into defO
                          from Ot in defO.DefaultIfEmpty()
                          join otl in _entities.OgrenimTipleris on Ot.OgrenimTipID equals otl.OgrenimTipID into defOtl
                          from Otl in defOtl.DefaultIfEmpty()
                          join prl in _entities.Programlars on s.ProgramKod equals prl.ProgramKod into defprl
                          from Prl in defprl.DefaultIfEmpty()
                          join abl in _entities.AnabilimDallaris on new { AnabilimDaliID = (Prl != null ? Prl.AnabilimDaliID : (int?)null) } equals new { AnabilimDaliID = (int?)abl.AnabilimDaliID } into defabl
                          from Abl in defabl.DefaultIfEmpty()
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
                              Otl.OgrenimTipAdi,
                              Abl.AnabilimDaliAdi,
                              Prl.ProgramAdi,
                              s.IsYtuArGor,
                              s.TalepArGorStatuID,
                              Ags.StatuAdi,
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
                string talepTipAciklama = "";
                if (talep.TalepTipID == TalepTipiEnum.LisansustuSureUzatmaTalebi)
                {
                    talepTipAciklama = talep.OgrenimTipKod.IsDoktora() ?
                        "Bu talep tipini seçecek öğrenciler, doktora tez önerisinden başarılı olmuş ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir. "
                        :
                        "Bu talep tipini seçecek öğrenciler Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşamasını tamamlamış ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir.";
                }
                else if (talep.TalepTipID == TalepTipiEnum.Covid19KayitDondurmaTalebi)
                {
                    talepTipAciklama = talep.OgrenimTipKod.IsDoktora() ?
                        "Bu talep tipini seçecek olan öğrencilerimizden: doktora tez önerisinden başarılı olunmuş ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir. Aksi takdirde talebiniz kabul edilmeyecektir."
                        :
                        "Bu talep tipini seçecek olan öğrencilerimizden: YTÜ Lisansüstü Eğitim ve Öğretim Yönetmeliği Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşaması tamamlanmış ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir Aksi takdirde talebiniz kabul edilmeyecektir.";
                }
                else talepTipAciklama = talep.TalepTipAciklama;
                if (!talepTipAciklama.IsNullOrWhiteSpace()) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Talep Tipi Açıklaması", Aciklama = talepTipAciklama });
                htmlBigliRow.Add(new MailTableRowDto { Baslik = "Talep Tarihi", Aciklama = talep.TalepTarihi.ToFormatDateAndTime() });
                htmlBigliRow.Add(new MailTableRowDto { Baslik = "Talep Durumu", Aciklama = talep.TalepDurumAdi });
                if (talep.TalepDurumID == TalepDurumuEnum.Rededildi) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Red Açıklaması", Aciklama = talep.TalepDurumAciklamasi });
                if (!aciklama.IsNullOrWhiteSpace()) htmlBigliRow.Add(new MailTableRowDto { Baslik = "Not", Aciklama = aciklama });

                contentBilgi.GrupBasligi = "'" + talep.TalepTipAdi + "' talebiniz " + talep.TalepDurumAdi;
                contentBilgi.Detaylar = htmlBigliRow;

                var mmmC = new MailMainContentDto();
                var enstituAdi = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod).EnstituAd;
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
                    _entities.GonderilenMaillers.Add(kModel);
                    _entities.SaveChanges();
                }
            }

        }

        [Authorize(Roles = RoleNames.GelenTalepKayit)]
        public ActionResult Istenenkaydet(int id, int talepDurumId, string talepDurumAciklamasi)
        {


            var talep = _entities.TalepGelenTaleplers.First(p => p.TalepGelenTalepID == id);
            var oldTdid = talep.TalepDurumID;

            talep.TalepDurumID = talepDurumId;
            if (talep.TalepDurumID == TalepDurumuEnum.Rededildi) talep.TalepDurumAciklamasi = talepDurumAciklamasi;
            talep.IslemTarihi = DateTime.Now;
            talep.IslemYapanID = UserIdentity.Current.Id;
            talep.IslemYapanIP = UserIdentity.Ip;
            _entities.SaveChanges();

            if (talepDurumId != TalepDurumuEnum.TalepYapildi && talepDurumId != oldTdid)
            {
                BilgiMaili(new List<int> { talep.TalepGelenTalepID }, talep.TalepSurecleri.EnstituKod);
            }
            var qbDrm = talep.TalepDurumlari;

            return new
            {
                qbDrm.TalepDurumAdi,
                qbDrm.ClassName,
                qbDrm.Color
            }.ToJsonResult();
        }

        [HttpPost]
        public ActionResult GelenTalepOnay(List<int> kTalepGelenTalepIDs, string onayAciklamasi, string ekd)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            var mmMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                IsSuccess = true,
                Title = "Toplu Talep Onaylama İşlemi"
            };
            if (UserIdentity.Current.IsAdmin)
            {
                try
                {
                    var talepler = _entities.TalepGelenTaleplers.Where(p => p.TalepDurumID == TalepDurumuEnum.TalepYapildi && kTalepGelenTalepIDs.Contains(p.TalepGelenTalepID)).ToList();

                    foreach (var item in talepler)
                    {
                        item.TalepDurumID = TalepDurumuEnum.Onaylandi;
                        item.IslemTarihi = DateTime.Now;
                        item.IslemYapanID = UserIdentity.Current.Id;
                        item.IslemYapanIP = UserIdentity.Ip;
                    }
                    _entities.SaveChanges();
                    mmMessage.Messages.Add(talepler.Count + " Talep onaylandı");
                    try
                    {
                        BilgiMaili(kTalepGelenTalepIDs, enstituKod, onayAciklamasi);
                        mmMessage.Messages.Add("Onaylanan " + talepler.Count + " Talep'e mail gönderildi.");
                    }
                    catch (Exception ex)
                    {
                        mmMessage.Messages.Add("Onaylanan Taleplere mail gönderilirken bir hata oluştu.");
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Onaylanan Taleplere mail gönderilirken bir hata oluştu! <br/><br/> Hata: " + ex.ToExceptionMessage(), "TalepYap/GelenTalepOnay<br/><br/>" + ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                    }
                    LogIslemleri.LogEkle("TalepGelenTalepler", LogCrudType.Update, talepler.ToJson());

                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Toplu Talep Onay işlemi yapılırken bir hata oluştu! Hata: " + ex.ToExceptionMessage());
                    SistemBilgilendirmeBus.SistemBilgisiKaydet("Toplu Talep Onay işlemi yapılırken bir hata oluştu! <br/><br/> Hata: " + ex.ToExceptionMessage(), "TalepYap/GelenTalepOnay<br/><br/>" + ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                }
            }
            else
            {
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Bu işlemi yapmaya yetkili değilsiniz.");

            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new
            {
                mmMessage.IsSuccess,
                Messages = strView
            }, "application/json", JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetTalepTipBilgi(int? talepTipId, int talepSurecId, int kullaniciId)
        {

            string aciklama = null;
            var kul = _entities.Kullanicilars.First(p => p.KullaniciID == kullaniciId);
            if (talepTipId == TalepTipiEnum.LisansustuSureUzatmaTalebi)
            {
                aciklama = kul.OgrenimTipKod.IsDoktora() ?
                    "Bu talep tipini seçecek öğrenciler, doktora tez önerisinden başarılı olmuş ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir. "
                    :
                    "Bu talep tipini seçecek öğrenciler Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşamasını tamamlamış ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir.";
            }
            else if (talepTipId == TalepTipiEnum.Covid19KayitDondurmaTalebi)
            {
                aciklama = kul.OgrenimTipKod.IsDoktora() ?
                    "Bu talep tipini seçecek olan öğrencilerimizden: doktora tez önerisinden başarılı olunmuş ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir. Aksi takdirde talebiniz kabul edilmeyecektir."
                    :
                    "Bu talep tipini seçecek olan öğrencilerimizden: YTÜ Lisansüstü Eğitim ve Öğretim Yönetmeliği Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşaması tamamlanmış ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir Aksi takdirde talebiniz kabul edilmeyecektir.";
            }

            var isDoktora = kul.OgrenimTipKod.IsDoktora();
            var talepTip = _entities.TalepSureciTalepTipleris.Where(p => p.TalepSurecID == talepSurecId && p.TalepTipID == talepTipId).Select(s => new
            {
                s.IsBelgeYuklemeVar,
                IsDoktora = isDoktora,
                TalepTipID = talepTipId,
                s.IsTaahhutIsteniyor,
                s.TalepTipleri.TaahhutAciklamasi,
                TalepTipAciklama = aciklama ?? s.TalepTipleri.TalepTipAciklama
            }).First();
            var jResult = talepTip.ToJsonResult();

            return jResult;
        }


        [Authorize]
        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = true
            };

            bool duzenleYetki = RoleNames.GelenTalepSil.InRoleCurrent();
            var talep = _entities.TalepGelenTaleplers.First(p => p.TalepGelenTalepID == id && p.KullaniciID == (duzenleYetki ? p.KullaniciID : UserIdentity.Current.Id));
           
            if (duzenleYetki == false)
            {
                if (Management.GetAktifTalepSurecId(talep.TalepSurecleri.EnstituKod, talep.TalepSurecID).HasValue)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Title = "Uyarı";
                    mmMessage.Messages.Add("Süreç tamamlandıktan sonra başvurunuzu silemezsiniz!");
                }
                if (UserIdentity.Current.Id != talep.KullaniciID)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Title = "Uyarı";
                    mmMessage.Messages.Add("Başka bir kullanıcıya ait talep işlemini silemezsiniz!");

                }
                else if (talep.TalepDurumID == TalepDurumuEnum.Onaylandi || talep.TalepDurumID == TalepDurumuEnum.Rededildi)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Title = "Hata";
                    var bDurumAdi = talep.TalepDurumlari.TalepDurumAdi;
                    mmMessage.Messages.Add("Talep durumu '" + bDurumAdi + "' olan talebi silemezsiniz.");
                }
              
            }
            if (mmMessage.IsSuccess)
            {
                try
                {
                    var talepDosyalar = talep.TalepGelenTalepBelgeleris.ToList();
                    _entities.TalepGelenTaleplers.Remove(talep);
                    _entities.TalepGelenTalepBelgeleris.RemoveRange(talepDosyalar);
                    _entities.SaveChanges();
                    foreach (var dosya in talepDosyalar)
                    {
                        if (System.IO.File.Exists(dosya.DosyaYolu))
                        {
                            try
                            { 
                                System.IO.File.Delete(dosya.DosyaYolu);
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                        }
                    }
                    LogIslemleri.LogEkle("TalepGelenTalepler", LogCrudType.Delete, talep.ToJson());
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Talep silinirken bir hata oluştu! Hata: " + ex.ToExceptionMessage());
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet("Talep silinirken bir hata oluştu! TalepGelenTalepID=" + id, "TalepYap /Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogTipiEnum.OnemsizHata);
                }
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}