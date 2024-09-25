using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class ValueConverterExtension
    {
        public static string ToLowerTurkish(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            // Türkçe karakterlerin küçük harfe dönüştürüldüğü alfabe
            string[] turkishCharacters = { "İ", "I", "Ç", "Ş", "Ğ", "Ü", "Ö", "ı", "ç", "ş", "ğ", "ü", "ö" };
            string[] plainCharacters = { "i", "i", "c", "s", "g", "u", "o", "i", "c", "s", "g", "u", "o" };

            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                int index = Array.IndexOf(turkishCharacters, c.ToString());
                if (index >= 0)
                {
                    sb.Append(plainCharacters[index]);
                }
                else if (char.IsLetter(c))
                {
                    sb.Append(char.ToLower(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
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
                        return null;
                }
            }

            return null;
        }
        public static bool? ToBooleanObj(this object obj)
        {
            bool dgr;
            if (obj != null && bool.TryParse(obj.ToString(), out dgr)) return Convert.ToBoolean(obj);
            return null;
        }
        public static double? ToDoubleObj(this object obj)
        {
            if (obj != null && obj.IsNumber()) return Convert.ToDouble(obj);
            return null;
        }
        public static int? ToIntObj(this object obj)
        {
            if (obj != null && (obj.IsNumber())) return Convert.ToInt32(Convert.ToDouble(obj));
            return null;
        }
        public static int ToIntObj(this object obj, int defaultValue)
        {

            return ToIntObj(obj) ?? defaultValue;
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
            return time.Value.ToFormatTime();
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
            DateTime tarih;
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

        public static string ToMonthName(this int? month)
        {
            if (month == null) return "";
            if (month == 1) return "Ocak";
            if (month == 2) return "Şubat";
            if (month == 3) return "Mart";
            if (month == 4) return "Nisan";
            if (month == 5) return "Mayıs";
            if (month == 6) return "Haziran";
            if (month == 7) return "Temmuz";
            if (month == 8) return "Ağustos";
            if (month == 9) return "Eylül";
            if (month == 10) return "Ekim";
            if (month == 11) return "Kasım";
            if (month == 12) return "Aralık";
            return "";
        }
        #endregion


        public static List<string> CustomSplit(this string input, char separator = ',')
        {
            if (input.IsNullOrWhiteSpace()) return new List<string>();
            return input.Split(separator).Where(p => !p.IsNullOrWhiteSpace()).Select(s => s.Trim()).ToList();
        }
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
            rsm = "https://lisansustu.yildiz.edu.tr" + rsm;
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
            var exts = new[] { "jpg", "jpeg", "gif", "png", "bmp", "tiff" };
            return exts.Contains(ext);
        }
        public static bool IsPdfFile(this string filePath)
        {
            if (filePath.IsNullOrWhiteSpace()) return false;
            var ext = filePath.Split('.').Last().ToLower();
            var exts = new[] { "pdf" };
            return exts.Contains(ext);
        }
        public static bool IsDocFile(this string filePath)
        {
            if (filePath.IsNullOrWhiteSpace()) return false;
            var ext = filePath.Split('.').Last().ToLower();
            var exts = new[] { "doc", "docx" };
            return exts.Contains(ext);
        }
        public static bool IsExcelFile(this string filePath)
        {
            if (filePath.IsNullOrWhiteSpace()) return false;
            var ext = filePath.Split('.').Last().ToLower();
            var exts = new[] { "xls", "xlsx" };
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
            double dbl;
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
            double dbl;
            if (double.TryParse(@string, out dbl))
                return dbl;
            return defaultValue;
        }
        public static MvcHtmlString ToEvetHayir(this bool value)
        {
            return new MvcHtmlString((value ? "Evet" : "Hayır"));
        }
        public static MvcHtmlString ToEvetHayir(this bool? value)
        {
            return new MvcHtmlString(value == null ? "" : (value.Value ? "Evet" : "Hayır"));
        }
        public static MvcHtmlString ToAktifPasif(this bool value)
        {
            return new MvcHtmlString(value ? "Aktif" : "Pasif");
        }
        public static MvcHtmlString ToAktifPasif(this bool? value)
        {
            return new MvcHtmlString(value == null ? "" : (value.Value ? "Aktif" : "Pasif"));
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

        private static readonly string ToOrderedListPadChar = string.Concat(((char)160).ToString(), ((char)160).ToString(), ((char)160).ToString());
        public static T[] ToOrderedList<T>(this IEnumerable<T> objectList, string rootPropertyField, string parentPropertyField, string textPropertyField)
        {
            //string padStr = ((char)160).ToString();
            //padStr = padStr + padStr + padStr;
            string padStr = ToOrderedListPadChar;
            return objectList.ToOrderedList(rootPropertyField, parentPropertyField, textPropertyField, padStr);
        }
        public static T[] ToOrderedList<T>(this IEnumerable<T> objectList, string rootPropertyField, string parentPropertyField, string textPropertyField, string padString)
        {
            var resultList = new List<T>();
            if (objectList == null || !objectList.Any()) return resultList.ToArray();
            //var LstObject = (object[])ObjectList;             
            var queryObjectList = objectList.AsQueryable();

            var type = queryObjectList.First().GetType();

            IEnumerable<string> ids;
            try
            {
                ids = queryObjectList.Select(s => type.GetProperty(rootPropertyField).GetValue(s, null).ToString()).ToArray();
            }
            catch
            {
                return resultList.ToArray();
            }
            var roots = new List<T>();
            foreach (var l in queryObjectList)
            {
                if (type.GetProperty(parentPropertyField).GetValue(l, null) == null) roots.Add(l);
                else
                {
                    var bid = type.GetProperty(parentPropertyField).GetValue(l, null).ToString();
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
                       object parentid = type.GetProperty(rootPropertyField).GetValue(parent, null);
                       var details = queryObjectList.Where(p =>
                           type.GetProperty(parentPropertyField).GetValue(p, null) != null && //it is root
                           type.GetProperty(parentPropertyField).GetValue(p, null).ToString() == parentid.ToString())
                           .AsEnumerable();
                       foreach (var m in details)
                       {
                           if (string.IsNullOrEmpty(textPropertyField) == false)
                           {
                               var val = type.GetProperty(textPropertyField).GetValue(m, null);
                               if (val != null)
                               {
                                   var str = val.ToString();
                                   if (str.StartsWith(padString) == false)
                                       for (int i = 0; i < deep; i++)
                                           str = padString + str;
                                   type.GetProperty(textPropertyField).SetValue(m, str, null);
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

        public static T[] ToOrderedList<T>(this T[] objectList, string rootPropertyField, string parentPropertyField, string textPropertyField, string padString, string setHasChildField)
        {
            List<T> resultList = new List<T>();
            if (objectList == null) return resultList.ToArray();
            //var LstObject = (object[])ObjectList;            
            var LstObject = objectList;
            if (LstObject.Length == 0) return resultList.ToArray();
            var Lst = LstObject.AsQueryable();

            var type = Lst.First().GetType();
            var ids = Lst.Select(s => s.GetType().GetProperty(rootPropertyField).GetValue(s, null).ToString()).ToArray();

            List<T> roots = new List<T>();
            foreach (var l in Lst)
            {
                if (type.GetProperty(parentPropertyField).GetValue(l, null) == null) roots.Add(l);
                else
                {
                    var bid = type.GetProperty(parentPropertyField).GetValue(l, null).ToString();
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
                       object parentid = type.GetProperty(rootPropertyField).GetValue(parent, null);
                       var details = Lst.Where(p =>
                           type.GetProperty(parentPropertyField).GetValue(p, null) != null && //it is root
                           type.GetProperty(parentPropertyField).GetValue(p, null).ToString() == parentid.ToString()).ToArray();
                       if (!string.IsNullOrEmpty(setHasChildField))
                       {
                           type.GetProperty(setHasChildField).SetValue(parent, details.Length > 0, null);
                       }
                       foreach (var m in details)
                       {
                           var val = type.GetProperty(textPropertyField).GetValue(m, null);
                           if (val != null)
                           {
                               var str = val.ToString();
                               for (int i = 0; i < deep; i++)
                                   str = padString + str;
                               type.GetProperty(textPropertyField).SetValue(m, str, null);
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


        public static string SetYaziContentParameters(string htmlContent, List<MailParameterDto> parameterDtos)
        { 
            htmlContent = htmlContent.Replace("{{", "{{_removeRw_");

            var contentStrList = htmlContent.Split(new[] { "{{", "}}" }, StringSplitOptions.None).ToList();

            foreach (var itemRp in parameterDtos.Where(p => p.Value.IsNullOrWhiteSpace()))
            {
                contentStrList = contentStrList.Where(p => (p.Contains("@" + itemRp.Key) && p.Contains("_removeRw_")) == false).ToList();
            }
            htmlContent = string.Join("", contentStrList);
            foreach (var itemRp in parameterDtos)
            {
                itemRp.Value = itemRp.Value ?? "";
                htmlContent = htmlContent.Replace("@" + itemRp.Key, (itemRp.IsLink ? "<a href='" + itemRp.Value + "' target='_blank'>" + itemRp.Value + "</a>" : itemRp.Value));
            } 
            return htmlContent;

        }

    }
}
