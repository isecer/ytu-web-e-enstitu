using System.Collections.Generic;
using System.Linq;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class MezuniyetSureciBus
    {

        public static List<MezuniyetOtoMailDto> GetOtoMailData()
        {

            var bsMList = new List<MezuniyetOtoMailDto>
            {
                new MezuniyetOtoMailDto { OtoMailID = 1,  MailSablonTipID = MailSablonTipiEnum.MezBasvuruTaslakHalinde,  Sure = 1, Aciklama = "Başvuru süreci bitimine 1 Gün kala Taslak durumundaki başvuruları bildir (Öğrenci)" },
                new MezuniyetOtoMailDto { OtoMailID = 2,  MailSablonTipID = MailSablonTipiEnum.MezBasvuruTaslakHalinde,  Sure = 2, Aciklama = "Başvuru süreci bitimine 2 Gün kala Taslak durumundaki başvuruları bildir (Öğrenci)" },
                new MezuniyetOtoMailDto { OtoMailID = 3,  MailSablonTipID = MailSablonTipiEnum.MezEykTarihineGoreSrAlinmali,Sure = 10, Aciklama = "SR talebi yapma süreci bitimine 10 Gün kala SR talebi yapmayanları bildir (Danışman,Öğrenci)" },
                new MezuniyetOtoMailDto { OtoMailID = 4,  MailSablonTipID = MailSablonTipiEnum.MezEykTarihineGoreSrAlinmali,Sure = 5, Aciklama = "SR talebi yapma süreci bitimine 5 Gün kala SR talebi yapmayanları bildir (Danışman,Öğrenci)" },
                new MezuniyetOtoMailDto { OtoMailID = 5,  MailSablonTipID = MailSablonTipiEnum.MezEykTarihineGoreSrAlinmadi,Sure = 5, Aciklama = "SR talebi yapma sürecini 5 Gün aşanları bildir (Enstitü)" },
                new MezuniyetOtoMailDto { OtoMailID = 6,  MailSablonTipID = MailSablonTipiEnum.MezSinavDegerlendirmeHatirlantmaDanismanDr,Sure = 1, Aciklama = "DR Sınav sonucu değerlendirmesi için hatırlatma (Danışman)" },
                new MezuniyetOtoMailDto { OtoMailID = 7,  MailSablonTipID = MailSablonTipiEnum.MezSinavDegerlendirmeHatirlantmaDanismanYl,Sure = 1, Aciklama = "YL Sınav sonucu değerlendirmesi için hatırlatma (Danışman)" },
                new MezuniyetOtoMailDto { OtoMailID = 8,  MailSablonTipID = MailSablonTipiEnum.MezTezSinavSonucuSistemeGirilmedi,Sure = 5, Aciklama = "Sınav olup sonucunu 5 gün içinde getirmeyenleri bildir (Estitü,Danışman,Öğrenci)" },
                new MezuniyetOtoMailDto { OtoMailID = 9,  MailSablonTipID = MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklenmeli,Sure =7, Aciklama = "Sınav olup Tez Dosyasını 7 gün içinde yüklemeyenleri bildir (Öğrenci)" },
                new MezuniyetOtoMailDto { OtoMailID = 10,   MailSablonTipID = MailSablonTipiEnum.MezCiltliTezTeslimYapilmali,Sure = 5,Aciklama = "Tez teslim tutanağını teslim tarihine 5 gün kala teslim etmeyenleri bildir (Danışman,Öğrenci)" },
                new MezuniyetOtoMailDto { OtoMailID = 11,   MailSablonTipID = MailSablonTipiEnum.MezCiltliTezTeslimYapilmadi,Sure = 5, Aciklama = "Tez teslim tutanağını teslim tarihini 5 gün geçirenleri bildir (Enstitü)" }
            };

            return bsMList;
        }

        public static bool MezuniyetSureciOtoMailOlustur(int mezuniyetSurecId)
        {
            using (var entities = new LisansustuBasvuruSistemiEntities())
            {
                var mezuniyetSureci = entities.MezuniyetSurecis.First(f => f.MezuniyetSurecID == mezuniyetSurecId);
                var mezuniyetSureciOtoMails = mezuniyetSureci.MezuniyetSureciOtoMails.ToList();
                var otoMailData = GetOtoMailData();

                var pasifeAlinacak = mezuniyetSureciOtoMails.Where(p => otoMailData.All(a => a.OtoMailID != p.OtoMailID)).ToList();
                var surecellenecekler = otoMailData.Where(p => mezuniyetSureciOtoMails.Any(a => a.OtoMailID == p.OtoMailID)).ToList();
                var eklenecekler = otoMailData.Where(p => mezuniyetSureciOtoMails.All(a => a.OtoMailID != p.OtoMailID)).ToList();
                foreach (var item in surecellenecekler)
                {
                    var qzaman = mezuniyetSureciOtoMails.First(p => p.OtoMailID == item.OtoMailID);
                    qzaman.Sure = item.Sure;
                    qzaman.MailSablonTipID = item.MailSablonTipID;
                }
                foreach (var item in pasifeAlinacak)
                {
                    item.IsAktif = false;
                }
                foreach (var item in eklenecekler)
                {
                    mezuniyetSureci.MezuniyetSureciOtoMails.Add(new MezuniyetSureciOtoMail
                    {
                        MailSablonTipID = item.MailSablonTipID,
                        Sure = item.Sure,
                        IsAktif = true,
                    });

                }

                entities.SaveChanges();
            }

            return true;

        }
    }
}