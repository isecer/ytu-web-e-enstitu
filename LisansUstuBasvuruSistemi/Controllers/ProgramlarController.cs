using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Programlar)]
    public class ProgramlarController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            var sEkod = EnstituBus.GetSelectedEnstitu(ekd);
            return Index(new FmProgramlar { PageSize = 15, EnstituKod = sEkod, Expand = false });
        }
        [HttpPost]
        public ActionResult Index(FmProgramlar model)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in _entities.Programlars
                    join sl in _entities.Programlars on new { s.ProgramKod } equals new { sl.ProgramKod } into defP
                    from slP in defP.DefaultIfEmpty()
                    join e in _entities.AnabilimDallaris on s.AnabilimDaliID equals e.AnabilimDaliID
                    join at in _entities.AlesTipleris on new { s.AlesTipID } equals new { at.AlesTipID }
                    join enst in _entities.Enstitulers on new { e.EnstituKod } equals new { enst.EnstituKod }
                    where enstKods.Contains(enst.EnstituKod)
                    select new FrProgramlar
                    {
                        AnabilimDaliID = s.AnabilimDaliID,
                        EnstituKod = enst.EnstituKod,
                        EnstituAd = enst.EnstituAd,
                        AnabilimDaliKod = e.AnabilimDaliKod,
                        AnabilimDaliAdi = e.AnabilimDaliAdi,
                        AlesTipID = s.AlesTipID,
                        AlesTipAdi = at.AlesTipAdi,
                        ProgramKod = s.ProgramKod,
                        ProgramAdi = slP != null ? slP.ProgramAdi : "",
                        Ingilizce = s.Ingilizce,
                        Ucretli = s.Ucretli,
                        Ucret = s.Ucret,
                        BasvuruAgnoAlimTipID = s.BasvuruAgnoAlimTipID,
                        LYuzdeOran = s.LYuzdeOran,
                        YLYuzdeOran = s.YLYuzdeOran,

                        AgnoAlimTipAdi = s.BasvuruAgnoAlimTipleri != null ? s.BasvuruAgnoAlimTipleri.AgnoAlimTipAdi : "",
                        AlesNotuYuksekOlanAlinsin = s.AlesNotuYuksekOlanAlinsin,
                        SecilenAlesTipleri = s.ProgramlarAlesEslesmeleris.Select(s => s.AlesTipleri.AlesTipAdi).ToList(),
                        LYLHerhangiBirindeGecenAlanIci = s.LYLHerhangiBirindeGecenAlanIci,
                        ProgramSecimiEkBilgi = s.ProgramSecimiEkBilgi, 
                        IsAlandisiBolumKisitlamasi = s.IsAlandisiBolumKisitlamasi,
                        AlandisiBolumKisitListesi = s.ProgramlarAlandisiBolumKisitlamalaris.Select(s2 => s2.OgrenciBolumleri.BolumAdi).OrderBy(o => o).ToList(),
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                        KullaniciTipID = s.KullaniciTipID,
                        KullaniciTipAdi = s.KullaniciTipleri.KullaniciTipAdi,

                    };
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (model.IsAlandisiBolumKisitlamasi.HasValue) q = q.Where(p => p.IsAlandisiBolumKisitlamasi == model.IsAlandisiBolumKisitlamasi);
            if (model.LYLHerhangiBirindeGecenAlanIci.HasValue) q = q.Where(p => p.LYLHerhangiBirindeGecenAlanIci == model.LYLHerhangiBirindeGecenAlanIci);
            if (model.AlesNotuYuksekOlanAlinsin.HasValue) q = q.Where(p => p.AlesNotuYuksekOlanAlinsin == model.AlesNotuYuksekOlanAlinsin);
            if (model.Ucretli.HasValue) q = q.Where(p => p.Ucretli == model.Ucretli);
            if (model.ProgramSecimiEkBilgi.HasValue) q = q.Where(p => p.ProgramSecimiEkBilgi == model.ProgramSecimiEkBilgi);
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.AlesTipID.HasValue) q = q.Where(p => p.AlesTipID == model.AlesTipID.Value);
            if (!model.ProgramAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.ProgramAdi.Contains(model.ProgramAdi) || p.AnabilimDaliAdi.Contains(model.ProgramAdi) || p.ProgramKod == model.ProgramAdi);
            if (model.KullaniciTipID.HasValue) q = q.Where(p => p.KullaniciTipID == model.KullaniciTipID);
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.AnabilimDaliAdi).ThenBy(o => o.ProgramAdi); 
            model.data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            };
            ViewBag.IndexModel = indexModel;

            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.AlesTipID = new SelectList(Management.cmbGetAktifAlesTipleri(true), "Value", "Caption", model.AlesTipID);
            var kullaniciTipList = new List<CmbIntDto>() { new CmbIntDto { }, new CmbIntDto { Value = KullaniciTipBilgi.YerliOgrenci, Caption = "Yerli Öğrenci" }, new CmbIntDto { Value = KullaniciTipBilgi.YabanciOgrenci, Caption = "Yabancı Öğrenci" } };
            ViewBag.KullaniciTipID = new SelectList(kullaniciTipList, "Value", "Caption", model.KullaniciTipID);
            ViewBag.Ucretli = new SelectList(ComboData.GetCmbVarYokData(true), "Value", "Caption", model.Ucretli);
            var alesTipNotSecimList = new List<CmbBoolDto>() { new CmbBoolDto { }, new CmbBoolDto { Value = true, Caption = "Ales Tipleri Arasında Notu Yüksek Olan Sonuç Alınsın" }, new CmbBoolDto { Value = false, Caption = "Programa Ait Ales Tipi Notu Alınsın" } };
            ViewBag.AlesNotuYuksekOlanAlinsin = new SelectList(alesTipNotSecimList, "Value", "Caption", model.AlesNotuYuksekOlanAlinsin);
            ViewBag.LYLHerhangiBirindeGecenAlanIci = new SelectList(Management.cmbAlanEslesmeData(true), "Value", "Caption", model.LYLHerhangiBirindeGecenAlanIci);
            ViewBag.ProgramSecimiEkBilgi = new SelectList(ComboData.GetCmbEvetHayirData(true), "Value", "Caption", model.ProgramSecimiEkBilgi);
            ViewBag.IsAlandisiBolumKisitlamasi = new SelectList(ComboData.GetCmbVarYokData(true), "Value", "Caption", model.IsAlandisiBolumKisitlamasi);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(string id, string ekd)
        {
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var model = new Programlar();
            List<int> sAlesTipIDs = null;
            var ogrenciBolumIDs = new List<int>();
            if (id.IsNullOrWhiteSpace() == false)
            {
                var data = _entities.Programlars.FirstOrDefault(p => p.ProgramKod == id);
                if (data != null)
                {
                    model = data;

                    ogrenciBolumIDs = data.ProgramlarAlandisiBolumKisitlamalaris.Select(s => s.OgrenciBolumID).Distinct().ToList();
                    sAlesTipIDs = data.ProgramlarAlesEslesmeleris.Select(s => s.AlesTipID).ToList();
                    enstituKod = data.AnabilimDallari.EnstituKod;


                }
            }
            else model.LYLHerhangiBirindeGecenAlanIci = true;

            ViewBag.AnabilimDaliID = new SelectList(Management.cmbGetYetkiliAnabilimDallari(true), "Value", "Caption", model.AnabilimDaliID);
            ViewBag.AlesTipID = new SelectList(Management.cmbGetAktifAlesTipleri(true), "Value", "Caption", model.AlesTipID);
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.Diller2 = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.KullaniciID = new SelectList(new List<CmbIntDto>(), "Value", "Caption");
            var kullaniciTipList = new List<CmbIntDto>() { new CmbIntDto { }, new CmbIntDto { Value = KullaniciTipBilgi.YerliOgrenci, Caption = "Yerli Öğrenci" }, new CmbIntDto { Value = KullaniciTipBilgi.YabanciOgrenci, Caption = "Yabancı Öğrenci" } };
            ViewBag.KullaniciTipID = new SelectList(kullaniciTipList, "Value", "Caption", model.KullaniciTipID);
            ViewBag.LYLHerhangiBirindeGecenAlanIci = new SelectList(Management.cmbAlanEslesmeData(false), "Value", "Caption", model.LYLHerhangiBirindeGecenAlanIci);
            ViewBag.OgrenciBolumID = new SelectList(new List<CmbIntDto>(), "Value", "Caption");
            ViewBag.BasvuruAgnoAlimTipID = new SelectList(Management.cmbGetBasvuruAgnoAlimTipleri(true), "Value", "Caption", model.BasvuruAgnoAlimTipID);
            var alesTipNotSecimList = new List<CmbBoolDto>() { new CmbBoolDto { Value = true, Caption = "Ales Tipleri Arasında Notu Yüksek Olan Sonuç Alınsın" }, new CmbBoolDto { Value = false, Caption = "Programa Ait Ales Tipi Notu Alınsın" } };
            ViewBag.AlesNotuYuksekOlanAlinsin = new SelectList(alesTipNotSecimList, "Value", "Caption", model.AlesNotuYuksekOlanAlinsin);

            var roleName = new List<string>() { RoleNames.MulakatSureci, RoleNames.Kotalar, RoleNames.Programlar, RoleNames.BolumEslestir };
            var kuls = UserBus.GetRoluOlanKullanicilar(roleName, enstituKod);
            var rolls = KullanicilarBus.GetProgramYetkisiOlanKullanicilar(kuls, model.ProgramKod);
            ViewBag.KullaniciIDs = rolls.Where(p => p.Checked == true).Select(s => s.Value.KullaniciID).ToList();

            var oBolIds = _entities.BolumEslestirs.Where(p => p.ProgramKod == model.ProgramKod).Select(s => s.OgrenciBolumID).ToList();

            ViewBag.AOgrenciBolumID = oBolIds;
            ViewBag.OgrenciBolumIDs = ogrenciBolumIDs;
            ViewBag.AlesTipIDs = new SelectList(new List<CmbIntDto>(), "Value", "Caption");
            ViewBag.SAlesTipIDs = sAlesTipIDs;
            ViewBag.OldID = model.ProgramKod;
            ViewBag.EnstituKod = enstituKod;
            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Kayit(Programlar kModel, string enstituKod, string oldId, List<int> kullaniciId, List<int> ogrenciBolumId, List<int> ogrenciBolumIDs, List<int> alesTipIDs)
        {
            var mmMessage = new MmMessage();
            string id = oldId.IsNullOrWhiteSpace() ? kModel.ProgramKod : oldId;
            alesTipIDs = alesTipIDs ?? new List<int>();
            #region Kontrol 

            if (kullaniciId == null) kullaniciId = new List<int>();
            if (ogrenciBolumId == null) ogrenciBolumId = new List<int>();
            if (ogrenciBolumIDs == null) ogrenciBolumIDs = new List<int>();

            if (id.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Program kodu boş bırakılamaz ve 0 dan büyük bir değer olmalıdır!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProgramKod" });
            if (kModel.AnabilimDaliID <= 0)
            {
                mmMessage.Messages.Add("Anabilim Dalı seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnabilimDaliID" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AnabilimDaliID" });
            if (kModel.AlesTipID <= 0)
            {
                mmMessage.Messages.Add("Ales Tipi seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AlesTipID" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AlesTipID" });
            if (kModel.ProgramKod.IsNullOrWhiteSpace() && oldId.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Kayıt işlemini yapabilmeni için Kod kısmını doldurmanız gerekmektedir!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProgramKod" });
            if (kModel.KullaniciTipID <= 0)
            {
                mmMessage.Messages.Add("Kayıt işlemini yapabilmeniz öğrenci tipi seçiniz!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciTipID" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "KullaniciTipID" });
            if (kModel.Ucretli)
            {
                if (kModel.Ucret.HasValue == false)
                {
                    mmMessage.Messages.Add("Ücretli seçilen programın ücret bilgisinin girilmesi gerekmektedir!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Ucret" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Ucret" });

            }
            if (kModel.AlesNotuYuksekOlanAlinsin)
            {
                if (alesTipIDs.Count == 0)
                {
                    mmMessage.Messages.Add("Ales notu yüksek olan seçeneğini seçebilmeniz için sağındaki kutucukta bulunan Ales tiplerini seçiniz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AlesTipIDs" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AlesTipIDs" });

            }
            if (kModel.BasvuruAgnoAlimTipID.HasValue)
            {
                if (kModel.BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.L_YLYuzdeBelirlensin)
                {
                    if (kModel.LYuzdeOran.HasValue == false)
                    {
                        mmMessage.Messages.Add("Lisans % oran bilgisinin girilmesi gerekmektedir!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LYuzdeOran" });
                    }
                    else if (kModel.YLYuzdeOran.HasValue == false)
                    {
                        mmMessage.Messages.Add("Yüksek Lisans % oran bilgisinin girilmesi gerekmektedir!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLYuzdeOran" });
                    }
                    else
                    {
                        if (kModel.LYuzdeOran + kModel.YLYuzdeOran != 100)
                        {
                            mmMessage.Messages.Add("Lisans % oranı ve Yüksek Lisans % oranı toplamı 100 e eşit olacak şekilde giriniz!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LYuzdeOran" });
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLYuzdeOran" });
                        }
                    }

                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvuruAgnoAlimTipID" });

            }
            if (kModel.YLAgnoKriteri.HasValue)
            {
                if (!(kModel.YLAgnoKriteri.Value >= 0 && kModel.YLAgnoKriteri.Value <= 100))
                {
                    mmMessage.Messages.Add("Girilecek Tezsiz Y.Lisans Min Agno not kriteri 0 ile 100 arasında olmalıdır");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLAgnoKriteri" });
                }
            }
            if (kModel.TYLAgnoKriteri.HasValue)
            {
                if (!(kModel.TYLAgnoKriteri.Value >= 0 && kModel.TYLAgnoKriteri.Value <= 100))
                {
                    mmMessage.Messages.Add("Girilecek Yüksek Tezli Y.Lisans Min Agno not kriteri 0 ile 100 arasında olmalıdır");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TYLAgnoKriteri" });
                }
            }
            if (kModel.DAgnoKriteri.HasValue)
            {
                if (!(kModel.DAgnoKriteri.Value >= 0 && kModel.DAgnoKriteri.Value <= 100))
                {
                    mmMessage.Messages.Add("Girilecek Doktora Min Agno not kriteri 0 ile 100 arasında olmalıdır");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DAgnoKriteri" });
                }
            }
            if (kModel.ProgramSecimiEkBilgi)
            {
                if (kModel.Aciklama.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Açıklama Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Aciklama" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Aciklama" });

            }
            if (kModel.IsAlandisiBolumKisitlamasi && ogrenciBolumIDs.Count == 0)
            {
                mmMessage.Messages.Add("Alan dışı öğrenci bölümü kısıtlaması seçeneği aktif edildiğinde kabul edilecek en az 1 öğrenci bölümünü listeden seçmeniz gerekmektedir.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsAlandisiBolumKisitlamasi" });
            }

            if (mmMessage.Messages.Count == 0)
            {
                int newOrEd = oldId.IsNullOrWhiteSpace() ? 1 : 0;
                var cnt = _entities.Programlars.Count(p => p.ProgramKod == id) + newOrEd;
                if (cnt > 1)
                {
                    mmMessage.Messages.Add("Tanımlamak istediğiniz kod daha önceden sisteme tanımlanmıştır, tekrar tanımlanamaz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                }
            }


            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                var bolm = _entities.AnabilimDallaris.First(p => p.AnabilimDaliID == kModel.AnabilimDaliID);
                if (oldId.IsNullOrWhiteSpace())
                {
                    var prg = _entities.Programlars.Add(new Programlar
                    {
                        AnabilimDaliID = kModel.AnabilimDaliID,
                        AnabilimDaliKod = bolm.AnabilimDaliKod,
                        ProgramKod = id,
                        ProgramAdi = kModel.ProgramAdi,
                        Aciklama = kModel.Aciklama,
                        AlesTipID = kModel.AlesTipID,
                        IsAktif = true,
                        Ingilizce = kModel.Ingilizce,
                        KullaniciTipID = kModel.KullaniciTipID,
                        Ucretli = kModel.Ucretli,
                        Ucret = kModel.Ucret,
                        ProgramSecimiEkBilgi = kModel.ProgramSecimiEkBilgi,
                        AlesNotuYuksekOlanAlinsin = kModel.AlesNotuYuksekOlanAlinsin,
                        BasvuruAgnoAlimTipID = kModel.BasvuruAgnoAlimTipID,
                        LYuzdeOran = kModel.LYuzdeOran,
                        YLYuzdeOran = kModel.YLYuzdeOran,
                        YLAgnoKriteri = kModel.YLAgnoKriteri,
                        TYLAgnoKriteri = kModel.TYLAgnoKriteri,
                        DAgnoKriteri = kModel.DAgnoKriteri,
                        BDAgnoKriteri = kModel.BDAgnoKriteri,
                        LYLHerhangiBirindeGecenAlanIci = kModel.LYLHerhangiBirindeGecenAlanIci,
                        IsAlandisiBolumKisitlamasi = kModel.IsAlandisiBolumKisitlamasi,
                        YokOgrenciKontroluYap = kModel.YokOgrenciKontroluYap,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemTarihi = DateTime.Now
                    });
                    if (kModel.AlesNotuYuksekOlanAlinsin) prg.ProgramlarAlesEslesmeleris = alesTipIDs.Select(s => new ProgramlarAlesEslesmeleri { AlesTipID = s }).ToList();
                    _entities.SaveChanges();
                    id = prg.ProgramKod;
                }
                else
                {
                    var data = _entities.Programlars.First(p => p.ProgramKod == id);
                    data.AnabilimDaliID = kModel.AnabilimDaliID;
                    data.AnabilimDaliKod = bolm.AnabilimDaliKod;
                    data.ProgramAdi = kModel.ProgramAdi;
                    data.Aciklama = kModel.Aciklama;
                    data.AlesTipID = kModel.AlesTipID;
                    data.Ingilizce = kModel.Ingilizce;
                    data.KullaniciTipID = kModel.KullaniciTipID;
                    data.Ucretli = kModel.Ucretli;
                    data.Ucret = kModel.Ucret;
                    data.ProgramSecimiEkBilgi = kModel.ProgramSecimiEkBilgi;
                    data.AlesNotuYuksekOlanAlinsin = kModel.AlesNotuYuksekOlanAlinsin;
                    data.LYLHerhangiBirindeGecenAlanIci = kModel.LYLHerhangiBirindeGecenAlanIci;
                    data.BasvuruAgnoAlimTipID = kModel.BasvuruAgnoAlimTipID;
                    data.LYuzdeOran = kModel.LYuzdeOran;
                    data.YLYuzdeOran = kModel.YLYuzdeOran;
                    data.YLAgnoKriteri = kModel.YLAgnoKriteri;
                    data.TYLAgnoKriteri = kModel.TYLAgnoKriteri;
                    data.DAgnoKriteri = kModel.DAgnoKriteri;
                    data.BDAgnoKriteri = kModel.BDAgnoKriteri;
                    data.IsAlandisiBolumKisitlamasi = kModel.IsAlandisiBolumKisitlamasi;
                    data.YokOgrenciKontroluYap = kModel.YokOgrenciKontroluYap;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;
                    _entities.ProgramlarAlesEslesmeleris.RemoveRange(data.ProgramlarAlesEslesmeleris);
                    if (kModel.AlesNotuYuksekOlanAlinsin) data.ProgramlarAlesEslesmeleris = alesTipIDs.Select(s => new ProgramlarAlesEslesmeleri { AlesTipID = s }).ToList();

                    if (kModel.IsAlandisiBolumKisitlamasi)
                    {
                        _entities.ProgramlarAlandisiBolumKisitlamalaris.RemoveRange(_entities.ProgramlarAlandisiBolumKisitlamalaris.Where(p => p.ProgramKod == id));

                        _entities.ProgramlarAlandisiBolumKisitlamalaris.AddRange(ogrenciBolumIDs.Select(s => new ProgramlarAlandisiBolumKisitlamalari
                        {
                            OgrenciBolumID = s,
                            ProgramKod = id

                        }));
                    }
                }

                var onceki = _entities.KullaniciProgramlaris.Where(p => p.ProgramKod == kModel.ProgramKod).ToList();
                _entities.KullaniciProgramlaris.RemoveRange(onceki);
                foreach (var item in kullaniciId)
                {
                    _entities.KullaniciProgramlaris.Add(new KullaniciProgramlari
                    {
                        KullaniciID = item,
                        ProgramKod = id
                    });


                }
                var oncekiOb = _entities.BolumEslestirs.Where(p => p.ProgramKod == kModel.ProgramKod).ToList();
                _entities.BolumEslestirs.RemoveRange(oncekiOb);
                foreach (var item in ogrenciBolumId)
                {
                    _entities.BolumEslestirs.Add(new BolumEslestir
                    {
                        OgrenciBolumID = item,
                        ProgramKod = id,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip
                    });


                }
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }



            ViewBag.EnstituKod = enstituKod;
            ViewBag.MmMessage = mmMessage;
            ViewBag.OldID = oldId;
            ViewBag.AnabilimDaliID = new SelectList(Management.cmbGetYetkiliAnabilimDallari(true), "Value", "Caption", kModel.AnabilimDaliID);
            ViewBag.AlesTipID = new SelectList(Management.cmbGetAktifAlesTipleri(true), "Value", "Caption", kModel.AlesTipID);
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.Diller2 = new SelectList(Management.GetDiller(true), "Value", "Caption");
            var kullaniciTipList = new List<CmbIntDto>() { new CmbIntDto { }, new CmbIntDto { Value = KullaniciTipBilgi.YerliOgrenci, Caption = "Yerli Öğrenci" }, new CmbIntDto { Value = KullaniciTipBilgi.YabanciOgrenci, Caption = "Yabancı Öğrenci" } };
            ViewBag.KullaniciTipID = new SelectList(kullaniciTipList, "Value", "Caption", kModel.KullaniciTipID);
            ViewBag.KullaniciID = new SelectList(new List<CmbIntDto>(), "Value", "Caption");
            ViewBag.OgrenciBolumID = new SelectList(new List<CmbIntDto>(), "Value", "Caption");
            ViewBag.BasvuruAgnoAlimTipID = new SelectList(Management.cmbGetBasvuruAgnoAlimTipleri(true), "Value", "Caption", kModel.BasvuruAgnoAlimTipID);
            ViewBag.LYLHerhangiBirindeGecenAlanIci = new SelectList(Management.cmbAlanEslesmeData(false), "Value", "Caption", kModel.LYLHerhangiBirindeGecenAlanIci);
            var alesTipNotSecimList = new List<CmbBoolDto>() { new CmbBoolDto { Value = true, Caption = "Ales Tipleri Arasında Notu Yüksek Olan Sonuç Alınsın" }, new CmbBoolDto { Value = false, Caption = "Programa Ait Ales Tipi Notu Alınsın" } };
            ViewBag.AlesNotuYuksekOlanAlinsin = new SelectList(alesTipNotSecimList, "Value", "Caption", kModel.AlesNotuYuksekOlanAlinsin);

            ViewBag.KullaniciIDs = kullaniciId;
            ViewBag.AOgrenciBolumID = ogrenciBolumId;
            ViewBag.OgrenciBolumIDs = ogrenciBolumIDs;
            ViewBag.SAlesTipIDs = alesTipIDs;
            ViewBag.AlesTipIDs = new SelectList(new List<CmbIntDto>(), "Value", "Caption");

            return View(kModel);
        }
        public ActionResult ProgramKullanicilarYetki(int anabilimDaliId, string programKod)
        {
            var abd = _entities.AnabilimDallaris.First(p => p.AnabilimDaliID == anabilimDaliId);
            var roleName = new List<string>() { RoleNames.MulakatSureci, RoleNames.Kotalar, RoleNames.Programlar, RoleNames.BolumEslestir };
            var kuls = UserBus.GetRoluOlanKullanicilar(roleName, abd.EnstituKod);
            var rolls = KullanicilarBus.GetProgramYetkisiOlanKullanicilar(kuls, programKod).Select(s => new { Value = s.Value.KullaniciID, Caption = (s.Value.Ad + " " + s.Value.Soyad + " [" + s.Value.KullaniciAdi + "]") }).ToList();
            return Json(rolls, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetBat(int basvuruAgnoAlimTipId)
        {
            var abd = _entities.BasvuruAgnoAlimTipleris.First(p => p.BasvuruAgnoAlimTipID == basvuruAgnoAlimTipId);

            return Json(new { Show = abd.YuzdeGir }, "application/json", JsonRequestBehavior.AllowGet);
        }

        public ActionResult OBolumYetki(int anabilimDaliId, string programKod)
        {
            var abd = _entities.AnabilimDallaris.FirstOrDefault(p => p.AnabilimDaliID == anabilimDaliId);
            var odb = _entities.BolumEslestirs.Where(p => p.ProgramKod == programKod).Select(s => s.OgrenciBolumID).ToList();
            var roles = _entities.OgrenciBolumleris.Where(p => p.EnstituKod == abd.EnstituKod).OrderBy(o => o.BolumAdi).ToList();
            var dataR = roles.Select(s => new CheckObject<OgrenciBolumleri>
            {
                Value = s,
                Checked = odb.Contains(s.OgrenciBolumID)
            }).OrderByDescending(o => o.Checked).ThenBy(t => t.Value.BolumAdi).Select(s2 => new { Value = s2.Value.OgrenciBolumID, Caption = s2.Value.BolumAdi });
            return Json(dataR, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetAlesTipIDs(string programKod)
        {
            var roles = new List<int>();
            var prg = _entities.Programlars.FirstOrDefault(p => p.ProgramKod == programKod);
            if (prg != null) roles = prg.ProgramlarAlesEslesmeleris.Select(s => s.AlesTipID).ToList();
            var dataR = _entities.AlesTipleris.Select(s => new CheckObject<AlesTipleri>
            {
                Value = s,
                Checked = roles.Contains(s.AlesTipID)
            }).OrderByDescending(o => o.Checked).ThenBy(t => t.Value.AlesTipAdi).Select(s2 => new { Value = s2.Value.AlesTipID, Caption = s2.Value.AlesTipAdi });
            return Json(dataR, "application/json", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Sil(string id)
        {
            var kayit = _entities.Programlars.FirstOrDefault(p => p.ProgramKod == id);
            var pAdi = _entities.Programlars.First(p => p.ProgramKod == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + pAdi.ProgramAdi + "' İsimli Program Silindi!";
                    _entities.Programlars.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + pAdi.ProgramAdi + "' İsimli Program Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Programlar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Program sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
