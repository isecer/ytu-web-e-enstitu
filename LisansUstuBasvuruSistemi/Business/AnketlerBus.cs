using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public class AnketlerBus
    {
        public static void AnketOlustur()
        {
            using (var entities = new LisansustuBasvuruSistemiEntities())
            {

                var anketler = entities.Ankets.ToList();
                var ensts = new List<string> { "020", "030" };
                foreach (var anket in anketler)
                {
                    foreach (var enstituItem in ensts)
                    {
                        var newAnket = new Anket
                        {
                            EnstituKod = enstituItem,
                            AnketAdi = anket.AnketAdi,
                            IsAktif = true,
                            IslemTarihi = DateTime.Now,
                            IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId,
                            IslemYapanIP = ":",
                            AnketSorus = anket.AnketSorus.ToList().Select(s => new AnketSoru
                            {
                                SiraNo = s.SiraNo,
                                SoruAdi = s.SoruAdi,
                                IsTabloVeriGirisi = s.IsTabloVeriGirisi,
                                IsTabloVeriMaxSatir = s.IsTabloVeriMaxSatir,
                                AnketSoruSeceneks = s.AnketSoruSeceneks.ToList().Select(sc => new AnketSoruSecenek
                                {
                                    SiraNo = sc.SiraNo,
                                    SecenekAdi = sc.SecenekAdi,
                                    IsYaziOrSayi = sc.IsYaziOrSayi,
                                    IsEkAciklamaGir = sc.IsEkAciklamaGir

                                }).ToList()

                            }).ToList()


                        };
                        entities.Ankets.Add(newAnket);
                    }
                }

                entities.SaveChanges();
            }
        }
        public static List<CmbIntDto> CmbGetAktifAnketler(string enstituKod, bool bosSecimVar = false, int? dahilAnketId = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Ankets.Where(p => p.EnstituKod == enstituKod && (p.IsAktif || p.AnketID == dahilAnketId)).OrderBy(o => o.AnketAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.AnketID, Caption = item.AnketAdi });
                }
            }
            return dct;

        }

    }
}