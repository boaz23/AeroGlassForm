using System;
using System.Runtime.CompilerServices;

namespace Utility {
    public static class Enums {
        [MethodImpl((MethodImplOptions)256)]
        public static TEnum And<TEnum>(this TEnum value, TEnum flags) where TEnum : Enum {
            return value & flags;
        }

        [MethodImpl((MethodImplOptions)256)]
        public static bool Equal<TEnum>(this TEnum value, TEnum flags) where TEnum : Enum {
            return value == flags;
        }

        [MethodImpl((MethodImplOptions)256)]
        public static TEnum Not<TEnum>(this TEnum value) where TEnum : Enum {
            return ~value;
        }

        [MethodImpl((MethodImplOptions)256)]
        public static TEnum Or<TEnum>(this TEnum value, TEnum flags) where TEnum : Enum {
            return value | flags;
        }

        [MethodImpl((MethodImplOptions)256)]
        public static TEnum Xor<TEnum>(this TEnum value, TEnum flags) where TEnum : Enum {
            return value ^ flags;
        }

        [MethodImpl((MethodImplOptions)256)]
        public static bool HasAnyOfFlags<TEnum>(this TEnum value, TEnum flags) where TEnum : Enum {
            return (value & flags) != 0;
        }

        [MethodImpl((MethodImplOptions)256)]
        public static bool HasFlags<TEnum>(this TEnum value, TEnum flags) where TEnum : Enum {
            return (value & flags) == flags;
        }
    }
}
