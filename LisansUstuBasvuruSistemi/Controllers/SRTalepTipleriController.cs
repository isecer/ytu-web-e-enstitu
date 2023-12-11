using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.SrTalepTipleri)]
    public class SrTalepTipleriController : Controller
    {

        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmSrTalepTipleri { });
        }
        [HttpPost]
        public ActionResult Index(FmSrTalepTipleri model)
        {

            var q = from s in _entities.SRTalepTipleris
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    select new FrSrTalepTipleri
                    {
                        SRTalepTipID = s.SRTalepTipID,
                        IsTezSinavi = s.IsTezSinavi, 
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapan = k.Ad + " " + k.Soyad,
                        IslemYapanIP = s.IslemYapanIP,
                        TalepTipAdi = s.TalepTipAdi  

                    };

            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (model.IsTezSinavi.HasValue) q = q.Where(p => p.IsTezSinavi == model.IsTezSinavi);
            if (!model.TalepTipAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.TalepTipAdi.Contains(model.TalepTipAdi));
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.TalepTipAdi); 
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            }; 
            ViewBag.IndexModel = indexModel;
            ViewBag.IsTezSinavi = new SelectList(ComboData.GetCmbEvetHayirData(true), "Value", "Caption", model.IsTezSinavi);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }

        public ActionResult Kayit(int? id)
        {
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var model = new SRTalepTipleri();  
            if (id.HasValue)
            {
                var data = _entities.SRTalepTipleris.FirstOrDefault(p => p.SRTalepTipID == id);
                if (data != null)
                {
                    model = data;  
                }
            } 
            ViewBag.Aylars = SrTalepleriBus.GetCmbAylar(false); 
            ViewBag.KullaniciTipleris = KullanicilarBus.GetCmbKullaniciTipleri(false, false); 
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(SRTalepTipleri kModel)
        {
         
            var mmMessage = new MmMessage();
            #region Kontrol

            if (kModel.SRTalepTipID <= 0)
            {
                mmMessage.Messages.Add("Talpe Tip ID bilgisi sıfırdan büyük olmalıdır."); 
            }
            if (kModel.TalepTipAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Talpe Tip Adı Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TalepTipAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TalepTipAdi" });
           
            #endregion
            if (mmMessage.Messages.Count == 0)
            {
               
                if (kModel.SRTalepTipID <= 0)
                {

                    kModel.IsAktif = true;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;
                    kModel.IslemTarihi = DateTime.Now;
                    _entities.SRTalepTipleris.Add(new SRTalepTipleri
                    {
                        IsTezSinavi = kModel.IsTezSinavi, 
                        IsAktif = kModel.IsAktif 

                    });
                    _entities.SaveChanges();
                }
                else
                {
                    var data = _entities.SRTalepTipleris.First(p => p.SRTalepTipID == kModel.SRTalepTipID);
                    data.IsTezSinavi = kModel.IsTezSinavi; 
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;  
                }

                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            { 
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
             
            ViewBag.MmMessage = mmMessage;   
            return View(kModel);
        } 
    }
}