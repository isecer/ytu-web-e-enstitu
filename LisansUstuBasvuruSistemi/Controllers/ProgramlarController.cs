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

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Programlar)]
    public class ProgramlarController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {
            var sEkod = Management.getSelectedEnstitu(EKD);
            return Index(new fmProgramlar { PageSize = 15, EnstituKod = sEkod, Expand = false });
        }
        [HttpPost]
        public ActionResult Index(fmProgramlar model)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in db.Programlars
                    join sl in db.Programlars on new { s.ProgramKod } equals new { sl.ProgramKod } into defP
                    from slP in defP.DefaultIfEmpty()
                    join e in db.AnabilimDallaris on s.AnabilimDaliID equals e.AnabilimDaliID
                    join at in db.AlesTipleris on new { s.AlesTipID } equals new { at.AlesTipID }
                    join enst in db.Enstitulers on new { e.EnstituKod } equals new { enst.EnstituKod }
                    where EnstKods.Contains(enst.EnstituKod)
                    select new frProgramlar
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
                        AlandisiBolumKisitListesi = s.ProgramlarAlandisiBolumKisitlamalaris.Select(s => s.OgrenciBolumleri.BolumAdi).OrderBy(o => o).ToList(),
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
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderBy(o => o.AnabilimDaliAdi).ThenBy(o => o.ProgramAdi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            ViewBag.IndexModel = IndexModel;

            ViewBag.EnstituKod = new SelectList(Management.cmbGetAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.AlesTipID = new SelectList(Management.cmbGetAktifAlesTipleri(true), "Value", "Caption", model.AlesTipID);
            var KullaniciTipList = new List<CmbIntDto>() { new CmbIntDto { }, new CmbIntDto { Value = KullaniciTipBilgi.YerliOgrenci, Caption = "Yerli Öğrenci" }, new CmbIntDto { Value = KullaniciTipBilgi.YabanciOgrenci, Caption = "Yabancı Öğrenci" } };
            ViewBag.KullaniciTipID = new SelectList(KullaniciTipList, "Value", "Caption", model.KullaniciTipID);
            ViewBag.Ucretli = new SelectList(Management.cmbVarYokData(true), "Value", "Caption", model.Ucretli);
            var AlesTipNotSecimList = new List<CmbBoolDto>() { new CmbBoolDto { }, new CmbBoolDto { Value = true, Caption = "Ales Tipleri Arasında Notu Yüksek Olan Sonuç Alınsın" }, new CmbBoolDto { Value = false, Caption = "Programa Ait Ales Tipi Notu Alınsın" } };
            ViewBag.AlesNotuYuksekOlanAlinsin = new SelectList(AlesTipNotSecimList, "Value", "Caption", model.AlesNotuYuksekOlanAlinsin);
            ViewBag.LYLHerhangiBirindeGecenAlanIci = new SelectList(Management.cmbAlanEslesmeData(true), "Value", "Caption", model.LYLHerhangiBirindeGecenAlanIci);
            ViewBag.ProgramSecimiEkBilgi = new SelectList(Management.cmbEvetHayirData(true), "Value", "Caption", model.ProgramSecimiEkBilgi);
            ViewBag.IsAlandisiBolumKisitlamasi = new SelectList(Management.cmbVarYokData(true), "Value", "Caption", model.IsAlandisiBolumKisitlamasi);
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(string id, string EKD)
        {
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            string _EnstituKod = Management.getSelectedEnstitu(EKD);
            var model = new Programlar();
            List<int> SAlesTipIDs = null;
            var OgrenciBolumIDs = new List<int>();
            if (id.IsNullOrWhiteSpace() == false)
            {
                var data = db.Programlars.Where(p => p.ProgramKod == id).FirstOrDefault();
                if (data != null)
                {
                    model = data;

                    OgrenciBolumIDs = data.ProgramlarAlandisiBolumKisitlamalaris.Select(s => s.OgrenciBolumID).Distinct().ToList();
                    SAlesTipIDs = data.ProgramlarAlesEslesmeleris.Select(s => s.AlesTipID).ToList();
                    _EnstituKod = data.AnabilimDallari.EnstituKod;


                }
            }
            else model.LYLHerhangiBirindeGecenAlanIci = true;

            ViewBag.AnabilimDaliID = new SelectList(Management.cmbGetYetkiliAnabilimDallari(true), "Value", "Caption", model.AnabilimDaliID);
            ViewBag.AlesTipID = new SelectList(Management.cmbGetAktifAlesTipleri(true), "Value", "Caption", model.AlesTipID);
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.Diller2 = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.KullaniciID = new SelectList(new List<CmbIntDto>(), "Value", "Caption");
            var KullaniciTipList = new List<CmbIntDto>() { new CmbIntDto { }, new CmbIntDto { Value = KullaniciTipBilgi.YerliOgrenci, Caption = "Yerli Öğrenci" }, new CmbIntDto { Value = KullaniciTipBilgi.YabanciOgrenci, Caption = "Yabancı Öğrenci" } };
            ViewBag.KullaniciTipID = new SelectList(KullaniciTipList, "Value", "Caption", model.KullaniciTipID);
            ViewBag.LYLHerhangiBirindeGecenAlanIci = new SelectList(Management.cmbAlanEslesmeData(false), "Value", "Caption", model.LYLHerhangiBirindeGecenAlanIci);
            ViewBag.OgrenciBolumID = new SelectList(new List<CmbIntDto>(), "Value", "Caption");
            ViewBag.BasvuruAgnoAlimTipID = new SelectList(Management.cmbGetBasvuruAgnoAlimTipleri(true), "Value", "Caption", model.BasvuruAgnoAlimTipID);
            var AlesTipNotSecimList = new List<CmbBoolDto>() { new CmbBoolDto { Value = true, Caption = "Ales Tipleri Arasında Notu Yüksek Olan Sonuç Alınsın" }, new CmbBoolDto { Value = false, Caption = "Programa Ait Ales Tipi Notu Alınsın" } };
            ViewBag.AlesNotuYuksekOlanAlinsin = new SelectList(AlesTipNotSecimList, "Value", "Caption", model.AlesNotuYuksekOlanAlinsin);

            var _roleName = new List<string>() { RoleNames.MulakatSureci, RoleNames.Kotalar, RoleNames.Programlar, RoleNames.BolumEslestir };
            var kuls = UserBus.GetRoluOlanKullanicilar(_roleName, _EnstituKod);
            var rolls = KullanicilarBus.GetProgramYetkisiOlanKullanicilar(kuls, model.ProgramKod);
            ViewBag.KullaniciIDs = rolls.Where(p => p.Checked == true).Select(s => s.Value.KullaniciID).ToList();

            var oBolIds = db.BolumEslestirs.Where(p => p.ProgramKod == model.ProgramKod).Select(s => s.OgrenciBolumID).ToList();

            ViewBag.AOgrenciBolumID = oBolIds;
            ViewBag.OgrenciBolumIDs = OgrenciBolumIDs;
            ViewBag.AlesTipIDs = new SelectList(new List<CmbIntDto>(), "Value", "Caption");
            ViewBag.SAlesTipIDs = SAlesTipIDs;
            ViewBag.OldID = model.ProgramKod;
            ViewBag.EnstituKod = _EnstituKod;
            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Kayit(Programlar kModel, string EnstituKod, string OldID, List<int> KullaniciID, List<int> OgrenciBolumID, List<int> OgrenciBolumIDs, List<int> AlesTipIDs)
        {
            var MmMessage = new MmMessage();
            string ID = OldID.IsNullOrWhiteSpace() ? kModel.ProgramKod : OldID;
            AlesTipIDs = AlesTipIDs ?? new List<int>();
            #region Kontrol 

            if (KullaniciID == null) KullaniciID = new List<int>();
            if (OgrenciBolumID == null) OgrenciBolumID = new List<int>();
            if (OgrenciBolumIDs == null) OgrenciBolumIDs = new List<int>();

            if (ID.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Program kodu boş bırakılamaz ve 0 dan büyük bir değer olmalıdır!");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProgramKod" });
            if (kModel.AnabilimDaliID <= 0)
            {
                MmMessage.Messages.Add("Anabilim Dalı seçiniz");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnabilimDaliID" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AnabilimDaliID" });
            if (kModel.AlesTipID <= 0)
            {
                MmMessage.Messages.Add("Ales Tipi seçiniz");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AlesTipID" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AlesTipID" });
            if (kModel.ProgramKod.IsNullOrWhiteSpace() && OldID.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Kayıt işlemini yapabilmeni için Kod kısmını doldurmanız gerekmektedir!");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProgramKod" });
            if (kModel.KullaniciTipID <= 0)
            {
                MmMessage.Messages.Add("Kayıt işlemini yapabilmeniz öğrenci tipi seçiniz!");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KullaniciTipID" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "KullaniciTipID" });
            if (kModel.Ucretli)
            {
                if (kModel.Ucret.HasValue == false)
                {
                    MmMessage.Messages.Add("Ücretli seçilen programın ücret bilgisinin girilmesi gerekmektedir!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Ucret" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Ucret" });

            }
            if (kModel.AlesNotuYuksekOlanAlinsin)
            {
                if (AlesTipIDs.Count == 0)
                {
                    MmMessage.Messages.Add("Ales notu yüksek olan seçeneğini seçebilmeniz için sağındaki kutucukta bulunan Ales tiplerini seçiniz!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AlesTipIDs" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AlesTipIDs" });

            }
            if (kModel.BasvuruAgnoAlimTipID.HasValue)
            {
                if (kModel.BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.L_YLYuzdeBelirlensin)
                {
                    if (kModel.LYuzdeOran.HasValue == false)
                    {
                        MmMessage.Messages.Add("Lisans % oran bilgisinin girilmesi gerekmektedir!");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LYuzdeOran" });
                    }
                    else if (kModel.YLYuzdeOran.HasValue == false)
                    {
                        MmMessage.Messages.Add("Yüksek Lisans % oran bilgisinin girilmesi gerekmektedir!");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLYuzdeOran" });
                    }
                    else
                    {
                        if (kModel.LYuzdeOran + kModel.YLYuzdeOran != 100)
                        {
                            MmMessage.Messages.Add("Lisans % oranı ve Yüksek Lisans % oranı toplamı 100 e eşit olacak şekilde giriniz!");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LYuzdeOran" });
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLYuzdeOran" });
                        }
                    }

                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvuruAgnoAlimTipID" });

            }
            if (kModel.YLAgnoKriteri.HasValue)
            {
                if (!(kModel.YLAgnoKriteri.Value >= 0 && kModel.YLAgnoKriteri.Value <= 100))
                {
                    MmMessage.Messages.Add("Girilecek Tezsiz Y.Lisans Min Agno not kriteri 0 ile 100 arasında olmalıdır");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLAgnoKriteri" });
                }
            }
            if (kModel.TYLAgnoKriteri.HasValue)
            {
                if (!(kModel.TYLAgnoKriteri.Value >= 0 && kModel.TYLAgnoKriteri.Value <= 100))
                {
                    MmMessage.Messages.Add("Girilecek Yüksek Tezli Y.Lisans Min Agno not kriteri 0 ile 100 arasında olmalıdır");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TYLAgnoKriteri" });
                }
            }
            if (kModel.DAgnoKriteri.HasValue)
            {
                if (!(kModel.DAgnoKriteri.Value >= 0 && kModel.DAgnoKriteri.Value <= 100))
                {
                    MmMessage.Messages.Add("Girilecek Doktora Min Agno not kriteri 0 ile 100 arasında olmalıdır");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DAgnoKriteri" });
                }
            }
            if (kModel.ProgramSecimiEkBilgi)
            {
                if (kModel.Aciklama.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("Açıklama Giriniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Aciklama" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Aciklama" });

            }
            if (kModel.IsAlandisiBolumKisitlamasi && OgrenciBolumIDs.Count == 0)
            {
                MmMessage.Messages.Add("Alan dışı öğrenci bölümü kısıtlaması seçeneği aktif edildiğinde kabul edilecek en az 1 öğrenci bölümünü listeden seçmeniz gerekmektedir.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsAlandisiBolumKisitlamasi" });
            }

            if (MmMessage.Messages.Count == 0)
            {
                int NewOrEd = OldID.IsNullOrWhiteSpace() ? 1 : 0;
                var cnt = db.Programlars.Where(p => p.ProgramKod == ID).Count() + NewOrEd;
                if (cnt > 1)
                {
                    MmMessage.Messages.Add("Tanımlamak istediğiniz kod daha önceden sisteme tanımlanmıştır, tekrar tanımlanamaz!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                }
            }


            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                var bolm = db.AnabilimDallaris.Where(p => p.AnabilimDaliID == kModel.AnabilimDaliID).First();
                if (OldID.IsNullOrWhiteSpace())
                {
                    var prg = db.Programlars.Add(new Programlar
                    {
                        AnabilimDaliID = kModel.AnabilimDaliID,
                        AnabilimDaliKod = bolm.AnabilimDaliKod,
                        ProgramKod = ID,
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
                    if (kModel.AlesNotuYuksekOlanAlinsin) prg.ProgramlarAlesEslesmeleris = AlesTipIDs.Select(s => new ProgramlarAlesEslesmeleri { AlesTipID = s }).ToList();
                    db.SaveChanges();
                    ID = prg.ProgramKod;
                }
                else
                {
                    var data = db.Programlars.Where(p => p.ProgramKod == ID).First();
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
                    db.ProgramlarAlesEslesmeleris.RemoveRange(data.ProgramlarAlesEslesmeleris);
                    if (kModel.AlesNotuYuksekOlanAlinsin) data.ProgramlarAlesEslesmeleris = AlesTipIDs.Select(s => new ProgramlarAlesEslesmeleri { AlesTipID = s }).ToList();

                    if (kModel.IsAlandisiBolumKisitlamasi)
                    {
                        db.ProgramlarAlandisiBolumKisitlamalaris.RemoveRange(db.ProgramlarAlandisiBolumKisitlamalaris.Where(p => p.ProgramKod == ID));

                        db.ProgramlarAlandisiBolumKisitlamalaris.AddRange(OgrenciBolumIDs.Select(s => new ProgramlarAlandisiBolumKisitlamalari
                        {
                            OgrenciBolumID = s,
                            ProgramKod = ID

                        }));
                    }
                }

                var Onceki = db.KullaniciProgramlaris.Where(p => p.ProgramKod == kModel.ProgramKod).ToList();
                db.KullaniciProgramlaris.RemoveRange(Onceki);
                foreach (var item in KullaniciID)
                {
                    db.KullaniciProgramlaris.Add(new KullaniciProgramlari
                    {
                        KullaniciID = item,
                        ProgramKod = ID
                    });


                }
                var OncekiOb = db.BolumEslestirs.Where(p => p.ProgramKod == kModel.ProgramKod).ToList();
                db.BolumEslestirs.RemoveRange(OncekiOb);
                foreach (var item in OgrenciBolumID)
                {
                    db.BolumEslestirs.Add(new BolumEslestir
                    {
                        OgrenciBolumID = item,
                        ProgramKod = ID,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip
                    });


                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }



            ViewBag.EnstituKod = EnstituKod;
            ViewBag.MmMessage = MmMessage;
            ViewBag.OldID = OldID;
            ViewBag.AnabilimDaliID = new SelectList(Management.cmbGetYetkiliAnabilimDallari(true), "Value", "Caption", kModel.AnabilimDaliID);
            ViewBag.AlesTipID = new SelectList(Management.cmbGetAktifAlesTipleri(true), "Value", "Caption", kModel.AlesTipID);
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.Diller2 = new SelectList(Management.GetDiller(true), "Value", "Caption");
            var KullaniciTipList = new List<CmbIntDto>() { new CmbIntDto { }, new CmbIntDto { Value = KullaniciTipBilgi.YerliOgrenci, Caption = "Yerli Öğrenci" }, new CmbIntDto { Value = KullaniciTipBilgi.YabanciOgrenci, Caption = "Yabancı Öğrenci" } };
            ViewBag.KullaniciTipID = new SelectList(KullaniciTipList, "Value", "Caption", kModel.KullaniciTipID);
            ViewBag.KullaniciID = new SelectList(new List<CmbIntDto>(), "Value", "Caption");
            ViewBag.OgrenciBolumID = new SelectList(new List<CmbIntDto>(), "Value", "Caption");
            ViewBag.BasvuruAgnoAlimTipID = new SelectList(Management.cmbGetBasvuruAgnoAlimTipleri(true), "Value", "Caption", kModel.BasvuruAgnoAlimTipID);
            ViewBag.LYLHerhangiBirindeGecenAlanIci = new SelectList(Management.cmbAlanEslesmeData(false), "Value", "Caption", kModel.LYLHerhangiBirindeGecenAlanIci);
            var AlesTipNotSecimList = new List<CmbBoolDto>() { new CmbBoolDto { Value = true, Caption = "Ales Tipleri Arasında Notu Yüksek Olan Sonuç Alınsın" }, new CmbBoolDto { Value = false, Caption = "Programa Ait Ales Tipi Notu Alınsın" } };
            ViewBag.AlesNotuYuksekOlanAlinsin = new SelectList(AlesTipNotSecimList, "Value", "Caption", kModel.AlesNotuYuksekOlanAlinsin);

            ViewBag.KullaniciIDs = KullaniciID;
            ViewBag.AOgrenciBolumID = OgrenciBolumID;
            ViewBag.OgrenciBolumIDs = OgrenciBolumIDs;
            ViewBag.SAlesTipIDs = AlesTipIDs;
            ViewBag.AlesTipIDs = new SelectList(new List<CmbIntDto>(), "Value", "Caption");

            return View(kModel);
        }
        public ActionResult ProgramKullanicilarYetki(int AnabilimDaliID, string ProgramKod)
        {
            var abd = db.AnabilimDallaris.Where(p => p.AnabilimDaliID == AnabilimDaliID).First();
            var _roleName = new List<string>() { RoleNames.MulakatSureci, RoleNames.Kotalar, RoleNames.Programlar, RoleNames.BolumEslestir };
            var kuls = UserBus.GetRoluOlanKullanicilar(_roleName, abd.EnstituKod);
            var rolls = KullanicilarBus.GetProgramYetkisiOlanKullanicilar(kuls, ProgramKod).Select(s => new { Value = s.Value.KullaniciID, Caption = (s.Value.Ad + " " + s.Value.Soyad + " [" + s.Value.KullaniciAdi + "]") }).ToList();
            return Json(rolls, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult getBAT(int BasvuruAgnoAlimTipID)
        {
            var abd = db.BasvuruAgnoAlimTipleris.Where(p => p.BasvuruAgnoAlimTipID == BasvuruAgnoAlimTipID).First();

            return Json(new { Show = abd.YuzdeGir }, "application/json", JsonRequestBehavior.AllowGet);
        }

        public ActionResult OBolumYetki(int AnabilimDaliID, string ProgramKod)
        {
            var abd = db.AnabilimDallaris.Where(p => p.AnabilimDaliID == AnabilimDaliID).FirstOrDefault();
            var odb = db.BolumEslestirs.Where(p => p.ProgramKod == ProgramKod).Select(s => s.OgrenciBolumID).ToList();
            var roles = db.OgrenciBolumleris.Where(p => p.EnstituKod == abd.EnstituKod).OrderBy(o => o.BolumAdi).ToList();
            var dataR = roles.Select(s => new CheckObject<OgrenciBolumleri>
            {
                Value = s,
                Checked = odb.Contains(s.OgrenciBolumID)
            }).OrderByDescending(o => o.Checked).ThenBy(t => t.Value.BolumAdi).Select(s2 => new { Value = s2.Value.OgrenciBolumID, Caption = s2.Value.BolumAdi });
            return Json(dataR, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetAlesTipIDs(string ProgramKod)
        {
            var roles = new List<int>();
            var prg = db.Programlars.Where(p => p.ProgramKod == ProgramKod).FirstOrDefault();
            if (prg != null) roles = prg.ProgramlarAlesEslesmeleris.Select(s => s.AlesTipID).ToList();
            var dataR = db.AlesTipleris.Select(s => new CheckObject<AlesTipleri>
            {
                Value = s,
                Checked = roles.Contains(s.AlesTipID)
            }).OrderByDescending(o => o.Checked).ThenBy(t => t.Value.AlesTipAdi).Select(s2 => new { Value = s2.Value.AlesTipID, Caption = s2.Value.AlesTipAdi });
            return Json(dataR, "application/json", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Sil(string id)
        {
            var kayit = db.Programlars.Where(p => p.ProgramKod == id).FirstOrDefault();
            var PAdi = db.Programlars.Where(p => p.ProgramKod == id).First();
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + PAdi.ProgramAdi + "' İsimli Program Silindi!";
                    db.Programlars.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + PAdi.ProgramAdi + "' İsimli Program Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "Programlar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Program sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
