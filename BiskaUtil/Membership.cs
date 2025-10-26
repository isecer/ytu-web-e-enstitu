using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BiskaUtil
{
    /// <summary>
    /// use RoleAttribute
    /// </summary>
    public interface IRoleName
    {
    }

    /// <summary>
    /// use MenuAttribute
    /// </summary>   
    public interface IMenu
    {
    }

    /// <summary>
    /// High-performance membership management with caching and minimal reflection
    /// </summary>
    public class Membership
    {
        public delegate void OnRequireUserIdentityEventHandler(string UserName, ref UserIdentity userIdentity);
        public static event OnRequireUserIdentityEventHandler OnRequireUserIdentity;

        // CACHE: Tüm veriler bellekte tutulur
        private static FieldInfo[] _cachedRoleFields;
        private static FieldInfo[] _cachedMenuFields;
        private static RoleAttribute[] _cachedRoles;
        private static MenuAttribute[] _cachedMenus;
        private static Assembly[] _targetAssemblies;
        private static readonly object _lockObject = new object();

        /// <summary>
        /// Sadece gerekli assembly'leri döndürür (cache'li)
        /// </summary>
        private static Assembly[] GetTargetAssemblies()
        {
            if (_targetAssemblies != null) return _targetAssemblies;

            lock (_lockObject)
            {
                if (_targetAssemblies != null) return _targetAssemblies;

                var assemblies = new List<Assembly>();

                // 1. Interface'lerin bulunduğu assembly
                var interfaceAssembly = typeof(IRoleName).Assembly;
                assemblies.Add(interfaceAssembly);

                // 2. Sadece proje assembly'lerini al (Microsoft, System vb. hariç)
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                var projectAssemblies = loadedAssemblies.Where(a =>
                {
                    var name = a.FullName;
                    // Sadece proje assembly'leri
                    return (name.StartsWith("LisansUstuBasvuruSistemi") ||
                            name.StartsWith("Entities") ||
                            name.StartsWith("Business")) &&
                           !name.Contains("resources") &&
                           !name.Contains("XmlSerializers") &&
                           a != interfaceAssembly;
                }).ToList();

                assemblies.AddRange(projectAssemblies);
                _targetAssemblies = assemblies.Distinct().ToArray();

                return _targetAssemblies;
            }
        }

        /// <summary>
        /// Role attribute'u olan tüm field'ları döndürür (cache'li)
        /// </summary>
        public static FieldInfo[] RoleFields()
        {
            if (_cachedRoleFields != null) return _cachedRoleFields;

            lock (_lockObject)
            {
                if (_cachedRoleFields != null) return _cachedRoleFields;

                var type = typeof(IRoleName);
                var infos = new List<FieldInfo>();

                try
                {
                    var targetAssemblies = GetTargetAssemblies();

                    foreach (var assembly in targetAssemblies)
                    {
                        try
                        {
                            // GetTypes() yerine GetExportedTypes() daha hızlı
                            Type[] types;
                            try
                            {
                                types = assembly.GetExportedTypes();
                            }
                            catch
                            {
                                types = assembly.GetTypes();
                            }

                            // Sadece IRoleName implement eden sınıflar
                            var relevantTypes = types.Where(t =>
                                t != null &&
                                !t.IsInterface &&
                                !t.IsAbstract &&
                                type.IsAssignableFrom(t)
                            );

                            foreach (var typex in relevantTypes)
                            {
                                try
                                {
                                    // Sadece public static const string field'lar
                                    var fields = typex.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                                        .Where(f =>
                                            f.IsLiteral &&
                                            f.FieldType == typeof(string));

                                    foreach (var field in fields)
                                    {
                                        // Attribute kontrolü
                                        if (Attribute.IsDefined(field, typeof(RoleAttribute)))
                                        {
                                            infos.Add(field);
                                        }
                                    }
                                }
                                catch
                                {
                                    // Tek bir type hatası tüm işlemi durdurmasın
                                    continue;
                                }
                            }
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            // Yüklenebilen tipleri işle
                            var loadedTypes = ex.Types.Where(t => t != null && type.IsAssignableFrom(t));
                            foreach (var typex in loadedTypes)
                            {
                                try
                                {
                                    var fields = typex.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                                        .Where(f => f.IsLiteral && f.FieldType == typeof(string));

                                    foreach (var field in fields)
                                    {
                                        if (Attribute.IsDefined(field, typeof(RoleAttribute)))
                                        {
                                            infos.Add(field);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                        catch
                        {
                            // Assembly yüklenemezse devam et
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RoleFields Genel Hata: {ex.Message}");
                }

                _cachedRoleFields = infos.ToArray();
                return _cachedRoleFields;
            }
        }

        /// <summary>
        /// Menu attribute'u olan tüm field'ları döndürür (cache'li)
        /// </summary>
        public static IEnumerable<FieldInfo> MenuFields()
        {
            if (_cachedMenuFields != null) return _cachedMenuFields;

            lock (_lockObject)
            {
                if (_cachedMenuFields != null) return _cachedMenuFields;

                var type = typeof(IMenu);
                var infos = new List<FieldInfo>();

                try
                {
                    var targetAssemblies = GetTargetAssemblies();

                    foreach (var assembly in targetAssemblies)
                    {
                        try
                        {
                            Type[] types;
                            try
                            {
                                types = assembly.GetExportedTypes();
                            }
                            catch
                            {
                                types = assembly.GetTypes();
                            }

                            var relevantTypes = types.Where(t =>
                                t != null &&
                                !t.IsInterface &&
                                !t.IsAbstract &&
                                type.IsAssignableFrom(t)
                            );

                            foreach (var typex in relevantTypes)
                            {
                                try
                                {
                                    var fields = typex.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                                        .Where(f =>
                                            f.IsLiteral &&
                                            f.FieldType == typeof(string));

                                    foreach (var field in fields)
                                    {
                                        if (Attribute.IsDefined(field, typeof(MenuAttribute)))
                                        {
                                            infos.Add(field);
                                        }
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            var loadedTypes = ex.Types.Where(t => t != null && type.IsAssignableFrom(t));
                            foreach (var typex in loadedTypes)
                            {
                                try
                                {
                                    var fields = typex.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                                        .Where(f => f.IsLiteral && f.FieldType == typeof(string));

                                    foreach (var field in fields)
                                    {
                                        if (Attribute.IsDefined(field, typeof(MenuAttribute)))
                                        {
                                            infos.Add(field);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MenuFields Genel Hata: {ex.Message}");
                }

                _cachedMenuFields = infos.ToArray();
                return _cachedMenuFields;
            }
        }

        /// <summary>
        /// Tüm rolleri döndürür (cache'li)
        /// </summary>
        public static RoleAttribute[] Roles()
        {
            if (_cachedRoles != null) return _cachedRoles;

            lock (_lockObject)
            {
                if (_cachedRoles != null) return _cachedRoles;

                var fields = RoleFields();
                var roles = new List<RoleAttribute>(fields.Length);
                int siraNo = 0;

                foreach (var field in fields)
                {
                    try
                    {
                        var oAttr = Attribute.GetCustomAttribute(field, typeof(RoleAttribute));
                        if (oAttr == null) continue;

                        var attr = (RoleAttribute)oAttr;
                        var oVal = field.GetValue(null);
                        if (oVal == null) continue;

                        var key = string.IsNullOrWhiteSpace(attr.RolAdi) ? oVal.ToString() : attr.RolAdi;
                        attr.RolAdi = key;

                        var rolKey = field.DeclaringType?.FullName + "." + field.Name;
                        if (attr.RolID == 0)
                            attr.RolID = 1000000000 + ToCrc16(rolKey);

                        attr.SiraNo = siraNo++;
                        roles.Add(attr);
                    }
                    catch
                    {
                        // Hatalı field'ları atla
                        continue;
                    }
                }

                _cachedRoles = roles.ToArray();
                return _cachedRoles;
            }
        }

        /// <summary>
        /// Tüm menüleri döndürür (cache'li)
        /// </summary>
        public static MenuAttribute[] Menus()
        {
            if (_cachedMenus != null) return _cachedMenus;

            lock (_lockObject)
            {
                if (_cachedMenus != null) return _cachedMenus;

                var fields = MenuFields().ToArray();
                var allRoles = Roles();
                var menus = new List<MenuAttribute>(fields.Length);

                foreach (var field in fields)
                {
                    try
                    {
                        var omenuAttr = Attribute.GetCustomAttribute(field, typeof(MenuAttribute));
                        var oroleAttr = Attribute.GetCustomAttribute(field, typeof(RoleAttribute));

                        if (omenuAttr == null) continue;

                        var attr = (MenuAttribute)omenuAttr;
                        var oVal = field.GetValue(null);
                        if (oVal == null) continue;

                        var key = string.IsNullOrWhiteSpace(attr.MenuAdi) ? oVal.ToString() : attr.MenuAdi;
                        attr.MenuAdi = key;

                        var menuKey = field.DeclaringType?.FullName + "." + field.Name;
                        if (attr.MenuID == 0)
                            attr.MenuID = 1000000000 + ToCrc16(menuKey);

                        // Otomatik ilişki koy
                        if (oroleAttr != null)
                        {
                            var lst = attr.BagliRoller != null ? new List<string>(attr.BagliRoller) : new List<string>();
                            lst.Add(oVal.ToString());
                            attr.BagliRoller = lst.ToArray();
                        }

                        menus.Add(attr);
                    }
                    catch
                    {
                        // Hatalı menu'leri atla
                        continue;
                    }
                }

                _cachedMenus = menus.ToArray();
                return _cachedMenus;
            }
        }

        /// <summary>
        /// Cache'i temizler (test için kullanılabilir)
        /// </summary>
        public static void ClearCache()
        {
            lock (_lockObject)
            {
                _cachedRoleFields = null;
                _cachedMenuFields = null;
                _cachedRoles = null;
                _cachedMenus = null;
                _targetAssemblies = null;
            }
        }

        private static int ToCrc32(string strText)
        {
            var x = Crc32.CRC32String(strText);
            try
            {
                return (int)x;
            }
            catch
            {
                return (int)Math.Round((double)(x / 3));
            }
        }

        private static int ToCrc16(string strText)
        {
            var x = Crc16.ComputeChecksum(strText);
            try
            {
                return (int)x;
            }
            catch
            {
                return ToCrc32(strText);
            }
        }

        public static UserIdentity GetUserIdentity(string UserName)
        {
            if (OnRequireUserIdentity != null)
            {
                UserIdentity uid = new UserIdentity(UserName);
                OnRequireUserIdentity(UserName, ref uid);
                return uid;
            }
            return new UserIdentity(UserName, false);
        }
    }
}