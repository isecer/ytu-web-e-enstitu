using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using System.Linq;

namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{
    public partial class RprMezuniyetTezdenUretilenYayinlariDegerlendirmeFormu_FR0304 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprMezuniyetTezdenUretilenYayinlariDegerlendirmeFormu_FR0304(int mezuniyetJuriOneriFormId, int? mezuniyetJuriOneriFormuJuriId)
        {
            InitializeComponent();
            using (var entities = new LubsDbEntities())
            {

                var joForm = entities.MezuniyetJuriOneriFormlaris.First(p => p.MezuniyetJuriOneriFormID == mezuniyetJuriOneriFormId);

                var mBasvuru = joForm.MezuniyetBasvurulari;



                xrCellEOYil.Text = mBasvuru.MezuniyetSureci.BaslangicYil.ToString() + "-" + mBasvuru.MezuniyetSureci.BitisYil.ToString();
                xrChkYariyilGuz.Checked = mBasvuru.MezuniyetSureci.DonemID == AkademikDonemEnum.GuzYariyili;
                xrChkYariyilBahar.Checked = mBasvuru.MezuniyetSureci.DonemID == AkademikDonemEnum.BaharYariyili;
                xrCellEnstituAdi.Text = mBasvuru.MezuniyetSureci.Enstituler.EnstituAd.ToUpper();
                xrCellAnabilimdaliProgramAdi.Text = mBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi.ToUpper() + " - " + mBasvuru.Programlar.ProgramAdi.ToUpper();
                xrCellOgrenciNo.Text = mBasvuru.OgrenciNo;
                xrCellOgrenciAdi.Text = mBasvuru.Ad + " " + mBasvuru.Soyad;

                var danismanBilgi = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TezDanismani");
                xrCellTezDanismanBilgi.Text = danismanBilgi.UnvanAdi + " " + danismanBilgi.AdSoyad;

                chkTurkce.Checked = mBasvuru.IsTezDiliTr == true;
                chkYabanci.Checked = mBasvuru.IsTezDiliTr == false;

                var tezBasligiTr = "";
                var tezBasligiEn = "";

                tezBasligiTr = joForm.IsTezBasligiDegisti == true
                    ? joForm.YeniTezBaslikTr
                    : mBasvuru.TezBaslikTr;
                tezBasligiEn = joForm.IsTezBasligiDegisti == true
                    ? joForm.YeniTezBaslikEn
                    : mBasvuru.TezBaslikEn;

                xrCellTezBaslikTr.Text = tezBasligiTr;
                xrCellTezBaslikEn.Text = tezBasligiEn;

                if (!mBasvuru.TezEsDanismanAdi.IsNullOrWhiteSpace())
                {
                    xrCellTezEsDanismanBilgi.Text = mBasvuru.TezEsDanismanUnvani + " " + mBasvuru.TezEsDanismanAdi;
                }
                #region YayinBilgi

                var yayinSartiVar = MezuniyetBus.GetMezuniyetAktifYonetmelik(mBasvuru.MezuniyetSurecID, mBasvuru.KullaniciID, mBasvuru.MezuniyetBasvurulariID).MezuniyetSureciYonetmelikleriOTs.Any(a => a.OgrenimTipKod == mBasvuru.OgrenimTipKod && a.IsZorunlu);
                chkYayinSartiVardir.Checked = yayinSartiVar;
                chkYayinSartYoktur.Checked = !yayinSartiVar;

                var yayins = (from qs in entities.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariID == mBasvuru.MezuniyetBasvurulariID)
                              join s in entities.MezuniyetSureciYayinTurleris on new { qs.MezuniyetBasvurulari.MezuniyetSurecID, qs.MezuniyetYayinTurID } equals new { s.MezuniyetSurecID, s.MezuniyetYayinTurID }
                              join sd in entities.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                              join inx in entities.MezuniyetYayinIndexTurleris on new { qs.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into definx
                              from inxD in definx.DefaultIfEmpty()
                              select new RprMezuniyetTezDegerlendirmeYayinBilgi
                              {
                                  YayinTurAdi = sd.MezuniyetYayinTurAdi,
                                  YayinBasligi = qs.YayinBasligi,
                                  IndexBilgisi = inxD != null ? inxD.IndexTurAdi : "",
                                  DoiNumarasi = qs.MezuniyetYayinLinkTurID == 5 ? qs.MezuniyetYayinKaynakLinki : "",



                              }).ToList();
                xrRowYayinBilgiBaslik.Visible = yayinSartiVar || yayins.Count > 0;
                this.DataSource = yayins;
                #endregion

                if (mezuniyetJuriOneriFormuJuriId.HasValue)
                {
                    var secilenJuri = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.MezuniyetJuriOneriFormuJuriID == mezuniyetJuriOneriFormuJuriId);
                    xrCellJuriAdSoyad.Text = secilenJuri.UnvanAdi + " " + secilenJuri.AdSoyad;
                    xrCellJuriUniversiteAdi.Text = secilenJuri.UniversiteAdi.ToUpper();
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
                this.DisplayName = (mBasvuru.Ad + " " + mBasvuru.Soyad) + " FR-0304 Tezden Üretilen Yayınları Değerlendirme Formu";

            }
        }


    }
}
