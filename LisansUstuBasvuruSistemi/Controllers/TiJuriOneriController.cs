using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize]
    public class TiJuriOneriController : Controller
    {
        // GET: TikOneri
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            return Index(new FmTikBasvuru() { PageSize = 10 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmTikBasvuru model, string ekd)
        {
            model.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            model.KullaniciID = model.KullaniciID ?? UserIdentity.Current.Id;


            var q = from s in _entities.TijBasvurus
                    join k in _entities.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    select new FrTijBasvuru
                    {
                        TijBasvuruID = s.TijBasvuruID,
                        UniqueID = s.UniqueID,
                        EnstituKod = s.EnstituKod,
                        BasvuruSonDonemSecilecekDersKodlari = s.BasvuruSonDonemSecilecekDersKodlari,
                        BasvuruTarihi = s.BasvuruTarihi,
                        KullaniciID = s.KullaniciID,
                        AdSoyad = k.Ad + " " + k.Soyad,
                        OgrenciNo = s.OgrenciNo,
                        ResimAdi = k.ResimAdi

                    };
            if (model.KullaniciID.HasValue) q = q.Where(p => p.KullaniciID == model.KullaniciID);
            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.IndexModel = indexModel;
            return View(model);

        }


        public ActionResult TikOneriFormu()
        {

            return View();
        }

        public ActionResult TikOneriFormuPost()
        {
            throw new NotImplementedException();
        }

        public ActionResult GetTikOneriFormu(int? kullaniciId, int? tikBasvuruOneriID, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            var cmbUnvanList = UnvanlarBus.GetCmbJuriUnvanlar(true);
            var cmbUniversiteList = Management.cmbGetAktifUniversiteler(true);
            kullaniciId = kullaniciId ?? UserIdentity.Current.Id;
            var kul = _entities.Kullanicilars.First(f => f.KullaniciID == kullaniciId);
            var ogrenciInfo = KullanicilarBus.OgrenciKontrol(kul.TcKimlikNo);
            kul = _entities.Kullanicilars.First(f => f.KullaniciID == kullaniciId);

            var tikb = _entities.TijBasvuruOneris.FirstOrDefault(p => p.TijBasvuruOneriID == tikBasvuruOneriID);
            //if (!tikBasvuruOneriID.HasValue)
            //{
            //    var kulTikBasvurusuVarMi =
            //        _entities.TikBasvurus.Any(a => a.KullaniciID == kullaniciId && a.OgrenciNo == kul.OgrenciNo);
            //    if (!kulTikBasvurusuVarMi)
            //    {
            //        var ot = _entities.OgrenimTipleris.First(f =>
            //            f.EnstituKod == enstituKod && f.OgrenimTipKod == kul.OgrenimTipKod);
            //        _entities.TikBasvurus.Add(new TikBasvuru
            //        {
            //            UniqueID = Guid.NewGuid(),
            //            EnstituKod = enstituKod,
            //            BasvuruSonDonemSecilecekDersKodlari = string.Join(",", ogrenciInfo.AktifDonemDers.DersKodNums),
            //            BasvuruTarihi = DateTime.Now,
            //            KullaniciID = kullaniciId.Value,
            //            OgrenciNo = ogrenciInfo.OgrenciInfo.OGR_NO,
            //            OgrenimTipID = ot.OgrenimTipID,
            //            ProgramKod = kul.ProgramKod,
            //            KayitOgretimYiliBaslangic = kul.KayitYilBaslangic,
            //            KayitOgretimYiliDonemID = kul.KayitDonemID,
            //            KayitTarihi = kul.KayitTarihi,
            //            TezDanismanID = kul.DanismanID.Value,
            //            YeterlikSozluSinavTarihi = DateTime.Now
            //        });
            //    }
            //}



            var model = new TijOneriFormuKayitDto
            {
                TijBasvuruID = tikb?.TijBasvuruID ?? 0,
                IsTezDiliTr = ogrenciInfo.IsTezDiliTr == true,
                TezBaslikTr = ogrenciInfo.OgrenciTez.TEZ_BASLIK,
                TezBaslikEn = ogrenciInfo.OgrenciTez.TEZ_BASLIK_ENG,
                SListUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption"),
                SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption")
            };

            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Success,
                IsSuccess = true
            };
            string view = "";

            if (!RoleNames.MezuniyetGelenBasvurularJuriOneriFormuKayit.InRoleCurrent())
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Jüri öneri formu kayıt işlemi için yetkili değilsiniz.");
            }
            else if (!RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() && tikb.TijBasvuru.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Bu mezuniyet başvurusu için danışman olarak belirlenmediğiniz için jüri öneri formu oluşturamazsınız.");
            }
            if (mMessage.Messages.Count == 0)
            {
                if (ogrenciInfo.Hata)
                {
                    mMessage.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu!");
                }
                else
                {
                    if (ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.IsNullOrWhiteSpace())
                        mMessage.Messages.Add("Danışman Bilgisi Çekilemedi.");

                    if (mMessage.Messages.Count > 0)
                    {
                        mMessage.MessageType = Msgtype.Warning;
                        mMessage.Messages.Add("Jüri öneri formunu oluşturabilmeniz için bu durumu enstitü yetkililerine iletiniz.");
                    }
                }

            }
            if (mMessage.Messages.Count == 0)
            {
                model.OgrenciAdSoyad = kul.Ad + " " + kul.Soyad + " - " + kul.OgrenciNo;
                model.OgrenciAnabilimdaliProgramAdi = kul.Programlar.AnabilimDallari.AnabilimDaliAdi + " - " + kul.Programlar.ProgramAdi;


                if (tikb != null)
                {
                    model.TijBasvuruOneriID = tikb.TijBasvuruOneriID;
                    model.JoFormJuriList = tikb.TijBasvuruOneriJurilers.Select(s => new KrTijOneriFormuJurileri
                    {
                        TijBasvuruOneriID = s.TijBasvuruOneriID,
                        TijBasvuruOneriJuriID = s.TijBasvuruOneriJuriID,
                        JuriTipAdi = s.JuriTipAdi,
                        UnvanAdi = s.UnvanAdi,
                        SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", s.UnvanAdi),
                        AdSoyad = s.AdSoyad,
                        EMail = s.EMail,
                        UniversiteID = s.UniversiteID,
                        SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", s.UniversiteID),
                        UniversiteAdi = s.UniversiteAdi,
                        AnabilimDaliAdi = s.AnabilimDaliAdi,
                        IsAsilOrYedek = s.IsAsilOrYedek
                    }).ToList();
                    if (!ogrenciInfo.Hata)
                    {
                        if (!ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.IsNullOrWhiteSpace())
                        {
                            var tD = model.JoFormJuriList.First(p => p.JuriTipAdi == "TezDanismani");
                            tD.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tD.UniversiteID);
                            tD.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tD.UnvanAdi);
                            if (tD.AdSoyad.ToUpper().Trim() != ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper().ToUpper().Trim() || tD.UnvanAdi.ToUpper().Trim() != ogrenciInfo.OgrenciInfo.DANISMAN_UNVAN1.ToJuriUnvanAdi())
                            {
                                tD.AdSoyad = ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper();
                                tD.UnvanAdi = ogrenciInfo.OgrenciInfo.DANISMAN_UNVAN1.ToJuriUnvanAdi();
                            }
                        }
                    }

                }
                else
                {

                    if (!ogrenciInfo.Hata)
                    {
                        if (!ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.IsNullOrWhiteSpace())
                        {
                            var tdBilgi = new KrTijOneriFormuJurileri
                            {
                                JuriTipAdi = "TezDanismani",
                                UnvanAdi = ogrenciInfo.OgrenciInfo.DANISMAN_UNVAN1.ToJuriUnvanAdi(),
                                AdSoyad = ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper(),

                            };
                            tdBilgi.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tdBilgi.UnvanAdi);
                            tdBilgi.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tdBilgi.UniversiteID);
                            model.JoFormJuriList.Add(tdBilgi);
                        }
                        else
                        {
                            model.JoFormJuriList.Add(new KrTijOneriFormuJurileri { JuriTipAdi = "TezDanismani", SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption"), SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption") });
                        }
                    }
                }


                view = ViewRenderHelper.RenderPartialView("TikOneri", "GetTikOneriFormu", model);
            }
            else { mMessage.IsSuccess = false; mMessage.MessageType = Msgtype.Warning; }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);

            return new
            {
                mMessage.IsSuccess,
                Content = view,
                Messages = strView
            }.ToJsonResult();
        }
    }
}