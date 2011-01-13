using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Strategy.Library.Helper
{
    /// <summary>
    /// Methods for manipulating enumerations.
    /// </summary>
    public class EnumHelper
    {
        /// <summary>
        /// Returns an array of the names of the constants in a given enumeration.
        /// </summary>
        /// <remarks>Necessary for the .NET CF</remarks>
        public static string[] GetNames(Type enumType)
        {
            Debug.Assert(enumType.IsEnum);
            FieldInfo[] fieldInfo = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            return fieldInfo.Select(field => field.Name).ToArray();
        }
    }
}
