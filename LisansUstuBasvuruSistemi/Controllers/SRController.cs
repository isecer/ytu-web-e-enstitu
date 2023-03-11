using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class SrController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();

        public ActionResult Index(string ekd)
        {
            return Index(new FmTalepler { }, ekd);
        }


        [HttpPost]
        public ActionResult Index(FmTalepler model, string ekd)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var kulls = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
            var bbModel = new IndexPageInfoDto
            {
                SistemBasvuruyaAcik = SrAyar.SalonRezervasyonTalebiAcikmi.GetAyarSr(enstituKod, "0").ToBoolean().Value,
                DonemAdi = DonemlerBus.CmbGetAkademikBulundugumuzTarih(DateTime.Now).Caption,
                EnstituYetki = true, // UserIdentity.Current.SeciliEnstituKodu == _EnstituKod;
                Enstitü = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod)
            };

            var ttip = _entities.SRTalepTipleris.Where(p => p.IsTezSinavi == false && p.SRTalepTipKullanicilars.Any(a => a.KullaniciTipID == kulls.KullaniciTipID)).ToList();

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
                        var ots = _entities.OgrenimTipleris.First(p => p.EnstituKod == kulls.EnstituKod && p.OgrenimTipKod == kulls.OgrenimTipKod);
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


            var q = from s in _entities.SRTalepleris
                    join tt in _entities.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in _entities.Kullanicilars on s.TalepYapanID equals k.KullaniciID
                    join kt in _entities.KullaniciTipleris on k.KullaniciTipID equals kt.KullaniciTipID
                    join sal in _entities.SRSalonlars on s.SRSalonID equals sal.SRSalonID
                    join hg in _entities.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                    join d in _entities.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                    join ot in _entities.OgrenimTipleris.Where(p => p.EnstituKod == kulls.EnstituKod) on k.OgrenimTipKod equals ot.OgrenimTipKod into defOt
                    from Ot in defOt.DefaultIfEmpty()
                    join otl in _entities.OgrenimTipleris on Ot.OgrenimTipID equals otl.OgrenimTipID into def1
                    from defOtl in def1.DefaultIfEmpty()
                    where s.EnstituKod == enstituKod && s.TalepYapanID == UserIdentity.Current.Id
                    select new FrTalepler
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
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.Tarih).ThenBy(t => t.BasSaat);
            var indexModel = new MIndexBilgi();
            var btDurulari = SrTalepleriBus.GetSrDurumList();
            foreach (var item in btDurulari)
            {
                var tipCount = q.Count(p => p.SRDurumID == item.SRDurumID);
                indexModel.ListB.Add(new mxRowModel { Key = item.DurumAdi, ClassName = item.ClassName, Color = item.Color, Toplam = tipCount });
            }
            indexModel.Toplam = model.RowCount; 
            model.data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
            ViewBag.bModel = bbModel;
            ViewBag.IndexModel = indexModel;
            ViewBag.SRSalonID = new SelectList(SrTalepleriBus.GetCmbSalonlar(enstituKod, true), "Value", "Caption", model.SRSalonID);
            ViewBag.SRDurumID = new SelectList(SrTalepleriBus.GetCmbSrDurumListe(true), "Value", "Caption", model.SRDurumID);
            return View(model);
        }

        public ActionResult TalepYap(int? id, string ekd, int? kullaniciId = null, string dlgid = "")
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };


            var yetkiliK = RoleNames.SrTalepDuzelt.InRoleCurrent();


            var model = new kmSRTalep
            {
                EnstituKod = enstituKod
            };
            bool isTezSinavi = false;


            if (yetkiliK)
            {
                if (kullaniciId.HasValue) model.TalepYapanID = kullaniciId.Value;
                else if (id.HasValue)
                {
                    var tlp = _entities.SRTalepleris.First(p => p.SRTalepID == id.Value);
                    model.TalepYapanID = tlp.TalepYapanID;
                }
                else model.TalepYapanID = UserIdentity.Current.Id;
            }
            else model.TalepYapanID = UserIdentity.Current.Id;

            var kulls = _entities.Kullanicilars.First(p => p.KullaniciID == model.TalepYapanID);

            var ttip = _entities.SRTalepTipleris.Where(p => p.SRTalepTipKullanicilars.Any(a => a.KullaniciTipID == kulls.KullaniciTipID)).ToList();
            if (ttip.Count > 0)
            {
                if (kulls.KullaniciTipID == KullaniciTipBilgi.YerliOgrenci || kulls.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci)
                    if ((kulls.YtuOgrencisi && (kulls.OgrenimTipKod == OgrenimTipi.Doktra || kulls.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans) && kulls.OgrenimDurumID == OgrenimDurum.HalenOğrenci) == false)
                    {
                        mmMessage.Messages.Add("Salon Rezervasyon talebi yapabilmek Öğrenim Seviyenizin Doktora veya Tezli YL ve Öğrenim durumunuzun Halen Öğrenci olarak güncellemeniz gerekmektedir.");
                    }
            }
            else
            {
                mmMessage.Messages.Add("Salon Rezervasyon talebi yapmak için yetkili değilsiniz.");
            }



            if (SrAyar.SalonRezervasyonTalebiAcikmi.GetAyarSr(enstituKod, "0").ToBoolean().Value)
            {
                if (id.HasValue)
                {

                    var data = _entities.SRTalepleris.First(p => p.SRTalepID == id.Value);
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
                    model.SRTaleplerJuris = _entities.SRTaleplerJuris.Where(p => p.SRTalepID == id).ToList();
                    isTezSinavi = data.SRTalepTipleri.IsTezSinavi;
                    if (yetkiliK == false)
                    {
                        // dil altyapısı eklenecek
                        if (data.TalepYapanID != UserIdentity.Current.Id)
                        {
                            var basvuruBilgi = data.Kullanicilar.Ad + " " + data.Kullanicilar.Soyad + " Kullanıcısına ait <br/>" + data.Tarih.ToString() + " tarihli salon rezervasyon talebi  <br/>";
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Farklı bir kullanıcıya ait salon rezervasyon bilgisi güncellenmek isteniyor! \r\n SRTalepID:" + model.SRTalepID + " \r\n " + basvuruBilgi.Replace("<br/>", "\r\n"), "SR/TalepYap", LogType.Saldırı);
                            mmMessage.Messages.Add("Başka bir kullanıcı adına rezervasyon yapmaya ya da düzeltmeye yetkili değilsiniz!");

                        }
                        else if (model.SRDurumID == SRTalepDurum.Reddedildi || model.SRDurumID == SRTalepDurum.Onaylandı)
                        {
                            var bDurumAdi = data.SRDurumlari.DurumAdi;
                            mmMessage.Messages.Add("Durumu " + bDurumAdi + " olan rezervasyonlar üzerinde düzenleme işlemi yapamazsınız!");
                        }


                    }

                }
                else
                {
                    var kul = _entities.Kullanicilars.First(p => p.KullaniciID == model.TalepYapanID);
                    model.AdSoyad = kul.Ad + " " + kul.Soyad;
                    model.OgrenciNo = kul.OgrenciNo;
                    model.Tarih = DateTime.Now.ToShortDateString().ToDate().Value;
                    model.SicilNo = kul.SicilNo;

                }
            }
            else
            {
                mmMessage.Messages.Add("Sistem salon rezervasyon talebi işlemine kapalıdır."); //?
            }
            if (mmMessage.Messages.Count > 0)
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Information, mmMessage.Messages.ToArray());
                return RedirectToAction("Index");

            }
            ViewBag.SRDurumID = new SelectList(SrTalepleriBus.GetCmbSrDurum(true, yetkiliK, model.SRSalonID <= 0), "Value", "Caption", model.SRDurumID);
            ViewBag.SRSalonID = new SelectList(SrTalepleriBus.GetCmbSalonlar(enstituKod, model.SRTalepTipID, true), "Value", "Caption", model.SRSalonID);

            ViewBag.SRTalepTipID = new SelectList(SrTalepleriBus.GetCmbSrTalepTipleri(true), "Value", "Caption", model.SRTalepTipID);

            ViewBag.MmMessage = mmMessage;
            ViewBag.IsTezSinavi = isTezSinavi;
            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult TalepYap(kmSRTalep kModel, string ekd)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var mmMessage = new MmMessage();
            bool belgeDuzenleYetki = RoleNames.BelgeTalebiDuzelt.InRoleCurrent();
            var ttip = new SRTalepTipleri();
            kModel.EnstituKod = enstituKod;
            var yetkiliK = RoleNames.SrTalepDuzelt.InRoleCurrent();

            if (kModel.TalepYapanID != UserIdentity.Current.Id && !yetkiliK)
            {
                string msg = "Başka bir kullanıcı adına rezervasyon yapmaya ya da düzeltmeye yetkili değilsiniz!";
                mmMessage.Messages.Add("Başka bir kullanıcı adına rezervasyon yapmaya ya da düzeltmeye yetkili değilsiniz!");
                SistemBilgilendirmeBus.SistemBilgisiKaydet(msg + "\r\n İşlem yapılmak istenen KullanıcıID:" + kModel.TalepYapanID + "\r\n İşlemYapanID:" + UserIdentity.Current.Id, "SR/TalepYap", LogType.Saldırı);
            }
            var kulls = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.TalepYapanID);
            var ttips = _entities.SRTalepTipleris.Where(p => p.SRTalepTipKullanicilars.Any(a => a.KullaniciTipID == kulls.KullaniciTipID)).ToList();
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
                ttip = _entities.SRTalepTipleris.First(p => p.SRTalepTipID == kModel.SRTalepTipID);
                if (ttip.IsTezSinavi)
                {
                    mmMessage.Messages.Add("Tez sınavı talebi mezuniyet işlemleri kısmından yapılmaktadır.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRTalepTipID" });
                }
                else
                {
                    var kotK = SrTalepleriBus.GetSrKotaBilgi(kModel.TalepYapanID, kModel.SRTalepTipID, (kModel.SRTalepID <= 0 ? (int?)null : kModel.SRTalepID));

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
                            var msg = SrTalepleriBus.SrKayitKontrol(kModel.SRSalonID.Value, kModel.SRTalepTipID, kModel.Tarih, ssts, kModel.SRTalepID, null);
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

            }
            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                int kid = 0;
                if (kModel.SRTalepID <= 0)
                {
                    var insD = new SRTalepleri
                    {
                        EnstituKod = enstituKod,
                        SRTalepTipID = kModel.SRTalepTipID,
                        TalepYapanID = kModel.TalepYapanID,
                        SRSalonID = kModel.SRSalonID,
                        Tarih = kModel.Tarih,
                        HaftaGunID = kModel.HaftaGunID,
                        BasSaat = kModel.BasSaat.Value,
                        BitSaat = kModel.BitSaat.Value
                    };
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
                    var inserted = _entities.SRTalepleris.Add(insD);
                    _entities.SaveChanges();
                    kid = inserted.SRTalepID;


                }
                else
                {
                    var kKayit = _entities.SRTalepleris.First(p => p.SRTalepID == kModel.SRTalepID);
                    kid = kModel.SRTalepID;
                    kKayit.EnstituKod = enstituKod;
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

                    var qJuriData = _entities.SRTaleplerJuris.Where(p => p.SRTalepID == kModel.SRTalepID).ToList();
                    _entities.SRTaleplerJuris.RemoveRange(qJuriData);
                }
                _entities.SaveChanges();
                if (ttip.IsTezSinavi)
                {
                    foreach (var item in qJuriL)
                    {
                        item.SRTalepID = kid;
                        _entities.SRTaleplerJuris.Add(item);
                    }
                    _entities.SaveChanges();
                }

                #region SendMail
                if (SrAyar.SrIslemlerindeMailGonder.GetAyarSr(enstituKod).ToBoolean().Value && kModel.SRTalepID <= 0)
                {
                    var enstLng = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod);
                    var talep = _entities.SRTalepleris.First(p => p.SRTalepID == kid);
                    var salon = _entities.SRSalonlars.First(p => p.SRSalonID == talep.SRSalonID);
                    var juriler = _entities.SRTaleplerJuris.Where(p => p.SRTalepID == talep.SRTalepID).ToList();
                    var haftaGunu = _entities.HaftaGunleris.First(p => p.HaftaGunID == talep.HaftaGunID);
                    var kullanıcı = _entities.Kullanicilars.First(p => p.KullaniciID == kModel.TalepYapanID);
                    var mmmC = new MailMainContentDto();
                    var enstituAdi = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod).EnstituAd;

                    mmmC.EnstituAdi = enstituAdi;
                    mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                    var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(enstituKod);
                    var sistemErisimAdresi = mailBilgi.SistemErisimAdresi;
                    var wurlAddr = sistemErisimAdresi.Split('/').ToList();
                    if (sistemErisimAdresi.Contains("//"))
                        sistemErisimAdresi = wurlAddr[0] + "//" + wurlAddr.Skip(2).Take(1).First();
                    else
                        sistemErisimAdresi = "http://" + wurlAddr.First();
                    mmmC.LogoPath = sistemErisimAdresi + "/Content/assets/images/ytu_logo_tr.png";
                    var mdl = new MailTableContentDto
                    {
                        AciklamaTextAlingCenter = true,
                        AciklamaBasligi = "Salon rezervasyon talebi işleminiz alınmıştır.",
                        GrupBasligi = "Rezervasyon talep detaylarınız"
                    };
                    mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Salon Adı", Aciklama = salon.SalonAdi });
                    mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Tarih", Aciklama = talep.Tarih.ToString("dd.MM.yyyyy") + " " + haftaGunu.HaftaGunAdi });
                    mdl.Detaylar.Add(new MailTableRowDto
                    {
                        Baslik = "Saat",
                        Aciklama = $"{talep.BasSaat:hh\\:mm}" + "-" +$"{talep.BitSaat:hh\\:mm}"
                    });
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
                            tezbasligi = tezDiliTr ? talep.MezuniyetBasvurulari.TezBaslikTr : talep.MezuniyetBasvurulari.TezBaslikEn;
                        }
                        mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Tez Başlığı", Aciklama = tezbasligi });
                        mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Tez Danışman Adı", Aciklama = talep.DanismanAdi });
                        if (talep.EsDanismanAdi.IsNullOrWhiteSpace() == false) mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Tez Eş Danışman Adı", Aciklama = talep.EsDanismanAdi });
                        if (talep.TezOzeti.IsNullOrWhiteSpace() == false) mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Tez Özeti", Aciklama = talep.TezOzetiHtml });


                        var mtcSinavJ = new MailTableContentDto
                        {
                            IsJuriBilgi = false,
                            GrupBasligi = "Jüri Bilgisi"
                        };
                        ;
                        foreach (var itemJr in juriler.Select((s, inx) => new { s, inx }).ToList())
                        {
                            mtcSinavJ.Detaylar.Add(new MailTableRowDto { SiraNo = (itemJr.inx + 1), Baslik = itemJr.s.JuriAdi, Aciklama = (itemJr.s.Telefon + " (" + itemJr.s.Email + ")"), });
                        }

                        mdl.Detaylar.Add(new MailTableRowDto
                        {
                            Colspan2 = true,
                            Aciklama = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", mtcSinavJ)
                        });
                    }
                    else
                    {
                        mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Açıklama", Aciklama = talep.Aciklama });
                    }
                    string content = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", mdl);
                    mmmC.Content = content;
                    string htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);
                    var eMailList = new List<MailSendList> { new MailSendList { EMail = talep.Kullanicilar.EMail, ToOrBcc = true } };
                    var snded = MailManager.SendMailRetVal(mailBilgi.EnstituKod, enstituAdi, htmlMail, eMailList, null);
                    if (snded != null)
                    {
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Salon rezervasyon talebi işlemi için mail gönderilirken bir hata oluştu! Hata: " + snded.ToExceptionMessage(), "SR/TalepYap", LogType.Hata);
                    }
                }
                #endregion

                if (kModel.TalepYapanID == UserIdentity.Current.Id) return RedirectToAction("Index");
                else return RedirectToAction("index", "SrGelenTalepler");

            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());

            }

            kModel.SRTaleplerJuris = qJuriL;
            ViewBag.SRDurumID = new SelectList(SrTalepleriBus.GetCmbSrDurum(true, belgeDuzenleYetki, kModel.SRSalonID <= 0), "Value", "Caption", kModel.SRDurumID);
            ViewBag.SRSalonID = new SelectList(SrTalepleriBus.GetCmbSalonlar(enstituKod, kModel.SRTalepTipID, true), "Value", "Caption", kModel.SRSalonID);
            ViewBag.SRTalepTipID = new SelectList(SrTalepleriBus.GetCmbSrTalepTipleri(true), "Value", "Caption", kModel.SRTalepTipID);
            ViewBag.MmMessage = mmMessage;
            ViewBag.IsTezSinavi = kModel.SRTalepTipID > 0 && ttip.IsTezSinavi;
            return View(kModel);
        }



        public ActionResult GetDetail(int id, bool isDelete, string ekd)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var kYetki = RoleNames.SrTalepDuzelt.InRoleCurrent();
            var q = (from s in _entities.SRTalepleris
                join tt in _entities.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID
                join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                join k in _entities.Kullanicilars on s.TalepYapanID equals k.KullaniciID
                join kt in _entities.KullaniciTipleris on k.KullaniciTipID equals kt.KullaniciTipID
                join sal in _entities.SRSalonlars on s.SRSalonID equals sal.SRSalonID into def1
                from defSal in def1.DefaultIfEmpty()
                join hg in _entities.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                join d in _entities.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                join ot in _entities.OgrenimTipleris on new { k.EnstituKod, k.OgrenimTipKod } equals new { ot.EnstituKod, OgrenimTipKod = (int?)ot.OgrenimTipKod } into defOt
                from Ot in defOt.DefaultIfEmpty()
                join otl in _entities.OgrenimTipleris on Ot.OgrenimTipID equals otl.OgrenimTipID into def2
                from defOtl in def2.DefaultIfEmpty()
                where s.EnstituKod == enstituKod && s.SRTalepID == id
                select new FrTalepler
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
                }).First();
            var kulls = _entities.Kullanicilars.First(p => p.KullaniciID == q.TalepYapanID);
            var bbModel = new IndexPageInfoDto();
            if (kulls.KullaniciTipID == KullaniciTipBilgi.IdariPersonel || kulls.KullaniciTipID == KullaniciTipBilgi.AkademikPersonel)
            {
                bbModel.BirimAdi = kulls.Birimler.BirimAdi;
                bbModel.UnvanAdi = kulls.Unvanlar.UnvanAdi;
                bbModel.SicilNo = kulls.SicilNo;
            }
            else if ((kulls.KullaniciTipID == KullaniciTipBilgi.YerliOgrenci || kulls.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci) && kulls.YtuOgrencisi)
            {
                var ots = _entities.OgrenimTipleris.First(p => p.OgrenimTipKod == kulls.OgrenimTipKod && p.EnstituKod == kulls.EnstituKod);
                bbModel.OgrenimDurumAdi = kulls.OgrenimDurumlari.OgrenimDurumAdi;
                bbModel.OgrenimTipAdi = ots.OgrenimTipAdi;
                bbModel.ProgramAdi = kulls.Programlar.ProgramAdi;
                bbModel.OgrenciNo = kulls.OgrenciNo;

            }
            ViewBag.bModel = bbModel; 
            ViewBag.SRDurumID = new SelectList(SrTalepleriBus.GetCmbSrDurumListe(true), "Value", "Caption", q.SRDurumID);
            ViewBag.IsDelete = isDelete; 
            return View(q);
        }



        [Authorize]
        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage(); 
            var kayit = _entities.SRTalepleris.FirstOrDefault(p => p.SRTalepID == id && p.TalepYapanID == UserIdentity.Current.Id);
            var basvuruBilgi = kayit.Kullanicilar.Ad + " " + "" + kayit.Kullanicilar.Soyad + " Kullanıcısına ait <br/>" + kayit.Tarih.ToShortDateString() + " " + kayit.BasSaat + "-" + kayit.BitSaat + " tarihli rezervasyon<br/>";
            try
            {
                if (kayit.SRDurumID != SRTalepDurum.TalepEdildi && RoleNames.SrGelenTalepler.InRoleCurrent() == false)
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
                    _entities.SRTalepleris.Remove(kayit);
                    _entities.SaveChanges();
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
                SistemBilgilendirmeBus.SistemBilgisiKaydet(basvuruBilgi + "silinemedi! Hata:" + ex.ToExceptionMessage(), "SR/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
            } 
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}