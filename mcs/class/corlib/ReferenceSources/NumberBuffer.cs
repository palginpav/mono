// Number.NumberBuffer — Windows .NET Framework compatible version
//
// Replaces corefx ref struct NumberBuffer with a regular struct that has:
// - Plain fields (sign, digits) instead of properties — required by Windows
//   System.Numerics.dll which accesses them via ldfld
// - NumberBufferBytes static field for stackalloc sizing
// - NumberBuffer(byte*) constructor for .NET Framework compat
// - PackForNative() method
//
// Binary layout preserves corefx field offsets via explicit padding:
//   offset 0:  int precision  (4 bytes)
//   offset 4:  int scale      (4 bytes)
//   offset 8:  bool sign      (1 byte) + 3 pad bytes = 4 bytes total
//   offset 12: inline digits  (102 bytes)
//   offset 114: char* digits  (pointer)

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security;

namespace System
{
    internal static partial class Number
    {
        private const int NumberMaxDigits = 50;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [FriendAccessAllowed]
        internal unsafe struct NumberBuffer
        {
            public static readonly int NumberBufferBytes = 12 + ((NumberMaxDigits + 1) * 2) + IntPtr.Size;

            public int precision;
            public int scale;
            public bool sign;
            private byte _pad1, _pad2, _pad3;

            [StructLayout(LayoutKind.Sequential, Size = (NumberMaxDigits + 1) * sizeof(char))]
            private struct DigitsAndNullTerminator { }

            private DigitsAndNullTerminator _digits;
            public char* digits;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Init()
            {
                fixed (DigitsAndNullTerminator* p = &_digits)
                {
                    digits = (char*)p;
                }
            }

            [SecurityCritical]
            public NumberBuffer(byte* stackBuffer)
            {
                this = default;
                Init();
            }

            [SecurityCritical]
            public byte* PackForNative()
            {
                return null;
            }
        }
    }
}
