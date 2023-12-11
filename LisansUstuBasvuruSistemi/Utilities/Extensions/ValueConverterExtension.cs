using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using Newtonsoft.Json;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class ValueConverterExtension
    {

        public static string ToStrObj(this object obj)
        {
            return obj != null ? Convert.ToString(obj) : null;
        }

        public static string ToStrObjEmptString(this object obj)
        {
            if (obj == null) return "";
            var str = Convert.ToString(obj);
            return str.Trim();
        }
        public static string ToFormatWithPrecision(this decimal number, int precision = 2)
        {
            return number % 1 == 0 ? number.ToString($"N0") : number.ToString($"N{precision}");
        }
        public static string ToFormatWithPrecision(this decimal? number, int precision = 2)
        {
            return !number.HasValue ? string.Empty : number.Value.ToFormatWithPrecision(precision);
        }
        public static decimal? ToMoney(this string moneyString)
        {
            var groupSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyGroupSeparator;
            var decimalSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator;
            return ToMoney(moneyString, decimalSeparator, groupSeparator);
        }
        public static decimal? ToMoney(this string moneyString, string decimalSeparator, string groupSeparator)
        {
            char[] numbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            var moneyStr = string.Join("",
                moneyString
                    .ToCharArray()
                    .Where(p => (p.ToString() == groupSeparator || p.ToString() == decimalSeparator || numbers.Contains(p))).ToArray()
            );
            decimal def = 0;
            if (decimal.TryParse(moneyStr, out def)) return def;
            return null;
        }
        public static decimal? ToDecimalObj(this object obj)
        {
            if (obj != null && obj.IsNumber()) return Convert.ToDecimal(obj);
            return null;
        }
        public static bool? ToBoolean(this string @string)
        {

            if (string.IsNullOrEmpty(@string)) return null;

            if (@string.ToUpper() == true.ToString().ToUpper() ||
                @string == "1" ||
                @string.ToLower() == "evet" ||
                @string.ToLower() == "var" ||
                @string.ToLower() == "on")
                return true;
            if (@string.ToUpper() == false.ToString().ToUpper() ||
                @string == "0" ||
                @string.ToLower() == "hayır" ||
                @string.ToLower() == "yok" ||
                @string.ToLower() == "off")
                return false;
            return null;
        }
        public static bool ToBoolean(this string @string, bool defaultValue)
        {
            var ok = defaultValue;
            if (string.IsNullOrEmpty(@string)) return ok;

            if (@string.ToUpper() == true.ToString().ToUpper() ||
                @string == "1" ||
                @string.ToLower() == "evet" ||
                @string.ToLower() == "var" ||
                @string.ToLower() == "on")
                ok = true;
            if (@string.ToUpper() == false.ToString().ToUpper() ||
                @string == "0" ||
                @string.ToLower() == "hayır" ||
                @string.ToLower() == "yok" ||
                @string.ToLower() == "off")
                ok = false;
            return ok;
        }
        public static bool? ToIntToBooleanObj(this object obj)
        {
            var intValue = obj.ToIntObj();
            if (obj != null && intValue.HasValue)
            {
                switch (intValue)
                {
                    case 1:
                        return true;
                    case 0:
                        return false;
                    default:
                        return (bool?)null;
                }
            }

            return (bool?)null;
        }
        public static bool? ToBooleanObj(this object obj)
        {
            bool dgr;
            if (obj != null && bool.TryParse(obj.ToString(), out dgr)) return Convert.ToBoolean(obj);
            return (bool?)null;
        }
        public static double? ToDoubleObj(this object obj)
        {
            if (obj != null && obj.IsNumber()) return Convert.ToDouble(obj);
            return (double?)null;
        }
        public static int? ToIntObj(this object obj)
        {
            if (obj != null && (obj.IsNumber())) return Convert.ToInt32(obj);
            return (int?)null;
        }
        public static Guid? ToGuidObj(this object obj)
        {
            if (obj == null)
            {
                return null;
            }
            string guidString = obj.ToString();
            Guid result;
            if (Guid.TryParse(guidString, out result))
            {
                return result;
            }
            return null;
        }
        public static int ToInt(this string @string, int @default)
        {
            var x = ToInt(@string);
            x = x ?? @default;
            return x.Value;
        }
        public static int? ToInt(this string @string)
        {
            int i = 0;
            if (int.TryParse(@string, out i))
                return i;
            return null;
        }
        public static int ToEmptyStringToZero(this object obj)
        {
            int retval = 0;
            if (obj != null && obj.ToString().Trim() != "") retval = obj.ToString().ToInt().Value;
            return retval;
        }
        public static int? ToNullIntZero(this object obj)
        {
            int? retval = null;
            if (obj != null && obj.ToString() != "0") retval = obj.ToString().ToInt();
            return retval;
        }
        public static string ToEmptyStringZero(this object obj)
        {
            string retval = "";
            if (obj != null && obj.ToString() != "0") retval = obj.ToString();
            return retval;
        }

        #region Datetime Convert
        public static string ToFormatTime(this TimeSpan? time)
        {
            time = time ?? TimeSpan.MinValue; 
            return time.ToFormatTime();
        }
        public static string ToFormatTime(this TimeSpan time)
        {
            return time == TimeSpan.MinValue ? "" : $"{time:hh\\:mm}";
        }
        public static string ToFormatDate(this DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "";
            return dateTime == DateTime.MinValue ? "" : dateTime.Value.ToFormatDate();
        }
        public static string ToFormatDate(this DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? "" : dateTime.ToString("dd.MM.yyyy");
        }
        public static string ToFormatDateAndTime(this DateTime? datetime)
        {
            if (!datetime.HasValue || datetime == DateTime.MinValue) return "";
            return datetime.Value.ToString("dd.MM.yyyy HH:mm");
        }
        public static string ToFormatDateAndTime(this DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? "" : dateTime.ToString("dd.MM.yyyy HH:mm");
        }

        public static string ToFormatDateDayTime(this DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? "" : dateTime.ToString("dd.MM.yyyy dddd HH.mm", new CultureInfo("tr-TR"));
        }
        public static string ToFormatDateDayTime(this DateTime? dateTime)
        {
            return dateTime == null || dateTime == DateTime.MinValue ? "" : dateTime.Value.ToString("dd.MM.yyyy dddd HH.mm", new CultureInfo("tr-TR"));
        }
        public static string ToFormatDateInput(this DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "";
            return dateTime == DateTime.MinValue ? "" : dateTime.Value.ToFormatDateInput();
        }
        public static string ToFormatDateInput(this DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? "" : dateTime.ToString("yyyy-MM-dd");
        }
        public static string ToFormatDateAndTimeInput(this DateTime? datetime)
        {
            if (!datetime.HasValue || datetime == DateTime.MinValue) return "";
            return datetime.Value.ToString("yyyy-MM-dd HH:mm");
        }
        public static string ToFormatDateAndTimeInput(this DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? "" : dateTime.ToString("yyyy-MM-dd HH:mm");
        }
        public static string ToFormatDateAndTimeInput2(this DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? "" : dateTime.ToString("dd-MM-yyyy HH:mm");
        }
        public static string ToFormatTime(this DateTime? datetime)
        {
            if (!datetime.HasValue) return "";
            return datetime == DateTime.MinValue ? "" : datetime.Value.ToString("HH.mm");
        }
        public static string ToFormatTime(this DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? "" : dateTime.ToString("HH.mm");
        }
        public static DateTime TodateToShortDate(this DateTime tarih)
        {
            var data1 = tarih.ToFormatDate().ToDate().Value;
            return data1;
        }
        public static DateTime? TodateToShortDate(this DateTime? tarih)
        {
            if (tarih != null) return tarih.ToFormatDate().ToDate().Value;
            else return null;
        }
        public static DateTime? ToDate(this string @string)
        {
            DateTime? result = null;
            DateTime tarih = DateTime.Today;
            if (string.IsNullOrWhiteSpace(@string) == false && DateTime.TryParse(@string, out tarih))
            {
                //result=tarih.Date;
                result = new DateTime(tarih.Year, tarih.Month, tarih.Day);
            }
            return result;
        }
        public static DateTime ToDate(this string @string, DateTime defaultDate)
        {
            var tarih = ToDate(@string);
            if (tarih.HasValue) return tarih.Value;
            return defaultDate;
        }
        #endregion
        public static bool IsNullOrEmpty(this string @string)
        {
            return string.IsNullOrEmpty(@string);
        }
        public static bool IsNullOrWhiteSpace(this string @string)
        {
            return string.IsNullOrWhiteSpace(@string);
        }
        public static string ToAsilYedek(this bool? durum)
        {
            string cins;
            if (!durum.HasValue) cins = "-";
            else if (durum.Value) cins = "Asil";
            else cins = "Yedek";
            return cins;
        }
        public static string ToTiDegerlendirmeSonucu(bool? isOyBirligiOrCoklugu, bool? isBasariliOrBasarisiz)
        {
            var returnSonuc = "";

            if (isOyBirligiOrCoklugu.HasValue && isBasariliOrBasarisiz.HasValue)
            {
                returnSonuc += isOyBirligiOrCoklugu.Value ? "Oy Birliği ile" : "Oy Çokluğu ile";
                returnSonuc += isBasariliOrBasarisiz.Value ? " Başarılı" : " Başarısız";

            }
            return returnSonuc;
        }


        public static string ToKullaniciResim(this string resimAdi)
        {
            var rsm = resimAdi.IsNullOrWhiteSpace() ? ("/" + SistemAyar.KullaniciDefaultResim) : ("/" + SistemAyar.KullaniciResimYolu + "/" + resimAdi);
            return rsm;
        }

        public static DateTime ToGetBitisTarihi(this DateTime baslangicTarihi, int ay)
        {
            // İki tarih arasındaki toplam ay süresini hesaplayın
            var bitisTarihi = baslangicTarihi.AddMonths(ay);


            return bitisTarihi;

        }
        public static bool InRole(this string userRole)
        {
            if (HttpContext.Current != null && HttpContext.Current.User != null)
            {
                bool hasRole = HttpContext.Current.User.IsInRole(userRole);
                if (!hasRole && UserIdentity.Current != null)
                    hasRole = UserIdentity.Current.Roles.Contains(userRole);
                return hasRole;
            }
            return false;
        }
        public static bool IsImage(this string filePath)
        {
            if (filePath.IsNullOrWhiteSpace()) return false;
            var ext = filePath.Split('.').Last().ToLower();
            var exts = new string[] { "jpg", "jpeg", "gif", "png", "bmp", "tiff" };
            return exts.Contains(ext);
        }
        public static bool IsPdfFile(this string filePath)
        {
            if (filePath.IsNullOrWhiteSpace()) return false;
            var ext = filePath.Split('.').Last().ToLower();
            var exts = new string[] { "pdf" };
            return exts.Contains(ext);
        }
        public static bool IsDocFile(this string filePath)
        {
            if (filePath.IsNullOrWhiteSpace()) return false;
            var ext = filePath.Split('.').Last().ToLower();
            var exts = new string[] { "doc" };
            return exts.Contains(ext);
        }
        public static bool IsDocXFile(this string filePath)
        {
            if (filePath.IsNullOrWhiteSpace()) return false;
            var ext = filePath.Split('.').Last().ToLower();
            var exts = new string[] { "docx" };
            return exts.Contains(ext);
        }
        public static bool IsXlsFile(this string filePath)
        {
            if (filePath.IsNullOrWhiteSpace()) return false;
            var ext = filePath.Split('.').Last().ToLower();
            var exts = new string[] { "xls" };
            return exts.Contains(ext);
        }
        public static bool IsXlsXFile(this string filePath)
        {
            if (filePath.IsNullOrWhiteSpace()) return false;
            var ext = filePath.Split('.').Last().ToLower();
            var exts = new string[] { "xlsx" };
            return exts.Contains(ext);
        }
        public static MvcHtmlString ToChecked(this bool? attrChecked)
        {
            return new MvcHtmlString(attrChecked.HasValue && attrChecked.Value ? "checked='checked'" : "");
        }
        public static MvcHtmlString ToChecked(this bool attrChecked)
        {
            return new MvcHtmlString(attrChecked ? "checked='checked'" : "");
        }
        public static double? ToDouble(this string @string)
        {
            double dbl = 0;
            if (double.TryParse(@string, out dbl))
                return dbl;
            return null;
        }
        public static MvcHtmlString ToSelected(this bool? attSelected)
        {
            return new MvcHtmlString(attSelected.HasValue && attSelected.Value ? "selected='selected'" : "");
        }
        public static MvcHtmlString ToSelected(this bool attSelected)
        {
            return new MvcHtmlString(attSelected ? "selected='selected'" : "");
        }
        public static double ToDouble(this string @string, double defaultValue)
        {
            double dbl = 0;
            if (double.TryParse(@string, out dbl))
                return dbl;
            return defaultValue;
        }
        public static MvcHtmlString ToEvetHayir(this bool Bool)
        {
            return new MvcHtmlString((Bool ? "Evet" : "Hayır"));
        }
        public static MvcHtmlString ToEvetHayir(this bool? Bool)
        {
            return new MvcHtmlString(Bool == null ? "" : (Bool.Value ? "Evet" : "Hayır"));
        }
        public static MvcHtmlString ToAktifPasif(this bool @bool)
        {
            return new MvcHtmlString(@bool ? "Aktif" : "Pasif");
        }
        public static MvcHtmlString ToAktifPasif(this bool? @bool)
        {
            return new MvcHtmlString(@bool == null ? "" : (@bool.Value ? "Aktif" : "Pasif"));
        }
        public static string ComputeHash(this string sifre, string tuz = null)
        {
            SHA256Managed sha = new SHA256Managed();
            //var tuz = "M@V0R£";
            //sifre = sifre+ tuz;
            if (tuz != null)
                sifre += tuz;
            byte[] sifreBytes = Encoding.UTF8.GetBytes(sifre);
            byte[] ozetBytes = sha.ComputeHash(sifreBytes);
            string hesaplananOzetSifre = Convert.ToBase64String(ozetBytes);
            return hesaplananOzetSifre;
        }
        public static string _ToOrderedListPadChar = string.Concat(((char)160).ToString(), ((char)160).ToString(), ((char)160).ToString());
        //public static string _ToOrderedListPadChar = "---";
        public static T[] ToOrderedList<T>(this IEnumerable<T> ObjectList, string RootPropertyField, string ParentPropertyField, string TextPropertyField)
        {
            //string padStr = ((char)160).ToString();
            //padStr = padStr + padStr + padStr;
            string padStr = _ToOrderedListPadChar;
            return ObjectList.ToOrderedList(RootPropertyField, ParentPropertyField, TextPropertyField, padStr);
        }
        public static T[] ToOrderedList<T>(this IEnumerable<T> ObjectList, string RootPropertyField, string ParentPropertyField, string TextPropertyField, string PadString)
        {
            List<T> resultList = new List<T>();
            if (ObjectList == null) return resultList.ToArray();
            //var LstObject = (object[])ObjectList;            
            var LstObject = ObjectList;
            if (LstObject.Count() == 0) return resultList.ToArray();
            var Lst = LstObject.AsQueryable();

            var type = Lst.First().GetType();
            IEnumerable<string> ids = new string[] { };
            try
            {
                ids = Lst.Select(s => type.GetProperty(RootPropertyField).GetValue(s, null).ToString()).ToArray();
            }
            catch
            {
                return resultList.ToArray();
            }
            List<T> roots = new List<T>();
            foreach (var l in Lst)
            {
                if (type.GetProperty(ParentPropertyField).GetValue(l, null) == null) roots.Add(l);
                else
                {
                    var bid = type.GetProperty(ParentPropertyField).GetValue(l, null).ToString();
                    if (ids.Contains(bid) == false) roots.Add(l);
                }
            }

            int deep = 0;
            Func<T, int> fxDetail = null;
            fxDetail = new Func<T, int>
                (
                   (parent) =>
                   {
                       deep++;
                       object parentid = type.GetProperty(RootPropertyField).GetValue(parent, null);
                       var details = Lst.Where(p =>
                           type.GetProperty(ParentPropertyField).GetValue(p, null) != null && //it is root
                           type.GetProperty(ParentPropertyField).GetValue(p, null).ToString() == parentid.ToString())
                           .AsEnumerable();
                       foreach (var m in details)
                       {
                           if (string.IsNullOrEmpty(TextPropertyField) == false)
                           {
                               var val = type.GetProperty(TextPropertyField).GetValue(m, null);
                               if (val != null)
                               {
                                   var str = val.ToString();
                                   if (str.StartsWith(PadString) == false)
                                       for (int i = 0; i < deep; i++)
                                           str = PadString + str;
                                   type.GetProperty(TextPropertyField).SetValue(m, str, null);
                               }
                           }
                           resultList.Add(m);
                           fxDetail(m);
                       }
                       deep--;
                       return 0;
                   }
                );

            foreach (var root in roots)
            {
                resultList.Add(root);
                fxDetail(root);
            }
            return resultList.ToArray();

        }

        public static T[] ToOrderedList<T>(this T[] ObjectList, string RootPropertyField, string ParentPropertyField, string TextPropertyField, string PadString, string SetHasChildField)
        {
            List<T> resultList = new List<T>();
            if (ObjectList == null) return resultList.ToArray();
            //var LstObject = (object[])ObjectList;            
            var LstObject = ObjectList;
            if (LstObject.Length == 0) return resultList.ToArray();
            var Lst = LstObject.AsQueryable();

            var type = Lst.First().GetType();
            var ids = Lst.Select(s => s.GetType().GetProperty(RootPropertyField).GetValue(s, null).ToString()).ToArray();

            List<T> roots = new List<T>();
            foreach (var l in Lst)
            {
                if (type.GetProperty(ParentPropertyField).GetValue(l, null) == null) roots.Add(l);
                else
                {
                    var bid = type.GetProperty(ParentPropertyField).GetValue(l, null).ToString();
                    if (ids.Contains(bid) == false) roots.Add(l);
                }
            }

            int deep = 0;
            Func<T, int> fxDetail = null;
            fxDetail = new Func<T, int>
                (
                   (parent) =>
                   {
                       deep++;
                       object parentid = type.GetProperty(RootPropertyField).GetValue(parent, null);
                       var details = Lst.Where(p =>
                           type.GetProperty(ParentPropertyField).GetValue(p, null) != null && //it is root
                           type.GetProperty(ParentPropertyField).GetValue(p, null).ToString() == parentid.ToString()).ToArray();
                       if (!string.IsNullOrEmpty(SetHasChildField))
                       {
                           type.GetProperty(SetHasChildField).SetValue(parent, details.Length > 0, null);
                       }
                       foreach (var m in details)
                       {
                           var val = type.GetProperty(TextPropertyField).GetValue(m, null);
                           if (val != null)
                           {
                               var str = val.ToString();
                               for (int i = 0; i < deep; i++)
                                   str = PadString + str;
                               type.GetProperty(TextPropertyField).SetValue(m, str, null);
                           }
                           resultList.Add(m);
                           fxDetail(m);
                       }
                       deep--;
                       return 0;
                   }
                );

            foreach (var root in roots)
            {
                resultList.Add(root);
                fxDetail(root);
            }
            return resultList.ToArray();
        }


    }
}
