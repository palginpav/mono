// Number compatibility methods for Windows System.Numerics.dll
// Ported from referencesource/mscorlib/system/number.cs
//
// Windows System.Numerics.dll BigInteger.Parse/ToString calls:
//   Number.TryStringToNumber(string, NumberStyles, ref NumberBuffer, StringBuilder, NumberFormatInfo, bool)
//   Number.FormatNumberBuffer(byte*, string, NumberFormatInfo, char*)
//
// These methods operate on our NumberBuffer struct with plain fields.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

namespace System
{
    internal static partial class Number
    {
        private static bool IsWhiteCompat(char ch)
        {
            return (ch == 0x20) || (ch >= 0x09 && ch <= 0x0D);
        }

        [SecurityCritical]
        private unsafe static char* MatchCharsCompat(char* p, string str)
        {
            fixed (char* stringPointer = str)
            {
                return MatchCharsCompat(p, stringPointer);
            }
        }

        [SecurityCritical]
        private unsafe static char* MatchCharsCompat(char* p, char* str)
        {
            if (*str == '\0')
                return null;
            for (; *str != '\0'; p++, str++)
            {
                if (*p != *str)
                {
                    if (*str == '\u00A0' && *p == '\u0020')
                        continue;
                    return null;
                }
            }
            return p;
        }

        private static bool TrailingZerosCompat(string s, int index)
        {
            for (int i = index; i < s.Length; i++)
            {
                if (s[i] != '\0')
                    return false;
            }
            return true;
        }

        [SecurityCritical]
        private unsafe static bool ParseNumberCompat(ref char* str, NumberStyles options,
            ref NumberBuffer number, StringBuilder sb, NumberFormatInfo numfmt, bool parseDecimal)
        {
            const int StateSign = 0x0001;
            const int StateParens = 0x0002;
            const int StateDigits = 0x0004;
            const int StateNonZero = 0x0008;
            const int StateDecimal = 0x0010;
            const int StateCurrency = 0x0020;

            number.scale = 0;
            number.sign = false;
            string decSep;
            string groupSep;
            string currSymbol = null;
            string ansicurrSymbol = null;
            string altdecSep = null;
            string altgroupSep = null;

            bool parsingCurrency = false;
            if ((options & NumberStyles.AllowCurrencySymbol) != 0)
            {
                currSymbol = numfmt.CurrencySymbol;
                altdecSep = numfmt.NumberDecimalSeparator;
                altgroupSep = numfmt.NumberGroupSeparator;
                decSep = numfmt.CurrencyDecimalSeparator;
                groupSep = numfmt.CurrencyGroupSeparator;
                parsingCurrency = true;
            }
            else
            {
                decSep = numfmt.NumberDecimalSeparator;
                groupSep = numfmt.NumberGroupSeparator;
            }

            int state = 0;
            bool signflag = false;
            bool bigNumber = (sb != null);
            bool bigNumberHex = (bigNumber && ((options & NumberStyles.AllowHexSpecifier) != 0));
            int maxParseDigits = bigNumber ? int.MaxValue : NumberMaxDigits;

            char* p = str;
            char ch = *p;
            char* next;

            while (true)
            {
                if (IsWhiteCompat(ch) && ((options & NumberStyles.AllowLeadingWhite) != 0) &&
                    (((state & StateSign) == 0) || (((state & StateSign) != 0) &&
                    (((state & StateCurrency) != 0) || numfmt.NumberNegativePattern == 2))))
                {
                }
                else if ((signflag = (((options & NumberStyles.AllowLeadingSign) != 0) && ((state & StateSign) == 0))) &&
                    ((next = MatchCharsCompat(p, numfmt.PositiveSign)) != null))
                {
                    state |= StateSign;
                    p = next - 1;
                }
                else if (signflag && (next = MatchCharsCompat(p, numfmt.NegativeSign)) != null)
                {
                    state |= StateSign;
                    number.sign = true;
                    p = next - 1;
                }
                else if (ch == '(' && ((options & NumberStyles.AllowParentheses) != 0) && ((state & StateSign) == 0))
                {
                    state |= StateSign | StateParens;
                    number.sign = true;
                }
                else if ((currSymbol != null && (next = MatchCharsCompat(p, currSymbol)) != null) ||
                    (ansicurrSymbol != null && (next = MatchCharsCompat(p, ansicurrSymbol)) != null))
                {
                    state |= StateCurrency;
                    currSymbol = null;
                    ansicurrSymbol = null;
                    p = next - 1;
                }
                else
                {
                    break;
                }
                ch = *++p;
            }

            int digCount = 0;
            int digEnd = 0;
            while (true)
            {
                if ((ch >= '0' && ch <= '9') || (((options & NumberStyles.AllowHexSpecifier) != 0) &&
                    ((ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'))))
                {
                    state |= StateDigits;
                    if (ch != '0' || (state & StateNonZero) != 0 || bigNumberHex)
                    {
                        if (digCount < maxParseDigits)
                        {
                            if (bigNumber)
                                sb.Append(ch);
                            else
                                number.digits[digCount++] = ch;
                            if (ch != '0' || parseDecimal)
                                digEnd = digCount;
                        }
                        if ((state & StateDecimal) == 0)
                            number.scale++;
                        state |= StateNonZero;
                    }
                    else if ((state & StateDecimal) != 0)
                    {
                        number.scale--;
                    }
                }
                else if (((options & NumberStyles.AllowDecimalPoint) != 0) && ((state & StateDecimal) == 0) &&
                    ((next = MatchCharsCompat(p, decSep)) != null ||
                    ((parsingCurrency) && (state & StateCurrency) == 0) && (next = MatchCharsCompat(p, altdecSep)) != null))
                {
                    state |= StateDecimal;
                    p = next - 1;
                }
                else if (((options & NumberStyles.AllowThousands) != 0) && ((state & StateDigits) != 0) &&
                    ((state & StateDecimal) == 0) &&
                    ((next = MatchCharsCompat(p, groupSep)) != null ||
                    ((parsingCurrency) && (state & StateCurrency) == 0) && (next = MatchCharsCompat(p, altgroupSep)) != null))
                {
                    p = next - 1;
                }
                else
                {
                    break;
                }
                ch = *++p;
            }

            bool negExp = false;
            number.precision = digEnd;
            if (bigNumber)
                sb.Append('\0');
            else
                number.digits[digEnd] = '\0';

            if ((state & StateDigits) != 0)
            {
                if ((ch == 'E' || ch == 'e') && ((options & NumberStyles.AllowExponent) != 0))
                {
                    char* temp = p;
                    ch = *++p;
                    if ((next = MatchCharsCompat(p, numfmt.PositiveSign)) != null)
                    {
                        ch = *(p = next);
                    }
                    else if ((next = MatchCharsCompat(p, numfmt.NegativeSign)) != null)
                    {
                        ch = *(p = next);
                        negExp = true;
                    }
                    if (ch >= '0' && ch <= '9')
                    {
                        int exp = 0;
                        do
                        {
                            exp = exp * 10 + (ch - '0');
                            ch = *++p;
                            if (exp > 1000)
                            {
                                exp = 9999;
                                while (ch >= '0' && ch <= '9')
                                    ch = *++p;
                            }
                        } while (ch >= '0' && ch <= '9');
                        if (negExp)
                            exp = -exp;
                        number.scale += exp;
                    }
                    else
                    {
                        p = temp;
                        ch = *p;
                    }
                }
                while (true)
                {
                    if (IsWhiteCompat(ch) && ((options & NumberStyles.AllowTrailingWhite) != 0))
                    {
                    }
                    else if ((signflag = (((options & NumberStyles.AllowTrailingSign) != 0) && ((state & StateSign) == 0))) &&
                        (next = MatchCharsCompat(p, numfmt.PositiveSign)) != null)
                    {
                        state |= StateSign;
                        p = next - 1;
                    }
                    else if (signflag && (next = MatchCharsCompat(p, numfmt.NegativeSign)) != null)
                    {
                        state |= StateSign;
                        number.sign = true;
                        p = next - 1;
                    }
                    else if (ch == ')' && ((state & StateParens) != 0))
                    {
                        state &= ~StateParens;
                    }
                    else if ((currSymbol != null && (next = MatchCharsCompat(p, currSymbol)) != null) ||
                        (ansicurrSymbol != null && (next = MatchCharsCompat(p, ansicurrSymbol)) != null))
                    {
                        currSymbol = null;
                        ansicurrSymbol = null;
                        p = next - 1;
                    }
                    else
                    {
                        break;
                    }
                    ch = *++p;
                }
                if ((state & StateParens) == 0)
                {
                    if ((state & StateNonZero) == 0)
                    {
                        if (!parseDecimal)
                            number.scale = 0;
                        if ((state & StateDecimal) == 0)
                            number.sign = false;
                    }
                    str = p;
                    return true;
                }
            }
            str = p;
            return false;
        }

        [SecuritySafeCritical]
        [FriendAccessAllowed]
        internal unsafe static bool TryStringToNumber(string str, NumberStyles options,
            ref NumberBuffer number, StringBuilder sb, NumberFormatInfo numfmt, bool parseDecimal)
        {
            if (str == null)
                return false;

            fixed (char* stringPointer = str)
            {
                char* p = stringPointer;
                if (!ParseNumberCompat(ref p, options, ref number, sb, numfmt, parseDecimal)
                    || (p - stringPointer < str.Length && !TrailingZerosCompat(str, (int)(p - stringPointer))))
                {
                    return false;
                }
            }
            return true;
        }

        // FormatNumberBuffer — called by Windows System.Numerics.dll BigInteger.ToString
        // In .NET Framework this is an icall to native code. We return null to signal
        // that the caller should use managed formatting instead.
        [FriendAccessAllowed]
        internal static unsafe string FormatNumberBuffer(byte* number, string format,
            NumberFormatInfo info, char* allDigits)
        {
            return null;
        }
    }
}
