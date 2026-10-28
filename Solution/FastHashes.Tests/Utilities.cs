#region Using Directives
using System;
#endregion

namespace FastHashes.Tests
{
    public static class Utilities
    {
        #region Methods
        public static String FormatNumericArray<T>(T[] array) where T : struct, IComparable, IConvertible, IFormattable
        {
            if (Type.GetTypeCode(typeof(T)) is not (TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64))
                return "INVALID";

            if (array == null)
                return "NULL";

            return $"[{String.Join(", ", array)}]";
        }
        #endregion
    }
}