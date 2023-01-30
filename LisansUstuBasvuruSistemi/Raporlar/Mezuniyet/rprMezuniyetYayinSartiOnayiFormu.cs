using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;

namespace LisansUstuBasvuruSistemi.Raporlar
{

    public partial class rprMezuniyetYayinSartiOnayiFormu : DevExpress.XtraReports.UI.XtraReport
    {
        public rprMezuniyetYayinSartiOnayiFormu(int MezuniyetBasvurulariID)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var MBasvuru = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == MezuniyetBasvurulariID).First();
                var enstituLng = MBasvuru.MezuniyetSureci.Enstituler;
                lblEnstituAdi.Text = enstituLng.EnstituAd;
                var onaylayan = db.Kullanicilars.Where(p => p.KullaniciID == MBasvuru.IslemYapanID).First();
                cell_OnaylayanKisi.Text = onaylayan.Ad + " " + onaylayan.Soyad;


                var KayitDonemi = MBasvuru.KayitOgretimYiliBaslangic + "/" + (MBasvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.Where(p => p.DonemID == MBasvuru.KayitOgretimYiliDonemID.Value).First().DonemAdi + " - " + MBasvuru.KayitTarihi.ToDateString();
                lngLbl_AkademikTarih.Text = "Eğitim Öğretim Yılı";
                cell_AkademikYil.Text = MBasvuru.MezuniyetSureci.BaslangicYil + "-" + MBasvuru.MezuniyetSureci.BitisYil + " " + db.Donemlers.Where(p => p.DonemID == MBasvuru.MezuniyetSureci.DonemID).First().DonemAdi;
                lblKayitTarihi.Text = "Kayıt Tarihi";
                cell_KayitTarihi.Text = KayitDonemi;
                Lbl_AdSoyad.Text = "Ad Soyad";
                cell_AdiSoyadi.Text = MBasvuru.Ad + " " + MBasvuru.Soyad;
                lbl_AnabilimdaliProg.Text = "Anabilim Dalı / Program";
                cell_AnabilimdaliProg.Text = MBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi + " / " + MBasvuru.Programlar.ProgramAdi;
                lbl_OgrenciNo.Text = "Öğrenci No";
                cell_OgrenciNo.Text = MBasvuru.OgrenciNo;
                lbl_OgrenimTipi.Text = "Öğrenim Seviyesi";
                cell_OgrenimTipi.Text = db.OgrenimTipleris.First(p => p.OgrenimTipKod == MBasvuru.OgrenimTipKod && p.EnstituKod == MBasvuru.MezuniyetSureci.EnstituKod).OgrenimTipAdi;

                lbl_yayinSartiVarMi.Text = "Yayın Şartı Var Mı?";
                MBasvuru.MezuniyetBasvurulariYayins.Select(s => s.MezuniyetYayinTurID).ToList();
                var msYTurs = MezuniyetBus.GetMezuniyetAktifYonetmelik(MBasvuru.MezuniyetSurecID, MBasvuru.KullaniciID, MezuniyetBasvurulariID);
                var yturIds = msYTurs.MezuniyetSureciYonetmelikleriOTs.Where(p => p.OgrenimTipKod == MBasvuru.OgrenimTipKod && p.IsZorunlu).Select(s => s.MezuniyetYayinTurID).ToList();
                var yturs = db.MezuniyetSureciYayinTurleris.Where(p => yturIds.Contains(p.MezuniyetYayinTurID)).ToList();
                chk_YS_Var.Checked = yturs.Count > 0;
                chk_YS_Var.Text = "Var";
                chk_YS_Yok.Checked = yturs.Count == 0;
                chk_YS_Yok.Text = "Yok";
                cell_AdSoyadImza.Text = MBasvuru.Ad + " " + MBasvuru.Soyad;
                cell_Tarih.Text = MBasvuru.BasvuruTarihi.ToDateString();
                var ysEvet = "Evet";
                var ysHayir = "Hayır";
                capt_YayinTuru.Text = "Yayın Türü Adı";
                capt_DanismanIsmiVarmi.Text = "Danışmanın İsmi Var mı?";
                capt_TezIcerikUygunMu.Text = "Tez İçeriği ile Uyumlu mu?";
                capt_Aciklama.Text = "Açıklama";

                var ysOnaylandı = "Onaylandı";
                var ysOnaylanmadı = "Onaylanmadı";
                lbl_min_ys_saglandi.Text = "MİNİMUM YAYIN ŞARTI SAĞLANMAKTADIR";
                var yayins = (
                              from yy in db.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariID == MBasvuru.MezuniyetBasvurulariID)
                              join s in db.MezuniyetSureciYayinTurleris.Where(p => p.MezuniyetSurecID == MBasvuru.MezuniyetSurecID) on yy.MezuniyetYayinTurID equals s.MezuniyetYayinTurID
                              join sd in db.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                              join Inx in db.MezuniyetYayinIndexTurleris on new { yy.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)Inx.MezuniyetYayinIndexTurID } into defInx
                              from InxB in defInx.DefaultIfEmpty()
                              select new raporMezuniyetBasvuruFormModel
                              {

                                  YayinTurAdi = sd.MezuniyetYayinTurAdi,
                                  DanismanIsmiVarMi = yy.DanismanIsmiVar == true ? ysEvet : ysHayir,
                                  TezIcerigiIleUygunMu = yy.TezIcerikUyumuVar == true ? ysEvet : ysHayir,
                                  Index = InxB != null ? InxB.IndexTurAdi : "",
                                  Aciklama = yy.Onaylandi == true ? ysOnaylandı : ysOnaylanmadı

                              }
                              ).ToList();
                this.DataSource = yayins;
                lblOnayMsj.Text = "Yayın belgelerimin ve bilgilerimin doğruluğunu taahhüt eder, aksi takdirde bütün haklarımdan feragat ederim.";


                var urlAdd = enstituLng.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MBB_" + MBasvuru.MezuniyetBasvurulariID + "_" + MBasvuru.RowID.ToString();
                xrQRCode.ImageUrl = urlAdd;
                xrQRCode.Image = urlAdd.CreateQrCode();

                this.DisplayName = MBasvuru.Ad + " " + MBasvuru.Soyad + " Mezuniyet Yayın Şartı Onayı Formu";
            }

        }

    }
}
