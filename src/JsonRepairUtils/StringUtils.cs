using System.Text;
using System.Text.RegularExpressions;

namespace JsonRepairUtils;

/// <summary>
/// This class provides utility methods working with strings for jsonrepair.
/// </summary>
internal static class StringUtils
{
    public const int CodeBackslash                = 0x5c; // "\"
    public const int CodeSlash                    = 0x2f; // "/"
    public const int CodeAsterisk                 = 0x2a; // "*"
    public const int CodeOpeningBrace             = 0x7b; // "{"
    public const int CodeClosingBrace             = 0x7d; // "}"
    public const int CodeOpeningBracket           = 0x5b; // "["
    public const int CodeClosingBracket           = 0x5d; // "]"
    public const int CodeOpenParenthesis          = 0x28; // "("
    public const int CodeCloseParenthesis         = 0x29; // ")"
    public const int CodeSpace                    = 0x20; // " "
    public const int CodeNewline                  = 0xa; // "\n"
    public const int CodeTab                      = 0x9; // "\t"
    public const int CodeReturn                   = 0xd; // "\r"
    public const int CodeBackspace                = 0x08; // "\b"
    public const int CodeFormFeed                 = 0x0c; // "\f"
    public const int CodeDoubleQuote              = 0x0022; // "
    public const int CodePlus                     = 0x2b; // "+"
    public const int CodeMinus                    = 0x2d; // "-"
    public const int CodeQuote                    = 0x27; // "'"
    public const int CodeZero                     = 0x30;
    public const int CodeOne                      = 0x31;
    public const int CodeNine                     = 0x39;
    public const int CodeComma                    = 0x2c; // ","
    public const int CodeDot                      = 0x2e; // "." (dot, period)
    public const int CodeColon                    = 0x3a; // ":"/// <param name="code">The character to check.</param>
    public const int CodeSemicolon                = 0x3b; // ";"
    public const int CodeUppercaseA               = 0x41; // "A"
    public const int CodeLowercaseA               = 0x61; // "a"
    public const int CodeUppercaseE               = 0x45; // "E"
    public const int CodeLowercaseE               = 0x65; // "e"
    public const int CodeUppercaseF               = 0x46; // "F"
    public const int CodeLowercaseF               = 0x66; // "f"
    private const int CodeNonBreakingSpace        = 0xa0;
    private const int CodeEnQuad                  = 0x2000;
    private const int CodeHairSpace               = 0x200a;
    private const int CodeNarrowNoBreakSpace      = 0x202f;
    private const int CodeMediumMathematicalSpace = 0x205f;
    private const int CodeIdeographicSpace        = 0x3000;
    private const int CodeDoubleQuoteLeft         = 0x201c; // “
    private const int CodeDoubleQuoteRight        = 0x201d; // ”
    private const int CodeQuoteLeft               = 0x2018; // ‘
    private const int CodeQuoteRight              = 0x2019; // ’
    private const int CodeGraveAccent             = 0x0060; // `
    private const int CodeAcuteAccent             = 0x00b4; // ´

    /// <summary>
    /// Checks if the given character code represents a hexadecimal digit.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a hexadecimal digit, false otherwise.</returns>
    public static bool IsHex(int code)
    {
        return (code >= CodeZero       && code <= CodeNine)       ||
               (code >= CodeUppercaseA && code <= CodeUppercaseF) ||
               (code >= CodeLowercaseA && code <= CodeLowercaseF);
    }

    /// <summary>
    /// Checks if the given character code represents a digit.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a digit, false otherwise.</returns>
    public static bool IsDigit(int code)
    {
        return code >= CodeZero && code <= CodeNine;
    }

    /// <summary>
    /// Checks if the given character code represents a non-zero digit.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a non-zero digit, false otherwise.</returns>
    public static bool IsNonZeroDigit(int code)
    {
        return code >= CodeOne && code <= CodeNine;
    }

    /// <summary>
    /// Checks if the given character code represents a valid string character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a valid string character, false otherwise.</returns>
    public static bool IsValidStringCharacter(int code)
    {
        return code >= 0x20 && code <= 0x10ffff;
    }

    /// <summary>
    /// Checks if the given character is a delimiter.
    /// </summary>
    /// <param name="character">The character to check.</param>
    /// <returns>True if the character is a delimiter, false otherwise.</returns>
    public static bool IsDelimiter(char character)
    {
        return character == CodeComma            ||
               character == CodeColon            ||
               character == CodeOpeningBracket   ||
               character == CodeClosingBracket   ||
               character == CodeOpeningBrace     ||
               character == CodeClosingBrace     ||
               character == CodeOpenParenthesis  ||
               character == CodeCloseParenthesis ||
               character == CodeNewline          ||
               IsQuote(character);
    }

    /// <summary>
    /// Checks if the given character is the start of a value.
    /// </summary>
    /// <param name="character">The character to check.</param>
    /// <returns>True if the character is the start of a value, false otherwise.</returns>
    public static bool IsStartOfValue(char character)
    {
        return char.IsLetter(character)        ||
               char.IsDigit(character)         ||
               character == CodeOpeningBracket ||
               character == CodeOpeningBrace   ||
               character == CodeMinus          ||
               IsQuote(character);
    }

    /// <summary>
    /// Checks if the given character code represents a control character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a control character, false otherwise.</returns>
    public static bool IsControlCharacter(int code)
    {
        return code == CodeNewline   ||
               code == CodeReturn    ||
               code == CodeTab       ||
               code == CodeBackspace ||
               code == CodeFormFeed;
    }

    /// <summary>
    /// Checks if the given character code represents a whitespace character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a whitespace character, false otherwise.</returns>
    public static bool IsWhitespace(int code)
    {
        return code is CodeSpace or CodeNewline or CodeTab or CodeReturn;
    }

    /// <summary>
    /// Checks if the given character code represents a special whitespace character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a special whitespace character, false otherwise.</returns>
    public static bool IsSpecialWhitespace(int code)
    {
        return code == CodeNonBreakingSpace                  ||
               (code >= CodeEnQuad && code <= CodeHairSpace) ||
               code == CodeNarrowNoBreakSpace                ||
               code == CodeMediumMathematicalSpace           ||
               code == CodeIdeographicSpace;
    }

    /// <summary>
    /// Checks if the given character code represents a quote character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a quote character, false otherwise.</returns>
    public static bool IsQuote(int code)
    {
        return IsDoubleQuoteLike(code) || IsSingleQuoteLike(code);
    }

    /// <summary>
    /// Checks if the given character code represents a double quote-like character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a double quote-like character, false otherwise.</returns>
    public static bool IsDoubleQuoteLike(int code)
    {
        return code == CodeDoubleQuote || code == CodeDoubleQuoteLeft || code == CodeDoubleQuoteRight;
    }

    /// <summary>
    /// Checks if the given character code represents a double quote character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a double quote character, false otherwise.</returns>
    public static bool IsDoubleQuote(int code)
    {
        return code == CodeDoubleQuote;
    }

    /// <summary>
    /// Checks if the given character code represents a single quote-like character.
    /// </summary>
    /// <param name="code">The character to check.</param>
    /// <returns>True if the code represents a single quote-like character, false otherwise.</returns>
    public static bool IsSingleQuoteLike(int code)
    {
        return code == CodeQuote       ||
               code == CodeQuoteLeft   ||
               code == CodeQuoteRight  ||
               code == CodeGraveAccent ||
               code == CodeAcuteAccent;
    }

    /// <summary>
    /// Strips the last occurrence of a substring from the given text.
    /// </summary>
    /// <param name="text">The text to strip.</param>
    /// <param name="textToStrip">The substring to strip.</param>
    /// <param name="stripRemainingText">True to strip the remaining text after the last occurrence, false otherwise.</param>
    /// <returns>The text with the last occurrence of the substring stripped.</returns>
    public static void StripLastOccurrence(StringBuilder text, string textToStrip, bool stripRemainingText = false)
    {
        var index = text.LastIndexOf(textToStrip);
        if (index == -1) return;

        var length = stripRemainingText ? text.Length - index : 1;
        text.Remove(index, length);
    }

    /// <summary>
    /// Inserts a string before the last whitespace in the given text.
    /// </summary>
    /// <param name="text">The text to insert into.</param>
    /// <param name="textToInsert">The string to insert.</param>
    /// <returns>The modified text with the string inserted.</returns>
    public static void InsertBeforeLastWhitespace(StringBuilder text, string textToInsert)
    {
        var index = text.Length;

        if (!IsWhitespace(text[index - 1]))
        {
            // no trailing whitespaces
            text.Append(textToInsert);
            return;
        }

        while (IsWhitespace(text[index - 1]))
        {
            index--;
        }

        text.Insert(index, textToInsert);
    }

    /// <summary>
    /// Checks if the given text ends with a comma or a newline.
    /// </summary>
    /// <param name="sb">The text to check.</param>
    /// <returns>True if the text ends with a comma or a newline, false otherwise.</returns>
    public static bool EndsWithCommaOrNewline(StringBuilder sb)
    {
        var text = sb.ToString();
        return Regex.IsMatch(text, @"[,\n][ \t\r]*$");
    }

    /// <summary>
    /// Returns the index of the start of the contents in a StringBuilder
    /// </summary>
    /// <param name="sb">StringBuilder</param>
    /// <param name="value">The string to find</param>
    /// <returns></returns>
    private static int LastIndexOf(this StringBuilder sb, string value)
    {
        for (var i = sb.Length - value.Length; i >= 0; i--)
        {
            if (sb[i] != value[0]) continue;

            var index = 1;
            while (index < value.Length && sb[i + index] == value[index])
                ++index;

            if (index == value.Length)
                return i;
        }

        return -1;
    }
}



