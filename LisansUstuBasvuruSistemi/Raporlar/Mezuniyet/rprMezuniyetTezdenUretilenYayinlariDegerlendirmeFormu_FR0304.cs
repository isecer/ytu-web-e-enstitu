using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.FilterModel;
using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprMezuniyetTezdenUretilenYayinlariDegerlendirmeFormu_FR0304 : DevExpress.XtraReports.UI.XtraReport
    {
        public rprMezuniyetTezdenUretilenYayinlariDegerlendirmeFormu_FR0304(int MezuniyetJuriOneriFormID, int? MezuniyetJuriOneriFormuJuriID)
        {
            InitializeComponent();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var JoForm = db.MezuniyetJuriOneriFormlaris.Where(p => p.MezuniyetJuriOneriFormID == MezuniyetJuriOneriFormID).First();

                var MBasvuru = JoForm.MezuniyetBasvurulari;

                bool IsYlOrDiger = MBasvuru.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans;

                xrCellEOYil.Text = MBasvuru.MezuniyetSureci.BaslangicYil.ToString() + "-" + MBasvuru.MezuniyetSureci.BitisYil.ToString();
                xrChkYariyilGuz.Checked = MBasvuru.MezuniyetSureci.DonemID == DonemBilgi.GuzYariyili;
                xrChkYariyilBahar.Checked = MBasvuru.MezuniyetSureci.DonemID == DonemBilgi.BaharYariyili;
                xrCellEnstituAdi.Text = MBasvuru.MezuniyetSureci.Enstituler.EnstituAd.ToUpper();
                xrCellAnabilimdaliProgramAdi.Text = MBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi.ToUpper() + " - " + MBasvuru.Programlar.ProgramAdi.ToUpper();
                xrCellOgrenciNo.Text = MBasvuru.OgrenciNo;
                xrCellOgrenciAdi.Text = MBasvuru.Ad + " " + MBasvuru.Soyad;

                var DanismanBilgi = JoForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi == "TezDanismani").First();
                xrCellTezDanismanBilgi.Text = DanismanBilgi.UnvanAdi + " " + DanismanBilgi.AdSoyad;

                chkTurkce.Checked = MBasvuru.IsTezDiliTr == true;
                chkYabanci.Checked = MBasvuru.IsTezDiliTr == false;

                var tezBasligiTr = "";
                var tezBasligiEn = "";

                tezBasligiTr = JoForm.IsTezBasligiDegisti == true
                    ? JoForm.YeniTezBaslikTr
                    : MBasvuru.TezBaslikTr;
                tezBasligiEn = JoForm.IsTezBasligiDegisti == true
                    ? JoForm.YeniTezBaslikEn
                    : MBasvuru.TezBaslikEn;

                xrCellTezBaslikTr.Text = tezBasligiTr;
                xrCellTezBaslikEn.Text = tezBasligiEn;

                if (!MBasvuru.TezEsDanismanAdi.IsNullOrWhiteSpace())
                {
                    xrCellTezEsDanismanBilgi.Text = MBasvuru.TezEsDanismanUnvani + " " + MBasvuru.TezEsDanismanAdi;
                }
                #region YayinBilgi

                var YayinSartiVar = Management.MezuniyetAktifYonetmelik(MBasvuru.MezuniyetSurecID, MBasvuru.KullaniciID,MBasvuru.MezuniyetBasvurulariID).MezuniyetSureciYonetmelikleriOTs.Any(a => a.OgrenimTipKod == MBasvuru.OgrenimTipKod && a.IsZorunlu);
                chkYayinSartiVardir.Checked = YayinSartiVar;
                chkYayinSartYoktur.Checked = !YayinSartiVar;

                var yayins = (from qs in db.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariID == MBasvuru.MezuniyetBasvurulariID)
                              join s in db.MezuniyetSureciYayinTurleris on new { qs.MezuniyetBasvurulari.MezuniyetSurecID, qs.MezuniyetYayinTurID } equals new { s.MezuniyetSurecID, s.MezuniyetYayinTurID }
                              join sd in db.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                              join inx in db.MezuniyetYayinIndexTurleris on new { qs.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into definx
                              from inxD in definx.DefaultIfEmpty()
                              select new rprMezuniyetTezDegerlendirmeYayinBilgi
                              {
                                  YayinTurAdi = sd.MezuniyetYayinTurAdi,
                                  YayinBasligi = qs.YayinBasligi,
                                  IndexBilgisi = inxD != null ? inxD.IndexTurAdi : "",
                                  DoiNumarasi = qs.MezuniyetYayinLinkTurID == 5 ? qs.MezuniyetYayinKaynakLinki : "",



                              }).ToList();
                xrRowYayinBilgiBaslik.Visible = YayinSartiVar || yayins.Count > 0;
                this.DataSource = yayins;
                #endregion

                if (MezuniyetJuriOneriFormuJuriID.HasValue)
                {
                    var secilenJuri = JoForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.MezuniyetJuriOneriFormuJuriID == MezuniyetJuriOneriFormuJuriID).First();
                    xrCellJuriAdSoyad.Text = secilenJuri.UnvanAdi + " " + secilenJuri.AdSoyad;
                    xrCellJuriUniversiteAdi.Text = secilenJuri.UniversiteID.HasValue ? secilenJuri.Universiteler.Ad.ToUpper() : secilenJuri.UniversiteAdi.ToUpper();
                    xrCellJuriTelefon.Text = "";
                    xrCellJuriFaks.Text = "";
                    xrCellJuriEPosta.Text = secilenJuri.EMail;
                }
                else
                {
                    xrCellJuriAdSoyad.Text = "";
                    xrCellJuriUniversiteAdi.Text = "";
                    xrCellJuriTelefon.Text = "";
                    xrCellJuriFaks.Text = "";
                    xrCellJuriEPosta.Text = "";
                }
                this.DisplayName = (MBasvuru.Ad + " " + MBasvuru.Soyad) + " FR-0304 Tezden Üretilen Yayınları Değerlendirme Formu";

            }
        }


    }
}
