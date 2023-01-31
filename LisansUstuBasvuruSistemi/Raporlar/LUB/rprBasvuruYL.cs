using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models; 
using BiskaUtil;
using System.Linq;
using System.Collections.Generic;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Raporlar
{

    public partial class rprBasvuruYL : DevExpress.XtraReports.UI.XtraReport
    {

        public rprBasvuruYL(int id, int? id2)
        {
            InitializeComponent();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                int? KullaniciID = RoleNames.GelenBasvurular.InRoleCurrent() ? (int?)null : UserIdentity.Current.Id;
                var BasvuruID = db.BasvurularTercihleris.Where(p => p.BasvuruTercihID == id && (KullaniciID.HasValue ? p.Basvurular.KullaniciID == KullaniciID : 1 == 1)).First().BasvuruID;
                var model = Management.getSecilenBasvuruDetay(BasvuruID);
                var btercih = model.BasvuruTercihleri.Where(p => p.BasvuruTercihID == id).First();
                var UserImgUrl = model.ResimAdi.ToKullaniciResim();
                imgResim.ImageUrl = System.Web.HttpContext.Current.Server.MapPath(UserImgUrl);


                string logoPath = "/Content/assets/images/ytu_logo_tr.png";
                lblAltBilgi.Text = "20 Nisan 2016 tarihli YÖK lisansüstü eğitim ve öğretim yönetmeliği 35.maddesine göre; Tezsiz yüksek lisans programları hariç, aynı anda birden fazla lisansüstü programa kayıt yaptırılamaz ve devam edilemez.";

                rprLogo.ImageUrl = System.Web.HttpContext.Current.Server.MapPath(logoPath);

                #region setlbl
                lblUniAdi.Text = "YILDIZ TEKNİK ÜNİVERSİTESİ";
                if (btercih.OgrenimTipKod == OgrenimTipi.ButunlesikDoktora)
                    lblBasvuruTip.Text = "BÜTÜNLEŞİK DOKTORA PROGRAMLARINA BAŞVURU";
                else if (btercih.OgrenimTipKod == OgrenimTipi.SanattaYeterlilik)
                    lblBasvuruTip.Text = "SANATTA YETERLİLİK PROGRAMLARINA BAŞVURU";
                else
                    lblBasvuruTip.Text = "LİSANSÜSTÜ PROGRAMLARA BAŞVURU";

                if (model.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) lblSinavGirisFormu.Text = "SINAV GİRİŞ FORMU";
                else if (model.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru) lblSinavGirisFormu.Text = "YATAY GEÇİŞ SINAVINA GİRİŞ FORMU";
                else lblSinavGirisFormu.Text = "YTU YENİ MEZUN BAŞVURU FORMU";

                if (model.KullaniciTipID == KullaniciTipBilgi.YerliOgrenci)
                {
                    cell_TcKimlikNo.Text = model.TcKimlikNo;
                    lngLbl_TCK.Text = "T.C. Kimlik No";
                }
                else
                {
                    cell_TcKimlikNo.Text = model.PasaportNo;
                    lngLbl_TCK.Text = "Pasaport No";
                }
                lngLbl_AdSoyad.Text = "Ad Soyad";
                cell_AdiSoyadi.Text = model.Ad + " " + model.Soyad;
                lngLbl_DogumTarihi.Text = "Doğum Tarihi";
                cell_DogumTarihi.Text = model.DogumTarihi.ToDateString();

                lngLbl_BasvuruBilgileri.Text = "Başvuru Bilgisi";
                lngLbl_EnstituAdi.Text = "Enstitü Adı";
                cell_EnstituAdi.Text = model.EnstituAdi;
                lngLbl_AkademikTarih.Text = "Eğitim Öğretim Yılı";
                cell_AkademikYil.Text = model.BasvuruSurec.BaslangicYil + "-" + model.BasvuruSurec.BitisYil + " " + db.Donemlers.Where(p => p.DonemID == model.BasvuruSurec.DonemID).First().DonemAdi;
                lngLbl_BasvurulanBolum.Text = "Anabilim Dalı";
                cell_BolumAdi1.Text = btercih.ProgramBilgileri.AnabilimDaliAdi;
                lngLbl_BasvurulanProgram.Text = "Program";
                cell_ProgramAdi1.Text = btercih.ProgramBilgileri.ProgramAdi;
                lblAlanTip1.Text = btercih.SiraNo + ".Tercih " + btercih.SiraNo + " (" + btercih.AlanTipAdi + ")";

                if (id2.HasValue)
                {
                    var btercih2 = model.BasvuruTercihleri.Where(p => p.BasvuruTercihID == id2.Value).First();
                    cell_BolumAdi2.Text = btercih2.ProgramBilgileri.AnabilimDaliAdi;
                    cell_ProgramAdi2.Text = btercih2.ProgramBilgileri.ProgramAdi;
                    lblAlanTip2.Text = btercih2.SiraNo + ".Tercih " + btercih2.SiraNo + " (" + btercih2.AlanTipAdi + ")";

                }



                lngLbl_SinavTuru.Text = "Sınav Türü";

                if (model.BasvurularSinavBilgi_A.SinavDetay != null)
                {
                    if (model.BasvurularSinavBilgi_A.Sinav.SinavTarihi.HasValue) cell_SinavTarih.Text = model.BasvurularSinavBilgi_A.Sinav.SinavTarihi.Value.ToDateString();
                    if (model.BasvurularSinavBilgi_A.SinavDetay.WebService)
                    {
                        var wsxmlNot = model.BasvurularSinavBilgi_A.Sinav.WsXmlData.toSinavSonucAlesXmlModel();
                        if (btercih.ProgramBilgileri.AlesNotuYuksekOlanAlinsin && btercih.ProgramBilgileri.AnabilimDallari.EnstituKod == EnstituKodlari.SosyalBilimleri)
                        {
                            var maxNot = new Dictionary<int, double>();
                            if (btercih.ProgramBilgileri.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.Sayısal)) maxNot.Add(AlesTipBilgi.Sayısal, wsxmlNot.SAY_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                            if (btercih.ProgramBilgileri.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.Sözel)) maxNot.Add(AlesTipBilgi.Sözel, wsxmlNot.SOZ_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                            if (btercih.ProgramBilgileri.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.EşitAğırlık)) maxNot.Add(AlesTipBilgi.EşitAğırlık, wsxmlNot.EA_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                            model.BasvurularSinavBilgi_A.Sinav.SinavNotu = maxNot.Select(s => s.Value).Max();
                        }
                        else
                        {
                            if (btercih.ProgramBilgileri.AlesTipID == AlesTipBilgi.Sayısal)
                                model.BasvurularSinavBilgi_A.Sinav.SinavNotu = wsxmlNot.SAY_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                            else if (btercih.ProgramBilgileri.AlesTipID == AlesTipBilgi.Sözel)
                                model.BasvurularSinavBilgi_A.Sinav.SinavNotu = wsxmlNot.SOZ_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                            else if (btercih.ProgramBilgileri.AlesTipID == AlesTipBilgi.EşitAğırlık)
                                model.BasvurularSinavBilgi_A.Sinav.SinavNotu = wsxmlNot.EA_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                        }
                        cell_SinavTarih.Text = model.BasvurularSinavBilgi_A.Sinav.WsSinavYil.ToString() + " / " + model.BasvurularSinavBilgi_A.Sinav.WsAciklanmaTarihi.ToString("dd.MM.yyyy");

                    }
                    else
                    {
                        cell_SinavTarih.Text = model.BasvurularSinavBilgi_A.Sinav.SinavTarihi.Value.ToDateString();
                    }
                    cell_SinavTuru.Text = model.BasvurularSinavBilgi_A.SinavDetay.SinavAdi + " (" + btercih.ProgramBilgileri.AlesTipAdi + ")";
                    cell_SinavPuan.Text = model.BasvurularSinavBilgi_A.Sinav.SinavNotu.ToString();



                    if (model.BasvurularSinavBilgi_A.SinavDetay.NotDonusum || model.BasvurularSinavBilgi_A.SinavDetay.OzelNotTipID == OzelNotTip.SeciliNotAraliklari)
                    {
                        string ackl = "";
                        string notB = "";
                        if (model.BasvurularSinavBilgi_A.Sinav.SubSinavAralikID.HasValue)
                        {
                            var sclnKriter = model.BasvurularSinavBilgi_A.SinavDetay.BasvuruSurecSinavTiplerSubSinavAraliks.Where(p => p.SubSinavAralikID == model.BasvurularSinavBilgi_A.Sinav.SubSinavAralikID).First();
                            if (sclnKriter.NotDonusum)
                            {
                                ackl = " (" + sclnKriter.SubSinavAralikAdi + ")";
                                notB = model.BasvurularSinavBilgi_A.Sinav.BasvuruSurecSubNot + " (Ales Karşılığı: " + model.BasvurularSinavBilgi_A.Sinav.SinavNotu + ")";
                            }
                            else notB = model.BasvurularSinavBilgi_A.Sinav.SinavNotu.ToString();


                        }
                        else
                        {
                            if (model.BasvurularSinavBilgi_A.SinavDetay.NotDonusum)
                            {
                                notB = model.BasvurularSinavBilgi_A.Sinav.BasvuruSurecSubNot + " ( (Ales Karşılığı: " + model.BasvurularSinavBilgi_A.Sinav.SinavNotu + ")";
                            }
                            else notB = model.BasvurularSinavBilgi_A.Sinav.SinavNotu.ToString();
                        }
                        cell_SinavTuru.Text = model.BasvurularSinavBilgi_A.SinavDetay.SinavAdi + ackl;
                        cell_SinavPuan.Text = notB;
                    }
                    else
                    {
                        cell_SinavTuru.Text = model.BasvurularSinavBilgi_A.SinavDetay.SinavAdi;
                        cell_SinavPuan.Text = model.BasvurularSinavBilgi_A.Sinav.SinavNotu.ToString();
                    }
                }
                else
                {
                    cell_SinavTuru.Text = "-";
                    cell_SinavTarih.Text = "-";
                    cell_SinavPuan.Text = "-";
                }

                lngLbl_SinavPuan.Text = "Sınav Puanı";
                lngLbl_SinavTarihi.Text = "Sınav Tarihi";


                lngLbl_YDSBilgisi.Text = "Yabancı Dili (Ulusal / Uluslararası Yabancı Dil Puanı / Tarihi)";
                lngLbl_SinavTuruYD.Text = "Sınav Türü";
                lngLbl_SinavPuanYD.Text = "Sınav Puanı";
                lngLbl_SinavTarihiYD.Text = "Sınav Tarihi";

                if (model.BasvurularSinavBilgi_D.SinavDetay != null)
                {
                    var sinavDilleri = db.SinavDilleris.ToList();

                    var sdil = sinavDilleri.Where(p => p.SinavDilID == model.BasvurularSinavBilgi_D.Sinav.SinavDilID).FirstOrDefault();
                    cell_SinavTuruYD.Text = model.BasvurularSinavBilgi_D.SinavDetay.SinavAdi + (sdil != null ? " (" + sdil.DilAdi + ")" : "");
                    if (model.BasvurularSinavBilgi_D.Sinav.IsTaahhutVar == true && model.BasvurularSinavBilgi_D.Sinav.SinavNotu == 0)
                    {
                        cell_SinavPuanYD.Text = "En düşük " + model.BasvurularSinavBilgi_D.SinavDetay.MinNotAdi + " sınav notlu belge taahhüt edildi.";
                    }
                    else
                    {
                        cell_SinavPuanYD.Text = model.BasvurularSinavBilgi_D.Sinav.SinavNotu.ToString();
                    }
                    if (!model.BasvurularSinavBilgi_D.SinavDetay.WebService)
                    {
                        cell_SinavTarihYD.Text = model.BasvurularSinavBilgi_D.Sinav.SinavTarihi.Value.ToDateString();
                    }
                    else
                    {
                        if (model.BasvurularSinavBilgi_D.SinavDetay.WsSinavCekimTipID.HasValue && model.BasvurularSinavBilgi_D.SinavDetay.WsSinavCekimTipID == WsCekimTipi.Tarih)
                        {

                            cell_SinavTarihYD.Text = model.BasvurularSinavBilgi_D.Sinav.SinavTarihi.Value.Year.ToString() + " / " + model.BasvurularSinavBilgi_D.Sinav.SinavTarihi.Value.ToString("dd.MM.yyyy");
                        }
                        else
                        {
                            cell_SinavTarihYD.Text = model.BasvurularSinavBilgi_D.Sinav.WsSinavYil.ToString() + " / " + model.BasvurularSinavBilgi_D.Sinav.WsAciklanmaTarihi.ToString("dd.MM.yyyy");
                        }
                    }
                }
                if (model.BasvuruSurecTipID != BasvuruSurecTipi.YatayGecisBasvuru)
                {
                    lngLbl_EgitimBilgileri.Text = "EĞİTİM BİLGİLERİ";
                    lngLbl_LisansEgitimi.Text = "Lisans Eğitimi";
                    lngLbl_LUniversiteAdi.Text = "Üniversite";
                    cell_LisansUniversite.Text = model.LUniversiteAdi;
                    lngLbl_LFakulteAdi.Text = "Fakülte";
                    cell_LisansFakulte.Text = model.LFakulteAdi;
                    lngLbl_LBolumAdi.Text = "Bölüm";
                    cell_LisansBolum.Text = model.LBolumAdi;
                    lngLbl_LYTU_L_AgnoGirilen.Text = "AGNO";
                    cell_LAGNO_Girilen.Text = "Puan Sistemi :" + model.LNotSistemi + ", Not:" + model.LMezuniyetNotu.Value.ToString("n2");
                    lngLbl_LAGNO_YOK.Text = "AGNO (YÖK Karşılığı)";
                    cell_LAGNO_YOK.Text = model.LMezuniyetNotu.Value.ToNotCevir(model.LNotSistemID.Value).Not100Luk.ToString("n2");

                }
                else
                {
                    SubBand1.Visible = false;
                    SubBand2.Visible = true;


                    lngLbl_EgitimBilgileri1.Text = "EĞİTİM BİLGİLERİ";
                    lngLbl_LisansEgitimi1.Text = "Lisans Eğitimi";
                    lngLbl_LUniversiteAdi1.Text = "Üniversite";
                    cell_LisansUniversite1.Text = model.LUniversiteAdi;
                    lngLbl_LFakulteAdi1.Text = "Fakülte";
                    cell_LisansFakulte1.Text = model.LFakulteAdi;
                    lngLbl_LBolumAdi1.Text = "Bölüm";
                    cell_LisansBolum1.Text = model.LBolumAdi;
                    lngLbl_LYTU_L_AgnoGirilen1.Text = "AGNO";
                    cell_LAGNO_Girilen1.Text = "Puan Sistemi:" + model.LNotSistemi + ", Not:" + model.LMezuniyetNotu.Value.ToString("n2");
                    lngLbl_LAGNO_YOK1.Text = "AGNO (YÖK Karşılığı)";
                    cell_LAGNO_YOK1.Text = model.LMezuniyetNotu.Value.ToNotCevir(model.LNotSistemID.Value).Not100Luk.ToString("n2");

                    lngLbl_YLisansEgitimi.Text = "Yüksek Lisans Eğitimi";
                    lngLbl_YLUniversiteAdi.Text = "Üniversite";
                    cell_YLisansUniversite.Text = model.YLUniversiteAdi;
                    lngLbl_YLFakulteAdi.Text = "Enstitü";
                    cell_YLisansFakulte.Text = model.YLFakulteAdi;
                    lngLbl_YLBolumAdi.Text = "Bölüm";
                    cell_YLisansBolum.Text = model.YLBolumAdi;
                    lngLbl_LYTU_YL_AgnoGirilen.Text = "AGNO";
                    cell_YLAGNO_Girilen.Text = "Puan Sistemi:" + model.YLNotSistemi + ", Not:" + model.YLMezuniyetNotu.Value.ToString("n2");
                    lngLbl_YLAGNO.Text = "AGNO (YÖK Karşılığı)";
                    cell_YLAGNO.Text = model.YLMezuniyetNotu.Value.ToNotCevir(model.YLNotSistemID.Value).Not100Luk.ToString("n2");
                }

                cell_paraf.Text = "Paraf:";
                cell_paraf1.Text = "Paraf:";
                lngLbl_BasvuruOnayMsj.Text = "Bu formda beyan ettiğim bilgiler ile belgelerin uyuşmaması durumunda başvurumun iptal edilmesini kabul ediyorum";
                cell_Tarih.Text = model.BasvuruTarihi.ToDateString();
                cell_AdSoyadImza.Text = model.Ad + " " + model.Soyad;
                #endregion
            }
        }

    }
}
