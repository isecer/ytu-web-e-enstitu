using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Business
{
    public class AnketlerBus
    {
        public static string GetAnketView(
        int anketId,
        int anketTipId,
        int? basvuruId = null,
        int? belgeTalepId = null,
        int? mezuniyetBasvurulariId = null,
        int? tdoBasvuruID = null,
        int? toBasvuruID = null,
        int? donemProjesiID = null,
        string rowId = null)
        {
            using (var entities = new LubsDbEntities())
            {

                var anket = entities.Ankets.First(f => f.AnketID == anketId);
                // Anket sorularını temel sorgu
                var anketSorulariQuery = from aso in entities.AnketSorus
                                         where aso.AnketID == anketId
                                         select aso;

                //İlişkili cevapları (AnketCevaplaris) filtrele
                IQueryable<AnketCevaplari> cevapQuery = entities.AnketCevaplaris.Where(p => p.AnketID == anketId);

                // Anket tipine göre ilgili filtreleri uygula
                switch (anketTipId)
                {
                    case AnketTipiEnum.LisanustuBasvuruAnketi:
                    case AnketTipiEnum.MezuniyetSureciDegerlendirmeAnketi:
                    case AnketTipiEnum.KayitHakkiKazananKayitYaptirmayanAnketi:
                        if (basvuruId.HasValue)
                            cevapQuery = cevapQuery.Where(p => p.BasvuruID == basvuruId.Value);
                        if (mezuniyetBasvurulariId.HasValue)
                            cevapQuery = cevapQuery.Where(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId.Value);
                        break;

                    case AnketTipiEnum.BelgeTalepAnketi:
                        if (belgeTalepId.HasValue)
                            cevapQuery = cevapQuery.Where(p => p.BelgeTalepID == belgeTalepId.Value);
                        break;
                    case AnketTipiEnum.DanismanAtamaBasvurunAnketi:
                        if (tdoBasvuruID.HasValue)
                            cevapQuery = cevapQuery.Where(p => p.TDOBasvuruID == tdoBasvuruID.Value);
                        break;
                    case AnketTipiEnum.DoktoraTezOneriSinaviBasvuruAnketi:
                        if (toBasvuruID.HasValue)
                            cevapQuery = cevapQuery.Where(p => p.ToBasvuruID == toBasvuruID.Value);
                        break;
                    case AnketTipiEnum.TezsizYukseklisansDonemProjesiBasvuruAnketi:
                        if (donemProjesiID.HasValue)
                            cevapQuery = cevapQuery.Where(p => p.DonemProjesiID == donemProjesiID.Value);
                        break;
                    default:
                        break;
                }

                // Sorgu birleşimi
                var anketSorulari = (from aso in anketSorulariQuery
                                     join sb in cevapQuery
                                         on aso.AnketSoruID equals sb.AnketSoruID into def1
                                     from sbc in def1.DefaultIfEmpty()
                                     select new
                                     {
                                         aso.AnketSoruID,
                                         AnketSoruSecenekID = sbc != null ? sbc.AnketSoruSecenekID : (int?)null,
                                         Aciklama = sbc != null ? sbc.EkAciklama : "",
                                         aso.SiraNo,
                                         aso.SoruAdi,
                                         aso.IsTabloVeriGirisi,
                                         aso.IsTabloVeriMaxSatir,
                                         Secenekler = aso.AnketSoruSeceneks
                                             .OrderBy(o => o.SiraNo)
                                             .Select(s => new
                                             {
                                                 Value = s.AnketSoruSecenekID,
                                                 s.SiraNo,
                                                 s.IsEkAciklamaGir,
                                                 s.IsYaziOrSayi,
                                                 Caption = s.SecenekAdi
                                             }).ToList()
                                     }).OrderBy(o => o.SiraNo).ToList();

                // 🧩 View Model
                var model = new KmAnketlerCevap
                {
                    AnketTipID = anketTipId,
                    RowID = rowId,
                    AnketID = anketId,
                    AnketAdi = anket.AnketAdi,
                    JsonStringData = anketSorulari.ToJson()
                };

                foreach (var item in anketSorulari)
                {
                    model.AnketCevapModel.Add(new AnketCevapDto
                    {
                        SecilenAnketSoruSecenekID = item.AnketSoruSecenekID,
                        SoruBilgi = new FrAnketDetayDto
                        {
                            AnketSoruID = item.AnketSoruID,
                            SoruAdi = item.SoruAdi,
                            SiraNo = item.SiraNo,
                            Aciklama = item.Aciklama,
                            IsTabloVeriGirisi = item.IsTabloVeriGirisi,
                            IsTabloVeriMaxSatir = item.IsTabloVeriMaxSatir
                        },
                        SoruSecenek = item.Secenekler.Select(s => new FrAnketSecenekDetayDto
                        {
                            AnketSoruSecenekID = s.Value,
                            SiraNo = s.SiraNo,
                            IsEkAciklamaGir = s.IsEkAciklamaGir,
                            IsYaziOrSayi = s.IsYaziOrSayi,
                            SecenekAdi = s.Caption
                        }).ToList(),
                        SelectListSoruSecenek = new SelectList(item.Secenekler, "Value", "Caption", item.AnketSoruSecenekID)
                    });
                }

                // 🎨 Partial View render et
                return ViewRenderHelper.RenderPartialView("Ajax", "GetAnket", model);
            }
        }


        public static MmMessage SetAnket(KmAnketlerCevap kModel)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Error,
                IsSuccess = false,
                Title = "Anket bilgisi oluşturulamadı. Lütfen aşağıdaki uyarıları inceleyiniz."
            };
            var qAnketSoruId = kModel.AnketSoruID.Select((s, inx) => new { AnketSoruID = s, inx }).ToList();
            var qAnketSoruSecenekId = kModel.AnketSoruSecenekID.Select((s, inx) => new { AnketSoruSecenekID = s, inx }).ToList();
            var qAnketSoruTabloVeri1 = kModel.TabloVeri1.Select((s, inx) => new { TabloVeri1 = s, inx }).ToList();
            var qAnketSoruTabloVeri2 = kModel.TabloVeri2.Select((s, inx) => new { TabloVeri2 = s, inx }).ToList();
            var qAnketSoruTabloVeri3 = kModel.TabloVeri3.Select((s, inx) => new { TabloVeri3 = s, inx }).ToList();
            var qAnketSoruTabloVeri4 = kModel.TabloVeri4.Select((s, inx) => new { TabloVeri4 = s, inx }).ToList();
            var qAnketSoruTabloVeri5 = kModel.TabloVeri5.Select((s, inx) => new { TabloVeri5 = s, inx }).ToList();
            var qAnketSoruSecenekAciklama = kModel.AnketSoruSecenekAciklama.Select((s, inx) => new { AnketSoruSecenekAciklama = s, inx }).ToList();

            using (var entities = new LubsDbEntities())
            {


                #region grupla

                var qGroup = (from s in qAnketSoruId
                              join ss in qAnketSoruSecenekId on s.inx equals ss.inx
                              join ac in qAnketSoruSecenekAciklama on s.inx equals ac.inx
                              join bss in entities.AnketSorus on new { s.AnketSoruID, kModel.AnketID } equals new
                              { bss.AnketSoruID, bss.AnketID }
                              join bssx in entities.AnketSoruSeceneks on new { bss.AnketSoruID, ss.AnketSoruSecenekID } equals
                                  new { bssx.AnketSoruID, AnketSoruSecenekID = (int?)bssx.AnketSoruSecenekID } into def1
                              from qs in def1.DefaultIfEmpty()
                              join v1 in qAnketSoruTabloVeri1 on s.inx equals v1.inx
                              join v2 in qAnketSoruTabloVeri2 on s.inx equals v2.inx
                              join v3 in qAnketSoruTabloVeri3 on s.inx equals v3.inx
                              join v4 in qAnketSoruTabloVeri4 on s.inx equals v4.inx
                              join v5 in qAnketSoruTabloVeri5 on s.inx equals v5.inx
                              group new
                              {
                                  ss.AnketSoruSecenekID,
                                  ac.AnketSoruSecenekAciklama,
                                  bss.IsTabloVeriGirisi,
                                  SoruCevabiYanlis = qs == null,
                                  IsEkAciklamaGir = qs?.IsEkAciklamaGir ?? false,
                                  v1.TabloVeri1,
                                  v2.TabloVeri2,
                                  v3.TabloVeri3,
                                  v4.TabloVeri4,
                                  v5.TabloVeri5,
                              }
                                  by new
                                  {
                                      bss.SiraNo,
                                      s.AnketSoruID,
                                      bss.AnketID,
                                      SecenekCount = bss.AnketSoruSeceneks.Count,
                                      bss.IsTabloVeriGirisi,
                                      bss.IsTabloVeriMaxSatir,
                                  }
                    into g1
                              select new AnketPostGroupModel
                              {
                                  inx = g1.Key.SiraNo,
                                  AnketID = g1.Key.AnketID,
                                  AnketSoruID = g1.Key.AnketSoruID,
                                  IsTabloVeriGirisi = g1.Key.IsTabloVeriGirisi,
                                  IsTabloVeriMaxSatir = g1.Key.IsTabloVeriMaxSatir,
                                  AnketSoruSecenekID = g1.Select(s => s.AnketSoruSecenekID).FirstOrDefault(),
                                  SecenekCount = g1.Key.SecenekCount,
                                  AnketSoruSecenekAciklama = g1.Select(s => s.AnketSoruSecenekAciklama).FirstOrDefault(),
                                  SoruCevabiYanlis = g1.Select(s => s.SoruCevabiYanlis).FirstOrDefault(),
                                  IsEkAciklamaGir = g1.Select(s => s.IsEkAciklamaGir).FirstOrDefault(),
                                  TabloVerileri = g1.Key.IsTabloVeriGirisi
                                      ? g1.Where(p => p.IsTabloVeriGirisi).Select(s => new AnketTabloVeriGirisModel
                                      {
                                          TabloVeri1 = s.TabloVeri1,
                                          TabloVeri2 = s.TabloVeri2,
                                          TabloVeri3 = s.TabloVeri3,
                                          TabloVeri4 = s.TabloVeri4,
                                          TabloVeri5 = s.TabloVeri5,
                                      }).ToList()
                                      : new List<AnketTabloVeriGirisModel>(),
                              }).OrderBy(o => o.inx).ToList();

                #endregion



                var hatalilar = new List<int>();




                if (qGroup.Any(p => !p.IsTabloVeriGirisi && p.AnketSoruSecenekID.HasValue == false))
                {
                    var data = qGroup.Where(p => !p.IsTabloVeriGirisi && p.AnketSoruSecenekID.HasValue == false)
                        .ToList();
                    mMessage.Messages.Add("Lütfen cevaplamadığınız anket sorularını cevaplayınız.");


                    foreach (var item in data)
                    {
                        mMessage.Messages.Add(item.inx + " Numaralı soru cevaplanmadı.");
                        mMessage.MessagesDialog.Add(new MrMessage
                        {
                            MessageType = MsgTypeEnum.Warning,
                            PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID
                        });
                        hatalilar.Add(item.AnketSoruID);
                    }
                }
                else if (qGroup.Any(p => !p.IsTabloVeriGirisi && p.AnketSoruSecenekID.HasValue && p.SoruCevabiYanlis))
                {
                    var data = qGroup.Where(p =>
                        !p.IsTabloVeriGirisi && p.AnketSoruSecenekID.HasValue == false && p.SoruCevabiYanlis).ToList();
                    mMessage.Messages.Add("Anket sorularına verdiğiniz cevaplardan bazıları sistemde bulunamadı!");
                    foreach (var item in data)
                    {

                        mMessage.Messages.Add(item.inx + " Numaralı sorunun cevabı hatalı");
                        mMessage.MessagesDialog.Add(new MrMessage
                        {
                            MessageType = MsgTypeEnum.Warning,
                            PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID
                        });
                        hatalilar.Add(item.AnketSoruID);
                    }
                }
                else if (qGroup.Any(p =>
                             !p.IsTabloVeriGirisi && p.IsEkAciklamaGir &&
                             p.AnketSoruSecenekAciklama.IsNullOrWhiteSpace()))
                {
                    var data = qGroup
                        .Where(p => !p.IsTabloVeriGirisi && p.IsEkAciklamaGir &&
                                    p.AnketSoruSecenekAciklama.IsNullOrWhiteSpace()).OrderBy(o => o.inx).ToList();
                    foreach (var item in data)
                    {

                        mMessage.Messages.Add(item.inx + " Numaralı soru için lütfen açıklama giriniz.");
                        mMessage.MessagesDialog.Add(new MrMessage
                        {
                            MessageType = MsgTypeEnum.Warning,
                            PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID
                        });
                        hatalilar.Add(item.AnketSoruID);
                    }
                }

                if (qGroup.Any(p => p.IsTabloVeriGirisi))
                {
                    var data = qGroup.Where(p => p.IsTabloVeriGirisi).ToList();

                    foreach (var item in data)
                    {
                        foreach (var item2 in item.TabloVerileri)
                        {
                            var dctVal = new Dictionary<string, string>();
                            foreach (var item3 in item2.GetType().GetProperties())
                            {
                                dctVal.Add(item3.Name, item3.GetValue(item2).ToStrObjEmptString());
                            }

                            if (dctVal.Take(item.SecenekCount).Any(p => !p.Value.IsNullOrWhiteSpace()) &&
                                dctVal.Take(item.SecenekCount).Any(p => p.Value.IsNullOrWhiteSpace()))
                            {
                                mMessage.Messages.Add(item.inx +
                                                      " Numaralı soru içindeki tüm başlıkları cevaplayınız.");
                                mMessage.MessagesDialog.Add(new MrMessage
                                {
                                    MessageType = MsgTypeEnum.Warning,
                                    PropertyName = "AnketSoruSecenekID_" + item.AnketSoruID
                                });
                                hatalilar.Add(item.AnketSoruID);
                            }

                            if (dctVal.Take(item.SecenekCount).Count(p => !p.Value.IsNullOrWhiteSpace()) ==
                                item.SecenekCount)
                            {
                                item2.InsertTablerRow = true;
                            }
                        }

                    }
                }

                mMessage.Messages = mMessage.Messages.ToList();
                if (mMessage.Messages.Count == 0)
                {


                    var lstData = new List<AnketCevaplari>();
                    foreach (var item in qGroup)
                    {
                        if (item.IsTabloVeriGirisi)
                        {
                            foreach (var item2 in item.TabloVerileri.Where(p => p.InsertTablerRow))
                            {
                                lstData.Add(new AnketCevaplari
                                {
                                    Tarih = DateTime.Now,
                                    AnketID = item.AnketID,
                                    AnketSoruID = item.AnketSoruID,
                                    AnketSoruSecenekID = null,
                                    EkAciklama = "",
                                    TabloVeri1 = item2.TabloVeri1,
                                    TabloVeri2 = item2.TabloVeri2,
                                    TabloVeri3 = item2.TabloVeri3,
                                    TabloVeri4 = item2.TabloVeri4,
                                    TabloVeri5 = item2.TabloVeri5,
                                });
                            }

                        }
                        else
                        {
                            lstData.Add(new AnketCevaplari
                            {
                                Tarih = DateTime.Now,
                                AnketID = item.AnketID,
                                AnketSoruID = item.AnketSoruID,
                                AnketSoruSecenekID = item.AnketSoruSecenekID,
                                EkAciklama = (item.IsEkAciklamaGir ? item.AnketSoruSecenekAciklama.Trim() : "")
                            });
                        }
                    }

                    if (kModel.AnketTipID == AnketTipiEnum.LisanustuBasvuruAnketi)
                    {
                        if (UserIdentity.Current.Informations.All(p => p.Key != "LUBAnket"))
                            UserIdentity.Current.Informations.Add("LUBAnket", lstData);
                        else UserIdentity.Current.Informations["LUBAnket"] = lstData;
                    }
                    else if (kModel.AnketTipID == AnketTipiEnum.BelgeTalepAnketi)
                    {
                        if (UserIdentity.Current.Informations.All(p => p.Key != "BTAnket"))
                            UserIdentity.Current.Informations.Add("BTAnket", lstData);
                        else UserIdentity.Current.Informations["BTAnket"] = lstData;
                    }
                    else if (kModel.AnketTipID == AnketTipiEnum.KayitHakkiKazananKayitYaptirmayanAnketi)
                    {
                        var nRwId = new Guid(kModel.RowID);
                        var basvuru = entities.Basvurulars.FirstOrDefault(p => p.RowID == nRwId);
                        if (basvuru != null && basvuru.BasvuruSurec.KayitOlmayanlarAnketID.HasValue &&
                            basvuru.AnketCevaplaris.All(p => p.AnketID != basvuru.BasvuruSurec.KayitOlmayanlarAnketID))
                        {
                            foreach (var item in lstData)
                            {
                                item.Tarih = DateTime.Now;
                                basvuru.AnketCevaplaris.Add(item);
                            }

                            entities.SaveChanges();
                        }
                    }
                    else if (kModel.AnketTipID == AnketTipiEnum.MezuniyetSureciDegerlendirmeAnketi)
                    {
                        var nRwId = new Guid(kModel.RowID);
                        var basvuru = entities.MezuniyetBasvurularis.FirstOrDefault(p => p.RowID == nRwId);
                        if (basvuru != null && basvuru.MezuniyetSureci.AnketID.HasValue &&
                            basvuru.AnketCevaplaris.All(p => p.AnketID != basvuru.MezuniyetSureci.AnketID))
                        {
                            foreach (var item in lstData)
                            {
                                item.Tarih = DateTime.Now;
                                basvuru.AnketCevaplaris.Add(item);
                            }

                            entities.SaveChanges();
                        }
                    }
                    else if (kModel.AnketTipID == AnketTipiEnum.DanismanAtamaBasvurunAnketi)
                    {
                        var nRwId = new Guid(kModel.RowID);
                        var basvuru = entities.TDOBasvurus.FirstOrDefault(p => p.UniqueID == nRwId);
                        var anketId = TdoAyar.IlkDanismanOnerisindeIstenenAnket.GetAyar(basvuru.EnstituKod, "").ToInt();
                         
                        if (anketId > 0 && basvuru.AnketCevaplaris.All(p => p.AnketID != anketId))
                        {
                            foreach (var item in lstData)
                            {
                                item.Tarih = DateTime.Now;
                                basvuru.AnketCevaplaris.Add(item);
                            }

                            entities.SaveChanges();
                        }
                    }
                    else if (kModel.AnketTipID == AnketTipiEnum.DoktoraTezOneriSinaviBasvuruAnketi)
                    {
                        var nRwId = new Guid(kModel.RowID);
                        var basvuru = entities.ToBasvurus.FirstOrDefault(p => p.UniqueID == nRwId);
                        var anketId = TiAyar.TezOneriIlkBasvuruAnketi.GetAyar(basvuru.EnstituKod, "").ToInt();

                        
                        if (anketId > 0 && basvuru.AnketCevaplaris.All(p => p.AnketID != anketId))
                        {
                            foreach (var item in lstData)
                            {
                                item.Tarih = DateTime.Now;
                                basvuru.AnketCevaplaris.Add(item);
                            }

                            entities.SaveChanges();
                        }
                    }
                    else if (kModel.AnketTipID == AnketTipiEnum.TezsizYukseklisansDonemProjesiBasvuruAnketi)
                    {

                        var nRwId = new Guid(kModel.RowID);
                        var basvuru = entities.DonemProjesis.FirstOrDefault(p => p.UniqueID == nRwId);
                        var anketId = DonemProjesiAyar.DonemProjesiIlkBasvuruAnketi.GetAyar(basvuru.EnstituKod, "").ToInt();
                        if (anketId.HasValue && basvuru.AnketCevaplaris.All(p => p.AnketID != anketId))
                        {
                            foreach (var item in lstData)
                            {
                                item.Tarih = DateTime.Now;
                                basvuru.AnketCevaplaris.Add(item);
                            }

                            entities.SaveChanges();
                        }
                    }
                    mMessage.IsSuccess = true;
                    mMessage.MessageType = MsgTypeEnum.Success;
                    //mMessage.Title = "Anket bilgileri doldurduğunuz için teşekkür ederiz.";

                }

                var hatasizlar = qGroup.Where(p => hatalilar.Contains(p.AnketSoruID) == false)
                    .Select(s => s.AnketSoruID).ToList();
                foreach (var item in hatasizlar)
                {
                    mMessage.MessagesDialog.Add(new MrMessage
                    { MessageType = MsgTypeEnum.Success, PropertyName = "AnketSoruSecenekID_" + item });

                }
            }

            return mMessage;

        }
        public static List<CmbIntDto> CmbGetAktifAnketler(string enstituKod, bool bosSecimVar = false, int? dahilAnketId = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.Ankets.Where(p => p.EnstituKod == enstituKod && (p.IsAktif || p.AnketID == dahilAnketId)).OrderBy(o => o.AnketAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.AnketID, Caption = item.AnketAdi });
                }
            }
            return dct;

        }

    }
}