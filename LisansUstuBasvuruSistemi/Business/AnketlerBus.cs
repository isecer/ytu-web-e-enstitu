using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

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
                            IslemYapanID = 1,
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
    }
}