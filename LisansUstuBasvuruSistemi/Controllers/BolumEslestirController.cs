using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.BolumEslestir)]
    public class BolumEslestirController : Controller
    {

        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmBolumEslestir { PageSize = 15 });
        }
        [HttpPost]
        public ActionResult Index(fmBolumEslestir model, bool export = false)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();

            var q = from s in db.Programlars
                    join e in db.AnabilimDallaris on new { s.AnabilimDaliID } equals new { e.AnabilimDaliID }
                    join at in db.AlesTipleris on new { s.AlesTipID } equals new { at.AlesTipID }
                    join enst in db.Enstitulers on new { e.EnstituKod } equals new { enst.EnstituKod }
                    where EnstKods.Contains(enst.EnstituKod)
                    select new frBolumEslestir
                    {
                        EnstituKod = enst.EnstituKod,
                        EnstituAd = enst.EnstituAd,
                        AnabilimDaliKod = e.AnabilimDaliKod,
                        AnabilimDaliAdi = e.AnabilimDaliAdi,
                        AlesTipID = s.AlesTipID,
                        AlesTipAdi = at.AlesTipAdi,
                        ProgramKod = s.ProgramKod,
                        ProgramAdi = s.ProgramAdi,
                        Ingilizce = s.Ingilizce,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,

                        OgrenciBolumAdlari = db.OgrenciBolumleris.Where(p => p.BolumEslestirs.Any(a => a.ProgramKod == s.ProgramKod)).Select(s2 => s2.BolumAdi).ToList(),
                        KullaniciTipAdi = s.KullaniciTipleri.KullaniciTipAdi

                    };
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.ProgramAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.ProgramAdi.Contains(model.ProgramAdi) || p.AnabilimDaliAdi.Contains(model.ProgramAdi));
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace())
            {
                if (model.Sort.Contains("EslestirmeSayisi"))
                {
                    if (model.Sort.Contains(" DESC")) q = q.OrderByDescending(o => o.OgrenciBolumAdlari.Count);
                    else q = q.OrderBy(o => o.OgrenciBolumAdlari.Count);
                }
                else q = q.OrderBy(model.Sort);
            }
            else q = q.OrderBy(o => o.AnabilimDaliAdi).ThenBy(o => o.ProgramAdi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();


            if (export && model.RowCount > 0)
            {
                GridView gv = new GridView();
                var qExp = q.AsQueryable();
                if (model.EnstituKod.IsNullOrWhiteSpace() == false)
                    qExp = qExp.Where(p => p.EnstituKod == model.EnstituKod);
                if (model.ProgramAdi.IsNullOrWhiteSpace() == false)
                    qExp = qExp.Where(p => p.ProgramAdi == model.ProgramAdi);
                var qdata = qExp.Select(s => new
                {
                    s.EnstituAd,
                    s.AnabilimDaliAdi,
                    s.ProgramKod,
                    s.ProgramAdi,
                    AlaniciOgrenciBolumAdlari = db.OgrenciBolumleris.Where(p => p.BolumEslestirs.Any(a => a.ProgramKod == s.ProgramKod)).Select(s2 => s2.BolumAdi).ToList(),
                    AlanDisiKabulEdilecekOgrenciBolumAdlari = db.OgrenciBolumleris.Where(p => p.ProgramlarAlandisiBolumKisitlamalaris.Any(a => a.Programlar.IsAlandisiBolumKisitlamasi && a.ProgramKod == s.ProgramKod)).Select(s2 => s2.BolumAdi).ToList()


                }).ToList();

                gv.DataSource = qdata.Select(s => new
                {
                    s.EnstituAd,
                    s.AnabilimDaliAdi,
                    s.ProgramKod,
                    s.ProgramAdi,
                    AlaniciOgrenciBolumAdlari = string.Join(Environment.NewLine, s.AlaniciOgrenciBolumAdlari.Select((s2, inx) => new { BolumAdi = (inx + 1) + ") " + s2 }).Select(sx => sx.BolumAdi).ToList()),
                    AlanDisiKabulEdilecekOgrenciBolumAdlari = string.Join("\n", s.AlanDisiKabulEdilecekOgrenciBolumAdlari.Select((s2, inx) => new { BolumAdi = (inx + 1) + ") " + s2 }).Select(sx => sx.BolumAdi).ToList()),
                }).ToList();
                gv.DataBind();
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_BolumEslestirmeListesi_" + DateTime.Now.ToString("dd.MM.yyyy") + ".xls");
            }



            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            ViewBag.IndexModel = IndexModel;
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            return View(model);
        }
        public ActionResult Kayit(string id, string EnstituKod)
        {
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var model = new kmBolumEslestir();

            if (id.IsNullOrWhiteSpace() == false)
            {
                var data = (from s in db.Programlars
                            join e in db.AnabilimDallaris on new { s.AnabilimDaliKod, EnstituKod } equals new { e.AnabilimDaliKod, e.EnstituKod }
                            join at in db.AlesTipleris on new { s.AlesTipID } equals new { at.AlesTipID }
                            join enst in db.Enstitulers on new { e.EnstituKod } equals new { enst.EnstituKod }
                            where EnstKods.Contains(enst.EnstituKod) && s.ProgramKod == id
                            select new
                            {
                                enst.EnstituKod,
                                enst.EnstituAd,
                                e.AnabilimDaliKod,
                                e.AnabilimDaliAdi,
                                s.AlesTipID,
                                at.AlesTipAdi,
                                s.ProgramKod,
                                s.ProgramAdi,
                                s.BolumEslestirs,

                            }).FirstOrDefault();
                if (data != null)
                {
                    model.EnstituKod = data.EnstituKod;
                    model.AnabilimDaliAdi = data.AnabilimDaliAdi;
                    model.EnstituAd = data.EnstituAd;
                    model.ProgramKod = data.ProgramKod;
                    model.ProgramAdi = data.ProgramAdi;
                    model.OgrenciBolumID = data.BolumEslestirs.Select(s => s.OgrenciBolumID).ToList();
                }
            }
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(kmBolumEslestir kModel, List<int> KullaniciTipIds = null, List<int> OgrenciBolumIDs = null)
        {
            var MmMessage = new MmMessage();
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            #region Kontrol

            if (kModel.ProgramKod.IsNullOrWhiteSpace())
            {
                string msg = "Program kod bilgisi alınamadı!";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "ProgramKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "ProgramKod" });



            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                var lstOB = kModel.OgrenciBolumID ?? new List<int>();
                var pBoles = db.BolumEslestirs.Where(p => p.ProgramKod == kModel.ProgramKod).ToList();
                db.BolumEslestirs.RemoveRange(pBoles);
                foreach (var item in lstOB)
                {
                    var enst = db.BolumEslestirs.Add(new BolumEslestir
                    {
                        ProgramKod = kModel.ProgramKod,
                        OgrenciBolumID = item,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemTarihi = DateTime.Now
                    });
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            var data = (from s in db.Programlars
                        join e in db.AnabilimDallaris on new { s.AnabilimDaliKod } equals new { e.AnabilimDaliKod }
                        join at in db.AlesTipleris on new { s.AlesTipID } equals new { at.AlesTipID }
                        join enst in db.Enstitulers on new { e.EnstituKod } equals new { enst.EnstituKod }
                        where EnstKods.Contains(enst.EnstituKod)
                        select new
                        {
                            enst.EnstituKod,
                            enst.EnstituAd,
                            e.AnabilimDaliKod,
                            e.AnabilimDaliAdi,
                            s.AlesTipID,
                            at.AlesTipAdi,
                            s.ProgramKod,
                            s.ProgramAdi,

                        }).FirstOrDefault();
            if (data != null)
            {
                kModel.EnstituKod = data.EnstituKod;
                kModel.AnabilimDaliAdi = data.AnabilimDaliAdi;
                kModel.EnstituAd = data.EnstituAd;
                kModel.ProgramKod = data.ProgramKod;
                kModel.ProgramAdi = data.ProgramAdi;
            }
            ViewBag.MmMessage = MmMessage;
            return View(kModel);
        }

    }
}
