using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class SRController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();

        public ActionResult Index(string EKD)
        {
            return Index(new fmTalepler { }, EKD);
        }


        [HttpPost]
        public ActionResult Index(fmTalepler model, string EKD)
        {
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var kulls = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();
            var bbModel = new BasvuruBilgiModel();
            bbModel.SistemBasvuruyaAcik = SRAyar.SalonRezervasyonTalebiAcikmi.getAyarSR(_EnstituKod, "0").ToBoolean().Value;
            bbModel.DonemAdi = Management.getAkademikBulundugumuzTarih( DateTime.Now).Caption;
            bbModel.EnstituYetki = true;// UserIdentity.Current.SeciliEnstituKodu == _EnstituKod;
            bbModel.Enstitü = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod ).First();

            var ttip = db.SRTalepTipleris.Where(p => p.IsTezSinavi == false && p.SRTalepTipKullanicilars.Any(a => a.KullaniciTipID == kulls.KullaniciTipID)).ToList();

            if (ttip.Count > 0)
            {
                if (kulls.KullaniciTipID == KullaniciTipBilgi.IdariPersonel || kulls.KullaniciTipID == KullaniciTipBilgi.AkademikPersonel)
                {
                    bbModel.KullaniciTipYetki = true;
                    bbModel.BirimAdi = kulls.Birimler.BirimAdi;
                    bbModel.UnvanAdi = kulls.Unvanlar.UnvanAdi;
                    bbModel.SicilNo = kulls.SicilNo;
                }
                else if (kulls.KullaniciTipID == KullaniciTipBilgi.YerliOgrenci || kulls.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci)
                {

                    if ((kulls.YtuOgrencisi && (kulls.OgrenimTipKod == OgrenimTipi.Doktra || kulls.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans) && kulls.OgrenimDurumID == OgrenimDurum.HalenOğrenci) == false)
                    {
                        bbModel.KullaniciTipYetki = false;
                        bbModel.KullaniciTipYetkiYokMsj = "Salon Rezervasyon talebi yapabilmek Öğrenim Seviyenizin Doktora veya Tezli YL ve Öğrenim durumunuzun Halen Öğrenci olarak güncellemeniz gerekmektedir.";
                    }
                    else
                    {
                        var ots = db.OgrenimTipleris.Where(p => p.EnstituKod == kulls.EnstituKod && p.OgrenimTipKod == kulls.OgrenimTipKod ).First();
                        bbModel.KullaniciTipYetki = true;
                        bbModel.OgrenimDurumAdi = kulls.OgrenimDurumlari.OgrenimDurumAdi;
                        bbModel.OgrenimTipAdi = ots.OgrenimTipAdi;
                        bbModel.ProgramAdi = kulls.Programlar.ProgramAdi;
                        bbModel.OgrenciNo = kulls.OgrenciNo;
                    }
                }
            }
            else
            {
                bbModel.KullaniciTipYetki = false;
                bbModel.KullaniciTipYetkiYokMsj = "Salon Rezervasyon talebi yapmak için yetkili değilsiniz.";
            }


            var q = from s in db.SRTalepleris
                    join tt in db.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID
                    join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in db.Kullanicilars on s.TalepYapanID equals k.KullaniciID
                    join kt in db.KullaniciTipleris on k.KullaniciTipID equals kt.KullaniciTipID
                    join sal in db.SRSalonlars on s.SRSalonID equals sal.SRSalonID
                    join hg in db.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                    join d in db.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                    join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == kulls.EnstituKod) on k.OgrenimTipKod equals ot.OgrenimTipKod into defOt
                    from Ot in defOt.DefaultIfEmpty()
                    join otl in db.OgrenimTipleris on Ot.OgrenimTipID equals otl.OgrenimTipID into def1
                    from defOtl in def1.DefaultIfEmpty()
                    where s.EnstituKod == _EnstituKod && s.TalepYapanID == UserIdentity.Current.Id
                    select new frTalepler
                    {
                        SRTalepID = s.SRTalepID,
                        EnstituKod = e.EnstituKod,
                        EnstituAdi = e.EnstituAd,
                        TalepYapanID = s.TalepYapanID,
                        TalepTipAdi = tt.TalepTipAdi,
                        SRTalepTipID = s.SRTalepTipID,
                        OgrenciNo = k.OgrenciNo,
                        SicilNo = k.SicilNo,
                        TalepYapan = k.Ad + " " + k.Soyad,
                        ResimAdi = k.ResimAdi,
                        OgrenimTipAdi = defOtl != null ? defOtl.OgrenimTipAdi : "",
                        KullaniciTipAdi = kt.KullaniciTipAdi,
                        SRSalonID = s.SRSalonID,
                        SalonAdi = sal.SalonAdi,
                        Tarih = s.Tarih,
                        HaftaGunID = s.HaftaGunID,
                        HaftaGunAdi = hg.HaftaGunAdi,
                        BasSaat = s.BasSaat,
                        BitSaat = s.BitSaat,
                        SRDurumID = s.SRDurumID,
                        DurumAdi = d.DurumAdi,
                        DurumListeAdi = d.DurumAdi,
                        ClassName = d.ClassName,
                        Color = d.Color,
                        SRDurumAciklamasi = s.SRDurumAciklamasi,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP
                    };

            // if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.SRSalonID.HasValue) q = q.Where(p => p.SRSalonID == model.SRSalonID.Value);
            if (model.SRDurumID.HasValue) q = q.Where(p => p.SRDurumID == model.SRDurumID.Value);
            if (!model.Aciklama.IsNullOrWhiteSpace()) q = q.Where(p => p.TalepYapan.Contains(model.Aciklama));
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.Tarih).ThenBy(t => t.BasSaat);
            var IndexModel = new MIndexBilgi();
            var btDurulari = Management.SRDurumList();
            foreach (var item in btDurulari)
            {
                var tipCount = q.Where(p => p.SRDurumID == item.SRDurumID).Count();
                IndexModel.ListB.Add(new mxRowModel { Key = item.DurumAdi, ClassName = item.ClassName, Color = item.Color, Toplam = tipCount });
            }
            IndexModel.Toplam = model.RowCount;
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
            ViewBag.bModel = bbModel;
            ViewBag.IndexModel = IndexModel;
            ViewBag.SRSalonID = new SelectList(Management.cmbSalonlar(_EnstituKod ,true), "Value", "Caption", model.SRSalonID);
            ViewBag.SRDurumID = new SelectList(Management.cmbSRDurumListe( true), "Value", "Caption", model.SRDurumID);
            return View(model);
        }

        public ActionResult TalepYap(int? id, string EKD, int? KullaniciID = null, string dlgid = "")
        {
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;


            var yetkiliK = RoleNames.SRTalepDuzelt.InRoleCurrent();


            var model = new kmSRTalep();
            model.EnstituKod = _EnstituKod;
            bool IsTezSinavi = false;


            if (yetkiliK)
            {
                if (KullaniciID.HasValue) model.TalepYapanID = KullaniciID.Value;
                else if (id.HasValue)
                {
                    var tlp = db.SRTalepleris.Where(p => p.SRTalepID == id.Value).First();
                    model.TalepYapanID = tlp.TalepYapanID;
                }
                else model.TalepYapanID = UserIdentity.Current.Id;
            }
            else model.TalepYapanID = UserIdentity.Current.Id;

            var kulls = db.Kullanicilars.Where(p => p.KullaniciID == model.TalepYapanID).First();

            var ttip = db.SRTalepTipleris.Where(p => p.SRTalepTipKullanicilars.Any(a => a.KullaniciTipID == kulls.KullaniciTipID)).ToList();
            if (ttip.Count > 0)
            {
                if (kulls.KullaniciTipID == KullaniciTipBilgi.YerliOgrenci || kulls.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci)
                    if ((kulls.YtuOgrencisi && (kulls.OgrenimTipKod == OgrenimTipi.Doktra || kulls.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans) && kulls.OgrenimDurumID == OgrenimDurum.HalenOğrenci) == false)
                    {
                        MmMessage.Messages.Add("Salon Rezervasyon talebi yapabilmek Öğrenim Seviyenizin Doktora veya Tezli YL ve Öğrenim durumunuzun Halen Öğrenci olarak güncellemeniz gerekmektedir.");
                    }
            }
            else
            {
                MmMessage.Messages.Add("Salon Rezervasyon talebi yapmak için yetkili değilsiniz.");
            }



            if (SRAyar.SalonRezervasyonTalebiAcikmi.getAyarSR(_EnstituKod, "0").ToBoolean().Value)
            {
                if (id.HasValue)
                {

                    var data = db.SRTalepleris.Where(p => p.SRTalepID == id.Value).First();
                    model.SRTalepID = data.SRTalepID;
                    model.SRTalepTipID = data.SRTalepTipID;
                    model.EnstituKod = data.EnstituKod;
                    model.AdSoyad = data.Kullanicilar.Ad + " " + data.Kullanicilar.Soyad;
                    model.OgrenciNo = data.Kullanicilar.OgrenciNo;
                    model.SicilNo = data.Kullanicilar.SicilNo;
                    model.TalepYapanID = data.TalepYapanID;
                    model.SRSalonID = data.SRSalonID;
                    model.Tarih = data.Tarih;
                    model.HaftaGunID = data.HaftaGunID;
                    model.BasSaat = data.BasSaat;
                    model.BitSaat = data.BitSaat; 
                    model.DanismanAdi = data.DanismanAdi;
                    model.EsDanismanAdi = data.EsDanismanAdi;
                    model.TezOzeti = data.TezOzeti;
                    model.TezOzetiHtml = data.TezOzetiHtml;
                    model.SRDurumID = data.SRDurumID;
                    model.SRDurumAciklamasi = data.SRDurumAciklamasi;
                    model.IslemTarihi = data.IslemTarihi;
                    model.IslemYapanID = data.IslemYapanID;
                    model.IslemYapanIP = data.IslemYapanIP;
                    model.Aciklama = data.Aciklama;
                    model.SRTaleplerJuris = db.SRTaleplerJuris.Where(p => p.SRTalepID == id).ToList();
                    IsTezSinavi = data.SRTalepTipleri.IsTezSinavi;
                    if (yetkiliK == false)
                    {
                        // dil altyapısı eklenecek
                        if (data.TalepYapanID != UserIdentity.Current.Id)
                        {
                            var basvuruBilgi = data.Kullanicilar.Ad + " " + data.Kullanicilar.Soyad + " Kullanıcısına ait <br/>" + data.Tarih.ToString() + " tarihli salon rezervasyon talebi  <br/>";
                            Management.SistemBilgisiKaydet("Farklı bir kullanıcıya ait salon rezervasyon bilgisi güncellenmek isteniyor! \r\n SRTalepID:" + model.SRTalepID + " \r\n " + basvuruBilgi.Replace("<br/>", "\r\n"), "SR/TalepYap", BilgiTipi.Saldırı);
                            MmMessage.Messages.Add("Başka bir kullanıcı adına rezervasyon yapmaya ya da düzeltmeye yetkili değilsiniz!");

                        }
                        else if (model.SRDurumID == SRTalepDurum.Reddedildi || model.SRDurumID == SRTalepDurum.Onaylandı)
                        {
                            var bDurumAdi = data.SRDurumlari.DurumAdi;
                            MmMessage.Messages.Add("Durumu " + bDurumAdi + " olan rezervasyonlar üzerinde düzenleme işlemi yapamazsınız!");
                        }
                       

                    }

                }
                else
                { 
                    var kul = db.Kullanicilars.Where(p => p.KullaniciID == model.TalepYapanID).First();
                    model.AdSoyad = kul.Ad + " " + kul.Soyad;
                    model.OgrenciNo = kul.OgrenciNo;
                    model.Tarih = DateTime.Now.ToShortDateString().ToDate().Value;
                    model.SicilNo = kul.SicilNo;
                     
                }
            }
            else
            {
                MmMessage.Messages.Add("Sistem salon rezervasyon talebi işlemine kapalıdır."); //?
            }
            if (MmMessage.Messages.Count > 0)
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Information, MmMessage.Messages.ToArray());
                return RedirectToAction("Index");

            }
            ViewBag.SRDurumID = new SelectList(Management.cmbSRDurum( true, yetkiliK, model.SRSalonID <= 0), "Value", "Caption", model.SRDurumID);
            ViewBag.SRSalonID = new SelectList(Management.cmbSalonlar(_EnstituKod, model.SRTalepTipID ,true), "Value", "Caption", model.SRSalonID);
            ViewBag.SRTalepTipID = new SelectList(Management.cmbSRTalepTipleri( true), "Value", "Caption", model.SRTalepTipID);
           
            ViewBag.MmMessage = MmMessage;
            ViewBag.IsTezSinavi = IsTezSinavi;
            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult TalepYap(kmSRTalep kModel, string EKD)
        {
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var mmMessage = new MmMessage();
            bool belgeDuzenleYetki = RoleNames.BelgeTalebiDuzelt.InRoleCurrent();
            var ttip = new SRTalepTipleri();
            kModel.EnstituKod = _EnstituKod;
            var yetkiliK = RoleNames.SRTalepDuzelt.InRoleCurrent();

            if (kModel.TalepYapanID != UserIdentity.Current.Id && !yetkiliK)
            {
                string msg = "Başka bir kullanıcı adına rezervasyon yapmaya ya da düzeltmeye yetkili değilsiniz!";
                mmMessage.Messages.Add("Başka bir kullanıcı adına rezervasyon yapmaya ya da düzeltmeye yetkili değilsiniz!");
                Management.SistemBilgisiKaydet(msg + "\r\n İşlem yapılmak istenen KullanıcıID:" + kModel.TalepYapanID + "\r\n İşlemYapanID:" + UserIdentity.Current.Id, "SR/TalepYap", BilgiTipi.Saldırı);
            }
            var kulls = db.Kullanicilars.Where(p => p.KullaniciID == kModel.TalepYapanID).First();
            var ttips = db.SRTalepTipleris.Where(p => p.SRTalepTipKullanicilars.Any(a => a.KullaniciTipID == kulls.KullaniciTipID)).ToList();
            kModel.AdSoyad = kulls.Ad + " " + kulls.Soyad;
            kModel.SicilNo = kulls.SicilNo;
            kModel.OgrenciNo = kulls.OgrenciNo;
            if (ttips.Count > 0)
            {
                if (kulls.KullaniciTipID == KullaniciTipBilgi.YerliOgrenci || kulls.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci)
                {

                    if ((kulls.YtuOgrencisi && (kulls.OgrenimTipKod == OgrenimTipi.Doktra || kulls.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans) && kulls.OgrenimDurumID == OgrenimDurum.HalenOğrenci) == false)
                    {
                        mmMessage.Messages.Add("Salon Rezervasyon talebi yapabilmek Öğrenim Seviyenizin Doktora veya Tezli YL ve Öğrenim durumunuzun Halen Öğrenci olarak güncellemeniz gerekmektedir.");
                    }
                }
            }
            else
            {
                mmMessage.Messages.Add("Salon Rezervasyon talebi yapmak için yetkili değilsiniz.");
            }

            var qjuriAdi = kModel.JuriAdi.Select((s, inx) => new { s, inx }).ToList();
            var qTelefon = kModel.Telefon.Select((s, inx) => new { s, inx }).ToList();
            var qEmail = kModel.Email.Select((s, inx) => new { s, inx }).ToList();
            var qJuriL = (from s in qjuriAdi
                          join t in qTelefon on s.inx equals t.inx
                          join e in qEmail on s.inx equals e.inx
                          select new SRTaleplerJuri
                          {
                              JuriAdi = s.s,
                              Telefon = t.s,
                              Email = e.s
                          }).ToList();





            if (kModel.SRTalepTipID <= 0)
            {
                mmMessage.Messages.Add("Talep Tipi seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRTalepTipID" });
            }
            else
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "SRTalepTipID" });
                ttip = db.SRTalepTipleris.Where(p =>  p.SRTalepTipID == kModel.SRTalepTipID).First();
                var kotK = Management.SRkotaKontrol(kModel.TalepYapanID, kModel.SRTalepTipID, (kModel.SRTalepID <= 0 ? (int?)null : kModel.SRTalepID));
                if (kotK.ValueB)
                {
                    if (kModel.SRSalonID <= 0)
                    {
                        mmMessage.Messages.Add("Salon Seçiniz");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRSalonID" });
                    }
                    else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "SRSalonID" });

                    if (kModel.Tarih == DateTime.MinValue)
                    {

                        mmMessage.Messages.Add("Talep Tarihi Seçiniz");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                    }
                    if (!kModel.BasSaat.HasValue || !kModel.BitSaat.HasValue)//bitiş saati mi baz alınsın başlangıç saati mi ?
                    {
                        mmMessage.Messages.Add("Lütfen belirtilen güne ait uygun saat seçiniz!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                    }

                    if (ttip.IsTezSinavi)
                    { 
                        if (kModel.DanismanAdi.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add("Danışman Adı Giriniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DanismanAdi" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "DanismanAdi" });
                        if (kModel.TezOzeti.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add("Tez Özeti Giriniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezOzeti" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "TezOzeti" });
 
                    }
                    else
                    {
                        if (kModel.Aciklama.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add("Açıklama Giriniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Aciklama" });
                        }
                        else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "Aciklama" });
                    }
                    if (mmMessage.Messages.Count == 0)
                    {
                        var ssts = new List<SROzelTanimSaatler>();
                        ssts.Add(new SROzelTanimSaatler { BasSaat = kModel.BasSaat.Value, BitSaat = kModel.BitSaat.Value });
                        var msg = Management.SRKayitKontrol(kModel.SRSalonID.Value, kModel.SRTalepTipID, kModel.Tarih, ssts ,kModel.SRTalepID, null);
                        mmMessage.Messages.AddRange(msg.Messages);
                    }
                }
                else
                {
                    string msg = kotK.ValueS;
                    mmMessage.Messages.Add(msg);
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRTalepTipID" });

                }

            }
            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                int KID = 0;
                if (kModel.SRTalepID <= 0)
                {
                    var insD = new SRTalepleri();
                    insD.EnstituKod = _EnstituKod;
                    insD.SRTalepTipID = kModel.SRTalepTipID;
                    insD.TalepYapanID = kModel.TalepYapanID;
                    insD.SRSalonID = kModel.SRSalonID;
                    insD.Tarih = kModel.Tarih;
                    insD.HaftaGunID = kModel.HaftaGunID;
                    insD.BasSaat = kModel.BasSaat.Value;
                    insD.BitSaat = kModel.BitSaat.Value;
                    if (ttip.IsTezSinavi)
                    { 
                        insD.DanismanAdi = kModel.DanismanAdi;
                        insD.EsDanismanAdi = kModel.EsDanismanAdi;
                        insD.TezOzeti = kModel.TezOzeti;
                        insD.TezOzetiHtml = kModel.TezOzetiHtml;
                    }
                    else
                    {
                        insD.Aciklama = kModel.Aciklama;
                    }
                    insD.SRDurumID = SRTalepDurum.TalepEdildi;
                    insD.SRDurumAciklamasi = kModel.SRDurumAciklamasi;
                    insD.IslemTarihi = kModel.IslemTarihi;
                    insD.IslemYapanID = kModel.IslemYapanID;
                    insD.IslemYapanIP = kModel.IslemYapanIP;

                    insD.UniqueID = Guid.NewGuid();
                    var Inserted = db.SRTalepleris.Add(insD);
                    db.SaveChanges();
                    KID = Inserted.SRTalepID;


                }
                else
                {
                    var kKayit = db.SRTalepleris.Where(p => p.SRTalepID == kModel.SRTalepID).First();
                    KID = kModel.SRTalepID;
                    kKayit.EnstituKod = _EnstituKod;
                    kKayit.SRTalepTipID = kModel.SRTalepTipID;
                    kKayit.TalepYapanID = kModel.TalepYapanID;
                    kKayit.SRSalonID = kModel.SRSalonID;
                    kKayit.Tarih = kModel.Tarih;
                    kKayit.HaftaGunID = kModel.HaftaGunID;
                    kKayit.BasSaat = kModel.BasSaat.Value;
                    kKayit.BitSaat = kModel.BitSaat.Value;
                    if (ttip.IsTezSinavi)
                    { 
                        kKayit.DanismanAdi = kModel.DanismanAdi;
                        kKayit.EsDanismanAdi = kModel.EsDanismanAdi;
                        kKayit.TezOzeti = kModel.TezOzeti;
                        kKayit.TezOzetiHtml = kModel.TezOzetiHtml;
                        kKayit.Aciklama = null;
                    }
                    else
                    { 
                        kKayit.DanismanAdi = null;
                        kKayit.EsDanismanAdi = null;
                        kKayit.TezOzeti = null;
                        kKayit.TezOzetiHtml = null;
                        kKayit.Aciklama = kModel.Aciklama;
                    }
                    kKayit.SRDurumID = kModel.SRDurumID;
                    kKayit.SRDurumAciklamasi = kModel.SRDurumAciklamasi;
                    kKayit.IslemTarihi = kModel.IslemTarihi;
                    kKayit.IslemYapanID = kModel.IslemYapanID;
                    kKayit.IslemYapanIP = kModel.IslemYapanIP;

                    var qJuriData = db.SRTaleplerJuris.Where(p => p.SRTalepID == kModel.SRTalepID).ToList();
                    db.SRTaleplerJuris.RemoveRange(qJuriData);
                }
                db.SaveChanges();
                if (ttip.IsTezSinavi)
                {
                    foreach (var item in qJuriL)
                    {
                        item.SRTalepID = KID;
                        db.SRTaleplerJuris.Add(item);
                    }
                    db.SaveChanges();
                }

                #region SendMail
                if (SRAyar.getAyarSR(SRAyar.SRIslemlerindeMailGonder, _EnstituKod).ToBoolean().Value && kModel.SRTalepID <= 0)
                {
                    var enstLng = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod ).First();
                    var talep = db.SRTalepleris.Where(p => p.SRTalepID == KID).First();
                    var salon = db.SRSalonlars.Where(p =>  p.SRSalonID == talep.SRSalonID).First();
                    var juriler = db.SRTaleplerJuris.Where(p => p.SRTalepID == talep.SRTalepID).ToList();
                    var haftaGunu = db.HaftaGunleris.Where(p =>  p.HaftaGunID == talep.HaftaGunID).First();
                    var kullanıcı = db.Kullanicilars.Where(p => p.KullaniciID == kModel.TalepYapanID).First();
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
                    var mdl = new mailTableContent();
                    mdl.AciklamaTextAlingCenter = true;
                    mdl.AciklamaBasligi = "Salon rezervasyon talebi işleminiz alınmıştır.";
                    mdl.GrupBasligi = "Rezervasyon talep detaylarınız";
                    mdl.Detaylar.Add(new mailTableRow { Baslik = "Salon Adı", Aciklama = salon.SalonAdi });
                    mdl.Detaylar.Add(new mailTableRow { Baslik = "Tarih", Aciklama = talep.Tarih.ToString("dd.MM.yyyyy") + " " + haftaGunu.HaftaGunAdi });
                    mdl.Detaylar.Add(new mailTableRow { Baslik = "Saat", Aciklama = string.Format("{0:hh\\:mm}", talep.BasSaat) + "-" + string.Format("{0:hh\\:mm}", talep.BitSaat) });
                    if (ttip.IsTezSinavi)
                    {
                        var tezDiliTr = talep.MezuniyetBasvurulari.IsTezDiliTr == true;
                        var tezbasligi = "";
                        if (talep.IsTezBasligiDegisti == true)
                        {
                            tezbasligi = tezDiliTr ? talep.YeniTezBaslikTr : talep.YeniTezBaslikEn;
                        }
                        else if (talep.MezuniyetBasvurulari.MezuniyetJuriOneriFormlaris.First().IsTezBasligiDegisti == true)
                        {
                            tezbasligi = tezDiliTr ? talep.MezuniyetBasvurulari.MezuniyetJuriOneriFormlaris.First().YeniTezBaslikTr : talep.MezuniyetBasvurulari.MezuniyetJuriOneriFormlaris.First().YeniTezBaslikEn;
                        }
                        else
                        {
                            tezbasligi = tezDiliTr ? talep.MezuniyetBasvurulari.TezBaslikTr: talep.MezuniyetBasvurulari.TezBaslikEn;
                        }
                        mdl.Detaylar.Add(new mailTableRow { Baslik = "Tez Başlığı", Aciklama = tezbasligi });
                        mdl.Detaylar.Add(new mailTableRow { Baslik = "Tez Danışman Adı", Aciklama = talep.DanismanAdi });
                        if (talep.EsDanismanAdi.IsNullOrWhiteSpace() == false) mdl.Detaylar.Add(new mailTableRow { Baslik = "Tez Eş Danışman Adı", Aciklama = talep.EsDanismanAdi });
                        if (talep.TezOzeti.IsNullOrWhiteSpace() == false) mdl.Detaylar.Add(new mailTableRow { Baslik = "Tez Özeti", Aciklama = talep.TezOzetiHtml });


                        var mtcSinavJ = new mailTableContent();
                        mtcSinavJ.IsJuriBilgi = false;
                        mtcSinavJ.GrupBasligi = "Jüri Bilgisi";
                        ;
                        foreach (var itemJr in juriler.Select((s, inx) => new { s, inx }).ToList())
                        {
                            mtcSinavJ.Detaylar.Add(new mailTableRow { SiraNo = (itemJr.inx + 1), Baslik = itemJr.s.JuriAdi, Aciklama = (itemJr.s.Telefon + " (" + itemJr.s.Email + ")"), });
                        }

                        mdl.Detaylar.Add(new mailTableRow
                        {
                            Colspan2 = true,
                            Aciklama = Management.RenderPartialView("Ajax", "getMailTableContent", mtcSinavJ)
                        });
                    }
                    else
                    {
                        mdl.Detaylar.Add(new mailTableRow { Baslik = "Açıklama", Aciklama = talep.Aciklama });
                    }
                    string content = Management.RenderPartialView("Ajax", "getMailTableContent", mdl);
                    mmmC.Content = content;
                    string htmlMail = Management.RenderPartialView("Ajax", "getMailContent", mmmC);
                    var User = mailBilgi.SmtpKullaniciAdi;
                    var EMailList = new List<MailSendList> { new MailSendList { EMail = talep.Kullanicilar.EMail, ToOrBcc = true } };
                    var snded = MailManager.sendMailRetVal(mailBilgi.EnstituKod, enstituAdi, htmlMail, EMailList, null);
                    if (snded != null)
                    {
                        Management.SistemBilgisiKaydet("Salon rezervasyon talebi işlemi için mail gönderilirken bir hata oluştu! Hata: " + snded.ToExceptionMessage(), "SR/TalepYap", BilgiTipi.Hata);
                    }
                }
                #endregion

                if (kModel.TalepYapanID == UserIdentity.Current.Id) return RedirectToAction("Index");
                else return RedirectToAction("index", "SRGelenTalepler");

            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());

            }

            kModel.SRTaleplerJuris = qJuriL;
            ViewBag.SRDurumID = new SelectList(Management.cmbSRDurum( true, belgeDuzenleYetki, kModel.SRSalonID <= 0), "Value", "Caption", kModel.SRDurumID);
            ViewBag.SRSalonID = new SelectList(Management.cmbSalonlar(_EnstituKod, kModel.SRTalepTipID ,true), "Value", "Caption", kModel.SRSalonID);
            ViewBag.SRTalepTipID = new SelectList(Management.cmbSRTalepTipleri( true), "Value", "Caption", kModel.SRTalepTipID);
            ViewBag.MmMessage = mmMessage;
            ViewBag.IsTezSinavi = kModel.SRTalepTipID <= 0 ? false : ttip.IsTezSinavi;
            return View(kModel);
        }



        public ActionResult getDetail(int id, bool IsDelete, string EKD)
        {
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var kYetki = RoleNames.SRTalepDuzelt.InRoleCurrent();
            var q = Queryable.First<frTalepler>((from s in db.SRTalepleris
                                                 join tt in db.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID
                                                 join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                                                 join k in db.Kullanicilars on s.TalepYapanID equals k.KullaniciID
                                                 join kt in db.KullaniciTipleris on k.KullaniciTipID equals kt.KullaniciTipID
                                                 join sal in db.SRSalonlars on s.SRSalonID equals sal.SRSalonID into def1
                                                 from defSal in def1.DefaultIfEmpty()
                                                 join hg in db.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                                                 join d in db.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                                                 join ot in db.OgrenimTipleris on new { k.EnstituKod, k.OgrenimTipKod } equals new { ot.EnstituKod, OgrenimTipKod = (int?)ot.OgrenimTipKod } into defOt
                                                 from Ot in defOt.DefaultIfEmpty()
                                                 join otl in db.OgrenimTipleris on Ot.OgrenimTipID equals otl.OgrenimTipID into def2
                                                 from defOtl in def2.DefaultIfEmpty()
                                                 where s.EnstituKod == _EnstituKod && s.SRTalepID == id
                                                 select new frTalepler
                                                 {
                                                     SRTalepID = s.SRTalepID,
                                                     EnstituKod = e.EnstituKod,
                                                     EnstituAdi = e.EnstituAd,
                                                     TalepYapanID = s.TalepYapanID,
                                                     SRTalepTipID = s.SRTalepTipID,
                                                     TalepTipAdi = tt.TalepTipAdi,
                                                     IsTezSinavi = tt.IsTezSinavi,
                                                     OgrenciNo = k.OgrenciNo,
                                                     TalepYapan = k.Ad + " " + k.Soyad,
                                                     ResimAdi = k.ResimAdi,
                                                     OgrenimTipAdi = defOtl != null ? defOtl.OgrenimTipAdi : "",
                                                     KullaniciTipAdi = kt.KullaniciTipAdi,
                                                     SRSalonID = s.SRSalonID,
                                                     SalonAdi = s.SRSalonID.HasValue ? defSal.SalonAdi : s.SalonAdi,
                                                     Tarih = s.Tarih,
                                                     HaftaGunID = s.HaftaGunID,
                                                     HaftaGunAdi = hg.HaftaGunAdi,
                                                     BasSaat = s.BasSaat,
                                                     BitSaat = s.BitSaat, 
                                                     DanismanAdi = s.DanismanAdi,
                                                     EsDanismanAdi = s.EsDanismanAdi,
                                                     TezOzeti = s.TezOzeti,
                                                     TezOzetiHtml = s.TezOzetiHtml,
                                                     Aciklama = s.Aciklama,
                                                     SRDurumID = s.SRDurumID,
                                                     DurumAdi = d.DurumAdi,
                                                     DurumListeAdi = d.DurumAdi,
                                                     ClassName = d.ClassName,
                                                     Color = d.Color,
                                                     SRDurumAciklamasi = s.SRDurumAciklamasi,
                                                     IslemTarihi = s.IslemTarihi,
                                                     IslemYapanID = s.IslemYapanID,
                                                     IslemYapanIP = s.IslemYapanIP,
                                                     JuriBilgi = s.SRTaleplerJuris.ToList()
                                                 }));
            var kulls = db.Kullanicilars.Where(p => p.KullaniciID == q.TalepYapanID).First();
            var bbModel = new BasvuruBilgiModel();
            if (kulls.KullaniciTipID == KullaniciTipBilgi.IdariPersonel || kulls.KullaniciTipID == KullaniciTipBilgi.AkademikPersonel)
            {
                bbModel.BirimAdi = kulls.Birimler.BirimAdi;
                bbModel.UnvanAdi = kulls.Unvanlar.UnvanAdi;
                bbModel.SicilNo = kulls.SicilNo;
            }
            else if ((kulls.KullaniciTipID == KullaniciTipBilgi.YerliOgrenci || kulls.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci) && kulls.YtuOgrencisi)
            {
                var ots = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == kulls.OgrenimTipKod && p.EnstituKod == kulls.EnstituKod ).First();
                bbModel.OgrenimDurumAdi = kulls.OgrenimDurumlari.OgrenimDurumAdi;
                bbModel.OgrenimTipAdi = ots.OgrenimTipAdi;
                bbModel.ProgramAdi = kulls.Programlar.ProgramAdi;
                bbModel.OgrenciNo = kulls.OgrenciNo;

            }
            ViewBag.bModel = bbModel;

            ViewBag.SRDurumID = new SelectList(Management.cmbSRDurumListe( true), "Value", "Caption", q.SRDurumID);
            ViewBag.IsDelete = IsDelete;
            
            return View(q);
        }



        [Authorize]
        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage();
            //var mmMessage = Management.getBasvuruSilKontrol(id);

            //if (mmMmMessage.IsSuccess)
            //{
            var kayit = db.SRTalepleris.Where(p => p.SRTalepID == id && p.TalepYapanID == UserIdentity.Current.Id).FirstOrDefault();
            var basvuruBilgi = kayit.Kullanicilar.Ad + " " + "" + kayit.Kullanicilar.Soyad + " Kullanıcısına ait <br/>" + kayit.Tarih.ToShortDateString() + " " + kayit.BasSaat + "-" + kayit.BitSaat + " tarihli rezervasyon<br/>";
            try
            {
                if (kayit.SRDurumID != SRTalepDurum.TalepEdildi && RoleNames.SRGelenTalepler.InRoleCurrent() == false)
                {
                    mmMessage.Messages.Add("Onaylanan veya reddedilen taleplerinizi silemezsiniz");
                    mmMessage.Title = "Bilgilendirme";
                    mmMessage.IsSuccess = false;
                    mmMessage.MessageType = Msgtype.Information;
                }
                else
                {

                    mmMessage.Messages.Add(basvuruBilgi + "sistemden Silindi!");
                    mmMessage.Title = "Bilgilendirme";
                    db.SRTalepleris.Remove(kayit);
                    db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;
                }
            }
            catch (Exception ex)
            {
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add(basvuruBilgi + "silinemedi!");
                mmMessage.Title = "Hata";
                Management.SistemBilgisiKaydet(basvuruBilgi + "silinemedi! Hata:" + ex.ToExceptionMessage(), "SR/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
            }

            //}
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}