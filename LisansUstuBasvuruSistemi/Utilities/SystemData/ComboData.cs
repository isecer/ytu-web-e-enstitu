using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Utilities.SystemData
{
    public class ComboData
    {
        public static List<CmbBoolDto> GecCmbVeVeya(bool bosSecimVar = false)
        {
            var lst = new List<CmbBoolDto>();
            if (bosSecimVar) lst.Add(new CmbBoolDto());
            lst.Add(new CmbBoolDto { Value = true, Caption = "Ve" });
            lst.Add(new CmbBoolDto { Value = false, Caption = "Veya" });

            return lst;
        }

        public static List<CmbStringDto> GetCmbGrupKod(int grupSayisi, string grupAdi = "Grup", bool bosSecimVar = false)
        {
            var lst = new List<CmbStringDto>();
            if (bosSecimVar) lst.Add(new CmbStringDto());
            for (int i = 1; i <= grupSayisi; i++)
            {
                lst.Add(new CmbStringDto { Value = grupAdi + i.ToString(), Caption = grupAdi + i.ToString() });
            }
            return lst;
        }

        public static List<CmbIntDto> GetCmbTarihKriterSecim(bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto());
            lst.Add(new CmbIntDto { Value = TarihKriterSecimEnum.SecilenTarihVeOncesi, Caption = "Seçilen Tarih ve Öncesi" });
            lst.Add(new CmbIntDto { Value = TarihKriterSecimEnum.SecilenTarihAraligi, Caption = "Seçilen Tarih Aralığı" });
            lst.Add(new CmbIntDto { Value = TarihKriterSecimEnum.SecilenTarihVeSonrasi, Caption = "Seçilen Tarih ve Sonrası" });

            return lst;
        }

        public static List<CmbBoolDto> GetCmbAktifPasifData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Aktif" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Pasif" });
            return dct;

        }

        public static List<CmbBoolDto> GetCmbAcikKapaliData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Kapalı" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Açık" });
            return dct;

        }

        public static List<CmbBoolDto> GetCmbDosyaEkiDurumData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Dosya Eki Olanlar" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Dosya Eki Olmayanlar" });
            return dct;

        }
        public static List<CmbBoolDto> GetCmbKomiteUyeKayitDurumData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Komite Üyesi Olanlar" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Komite Üyesi Olmayanlar" });
            return dct;

        }
        public static List<CmbBoolDto> GetCmbAsilYedekDurumData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Asil" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Yedek" });
            return dct;
        }
        public static List<CmbBoolDto> GetCmbEykGonderimDurumData(bool bosSecimVar = false, DateTime? onayTarihi = null)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "EYK'ya gönderimi onaylandı" + (onayTarihi.HasValue ? " (" + onayTarihi.ToFormatDateAndTime() + ")" : "") });
            dct.Add(new CmbBoolDto { Value = false, Caption = "EYK'ya gönderimi onaylanmadı" });
            return dct;
        }

        public static List<CmbBoolDto> GetCmbEykOnayDurumData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Eyk'da Onaylandı" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Eyk'da Onaylanmadı" });
            return dct;

        }

        public static List<CmbBoolDto> GetCmbDoluBosData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Dolu" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Boş" });
            return dct;

        }

        public static List<CmbBoolDto> GetCmbVarYokData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Var" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Yok" });
            return dct;

        }

        public static List<CmbBoolDto> GetCmbEvetHayirData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Evet" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Hayir" });
            return dct;

        }

        public static List<CmbBoolDto> GetCmbGrupGosterData()
        {
            var dct = new List<CmbBoolDto>
            {
                new CmbBoolDto { Value = true, Caption = "Grup Olarak Göster" },
                new CmbBoolDto { Value = false, Caption = "Tek Olarak Göster" }
            };
            return dct;

        }

        public static List<CmbIntDto> CmbCardBonusType()
        {
            var mdl = new List<CmbIntDto>
            {
                new CmbIntDto { Value = null, Caption = "" },
                new CmbIntDto { Value = 1, Caption = "Bonus Kart Özelliği Var" },
                new CmbIntDto { Value = 0, Caption = "Bonus Kart Özelliği Yok" }
            };
            return mdl;
        }

        public static List<CmbIntDto> CmbCardMaximumType()
        {
            var mdl = new List<CmbIntDto>
            {
                new CmbIntDto { Value = null, Caption = "" },
                new CmbIntDto { Value = 1, Caption = "Var" },
                new CmbIntDto { Value = 0, Caption = "Yok" }
            };
            return mdl;
        }

        public static List<CmbIntDto> CmbTaksitList()
        {
            var mdl = new List<CmbIntDto>
            {
                new CmbIntDto { Value = null, Caption = "Taksit İstemiyorum" },
                new CmbIntDto { Value = 5, Caption = "5 Taksit" }
            };
            return mdl;
        }
    }
}