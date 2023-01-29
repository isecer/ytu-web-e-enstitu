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

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize]
    public class TalepYapController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {
            return Index(new fmTalep() { PageSize = 10 }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmTalep model, string EKD)
        {
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var Kul = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();

            var bbModel = new BasvuruBilgiModel();
            bbModel.Kullanici = Kul;
            var TalepSurecID = Management.getAktifTalepSurecID(_EnstituKod);
            bbModel.SistemBasvuruyaAcik = TalepSurecID.HasValue;
            bbModel.EnstituYetki = UserIdentity.Current.SeciliEnstituKodu.Contains(_EnstituKod) || UserIdentity.Current.SeciliEnstituKodu == _EnstituKod;
            bbModel.Enstitü = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod ).First();
            if (bbModel.SistemBasvuruyaAcik)
            {
                var Surec = db.TalepSurecleris.Where(p => p.TalepSurecID == TalepSurecID.Value).FirstOrDefault();
                bbModel.DonemAdi = Surec.BaslangicTarihi.ToString("yyyy-MM-dd HH:mm") + " / " +
                                   Surec.BitisTarihi.ToString("yyyy-MM-dd HH:mm");
            }
            bbModel.YtuOgrencisi = Kul.YtuOgrencisi;
            bbModel.KullaniciTipYetki = true;
            if (Kul.KayitDonemID.HasValue == false && Kul.OgrenimDurumID == OgrenimDurum.HalenOğrenci && Kul.KayitDonemID.HasValue == false)
            {
                var kullKayitB = Management.KullaniciKayitBilgisiGuncelle(Kul.KullaniciID);
                Kul = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();
            }
            if (Kul.YtuOgrencisi)
            {

                var otb = db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod  && p.OgrenimTipKod == Kul.OgrenimTipKod).First();
                bbModel.KayitDonemi = Kul.KayitYilBaslangic + "/" + (Kul.KayitYilBaslangic + 1) + " " + db.Donemlers.Where(p => p.DonemID == Kul.KayitDonemID.Value ).First().DonemAdi + " , " + Kul.KayitTarihi.ToString("dd.MM.yyyy");
                bbModel.OgrenimDurumAdi = Kul.OgrenimDurumlari.OgrenimDurumAdi;
                bbModel.OgrenimTipAdi = otb.OgrenimTipAdi;
                bbModel.AnabilimdaliAdi = Kul.Programlar.AnabilimDallari.AnabilimDaliAdi;
                bbModel.ProgramAdi = Kul.Programlar.ProgramAdi;
                bbModel.OgrenciNo = Kul.OgrenciNo;

                if (Kul.Programlar.AnabilimDallari.EnstituKod != _EnstituKod)
                {
                    var OncekiEnstitu = _EnstituKod == EnstituKodlari.FenBilimleri ? "fbe" : "sbe";
                    var GidilecekEnstitu = _EnstituKod == EnstituKodlari.FenBilimleri ? "sbe" : "fbe";


                    var UrlStr = Url.Action("Index", "TalepYap").Replace(OncekiEnstitu, GidilecekEnstitu);
                    return Redirect(UrlStr);
                }
            }

            
            ViewBag.bModel = bbModel;

            #region data
            var q = from s in db.TalepGelenTaleplers
                    join ts in db.TalepSurecleris on s.TalepSurecID equals ts.TalepSurecID
                    join kul in db.Kullanicilars on s.KullaniciID equals kul.KullaniciID
                    join tt in db.TalepTipleris on s.TalepTipID equals tt.TalepTipID
                    join td in db.TalepDurumlaris on s.TalepDurumID equals td.TalepDurumID
                    join ags in db.TalepArGorStatuleris on s.TalepArGorStatuID equals ags.TalepArGorStatuID into defAgs
                    from Ags in defAgs.DefaultIfEmpty()
                    join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod into defO
                    from Ot in defO.DefaultIfEmpty()
                    join otl in db.OgrenimTipleris on Ot.OgrenimTipID equals otl.OgrenimTipID into defOtl
                    from Otl in defOtl.DefaultIfEmpty()
                    join prl in db.Programlars on s.ProgramKod equals prl.ProgramKod into defprl
                    from Prl in defprl.DefaultIfEmpty()
                    join abl in db.AnabilimDallaris on new { AnabilimDaliID = (Prl != null ? Prl.AnabilimDaliID : (int?)null) } equals new { AnabilimDaliID = (int?)abl.AnabilimDaliID } into defabl
                    from Abl in defabl.DefaultIfEmpty()
                    where s.KullaniciID == Kul.KullaniciID
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
                        YtuOgrencisi = s.ProgramKod == null ? false : true,
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



            if (model.Sort.IsNullOrWhiteSpace() == false) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.TalepTarihi);




            model.RowCount = q.Count();
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;

            var IndexModel = new MIndexBilgi();
            var btDurulari = Management.BelgeTalepDurumList();
            //foreach (var item in btDurulari)
            //{
            //    var tipCount = q.Where(p => p.BelgeDurumID == item.BelgeDurumID).Count();
            //    IndexModel.ListB.Add(new mxRowModel { Key = item.DurumListeAdi, ClassName = item.BelgeDurumlari.ClassName, Color = item.BelgeDurumlari.Color, Toplam = tipCount });
            //}
            IndexModel.Toplam = model.RowCount;
            model.Data = q.Skip(PS.StartRowIndex).Take(model.PageSize).Select(item => new frTalep()
            {
                TalepGelenTalepID = item.TalepGelenTalepID,
                IsbelgeYuklemesiVar = item.IsBelgeYuklemeVar,
                YtuOgrencisi = item.YtuOgrencisi,
                TalepDurumID = item.TalepDurumID,
                TalepTipAciklama = item.TalepTipID == TalepTipi.LisansustuSureUzatmaTalebi ?
                                (item.OgrenimTipKod == OgrenimTipi.Doktra ?
                                    "Bu talep tipini seçecek öğrenciler, doktora tez önerisinden başarılı olmuş ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir. "
                                    :
                                    "Bu talep tipini seçecek öğrenciler Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşamasını tamamlamış ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir.")
                                 :
                                 item.TalepTipID == TalepTipi.Covid19KayitDondurmaTalebi ?
                                 (item.OgrenimTipKod == OgrenimTipi.Doktra ?
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

            ViewBag.IndexModel = IndexModel;

            #endregion 
            return View(model);
        }

        public ActionResult TalepYap(int? TalepGelenTalepID, string EKD)
        {
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var Talep = new TalepGelenTalepler();
            var MmMessage = new MmMessage();
            var KayitYetki =  RoleNames.GelenTalepKayit.InRoleCurrent();

            if (TalepGelenTalepID.HasValue)
            {
                Talep = db.TalepGelenTaleplers.Where(p => p.TalepGelenTalepID == TalepGelenTalepID.Value && p.KullaniciID == (KayitYetki ? p.KullaniciID : UserIdentity.Current.Id)).First();
            }
            else
            {
                Talep.TalepSurecID = Management.getAktifTalepSurecID(_EnstituKod) ?? 0;
                var Kul = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();

                Talep.KullaniciID = Kul.KullaniciID;
                Talep.AdSoyad = Kul.Ad + " " + Kul.Soyad;
                if (Kul.YtuOgrencisi)
                {
                    Talep.OgrenciNo = Kul.OgrenciNo;
                    var Ot = db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod && p.OgrenimTipKod == Kul.OgrenimTipKod).First();
                    Talep.OgrenimTipID = Ot.OgrenimTipID;
                    Talep.OgrenimTipKod = Ot.OgrenimTipKod;
                    Talep.ProgramKod = Kul.ProgramKod;
                }
            }

            ViewBag.MmMessage = MmMessage;
            ViewBag.TalepArGorStatuID = new SelectList(Management.cmbArGorStatuleri(true), "Value", "Caption", Talep.TalepArGorStatuID);
            ViewBag.TalepTipID = new SelectList(Management.cmbTalepTipleriSurec(Talep.TalepSurecID, Talep.TalepTipID, true), "Value", "Caption", Talep.TalepTipID);

            
            return View(Talep);
        }

        [HttpPost]
        public ActionResult TalepYap(TalepGelenTalepler kModel, HttpPostedFileBase Dosya, string DosyaAdi, HttpPostedFileBase DosyaDanismanOnay, string DosyaAdiDanismanOnay, string EKD)
        {
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var MmMessage = new MmMessage();
            MmMessage.Title = "";
            var KayitYetki =  RoleNames.GelenTalepKayit.InRoleCurrent();
            #region kontrol
            var Kul = db.Kullanicilars.Where(p => p.KullaniciID == kModel.KullaniciID).First();

            if (kModel.TalepGelenTalepID <= 0)
            {
                kModel.TalepSurecID = Management.getAktifTalepSurecID(_EnstituKod) ?? 0;

                if (kModel.TalepSurecID <= 0)
                {
                    MmMessage.Messages.Add("Sistem talep işlemlerine kapalıdır.");
                }
                else kModel.TalepSurecID = kModel.TalepSurecID;
            }
            else
            {
                var TalepSurec = Management.GetTalepSurec(kModel.TalepSurecID);
                if (!KayitYetki && !TalepSurec.AktifSurec)
                {
                    MmMessage.Messages.Add("Sistem talep işlemlerine kapalıdır.");
                }
            }
            if (MmMessage.Messages.Count == 0)
                if (kModel.TalepTipID <= 0)
                {
                    MmMessage.Messages.Add("Talep Tipi seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TalepTipID" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TalepTipID" });


            if (Kul.YtuOgrencisi)
            {
                if (kModel.TalepTipID == TalepTipi.LisansustuSureUzatmaTalebi)
                {
                    if (kModel.IsHarcBorcuVar == true)
                    {
                        MmMessage.Messages.Add("Talep işleminin yapılabilmesi ödenmeyen harç borcunuzun bulunmaması gerekmektedir.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsHarcBorcuVar" });

                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "IsHarcBorcuVar" });
                }
                if (MmMessage.Messages.Count == 0)
                {
                    if (kModel.IsYtuArGor == true)
                    {
                        if (!kModel.TalepArGorStatuID.HasValue)
                        {
                            MmMessage.Messages.Add("Araştırma Görevlisi Statüsü seçiniz.");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TalepArGorStatuID" });

                        }
                        else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TalepArGorStatuID" });
                    }

                    if (kModel.OgrenimTipKod == OgrenimTipi.Doktra)
                    {
                        if (kModel.TalepTipID == TalepTipi.LisansustuSureUzatmaTalebi || kModel.TalepTipID == TalepTipi.Covid19KayitDondurmaTalebi)
                        {
                            if (kModel.TalepTipID == TalepTipi.LisansustuSureUzatmaTalebi)
                            {
                                if (kModel.IsTezOnerisiYapildi != true)
                                {
                                    MmMessage.Messages.Add("Başvuru Yapılabilmesi İçin Tez Önerisinin Verilmiş Olması Gerekmektedir.");
                                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsTezOnerisiYapildi" });
                                }
                                else
                                {
                                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "IsTezOnerisiYapildi" });
                                    if (!kModel.DoktoraTezOneriTarihi.HasValue)
                                    {
                                        MmMessage.Messages.Add("Doktora Tez Öneri Tarihi bilgisini giriniz.");
                                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DoktoraTezOneriTarihi" });
                                    }
                                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DoktoraTezOneriTarihi" });
                                    if (DosyaDanismanOnay == null && DosyaAdiDanismanOnay.IsNullOrWhiteSpace())
                                    {
                                        MmMessage.Messages.Add("Danışman İmzalı Onay Belgesi Yükleyiniz.");
                                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DosyaDanismanOnay" });
                                    }
                                    else if (DosyaDanismanOnay != null)
                                    {
                                        if (DosyaDanismanOnay.FileName.Split('.').Last().ToLower() != "pdf")
                                        {
                                            string msg = "Yükleyeceğiniz te öneri belgesi pdf türünde olmalıdır.";
                                            MmMessage.Messages.Add(msg);
                                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DosyaDanismanOnay" });
                                        }
                                        else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DosyaDanismanOnay" });
                                    }
                                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DosyaDanismanOnay" });
                                }


                            }
                            else
                            {
                                if (kModel.IsTezOnerisiYapildi == true)
                                {
                                    if (!kModel.DoktoraTezOneriTarihi.HasValue)
                                    {
                                        MmMessage.Messages.Add("Doktora Tez Öneri Tarihi bilgisini giriniz.");
                                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DoktoraTezOneriTarihi" });

                                    }
                                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DoktoraTezOneriTarihi" });
                                    if (DosyaDanismanOnay == null && DosyaAdiDanismanOnay.IsNullOrWhiteSpace())
                                    {
                                        MmMessage.Messages.Add("Danışman İmzalı Onay Belgesi Yükleyiniz.");
                                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DosyaDanismanOnay" });
                                    }
                                    else if (DosyaDanismanOnay != null)
                                    {
                                        if (DosyaDanismanOnay.FileName.Split('.').Last().ToLower() != "pdf")
                                        {
                                            string msg = "Yükleyeceğiniz te öneri belgesi pdf türünde olmalıdır.";
                                            MmMessage.Messages.Add(msg);
                                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DosyaDanismanOnay" });
                                        }
                                        else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DosyaDanismanOnay" });
                                    }
                                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DosyaDanismanOnay" });
                                }
                            }
                        }

                    }
                    else
                    {
                        if (kModel.TalepTipID == TalepTipi.LisansustuSureUzatmaTalebi || kModel.TalepTipID == TalepTipi.Covid19KayitDondurmaTalebi)
                        {
                            if (kModel.TalepTipID == TalepTipi.LisansustuSureUzatmaTalebi)
                            {
                                if (kModel.IsDersYukuTamamlandi != true)
                                {
                                    MmMessage.Messages.Add("Talep işleminin yapılabilmesi için ders yükünün tamamlanması gerekmektedir.");
                                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsDersYukuTamamlandi" });
                                }
                                else
                                {
                                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DosyaDanismanOnay" });
                                    if (kModel.IsDersYukuTamamlandi == true && DosyaDanismanOnay == null && DosyaAdiDanismanOnay.IsNullOrWhiteSpace())
                                    {
                                        MmMessage.Messages.Add("Danışman İmzalı Onay Belgesi Yükleyiniz.");
                                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DosyaDanismanOnay" });
                                    }
                                    else if (DosyaDanismanOnay != null)
                                    {
                                        if (DosyaDanismanOnay.FileName.Split('.').Last().ToLower() != "pdf")
                                        {
                                            string msg = "Yükleyeceğiniz ders yükü belgesi pdf türünde olmalıdır.";
                                            MmMessage.Messages.Add(msg);
                                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DosyaDanismanOnay" });
                                        }
                                        else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DosyaDanismanOnay" });
                                    }
                                }
                            }
                            else
                            {
                                if (kModel.IsDersYukuTamamlandi == true)
                                {
                                    if (DosyaDanismanOnay == null && DosyaAdiDanismanOnay.IsNullOrWhiteSpace())
                                    {
                                        MmMessage.Messages.Add("Danışman İmzalı Onay Belgesi Yükleyiniz.");
                                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DosyaDanismanOnay" });
                                    }
                                    else if (DosyaDanismanOnay != null)
                                    {
                                        if (DosyaDanismanOnay.FileName.Split('.').Last().ToLower() != "pdf")
                                        {
                                            string msg = "Yükleyeceğiniz ders yükü belgesi pdf türünde olmalıdır.";
                                            MmMessage.Messages.Add(msg);
                                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DosyaDanismanOnay" });
                                        }
                                        else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DosyaDanismanOnay" });
                                    }
                                }

                            }
                        }


                    }
                }

            }




            if (MmMessage.Messages.Count == 0)
            {
                if (Kul.TalepGelenTaleplers.Any(a =>
                    a.TalepSurecID == kModel.TalepSurecID && (a.TalepTipID == kModel.TalepTipID || a.TalepTipleri.BirlikteBasvurulamayacakTalepTipID == kModel.TalepTipID) && a.TalepGelenTalepID != kModel.TalepGelenTalepID))
                {
                    MmMessage.Messages.Add("Bu talep alım sürecinde zaten bir talebiniz bulunmaktadır. Bu talep tipinde yeni talep başvurusu yapamazsınız.");
                }
            }
            var TalepTip = new TalepSureciTalepTipleri();
            if (MmMessage.Messages.Count == 0)
            {


                TalepTip = db.TalepSureciTalepTipleris.Where(p => p.TalepSurecID == kModel.TalepSurecID && p.TalepTipID == kModel.TalepTipID).First();
                if (TalepTip.IsBelgeYuklemeVar)
                {
                    if (Dosya == null && DosyaAdi.IsNullOrWhiteSpace())
                    {
                        MmMessage.Messages.Add("Belge seçiniz.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Dosya" });

                    }
                    else if (Dosya != null)
                    {
                        if (Dosya.FileName.Split('.').Last().ToLower() != "pdf")
                        {
                            string msg = "Yükleyeceğiniz belge pdf türünde olmalıdır.";
                            MmMessage.Messages.Add(msg);
                            MmMessage.MessagesDialog.Add(new MrMessage
                            {
                                MessageType = Msgtype.Warning,
                                PropertyName = "Dosya",
                                Message = msg
                            });
                        }
                        else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Dosya" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Dosya" });

                }

                if (TalepTip.IsTaahhutIsteniyor)
                {
                    if (kModel.IsTaahut != true)
                    {

                        MmMessage.Messages.Add("Talep işleminin yapılabilmesi taahhüt onayı verilmesi gerekmektedir.");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsTaahut" });

                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "IsTaahut" });

                }
                else kModel.IsTaahut = null;
            }
            #endregion

            if (MmMessage.Messages.Count == 0)
            {
                var YeniKayit = kModel.TalepGelenTalepID <= 0;

                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.KullaniciID = Kul.KullaniciID;
                kModel.TalepTipID = kModel.TalepTipID;
                kModel.AdSoyad = Kul.Ad + " " + Kul.Soyad;

                if (kModel.IsTezOnerisiYapildi != true && kModel.IsDersYukuTamamlandi != true)
                {
                    DosyaDanismanOnay = null;
                }
                if (kModel.IsTezOnerisiYapildi != true) kModel.DoktoraTezOneriTarihi = null;
                if (Kul.YtuOgrencisi)
                {
                    kModel.OgrenciNo = Kul.OgrenciNo;
                    var Ot = db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod && p.OgrenimTipKod == Kul.OgrenimTipKod).First();
                    kModel.OgrenimTipID = Ot.OgrenimTipID;
                    kModel.OgrenimTipKod = Ot.OgrenimTipKod;
                    kModel.ProgramKod = Kul.ProgramKod;
                }
                kModel.DoktoraTezOneriTarihi = kModel.OgrenimTipKod == OgrenimTipi.Doktra ? kModel.DoktoraTezOneriTarihi : null;

                var talep = new TalepGelenTalepler();
                if (YeniKayit)
                {
                    kModel.TalepTarihi = DateTime.Now;
                    kModel.TalepDurumID = TalepDurumu.TalepYapildi;//talep edildi
                    kModel.TalepTarihi = DateTime.Now;


                    talep = db.TalepGelenTaleplers.Add(kModel);
                    db.SaveChanges();
                    bilgiMaili(new List<int> { talep.TalepGelenTalepID } ,_EnstituKod);

                }
                else
                {
                    talep = db.TalepGelenTaleplers.Where(p => p.TalepGelenTalepID == kModel.TalepGelenTalepID && p.KullaniciID == (KayitYetki ? p.KullaniciID : UserIdentity.Current.Id)).First();
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
                    db.SaveChanges();
                }
                if (TalepTip.IsBelgeYuklemeVar && Dosya != null)
                {
                    string DosyaYolu = "/TalepDosyalari/TT_" + kModel.TalepTipID + "_" + talep.TalepGelenTalepID + "_" + Dosya.FileName.ToFileNameAddGuid();
                    var sfilename = Server.MapPath("~" + DosyaYolu);
                    Dosya.SaveAs(sfilename);

                    db.TalepGelenTalepBelgeleris.RemoveRange(talep.TalepGelenTalepBelgeleris).Where(p => p.IsDanismanOnayDosyasi == false);
                    foreach (var belge in talep.TalepGelenTalepBelgeleris)
                    {
                        var path = Server.MapPath("~" + belge.DosyaYolu);
                        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                    }


                    talep.TalepGelenTalepBelgeleris.Add(new TalepGelenTalepBelgeleri()
                    {

                        DosyaAdi = Dosya.FileName.GetFileName().ReplaceSpecialCharacter(),
                        DosyaYolu = DosyaYolu

                    });
                    db.SaveChanges();
                }

                if ((kModel.TalepTipID == TalepTipi.LisansustuSureUzatmaTalebi || kModel.TalepTipID == TalepTipi.Covid19KayitDondurmaTalebi))
                {
                    var Dosyalar = new Dictionary<TalepGelenTalepBelgeleri, HttpPostedFileBase>();
                    if (kModel.TalepTipID == TalepTipi.Covid19KayitDondurmaTalebi && DosyaDanismanOnay != null)
                    {
                        Dosyalar.Add(new TalepGelenTalepBelgeleri
                        {
                            IsDanismanOnayDosyasi = true,
                            DosyaAdi = DosyaDanismanOnay.FileName.ReplaceSpecialCharacter(),
                            DosyaYolu = "/TalepDosyalari/TT_" + kModel.TalepTipID + "_" + talep.TalepGelenTalepID + "_" + DosyaDanismanOnay.FileName.ToFileNameAddGuid()
                        }, DosyaDanismanOnay);
                    }

                    foreach (var itemD in Dosyalar)
                    {
                        var sfilename = Server.MapPath("~" + itemD.Key.DosyaYolu);
                        itemD.Value.SaveAs(sfilename);
                        var qSilinecekler = talep.TalepGelenTalepBelgeleris.AsQueryable();
                        if (itemD.Key.IsDanismanOnayDosyasi == true) qSilinecekler = qSilinecekler.Where(p => p.IsDanismanOnayDosyasi);
                        var Silinecekler = qSilinecekler.ToList();
                        foreach (var belge in Silinecekler)
                        {
                            var path = Server.MapPath("~" + belge.DosyaYolu);
                            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                        }
                        db.TalepGelenTalepBelgeleris.RemoveRange(Silinecekler);


                        talep.TalepGelenTalepBelgeleris.Add(new TalepGelenTalepBelgeleri()
                        {

                            DosyaAdi = itemD.Key.DosyaAdi,
                            DosyaYolu = itemD.Key.DosyaYolu,
                            IsDanismanOnayDosyasi = itemD.Key.IsDanismanOnayDosyasi,

                        });
                        db.SaveChanges();
                    }

                }
                LogIslemleri.LogEkle("TalepGelenTalepler", YeniKayit ? IslemTipi.Insert : IslemTipi.Update, talep.ToJson());
                MmMessage.IsSuccess = true;
                MmMessage.MessageType = Msgtype.Success;
                return MmMessage.toJsonResult();
            }
            else
            {
                MmMessage.IsSuccess = false;
                MmMessage.MessageType = Msgtype.Warning;
                return MmMessage.toJsonResult();
            }
        }

        void bilgiMaili(List<int> TalepGelenTalepIDs, string _EnstituKod, string Aciklama = "")
        {


            var Taleps = (from s in db.TalepGelenTaleplers
                          join ts in db.TalepSurecleris on s.TalepSurecID equals ts.TalepSurecID
                          join kul in db.Kullanicilars on s.KullaniciID equals kul.KullaniciID
                          join tt in db.TalepTipleris on s.TalepTipID equals tt.TalepTipID
                          join td in db.TalepDurumlaris on s.TalepDurumID equals td.TalepDurumID
                          join ags in db.TalepArGorStatuleris on s.TalepArGorStatuID equals ags.TalepArGorStatuID into defAgs
                          from Ags in defAgs.DefaultIfEmpty()
                          join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod into defO
                          from Ot in defO.DefaultIfEmpty()
                          join otl in db.OgrenimTipleris on Ot.OgrenimTipID equals otl.OgrenimTipID into defOtl
                          from Otl in defOtl.DefaultIfEmpty()
                          join prl in db.Programlars on s.ProgramKod equals prl.ProgramKod into defprl
                          from Prl in defprl.DefaultIfEmpty()
                          join abl in db.AnabilimDallaris on new { AnabilimDaliID = (Prl != null ? Prl.AnabilimDaliID : (int?)null) } equals new { AnabilimDaliID = (int?)abl.AnabilimDaliID } into defabl
                          from Abl in defabl.DefaultIfEmpty()
                          where TalepGelenTalepIDs.Contains(s.TalepGelenTalepID)
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
            foreach (var Talep in Taleps)
            {
                var htmlBigliRow = new List<mailTableRow>();
                var contentBilgi = new mailTableContent();
                htmlBigliRow.Add(new mailTableRow { Baslik = "Ad Soyad", Aciklama = Talep.AdSoyad });
                if (Talep.YtuOgrencisi)
                {
                    if (!Talep.OgrenciNo.IsNullOrWhiteSpace()) htmlBigliRow.Add(new mailTableRow { Baslik = "Öğrenci No", Aciklama = Talep.OgrenciNo });
                    if (Talep.OgrenimTipKod.HasValue) htmlBigliRow.Add(new mailTableRow { Baslik = "Öğrenim Seviyesi", Aciklama = Talep.OgrenimTipAdi });
                    if (!Talep.ProgramAdi.IsNullOrWhiteSpace()) htmlBigliRow.Add(new mailTableRow { Baslik = "Program", Aciklama = Talep.ProgramAdi });
                }

                htmlBigliRow.Add(new mailTableRow { Baslik = "Talep Tipi", Aciklama = Talep.TalepTipAdi });
                string TalepTipAciklama = "";
                if (Talep.TalepTipID == TalepTipi.LisansustuSureUzatmaTalebi)
                {
                    TalepTipAciklama = Talep.OgrenimTipKod == OgrenimTipi.Doktra ?
                        "Bu talep tipini seçecek öğrenciler, doktora tez önerisinden başarılı olmuş ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir. "
                        :
                        "Bu talep tipini seçecek öğrenciler Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşamasını tamamlamış ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir.";
                }
                else if (Talep.TalepTipID == TalepTipi.Covid19KayitDondurmaTalebi)
                {
                    TalepTipAciklama = Talep.OgrenimTipKod == OgrenimTipi.Doktra ?
                        "Bu talep tipini seçecek olan öğrencilerimizden: doktora tez önerisinden başarılı olunmuş ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir. Aksi takdirde talebiniz kabul edilmeyecektir."
                        :
                        "Bu talep tipini seçecek olan öğrencilerimizden: YTÜ Lisansüstü Eğitim ve Öğretim Yönetmeliği Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşaması tamamlanmış ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir Aksi takdirde talebiniz kabul edilmeyecektir.";
                }
                else TalepTipAciklama = Talep.TalepTipAciklama;
                if (!TalepTipAciklama.IsNullOrWhiteSpace()) htmlBigliRow.Add(new mailTableRow { Baslik = "Talep Tipi Açıklaması", Aciklama = TalepTipAciklama });
                htmlBigliRow.Add(new mailTableRow { Baslik = "Talep Tarihi", Aciklama = Talep.TalepTarihi.ToFormatDateAndTime() });
                htmlBigliRow.Add(new mailTableRow { Baslik = "Talep Durumu", Aciklama = Talep.TalepDurumAdi });
                if (Talep.TalepDurumID == TalepDurumu.Rededildi) htmlBigliRow.Add(new mailTableRow { Baslik = "Red Açıklaması", Aciklama = Talep.TalepDurumAciklamasi });
                if (!Aciklama.IsNullOrWhiteSpace()) htmlBigliRow.Add(new mailTableRow { Baslik = "Not", Aciklama = Aciklama });

                contentBilgi.GrupBasligi = "'" + Talep.TalepTipAdi + "' talebiniz " + Talep.TalepDurumAdi;
                contentBilgi.Detaylar = htmlBigliRow;

                var mmmC = new mdlMailMainContent();
                var enstituAdi = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod ).First().EnstituAd; 
                mmmC.EnstituAdi = enstituAdi;
                mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(_EnstituKod);
                var _ea = mailBilgi.SistemErisimAdresi;
                var WurlAddr = _ea.Split('/').ToList();
                if (_ea.Contains("//"))
                    _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
                else
                    _ea = "http://" + WurlAddr.First();
                mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png";
                var HCB = Management.RenderPartialView("Ajax", "getMailTableContent", contentBilgi);
                mmmC.Content = HCB;
                string htmlMail = Management.RenderPartialView("Ajax", "getMailContent", mmmC);
                var emailSend = MailManager.sendMail(mailBilgi.EnstituKod, "Talep İşleminiz Hk.", htmlMail, Talep.EMail, null);

                if (emailSend)
                {
                    var kModel = new GonderilenMailler();
                    kModel.Tarih = DateTime.Now;
                    kModel.EnstituKod = mailBilgi.EnstituKod;

                    kModel.MesajID = null;
                    kModel.Konu = "Talep İşlemleri: " + Talep.TalepTipAdi + "  (" + Talep.AdSoyad + " [" + Talep.TalepDurumAdi + "])";
                    kModel.Aciklama = "";
                    kModel.AciklamaHtml = htmlMail;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemTarihi = DateTime.Now;
                    kModel.IslemYapanIP = UserIdentity.Ip;
                    kModel.Gonderildi = true;
                    kModel.GonderilenMailKullanicilars = new List<GonderilenMailKullanicilar>();
                    kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = Talep.EMail });
                    db.GonderilenMaillers.Add(kModel);
                    db.SaveChanges();
                }
            }

        }

        [Authorize(Roles = RoleNames.GelenTalepKayit)]
        public ActionResult Istenenkaydet(int id, int TalepDurumID, string TalepDurumAciklamasi)
        {
            

            var talep = db.TalepGelenTaleplers.Where(p => p.TalepGelenTalepID == id).First();
            var oldTDID = talep.TalepDurumID;

            talep.TalepDurumID = TalepDurumID;
            if (talep.TalepDurumID == TalepDurumu.Rededildi) talep.TalepDurumAciklamasi = TalepDurumAciklamasi;
            talep.IslemTarihi = DateTime.Now;
            talep.IslemYapanID = UserIdentity.Current.Id;
            talep.IslemYapanIP = UserIdentity.Ip;
            db.SaveChanges();

            if (TalepDurumID != TalepDurumu.TalepYapildi && TalepDurumID != oldTDID)
            {
                bilgiMaili(new List<int> { talep.TalepGelenTalepID } ,talep.TalepSurecleri.EnstituKod);
            }
            var qbDrm = talep.TalepDurumlari;

            return new
            {
                TalepDurumAdi = qbDrm.TalepDurumAdi,
                ClassName = qbDrm.ClassName,
                Color = qbDrm.Color
            }.toJsonResult();
        }

        [HttpPost]
        public ActionResult GelenTalepOnay(List<int> KTalepGelenTalepIDs, string OnayAciklamasi, string EKD)
        {
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);

            var mmMessage = new MmMessage();
            mmMessage.MessageType = Msgtype.Success;
            mmMessage.IsSuccess = true;
            mmMessage.Title = "Toplu Talep Onaylama İşlemi";
            if (UserIdentity.Current.IsAdmin)
            {
                try
                {
                    var Talepler = db.TalepGelenTaleplers.Where(p => p.TalepDurumID == TalepDurumu.TalepYapildi && KTalepGelenTalepIDs.Contains(p.TalepGelenTalepID)).ToList();

                    foreach (var item in Talepler)
                    {
                        item.TalepDurumID = TalepDurumu.Onaylandi;
                        item.IslemTarihi = DateTime.Now;
                        item.IslemYapanID = UserIdentity.Current.Id;
                        item.IslemYapanIP = UserIdentity.Ip;
                    }
                    db.SaveChanges();
                    mmMessage.Messages.Add(Talepler.Count + " Talep onaylandı");
                    try
                    {
                        bilgiMaili(KTalepGelenTalepIDs ,_EnstituKod, OnayAciklamasi);
                        mmMessage.Messages.Add("Onaylanan " + Talepler.Count + " Talep'e mail gönderildi.");
                    }
                    catch (Exception ex)
                    {
                        mmMessage.Messages.Add("Onaylanan Taleplere mail gönderilirken bir hata oluştu.");
                        Management.SistemBilgisiKaydet("Onaylanan Taleplere mail gönderilirken bir hata oluştu! <br/><br/> Hata: " + ex.ToExceptionMessage(), "TalepYap/GelenTalepOnay<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                    }
                    LogIslemleri.LogEkle("TalepGelenTalepler", IslemTipi.Update, Talepler.ToJson());

                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Toplu Talep Onay işlemi yapılırken bir hata oluştu! Hata: " + ex.ToExceptionMessage());
                    Management.SistemBilgisiKaydet("Toplu Talep Onay işlemi yapılırken bir hata oluştu! <br/><br/> Hata: " + ex.ToExceptionMessage(), "TalepYap/GelenTalepOnay<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                }
            }
            else
            {
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Bu işlemi yapmaya yetkili değilsiniz.");

            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new
            {
                IsSuccess = mmMessage.IsSuccess,
                Messages = strView
            }, "application/json", JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetTalepTipBilgi(int? TalepTipID, int TalepSurecID, int KullaniciID)
        {

            string Aciklama = null;
            var Kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).First();
            if (TalepTipID == TalepTipi.LisansustuSureUzatmaTalebi)
            {
                Aciklama = Kul.OgrenimTipKod == OgrenimTipi.Doktra ?
                    "Bu talep tipini seçecek öğrenciler, doktora tez önerisinden başarılı olmuş ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir. "
                    :
                    "Bu talep tipini seçecek öğrenciler Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşamasını tamamlamış ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir.";
            }
            else if (TalepTipID == TalepTipi.Covid19KayitDondurmaTalebi)
            {
                Aciklama = Kul.OgrenimTipKod == OgrenimTipi.Doktra ?
                    "Bu talep tipini seçecek olan öğrencilerimizden: doktora tez önerisinden başarılı olunmuş ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir. Aksi takdirde talebiniz kabul edilmeyecektir."
                    :
                    "Bu talep tipini seçecek olan öğrencilerimizden: YTÜ Lisansüstü Eğitim ve Öğretim Yönetmeliği Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşaması tamamlanmış ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir Aksi takdirde talebiniz kabul edilmeyecektir.";
            }
            var TalepTip = db.TalepSureciTalepTipleris.Where(p => p.TalepSurecID == TalepSurecID && p.TalepTipID == TalepTipID).Select(s => new
            {
                s.IsBelgeYuklemeVar,
                IsDoktora = Kul.OgrenimTipKod == OgrenimTipi.Doktra,
                TalepTipID,
                s.IsTaahhutIsteniyor, 
                s.TalepTipleri.TaahhutAciklamasi,
                TalepTipAciklama = (Aciklama != null ? Aciklama : s.TalepTipleri.TalepTipAciklama)
            }).First();
            var jResult= TalepTip.toJsonResult();

            return jResult;
        }


        [Authorize]
        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = true;
           
            bool DuzenleYetki =  RoleNames.GelenTalepSil.InRoleCurrent();
            var talep = db.TalepGelenTaleplers.Where(p => p.TalepGelenTalepID == id && p.KullaniciID == (DuzenleYetki ? p.KullaniciID : UserIdentity.Current.Id)).FirstOrDefault();
            var kul = talep.Kullanicilar;
            if (DuzenleYetki == false)
            {
                if (UserIdentity.Current.Id != kul.KullaniciID)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Title = "Uyarı";
                    mmMessage.Messages.Add("Başka bir kullanıcıya ait talep işlemini silemezsiniz!");

                }
                else if (talep.TalepDurumID == TalepDurumu.Onaylandi || talep.TalepDurumID == TalepDurumu.Rededildi)
                {
                    mmMessage.MessageType = Msgtype.Error;
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
                    db.TalepGelenTaleplers.Remove(talep);
                    db.SaveChanges();
                    LogIslemleri.LogEkle("TalepGelenTalepler", IslemTipi.Delete, talep.ToJson());
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Talep silinirken bir hata oluştu! Hata: " + ex.ToExceptionMessage());
                    mmMessage.Title = "Hata";
                    Management.SistemBilgisiKaydet("Talep silinirken bir hata oluştu! TalepGelenTalepID=" + id, "TalepYap /Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}