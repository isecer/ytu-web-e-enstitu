using System.Linq;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{

    public partial class RprMezuniyetYayinSartiOnayiFormu : DevExpress.XtraReports.UI.XtraReport
    {
        public RprMezuniyetYayinSartiOnayiFormu(int mezuniyetBasvurulariId)
        {
            InitializeComponent();

            using (var entities = new LubsDbEntities())
            {
                var mBasvuru = entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
                var enstituLng = mBasvuru.MezuniyetSureci.Enstituler;
                lblEnstituAdi.Text = enstituLng.EnstituAd;
                var onaylayan = entities.Kullanicilars.First(p => p.KullaniciID == mBasvuru.IslemYapanID);
                cell_OnaylayanKisi.Text = onaylayan.Ad + " " + onaylayan.Soyad;


                var kayitDonemi = mBasvuru.KayitOgretimYiliBaslangic + "/" + (mBasvuru.KayitOgretimYiliBaslangic + 1) + " " + entities.Donemlers.First(p => p.DonemID == mBasvuru.KayitOgretimYiliDonemID.Value).DonemAdi + " - " + mBasvuru.KayitTarihi.ToFormatDate();
                lngLbl_AkademikTarih.Text = "Eğitim Öğretim Yılı";
                cell_AkademikYil.Text = mBasvuru.MezuniyetSureci.BaslangicYil + "-" + mBasvuru.MezuniyetSureci.BitisYil + " " + entities.Donemlers.First(p => p.DonemID == mBasvuru.MezuniyetSureci.DonemID).DonemAdi;
                lblKayitTarihi.Text = "Kayıt Tarihi";
                cell_KayitTarihi.Text = kayitDonemi;
                Lbl_AdSoyad.Text = "Ad Soyad";
                cell_AdiSoyadi.Text = mBasvuru.Ad + " " + mBasvuru.Soyad;
                lbl_AnabilimdaliProg.Text = "Anabilim Dalı / Program";
                cell_AnabilimdaliProg.Text = mBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi + " / " + mBasvuru.Programlar.ProgramAdi;
                lbl_OgrenciNo.Text = "Öğrenci No";
                cell_OgrenciNo.Text = mBasvuru.OgrenciNo;
                lbl_OgrenimTipi.Text = "Öğrenim Seviyesi";
                cell_OgrenimTipi.Text = entities.OgrenimTipleris.First(p => p.OgrenimTipKod == mBasvuru.OgrenimTipKod && p.EnstituKod == mBasvuru.MezuniyetSureci.EnstituKod).OgrenimTipAdi;

                lbl_yayinSartiVarMi.Text = "Yayın Şartı Var Mı?";
                var msYTurs = MezuniyetBus.GetMezuniyetAktifYonetmelik(mBasvuru.MezuniyetSurecID, mBasvuru.KullaniciID, mezuniyetBasvurulariId);
                var yturIds = msYTurs.MezuniyetSureciYonetmelikleriOTs.Where(p => p.OgrenimTipKod == mBasvuru.OgrenimTipKod && p.IsZorunlu).Select(s => s.MezuniyetYayinTurID).ToList();
                var yturs = entities.MezuniyetSureciYayinTurleris.Where(p => yturIds.Contains(p.MezuniyetYayinTurID)).ToList();
                chk_YS_Var.Checked = yturs.Count > 0;
                chk_YS_Var.Text = "Var";
                chk_YS_Yok.Checked = yturs.Count == 0;
                chk_YS_Yok.Text = "Yok";
                cell_AdSoyadImza.Text = mBasvuru.Ad + " " + mBasvuru.Soyad;
                cell_Tarih.Text = mBasvuru.BasvuruTarihi.ToFormatDate();
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
                              from yy in entities.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariID == mBasvuru.MezuniyetBasvurulariID)
                              join s in entities.MezuniyetSureciYayinTurleris.Where(p => p.MezuniyetSurecID == mBasvuru.MezuniyetSurecID) on yy.MezuniyetYayinTurID equals s.MezuniyetYayinTurID
                              join sd in entities.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                              join inx in entities.MezuniyetYayinIndexTurleris on new { yy.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into defInx
                              from inxB in defInx.DefaultIfEmpty()
                              select new RaporMezuniyetBasvuruFormModel
                              {

                                  YayinTurAdi = sd.MezuniyetYayinTurAdi,
                                  DanismanIsmiVarMi = yy.DanismanIsmiVar == true ? ysEvet : ysHayir,
                                  TezIcerigiIleUygunMu = yy.TezIcerikUyumuVar == true ? ysEvet : ysHayir,
                                  Index = inxB != null ? inxB.IndexTurAdi : "",
                                  Aciklama = yy.Onaylandi == true ? ysOnaylandı : ysOnaylanmadı

                              }
                              ).ToList();
                DataSource = yayins;
                lblOnayMsj.Text = "Yayın belgelerimin ve bilgilerimin doğruluğunu taahhüt eder, aksi takdirde bütün haklarımdan feragat ederim.";


                var urlAdd = enstituLng.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MBB_" + mBasvuru.MezuniyetBasvurulariID + "_" + mBasvuru.RowID.ToString();
                xrQRCode.ImageUrl = urlAdd;
                xrQRCode.Image = urlAdd.CreateQrCode();

                DisplayName = mBasvuru.Ad + " " + mBasvuru.Soyad + " Mezuniyet Yayın Şartı Onayı Formu";
            }

        }

    }
}
