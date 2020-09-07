using System;

namespace Utility {
    public static class EnumsHelper<TEnum> where TEnum : Enum {
        public static readonly bool IsFlagsAttributeDefined;
        public static readonly string[] Names;
        public static readonly TypeCode TypeCode;
        public static readonly Type Type,
                                    UnderlyingType;
        public static readonly TEnum AllBits,
                                     NoBits,
                                     UnusedBits,
                                     UsedBits;
        public static readonly TEnum[] Values;

        static EnumsHelper() {
            Type enumType,
                 enumUnderlyingType;
            TypeCode enumTypeCode;
            TEnum[] enumValues;
            TEnum enumNoBits,
                  enumAllBits,
                  enumUsedBits;
            int enumValues_length;

            enumType = typeof(TEnum);
            if (enumType == typeof(Enum)) {
                throw new InvalidOperationException("Type must be an enum and cannot be '[mscorlib]System.Enum'.");
            }
            Type = enumType;
            enumUnderlyingType = enumType.GetEnumUnderlyingType();
            if (enumUnderlyingType == typeof(int)) {
                enumTypeCode = TypeCode.Int32;
            }
            else if (enumUnderlyingType == typeof(sbyte)) {
                enumTypeCode = TypeCode.SByte;
            }
            else if (enumUnderlyingType == typeof(short)) {
                enumTypeCode = TypeCode.Int16;
            }
            else if (enumUnderlyingType == typeof(long)) {
                enumTypeCode = TypeCode.Int64;
            }
            else if (enumUnderlyingType == typeof(uint)) {
                enumTypeCode = TypeCode.UInt32;
            }
            else if (enumUnderlyingType == typeof(byte)) {
                enumTypeCode = TypeCode.Byte;
            }
            else if (enumUnderlyingType == typeof(ushort)) {
                enumTypeCode = TypeCode.UInt16;
            }
            else if (enumUnderlyingType == typeof(ulong)) {
                enumTypeCode = TypeCode.UInt64;
            }
            else {
                throw new InvalidOperationException("Unknown enum type.");
            }
            UnderlyingType = enumUnderlyingType;
            TypeCode = enumTypeCode;
            IsFlagsAttributeDefined = Type.IsDefined(typeof(FlagsAttribute), false);
            Names = Type.GetEnumNames();
            enumValues = (TEnum[])Type.GetEnumValues();
            Values = enumValues;
            enumNoBits = default(TEnum);
            NoBits = enumNoBits;
            enumAllBits = ~enumNoBits;
            AllBits = enumAllBits;
            enumUsedBits = enumNoBits;
            enumValues_length = enumValues.Length;
            for (int i = 0; i < enumValues_length; i++) {
                enumUsedBits = enumUsedBits | enumValues[i];
            }
            UsedBits = enumUsedBits;
            UnusedBits = ~enumUsedBits;
        }
    }
}
