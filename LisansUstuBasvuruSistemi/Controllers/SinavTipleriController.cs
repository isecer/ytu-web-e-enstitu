using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.SinavTipleri)]
    public class SinavTipleriController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmSinavTipleri { PageSize = 15 });
        }
        [HttpPost]
        public ActionResult Index(fmSinavTipleri model)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();

            var q = from s in db.SinavTipleris
                    join sl in db.SinavTipleris on s.SinavTipID equals sl.SinavTipID
                    join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                    where EnstKods.Contains(s.EnstituKod)
                    select new frSinavTipleri
                    {
                        SinavTipID = s.SinavTipID,
                        SinavTipKod = s.SinavTipKod,
                        SinavAdi = sl.SinavAdi,

                        EnstituKod = s.EnstituKod,
                        TarihGirisMaxGecmisYil = s.TarihGirisMaxGecmisYil,
                        EnstituAd = e.EnstituAd,
                        SinavTipGrupID = s.SinavTipGrupID,
                        SinavTipGrupAdi = s.SinavTipGruplari.SinavTipGrupAdi,
                        //LocalService=s.LocalService,
                        WebService = s.WebService,
                        WebServiceKod = s.WebServiceKod,
                        OzelTarih = s.OzelTarih,
                        OzelTarihTipID = s.OzelTarihTipID,
                        Tarih1 = s.Tarih1,
                        Tarih2 = s.Tarih2,
                        OzelNot = s.OzelNot,
                        OzelNotTipID = s.OzelNotTipID,
                        KusuratVar = s.KusuratVar,
                        Min = s.Min,
                        Max = s.Max,
                        NotDonusum = s.NotDonusum,
                        NotDonusumFormulu = s.NotDonusumFormulu,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapan = s.Kullanicilar.KullaniciAdi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        //SinavNotlaris = s.SinavNotlaris.ToList(),
                        //SinavTiplerSubSinavAraliks = s.SinavTiplerSubSinavAraliks.ToList(),
                        //SinavTarihleris = s.SinavTarihleris.ToList(),
                        //krSinavTipleriDonems = (from sq in s.SinavTipleriDonems
                        //                        join dn in db.Donemlers on sq.WsDonemKod equals dn.Donemler.WsDonemKod
                        //                        select new krSinavTipleriDonems
                        //                        {
                        //                            SinavTipID = sq.SinavTipID,
                        //                            SinavTipDonemID = sq.SinavTipDonemID,
                        //                            Yil = sq.Yil,
                        //                            WsDonemKod = sq.WsDonemKod,
                        //                            DonemAdi = dn.DonemAdi,
                        //                            IsTaahhutVar = sq.IsTaahhutVar,
                        //                        }).ToList(),

                        //SinavTipleriOTNotAraliklariList = (from s2 in s.SinavTipleriOTNotAraliklaris.Where(p => p.EnstituKod == s.EnstituKod && p.SinavTipID == s.SinavTipID)
                        //                                   join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == s.EnstituKod) on s2.OgrenimTipKod equals ot.OgrenimTipKod
                        //                                   join otl in db.OgrenimTipleris on ot.OgrenimTipID equals otl.OgrenimTipID
                        //                                   select new krSinavTipleriOTNotAraliklari
                        //                                   {
                        //                                       OgrenimTipKod = s2.OgrenimTipKod,
                        //                                       OgrenimTipAdi = otl.OgrenimTipAdi,
                        //                                       Ingilizce = s2.Ingilizce,
                        //                                       Min = s2.Min,
                        //                                       Max = s2.Max
                        //                                   }).ToList()
                    };
            q = q.Where(p => UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.SinavAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.SinavAdi.Contains(model.SinavAdi));
            if (model.SinavTipGrupID.HasValue) q = q.Where(p => p.SinavTipGrupID == model.SinavTipGrupID.Value);
            if (model.WebService.HasValue) q = q.Where(p => p.WebService == model.WebService.Value);
            if (model.OzelTarih.HasValue) q = q.Where(p => p.OzelTarih == model.OzelTarih.Value);
            if (model.OzelNot.HasValue) q = q.Where(p => p.OzelNot == model.OzelNot.Value);
            if (model.KusuratVar.HasValue) q = q.Where(p => p.KusuratVar == model.KusuratVar.Value);
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif.Value);
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderBy(o => o.EnstituAd).ThenBy(o => o.SinavAdi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            ViewBag.IndexModel = IndexModel;
            ViewBag.EnstituKod = new SelectList(Management.cmbGetAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.SinavTipGrupID = new SelectList(Management.cmbGetSinavTipGruplari(true), "Value", "Caption", model.SinavTipGrupID);
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string EKD)
        {
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            var model = new KmSinavTipleri();
            model.EnstituKod = Management.getSelectedEnstitu(EKD);
            if (id.HasValue)
            {
                var data = db.SinavTipleris.Where(p => p.SinavTipID == id).Select(s => new KmSinavTipleri
                {
                    SinavTipID = s.SinavTipID,
                    EnstituKod = s.EnstituKod,
                    SinavTipGrupID = s.SinavTipGrupID,
                    SinavTipKod = s.SinavTipKod,
                    SinavAdi = s.SinavAdi,
                    WebService = s.WebService,
                    WebServiceKod = s.WebServiceKod,
                    WsSinavCekimTipID = s.WsSinavCekimTipID,
                    OzelTarih = s.OzelTarih,
                    OzelTarihTipID = s.OzelTarihTipID,
                    Tarih1 = s.Tarih1,
                    Tarih2 = s.Tarih2,
                    TarihGirisMaxGecmisYil = s.TarihGirisMaxGecmisYil,
                    OzelNot = s.OzelNot,
                    OzelNotTipID = s.OzelNotTipID,
                    NotDonusum = s.NotDonusum,
                    NotDonusumFormulu = s.NotDonusumFormulu,
                    KusuratVar = s.KusuratVar,
                    Min = s.Min,
                    Max = s.Max,
                    GIsTaahhutVar = s.GIsTaahhutVar,
                    IsAktif = s.IsAktif,
                    SinavTipleriDils = s.SinavTipleriDils,
                    SinavTipleriDonems = s.SinavTipleriDonems,
                    SinavTarihleris = s.SinavTarihleris,
                    SinavNotlaris = s.SinavNotlaris,
                    SinavTiplerSubSinavAraliks = s.SinavTiplerSubSinavAraliks,
                    SinavTipleriOTNotAraliklaris = s.SinavTipleriOTNotAraliklaris,


                }).FirstOrDefault();
                if (data != null)
                {
                    model.EnstituKod = data.EnstituKod;
                    model = data;
                }
            }

            var OgrenimTipleris = db.OgrenimTipleris.Where(p => p.EnstituKod == model.EnstituKod && p.IsAktif).ToList();
            var SinavTipleriOTNotAraliklaris = new List<SinavTipleriOTNotAraliklari>();
            var Dils = new List<bool> { true, false };
            foreach (var itemD in Dils)
            {
                foreach (var itemOt in OgrenimTipleris)
                {
                    var Kayit = model.SinavTipleriOTNotAraliklaris.Where(p => p.OgrenimTipKod == itemOt.OgrenimTipKod && p.Ingilizce == itemD).FirstOrDefault();
                    SinavTipleriOTNotAraliklaris.Add(
                         new SinavTipleriOTNotAraliklari
                         {
                             EnstituKod = model.EnstituKod,
                             SinavTipID = model.SinavTipID,
                             OgrenimTipKod = itemOt.OgrenimTipKod,
                             IsGecerli = Kayit != null ? Kayit.IsGecerli : false,
                             IsIstensin = Kayit != null ? Kayit.IsIstensin : false,
                             Ingilizce = itemD,
                             IsOzelNotAralik = Kayit != null ? Kayit.IsOzelNotAralik : false,
                             Min = Kayit != null ? Kayit.Min : null,
                             Max = Kayit != null ? Kayit.Max : null,
                             SinavTipleriOTNotAraliklariGecersizProgramlars = Kayit != null ? Kayit.SinavTipleriOTNotAraliklariGecersizProgramlars : new List<SinavTipleriOTNotAraliklariGecersizProgramlar>()

                         });
                }
            }
            model.SinavTipleriOTNotAraliklaris = SinavTipleriOTNotAraliklaris;


            ViewBag.OgrenimTipleris = OgrenimTipleris;
            ViewBag.SinavDilleris = db.SinavDilleris.ToList();
            ViewBag.Programlars = db.Programlars.Where(p => p.AnabilimDallari.EnstituKod == model.EnstituKod).OrderBy(o => o.ProgramAdi).ToList();
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.SinavTipGrupID = new SelectList(Management.cmbGetSinavTipGruplari(true), "Value", "Caption", model.SinavTipGrupID);
            ViewBag.OzelTarihTipID = new SelectList(Management.cmbGetOzelTarihTipleri(true), "Value", "Caption", model.OzelTarihTipID);
            ViewBag.OzelNotTipID = new SelectList(Management.cmbGetOzelNotTipleri(true), "Value", "Caption", model.OzelNotTipID);
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.WsSinavCekimTipID = new SelectList(Management.cmbGetWsSinavCekimTipleri(true, WsCekimTipi.Donemsel), "Value", "Caption", model.WsSinavCekimTipID);
            ViewBag.SWsDonemKod = new SelectList(Management.cmbGetWsSinavCekimTipDetay(model.WsSinavCekimTipID ?? 0, false), "Value", "Caption");
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(model.EnstituKod), "Value", "Caption");
            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Kayit(KmSinavTipleri kModel)
        {

            var MmMessage = new MmMessage();

            #region SetDetayData
            //Sınav Puanı Bilgileri  
            var qWebServisYil = kModel.Yil.Select((s, inx) => new { s, inx }).ToList();
            var qIsTaahhutVar = kModel.IsTaahhutVar.Select((s, inx) => new { s, inx }).ToList();
            var qWebServisDonemlari = (from snWsY in qWebServisYil
                                       join th in qIsTaahhutVar on snWsY.inx equals th.inx
                                       select new krSinavTipleriDonems
                                       {
                                           WsDonemKod = null,
                                           Yil = snWsY.s,
                                           IsTaahhutVar = th.s
                                       }).ToList();
 


            //Sınav Tarihi Bilgileri
            var qSinavTarihleriID = kModel.SinavTarihleriID.Select((s, inx) => new { s, inx }).ToList();
            var qSinavTarihi = kModel.SinavTarihi.Select((s, inx) => new { s, inx }).ToList();
            var qSinavTarihleri = (from stID in qSinavTarihleriID
                                   join stTar in qSinavTarihi on stID.inx equals stTar.inx
                                   select new
                                   {
                                       Index = stID.inx,
                                       SinavTarihleriID = stID.s,
                                       SinavTarihi = stTar.s
                                   }).ToList();
            //Sınav Puanı Bilgileri
            var qSinavNotlariID = kModel.SinavNotlariID.Select((s, inx) => new { s, inx }).ToList();
            var qSinavNotAdi = kModel.SinavNotAdi.Select((s, inx) => new { s, inx }).ToList();
            var qSinavNotDeger = kModel.SinavNotDeger.Select((s, inx) => new { s, inx }).ToList();
            var qSinavNotlari = (from snID in qSinavNotlariID
                                 join snAd in qSinavNotAdi on snID.inx equals snAd.inx
                                 join snNot in qSinavNotDeger on snID.inx equals snNot.inx
                                 select new
                                 {
                                     Index = snID.inx,
                                     SinavNotlariID = snID.s,
                                     SinavNotAdi = snAd.s,
                                     SinavNotDeger = snNot.s
                                 }).ToList();

            var qSubSinavAralikID = kModel.SubSinavAralikID.Select((s, inx) => new { s, inx }).ToList();
            var qSubSinavAralikAdi = kModel.SubSinavAralikAdi.Select((s, inx) => new { s, inx }).ToList();
            var qSubSinavMin = kModel.SubSinavMin.Select((s, inx) => new { s, inx }).ToList();
            var qSubSinavMax = kModel.SubSinavMax.Select((s, inx) => new { s, inx }).ToList();
            var qNotDonusumFormulu = kModel.SubNotDonusumFormulu.Select((s, inx) => new { s, inx }).ToList();
            var qNotDonusum = kModel.SubNotDonusum.Select((s, inx) => new { s, inx }).ToList();
            var qSubSinavAralik = (from snID in qSubSinavAralikID
                                   join snAd in qSubSinavAralikAdi on snID.inx equals snAd.inx
                                   join snNotmin in qSubSinavMin on snID.inx equals snNotmin.inx
                                   join snNotmax in qSubSinavMax on snID.inx equals snNotmax.inx
                                   join ndonusum in qNotDonusum on snID.inx equals ndonusum.inx
                                   join formul in qNotDonusumFormulu on snID.inx equals formul.inx
                                   select new
                                   {
                                       Index = snID.inx,
                                       SubSinavAralikID = snID.s,
                                       SubSinavAralikAdi = snAd.s,
                                       SubSinavMin = snNotmin.s,
                                       SubSinavMax = snNotmax.s,
                                       NotDonusum = ndonusum.s,
                                       NotDonusumFormulu = formul.s
                                   }).ToList();

            //Sinav ogrenimTip Not Araliklari
            var qNAOgrenimTipKod = kModel.NAOgrenimTipKod.Select((s, inx) => new { s, inx }).ToList();
            var qNAIngilizce = kModel.NAIngilizce.Select((s, inx) => new { s, inx }).ToList();
            var qNAIsIstensin = kModel.NAIsIstensin.Select((s, inx) => new { s = (s == 1 ? true : false), inx }).ToList();
            var qNAIsGecerli = kModel.NAIsGecerli.Select((s, inx) => new { s = (s == 1 ? true : false), inx }).ToList();
            var qNAMin = kModel.NAMin.Select((s, inx) => new { s, inx }).ToList();
            var qNAMax = kModel.NAMax.Select((s, inx) => new { s, inx }).ToList();
            var qIPProgramKod = kModel.IPProgramKod.Select(s => new { ID = s.Split('_')[0], ProgramKod = s.Split('_')[1] }).ToList();

            var qNAOgrenimTipKodNotAralik = (from ot in qNAOgrenimTipKod
                                             join ing in qNAIngilizce on ot.inx equals ing.inx
                                             join gcr in qNAIsGecerli on ot.inx equals gcr.inx
                                             join ist in qNAIsIstensin on ot.inx equals ist.inx
                                             join min in qNAMin on ot.inx equals min.inx
                                             join max in qNAMax on ot.inx equals max.inx
                                             join otl in db.OgrenimTipleris on ot.s equals otl.OgrenimTipID
                                             select new krSinavTipleriOTNotAraliklari
                                             {
                                                 OgrenimTipKod = ot.s,
                                                 OgrenimTipAdi = otl.OgrenimTipAdi,
                                                 Ingilizce = ing.s,
                                                 IsGecerli = gcr.s,
                                                 IsIstensin = ist.s,
                                                 IsOzelNotAralik = ist.s && (min.s.HasValue && max.s.HasValue),
                                                 Min = min.s,
                                                 Max = max.s,
                                                 ProgramKods = qIPProgramKod.Where(p => p.ID == ("ProgramKod-" + ot.s + "-" + (ing.s ? "En" : "Tr"))).Select(s => s.ProgramKod).ToList()
                                             }).ToList();
            #endregion

            #region kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Enstitü seçiniz");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });

            if (kModel.SinavTipGrupID <= 0)
            {
                MmMessage.Messages.Add("Sınav tip grup bilgisini seçiniz!");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SinavTipGrupID" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SinavTipGrupID" });
            if (kModel.SinavDilIDs.Count <= 0)
            {
                MmMessage.Messages.Add("Sınav tipine ait Sınav Dillerini Seçiniz!");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SinavDilIDs" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SinavDilIDs" });
            if (kModel.SinavTipKod <= 0)
            {
                MmMessage.Messages.Add("Sınav tip kodu bilgisini giriniz!");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SinavTipKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SinavTipKod" });
            if (kModel.SinavAdi.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Sınav Adı bilgisini giriniz!");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SinavAdi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SinavAdi" });
            if (kModel.WebService)
            {
                if (kModel.WsSinavCekimTipID.HasValue == false)
                {
                    MmMessage.Messages.Add("Web servisi veri çekim tipini seçiniz!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "WsSinavCekimTipID" });
                }
                else
                {
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "WsSinavCekimTipID" });
                    if (kModel.WsSinavCekimTipID == WsCekimTipi.Tarih)
                    {

                        //if (qWebservisiLocalDonem.Count == 0)
                        //{
                        //    string msg = "Web servisi için tarih bilgisinin girilmesi zorunludur!";
                        //    MmMessage.Messages.Add(msg);
                        //    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "WebService" });
                        //}
                        //else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "WebService" });
                    }
                    else
                    {
                        if (kModel.WebServiceKod.IsNullOrWhiteSpace())
                        {
                            MmMessage.Messages.Add("Web servisi seçeneği işaretli olan Sınav tiplerinin Web Servisi Kodu tanımlanması zorunludur!");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "WebServiceKod" });
                        }
                        else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "WebServiceKod" });
                        if (qWebServisDonemlari.Count == 0)
                        {
                            MmMessage.Messages.Add("Web servisi için Yıl bilgilerinin girilmesi zorunludur!");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "WebService" });
                        }
                        else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "WebService" });
                    }
                }
                if (kModel.TarihGirisMaxGecmisYil.HasValue == false)
                {
                    MmMessage.Messages.Add("Web servisi seçeneği işaretli olan Sınav tiplerinin Maks Geçmiş Yıl Kabul bilgisinin tanımlanması zorunludur!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TarihGirisMaxGecmisYil" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TarihGirisMaxGecmisYil" });



            }
            else
            {
                if (kModel.OzelTarih)
                {

                    if (kModel.OzelTarihTipID == OzelTarihTip.BelirliTarhidenSonrasi || kModel.OzelTarihTipID == OzelTarihTip.BelirliTarihdenOncesi)
                    {
                        if (kModel.Tarih1.HasValue == false)
                        {
                            MmMessage.Messages.Add("Seçilen özel tarih kriteri için Tarih bilgisi girilmesi gerekmektedir!");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih1" });
                        }
                        else
                        {
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Tarih1" });
                        }
                    }
                    else if (kModel.OzelTarihTipID == OzelTarihTip.IkiTarihArasi)
                    {
                        if (kModel.Tarih1.HasValue == false)
                        {
                            MmMessage.Messages.Add("Seçilen özel tarih kriteri için Başlangıç Tarihi bilgisi girilmesi gerekmektedir!");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih1" });
                        }
                        else
                        {
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Tarih1" });
                        }
                        if (kModel.Tarih2.HasValue == false)
                        {
                            MmMessage.Messages.Add("Seçilen özel tarih kriteri için Bitiş Tarihi bilgisi girilmesi gerekmektedir!");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih2" });
                        }
                        else
                        {
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Tarih2" });
                        }
                    }
                    else if (kModel.OzelTarihTipID == OzelTarihTip.BelirliTarihler)
                    {
                        if (qSinavTarihleri.Count == 0)
                        {
                            MmMessage.Messages.Add("Seçilen özel tarih kriteri için  tarih bilgilerinin eklenmesi zorunludur!");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OzelTarih" });
                        }
                        else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OzelTarih" });
                    }
                    else if (kModel.OzelTarihTipID == OzelTarihTip.MaksGecmisYil)
                    {
                        if (kModel.TarihGirisMaxGecmisYil.HasValue == false)
                        {
                            MmMessage.Messages.Add("Özel tarih seçeneği işaretli olan Sınav tiplerinin Maks Geçmiş Yıl Kabul bilgisinin tanımlanması zorunludur!");
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TarihGirisMaxGecmisYil" });
                        }
                        else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TarihGirisMaxGecmisYil" });
                    }
                    else if (kModel.OzelTarihTipID.HasValue == false)
                    {
                        MmMessage.Messages.Add("Özel tarih seçeneği işaretli iken özel tarih tipi seçimi zorunludur!");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OzelTarihTipID" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OzelTarihTipID" });
                }
                else
                {
                    if (kModel.TarihGirisMaxGecmisYil.HasValue == false)
                    {
                        MmMessage.Messages.Add("Özel Not seçeneği işaretli olan Sınav tiplerinin Maks Geçmiş Yıl Kabul bilgisinin tanımlanması zorunludur!");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TarihGirisMaxGecmisYil" });
                    }
                    else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TarihGirisMaxGecmisYil" });

                }

                if (kModel.OzelNot)
                {
                    if (kModel.OzelNotTipID.HasValue == false)
                    {
                        MmMessage.Messages.Add("Sınav tipi için özel not seçeneği seçildiğinden özel not tipinin belirlenmesi gerekmektedir!");
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OzelNotTipID" });
                    }
                    else
                    {
                        if (kModel.OzelNotTipID == OzelNotTip.SeciliNotlar)
                        {
                            if (qSinavNotlari.Count == 0)
                            {
                                MmMessage.Messages.Add("Sınav tipi için özel not seçeneği seçildiğinden özel not bilgilerinin girilmesi zorunludur!");
                                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OzelNot" });
                            }
                            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OzelNot" });
                        }
                        else if (kModel.OzelNotTipID == OzelNotTip.SeciliNotAraliklari)
                        {
                            if (qSubSinavAralik.Count == 0)
                            {
                                MmMessage.Messages.Add("Sınav tipi için özel not seçeneği seçildiğinden özel not aralık bilgilerinin girilmesi zorunludur!");
                                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OzelNot" });
                            }
                            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OzelNot" });
                        }
                    }
                }

            }


            if (qNAOgrenimTipKodNotAralik.Where(p => p.IsIstensin).Count() == 0)
            {
                MmMessage.Messages.Add("Kayıt edilmek istenen sınav tipinin hangi öğrenim seviyelerinde isteneceğini belirleyiniz!");
            }
            foreach (var item in qNAOgrenimTipKodNotAralik)
            {

                if (item.IsIstensin || true) // tez danışmanı önerisinde sınav bilgileri not aralıkları kullanılacağından not aralıkları her koşulda girilmesi gerekmekte
                {
                    if (item.Min.HasValue == false || item.Max.HasValue == false)
                    {
                        MmMessage.Messages.Add(item.OgrenimTipAdi + " " + (item.Ingilizce ? " İngilizce" : " Türkçe") + " not aralık kriteri için min ve max not aralıkları boş bırakılamaz");
                        item.SuccessRow = false;
                        if (item.Min.HasValue == false) item.PropName.Add("NAMin");
                        if (item.Max.HasValue == false) item.PropName.Add("NAMax");

                    }
                    else if (item.Min.Value < 0 || item.Max.Value < 0)
                    {
                        MmMessage.Messages.Add(item.OgrenimTipAdi + " " + (item.Ingilizce ? " İngilizce" : " Türkçe") + " not aralık kriteri için min ve max not aralıkları 0 dan büyük olmalıdır");
                        item.SuccessRow = false;
                        if (item.Min.Value < 0) item.PropName.Add("NAMin");
                        if (item.Max.Value < 0) item.PropName.Add("NAMax");
                    }
                    else if (item.Min.Value > item.Max.Value)
                    {
                        MmMessage.Messages.Add(item.OgrenimTipAdi + " " + (item.Ingilizce ? " İngilizce" : " Türkçe") + " not aralık kriteri için min not max not'dan büyük olamaz");
                        item.SuccessRow = false;
                        item.PropName.Add("NAMin");
                        item.PropName.Add("NAMax");
                    }
                }
            }
            if (kModel.NotDonusum)
                if (kModel.NotDonusumFormulu.IsNullOrWhiteSpace())
                {
                    MmMessage.Messages.Add("Not dönüşümü olacak seçeneği seçildiğinde not dönüşüm formulünün girilmesi zorunludur!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "NotDonusumFormulu" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "NotDonusumFormulu" });

            if (MmMessage.Messages.Count == 0)
            {
                var cnt = db.SinavTipleris.Where(p => p.SinavTipKod == kModel.SinavTipKod && p.EnstituKod == kModel.EnstituKod && p.SinavTipID != kModel.SinavTipID).Count();
                if (cnt > 0)
                {
                    MmMessage.Messages.Add("Tanımlamak istediğiniz kod daha önceden sisteme tanımlanmıştır, tekrar tanımlanamaz!");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SinavTipKod" });
                }
            }

            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;

                if (kModel.OzelTarih)
                {
                    kModel.TarihGirisMaxGecmisYil = null;
                    if (kModel.OzelTarihTipID == OzelTarihTip.BelirliTarhidenSonrasi || kModel.OzelTarihTipID == OzelTarihTip.BelirliTarihdenOncesi)
                    {
                        kModel.Tarih2 = (DateTime?)null;
                    }
                    else if (kModel.OzelTarihTipID == OzelTarihTip.BelirliTarihler)
                    {
                        kModel.Tarih1 = (DateTime?)null;
                        kModel.Tarih2 = (DateTime?)null;
                    }
                }
                else
                {
                    kModel.Tarih1 = (DateTime?)null;
                    kModel.Tarih2 = (DateTime?)null;
                }
                kModel.NotDonusumFormulu = kModel.NotDonusum ? kModel.NotDonusumFormulu : null;
                #region DetayData
                var newSinanTipleriDils = kModel.SinavDilIDs.Select(s => new SinavTipleriDil { SinavDilID = s }).ToList();
                var newSinavTipleriDonems = qWebServisDonemlari.Select(s => new SinavTipleriDonem
                {
                    Yil = s.Yil,
                    WsDonemKod = "",
                    WsDonemAd = "",
                    IsTaahhutVar = kModel.SinavTipGrupID == SinavTipGrup.Ales_Gree ? false : s.IsTaahhutVar
                }).ToList();
                var newSinavTarihleris = qSinavTarihleri.Select(s => new SinavTarihleri
                {
                    SinavTarihleriID = s.SinavTarihleriID,
                    SinavTarihi = s.SinavTarihi,
                }).ToList();
                var newSinavNotlaris = qSinavNotlari.Select(s => new SinavNotlari
                {
                    SinavNotlariID = s.SinavNotlariID,
                    SinavNotAdi = s.SinavNotAdi,
                    SinavNotDeger = s.SinavNotDeger
                }).ToList();
                var newSinavTiplerSubSinavAraliks = qSubSinavAralik.Select(s => new SinavTiplerSubSinavAralik
                {
                    SubSinavAralikID = s.SubSinavAralikID,
                    SubSinavAralikAdi = s.SubSinavAralikAdi,
                    SubSinavMin = s.SubSinavMin,
                    SubSinavMax = s.SubSinavMax,
                    NotDonusum = s.NotDonusum,
                    NotDonusumFormulu = s.NotDonusum ? s.NotDonusumFormulu : null
                }).ToList();
                var newSinavTipleriOTNotAraliklaris = qNAOgrenimTipKodNotAralik.Select(s => new SinavTipleriOTNotAraliklari
                {
                    EnstituKod = kModel.EnstituKod,
                    OgrenimTipKod = s.OgrenimTipKod,
                    IsGecerli = s.IsGecerli,
                    IsIstensin = s.IsIstensin,
                    Ingilizce = s.Ingilizce,
                    IsOzelNotAralik = s.IsOzelNotAralik,
                    Min = s.Min.Value,
                    Max = s.Max.Value,
                    SinavTipleriOTNotAraliklariGecersizProgramlars = s.ProgramKods.Select(sp => new SinavTipleriOTNotAraliklariGecersizProgramlar { ProgramKod = sp }).ToList()
                }).ToList();
                #endregion
                if (kModel.SinavTipID <= 0)
                {
                    kModel.IsAktif = true;
                    db.SinavTipleris.Add(new SinavTipleri
                    {
                        EnstituKod = kModel.EnstituKod,
                        SinavTipGrupID = kModel.SinavTipGrupID,
                        SinavTipKod = kModel.SinavTipKod,
                        SinavAdi = kModel.SinavAdi,
                        WebService = kModel.WebService,
                        WebServiceKod = kModel.WebServiceKod,
                        WsSinavCekimTipID = kModel.WsSinavCekimTipID,
                        TarihGirisMaxGecmisYil = kModel.TarihGirisMaxGecmisYil,
                        OzelTarih = kModel.OzelTarih,
                        OzelNot = kModel.OzelNot,
                        OzelNotTipID = kModel.OzelNot ? kModel.OzelNotTipID : null,
                        OzelTarihTipID = kModel.OzelTarihTipID,
                        Tarih1 = kModel.Tarih1,
                        Tarih2 = kModel.Tarih2,
                        GIsTaahhutVar = kModel.WebService ? false : kModel.GIsTaahhutVar,
                        KusuratVar = kModel.KusuratVar,
                        Min = kModel.Min,
                        Max = kModel.Max,
                        NotDonusum = kModel.NotDonusum,
                        NotDonusumFormulu = kModel.NotDonusumFormulu,
                        IsAktif = kModel.IsAktif,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemTarihi = DateTime.Now,
                        SinavTipleriDils = newSinanTipleriDils,
                        SinavTipleriDonems = newSinavTipleriDonems,
                        SinavTarihleris = newSinavTarihleris,
                        SinavNotlaris = newSinavNotlaris,
                        SinavTiplerSubSinavAraliks = newSinavTiplerSubSinavAraliks,
                        SinavTipleriOTNotAraliklaris = newSinavTipleriOTNotAraliklaris
                    });
                }
                else
                {
                    var data = db.SinavTipleris.Where(p => p.SinavTipID == kModel.SinavTipID).First();
                    data.EnstituKod = kModel.EnstituKod;
                    data.SinavTipGrupID = kModel.SinavTipGrupID;
                    data.SinavTipKod = kModel.SinavTipKod;
                    data.SinavAdi = kModel.SinavAdi;
                    data.WebService = kModel.WebService;
                    data.WebServiceKod = kModel.WebServiceKod;
                    data.WsSinavCekimTipID = kModel.WsSinavCekimTipID;
                    data.TarihGirisMaxGecmisYil = kModel.TarihGirisMaxGecmisYil;
                    data.OzelTarih = kModel.OzelTarih;
                    data.OzelTarihTipID = kModel.OzelTarihTipID;
                    data.Tarih1 = kModel.Tarih1;
                    data.Tarih2 = kModel.Tarih2;
                    data.OzelNot = kModel.OzelNot;
                    data.OzelNotTipID = kModel.OzelNot ? kModel.OzelNotTipID : null;
                    data.KusuratVar = kModel.KusuratVar;
                    data.Min = kModel.Min;
                    data.Max = kModel.Max;
                    data.GIsTaahhutVar = kModel.WebService ? false : kModel.GIsTaahhutVar;
                    data.NotDonusum = kModel.NotDonusum;
                    data.NotDonusumFormulu = kModel.NotDonusumFormulu;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;
                    db.SinavTipleriDils.RemoveRange(data.SinavTipleriDils);
                    db.SinavTipleriDonems.RemoveRange(data.SinavTipleriDonems);
                    db.SinavTarihleris.RemoveRange(data.SinavTarihleris);
                    db.SinavNotlaris.RemoveRange(data.SinavNotlaris);
                    db.SinavTiplerSubSinavAraliks.RemoveRange(data.SinavTiplerSubSinavAraliks);
                    db.SinavTipleriOTNotAraliklaris.RemoveRange(data.SinavTipleriOTNotAraliklaris);

                    data.SinavTipleriDils = newSinanTipleriDils;
                    data.SinavTipleriDonems = newSinavTipleriDonems;
                    data.SinavTarihleris = newSinavTarihleris;
                    data.SinavNotlaris = newSinavNotlaris;
                    data.SinavTiplerSubSinavAraliks = newSinavTiplerSubSinavAraliks;
                    data.SinavTipleriOTNotAraliklaris = newSinavTipleriOTNotAraliklaris;
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }

            #region ReturnData
            kModel.SinavTipleriDils = kModel.SinavDilIDs.Select(s => new SinavTipleriDil
            {
                SinavDilID = s
            }).ToList();

            kModel.SinavNotlaris = qSinavNotlari.Select(s => new SinavNotlari { SinavNotlariID = s.SinavNotlariID, SinavNotAdi = s.SinavNotAdi, SinavNotDeger = s.SinavNotDeger }).ToList();

            kModel.SinavTiplerSubSinavAraliks = qSubSinavAralik.Select(s => new SinavTiplerSubSinavAralik { SubSinavAralikID = s.SubSinavAralikID, SinavTipID = kModel.SinavTipID, SubSinavAralikAdi = s.SubSinavAralikAdi, SubSinavMin = s.SubSinavMin, SubSinavMax = s.SubSinavMax, NotDonusum = s.NotDonusum, NotDonusumFormulu = s.NotDonusumFormulu }).ToList();

            kModel.SinavTarihleris = qSinavTarihleri.Select(s => new SinavTarihleri { SinavTarihleriID = s.SinavTarihleriID, SinavTarihi = s.SinavTarihi }).ToList();

            var qwebInsertD = (from s in qWebServisDonemlari
                               select new
                               {
                                   s.SinavTipDonemID,
                                   s.Yil,
                                   WsDonemKod = "",
                                   DonemAdi = ""
                               }).ToList();
            kModel.WsSinavCekimTipID = kModel.WsSinavCekimTipID ?? WsCekimTipi.Donemsel;

            foreach (var item in qwebInsertD)
            {
                kModel.SinavTipleriDonems.Add(new SinavTipleriDonem { Yil = item.Yil, WsDonemKod = "", WsDonemAd = "" });
            }

            var OgrenimTipleris = db.OgrenimTipleris.Where(p => p.EnstituKod == kModel.EnstituKod && p.IsAktif).ToList();
            var SinavTipleriOTNotAraliklaris = new List<SinavTipleriOTNotAraliklari>();
            var Dils = new List<bool> { true, false };
            foreach (var itemD in Dils)
            {
                foreach (var itemOt in OgrenimTipleris)
                {
                    var Kayit = qNAOgrenimTipKodNotAralik.Where(p => p.OgrenimTipKod == itemOt.OgrenimTipKod && p.Ingilizce == itemD).FirstOrDefault();
                    SinavTipleriOTNotAraliklaris.Add(
                                             new SinavTipleriOTNotAraliklari
                                             {
                                                 EnstituKod = kModel.EnstituKod,
                                                 SinavTipID = kModel.SinavTipID,
                                                 OgrenimTipKod = itemOt.OgrenimTipKod,
                                                 IsGecerli = Kayit != null ? Kayit.IsGecerli : false,
                                                 IsIstensin = Kayit != null ? Kayit.IsIstensin : false,
                                                 Ingilizce = itemD,
                                                 IsOzelNotAralik = Kayit != null ? Kayit.IsOzelNotAralik : false,
                                                 Min = Kayit != null ? Kayit.Min : null,
                                                 Max = Kayit != null ? Kayit.Max : null,
                                                 SinavTipleriOTNotAraliklariGecersizProgramlars = Kayit.ProgramKods.Select(s => new SinavTipleriOTNotAraliklariGecersizProgramlar { ProgramKod = s }).ToList()

                                             });
                }
            }
            kModel.SinavTipleriOTNotAraliklaris = SinavTipleriOTNotAraliklaris;

            ViewBag.MmMessage = MmMessage;
            ViewBag.OgrenimTipleris = OgrenimTipleris;
            ViewBag.SinavDilleris = db.SinavDilleris.ToList();
            ViewBag.Programlars = db.Programlars.Where(p => p.AnabilimDallari.EnstituKod == kModel.EnstituKod).OrderBy(o => o.ProgramAdi).ToList();
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.SinavTipGrupID = new SelectList(Management.cmbGetSinavTipGruplari(true), "Value", "Caption", kModel.SinavTipGrupID);
            ViewBag.OzelTarihTipID = new SelectList(Management.cmbGetOzelTarihTipleri(true), "Value", "Caption", kModel.OzelTarihTipID);
            ViewBag.OzelNotTipID = new SelectList(Management.cmbGetOzelNotTipleri(true), "Value", "Caption", kModel.OzelNotTipID);
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.WsSinavCekimTipID = new SelectList(Management.cmbGetWsSinavCekimTipleri(true, WsCekimTipi.Donemsel), "Value", "Caption", kModel.WsSinavCekimTipID);
            ViewBag.SWsDonemKod = new SelectList(Management.cmbGetWsSinavCekimTipDetay(kModel.WsSinavCekimTipID ?? 0, false), "Value", "Caption");
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(kModel.EnstituKod), "Value", "Caption");
            #endregion
            return View(kModel);
        }
        public ActionResult getOtBilgi(string EnstituKod, int SinavTipID)
        {

            var ots = db.OgrenimTipleris.Where(p => p.EnstituKod == EnstituKod && p.IsAktif).ToList();
            var dilDOngu = new List<bool> { { true }, { false } };
            var krNotAralikMld = new List<krSinavTipleriOTNotAraliklari>();
            foreach (var item in dilDOngu)
            {
                var qotNA = (from s in ots
                             join na in db.SinavTipleriOTNotAraliklaris.Where(p => p.Ingilizce == item && p.SinavTipID == SinavTipID && p.EnstituKod == EnstituKod) on s.OgrenimTipKod equals na.OgrenimTipKod into def1
                             from notAr in def1.DefaultIfEmpty()
                             select new krSinavTipleriOTNotAraliklari
                             {
                                 OgrenimTipKod = s.OgrenimTipKod,
                                 OgrenimTipAdi = s.OgrenimTipAdi,
                                 Ingilizce = item,
                                 IsGecerli = notAr != null ? notAr.IsGecerli : false,
                                 IsIstensin = notAr != null ? notAr.IsIstensin : false,
                                 IsOzelNotAralik = notAr != null ? notAr.IsOzelNotAralik : false,
                                 Min = notAr != null ? notAr.Min : (double?)null,
                                 Max = notAr != null ? notAr.Max : (double?)null,
                                 ProgramKods = notAr != null ? notAr.SinavTipleriOTNotAraliklariGecersizProgramlars.Select(s => s.ProgramKod).ToList() : new List<string>()
                             }).ToList();
                krNotAralikMld.AddRange(qotNA);

            }
            var prK = db.SinavTipleriOTNotAraliklariGecersizProgramlars.Where(p => p.SinavTipleriOTNotAraliklari.SinavTipID == SinavTipID).Select(s => s.ProgramKod).ToList();
            var pr = db.Programlars.Where(p => p.AnabilimDallari.EnstituKod == EnstituKod).OrderBy(o => o.ProgramAdi).ToList();
            var dataR = pr.Select(s => new CheckObject<Programlar>
            {
                Value = s,
                Checked = prK.Contains(s.ProgramKod)
            }).OrderByDescending(o => o.Checked).ThenBy(t => t.Value.ProgramAdi).Select(s2 => new CmbStringDto { Value = s2.Value.ProgramKod, Caption = s2.Value.ProgramAdi }).ToList();


            ViewBag.IPProgramKod = dataR;
            return View(krNotAralikMld);
        }

        public ActionResult getProgramlar(int MailSablonlariID)
        {
            var KulID = UserIdentity.Current.Id;
            var sbl = db.MailSablonlaris.Where(p => p.MailSablonlariID == MailSablonlariID).Select(s => new { s.Sablon, s.SablonHtml, MailSablonlariEkleri = s.MailSablonlariEkleris.Select(s2 => new { s2.MailSablonlariEkiID, s2.EkAdi, s2.EkDosyaYolu }) }).First();
            return Json(new { sbl.Sablon, sbl.SablonHtml, sbl.MailSablonlariEkleri }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult getDetail(int id, int tbInx)
        {

            var model = (from s in db.SinavTipleris
                         join sl in db.SinavTipleris on s.SinavTipID equals sl.SinavTipID
                         join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                         where s.SinavTipID == id
                         select new frSinavTipleri
                         {
                             SinavTipID = s.SinavTipID,
                             SinavTipKod = s.SinavTipKod,
                             SinavAdi = sl.SinavAdi,

                             EnstituKod = s.EnstituKod,
                             TarihGirisMaxGecmisYil = s.TarihGirisMaxGecmisYil,
                             EnstituAd = e.EnstituAd,
                             SinavTipGrupID = s.SinavTipGrupID,
                             SinavTipGrupAdi = s.SinavTipGruplari.SinavTipGrupAdi,
                             WebService = s.WebService,
                             WebServiceKod = s.WebServiceKod,
                             WsSinavCekimTipID = s.WsSinavCekimTipID,
                             WsSinavCekimTipAdi = s.WsSinavCekimTipleri != null ? s.WsSinavCekimTipleri.WsSinavCekimTipAdi : "",
                             OzelTarih = s.OzelTarih,
                             OzelTarihTipID = s.OzelTarihTipID,
                             Tarih1 = s.Tarih1,
                             Tarih2 = s.Tarih2,
                             OzelNot = s.OzelNot,
                             OzelNotTipID = s.OzelNotTipID,
                             KusuratVar = s.KusuratVar,
                             Min = s.Min,
                             Max = s.Max,
                             NotDonusum = s.NotDonusum,
                             NotDonusumFormulu = s.NotDonusumFormulu,
                             IsAktif = s.IsAktif,
                             IslemTarihi = s.IslemTarihi,
                             IslemYapan = s.Kullanicilar.KullaniciAdi,
                             IslemYapanID = s.IslemYapanID,
                             IslemYapanIP = s.IslemYapanIP,
                             SinavNotlaris = s.SinavNotlaris.ToList(),
                             SinavTiplerSubSinavAraliks = s.SinavTiplerSubSinavAraliks.ToList(),
                             SinavTarihleris = s.SinavTarihleris.ToList(),
                             krSinavTipleriDonems = (from sq in s.SinavTipleriDonems
                                                     select new krSinavTipleriDonems
                                                     {
                                                         SinavTipID = sq.SinavTipID,
                                                         SinavTipDonemID = sq.SinavTipDonemID,
                                                         Yil = sq.Yil,
                                                         WsDonemKod = sq.WsDonemKod,
                                                         IsTaahhutVar = sq.IsTaahhutVar,
                                                     }).ToList(),

                             SinavTipleriOTNotAraliklariList = (from s2 in s.SinavTipleriOTNotAraliklaris.Where(p => p.EnstituKod == s.EnstituKod && p.SinavTipID == s.SinavTipID)
                                                                join ot in db.OgrenimTipleris on new { s.EnstituKod, s2.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                                                join otl in db.OgrenimTipleris on ot.OgrenimTipID equals otl.OgrenimTipID

                                                                select new krSinavTipleriOTNotAraliklari
                                                                {
                                                                    OgrenimTipKod = s2.OgrenimTipKod,
                                                                    OgrenimTipAdi = otl.OgrenimTipAdi,
                                                                    IsGecerli = s2.IsGecerli,
                                                                    IsIstensin = s2.IsIstensin,
                                                                    Ingilizce = s2.Ingilizce,
                                                                    Min = s2.Min,
                                                                    Max = s2.Max,
                                                                    IstenmeyenProgramlar = s2.SinavTipleriOTNotAraliklariGecersizProgramlars.Select(sq => new CmbStringDto { Value = sq.ProgramKod, Caption = sq.Programlar.ProgramAdi }).ToList()
                                                                }).ToList()
                         }).First();

            if (db.SinavTipleriOT_SNA.Any(p => p.SinavTipID == model.SinavTipID))
            {
                var qmodel = (from s in db.SinavTipleriOT_SNA.Where(p => p.SinavTipID == model.SinavTipID)
                              select new frSinavTipleriSPA
                              {
                                  SinavTipleriOT_SNAID = s.SinavTipleriOT_SNAID,
                                  SinavTipID = s.SinavTipID,
                                  SinavTipleriOT_SNA_PR = s.SinavTipleriOT_SNA_PR.ToList(),
                                  SinavTipleriOTNotAraliklariList = (from s2 in s.SinavTipleriOT_SNA_OT.Where(p => p.SinavTipleriOT_SNAID == s.SinavTipleriOT_SNAID)
                                                                     join ot in db.OgrenimTipleris on new { s.SinavTipleri.EnstituKod, s2.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                                                     join otl in db.OgrenimTipleris on ot.OgrenimTipID equals otl.OgrenimTipID

                                                                     select new krSinavTipleriOTNotAraliklari
                                                                     {
                                                                         OgrenimTipKod = s2.OgrenimTipKod,
                                                                         OgrenimTipAdi = otl.OgrenimTipAdi,
                                                                         IsGecerli = s2.IsGecerli,
                                                                         IsIstensin = s2.IsIstensin,
                                                                         Ingilizce = s2.Ingilizce,
                                                                         Min = s2.Min,
                                                                         Max = s2.Max,
                                                                     }).ToList()
                              }).OrderBy(o => o.SinavTipleriOT_SNAID).ToList();
                model.frSinavTipleriSPA = qmodel;
            }

            var ots = db.OgrenimTipleris.Where(p => p.EnstituKod == model.EnstituKod && p.IsAktif).ToList();
            var dilDOngu = new List<bool> { { true }, { false } };
            var krNotAralikMld = new List<krSinavTipleriOTNotAraliklari>();
            foreach (var item in dilDOngu)
            {
                var qotNA = (from s in ots
                             join na in db.SinavTipleriOTNotAraliklaris.Where(p => p.Ingilizce == item && p.SinavTipID == model.SinavTipID && p.EnstituKod == model.EnstituKod) on s.OgrenimTipKod equals na.OgrenimTipKod into def1
                             from notAr in def1.DefaultIfEmpty()
                             select new krSinavTipleriOTNotAraliklari
                             {
                                 OgrenimTipKod = s.OgrenimTipKod,
                                 OgrenimTipAdi = s.OgrenimTipAdi,
                                 Ingilizce = item,
                                 IsGecerli = notAr != null ? notAr.IsGecerli : false,
                                 IsIstensin = notAr != null ? notAr.IsIstensin : false,
                                 IsOzelNotAralik = notAr != null ? notAr.IsOzelNotAralik : false,
                                 Min = notAr != null ? notAr.Min : (double?)null,
                                 Max = notAr != null ? notAr.Max : (double?)null,
                                 IstenmeyenProgramlar = notAr != null ? (notAr.SinavTipleriOTNotAraliklariGecersizProgramlars.Select(sq => new CmbStringDto { Value = sq.ProgramKod, Caption = sq.Programlar.ProgramAdi }).ToList()) : new List<CmbStringDto>()
                             }).ToList();
                krNotAralikMld.AddRange(qotNA);
            }
            model.SinavTipleriOTNotAraliklariList = krNotAralikMld.OrderBy(o => o.Ingilizce).ThenBy(t => t.OgrenimTipAdi).ToList();
            model.SelectedTabIndex = tbInx;
            return View(model);
        }

        public ActionResult STProgramaOzelNotKriterEkle(int? id, int SinavTipID, string dlgid)
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            ViewBag.MmMessage = MmMessage;
            var model = new kmSinavTipleriSPNA();

            var stip = db.SinavTipleris.Where(p => p.SinavTipID == SinavTipID).First();
            if (id.HasValue)
            {
                var data = db.SinavTipleriOT_SNA.Where(p => p.SinavTipleriOT_SNAID == id).FirstOrDefault();
                if (data != null)
                {
                    model.SinavTipleriOT_SNAID = data.SinavTipleriOT_SNAID;
                    model.SinavTipID = data.SinavTipID;
                    model.IPProgramKod = data.SinavTipleriOT_SNA_PR.Select(s => s.ProgramKod).ToList();
                }

                var ots = db.OgrenimTipleris.Where(p => p.EnstituKod == stip.EnstituKod && p.IsAktif).ToList();
                var dilDOngu = new List<bool> { { true }, { false } };
                var krNotAralikMld = new List<krSinavTipleriOTNotAraliklari>();
                foreach (var item in dilDOngu)
                {
                    var qotNA = (from s in ots
                                 join st in stip.SinavTipleriOTNotAraliklaris.Where(p => p.Ingilizce == item && p.SinavTipID == model.SinavTipID && p.EnstituKod == stip.EnstituKod) on s.OgrenimTipKod equals st.OgrenimTipKod into def2
                                 from notAnaAr in def2.DefaultIfEmpty()
                                 join na in db.SinavTipleriOT_SNA_OT.Where(p => p.Ingilizce == item && p.SinavTipleriOT_SNAID == model.SinavTipleriOT_SNAID) on s.OgrenimTipKod equals na.OgrenimTipKod into def1
                                 from notAr in def1.DefaultIfEmpty()
                                 select new krSinavTipleriOTNotAraliklari
                                 {
                                     OgrenimTipKod = s.OgrenimTipKod,
                                     OgrenimTipAdi = s.OgrenimTipAdi,
                                     Ingilizce = item,
                                     IsGecerli = notAr != null ? notAr.IsGecerli : (notAnaAr != null ? notAnaAr.IsGecerli : false),
                                     IsIstensin = notAr != null ? notAr.IsIstensin : (notAnaAr != null ? notAnaAr.IsIstensin : false),
                                     IsOzelNotAralik = notAr != null ? notAr.IsOzelNotAralik : (notAnaAr != null ? notAnaAr.IsOzelNotAralik : false),
                                     Min = notAr != null ? notAr.Min : (notAnaAr != null ? notAnaAr.Min : (double?)null),
                                     Max = notAr != null ? notAr.Max : (notAnaAr != null ? notAnaAr.Max : (double?)null)
                                 }).ToList();
                    krNotAralikMld.AddRange(qotNA);


                }
                model.SinavTipleriOTNotAraliklari = krNotAralikMld.OrderBy(o => o.Ingilizce).ThenBy(t => t.OgrenimTipAdi).ToList();
            }
            else
            {
                var data = db.SinavTipleris.Where(p => p.SinavTipID == SinavTipID).FirstOrDefault();
                if (data != null)
                {
                    model.SinavTipID = data.SinavTipID;


                }

                var ots = db.OgrenimTipleris.Where(p => p.EnstituKod == stip.EnstituKod && p.IsAktif).ToList();
                var dilDOngu = new List<bool> { { true }, { false } };
                var krNotAralikMld = new List<krSinavTipleriOTNotAraliklari>();
                foreach (var item in dilDOngu)
                {
                    var qotNA = (from s in ots
                                 join na in data.SinavTipleriOTNotAraliklaris.Where(p => p.Ingilizce == item && p.SinavTipID == model.SinavTipID && p.EnstituKod == stip.EnstituKod) on s.OgrenimTipKod equals na.OgrenimTipKod into def1
                                 from notAr in def1.DefaultIfEmpty()
                                 select new krSinavTipleriOTNotAraliklari
                                 {
                                     OgrenimTipKod = s.OgrenimTipKod,
                                     OgrenimTipAdi = s.OgrenimTipAdi,
                                     Ingilizce = item,
                                     IsGecerli = notAr != null ? notAr.IsGecerli : false,
                                     IsIstensin = notAr != null ? notAr.IsIstensin : false,
                                     IsOzelNotAralik = notAr != null ? notAr.IsOzelNotAralik : false,
                                     Min = notAr != null ? notAr.Min : (double?)null,
                                     Max = notAr != null ? notAr.Max : (double?)null
                                 }).ToList();
                    krNotAralikMld.AddRange(qotNA);


                }
                model.SinavTipleriOTNotAraliklari = krNotAralikMld.OrderBy(o => o.Ingilizce).ThenBy(t => t.OgrenimTipAdi).ToList();
            }




            var nContains = db.SinavTipleriOT_SNA_PR.Where(p => p.SinavTipleriOT_SNA.SinavTipID == model.SinavTipID && p.SinavTipleriOT_SNAID != model.SinavTipleriOT_SNAID).Select(s => s.ProgramKod).ToList();
            var pr = db.Programlars.Where(p => nContains.Contains(p.ProgramKod) == false && p.AnabilimDallari.EnstituKod == stip.EnstituKod).OrderBy(o => o.ProgramAdi).ToList();
            var dataR = pr.Select(s => new kulaniciProgramYetkiModel
            {
                ProgramKod = s.ProgramKod,
                ProgramAdi = s.ProgramAdi,
                YetkiVar = model.IPProgramKod.Contains(s.ProgramKod)
            }).OrderByDescending(o => o.YetkiVar).ThenBy(t => t.ProgramAdi).ToList();
            ViewBag.Programlar = dataR;
            ViewBag.stip = stip;
            return View(model);
        }

        [HttpPost]
        public ActionResult STProgramaOzelNotKriterEkle(kmSinavTipleriSPNA kModel, string dlgid = "")
        {

            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            //Sınav Puanı Bilgileri 

            var stip = db.SinavTipleris.Where(p => p.SinavTipID == kModel.SinavTipID).First();


            //Sinav ogrenimTip Not Araliklari
            var qNAOgrenimTipKod = kModel.NAOgrenimTipKod.Select((s, inx) => new { s, inx }).ToList();
            var qNAIngilizce = kModel.NAIngilizce.Select((s, inx) => new { s, inx }).ToList();
            var qNAIsIstensin = kModel.NAIsIstensin.Select((s, inx) => new { s = (s == 1 ? true : false), inx }).ToList();
            var qNAIsGecerli = kModel.NAIsGecerli.Select((s, inx) => new { s = (s == 1 ? true : false), inx }).ToList();
            var qNAMin = kModel.NAMin.Select((s, inx) => new { s, inx }).ToList();
            var qNAMax = kModel.NAMax.Select((s, inx) => new { s, inx }).ToList();

            var qNAOgrenimTipKodNotAralik = (from ot in qNAOgrenimTipKod
                                             join ing in qNAIngilizce on ot.inx equals ing.inx
                                             join gcr in qNAIsGecerli on ot.inx equals gcr.inx
                                             join ist in qNAIsIstensin on ot.inx equals ist.inx
                                             join min in qNAMin on ot.inx equals min.inx
                                             join max in qNAMax on ot.inx equals max.inx
                                             join otl in db.OgrenimTipleris on ot.s equals otl.OgrenimTipID
                                             select new krSinavTipleriOTNotAraliklari
                                             {
                                                 OgrenimTipKod = ot.s,
                                                 OgrenimTipAdi = otl.OgrenimTipAdi,
                                                 Ingilizce = ing.s,
                                                 IsGecerli = gcr.s,
                                                 IsIstensin = ist.s,
                                                 IsOzelNotAralik = ist.s && (min.s.HasValue && max.s.HasValue),
                                                 Min = min.s,
                                                 Max = max.s
                                             }).ToList();

            #region kontrol



            if (qNAOgrenimTipKodNotAralik.Where(p => p.IsIstensin).Count() == 0)
            {
                string msg = "Kayıt edilmek istenen sınav tipinin hangi öğrenim seviyelerinde isteneceğini belirleyiniz!";
                MmMessage.Messages.Add(msg);
            }
            foreach (var item in qNAOgrenimTipKodNotAralik)
            {

                if (item.IsIstensin)
                {
                    if (item.Min.HasValue == false || item.Max.HasValue == false)
                    {
                        string msg = item.OgrenimTipAdi + " " + (item.Ingilizce ? " İngilizce" : " Türkçe") + " not aralık kriteri için min ve max not aralıkları boş bırakılamaz";
                        MmMessage.Messages.Add(msg);
                        item.SuccessRow = false;
                        if (item.Min.HasValue == false) item.PropName.Add("NAMin");
                        if (item.Max.HasValue == false) item.PropName.Add("NAMax");

                    }
                    else if (item.Min.Value < 0 || item.Max.Value < 0)
                    {
                        string msg = item.OgrenimTipAdi + " " + (item.Ingilizce ? " İngilizce" : " Türkçe") + " not aralık kriteri için min ve max not aralıkları 0 dan büyük olmalıdır";
                        MmMessage.Messages.Add(msg);
                        item.SuccessRow = false;
                        if (item.Min.Value < 0) item.PropName.Add("NAMin");
                        if (item.Max.Value < 0) item.PropName.Add("NAMax");
                    }
                    else if (item.Min.Value > item.Max.Value)
                    {
                        string msg = item.OgrenimTipAdi + " " + (item.Ingilizce ? " İngilizce" : " Türkçe") + " not aralık kriteri için min not max not'dan büyük olamaz";
                        MmMessage.Messages.Add(msg);
                        item.SuccessRow = false;
                        item.PropName.Add("NAMin");
                        item.PropName.Add("NAMax");
                    }
                }
            }

            var qprKods = db.SinavTipleriOT_SNA_PR.Where(p => p.SinavTipleriOT_SNAID != kModel.SinavTipleriOT_SNAID && p.SinavTipleriOT_SNA.SinavTipID == kModel.SinavTipID).Select(s => s.ProgramKod).Distinct().ToList();
            kModel.IPProgramKod = kModel.IPProgramKod.Where(p => !qprKods.Contains(p)).ToList();
            if (kModel.IPProgramKod.Count == 0)
            {

                string msg = "Yeni not tanımını kayıt edebilmek için en az 1 program seçmeniz gerekmektedir!";
                MmMessage.Messages.Add(msg);
            }
            #endregion
            if (MmMessage.Messages.Count == 0)
            {


                if (kModel.SinavTipleriOT_SNAID <= 0)
                {
                    var enst = db.SinavTipleriOT_SNA.Add(new SinavTipleriOT_SNA
                    {
                        SinavTipID = kModel.SinavTipID
                    });
                    db.SaveChanges();
                    kModel.SinavTipleriOT_SNAID = enst.SinavTipleriOT_SNAID;

                }
                else
                {
                    var data = db.SinavTipleriOT_SNA.Where(p => p.SinavTipleriOT_SNAID == kModel.SinavTipleriOT_SNAID).First();
                    var lstPr = db.SinavTipleriOT_SNA_PR.Where(p => p.SinavTipleriOT_SNAID == kModel.SinavTipleriOT_SNAID).ToList();
                    db.SinavTipleriOT_SNA_PR.RemoveRange(lstPr);

                    var otNotAr = db.SinavTipleriOT_SNA_OT.Where(p => p.SinavTipleriOT_SNAID == kModel.SinavTipleriOT_SNAID).ToList();
                    db.SinavTipleriOT_SNA_OT.RemoveRange(otNotAr);
                }
                foreach (var item in kModel.IPProgramKod)
                {
                    db.SinavTipleriOT_SNA_PR.Add(new Models.SinavTipleriOT_SNA_PR { SinavTipleriOT_SNAID = kModel.SinavTipleriOT_SNAID, ProgramKod = item });
                }

                if (stip.SinavTipGrupID == SinavTipGrup.DilSinavlari || stip.SinavTipGrupID == SinavTipGrup.Ales_Gree)
                    foreach (var item in qNAOgrenimTipKodNotAralik)
                    {
                        var qST = db.SinavTipleriOT_SNA_OT.Add(new SinavTipleriOT_SNA_OT
                        {
                            SinavTipleriOT_SNAID = kModel.SinavTipleriOT_SNAID,
                            OgrenimTipKod = item.OgrenimTipKod,
                            IsGecerli = item.IsGecerli,
                            IsIstensin = item.IsIstensin,
                            Ingilizce = item.Ingilizce,
                            IsOzelNotAralik = item.IsOzelNotAralik,
                            Min = item.IsOzelNotAralik ? item.Min.Value : (double?)null,
                            Max = item.IsOzelNotAralik ? item.Max.Value : (double?)null
                        });
                    }
                db.SaveChanges();
                MmMessage.IsSuccess = true;
                MessageBox.Show("Not kriteri tanımlandı", "Kayıt işlemi");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            if (MmMessage.IsSuccess) MmMessage.IsCloseDialog = true;

            ViewBag.MmMessage = MmMessage;
            kModel.SinavTipleriOTNotAraliklari = qNAOgrenimTipKodNotAralik;
            foreach (var item in qNAOgrenimTipKodNotAralik)
            {
                item.IsOzelNotAralik = item.IsIstensin;
            }

            var prK = kModel.IPProgramKod;
            var nContains = db.SinavTipleriOT_SNA_PR.Where(p => p.SinavTipleriOT_SNA.SinavTipID == kModel.SinavTipID && p.SinavTipleriOT_SNAID != kModel.SinavTipleriOT_SNAID).Select(s => s.ProgramKod).ToList();
            var pr = db.Programlars.Where(p => nContains.Contains(p.ProgramKod) == false && p.AnabilimDallari.EnstituKod == stip.EnstituKod && p.IsAktif).OrderBy(o => o.ProgramAdi).ToList();
            var dataR = pr.Select(s => new kulaniciProgramYetkiModel
            {
                ProgramKod = s.ProgramKod,
                ProgramAdi = s.ProgramAdi,
                YetkiVar = prK.Contains(s.ProgramKod)
            }).OrderByDescending(o => o.YetkiVar).ThenBy(t => t.ProgramAdi).ToList();
            ViewBag.Programlar = dataR;
            ViewBag.stip = stip;
            return View(kModel);
        }


        public ActionResult getWsCekimTipDetay(int WsSinavCekimTipID, int SinavTipKod)
        {

            var cekimTip = db.WsSinavCekimTipleris.Where(p => p.WsSinavCekimTipID == WsSinavCekimTipID).First();
            var list = new List<CmbStringDto>();
            if (cekimTip.GetLocalData == false)
            {
                list = Management.cmbGetWsSinavCekimTipDetay(WsSinavCekimTipID, false);
            }
            else
            {
                list = Management.cmbGetWsSinavCekimTipDetayGetLocalData(WsSinavCekimTipID, SinavTipKod, false);

            }
            return list.Select(s => new { s.Value, s.Caption }).toJsonResult();
        }
        public ActionResult Sil(int id, string EnstituKod)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();

            var kayit = db.SinavTipleris.Where(p => p.SinavTipID == id && EnstKods.Contains(p.EnstituKod) && p.EnstituKod == EnstituKod).FirstOrDefault();
            var PAdi = db.SinavTipleris.Where(p => p.SinavTipID == id).First();
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + PAdi.SinavAdi + "' İsimli Sınav Tipi Silindi!";
                    db.SinavTipleris.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + PAdi.SinavAdi + "' Sınav Tipi Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "Ünvanlar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Sınav Tipi sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult SilSTPK(int id)
        {

            var kayit = db.SinavTipleriOT_SNA.Where(p => p.SinavTipleriOT_SNAID == id).FirstOrDefault();
            var PAdi = db.SinavTipleris.Where(p => p.SinavTipID == kayit.SinavTipID).First();
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + PAdi.SinavAdi + "' İsimli Sınav tipine ait programa özel not kriteri Silindi!";
                    db.SinavTipleriOT_SNA.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + PAdi.SinavAdi + "' İsimli Sınav tipine ait programa özel not kriteri Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "SinavTipleri/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Sınav tipine ait programa özel not kriteri sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
