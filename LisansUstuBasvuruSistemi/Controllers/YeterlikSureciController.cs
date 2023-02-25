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
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.YeterlikSureci)]
    public class YeterlikSureciController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _context = new LisansustuBasvuruSistemiEntities();
        // GET: YeterlikSureci
        public ActionResult Index(string ekd)
        {
            return Index(new FmYeterlikSureciDto
            {
                PageSize = 15
            }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmYeterlikSureciDto model, string ekd)
        {
            model.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var enstituKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in _context.YeterlikSurecis
                    join e in _context.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                    join d in _context.Donemlers on new { s.DonemID } equals new { d.DonemID }
                    join k in _context.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where enstituKods.Contains(e.EnstituKod)
                    select new FrYeterlikSureci
                    {
                        YeterlikSurecID = s.YeterlikSurecID,
                        EnstituKod = s.EnstituKod,
                        EnstituAdi = e.EnstituAd,
                        BaslangicYil = s.BaslangicYil,
                        BitisYil = s.BitisYil,
                        DonemID = s.DonemID,
                        DonemAdi = s.BaslangicYil + "/" + s.BitisYil + " " + d.DonemAdi,
                        BaslangicTarihi = s.BaslangicTarihi,
                        BitisTarihi = s.BitisTarihi,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapan = k.Ad + " " + k.Soyad,
                        IslemYapanIP = s.IslemYapanIP

                    };
            q = q.Where(p => p.EnstituKod == model.EnstituKod);
            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount
            };
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BaslangicTarihi);
            model.FrYeterlikSurecis = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            ViewBag.IndexModel = indexModel;
            return View(model);
        }

        public ActionResult Kayit(int? id, string ekd)
        {
            var model = new KmYeterlikSureciDto
            {
                EnstituKod = EnstituBus.GetSelectedEnstitu(ekd)
            };
            id = id ?? 0;
            if (id > 0)
            {
                var data = _context.YeterlikSurecis.First(p => p.YeterlikSurecID == id);
                model.YeterlikSurecID = data.YeterlikSurecID;
                model.EnstituKod = data.EnstituKod;
                model.BaslangicYil = data.BaslangicYil;
                model.BitisYil = data.BitisYil;
                model.DonemID = data.DonemID;
                model.BaslangicTarihi = data.BaslangicTarihi;
                model.BitisTarihi = data.BitisTarihi;
                model.IsAktif = data.IsAktif;
                model.OgretimYili = data.BaslangicYil + "/" + data.BitisYil + "/" + data.DonemID;
            }
            else
            {
                var ogretimYili = DateTime.Now.ToEgitimOgretimYilBilgi();
                model.OgretimYili = ogretimYili.BaslangicYili + "/" + ogretimYili.BitisYili + "/" + ogretimYili.Donem;
            }

            model.KmYeterlikSureciOgrenimTipKriterleris = YeterlikBus.GetOgrenimTipKriterleri(model.EnstituKod, id > 0 ? id : null);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(), "Value", "Caption", model.OgretimYili);
            ViewBag.MmMessage = new MmMessage();
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(KmYeterlikSureciDto kModel, string ekd)
        {
            var mmMessage = new MmMessage();
            kModel.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            var yeterlikSurecOgrenimTipId = kModel.YeterlikSurecOgrenimTipID.Select((s, inx) => new { Inx = inx, YeterlikSurecOgrenimTipID = s }).ToList();
            var ogrenimTipId = kModel.OgrenimTipID.Select((s, inx) => new { Inx = inx, OgrenimTipID = s }).ToList();
            var ogrenimTipKod = kModel.OgrenimTipKod.Select((s, inx) => new { Inx = inx, OgrenimTipKod = s }).ToList();
            var ysMaxBasvuruDonemNo = kModel.YsMaxBasvuruDonemNo.Select((s, inx) => new { Inx = inx, YsMaxBasvuruDonemNo = s }).ToList();
            var ysBasToplamKrediKriteri = kModel.YsBasToplamKrediKriteri.Select((s, inx) => new { Inx = inx, YsBasToplamKrediKriteri = s }).ToList();
            var ysBasEtikNotKriteri = kModel.YsBasEtikNotKriteri.Select((s, inx) => new { Inx = inx, YsBasEtikNotKriteri = s }).ToList();
            var ysBasSeminerNotKriteri = kModel.YsBasSeminerNotKriteri.Select((s, inx) => new { Inx = inx, YsBasSeminerNotKriteri = s }).ToList();

            var ogrenimTipleri = _context.OgrenimTipleris.Where(p => p.EnstituKod == kModel.EnstituKod).ToList();
            var yeterlikSureciOgrenimTipKriterleri = (from kr in yeterlikSurecOgrenimTipId
                                                      join ot in ogrenimTipId on kr.Inx equals ot.Inx
                                                      join otk in ogrenimTipKod on kr.Inx equals otk.Inx
                                                      join dn in ysMaxBasvuruDonemNo on kr.Inx equals dn.Inx
                                                      join dk in ysBasToplamKrediKriteri on kr.Inx equals dk.Inx
                                                      join kk in ysBasEtikNotKriteri on kr.Inx equals kk.Inx
                                                      join agk in ysBasSeminerNotKriteri on kr.Inx equals agk.Inx
                                                      join otl in ogrenimTipleri on ot.OgrenimTipID equals otl.OgrenimTipID
                                                      select new
                                                      {
                                                          kr.Inx,
                                                          kr.YeterlikSurecOgrenimTipID,
                                                          ot.OgrenimTipID,
                                                          otk.OgrenimTipKod,
                                                          dn.YsMaxBasvuruDonemNo,
                                                          dk.YsBasToplamKrediKriteri,
                                                          kk.YsBasEtikNotKriteri,
                                                          agk.YsBasSeminerNotKriteri,
                                                          otl.OgrenimTipAdi,
                                                      }).ToList();
            kModel.KmYeterlikSureciOgrenimTipKriterleris = yeterlikSureciOgrenimTipKriterleri.Select(s => new KmYeterlikSureciOgrenimTipKriterleri
            {
                YeterlikSurecOgrenimTipID = s.YeterlikSurecOgrenimTipID,
                OgrenimTipAdi = s.OgrenimTipAdi,
                OgrenimTipKod = s.OgrenimTipKod,
                OgrenimTipID = s.OgrenimTipID,
                YsMaxBasvuruDonemNo = s.YsMaxBasvuruDonemNo,
                YsBasToplamKrediKriteri = s.YsBasToplamKrediKriteri,
                YsBasEtikNotKriteri = s.YsBasEtikNotKriteri,
                YsBasSeminerNotKriteri = s.YsBasSeminerNotKriteri

            }).ToList();

            if (kModel.BaslangicTarihi == DateTime.MinValue || kModel.BitisTarihi == DateTime.MinValue)
            {
                if (kModel.BaslangicTarihi == DateTime.MinValue)
                {
                    mmMessage.Messages.Add("Geçerli Bir Başlangıç Tarih Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BaslangicTarihi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BaslangicTarihi" });
                if (kModel.BitisTarihi == DateTime.MinValue)
                {
                    mmMessage.Messages.Add("Geçerli Bir Bitiş Tarih Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitisTarihi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BitisTarihi" });
            }
            else if (kModel.BaslangicTarihi >= kModel.BitisTarihi)
            {
                mmMessage.Messages.Add("Başlangıç tarihi bitiş tarihinden büyük olamaz!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BaslangicTarihi" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitisTarihi" });
            }
            else
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BaslangicTarihi" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BitisTarihi" });
            }
            var donemDto = new EgitimOgretimDonemDto();
            if (kModel.OgretimYili.IsNullOrWhiteSpace() == false)
            {
                var oy = kModel.OgretimYili.Split('/').ToList();
                donemDto.BaslangicYili = oy[0].ToInt().Value;
                donemDto.BitisYili = oy[1].ToInt().Value;
                donemDto.Donem = oy[2].ToInt().Value;
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgretimYili" });
            }
            else
            {
                mmMessage.Messages.Add("Öğretim yılı seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgretimYili" });
            }
            if (mmMessage.Messages.Count == 0)
            {

                var surecCount = _context.YeterlikSurecis.Count(p => p.EnstituKod == kModel.EnstituKod && p.YeterlikSurecID != kModel.YeterlikSurecID &&
                                                                 (
                                                                     (p.BaslangicTarihi <= kModel.BaslangicTarihi && p.BitisTarihi >= kModel.BaslangicTarihi)
                                                                     ||
                                                                     (p.BaslangicTarihi <= kModel.BitisTarihi && p.BitisTarihi >= kModel.BitisTarihi)
                                                                     ||
                                                                     (kModel.BaslangicTarihi <= p.BaslangicTarihi && kModel.BitisTarihi >= p.BaslangicTarihi)
                                                                     ||
                                                                     (kModel.BaslangicTarihi <= p.BitisTarihi && kModel.BitisTarihi >= p.BitisTarihi)
                                                                 ));
                if (surecCount > 0)
                {
                    mmMessage.Messages.Add("Girmiş olduğunuz tarihler için daha önceden yeterlik süreci kayıt edilmiştir.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BaslangicTarihi" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitisTarihi" });
                }
            }

            //if (mmMessage.Messages.Count == 0)
            //{
            //    foreach (var item in yeterlikSureciOgrenimTipKriterleri)
            //    {
            //        if (!item.YsBasToplamKrediKriteri.HasValue || item.YsBasToplamKrediKriteri <= 0)
            //        {
            //            mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için Min Kredi bilgisi 0 dan büyük olmalı.");
            //        }
            //        if (item.YsBasEtikNotKriteri.IsNullOrWhiteSpace())
            //        {
            //            mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için Etik dersi not kriteri bilgisi giriniz.");
            //        }
            //        if (item.YsBasSeminerNotKriteri.IsNullOrWhiteSpace())
            //        {
            //            mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için Seminer dersi not kriteri bilgisi giriniz.");
            //        }
            //    }
            //}

            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.BaslangicYil = donemDto.BaslangicYili;
                kModel.BitisYil = donemDto.BitisYili;
                kModel.DonemID = donemDto.Donem;

                if (kModel.YeterlikSurecID <= 0)
                {
                    var eklenen = _context.YeterlikSurecis.Add(new YeterlikSureci
                    {
                        EnstituKod = kModel.EnstituKod,
                        BaslangicYil = kModel.BaslangicYil,
                        BitisYil = kModel.BitisYil,
                        DonemID = kModel.DonemID,
                        BaslangicTarihi = kModel.BaslangicTarihi,
                        BitisTarihi = kModel.BitisTarihi,
                        IsAktif = true,
                        IslemTarihi = kModel.IslemTarihi,
                        IslemYapanID = kModel.IslemYapanID,
                        IslemYapanIP = kModel.IslemYapanIP
                    });
                    _context.SaveChanges();
                    kModel.YeterlikSurecID = eklenen.YeterlikSurecID;


                }
                else
                {
                    var data = _context.YeterlikSurecis.First(p => p.YeterlikSurecID == kModel.YeterlikSurecID);
                    data.EnstituKod = kModel.EnstituKod;
                    data.BaslangicYil = kModel.BaslangicYil;
                    data.BitisYil = kModel.BitisYil;
                    data.DonemID = kModel.DonemID;
                    data.IsAktif = kModel.IsAktif;
                    data.BaslangicTarihi = kModel.BaslangicTarihi;
                    data.BitisTarihi = kModel.BitisTarihi;
                    data.IslemTarihi = DateTime.Now;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemYapanIP = kModel.IslemYapanIP;
                    _context.YeterlikSurecOgrenimTipleris.RemoveRange(data.YeterlikSurecOgrenimTipleris);

                    LogIslemleri.LogEkle("YeterlikSureci", IslemTipi.Update, data.ToJson());

                }


                _context.YeterlikSurecOgrenimTipleris.AddRange(kModel.KmYeterlikSureciOgrenimTipKriterleris.Select(s => new YeterlikSurecOgrenimTipleri
                {
                    YeterlikSurecID = kModel.YeterlikSurecID,
                    OgrenimTipID = s.OgrenimTipID,
                    OgrenimTipKod = s.OgrenimTipKod,
                    YsMaxBasvuruDonemNo = s.YsMaxBasvuruDonemNo,
                    YsBasEtikNotKriteri = s.YsBasEtikNotKriteri,
                    YsBasSeminerNotKriteri = s.YsBasSeminerNotKriteri,
                    YsBasToplamKrediKriteri = s.YsBasToplamKrediKriteri,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id,
                    IslemYapanIP = UserIdentity.Ip


                }));
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(), "Value", "Caption", kModel.OgretimYili);
            ViewBag.MmMessage = mmMessage;
            return View(kModel);
        }

        public ActionResult KriterMuafOgrenciler(int id)
        {
            var surec = _context.YeterlikSurecis.First(p => p.YeterlikSurecID == id);
            return View(surec);
        }
        public ActionResult KriterMuafOgrenciEkle(int yeterlikSurecId, int? ogrenciId)
        {
            var success = false;
            var message = "";
            if (!ogrenciId.HasValue)
            {
                message = "Öğrenci seçiniz.";
            }
            else if (_context.YeterlikSureciKriterMuafOgrencilers.Any(p => p.YeterlikSurecID == yeterlikSurecId && p.KullaniciID == ogrenciId.Value))
            {
                message = "Bu öğrenci daha önce eklendi.";
            }
            else
            {
                _context.YeterlikSureciKriterMuafOgrencilers.Add(new YeterlikSureciKriterMuafOgrenciler
                {
                    YeterlikSurecID = yeterlikSurecId,
                    KullaniciID = ogrenciId.Value,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id
                });
                _context.SaveChanges();
                success = true;
            } 
            return new { success, message }.ToJsonResult();

        }
        public ActionResult KriterMuafOgrenciSil(int yeterlikSurecId, int ogrenciId)
        {
           
            if (_context.YeterlikSureciKriterMuafOgrencilers.Any(p => p.YeterlikSurecID == yeterlikSurecId && p.KullaniciID == ogrenciId))
            {
                var ogrenci = _context.YeterlikSureciKriterMuafOgrencilers.First(p =>
                    p.YeterlikSurecID == yeterlikSurecId && p.KullaniciID == ogrenciId);
                _context.YeterlikSureciKriterMuafOgrencilers.Remove(ogrenci);
                _context.SaveChanges();
            }

            return true.ToJsonResult();
        }
        public ActionResult GetFilterKullanici(string term)
        {

            var ogrenciList = _context.Kullanicilars.Where(p => p.YtuOgrencisi && (p.Ad + " " + p.Soyad).Contains(term) || p.OgrenciNo.StartsWith(term) || p.TcKimlikNo.StartsWith(term)).Select(s => new
            {
                s.KullaniciID,
                s.Ad,
                s.Soyad,
                s.OgrenciNo,
                s.ResimAdi,
                s.Programlar.ProgramAdi
            }).Take(15).ToList()
                .Select(s => new
                {
                    id = s.KullaniciID,
                    s.ProgramAdi,
                    text = s.OgrenciNo + " " + s.Ad + " " + s.Soyad,
                    Images = s.ResimAdi.ToKullaniciResim()
                }).ToList();
            return ogrenciList.ToJsonResult();
        }


        [Authorize(Roles = RoleNames.YeterlikSureciSil)]
        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage();

            var kayit = _context.YeterlikSurecis.FirstOrDefault(p => p.YeterlikSurecID == id);

            if (kayit != null)
            {
                var donemAdi = kayit.BaslangicYil + "/" + kayit.BitisYil + " " + kayit.Donemler.DonemAdi;
                try
                {
                    _context.YeterlikSurecis.Remove(kayit);
                    _context.SaveChanges();
                    mmMessage.Title = "Uyarı";
                    mmMessage.Messages.Add(donemAdi + " Dönemine ait Yeterlik süreci silindi!");
                    mmMessage.MessageType = Msgtype.Success;
                    mmMessage.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    var errMessage = "'" + donemAdi + "' Dönemine ait Yeterlik süreci silinirken bir hata oluştu! </br> Hata:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(errMessage, "YeterlikSureci/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                    mmMessage.Title = "Hata";
                    mmMessage.Messages.Add(errMessage);
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                }
            }
            else
            {
                mmMessage.Title = "Hata";
                mmMessage.Messages.Add("Silmek istediğiniz Yeterlik süreci sistemde bulunamadı!");
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = true;
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return new { mmMessage.IsSuccess, Messages = strView }.ToJsonResult();
        }


       
    }
}