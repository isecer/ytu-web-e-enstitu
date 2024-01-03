using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class JsonConverter
    {
        private static string _tojson(object obje, IReadOnlyCollection<string> propertyFields)
        {
            Func<object, string> fnc = o =>
            {
                #region parse
                var sb = new StringBuilder();
                var hasNewField = false;
                if (o is string)
                {
                    return "\"" + o.ToString().Replace("\"", "\\\"") + "\"";
                }
                if (o.GetType().IsValueType)
                {
                    if (!QualifierRequired(o.GetType())) return o.ToString();
                    var valStr = o.ToString().Replace("\\", "\\\\").Replace("\"", @"\""");
                    return "\"" + valStr + "\"";
                }
                if (o.GetType().IsEnum)
                {
                    return "\"" + o.ToString().SpaceByCapitalLetters() + "\"";
                }

                sb.Append("{");
                var props = o.GetType().GetProperties();
                foreach (var t in props)
                {
                    if (propertyFields != null && propertyFields.Count > 0 && propertyFields.Contains(t.Name) == false) continue;

                    var type = t.PropertyType;
                    if (!((type.IsValueType) || (type == typeof(String)))) continue;
                    if (type.IsEnum) continue;

                    var valx = t.GetValue(o, null);
                    if (QualifierRequired(type))
                    {
                        if (hasNewField) sb.Append(",");
                        if (valx != null)
                        {
                            var valStr = valx.ToString().Replace("\\", "\\\\").Replace("\"", @"\""");
                            sb.Append("\"" + t.Name + "\":\"" + valStr + "\"");
                        }
                        else
                            sb.Append("\"" + t.Name + "\":\"\"");
                    }
                    else
                    {
                        if (hasNewField) sb.Append(",");
                        if (valx != null)
                            sb.Append("\"" + t.Name + "\":" + valx.ToString().Replace(",", "."));
                        else
                            sb.Append("\"" + t.Name + "\":null");
                    }
                    hasNewField = true;
                }
                sb.Append("}");
                #endregion
                return sb.ToString();
            };


            var objectType = obje.GetType();
            if (objectType == typeof(IList))
            {
                #region if List Type
                var lst = (IList)obje;
                var sb2 = new StringBuilder();
                sb2.Append("[");
                for (int i = 0; i < lst.Count; i++)
                {
                    if (i > 0) sb2.Append(",");
                    sb2.AppendLine(fnc(lst[i]));
                }
                sb2.Append("]");
                return sb2.ToString();
                #endregion
            }
            if (objectType == typeof(IEnumerable))
            {
                #region enumarable
                var sb4 = new StringBuilder();
                sb4.Append("[");
                var pgdata = (IEnumerable)obje;
                var ok = false;
                foreach (var pgd in pgdata)
                {
                    if (ok) { sb4.AppendLine(","); }
                    sb4.AppendLine(_tojson(pgd, propertyFields));
                    ok = true;
                }
                sb4.Append("]");
                return sb4.ToString();
                #endregion
            }
            if (objectType == typeof(IQueryable))
            {
                #region querable
                var sb3 = new StringBuilder();
                sb3.Append("[");
                var pgdata = (IQueryable)obje;
                var ok = false;
                foreach (var pgd in pgdata)
                {
                    if (ok) { sb3.AppendLine(","); }
                    sb3.AppendLine(pgd.ToJson());
                    ok = true;
                }
                sb3.Append("]");
                return sb3.ToString();
                #endregion
            }
            if (objectType == typeof(string))
            {
                var valStr = obje.ToString().Replace("\\", "\\\\").Replace("\"", @"\""");
                return valStr;
            }

            var aaa = typeof(IEnumerable);
            var aaa2 = typeof(IList);
            if (objectType ==  typeof(IEnumerable))
            {
                #region enumarable
                var sb4 = new StringBuilder();
                sb4.Append("[");
                var pgdata = (IEnumerable)obje;
                var ok = false;
                foreach (var pgd in pgdata)
                {
                    if (ok) { sb4.AppendLine(","); }
                    sb4.AppendLine(pgd.ToJson());
                    ok = true;
                }
                sb4.Append("]");
                return sb4.ToString();
                #endregion
            }

            if (obje is System.Collections.IList)
            {
                #region enumarable
                var sb4 = new StringBuilder();
                sb4.Append("[");
                var pgdata = (IEnumerable)obje;
                var ok = false;
                foreach (var pgd in pgdata)
                {
                    if (ok) { sb4.AppendLine(","); }
                    sb4.AppendLine(pgd.ToJson());
                    ok = true;
                }
                sb4.Append("]");
                return sb4.ToString();
                #endregion
            }

            if (objectType.IsValueType)
            {
                return QualifierRequired(obje.GetType()) ? obje.ToString().Replace("\\", "\\\\").Replace("\"", @"\""") : obje.ToString().Replace(",", ".");
            }
            if (objectType.IsEnum)
                return "\"" + obje.ToString().SpaceByCapitalLetters() + "\"";
            return fnc(obje);
        }
        public static string ToJson(this object @object)
        {
            var retwal = _tojson(@object, null);
            return FormatJson(retwal);
        }
        public static string ToJson(this object @object, params string[] propertyFields)
        {
            var retwal = _tojson(@object, propertyFields);
            return FormatJson(retwal);
        }
        private static bool QualifierRequired(Type type)
        {
            var qualifierRequireds = new[] { typeof(Guid), typeof(DateTime), typeof(Guid?), typeof(DateTime?), typeof(Char?),
                typeof(char), typeof(string), typeof(bool), typeof(bool?)
                //typeof(double), typeof(float), typeof(decimal) ,
                //typeof(double?), typeof(float?), typeof(decimal?)
            };
            return type.IsEnum || qualifierRequireds.Contains(type);
        }
        public static string SpaceByCapitalLetters(this string @string)
        {
            var result = "";
            for (int i = 0; i < @string.Length; i++)
            {
                if (i > 0 && char.IsUpper(@string[i])) result += " " + @string[i].ToString().ToUpper();
                else result += @string[i].ToString();
            }
            return result;
        }
        static string FormatJson(string json)
        {
            return json;
            // JSON'u biçimlendirilmiş bir şekilde geri döndürme. şimdilik kullanılmıyor
            return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented);
        }
    }
}