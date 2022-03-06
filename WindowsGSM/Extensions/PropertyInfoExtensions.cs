using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace WindowsGSM.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static bool TryGetCustomAttribute<T>(this PropertyInfo propertyInfo, [NotNullWhen(true)] out T? result) where T : Attribute
        {
            result = propertyInfo.GetCustomAttribute<T>();

            return result != null;
        }

        public static bool TryGetCustomAttributes<T>(this PropertyInfo propertyInfo, out List<T> result) where T : Attribute
        {
            result = propertyInfo.GetCustomAttributes<T>()?.ToList() ?? new List<T>();

            return result.Count != 0;
        }

        public static bool TryGetValue<T>(this PropertyInfo propertyInfo, object? obj, [NotNullWhen(true)] out T? result)
        {
            result = (T?)propertyInfo.GetValue(obj);

            return result != null;
        }
    }
}
