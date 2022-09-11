#region Using Directives
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace FastHashes.Tests
{
    public static class Utilities
    {
        #region Methods
        private static Boolean IsValidPrimitive(Type type)
        {
            if (!type.IsPrimitive)
                return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;

                default:
                    return false;
            }
        }

        public static String FormatNumericArray<T>(T[] array) where T : struct, IComparable, IConvertible, IFormattable
        {
            if (!IsValidPrimitive(typeof(T)))
                return "INVALID";

            if (array == null)
                return "NULL";

            Int32 arrayLength = array.Length;

            String result;

            if (array.Length == 0)
                result = "[]";
            else
            {
                StringBuilder builder = new StringBuilder("[");

                for (Int32 i = 0; i < arrayLength - 1; ++i)
                    builder.Append($"{array[i]}, ");

                builder.Append($"{array[arrayLength - 1]}]");

                result = builder.ToString();
            }

            return result;
        }
        #endregion
    }
}