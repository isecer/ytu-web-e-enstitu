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
    [Authorize(Roles = RoleNames.SinavTipleri)]
    public class SinavTipleriController : Controller
    {
        private LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            return Index(new FmSinavTipleri { PageSize = 15 },ekd);
        }
        [HttpPost]
        public ActionResult Index(FmSinavTipleri model, string ekd)
        {
            model.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();

            var q = from s in _entities.SinavTipleris
                    join sl in _entities.SinavTipleris on s.SinavTipID equals sl.SinavTipID
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    where enstKods.Contains(s.EnstituKod)
                    select new FrSinavTipleri
                    {
                        SinavTipID = s.SinavTipID,
                        SinavTipKod = s.SinavTipKod,
                        SinavAdi = sl.SinavAdi,

                        EnstituKod = s.EnstituKod,
                        EnstituAd = e.EnstituAd,
                        SinavTipGrupID = s.SinavTipGrupID,
                        SinavTipGrupAdi = s.SinavTipGruplari.SinavTipGrupAdi,
                        OzelNot = s.OzelNot,
                        OzelNotTipID = s.OzelNotTipID, 
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapan = s.Kullanicilar.KullaniciAdi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP

                    };
            q = q.Where(p => UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.SinavAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.SinavAdi.Contains(model.SinavAdi));
            if (model.SinavTipGrupID.HasValue) q = q.Where(p => p.SinavTipGrupID == model.SinavTipGrupID.Value);
            if (model.OzelNot.HasValue) q = q.Where(p => p.OzelNot == model.OzelNot.Value);
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif.Value);
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.EnstituAd).ThenBy(o => o.SinavAdi);
            model.FrSinavTipleris = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            };
            ViewBag.IndexModel = indexModel;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.SinavTipGrupID = new SelectList(Management.CmbGetSinavTipGruplari(true), "Value", "Caption", model.SinavTipGrupID);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string ekd)
        {
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var model = new KmSinavTipleri
            {
                EnstituKod = EnstituBus.GetSelectedEnstitu(ekd)
            };
            if (id.HasValue)
            {
                var data = _entities.SinavTipleris.Where(p => p.SinavTipID == id).Select(s => new KmSinavTipleri
                {
                    SinavTipID = s.SinavTipID,
                    EnstituKod = s.EnstituKod,
                    SinavTipGrupID = s.SinavTipGrupID,
                    SinavTipKod = s.SinavTipKod,
                    SinavAdi = s.SinavAdi,
                    OzelNot = s.OzelNot,
                    OzelNotTipID = s.OzelNotTipID, 
                    IsAktif = s.IsAktif,  
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

            var ogrenimTipleris = _entities.OgrenimTipleris.Where(p => p.EnstituKod == model.EnstituKod && p.IsAktif).ToList();
            var sinavTipleriOtNotAraliklaris = new List<SinavTipleriOTNotAraliklari>();
            var dils = new List<bool> { true, false };
            foreach (var itemD in dils)
            {
                foreach (var itemOt in ogrenimTipleris)
                {
                    var kayit = model.SinavTipleriOTNotAraliklaris.FirstOrDefault(p => p.OgrenimTipKod == itemOt.OgrenimTipKod && p.Ingilizce == itemD);
                    sinavTipleriOtNotAraliklaris.Add(
                         new SinavTipleriOTNotAraliklari
                         {
                             EnstituKod = model.EnstituKod,
                             SinavTipID = model.SinavTipID,
                             OgrenimTipKod = itemOt.OgrenimTipKod,
                             IsGecerli = kayit?.IsGecerli ?? false,
                             IsIstensin = kayit?.IsIstensin ?? false,
                             Ingilizce = itemD,
                             IsOzelNotAralik = kayit?.IsOzelNotAralik ?? false,
                             Min = kayit?.Min,
                             Max = kayit?.Max,
                             SinavTipleriOTNotAraliklariGecersizProgramlars = kayit != null ? kayit.SinavTipleriOTNotAraliklariGecersizProgramlars : new List<SinavTipleriOTNotAraliklariGecersizProgramlar>()

                         });
                }
            }
            model.SinavTipleriOTNotAraliklaris = sinavTipleriOtNotAraliklaris;


            ViewBag.OgrenimTipleris = ogrenimTipleris;
            ViewBag.SinavDilleris = _entities.SinavDilleris.ToList();
            ViewBag.Programlars = _entities.Programlars.Where(p => p.AnabilimDallari.EnstituKod == model.EnstituKod).OrderBy(o => o.ProgramAdi).ToList();
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.SinavTipGrupID = new SelectList(Management.CmbGetSinavTipGruplari(true), "Value", "Caption", model.SinavTipGrupID);
            ViewBag.OzelNotTipID = new SelectList(Management.CmbGetOzelNotTipleri(true), "Value", "Caption", model.OzelNotTipID);
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(model.EnstituKod), "Value", "Caption");
            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Kayit(KmSinavTipleri kModel)
        {

            var mmMessage = new MmMessage();

            #region SetDetayData


            //Sınav Tarihi Bilgileri
            var qSinavTarihleriId = kModel.SinavTarihleriID.Select((s, inx) => new { s, inx }).ToList();
            var qSinavTarihi = kModel.SinavTarihi.Select((s, inx) => new { s, inx }).ToList();
            var qSinavTarihleri = (from stID in qSinavTarihleriId
                                   join stTar in qSinavTarihi on stID.inx equals stTar.inx
                                   select new
                                   {
                                       Index = stID.inx,
                                       SinavTarihleriID = stID.s,
                                       SinavTarihi = stTar.s
                                   }).ToList();
            //Sınav Puanı Bilgileri
            var qSinavNotlariId = kModel.SinavNotlariID.Select((s, inx) => new { s, inx }).ToList();
            var qSinavNotAdi = kModel.SinavNotAdi.Select((s, inx) => new { s, inx }).ToList();
            var qSinavNotDeger = kModel.SinavNotDeger.Select((s, inx) => new { s, inx }).ToList();
            var qSinavNotlari = (from snId in qSinavNotlariId
                                 join snAd in qSinavNotAdi on snId.inx equals snAd.inx
                                 join snNot in qSinavNotDeger on snId.inx equals snNot.inx
                                 select new
                                 {
                                     Index = snId.inx,
                                     SinavNotlariID = snId.s,
                                     SinavNotAdi = snAd.s,
                                     SinavNotDeger = snNot.s
                                 }).ToList();

            var qSubSinavAralikId = kModel.SubSinavAralikID.Select((s, inx) => new { s, inx }).ToList();
            var qSubSinavAralikAdi = kModel.SubSinavAralikAdi.Select((s, inx) => new { s, inx }).ToList();
            var qSubSinavMin = kModel.SubSinavMin.Select((s, inx) => new { s, inx }).ToList();
            var qSubSinavMax = kModel.SubSinavMax.Select((s, inx) => new { s, inx }).ToList();
            var qSubSinavAralik = (from snID in qSubSinavAralikId
                                   join snAd in qSubSinavAralikAdi on snID.inx equals snAd.inx
                                   join snNotmin in qSubSinavMin on snID.inx equals snNotmin.inx
                                   join snNotmax in qSubSinavMax on snID.inx equals snNotmax.inx
                                   select new
                                   {
                                       Index = snID.inx,
                                       SubSinavAralikID = snID.s,
                                       SubSinavAralikAdi = snAd.s,
                                       SubSinavMin = snNotmin.s,
                                       SubSinavMax = snNotmax.s
                                   }).ToList();

            //Sinav ogrenimTip Not Araliklari
            var qNaOgrenimTipKod = kModel.NAOgrenimTipKod.Select((s, inx) => new { s, inx }).ToList();
            var qNaIngilizce = kModel.NAIngilizce.Select((s, inx) => new { s, inx }).ToList();
            var qNaIsIstensin = kModel.NAIsIstensin.Select((s, inx) => new { s = (s == 1 ? true : false), inx }).ToList();
            var qNaIsGecerli = kModel.NAIsGecerli.Select((s, inx) => new { s = (s == 1 ? true : false), inx }).ToList();
            var qNaMin = kModel.NAMin.Select((s, inx) => new { s, inx }).ToList();
            var qNaMax = kModel.NAMax.Select((s, inx) => new { s, inx }).ToList();
            var qIpProgramKod = kModel.IPProgramKod.Select(s => new { ID = s.Split('_')[0], ProgramKod = s.Split('_')[1] }).ToList();

            var qNaOgrenimTipKodNotAralik = (from ot in qNaOgrenimTipKod
                                             join ing in qNaIngilizce on ot.inx equals ing.inx
                                             join gcr in qNaIsGecerli on ot.inx equals gcr.inx
                                             join ist in qNaIsIstensin on ot.inx equals ist.inx
                                             join min in qNaMin on ot.inx equals min.inx
                                             join max in qNaMax on ot.inx equals max.inx
                                             join otl in _entities.OgrenimTipleris on ot.s equals otl.OgrenimTipID
                                             select new KrSinavTipleriOtNotAraliklari
                                             {
                                                 OgrenimTipKod = ot.s,
                                                 OgrenimTipAdi = otl.OgrenimTipAdi,
                                                 Ingilizce = ing.s,
                                                 IsGecerli = gcr.s,
                                                 IsIstensin = ist.s,
                                                 IsOzelNotAralik = ist.s && (min.s.HasValue && max.s.HasValue),
                                                 Min = min.s,
                                                 Max = max.s,
                                                 ProgramKods = qIpProgramKod.Where(p => p.ID == ("ProgramKod-" + ot.s + "-" + (ing.s ? "En" : "Tr"))).Select(s => s.ProgramKod).ToList()
                                             }).ToList();
            #endregion

            #region kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Enstitü seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EnstituKod" });

            if (kModel.SinavTipGrupID <= 0)
            {
                mmMessage.Messages.Add("Sınav tip grup bilgisini seçiniz!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SinavTipGrupID" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "SinavTipGrupID" });
            if (kModel.SinavTipKod <= 0)
            {
                mmMessage.Messages.Add("Sınav tip kodu bilgisini giriniz!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SinavTipKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "SinavTipKod" });
            if (kModel.SinavAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Sınav Adı bilgisini giriniz!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SinavAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "SinavAdi" });


            if (kModel.OzelNot)
            {
                if (kModel.OzelNotTipID.HasValue == false)
                {
                    mmMessage.Messages.Add("Sınav tipi için özel not seçeneği seçildiğinden özel not tipinin belirlenmesi gerekmektedir!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OzelNotTipID" });
                }
                else
                {
                    if (kModel.OzelNotTipID == OzelNotTipEnum.SeciliNotlar)
                    {
                        if (qSinavNotlari.Count == 0)
                        {
                            mmMessage.Messages.Add("Sınav tipi için özel not seçeneği seçildiğinden özel not bilgilerinin girilmesi zorunludur!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OzelNot" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OzelNot" });
                    }
                    else if (kModel.OzelNotTipID == OzelNotTipEnum.SeciliNotAraliklari)
                    {
                        if (qSubSinavAralik.Count == 0)
                        {
                            mmMessage.Messages.Add("Sınav tipi için özel not seçeneği seçildiğinden özel not aralık bilgilerinin girilmesi zorunludur!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OzelNot" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OzelNot" });
                    }
                }
            }




            if (!qNaOgrenimTipKodNotAralik.Any(p => p.IsIstensin))
            {
                mmMessage.Messages.Add("Kayıt edilmek istenen sınav tipinin hangi öğrenim seviyelerinde isteneceğini belirleyiniz!");
            }
            foreach (var item in qNaOgrenimTipKodNotAralik)
            {

                if (item.IsIstensin || true) // tez danışmanı önerisinde sınav bilgileri not aralıkları kullanılacağından not aralıkları her koşulda girilmesi gerekmekte
                {
                    if (item.Min.HasValue == false || item.Max.HasValue == false)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " " + (item.Ingilizce ? " İngilizce" : " Türkçe") + " not aralık kriteri için min ve max not aralıkları boş bırakılamaz");
                        item.SuccessRow = false;
                        if (item.Min.HasValue == false) item.PropName.Add("NAMin");
                        if (item.Max.HasValue == false) item.PropName.Add("NAMax");

                    }
                    else if (item.Min.Value < 0 || item.Max.Value < 0)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " " + (item.Ingilizce ? " İngilizce" : " Türkçe") + " not aralık kriteri için min ve max not aralıkları 0 dan büyük olmalıdır");
                        item.SuccessRow = false;
                        if (item.Min.Value < 0) item.PropName.Add("NAMin");
                        if (item.Max.Value < 0) item.PropName.Add("NAMax");
                    }
                    else if (item.Min.Value > item.Max.Value)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " " + (item.Ingilizce ? " İngilizce" : " Türkçe") + " not aralık kriteri için min not max not'dan büyük olamaz");
                        item.SuccessRow = false;
                        item.PropName.Add("NAMin");
                        item.PropName.Add("NAMax");
                    }
                }
            }

            if (mmMessage.Messages.Count == 0)
            {
                if (_entities.SinavTipleris.Any(p => p.SinavTipKod == kModel.SinavTipKod && p.EnstituKod == kModel.EnstituKod && p.SinavTipID != kModel.SinavTipID))
                {
                    mmMessage.Messages.Add("Tanımlamak istediğiniz kod daha önceden sisteme tanımlanmıştır, tekrar tanımlanamaz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SinavTipKod" });
                }
            }

            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;


                #region DetayData
             
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
                }).ToList();
                var newSinavTipleriOtNotAraliklaris = qNaOgrenimTipKodNotAralik.Select(s => new SinavTipleriOTNotAraliklari
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
                    _entities.SinavTipleris.Add(new SinavTipleri
                    {
                        EnstituKod = kModel.EnstituKod,
                        SinavTipGrupID = kModel.SinavTipGrupID,
                        SinavTipKod = kModel.SinavTipKod,
                        SinavAdi = kModel.SinavAdi,
                        OzelNot = kModel.OzelNot,
                        OzelNotTipID = kModel.OzelNot ? kModel.OzelNotTipID : null, 
                        IsAktif = kModel.IsAktif,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemTarihi = DateTime.Now,  
                        SinavNotlaris = newSinavNotlaris,
                        SinavTiplerSubSinavAraliks = newSinavTiplerSubSinavAraliks,
                        SinavTipleriOTNotAraliklaris = newSinavTipleriOtNotAraliklaris
                    });
                }
                else
                {
                    var data = _entities.SinavTipleris.First(p => p.SinavTipID == kModel.SinavTipID);
                    data.EnstituKod = kModel.EnstituKod;
                    data.SinavTipGrupID = kModel.SinavTipGrupID;
                    data.SinavTipKod = kModel.SinavTipKod;
                    data.SinavAdi = kModel.SinavAdi;
                    data.OzelNot = kModel.OzelNot;
                    data.OzelNotTipID = kModel.OzelNot ? kModel.OzelNotTipID : null; 
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;  
                    _entities.SinavNotlaris.RemoveRange(data.SinavNotlaris);
                    _entities.SinavTiplerSubSinavAraliks.RemoveRange(data.SinavTiplerSubSinavAraliks);
                    _entities.SinavTipleriOTNotAraliklaris.RemoveRange(data.SinavTipleriOTNotAraliklaris);
                      
                    data.SinavNotlaris = newSinavNotlaris;
                    data.SinavTiplerSubSinavAraliks = newSinavTiplerSubSinavAraliks;
                    data.SinavTipleriOTNotAraliklaris = newSinavTipleriOtNotAraliklaris;
                }
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }

            #region ReturnData


            kModel.SinavNotlaris = qSinavNotlari.Select(s => new SinavNotlari { SinavNotlariID = s.SinavNotlariID, SinavNotAdi = s.SinavNotAdi, SinavNotDeger = s.SinavNotDeger }).ToList();

            kModel.SinavTiplerSubSinavAraliks = qSubSinavAralik.Select(s => new SinavTiplerSubSinavAralik
            {
                SubSinavAralikID = s.SubSinavAralikID,
                SinavTipID = kModel.SinavTipID,
                SubSinavAralikAdi = s.SubSinavAralikAdi,
                SubSinavMin = s.SubSinavMin,
                SubSinavMax = s.SubSinavMax,
            }).ToList();

            
           

            var ogrenimTipleris = _entities.OgrenimTipleris.Where(p => p.EnstituKod == kModel.EnstituKod && p.IsAktif).ToList();
            var sinavTipleriOtNotAraliklaris = new List<SinavTipleriOTNotAraliklari>();
            var dils = new List<bool> { true, false };
            foreach (var itemD in dils)
            {
                foreach (var itemOt in ogrenimTipleris)
                {
                    var kayit = qNaOgrenimTipKodNotAralik.FirstOrDefault(p => p.OgrenimTipKod == itemOt.OgrenimTipKod && p.Ingilizce == itemD);
                    sinavTipleriOtNotAraliklaris.Add(
                                             new SinavTipleriOTNotAraliklari
                                             {
                                                 EnstituKod = kModel.EnstituKod,
                                                 SinavTipID = kModel.SinavTipID,
                                                 OgrenimTipKod = itemOt.OgrenimTipKod,
                                                 IsGecerli = kayit != null && kayit.IsGecerli,
                                                 IsIstensin = kayit != null && kayit.IsIstensin,
                                                 Ingilizce = itemD,
                                                 IsOzelNotAralik = kayit != null && kayit.IsOzelNotAralik,
                                                 Min = kayit?.Min,
                                                 Max = kayit?.Max,
                                                 SinavTipleriOTNotAraliklariGecersizProgramlars = kayit.ProgramKods.Select(s => new SinavTipleriOTNotAraliklariGecersizProgramlar { ProgramKod = s }).ToList()

                                             });
                }
            }
            kModel.SinavTipleriOTNotAraliklaris = sinavTipleriOtNotAraliklaris;

            ViewBag.MmMessage = mmMessage;
            ViewBag.OgrenimTipleris = ogrenimTipleris;
            ViewBag.SinavDilleris = _entities.SinavDilleris.ToList();
            ViewBag.Programlars = _entities.Programlars.Where(p => p.AnabilimDallari.EnstituKod == kModel.EnstituKod).OrderBy(o => o.ProgramAdi).ToList();
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.SinavTipGrupID = new SelectList(Management.CmbGetSinavTipGruplari(true), "Value", "Caption", kModel.SinavTipGrupID);
            ViewBag.OzelNotTipID = new SelectList(Management.CmbGetOzelNotTipleri(true), "Value", "Caption", kModel.OzelNotTipID);
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(kModel.EnstituKod), "Value", "Caption");
            #endregion
            return View(kModel);
        }
        public ActionResult GetOtBilgi(string enstituKod, int sinavTipId)
        {

            var ots = _entities.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod && p.IsAktif).ToList();
            var dilDOngu = new List<bool> { { true }, { false } };
            var krNotAralikMld = new List<KrSinavTipleriOtNotAraliklari>();
            foreach (var item in dilDOngu)
            {
                var qotNa = (from s in ots
                             join na in _entities.SinavTipleriOTNotAraliklaris.Where(p => p.Ingilizce == item && p.SinavTipID == sinavTipId && p.EnstituKod == enstituKod) on s.OgrenimTipKod equals na.OgrenimTipKod into def1
                             from notAr in def1.DefaultIfEmpty()
                             select new KrSinavTipleriOtNotAraliklari
                             {
                                 OgrenimTipKod = s.OgrenimTipKod,
                                 OgrenimTipAdi = s.OgrenimTipAdi,
                                 Ingilizce = item,
                                 IsGecerli = notAr?.IsGecerli ?? false,
                                 IsIstensin = notAr?.IsIstensin ?? false,
                                 IsOzelNotAralik = notAr != null && notAr.IsOzelNotAralik,
                                 Min = notAr?.Min,
                                 Max = notAr?.Max,
                                 ProgramKods = notAr != null ? notAr.SinavTipleriOTNotAraliklariGecersizProgramlars.Select(s2 => s2.ProgramKod).ToList() : new List<string>()
                             }).ToList();
                krNotAralikMld.AddRange(qotNa);

            }
            var prK = _entities.SinavTipleriOTNotAraliklariGecersizProgramlars.Where(p => p.SinavTipleriOTNotAraliklari.SinavTipID == sinavTipId).Select(s => s.ProgramKod).ToList();
            var pr = _entities.Programlars.Where(p => p.AnabilimDallari.EnstituKod == enstituKod).OrderBy(o => o.ProgramAdi).ToList();
            var dataR = pr.Select(s => new CheckObject<Programlar>
            {
                Value = s,
                Checked = prK.Contains(s.ProgramKod)
            }).OrderByDescending(o => o.Checked).ThenBy(t => t.Value.ProgramAdi).Select(s2 => new CmbStringDto { Value = s2.Value.ProgramKod, Caption = s2.Value.ProgramAdi }).ToList();


            ViewBag.IPProgramKod = dataR;
            return View(krNotAralikMld);
        }

        public ActionResult GetProgramlar(int mailSablonlariId)
        {
            var sbl = _entities.MailSablonlaris.Where(p => p.MailSablonlariID == mailSablonlariId).Select(s => new { s.Sablon, s.SablonHtml, MailSablonlariEkleri = s.MailSablonlariEkleris.Select(s2 => new { s2.MailSablonlariEkiID, s2.EkAdi, s2.EkDosyaYolu }) }).First();
            return Json(new { sbl.Sablon, sbl.SablonHtml, sbl.MailSablonlariEkleri }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetDetail(int id, int tbInx)
        {

            var model = (from s in _entities.SinavTipleris
                         join sl in _entities.SinavTipleris on s.SinavTipID equals sl.SinavTipID
                         join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                         where s.SinavTipID == id
                         select new FrSinavTipleri
                         {
                             SinavTipID = s.SinavTipID,
                             SinavTipKod = s.SinavTipKod,
                             SinavAdi = sl.SinavAdi,

                             EnstituKod = s.EnstituKod,
                             EnstituAd = e.EnstituAd,
                             SinavTipGrupID = s.SinavTipGrupID,
                             SinavTipGrupAdi = s.SinavTipGruplari.SinavTipGrupAdi,
                             OzelNot = s.OzelNot,
                             OzelNotTipID = s.OzelNotTipID, 
                             IsAktif = s.IsAktif,
                             IslemTarihi = s.IslemTarihi,
                             IslemYapan = s.Kullanicilar.KullaniciAdi,
                             IslemYapanID = s.IslemYapanID,
                             IslemYapanIP = s.IslemYapanIP,
                             SinavNotlaris = s.SinavNotlaris.ToList(),
                             SinavTiplerSubSinavAraliks = s.SinavTiplerSubSinavAraliks.ToList(), 
                            

                             SinavTipleriOtNotAraliklariList = (from s2 in s.SinavTipleriOTNotAraliklaris.Where(p => p.EnstituKod == s.EnstituKod && p.SinavTipID == s.SinavTipID)
                                                                join ot in _entities.OgrenimTipleris on new { s.EnstituKod, s2.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                                                join otl in _entities.OgrenimTipleris on ot.OgrenimTipID equals otl.OgrenimTipID

                                                                select new KrSinavTipleriOtNotAraliklari
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

            if (_entities.SinavTipleriOT_SNA.Any(p => p.SinavTipID == model.SinavTipID))
            {
                var qmodel = (from s in _entities.SinavTipleriOT_SNA.Where(p => p.SinavTipID == model.SinavTipID)
                              select new FrSinavTipleriSpa
                              {
                                  SinavTipleriOT_SNAID = s.SinavTipleriOT_SNAID,
                                  SinavTipID = s.SinavTipID,
                                  SinavTipleriOT_SNA_PR = s.SinavTipleriOT_SNA_PR.ToList(),
                                  SinavTipleriOtNotAraliklariList = (from s2 in s.SinavTipleriOT_SNA_OT.Where(p => p.SinavTipleriOT_SNAID == s.SinavTipleriOT_SNAID)
                                                                     join ot in _entities.OgrenimTipleris on new { s.SinavTipleri.EnstituKod, s2.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                                                                     join otl in _entities.OgrenimTipleris on ot.OgrenimTipID equals otl.OgrenimTipID

                                                                     select new KrSinavTipleriOtNotAraliklari
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
                model.FrSinavTipleriSpa = qmodel;
            }

            var ots = _entities.OgrenimTipleris.Where(p => p.EnstituKod == model.EnstituKod && p.IsAktif).ToList();
            var dilDOngu = new List<bool> { { true }, { false } };
            var krNotAralikMld = new List<KrSinavTipleriOtNotAraliklari>();
            foreach (var item in dilDOngu)
            {
                var qotNa = (from s in ots
                             join na in _entities.SinavTipleriOTNotAraliklaris.Where(p => p.Ingilizce == item && p.SinavTipID == model.SinavTipID && p.EnstituKod == model.EnstituKod) on s.OgrenimTipKod equals na.OgrenimTipKod into def1
                             from notAr in def1.DefaultIfEmpty()
                             select new KrSinavTipleriOtNotAraliklari
                             {
                                 OgrenimTipKod = s.OgrenimTipKod,
                                 OgrenimTipAdi = s.OgrenimTipAdi,
                                 Ingilizce = item,
                                 IsGecerli = notAr?.IsGecerli ?? false,
                                 IsIstensin = notAr?.IsIstensin ?? false,
                                 IsOzelNotAralik = notAr != null && notAr.IsOzelNotAralik,
                                 Min = notAr?.Min,
                                 Max = notAr?.Max,
                                 IstenmeyenProgramlar = notAr != null ? (notAr.SinavTipleriOTNotAraliklariGecersizProgramlars.Select(sq => new CmbStringDto { Value = sq.ProgramKod, Caption = sq.Programlar.ProgramAdi }).ToList()) : new List<CmbStringDto>()
                             }).ToList();
                krNotAralikMld.AddRange(qotNa);
            }
            model.SinavTipleriOtNotAraliklariList = krNotAralikMld.OrderBy(o => o.Ingilizce).ThenBy(t => t.OgrenimTipAdi).ToList();
            model.SelectedTabIndex = tbInx;
            return View(model);
        }

        public ActionResult StProgramaOzelNotKriterEkle(int? id, int sinavTipId, string dlgid)
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            ViewBag.MmMessage = mmMessage;
            var model = new KmSinavTipleriSpna();

            var stip = _entities.SinavTipleris.First(p => p.SinavTipID == sinavTipId);
            if (id.HasValue)
            {
                var data = _entities.SinavTipleriOT_SNA.FirstOrDefault(p => p.SinavTipleriOT_SNAID == id);
                if (data != null)
                {
                    model.SinavTipleriOT_SNAID = data.SinavTipleriOT_SNAID;
                    model.SinavTipID = data.SinavTipID;
                    model.IPProgramKod = data.SinavTipleriOT_SNA_PR.Select(s => s.ProgramKod).ToList();
                }

                var ots = _entities.OgrenimTipleris.Where(p => p.EnstituKod == stip.EnstituKod && p.IsAktif).ToList();
                var dilDOngu = new List<bool> { { true }, { false } };
                var krNotAralikMld = new List<KrSinavTipleriOtNotAraliklari>();
                foreach (var item in dilDOngu)
                {
                    var qotNa = (from s in ots
                                 join st in stip.SinavTipleriOTNotAraliklaris.Where(p => p.Ingilizce == item && p.SinavTipID == model.SinavTipID && p.EnstituKod == stip.EnstituKod) on s.OgrenimTipKod equals st.OgrenimTipKod into def2
                                 from notAnaAr in def2.DefaultIfEmpty()
                                 join na in _entities.SinavTipleriOT_SNA_OT.Where(p => p.Ingilizce == item && p.SinavTipleriOT_SNAID == model.SinavTipleriOT_SNAID) on s.OgrenimTipKod equals na.OgrenimTipKod into def1
                                 from notAr in def1.DefaultIfEmpty()
                                 select new KrSinavTipleriOtNotAraliklari
                                 {
                                     OgrenimTipKod = s.OgrenimTipKod,
                                     OgrenimTipAdi = s.OgrenimTipAdi,
                                     Ingilizce = item,
                                     IsGecerli = notAr?.IsGecerli ?? (notAnaAr?.IsGecerli ?? false),
                                     IsIstensin = notAr?.IsIstensin ?? (notAnaAr?.IsIstensin ?? false),
                                     IsOzelNotAralik = notAr?.IsOzelNotAralik ?? (notAnaAr != null && notAnaAr.IsOzelNotAralik),
                                     Min = notAr != null ? notAr.Min : notAnaAr?.Min,
                                     Max = notAr != null ? notAr.Max : notAnaAr?.Max
                                 }).ToList();
                    krNotAralikMld.AddRange(qotNa);


                }
                model.KrSinavTipleriOtNotAraliklaris = krNotAralikMld.OrderBy(o => o.Ingilizce).ThenBy(t => t.OgrenimTipAdi).ToList();
            }
            else
            {
                var data = _entities.SinavTipleris.FirstOrDefault(p => p.SinavTipID == sinavTipId);
                if (data != null)
                {
                    model.SinavTipID = data.SinavTipID;


                }

                var ots = _entities.OgrenimTipleris.Where(p => p.EnstituKod == stip.EnstituKod && p.IsAktif).ToList();
                var dilDOngu = new List<bool> { { true }, { false } };
                var krNotAralikMld = new List<KrSinavTipleriOtNotAraliklari>();
                foreach (var item in dilDOngu)
                {
                    var qotNa = (from s in ots
                                 join na in data.SinavTipleriOTNotAraliklaris.Where(p => p.Ingilizce == item && p.SinavTipID == model.SinavTipID && p.EnstituKod == stip.EnstituKod) on s.OgrenimTipKod equals na.OgrenimTipKod into def1
                                 from notAr in def1.DefaultIfEmpty()
                                 select new KrSinavTipleriOtNotAraliklari
                                 {
                                     OgrenimTipKod = s.OgrenimTipKod,
                                     OgrenimTipAdi = s.OgrenimTipAdi,
                                     Ingilizce = item,
                                     IsGecerli = notAr?.IsGecerli ?? false,
                                     IsIstensin = notAr?.IsIstensin ?? false,
                                     IsOzelNotAralik = notAr != null && notAr.IsOzelNotAralik,
                                     Min = notAr?.Min,
                                     Max = notAr?.Max
                                 }).ToList();
                    krNotAralikMld.AddRange(qotNa);


                }
                model.KrSinavTipleriOtNotAraliklaris = krNotAralikMld.OrderBy(o => o.Ingilizce).ThenBy(t => t.OgrenimTipAdi).ToList();
            }




            var nContains = _entities.SinavTipleriOT_SNA_PR.Where(p => p.SinavTipleriOT_SNA.SinavTipID == model.SinavTipID && p.SinavTipleriOT_SNAID != model.SinavTipleriOT_SNAID).Select(s => s.ProgramKod).ToList();
            var pr = _entities.Programlars.Where(p => nContains.Contains(p.ProgramKod) == false && p.AnabilimDallari.EnstituKod == stip.EnstituKod).OrderBy(o => o.ProgramAdi).ToList();
            var dataR = pr.Select(s => new KulaniciProgramYetkiModel
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
        public ActionResult StProgramaOzelNotKriterEkle(KmSinavTipleriSpna kModel, string dlgid = "")
        {

            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            //Sınav Puanı Bilgileri 

            var stip = _entities.SinavTipleris.First(p => p.SinavTipID == kModel.SinavTipID);


            //Sinav ogrenimTip Not Araliklari
            var qNaOgrenimTipKod = kModel.NAOgrenimTipKod.Select((s, inx) => new { s, inx }).ToList();
            var qNaIngilizce = kModel.NAIngilizce.Select((s, inx) => new { s, inx }).ToList();
            var qNaIsIstensin = kModel.NAIsIstensin.Select((s, inx) => new { s = (s == 1 ? true : false), inx }).ToList();
            var qNaIsGecerli = kModel.NAIsGecerli.Select((s, inx) => new { s = (s == 1 ? true : false), inx }).ToList();
            var qNaMin = kModel.NAMin.Select((s, inx) => new { s, inx }).ToList();
            var qNaMax = kModel.NAMax.Select((s, inx) => new { s, inx }).ToList();

            var qNaOgrenimTipKodNotAralik = (from ot in qNaOgrenimTipKod
                                             join ing in qNaIngilizce on ot.inx equals ing.inx
                                             join gcr in qNaIsGecerli on ot.inx equals gcr.inx
                                             join ist in qNaIsIstensin on ot.inx equals ist.inx
                                             join min in qNaMin on ot.inx equals min.inx
                                             join max in qNaMax on ot.inx equals max.inx
                                             join otl in _entities.OgrenimTipleris on ot.s equals otl.OgrenimTipID
                                             select new KrSinavTipleriOtNotAraliklari
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



            if (!qNaOgrenimTipKodNotAralik.Any(p => p.IsIstensin))
            {
                mmMessage.Messages.Add("Kayıt edilmek istenen sınav tipinin hangi öğrenim seviyelerinde isteneceğini belirleyiniz!");
            }
            foreach (var item in qNaOgrenimTipKodNotAralik)
            {

                if (item.IsIstensin)
                {
                    if (item.Min.HasValue == false || item.Max.HasValue == false)
                    {
                        string msg = item.OgrenimTipAdi + " " + (item.Ingilizce ? " İngilizce" : " Türkçe") + " not aralık kriteri için min ve max not aralıkları boş bırakılamaz";
                        mmMessage.Messages.Add(msg);
                        item.SuccessRow = false;
                        if (item.Min.HasValue == false) item.PropName.Add("NAMin");
                        if (item.Max.HasValue == false) item.PropName.Add("NAMax");

                    }
                    else if (item.Min.Value < 0 || item.Max.Value < 0)
                    {
                        string msg = item.OgrenimTipAdi + " " + (item.Ingilizce ? " İngilizce" : " Türkçe") + " not aralık kriteri için min ve max not aralıkları 0 dan büyük olmalıdır";
                        mmMessage.Messages.Add(msg);
                        item.SuccessRow = false;
                        if (item.Min.Value < 0) item.PropName.Add("NAMin");
                        if (item.Max.Value < 0) item.PropName.Add("NAMax");
                    }
                    else if (item.Min.Value > item.Max.Value)
                    {
                        string msg = item.OgrenimTipAdi + " " + (item.Ingilizce ? " İngilizce" : " Türkçe") + " not aralık kriteri için min not max not'dan büyük olamaz";
                        mmMessage.Messages.Add(msg);
                        item.SuccessRow = false;
                        item.PropName.Add("NAMin");
                        item.PropName.Add("NAMax");
                    }
                }
            }

            var qprKods = _entities.SinavTipleriOT_SNA_PR.Where(p => p.SinavTipleriOT_SNAID != kModel.SinavTipleriOT_SNAID && p.SinavTipleriOT_SNA.SinavTipID == kModel.SinavTipID).Select(s => s.ProgramKod).Distinct().ToList();
            kModel.IPProgramKod = kModel.IPProgramKod.Where(p => !qprKods.Contains(p)).ToList();
            if (kModel.IPProgramKod.Count == 0)
            {
                mmMessage.Messages.Add("Yeni not tanımını kayıt edebilmek için en az 1 program seçmeniz gerekmektedir!");
            }
            #endregion
            if (mmMessage.Messages.Count == 0)
            {


                if (kModel.SinavTipleriOT_SNAID <= 0)
                {
                    var enst = _entities.SinavTipleriOT_SNA.Add(new SinavTipleriOT_SNA
                    {
                        SinavTipID = kModel.SinavTipID
                    });
                    _entities.SaveChanges();
                    kModel.SinavTipleriOT_SNAID = enst.SinavTipleriOT_SNAID;

                }
                else
                {
                    var data = _entities.SinavTipleriOT_SNA.First(p => p.SinavTipleriOT_SNAID == kModel.SinavTipleriOT_SNAID);
                    var lstPr = _entities.SinavTipleriOT_SNA_PR.Where(p => p.SinavTipleriOT_SNAID == kModel.SinavTipleriOT_SNAID).ToList();
                    _entities.SinavTipleriOT_SNA_PR.RemoveRange(lstPr);

                    var otNotAr = _entities.SinavTipleriOT_SNA_OT.Where(p => p.SinavTipleriOT_SNAID == kModel.SinavTipleriOT_SNAID).ToList();
                    _entities.SinavTipleriOT_SNA_OT.RemoveRange(otNotAr);
                }
                foreach (var item in kModel.IPProgramKod)
                {
                    _entities.SinavTipleriOT_SNA_PR.Add(new Models.SinavTipleriOT_SNA_PR { SinavTipleriOT_SNAID = kModel.SinavTipleriOT_SNAID, ProgramKod = item });
                }

                if (stip.SinavTipGrupID == SinavTipGrupEnum.DilSinavlari || stip.SinavTipGrupID == SinavTipGrupEnum.Ales_Gree)
                    foreach (var item in qNaOgrenimTipKodNotAralik)
                    {
                        var qST = _entities.SinavTipleriOT_SNA_OT.Add(new SinavTipleriOT_SNA_OT
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
                _entities.SaveChanges();
                mmMessage.IsSuccess = true;
                MessageBox.Show("Not kriteri tanımlandı", "Kayıt işlemi");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            if (mmMessage.IsSuccess) mmMessage.IsCloseDialog = true;

            ViewBag.MmMessage = mmMessage;
            kModel.KrSinavTipleriOtNotAraliklaris = qNaOgrenimTipKodNotAralik;
            foreach (var item in qNaOgrenimTipKodNotAralik)
            {
                item.IsOzelNotAralik = item.IsIstensin;
            }

            var prK = kModel.IPProgramKod;
            var nContains = _entities.SinavTipleriOT_SNA_PR.Where(p => p.SinavTipleriOT_SNA.SinavTipID == kModel.SinavTipID && p.SinavTipleriOT_SNAID != kModel.SinavTipleriOT_SNAID).Select(s => s.ProgramKod).ToList();
            var pr = _entities.Programlars.Where(p => nContains.Contains(p.ProgramKod) == false && p.AnabilimDallari.EnstituKod == stip.EnstituKod && p.IsAktif).OrderBy(o => o.ProgramAdi).ToList();
            var dataR = pr.Select(s => new KulaniciProgramYetkiModel
            {
                ProgramKod = s.ProgramKod,
                ProgramAdi = s.ProgramAdi,
                YetkiVar = prK.Contains(s.ProgramKod)
            }).OrderByDescending(o => o.YetkiVar).ThenBy(t => t.ProgramAdi).ToList();
            ViewBag.Programlar = dataR;
            ViewBag.stip = stip;
            return View(kModel);
        }



        public ActionResult Sil(int id, string enstituKod)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();

            var kayit = _entities.SinavTipleris.FirstOrDefault(p => p.SinavTipID == id && enstKods.Contains(p.EnstituKod) && p.EnstituKod == enstituKod);
            var pAdi = _entities.SinavTipleris.First(p => p.SinavTipID == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + pAdi.SinavAdi + "' İsimli Sınav Tipi Silindi!";
                    _entities.SinavTipleris.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + pAdi.SinavAdi + "' Sınav Tipi Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Ünvanlar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Sınav Tipi sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult SilStpk(int id)
        {

            var kayit = _entities.SinavTipleriOT_SNA.FirstOrDefault(p => p.SinavTipleriOT_SNAID == id);
            var pAdi = _entities.SinavTipleris.First(p => p.SinavTipID == kayit.SinavTipID);
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + pAdi.SinavAdi + "' İsimli Sınav tipine ait programa özel not kriteri Silindi!";
                    _entities.SinavTipleriOT_SNA.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + pAdi.SinavAdi + "' İsimli Sınav tipine ait programa özel not kriteri Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "SinavTipleri/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Sınav tipine ait programa özel not kriteri sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
