using System;

namespace JsonRepairUtils;

/// <summary>
/// This class provides extension methods for the string class.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Returns the character code at the specified index in the string.
    /// </summary>
    /// <param name="str">The string to retrieve the character code from.</param>
    /// <param name="i">The index of the character.</param>
    /// <returns>The character code at the specified index.</returns>
    public static char CharCodeAt(this string str, int i)
    {
        if (i < 0 || i >= str.Length) return '\0';
        return str[i];
    }

    /// <summary>
    /// Returns a substring of the specified length starting from the specified index in the string,
    /// ensuring that the substring does not exceed the string length.
    /// </summary>
    /// <param name="str">The string to retrieve the substring from.</param>
    /// <param name="startIndex">The starting index of the substring.</param>
    /// <param name="length">The length of the substring.</param>
    /// <returns>The substring of the specified length starting from the specified index.</returns>
    public static string SubstringSafe(this string str, int startIndex, int length)
    {
        return str.Substring(startIndex, Math.Min(length, str.Length - startIndex));
    }
}
