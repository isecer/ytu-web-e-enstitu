using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class YGMulakatSureciController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        [Authorize(Roles = RoleNames.YGMulakatSureci)]
        public ActionResult Index(string EKD)
        {
            return Index(new fmMulakatSureci() { PageSize = 10 }, EKD);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.YGMulakatSureci)]
        public ActionResult Index(fmMulakatSureci model, string EKD)
        {
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var kulls = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();
            var bbModel = new MulakatBilgiModel();
            var BasvuruSurecID = Management.getAktifMulakatSurecID(_EnstituKod, BasvuruSurecTipi.YatayGecisBasvuru, null, true);
            bbModel.SistemBasvuruyaAcik = BasvuruSurecID.HasValue; 

            if (BasvuruSurecID.HasValue == false)
            {
                BasvuruSurecID = Management.getAktifMulakatSonucGiris(_EnstituKod);
            }
            bbModel.SistemGirisSinavBilgiAcik = BasvuruSurecID.HasValue;
            bbModel.AktifSurecID = BasvuruSurecID ?? 0;
            bbModel.BasvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID.Value).FirstOrDefault();
            if (bbModel.BasvuruSurec != null)
                bbModel.DonemAdi = db.Donemlers.Where(p => p.DonemID == bbModel.BasvuruSurec.DonemID ).First().DonemAdi;

            bbModel.Enstitü = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod ).First();
            bbModel.EnstituYetki = UserIdentity.Current.EnstituKods.Contains(_EnstituKod);
            var kulProgIds = Management.GetUserProgramKods(UserIdentity.Current.Id, _EnstituKod);
            var prgramlar = (from s in db.Programlars
                             join b in db.AnabilimDallaris.Where(p =>  p.EnstituKod == _EnstituKod) on s.AnabilimDaliKod equals b.AnabilimDaliKod
                             where kulProgIds.Contains(s.ProgramKod)
                             orderby b.AnabilimDaliAdi, s.ProgramAdi
                             select new
                             {
                                 s.ProgramKod,
                                 s.ProgramAdi,
                                 b.AnabilimDaliKod,
                                 b.AnabilimDaliAdi
                             }).ToList();
            var pList = new List<string>();
            int inx = 0;
            foreach (var item in prgramlar)
            {
                inx++;
                pList.Add(inx + ") " + item.AnabilimDaliAdi + " / " + item.ProgramAdi + " [" + item.ProgramKod + "]");
            }
            var eKods = UserIdentity.Current.EnstituKods;

            var q = from m in db.Mulakats
                    join s in db.BasvuruSurecs.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru) on m.BasvuruSurecID equals s.BasvuruSurecID
                    join prg in db.Programlars on m.ProgramKod equals prg.ProgramKod
                    join bl in db.AnabilimDallaris.Where(p =>  p.EnstituKod == _EnstituKod) on prg.AnabilimDaliKod equals bl.AnabilimDaliKod
                    join ot in db.OgrenimTipleris on new { s.EnstituKod, m.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                    join otl in db.OgrenimTipleris on new { ot.OgrenimTipID } equals new { otl.OgrenimTipID }
                    join d in db.Donemlers on s.DonemID equals d.DonemID
                    join k in db.Kullanicilars on m.IslemYapanID equals k.KullaniciID
                    where eKods.Contains(s.EnstituKod) && s.EnstituKod == _EnstituKod && kulProgIds.Contains(prg.ProgramKod)
                    select new
                    {
                        m.MulakatID,
                        m.BasvuruSurec,
                        m.BasvuruSurecID,
                        m.ProgramKod,
                        bl.AnabilimDaliAdi,
                        m.OgrenimTipKod,
                        prg.ProgramAdi,
                        otl.OgrenimTipAdi,
                        BasvuruSurecAdi = s.BaslangicYil + " / " + s.BitisYil + " " + d.DonemAdi,
                        s.BaslangicTarihi,
                        s.BitisTarihi,
                        SonucGirisBaslangicTarihi = s.SonucGirisBaslangicTarihi.Value,
                        SonucGirisBitisTarihi = s.SonucGirisBitisTarihi.Value,
                        d.DonemAdi,
                        m.IslemTarihi,
                        m.IslemYapanID,
                        IslemYapan = k.Ad + " " + k.Soyad,
                        m.IslemYapanIp

                    };

            if (model.ProgramKod.IsNullOrWhiteSpace() == false) q = q.Where(p => p.ProgramKod == model.ProgramKod);
            if (model.ProgramAdi.IsNullOrWhiteSpace() == false) q = q.Where(p => p.AnabilimDaliAdi.Contains(model.ProgramAdi) || p.ProgramAdi.Contains(model.ProgramAdi));
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod);
            if (model.BasvuruSurecID.HasValue) q = q.Where(p => p.BasvuruSurecID == model.BasvuruSurecID);
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.BaslangicTarihi);
            var qdata = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new frMulakatSureci
            {
                MulakatID = s.MulakatID,
                BasvuruSurecAdi = s.BasvuruSurecAdi,
                AnabilimDaliAdi = s.AnabilimDaliAdi,
                ProgramKod = s.ProgramKod,
                ProgramAdi = s.ProgramAdi,
                BaslangicTarihi = s.BaslangicTarihi,
                BitisTarihi = s.BitisTarihi,
                SonucGirisBaslangicTarihi = s.SonucGirisBaslangicTarihi,
                SonucGirisBitisTarihi = s.SonucGirisBitisTarihi,
                OgrenimTipKod = s.OgrenimTipKod,
                OgrenimTipAdi = s.OgrenimTipAdi,
                BasvuruSurecID = s.BasvuruSurecID,
                IslemTarihi = s.IslemTarihi,
                IslemYapanID = s.IslemYapanID,
                IslemYapan = s.IslemYapan,
                IslemYapanIp = s.IslemYapanIp
            }).ToList();

            model.Data = qdata;

            bbModel.Programlar = pList;
           
            ViewBag.bModel = bbModel;

            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(_EnstituKod ,true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.BasvuruSurecID = new SelectList(Management.getbasvuruSurecleri(_EnstituKod, BasvuruSurecTipi.YatayGecisBasvuru ,true), "Value", "Caption", model.BasvuruSurecID);
            return View(model);
        }


        [Authorize(Roles = RoleNames.YGMulakatKayıt)]
        public ActionResult MulakatKayit(int? id, string EKD, string dlgid)
        {
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            var model = new kmMulakat();

            var kulProgIDs = Management.GetUserProgramKods(UserIdentity.Current.Id, _EnstituKod);
            if (id.HasValue)
            {
                var mlkt = db.Mulakats.Where(p => p.MulakatID == id.Value).FirstOrDefault();
                if (mlkt != null)
                {
                    var BasvuruSurecID = Management.getAktifMulakatSurecID(_EnstituKod, BasvuruSurecTipi.YatayGecisBasvuru, mlkt.BasvuruSurecID);




                    if (kulProgIDs.Contains(mlkt.ProgramKod) == false)
                    {
                        MmMessage.Messages.Add("Yetkiniz olmayan bir mülakat kaydını düzeltemezsiniz!");
                        Management.SistemBilgisiKaydet("Yetkili olunmayan mülakat bilgisi düzenlenmek istendi! \r\nMülakatID=" + mlkt.MulakatID + " \r\n ProgramKod=" + mlkt.ProgramKod, "MulakatSureci/MulakatKayit", LogType.Saldırı);
                    }
                    else if (BasvuruSurecID.HasValue == false && UserIdentity.Current.IsAdmin == false)
                    {
                        MmMessage.Messages.Add("Düzenlemek istediğiniz mülakatın süreci geçmiştir! Düzeltme işlemi yapamazsınız!");
                    }
                    else
                    {
                        model.MulakatID = mlkt.MulakatID;
                        model.BasvuruSurecID = mlkt.BasvuruSurecID;
                        model.ProgramKod = mlkt.ProgramKod;
                        model.OgrenimTipKod = mlkt.OgrenimTipKod;
                        model.MulakatDetayi = (from s in mlkt.MulakatDetays
                                               join k in db.Kampuslers on s.KampusID equals k.KampusID
                                               join st in db.MulakatSinavTurleris on s.MulakatSinavTurID equals st.MulakatSinavTurID
                                               select new krMulakatDetay
                                               {
                                                   MulakatID = mlkt.MulakatID,
                                                   MulakatSinavTurID = s.MulakatSinavTurID,
                                                   MulakatSinavTurAdi = st.MulakatSinavTurAdi,
                                                   YuzdeOran = s.YuzdeOran,
                                                   SinavTarihi = s.SinavTarihi,
                                                   KampusAdi = k.KampusAdi,
                                                   KampusID = s.KampusID,
                                                   YerAdi = s.YerAdi
                                               }).ToList();
                        model.MulakatJuris = mlkt.MulakatJuris.OrderByDescending(o => o.IsAsil).ThenBy(t => t.SiraNo).ToList();

                    }
                }
            }
            else
            {
                var BasvuruSurecID = Management.getAktifMulakatSurecID(_EnstituKod, BasvuruSurecTipi.YatayGecisBasvuru, null);

                if (!BasvuruSurecID.HasValue)
                {
                    string msg = "Mülakat Jüri & Sınav yer bilgisi girişi için aktif bir süreç bulunmamaktadır! İşlem yapılamaz!";
                    MmMessage.Messages.Add(msg);
                }
                else
                {
                    model.BasvuruSurecID = BasvuruSurecID.Value;
                }
            }
            if (MmMessage.Messages.Count > 0)
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
                return RedirectToAction("Index");
            }
            var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == model.BasvuruSurecID).First();

            ViewBag.isYuzdeGirisBolum = bsurec.isYuzdeGirisBolum;
            ViewBag.MmMessage = MmMessage;
            ViewBag.ProgramKod = new SelectList(Management.cmbGetKullaniciMulakatProgramlari(model.BasvuruSurecID, UserIdentity.Current.Id, true, (model.MulakatID <= 0 ? (int?)null : model.MulakatID)), "Value", "Caption", model.ProgramKod);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbGetKullaniciMulakatOgrenimTipleri(model.BasvuruSurecID, UserIdentity.Current.Id, model.ProgramKod, true, (model.MulakatID <= 0 ? (int?)null : model.MulakatID)), "Value", "Caption", model.OgrenimTipKod);
            ViewBag._MulakatSinavTurID = new SelectList(Management.cmbGetMulakatSinavTurleri(true), "Value", "Caption");
            ViewBag._KampusID = new SelectList(Management.cmbGetKampusler(true), "Value", "Caption");
            ViewBag._MulakatSaatleri = new SelectList(Management.cmbGetMulakatSaatleri(), "Value", "Caption");

            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.YGMulakatKayıt)]
        public ActionResult MulakatKayit(kmMulakat kModel, string EKD, string dlgid)
        {
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            var kulProgIDs = Management.GetUserProgramKods(UserIdentity.Current.Id, _EnstituKod);
            var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID).First();

            if (kModel.YuzdeOran == null) kModel.YuzdeOran = new List<int>();
            if (kModel.MulakatSinavTurID == null) kModel.MulakatSinavTurID = new List<int>();
            if (bsurec.isYuzdeGirisBolum == false)
            {
                for (int i = 0; i < kModel.MulakatSinavTurID.Count; i++)
                {
                    kModel.YuzdeOran.Add(0);
                }
            }

            var qMulakatSinavTurID = kModel.MulakatSinavTurID.Select((s, inx) => new { s, inx }).ToList();
            var qYuzdeOran = kModel.YuzdeOran.Select((s, inx) => new { s, inx }).ToList();
            var qSinavTarihi = kModel.SinavTarihi.Select((s, inx) => new { s, inx }).ToList();
            var qKampusID = kModel.KampusID.Select((s, inx) => new { s, inx }).ToList();
            var qYerAdi = kModel.YerAdi.Select((s, inx) => new { s, inx }).ToList();
            var bsurecSt = bsurec.BasvuruSurecMulakatSinavTurleris.ToList();
            var qMulakatDetay = (from snID in qMulakatSinavTurID
                                 join sydO in qYuzdeOran on snID.inx equals sydO.inx
                                 join sTar in qSinavTarihi on snID.inx equals sTar.inx
                                 join kmps in qKampusID on snID.inx equals kmps.inx
                                 join yerAd in qYerAdi on snID.inx equals yerAd.inx
                                 join st in bsurecSt on snID.s equals st.MulakatSinavTurID into defSt
                                 from bST in defSt.DefaultIfEmpty()
                                 select new
                                 {
                                     Index = snID.inx,
                                     MulakatSinavTurID = snID.s,
                                     SinavTarihi = sTar.s,
                                     KampusID = kmps.s,
                                     YerAdi = yerAd.s,
                                     YuzdeOran = bsurec.isYuzdeGirisBolum ? sydO.s : (bST != null ? (bST.YuzdeOran ?? 0) : 0)
                                 }).ToList();

            var qIsAsil = kModel.IsAsil.Select((s, inx) => new { s, inx }).ToList();
            var qJuriAdi = kModel.JuriAdi.Select((s, inx) => new { s, inx }).ToList();
            var qSiraNo = kModel.SiraNo.Select((s, inx) => new { s, inx }).ToList();
            var qJuriler = (from asil in qIsAsil
                            join juri in qJuriAdi on asil.inx equals juri.inx
                            join sira in qSiraNo on juri.inx equals sira.inx
                            select new
                            {
                                Index = asil.inx,
                                IsAsil = asil.s,
                                JuriAdi = juri.s,
                                SiraNo = sira.s
                            }).ToList();
            if (kModel.MulakatID > 0)
            {
                var mlkt = db.Mulakats.Where(p => p.MulakatID == kModel.MulakatID).FirstOrDefault();
                if (mlkt != null)
                {
                    var BasvuruSurecID = Management.getAktifMulakatSurecID(_EnstituKod, BasvuruSurecTipi.YatayGecisBasvuru, mlkt.BasvuruSurecID);

                    if (kulProgIDs.Contains(mlkt.ProgramKod) == false)
                    {
                        MmMessage.Messages.Add("Yetkiniz olmayan bir mülakat kaydını düzeltemezsiniz!");
                        Management.SistemBilgisiKaydet("Yetkili olunmayan mülakat bilgisi düzenlenmek istendi! \r\nMülakatID=" + mlkt.MulakatID + " \r\n ProgramKod=" + mlkt.ProgramKod, "MulakatSureci/MulakatKayit", LogType.Saldırı);
                    }
                    else if (BasvuruSurecID.HasValue == false && UserIdentity.Current.IsAdmin == false)
                    {
                        MmMessage.Messages.Add("Düzenlemek istediğiniz mülakatın süreci geçmiştir! Düzeltme işlemi yapamazsınız!");
                    }
                }
            }
            else
            {
                if (kModel.BasvuruSurecID <= 0)
                {
                    MmMessage.Messages.Add("Düzenlemek istediğiniz mülakata sürecine ait başvuru süreci bulunamadı! Düzeltme işlemi yapamazsınız!");
                }
            }

            if (kModel.ProgramKod.IsNullOrWhiteSpace())
            {
                string msg = "Program seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
            }
            else if (kulProgIDs.Contains(kModel.ProgramKod) == false)
            {
                MmMessage.Messages.Add("Yetkiniz olmayan bir programa mülakat kaydı yapamazsınız!");
                Management.SistemBilgisiKaydet("Yetkili olunmayan programa mülakat bilgisi eklenmek istendi! \r\nMülakatID=" + kModel.MulakatID + " \r\n ProgramKod=" + kModel.ProgramKod, "MulakatSureci/MulakatKayit", LogType.Saldırı);
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProgramKod" });

            if (kModel.OgrenimTipKod <= 0)
            {
                string msg = "Öğrenim seviyesi seçiniz!";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimTipKod" });

            var bsMulSinTurs = db.BasvuruSurecMulakatSinavTurleris.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID).ToList();
            if (qMulakatDetay.Count == 0)
            {

                string msg = "Sınav bilgilerini ekleyiniz!";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_MulakatSinavTurID" });
                if (bsurec.isYuzdeGirisBolum)
                {
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_YuzdeOran" });
                }
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_SinavTarihi" });
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_KampusID" });
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_YerAdi" });
            }


            //else if (qMulakatDetay.Count != bsMulSinTurs.Count)
            //{
            //    //    var varolan = qMulakatDetay.Select(s => s.MulakatSinavTurID).ToList();
            //    //    var eklenecek = bsMulSinTurs.Where(p => varolan.Contains(p.MulakatSinavTurID) == false).Select(s => s.MulakatSinavTurID).ToList();
            //    //    var eksikSinavTurAds = db.MulakatSinavTurleris.Where(p =>  eklenecek.Contains(p.MulakatSinavTurID)).Select(s => s.MulakatSinavTurAdi).ToList();
            //    //    string msg = "Eksik kalan sınav bilgilerini ekleyiniz! (" + string.Join(",", eksikSinavTurAds) + ")";
            //    MmMessage.Messages.Add(msg);
            //    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_MulakatSinavTurID" });
            //    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_SinavTarihi" });
            //    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_KampusID" });
            //    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_YerAdi" });
            //}
            if (qJuriler.Count == 0)
            {
                string msg = "En az 1 Asil 1 Yedek Jüri bilgisi giriniz!";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_JuriAdi" });
            }
            else
            {
                if (qJuriler.Any(p => p.IsAsil) == false)
                {
                    string msg = "Asil Jüri bilgisi giriniz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_JuriAdi" });
                }
                if (qJuriler.Any(p => !p.IsAsil) == false)
                {
                    string msg = "Yedek Jüri bilgisi giriniz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_JuriAdi" });
                }
            }
            if (MmMessage.Messages.Count == 0)
            {
                var cnt = db.Mulakats.Where(p => p.ProgramKod == kModel.ProgramKod && p.OgrenimTipKod == kModel.OgrenimTipKod && p.BasvuruSurecID == kModel.BasvuruSurecID && p.MulakatID != kModel.MulakatID).Count();
                if (cnt > 0)
                {
                    string msg = "Tanımlamak istediğiniz mülakat bilgisi daha önceden sisteme tanımlanmıştır, tekrar tanımlanamaz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
                }

                var eklenenMsTurIDs = qMulakatDetay.Select(s => s.MulakatSinavTurID).ToList();
                var stur = bsMulSinTurs.Where(p => p.Zorunlu && !eklenenMsTurIDs.Contains(p.MulakatSinavTurID)).ToList();
                if (stur.Count > 0)
                {
                    var eklenecek = bsMulSinTurs.Where(p => eklenenMsTurIDs.Contains(p.MulakatSinavTurID) == false && p.Zorunlu).Select(s => s.MulakatSinavTurID).ToList();
                    var eksikSinavTurAds = db.MulakatSinavTurleris.Where(p =>  eklenecek.Contains(p.MulakatSinavTurID)).Select(s => s.MulakatSinavTurAdi).ToList();
                    string msg = "Zorunlu Sınav Bilgilerini Ekleyiniz! (" + string.Join(",", eksikSinavTurAds) + ")";
                    MmMessage.Messages.Add(msg);
                    if (bsurec.isYuzdeGirisBolum)
                    {
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_YuzdeOran" });
                    }
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_MulakatSinavTurID" });
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_SinavTarihi" });
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_KampusID" });
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_YerAdi" });
                }
                else
                {
                    if (qMulakatDetay.Count == 1 && !bsurec.isYuzdeGirisBolum)
                    {

                    }
                    else if (qMulakatDetay.Sum(s => s.YuzdeOran) != 100)
                    {
                        string msg = "Eklenen sınav tür % oranları toplamı 100% oranını sağlamamaktadır. Lütfen kontrol ediniz!";
                        MmMessage.Messages.Add(msg);
                    }
                }


            }

            if (MmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIp = UserIdentity.Ip;
                if (kModel.MulakatID <= 0)
                {
                    var mlk = db.Mulakats.Add(new Mulakat
                    {
                        BasvuruSurecID = kModel.BasvuruSurecID,
                        ProgramKod = kModel.ProgramKod,
                        OgrenimTipKod = kModel.OgrenimTipKod,
                        IslemTarihi = kModel.IslemTarihi,
                        IslemYapanID = kModel.IslemYapanID,
                        IslemYapanIp = kModel.IslemYapanIp
                    });
                    db.SaveChanges();
                    kModel.MulakatID = mlk.MulakatID;

                }
                else
                {

                    var mulakat = db.Mulakats.Where(p => p.MulakatID == kModel.MulakatID).First();
                    mulakat.ProgramKod = kModel.ProgramKod;
                    mulakat.OgrenimTipKod = kModel.OgrenimTipKod;
                    mulakat.IslemTarihi = kModel.IslemTarihi;
                    mulakat.IslemYapanID = kModel.IslemYapanID;
                    mulakat.IslemYapanIp = kModel.IslemYapanIp;
                    var mdetay = db.MulakatDetays.Where(p => p.MulakatID == kModel.MulakatID).ToList();
                    db.MulakatDetays.RemoveRange(mdetay);
                    var jdetay = db.MulakatJuris.Where(p => p.MulakatID == kModel.MulakatID).ToList();
                    db.MulakatJuris.RemoveRange(jdetay);
                }

                foreach (var item in qMulakatDetay)
                {
                    var mdItm = new MulakatDetay { MulakatID = kModel.MulakatID, MulakatSinavTurID = item.MulakatSinavTurID, YuzdeOran = (qMulakatDetay.Count == 1 ? 100 : item.YuzdeOran), SinavTarihi = item.SinavTarihi, KampusID = item.KampusID, YerAdi = item.YerAdi };

                    db.MulakatDetays.Add(mdItm);

                }

                foreach (var item in qJuriler)
                {
                    db.MulakatJuris.Add(new MulakatJuri { MulakatID = kModel.MulakatID, SiraNo = item.SiraNo, JuriAdi = item.JuriAdi, IsAsil = item.IsAsil });
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            if (qMulakatDetay.Count > 0)
            {
                var qmD = (from s in qMulakatDetay
                           join k in db.Kampuslers on s.KampusID equals k.KampusID
                           join st in db.MulakatSinavTurleris on s.MulakatSinavTurID equals st.MulakatSinavTurID
                           select new krMulakatDetay
                           {
                               MulakatID = kModel.MulakatID,
                               MulakatSinavTurID = s.MulakatSinavTurID,
                               MulakatSinavTurAdi = st.MulakatSinavTurAdi,
                               YuzdeOran = s.YuzdeOran,
                               SinavTarihi = s.SinavTarihi,
                               KampusAdi = k.KampusAdi,
                               KampusID = s.KampusID,
                               YerAdi = s.YerAdi
                           }).ToList();
                kModel.MulakatDetayi = qmD;
            }
            if (qJuriler.Count > 0)
            {
                kModel.MulakatJuris = qJuriler.Select(s => new MulakatJuri { MulakatID = kModel.MulakatID, SiraNo = s.SiraNo, JuriAdi = s.JuriAdi, IsAsil = s.IsAsil }).OrderByDescending(o => o.IsAsil).ThenBy(t => t.SiraNo).ToList();
            }
            ViewBag.MmMessage = MmMessage;
            ViewBag.ProgramKod = new SelectList(Management.cmbGetKullaniciMulakatProgramlari(kModel.BasvuruSurecID, UserIdentity.Current.Id, true, (kModel.MulakatID <= 0 ? (int?)null : kModel.MulakatID)), "Value", "Caption", kModel.ProgramKod);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbGetKullaniciMulakatOgrenimTipleri(kModel.BasvuruSurecID, UserIdentity.Current.Id, kModel.ProgramKod, true, (kModel.MulakatID <= 0 ? (int?)null : kModel.MulakatID)), "Value", "Caption", kModel.OgrenimTipKod);
            ViewBag._MulakatSinavTurID = new SelectList(Management.cmbGetMulakatSinavTurleri(true), "Value", "Caption");
            ViewBag._KampusID = new SelectList(Management.cmbGetKampusler(true), "Value", "Caption");
            ViewBag._MulakatSaatleri = new SelectList(Management.cmbGetMulakatSaatleri(), "Value", "Caption");
            ViewBag.isYuzdeGirisBolum = bsurec.isYuzdeGirisBolum;
            return View(kModel);
        }

        public ActionResult getOT(int BasvuruSurecID, string ProgramKod, int? MulakatID = null)
        {
            var lst = Management.cmbGetKullaniciMulakatOgrenimTipleri(BasvuruSurecID, UserIdentity.Current.Id, ProgramKod, true, MulakatID);
            return lst.Select(s => new { s.Value, s.Caption }).toJsonResult();
        }
        public class mulakatDetayKontrolM : MulakatDetay
        {
            public int BasvuruSurecID { get; set; }
            public string SinavSaati { get; set; }
            public List<int?> LMulakatSinavTurID { get; set; }
            public List<int?> LYuzdeOrani { get; set; }
            public mulakatDetayKontrolM()
            {
                LMulakatSinavTurID = new List<int?>();
                LYuzdeOrani = new List<int?>();
            }
        }
        public ActionResult getSdetayKontrtol(mulakatDetayKontrolM data)
        {

            var mmMessage = new MmMessage();
            mmMessage.Title = "Sınav Bilgisini Eklenemedi!";
            int YuzdeOran = 0;
            var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == data.BasvuruSurecID).First();
            int? degisenSinavTurID = null;
            int? degisenYuzdeOran = null;
            if (data.MulakatSinavTurID <= 0)
            {
                string msg = "Sınav türünü seçiniz";
                mmMessage.Messages.Add(msg);
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_MulakatSinavTurID" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_YuzdeOran" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_SinavTarihi" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_KampusID" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_YerAdi" });
            }
            else
            {
                var qMulakatSinavTurID = data.LMulakatSinavTurID.Select((s, inx) => new { s, inx }).ToList();
                if (bsurec.isYuzdeGirisBolum == false)
                {
                    for (int i = 0; i < data.LMulakatSinavTurID.Count; i++)
                    {
                        data.LYuzdeOrani.Add(0);
                    }
                }
                var qYuzdeOran = data.LYuzdeOrani.Select((s, inx) => new { s, inx }).ToList();
                var qMulakatDetay = (from snID in qMulakatSinavTurID
                                     join yo in qYuzdeOran on snID.inx equals yo.inx
                                     select new
                                     {
                                         Index = snID.inx,
                                         MulakatSinavTurID = snID.s,
                                         YuzdeOran = yo.s
                                     }).ToList();

                var stur = db.MulakatSinavTurleris.Where(p => p.MulakatSinavTurID == data.MulakatSinavTurID ).First();
                if (!bsurec.isYuzdeGirisBolum)
                {
                    var bsMul = db.BasvuruSurecMulakatSinavTurleris.Where(p => p.BasvuruSurecID == data.BasvuruSurecID && p.MulakatSinavTurID == stur.MulakatSinavTurID).First();
                    YuzdeOran = bsMul.YuzdeOran.Value;
                }
                else YuzdeOran = data.YuzdeOran;
                if (qMulakatDetay.Where(p => p.MulakatSinavTurID == data.MulakatSinavTurID).Count() > 0)
                {
                    string msg = "'" + stur.MulakatSinavTurAdi + "' sınav türü zaten eklidir tekrar eklenemez!";
                    mmMessage.Messages.Add(msg);
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_MulakatSinavTurID" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_MulakatSinavTurID" });

                if (mmMessage.Messages.Count == 0)
                {
                    if (YuzdeOran <= 0 || YuzdeOran > 100)
                    {
                        string msg = "'" + stur.MulakatSinavTurAdi + "' sınav türü % oranı 1 ile 100 aralığında olmalıdır!";
                        mmMessage.Messages.Add(msg);
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_YuzdeOran" });
                    }
                    else
                    {
                        if (qMulakatDetay.Sum(s => s.YuzdeOran) + YuzdeOran > 100)
                        {
                            //if (qMulakatDetay.Count == 1)
                            //{
                            //    var mul = qMulakatDetay.First();
                            //    degisenSinavTurID = mul.MulakatSinavTurID;
                            //    degisenYuzdeOran = 100 - YuzdeOran;
                            //}
                            //else
                            //{
                            string msg = "Eklenen sınav tiplerinin toplam % oranı %100 ü geçmemesi gerekmektedir!";
                            mmMessage.Messages.Add(msg);
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_YuzdeOran" });
                            // } 
                        }
                        else if (qMulakatDetay.Sum(s => s.YuzdeOran) + YuzdeOran < 100)
                        {
                            if (qMulakatDetay.Count == 1)
                            {
                                var mul = qMulakatDetay.First();
                                degisenSinavTurID = mul.MulakatSinavTurID;
                                degisenYuzdeOran = 100 - YuzdeOran;
                            }

                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_YuzdeOran" });

                    }
                    if (data.SinavTarihi == DateTime.MinValue)
                    {
                        string msg = "Sınav türünü ekleyebilmek için sınav tarihi giriniz!";
                        mmMessage.Messages.Add(msg);
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_SinavTarihi" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_SinavTarihi" });
                    if (data.SinavSaati.IsNullOrWhiteSpace())
                    {
                        string msg = "Sınav türünü ekleyebilmek için sınav saatini giriniz!";
                        mmMessage.Messages.Add(msg);
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_SinavSaati" });
                    }
                    else
                    {
                        try
                        {
                            var saat = Convert.ToDateTime(DateTime.Now.ToString("dd-MM-yyyy") + " " + data.SinavSaati);
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_SinavSaati" });
                        }
                        catch (Exception)
                        {
                            string msg = "Girmiş olduğunuz saat formatı uygun değildir! Örnek format (saat:dakika)= " + DateTime.Now.ToString("HH:mm");
                            mmMessage.Messages.Add(msg);
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_SinavSaati" });
                        }
                    }

                    if (data.KampusID <= 0)
                    {
                        string msg = "Sınav türünü ekleyebilmek kampüs seçiniz!";
                        mmMessage.Messages.Add(msg);
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_KampusID" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_KampusID", Message = "" });
                    if (data.YerAdi.IsNullOrWhiteSpace())
                    {
                        string msg = "Sınav yeri bilgisini giriniz!";
                        mmMessage.Messages.Add(msg);
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "_YerAdi" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_YerAdi", Message = "" });

                }
                else
                {
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_YuzdeOran" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_SinavTarihi" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_KampusID" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_YerAdi" });
                }
            }
            var strView = "";
            if (mmMessage.Messages.Count == 0 && bsurec.isYuzdeGirisBolum)
            {
                var stip = bsurec.BasvuruSurecMulakatSinavTurleris.Where(p => p.MulakatSinavTurID == data.MulakatSinavTurID).FirstOrDefault();
                if (stip != null && stip.YuzdeOran.HasValue)
                {
                    if (stip.YuzdeOran.Value > data.YuzdeOran)
                    {
                        string msg = "Tanımlamaya çalıştığınız sınav bilgisinin minimum % oranı %" + stip.YuzdeOran + " olarak belirlenmiştir. Daha düşük bir oran girilemez!";
                        mmMessage.Messages.Add(msg);
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_YuzdeOran" });
                    }
                }
            }
            if (mmMessage.Messages.Count > 0)
            {
                mmMessage.IsSuccess = mmMessage.Messages.Count == 0;
                mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
                strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            }
            else
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_MulakatSinavTurID" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_YuzdeOran" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_SinavTarihi" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_KampusID" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "_YerAdi" });
            }

            return Json(new
            {
                IsSuccess = mmMessage.Messages.Count == 0 ? true : false,
                isYuzdeGirisBolum = bsurec.isYuzdeGirisBolum,
                YuzdeOran = YuzdeOran,
                Messages = strView,
                mmMessage = mmMessage,
                degisenSinavTurID = degisenSinavTurID,
                degisenYuzdeOran = degisenYuzdeOran
            }, "application/json", JsonRequestBehavior.AllowGet);

        }
        public ActionResult getMulakatDetay(int BasvuruSurecID, int OgrenimTipKod, string ProgramKod, int tbInx, bool IsDelete, bool IsBootBox = false, int? MulakatID = null)
        {
            var model = new BasvuruMulakatDetayDto();
           





            var mulakat = new Mulakat();

            var mAny = db.Mulakats.Where(p => p.BasvuruSurecID == BasvuruSurecID && p.OgrenimTipKod == OgrenimTipKod && p.ProgramKod == ProgramKod).Any();
            if (MulakatID.HasValue || mAny)
            {
                if (MulakatID.HasValue) mulakat = db.Mulakats.Where(p => p.MulakatID == MulakatID.Value).FirstOrDefault();
                else mulakat = db.Mulakats.Where(p => p.BasvuruSurecID == BasvuruSurecID && p.OgrenimTipKod == OgrenimTipKod && p.ProgramKod == ProgramKod).FirstOrDefault();
                BasvuruSurecID = mulakat.BasvuruSurecID;
                ProgramKod = mulakat.ProgramKod;
                OgrenimTipKod = mulakat.OgrenimTipKod;
                MulakatID = mulakat.MulakatID;

            }
            var BasvuruSureci = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();


            var kota = BasvuruSureci.BasvuruSurecKotalars.Where(p => p.ProgramKod == ProgramKod && p.OgrenimTipKod == OgrenimTipKod).FirstOrDefault();
            model.OrtakKota = kota.OrtakKota;
            model.OrtakKotaSayisi = kota.OrtakKotaSayisi;
            model.AlanIciKota = kota.AlanIciKota;
            model.AlanDisiKota = kota.AlanDisiKota;
            model.IsAlesYerineDosyaNotuIstensin = kota.IsAlesYerineDosyaNotuIstensin == true;
            var bsOT = BasvuruSureci.BasvuruSurecOgrenimTipleris.Where(p => p.OgrenimTipKod == OgrenimTipKod).First();
            var PrL = db.Programlars.Where(p =>  p.ProgramKod == ProgramKod).First();
            var _EnstituKod = BasvuruSureci.EnstituKod;
            model.MulakatID = MulakatID.HasValue ? MulakatID.Value : 0;
            model.BasvuruSurecID = BasvuruSurecID;
            model.ProgramKod = ProgramKod;
            model.OgrenimTipKod = OgrenimTipKod;
            model.ProgramAdi = PrL.ProgramAdi;
            model.AnabilimDaliAdi = PrL.AnabilimDallari.AnabilimDaliAdi;
            model.OgrenimTipAdi = db.OgrenimTipleris.Where(p => p.EnstituKod == BasvuruSureci.EnstituKod  && p.OgrenimTipKod == OgrenimTipKod).First().OgrenimTipAdi;
            model.BasvuruSurecAdi = BasvuruSureci.BaslangicYil + " / " + BasvuruSureci.BitisYil + " " + BasvuruSureci.Donemler.DonemAdi;
            model.BaslangicTarihi = BasvuruSureci.BaslangicTarihi;
            model.BitisTarihi = BasvuruSureci.BitisTarihi;
            model.SonucGirisBaslangicTarihi = BasvuruSureci.SonucGirisBaslangicTarihi;
            model.SonucGirisBitisTarihi = BasvuruSureci.SonucGirisBitisTarihi;
            model.AGNOGirisBaslangicTarihi = BasvuruSureci.AGNOGirisBaslangicTarihi;
            model.AGNOGirisBitisTarihi = BasvuruSureci.AGNOGirisBitisTarihi;
            model.BaslangicYil = BasvuruSureci.BaslangicYil;
            model.BitisYil = BasvuruSureci.BitisYil;
            model.DonemAdi = BasvuruSureci.Donemler.DonemAdi;
            model.MulakatSurecineGirecek = kota.MulakatSurecineGirecek ?? bsOT.MulakatSurecineGirecek;
            model.AlanIciBilimselHazirlik = bsOT.AlanIciBilimselHazirlik;
            model.AlanDisiBilimselHazirlik = bsOT.AlanDisiBilimselHazirlik;
            if (MulakatID > 0)
            {
                model.MulakatDetay = (from s in db.MulakatDetays.Where(p => p.MulakatID == MulakatID)
                                      join k in db.Kampuslers on s.KampusID equals k.KampusID
                                      join st in db.MulakatSinavTurleris on s.MulakatSinavTurID equals st.MulakatSinavTurID
                                      select new krMulakatDetay
                                      {
                                          MulakatID = s.MulakatID,
                                          MulakatSinavTurID = s.MulakatSinavTurID,
                                          MulakatSinavTurAdi = st.MulakatSinavTurAdi,
                                          YuzdeOran = s.YuzdeOran,
                                          SinavTarihi = s.SinavTarihi,
                                          KampusAdi = k.KampusAdi,
                                          KampusID = s.KampusID,
                                          YerAdi = s.YerAdi

                                      }).ToList();
                model.MulakatJuris = db.MulakatJuris.Where(p => p.MulakatID == MulakatID).OrderByDescending(o => o.IsAsil).ThenBy(t => t.SiraNo).ToList();
                model.MulakatSinavNotGirisiAktif = Management.getAktifMulakatSonucGiris(_EnstituKod, model.BasvuruSurecID).HasValue;
            }
            model.MulakatSonuc = Management.getMulakatSonucHesapList(BasvuruSurecID, ProgramKod, OgrenimTipKod);
            if (model.MulakatSonuc.Count > 0)
            {
                var hesaplananCount = model.MulakatSonuc.Where(p => p.MulakatSonucTipID != MulakatSonucTipi.Hesaplanmadı).Count();
                model.SonucHesaplandi = model.MulakatSonuc.Count == hesaplananCount;
            }


            model.YaziliNotuIstensin = model.MulakatSurecineGirecek && mulakat.MulakatDetays.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Yazili);
            model.SozluNotuIstensin = model.MulakatSurecineGirecek && mulakat.MulakatDetays.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Sozlu);
            model.SelectedTabIndex = tbInx;
            ViewBag.IsBootBox = IsBootBox;
            ViewBag.IsDelete = IsDelete;
           
            return View(model);
        }

        [Authorize(Roles = RoleNames.YGBasvuruSureciKayit)]
        public ActionResult AlanDegistirmeIslemi(string UniqueID, bool IsAlanIciOrDisi)
        {
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = false;
            mmMessage.Title = "Tercih alan değiştirme işlemi";
            var gUniqueID = new Guid(UniqueID);
            var Tercih = db.BasvurularTercihleris.Where(p => p.UniqueID == gUniqueID).First();

            if (Tercih.MulakatSonuclaris.Any(a => a.MulakatSonucTipID != MulakatSonucTipi.Hesaplanmadı))
            {

                mmMessage.Messages.Add("Bu tercih için hesaplama yapıldığından alan değiştirme işlemi uygulanamaz");
            }
            else
            {
                var Bsurec = Tercih.Basvurular.BasvuruSurec;
                var Kota = Bsurec.BasvuruSurecKotalars.Where(p => p.OgrenimTipKod == Tercih.OgrenimTipKod && p.ProgramKod == Tercih.ProgramKod).First();
                if (Kota.OrtakKota && IsAlanIciOrDisi)
                {
                    mmMessage.Messages.Add("Ortak kota kullanan programlarda tercih alan içine aktarılamaz.");
                }
                else if (Kota.AlanIciKota > 0 && Kota.AlanDisiKota == 0 && !IsAlanIciOrDisi)
                {
                    mmMessage.Messages.Add("Sadece alan içi kotası bulunan programlarda tercih alan dışına aktarılamaz.");
                }
                else if (Kota.AlanIciKota == 0 && Kota.AlanDisiKota > 0 && IsAlanIciOrDisi)
                {
                    mmMessage.Messages.Add("Sadece alan dışı kotası bulunan programlarda tercih alan içine aktarılamaz.");
                }
               
                var jsonStr = JsonConvert.SerializeObject(new { Tercih.BasvuruTercihID, Tercih.BasvuruID, Tercih.SiraNo, Tercih.OgrenimTipKod, Tercih.ProgramKod, Tercih.AlanTipID }, Formatting.Indented);

                if (mmMessage.Messages.Count == 0)
                {
                    Tercih.AlanTipID = IsAlanIciOrDisi ? AlanTipi.AlanIci : AlanTipi.AlanDisi;

                    if (Tercih.MulakatSonuclaris.Any())
                    {
                        foreach (var item in Tercih.MulakatSonuclaris)
                        {
                            item.AlanTipID = Tercih.AlanTipID;
                            item.IslemTarihi = DateTime.Now;
                            item.IslemYapanIP = UserIdentity.Ip;
                            item.IslemYapanID = UserIdentity.Current.Id;
                        }
                    }
                    db.SaveChanges();
                    var Msg = Tercih.Basvurular.Ad + " " + Tercih.Basvurular.Soyad + " isimli öğrenci tercih alan tipi  " + (IsAlanIciOrDisi ? "'Alan İçi'" : "'Alan Dışı'") + " olarak güncellendi.";
                    mmMessage.Messages.Add(Msg);
                    var UpdatedjsonStr = JsonConvert.SerializeObject(new { Tercih.BasvuruTercihID, Tercih.BasvuruID, Tercih.SiraNo, Tercih.OgrenimTipKod, Tercih.ProgramKod, Tercih.AlanTipID }, Formatting.Indented);

                     Msg += "\r\n\r\n Değişim Öncesi Json: \r\n" + jsonStr + "\r\n\r\nDeğişim Sonrası Json:\r\n" + UpdatedjsonStr;
                    Management.SistemBilgisiKaydet("Alan tipi değiştirme işlemi \r\n" + Msg, "MulakatSureci/AlanDegistirmeIslemi", LogType.Uyarı);

                }
            }

            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Information;
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }


        [Authorize(Roles = RoleNames.YGMulakatKayıt)]
        public ActionResult GirisSinavNotuKaydet(krMulakatSonucPostModel mdl, string EKD)
        {
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = true;

            var colorS = new Dictionary<int, string>();
            try
            {
                var BasvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == mdl.BasvuruSurecID).First();
                var bsSinavBilgi = BasvuruSurec.BasvuruSurecSinavTipleris.ToList();
                var mlktsB = new List<krMulakatSonuc>();
                var YaziliSinaviIstensin = false;
                var SozluSinaviIstensin = false;
                if (mdl.MulakatSurecineGirecek)
                {
                    var mulakat = db.Mulakats.Where(p => p.MulakatID == mdl.MulakatID).First();
                    var mulSinavTurs = mulakat.MulakatDetays.ToList();
                    YaziliSinaviIstensin = mulSinavTurs.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Yazili);
                    SozluSinaviIstensin = mulSinavTurs.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Sozlu);
                    mlktsB = Management.getMulakatSonucList(mdl.MulakatID).Where(p => p.AlanTipID == mdl.AlanTipID).ToList();

                }
                else
                {
                    mlktsB = Management.getMulakatSonucListMulakatsiz(mdl.BasvuruSurecID, mdl.ProgramKod, mdl.OgrenimTipKod, mdl.BasvuruTercihID).Where(p => p.AlanTipID == mdl.AlanTipID).ToList();
                }


                var btercihleri = db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruSurecID == mdl.BasvuruSurecID && p.OgrenimTipKod == mdl.OgrenimTipKod && p.ProgramKod == mdl.ProgramKod && p.AlanTipID == mdl.AlanTipID).ToList();
                var btercihIds = btercihleri.Select(s => s.BasvuruTercihID).ToList();

                var IsAlesYerineDosyaNotuIstensin = mdl.IsAlesYerineDosyaNotuIstensin;

                var qBasvuruTercihID = mdl.BasvuruTercihID.Select((s, inx) => new { BasvuruTercihID = s, inx }).ToList();
                var qDosyaNotuNotu = mdl.AlesNotuOrDosyaNotu.Select((s, inx) => new { AlesNotuOrDosyaNotu = s, inx }).ToList();
                var qYaziliNotu = mdl.YaziliNotu.Select((s, inx) => new { YaziliNotu = s, inx }).ToList();
                var qSozluNotu = mdl.SozluNotu.Select((s, inx) => new { SozluNotu = s, inx }).ToList();
                var qSinavaGirmediY = mdl.SinavaGirmediY.Select((s, inx) => new { SinavaGirmediY = s, inx }).ToList();
                var qSinavaGirmediS = mdl.SinavaGirmediS.Select((s, inx) => new { SinavaGirmediS = s, inx }).ToList();
                var qBhazirlik = mdl.BilimselHazirlikVar.Select((s, inx) => new { Bhazirlik = s, inx }).ToList();
                var qdata = (from tercih in qBasvuruTercihID
                             join sNotY in qYaziliNotu on tercih.inx equals sNotY.inx
                             join sNotS in qSozluNotu on tercih.inx equals sNotS.inx
                             join sGirmediY in qSinavaGirmediY on tercih.inx equals sGirmediY.inx
                             join sGirmediS in qSinavaGirmediS on tercih.inx equals sGirmediS.inx
                             join bHaz in qBhazirlik on tercih.inx equals bHaz.inx
                             join mlkB in mlktsB on tercih.BasvuruTercihID equals mlkB.BasvuruTercihID
                             join dNot in qDosyaNotuNotu on tercih.inx equals dNot.inx into defdNot
                             from DNot in defdNot.DefaultIfEmpty()

                             select new
                             {
                                 Index = tercih.inx,
                                 BasvuruTercihID = tercih.BasvuruTercihID,
                                 AdSoyad = mlkB.AdSoyad,
                                 YaziliNotu = sNotY.YaziliNotu,
                                 SozluNotu = sNotS.SozluNotu,
                                 SinavaGirmediY = (!sNotY.YaziliNotu.HasValue && (!sGirmediY.SinavaGirmediY.HasValue || sGirmediY.SinavaGirmediY.Value == false) ? null : sGirmediY.SinavaGirmediY),
                                 SinavaGirmediS = (!sNotS.SozluNotu.HasValue && (!sGirmediS.SinavaGirmediS.HasValue || sGirmediS.SinavaGirmediS.Value == false) ? null : sGirmediS.SinavaGirmediS),
                                 BilimselHazirlikVar = bHaz.Bhazirlik ?? false,
                                 AlesNotuOrDosyaNotu = DNot == null ? null : DNot.AlesNotuOrDosyaNotu
                             }).ToList();

                if (mdl.MulakatSurecineGirecek)
                {
                    var surecAktif = Management.getAktifMulakatSonucGiris(BasvuruSurec.EnstituKod, BasvuruSurec.BasvuruSurecID).HasValue;
                    if (surecAktif || UserIdentity.Current.IsAdmin)
                    {
                        if (mdl.BasvuruTercihID.Count != btercihleri.Where(a => mdl.BasvuruTercihID.Contains(a.BasvuruTercihID)).Count())
                        {
                            mmMessage.IsSuccess = false;
                            mmMessage.Messages.Add("Bu mülakata ait olmayan başvuruları kayıt edemezsiniz!");
                            Management.SistemBilgisiKaydet("Bu mülakata ait olmayan başvurular kayıt edemezsiniz!\r\nMulakatID:" + mdl.MulakatID +
                                                                                                                       "\r\nGelen Başvuru Tercihleri:" + string.Join(",", mdl.BasvuruTercihID) +
                                                                                                                       "\r\nAsıl Başvuru Tercihleri:" + string.Join(",", btercihIds), "MulakatSureci/GirisSinavNotuKaydet", LogType.Saldırı);
                        }
                    }
                    else
                    {
                        mmMessage.IsSuccess = false;
                        mmMessage.Messages.Add("Mülakata sınav not girişi süreci kapalıdır! Kayıt işlemi yapılamaz.");
                        Management.SistemBilgisiKaydet("Mülakata sınav not girişi süreci kapalı iken not sonuçları  girilmek isteniyor!\r\nMulakatID:" + mdl.MulakatID, "MulakatSureci/GirisSinavNotuKaydet", LogType.Saldırı);
                    }
                }
                else
                {
                    if (btercihleri.Any(a => a.MulakatSonuclaris.Any(a2 => a2.MulakatSonucTipID != MulakatSonucTipi.Hesaplanmadı)))
                    {
                        mmMessage.Messages.Add("Hesaplanan başvurular bulunmakta. Not giriş işlemi yapılamaz.");
                    }
                }
                if (mmMessage.IsSuccess)
                {
                    if (YaziliSinaviIstensin)
                    {
                        var notAralikDisiY = qdata.Where(p => p.YaziliNotu.HasValue && (p.YaziliNotu.Value < 0 || p.YaziliNotu.Value > 100)).ToList();
                        if (notAralikDisiY.Count > 0)
                        {
                            mmMessage.IsSuccess = false;
                            foreach (var item in notAralikDisiY)
                            {
                                mmMessage.Messages.Add(item.AdSoyad + " öğrencisine ait yazılı sınav notu 0 ile 100 aralığında olması gerekmekte! Kayıt işlemi yapılamaz.");
                            }
                        }
                    }
                    if (SozluSinaviIstensin)
                    {
                        var notAralikDisiS = qdata.Where(p => p.SozluNotu.HasValue && (p.SozluNotu.Value < 0 || p.SozluNotu.Value > 100)).ToList();
                        if (notAralikDisiS.Count > 0)
                        {
                            mmMessage.IsSuccess = false;
                            foreach (var item in notAralikDisiS)
                            {
                                mmMessage.Messages.Add(item.AdSoyad + " öğrencisine ait sözlü sınav notu 0 ile 100 aralığında olması gerekmekte! Kayıt işlemi yapılamaz.");
                            }
                        }
                    }
                    if (IsAlesYerineDosyaNotuIstensin)
                    {
                        var notAralikDNot = qdata.Where(p => p.AlesNotuOrDosyaNotu.HasValue && (p.AlesNotuOrDosyaNotu.Value < 0 || p.AlesNotuOrDosyaNotu.Value > 100)).ToList();
                        if (notAralikDNot.Count > 0)
                        {
                            mmMessage.IsSuccess = false;
                            foreach (var item in notAralikDNot)
                            {
                                mmMessage.Messages.Add(item.AdSoyad + " öğrencisine ait dosya notu 0 ile 100 aralığında olması gerekmekte! Kayıt işlemi yapılamaz.");
                            }
                        }
                    }
                }
                if (mmMessage.IsSuccess)
                {

                    if (mdl.MulakatSurecineGirecek)
                    {
                        var oldData = db.MulakatSonuclaris.Where(p => p.MulakatID == mdl.MulakatID && p.AlanTipID == mdl.AlanTipID).ToList();
                        db.MulakatSonuclaris.RemoveRange(oldData);
                    }
                    else
                    {
                        var mSonuclari = btercihleri.SelectMany(s => s.MulakatSonuclaris).ToList();
                        db.MulakatSonuclaris.RemoveRange(mSonuclari);
                    }

                    foreach (var item in mlktsB)
                    {
                        var mlktSonucItem = new MulakatSonuclari();
                        var newB = qdata.First(p => p.BasvuruTercihID == item.BasvuruTercihID);
                        var btercih = btercihleri.First(p => p.BasvuruTercihID == item.BasvuruTercihID);
                        mlktSonucItem.BasvuruSurecID = mdl.BasvuruSurecID;
                        mlktSonucItem.MulakatSonucTipID = item.MulakatSonucTipID;
                        mlktSonucItem.MulakatID = item.MulakatID;
                        mlktSonucItem.BasvuruID = item.BasvuruID;
                        mlktSonucItem.BasvuruTercihID = item.BasvuruTercihID;
                        mlktSonucItem.AlanTipID = item.AlanTipID;
                        mlktSonucItem.SiraNo = item.SiraNo;
                        mlktSonucItem.BilimselHazirlikVar = newB.BilimselHazirlikVar;
                        mlktSonucItem.IslemYapanIP = UserIdentity.Ip;
                        mlktSonucItem.IslemYapanID = UserIdentity.Current.Id;
                        mlktSonucItem.IslemTarihi = DateTime.Now;
                        if (!item.IsAlesYerineDosyaNotuIstensin)
                        {
                            var sinavBilgi = btercih.Basvurular.BasvurularSinavBilgis.FirstOrDefault(p => p.SinavTipGrupID == SinavTipGrup.Ales_Gree);
                            if (sinavBilgi != null)
                            {
                                var _snvBilgi = bsSinavBilgi.Where(p => p.SinavTipID == sinavBilgi.SinavTipID).First();
                                if (_snvBilgi.WebService)
                                {
                                    var wsxmlNot = sinavBilgi.WsXmlData.toSinavSonucAlesXmlModel();
                                    if (btercih.Programlar.AlesNotuYuksekOlanAlinsin)
                                    {
                                        var maxNot = new Dictionary<int, double>();
                                        if (btercih.Programlar.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.Sayısal)) maxNot.Add(AlesTipBilgi.Sayısal, wsxmlNot.SAY_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                        if (btercih.Programlar.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.Sözel)) maxNot.Add(AlesTipBilgi.Sözel, wsxmlNot.SOZ_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                        if (btercih.Programlar.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.EşitAğırlık)) maxNot.Add(AlesTipBilgi.EşitAğırlık, wsxmlNot.EA_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                        mlktSonucItem.AlesNotuOrDosyaNotu = maxNot.Select(s => s.Value).Max();
                                    }
                                    else
                                    {
                                        if (btercih.Programlar.AlesTipID == AlesTipBilgi.Sayısal)
                                            mlktSonucItem.AlesNotuOrDosyaNotu = wsxmlNot.SAY_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                                        else if (btercih.Programlar.AlesTipID == AlesTipBilgi.Sözel)
                                            mlktSonucItem.AlesNotuOrDosyaNotu = wsxmlNot.SOZ_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                                        else if (btercih.Programlar.AlesTipID == AlesTipBilgi.EşitAğırlık)
                                            mlktSonucItem.AlesNotuOrDosyaNotu = wsxmlNot.EA_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                                    }
                                }
                                else
                                    mlktSonucItem.AlesNotuOrDosyaNotu = sinavBilgi.SinavNotu;
                            }
                            else mlktSonucItem.AlesNotuOrDosyaNotu = null;
                        }
                        else mlktSonucItem.AlesNotuOrDosyaNotu = newB.AlesNotuOrDosyaNotu;
                        mlktSonucItem.Agno = item.Agno;
                        mlktSonucItem.SinavaGirmediY = YaziliSinaviIstensin ? newB.SinavaGirmediY : null;
                        mlktSonucItem.YaziliNotu = YaziliSinaviIstensin ? newB.YaziliNotu : null;
                        mlktSonucItem.SinavaGirmediS = SozluSinaviIstensin ? newB.SinavaGirmediS : null;
                        mlktSonucItem.SozluNotu = SozluSinaviIstensin ? newB.SozluNotu : null;
                        mlktSonucItem.GenelBasariNotu = item.GenelBasariNotu;
                        db.MulakatSonuclaris.Add(mlktSonucItem);

                        bool successRow = true;
                        if (YaziliSinaviIstensin)
                        {
                            if (mlktSonucItem.SinavaGirmediY.HasValue == false && mlktSonucItem.YaziliNotu.HasValue == false) successRow = false;
                        }
                        if (SozluSinaviIstensin)
                        {
                            if (mlktSonucItem.SinavaGirmediS.HasValue == false && mlktSonucItem.SozluNotu.HasValue == false) successRow = false;
                        }
                        if (IsAlesYerineDosyaNotuIstensin)
                        {
                            if (mlktSonucItem.AlesNotuOrDosyaNotu.HasValue == false) successRow = false;
                        }
                        colorS.Add(mlktSonucItem.BasvuruTercihID, successRow ? "green" : "red");
                    }
                    db.SaveChanges();
                    mmMessage.Messages.Add((mdl.MulakatSurecineGirecek ? "Mükakat" : "Dosya") + " notu bilgileri kaydedildi.");

                }
            }
            catch (Exception ex)
            {
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add((mdl.MulakatSurecineGirecek ? "Mükakat" : "Dosya") + " not bilgileri kaydedilirken bir hata oluştu! Hata:" + ex.ToExceptionMessage());
                Management.SistemBilgisiKaydet((mdl.MulakatSurecineGirecek ? "Mükakat" : "Dosya") + " not bilgileri kaydedilirken bir hata oluştu!\r\n Hata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogType.Kritik);
            }
            if (mmMessage.IsSuccess)
            {

                mmMessage.Title = "Not kayıt işlemi";
                mmMessage.MessageType = Msgtype.Success;
            }
            else
            {
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.Title = "Hata";

            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView, ColorSet = Json(colorS.Select(s => new { s.Key, s.Value })) }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = RoleNames.YGMulakatSil)]
        public ActionResult Sil(int id)
        {

            
            var mmMessage = new MmMessage();
            var mlkt = db.Mulakats.Where(p => p.MulakatID == id).First();
            // var surecAktif = Management.getAktifMulakatSonucGiris(mlkt.BasvuruSurec.EnstituKod, mlkt.BasvuruSurecID).HasValue;
            var surecAktifJuriGiris = Management.getAktifMulakatSurecID(mlkt.BasvuruSurec.EnstituKod, BasvuruSurecTipi.YatayGecisBasvuru, mlkt.BasvuruSurecID).HasValue;

            var uPrKods = Management.GetUserProgramKods(UserIdentity.Current.Id, mlkt.BasvuruSurec.EnstituKod);
            if (uPrKods.Contains(mlkt.ProgramKod))
            {
                if (surecAktifJuriGiris || UserIdentity.Current.IsAdmin)
                {
                    var kayit = db.Mulakats.Where(p => p.MulakatID == id).FirstOrDefault();
                    var programAdi = kayit.Programlar.ProgramAdi;
                    var osAdi = db.OgrenimTipleris.Where(p => p.EnstituKod == mlkt.BasvuruSurec.EnstituKod && p.OgrenimTipKod == mlkt.OgrenimTipKod ).First().OgrenimTipAdi;
                    var tarih = kayit.IslemTarihi.ToString();
                    string msg = tarih + " tarihli " + programAdi + " programının " + osAdi + " öğrenim seviyesi bilgisi";
                    try
                    {
                        mmMessage.Title ="Uyarı";
                        db.Mulakats.Remove(kayit);
                        db.SaveChanges();
                        mmMessage.Messages.Add(msg + " sistemden silindi");
                        mmMessage.MessageType = Msgtype.Success;
                        mmMessage.IsSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        mmMessage.MessageType = Msgtype.Error;
                        mmMessage.IsSuccess = false;
                        msg += " sistemden silinemedi! <br>Hata:" + ex.ToExceptionMessage();
                        mmMessage.Messages.Add(msg);
                        mmMessage.Title = "Hata";
                        Management.SistemBilgisiKaydet(msg, "MulakatSureci/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                    }
                }
                else
                {
                    mmMessage.Title ="Uyarı";
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.Messages.Add("Jüri/Yer bilgi süreci geçen mülakat bilgileri silinemez!");
                    mmMessage.IsSuccess = false;

                }
            }
            else
            {
                mmMessage.IsSuccess = false;
                mmMessage.Title ="Uyarı";
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.Messages.Add("Bu mülakat bilgisini silmeye yetkili değilsiniz!");
                Management.SistemBilgisiKaydet("Mulakat bilgisi yetkisiz silme işlemi!\r\nMulakatID:" + id + "\r\nSilinmek İstenen Program Kodu:" + mlkt.ProgramKod + "\r\nProgram Yetkileri:" + string.Join(", ", uPrKods), "MulakatSureci/Sil", LogType.OnemsizHata);
            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);

            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }


    }
}