using System.Text;
using System.Text.RegularExpressions;

namespace JsonRepairUtils;

public class JsonRepair
{
    /// <summary>
    /// Dictionary of control characters and their corresponding escape sequences.
    /// </summary>
    private readonly Dictionary<char, string> _controlCharacters = new()
    {
        { '\b', "\b" },
        { '\f', "\f" },
        { '\n', "\n" },
        { '\r', "\r" },
        { '\t', "\t" }
    };

    /// <summary>
    /// Dictionary of escape characters and their corresponding escape sequences.
    /// </summary>
    private readonly Dictionary<char, string> _escapeCharacters = new()
    {
        { '\"', "\"" },
        { '\\', "\\" },
        { '/' , "/"  },
        { 'b' , "\b" },
        { 'f' , "\f" },
        { 'n' , "\n" },
        { 'r' , "\r" },
        { 't' , "\t" }
    };

    private int _i; // Current index in input text
    private string _text = "" ; // input text
    private readonly StringBuilder _output = new(); // generated output
    private readonly MatchingQuotes _matchingQuotes = new(); // Helper class to match opening and closing quotes

    /// <summary>
    /// Indicator of whether the <see cref ="JsonRepair.Repair"/> will throw errors.
    /// </summary>
    public bool ThrowExceptions { get; set; } = true;

    /// <summary>
    /// Repairs a string containing an invalid JSON document.
    /// </summary>
    /// <param name="input">The JSON document to repair</param>
    /// <returns>The repaired JSON document</returns>
    /// <exception cref="JsonRepairError">Thrown when an error occurs during JSON repair</exception>
    public string Repair(string input)
    {
        _text = input;
        _output.EnsureCapacity(input.Length);

        var processed = ParseValue();
        if (!processed) { ThrowUnexpectedEnd(); }

        var processedComma = ParseCharacter(StringUtils.CodeComma);
        if (processedComma)
        {
            ParseWhitespaceAndSkipComments();
        }

        if (StringUtils.IsStartOfValue(_text.CharCodeAt(_i)) && StringUtils.EndsWithCommaOrNewline(_output))
        {
            // start of a new value after end of the root level object: looks like
            // newline delimited JSON -> turn into a root level array
            if (!processedComma)
            {
                // repair missing comma
                StringUtils.InsertBeforeLastWhitespace(_output, ",");
            }

            ParseNewlineDelimitedJson();
        }
        else if (processedComma)
        {
            // repair: remove trailing comma
            StringUtils.StripLastOccurrence(_output, ",");
        }

        if (_i >= _text.Length)
        {
            // reached the end of the document properly
            var result = _output.ToString();
            Reset();
            return result;
        }
        else
        {
            ThrowUnexpectedCharacter();

            var result = _output.ToString();
            Reset();
            return result;
        }
    }

    private void Reset()
    {
        _i = 0;
        _matchingQuotes.Reset();
        _text = "";
        _output.Clear();
    }

    /// <summary>
    /// Parses a JSON value.
    /// </summary>
    /// <returns>True if a value was parsed, false otherwise</returns>
    private bool ParseValue()
    {
        ParseWhitespaceAndSkipComments();
        var processed =
            ParseObject()   ||
            ParseArray()    ||
            ParseString()   ||
            ParseNumber()   ||
            ParseKeywords() ||
            ParseUnquotedString();
        ParseWhitespaceAndSkipComments();

        return processed;
    }

    /// <summary>
    /// Parses and repairs whitespace in the JSON document.
    /// </summary>
    /// <returns>True if any whitespace was parsed and repaired, false otherwise</returns>
    private void ParseWhitespaceAndSkipComments()
    {
        if (_i >= _text.Length) return;

        _ = ParseWhitespace();
        bool changed;
        do
        {
            changed = ParseComment();
            if (changed)
            {
                changed = ParseWhitespace();
            }
        } while (changed);
    }

    /// <summary>
    /// Parses and repairs whitespace in the JSON document.
    /// </summary>
    /// <returns>True if any whitespace was parsed and repaired, false otherwise</returns>
    private bool ParseWhitespace()
    {
        var whitespace = new StringBuilder();
        bool normal;
        while ((normal = StringUtils.IsWhitespace(_text.CharCodeAt(_i))) || StringUtils.IsSpecialWhitespace(_text.CharCodeAt(_i)))
        {
            if (normal)
            {
                whitespace.Append(_text.CharCodeAt(_i));
            }
            else
            {
                // repair special whitespace
                whitespace.Append(' ');
            }

            _i++;
        }

        if (whitespace.Length <= 0) return false;

        _output.Append(whitespace);
        return true;
    }

    /// <summary>
    /// Parses and removes any comments
    /// </summary>
    /// <returns>True if any comment was parsed and removed, false otherwise</returns>
    private bool ParseComment()
    {
        // find a block comment '/* ... */'
        if (_text.CharCodeAt(_i) == StringUtils.CodeSlash && _text.CharCodeAt(_i + 1) == StringUtils.CodeAsterisk)
        {
            // repair block comment by skipping it
            while (_i < _text.Length && !AtEndOfBlockComment())
            {
                _i++;
            }
            _i += 2;

            return true;
        }

        // find a line comment '// ...'
        if (_text.CharCodeAt(_i) == StringUtils.CodeSlash && _text.CharCodeAt(_i + 1) == StringUtils.CodeSlash)
        {
            // repair line comment by skipping it
            while (_i < _text.Length && _text.CharCodeAt(_i) != StringUtils.CodeNewline)
            {
                _i++;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses a JSON character.
    /// </summary>
    /// <param name="code">The character code to parse</param>
    /// <returns>True if the character was parsed, false otherwise</returns>
    private bool ParseCharacter(int code)
    {
        if (_text.CharCodeAt(_i) != code) return false;

        _output.Append(_text.CharCodeAt(_i));
        _i++;

        return true;
    }

    /// <summary>
    /// Skips a JSON character.
    /// </summary>
    /// <param name="code">The character code to skip</param>
    /// <returns>True if the character was skipped, false otherwise</returns>
    private bool SkipCharacter(int code)
    {
        if (_text.CharCodeAt(_i) != code) return false;

        _i++;

        return true;
    }

    /// <summary>
    /// Skips a JSON escape character.
    /// </summary>
    /// <returns>True if the escape character was skipped, false otherwise</returns>
    private bool SkipEscapeCharacter()
    {
        return SkipCharacter(StringUtils.CodeBackslash);
    }

    /// <summary>
    /// Parses a JSON object.
    /// </summary>
    /// <returns>True if an object was parsed, false otherwise</returns>
    private bool ParseObject()
    {
        if (_text.CharCodeAt(_i) != StringUtils.CodeOpeningBrace) return false;

        _output.Append('{');
        _i++;
        ParseWhitespaceAndSkipComments();

        var initial = true;
        while (_i < _text.Length && _text.CharCodeAt(_i) != StringUtils.CodeClosingBrace)
        {
            if (!initial)
            {
                var processedComma = ParseCharacter(StringUtils.CodeComma);
                if (!processedComma)
                {
                    // repair missing comma
                    StringUtils.InsertBeforeLastWhitespace(_output, ",");
                }
                ParseWhitespaceAndSkipComments();
            }
            else
            {
                initial = false;
            }

            var processedKey = ParseString() || ParseUnquotedString();
            if (!processedKey)
            {
                if (
                    _text.CharCodeAt(_i) == StringUtils.CodeClosingBrace   ||
                    _text.CharCodeAt(_i) == StringUtils.CodeOpeningBrace   ||
                    _text.CharCodeAt(_i) == StringUtils.CodeClosingBracket ||
                    _text.CharCodeAt(_i) == StringUtils.CodeOpeningBracket ||
                    _text.CharCodeAt(_i) == '\0'
                )
                {
                    // repair trailing comma
                    StringUtils.StripLastOccurrence(_output, ",");
                }
                else
                {
                    ThrowObjectKeyExpected();
                }
                break;
            }

            ParseWhitespaceAndSkipComments();
            var processedColon = ParseCharacter(StringUtils.CodeColon);
            if (!processedColon)
            {
                if (StringUtils.IsStartOfValue(_text.CharCodeAt(_i)))
                {
                    // repair missing colon
                    StringUtils.InsertBeforeLastWhitespace(_output, ":");
                }
                else
                {
                    ThrowColonExpected();
                }
            }
            var processedValue = ParseValue();
            if (processedValue) continue;

            if (processedColon)
            {
                // repair missing object value
                _output.Append("null");
            }
            else
            {
                ThrowColonExpected();
            }
        }

        if (_text.CharCodeAt(_i) == StringUtils.CodeClosingBrace)
        {
            _output.Append('}');
            _i++;
        }
        else
        {
            // repair missing end bracket
            StringUtils.InsertBeforeLastWhitespace(_output, "}");
        }

        return true;
    }

    /// <summary>
    /// Parses a JSON array.
    /// </summary>
    /// <returns>True if an array was parsed, false otherwise</returns>
    private bool ParseArray()
    {
        if (_text.CharCodeAt(_i) != StringUtils.CodeOpeningBracket) return false;

        _output.Append('[');
        _i++;
        ParseWhitespaceAndSkipComments();

        var initial = true;
        while (_i < _text.Length && _text.CharCodeAt(_i) != StringUtils.CodeClosingBracket)
        {
            if (!initial)
            {
                var processedComma = ParseCharacter(StringUtils.CodeComma);
                if (!processedComma)
                {
                    // repair missing comma
                    StringUtils.InsertBeforeLastWhitespace(_output, ",");
                }
            }
            else
            {
                initial = false;
            }

            var processedValue = ParseValue();
            if (processedValue) continue;

            // repair trailing comma
            StringUtils.StripLastOccurrence(_output, ",");
            break;
        }

        if (_text.CharCodeAt(_i) == StringUtils.CodeClosingBracket)
        {
            _output.Append(']');
            _i++;
        }
        else
        {
            // repair missing closing array bracket
            StringUtils.InsertBeforeLastWhitespace(_output, "]");
        }

        return true;
    }

    // <summary>
    // Parses and repairs Newline Delimited JSON (NDJSON): multiple JSON objects separated by a newline character.
    // </summary>
    private void ParseNewlineDelimitedJson()
    {
        // repair NDJSON
        var initial = true;
        var processedValue = true;
        while (processedValue)
        {
            if (!initial)
            {
                // parse optional comma, insert when missing
                var processedComma = ParseCharacter(StringUtils.CodeComma);
                if (!processedComma)
                {
                    // repair: add missing comma
                    StringUtils.InsertBeforeLastWhitespace(_output, ",");
                }
            }
            else
            {
                initial = false;
            }

            processedValue = ParseValue();
        }

        if (!processedValue)
        {
            // repair: remove trailing comma
            StringUtils.StripLastOccurrence(_output, ",");
        }

        // repair: wrap the output inside array brackets
        _output.Insert(0, "[\n");
        _output.Append("\n]");
    }


    /// <summary>
    /// Parses a JSON string.
    /// </summary>
    /// <returns>True if a string was parsed, false otherwise</returns>
    private bool ParseString()
    {
        var skipEscapeChars = _text.CharCodeAt(_i) == StringUtils.CodeBackslash;
        if (skipEscapeChars)
        {
            // repair: remove the first escape character
            _i++;
            skipEscapeChars = true;
        }

        if (!StringUtils.IsQuote(_text.CharCodeAt(_i))) return false;

        _matchingQuotes.SetStartQuote(_text.CharCodeAt(_i));
        _output.Append('"');
        _i++;

        while (_i < _text.Length && !_matchingQuotes.IsMatchingEndQuote(_text.CharCodeAt(_i)))
        {
            if (_text.CharCodeAt(_i) == StringUtils.CodeBackslash)
            {
                var character = _text.CharCodeAt(_i + 1);
                var escapeChar = _escapeCharacters.GetValueOrDefault(character);
                if (escapeChar != null)
                {
                    _output.Append(_text.AsSpan(_i, 2));
                    _i += 2;
                }
                else if (character == 'u')
                {
                    if (
                        StringUtils.IsHex(_text.CharCodeAt(_i + 2)) &&
                        StringUtils.IsHex(_text.CharCodeAt(_i + 3)) &&
                        StringUtils.IsHex(_text.CharCodeAt(_i + 4)) &&
                        StringUtils.IsHex(_text.CharCodeAt(_i + 5))
                    )
                    {
                        _output.Append(_text.AsSpan(_i, 6));
                        _i += 6;
                    }
                    else
                    {
                        ThrowInvalidUnicodeCharacter(_i);
                    }
                }
                else
                {
                    // repair invalid escape character: remove it
                    _output.Append(character);
                    _i += 2;
                }
            }
            else
            {
                var character = _text.CharCodeAt(_i);
                int code = _text.CharCodeAt(_i);

                if (code == StringUtils.CodeDoubleQuote && _text.CharCodeAt(_i - 1) != StringUtils.CodeBackslash)
                {
                    // repair unescaped double quote
                    _output.Append($"\\{character}");
                    _i++;
                }
                else if (StringUtils.IsControlCharacter(character))
                {
                    // unescaped control character
                    _output.Append(_controlCharacters[character]);
                    _i++;
                }
                else
                {
                    if (!StringUtils.IsValidStringCharacter(code))
                    {
                        ThrowInvalidCharacter(character);
                    }
                    _output.Append(character);
                    _i++;
                }
            }

            if (!skipEscapeChars) continue;

            var processed = SkipEscapeCharacter();
            if (processed)
            {
                // repair: skipped escape character (nothing to do)
            }
        }

        if (StringUtils.IsQuote(_text.CharCodeAt(_i)))
        {
            if (_text.CharCodeAt(_i) != StringUtils.CodeDoubleQuote)
            {
                // repair non-normalized quote. todo?
            }
            _output.Append('"');
            _i++;
        }
        else
        {
            // repair missing end quote
            _output.Append('"');
        }

        ParseConcatenatedString();

        return true;
    }

    /// <summary>
    /// Parses and repairs concatenated JSON strings in the JSON document.
    /// </summary>
    /// <returns>True if any concatenated strings were parsed and repaired, false otherwise</returns>
    private void ParseConcatenatedString()
    {
        ParseWhitespaceAndSkipComments();
        while (_text.CharCodeAt(_i) == StringUtils.CodePlus)
        {
            _i++;
            ParseWhitespaceAndSkipComments();

            // repair: remove the end quote of the first string
            StringUtils.StripLastOccurrence(_output, "\"", true);
            var start = _output.Length;
            ParseString();

            // repair: remove the start quote of the second string
            _output.Remove(start, 1);
        }
    }

    /// <summary>
    /// Parses a JSON number.
    /// </summary>
    /// <returns>True if a number was parsed, false otherwise</returns>
    private bool ParseNumber()
    {
        var start = _i;
        if (_text.CharCodeAt(_i) == StringUtils.CodeMinus)
        {
            _i++;
            if (ExpectDigitOrRepair(start))
            {
                return true;
            }
        }

        if (_text.CharCodeAt(_i) == StringUtils.CodeZero)
        {
            _i++;
        }
        else if (StringUtils.IsNonZeroDigit(_text.CharCodeAt(_i)))
        {
            _i++;
            while (StringUtils.IsDigit(_text.CharCodeAt(_i)))
            {
                _i++;
            }
        }

        if (_text.CharCodeAt(_i) == StringUtils.CodeDot)
        {
            _i++;
            if (ExpectDigitOrRepair(start))
            {
                return true;
            }
            while (StringUtils.IsDigit(_text.CharCodeAt(_i)))
            {
                _i++;
            }
        }

        if (_text.CharCodeAt(_i) == StringUtils.CodeLowercaseE || _text.CharCodeAt(_i) == StringUtils.CodeUppercaseE)
        {
            _i++;
            if (_text.CharCodeAt(_i) == StringUtils.CodeMinus || _text.CharCodeAt(_i) == StringUtils.CodePlus)
            {
                _i++;
            }
            if (ExpectDigitOrRepair(start))
            {
                return true;
            }
            while (StringUtils.IsDigit(_text.CharCodeAt(_i)))
            {
                _i++;
            }
        }

        if (_i <= start) return false;

        _output.Append(_text.AsSpan(start, _i - start));

        return true;
    }

    /// <summary>
    /// Parses and repairs JSON keywords (true, false, null) in the JSON document.
    /// </summary>
    /// <returns>True if a keyword was parsed and repaired, false otherwise</returns>
    private bool ParseKeywords()
    {
        return
            ParseKeyword("true", "true")   ||
            ParseKeyword("false", "false") ||
            ParseKeyword("null", "null")   ||
            // repair Python keywords True, False, None
            ParseKeyword("True", "true")   ||
            ParseKeyword("False", "false") ||
            ParseKeyword("None", "null");
    }

    /// <summary>
    /// Parses a specific JSON keyword.
    /// </summary>
    /// <param name="name">The name of the keyword</param>
    /// <param name="value">The repaired value of the keyword</param>
    /// <returns>True if the keyword was parsed and repaired, false otherwise</returns>
    private bool ParseKeyword(string name, string value)
    {
        if (_text.SubstringSafe(_i, name.Length) != name) return false;

        _output.Append(value);
        _i += name.Length;

        return true;
    }

    /// <summary>
    /// Parses an unquoted JSON string or a function call.
    /// </summary>
    /// <returns>True if an unquoted string or a function call was parsed, false otherwise</returns>
    private bool ParseUnquotedString()
    {
        // note that the symbol can end with whitespaces: we stop at the next delimiter
        var start = _i;
        while (_i < _text.Length && !StringUtils.IsDelimiter(_text.CharCodeAt(_i)))
        {
            _i++;
        }

        if (_i <= start) return false;

        if (_text.CharCodeAt(_i) == StringUtils.CodeOpenParenthesis)
        {
            // repair a MongoDB function call like NumberLong("2")
            // repair a JSONP function call like callback({...});
            _i++;

            ParseValue();

            if (_text.CharCodeAt(_i) == StringUtils.CodeCloseParenthesis)
            {
                // repair: skip close bracket of function call
                _i++;
                if (_text.CharCodeAt(_i) == StringUtils.CodeSemicolon)
                {
                    // repair: skip semicolon after JSONP call
                    _i++;
                }
            }

            return true;
        }

        // repair unquoted string

        // first, go back to prevent getting trailing whitespaces in the string
        while (StringUtils.IsWhitespace(_text.CharCodeAt(_i - 1)) && _i > 0)
        {
            _i--;
        }

        var symbol = _text.Substring(start, _i - start);
        _output.Append(symbol == "undefined" ? "null" : $"\"{symbol}\"");

        return true;
    }

    /// <summary>
    /// Parses input text at current position for end of comment
    /// </summary>
    /// <returns>True if an end of block comment, false otherwise</returns>
    private bool AtEndOfBlockComment()
    {
        return _text.CharCodeAt(_i) == '*' && _text.CharCodeAt(_i + 1) == '/';
    }

    /// <summary>
    /// Throws an error if input text at current position is not a digit
    /// </summary>
    /// <param name="start">Start position of number</param>
    private void ExpectDigit(int start)
    {
        if (StringUtils.IsDigit(_text.CharCodeAt(_i))) return;

        var numSoFar = _text.Substring(start, _i - start);
        Reset();
        throw new JsonRepairError($"Invalid number '{numSoFar}', expecting a digit {Got()}", 2);
    }


    /// <summary>
    /// Parses an number cut off at the end JSON string or a function call.
    /// </summary>
    /// <returns>True if number can be fixed</returns>
    private bool ExpectDigitOrRepair(int start)
    {
        if (_i >= _text.Length)
        {
            // repair numbers cut off at the end
            // this will only be called when we end after a '.', '-', or 'e' and does not
            // change the number more than it needs to make it valid JSON
            _output.Append(string.Concat(_text.AsSpan(start, _i - start), "0"));
            return true;
        }

        ExpectDigit(start);

        return false;
    }

    /// <summary>
    /// Throws an invalid character exception
    /// Will be ignored if the ThrowExceptions property is false
    /// </summary>
    /// <param name="character"></param>
    /// <exception cref="JsonRepairError"></exception>
    private void ThrowInvalidCharacter(char character)
    {
        if (!ThrowExceptions) return;

        Reset();
        throw new JsonRepairError($"Invalid character {character}", _i);
    }

    /// <summary>
    /// Throws an unexpected character exception
    /// Will be ignored if the ThrowExceptions property is false
    /// </summary>
    /// <exception cref="JsonRepairError"></exception>
    private void ThrowUnexpectedCharacter()
    {
        if (!ThrowExceptions) return;

        Reset();
        throw new JsonRepairError($"Unexpected character {_text.CharCodeAt(_i)}", _i);
    }

    /// <summary>
    /// Throws an unexpected end exception
    /// Will be ignored if the ThrowExceptions property is false
    /// </summary>
    /// <exception cref="JsonRepairError"></exception>
    private void ThrowUnexpectedEnd()
    {
        if (!ThrowExceptions) return;

        Reset();
        throw new JsonRepairError("Unexpected end of json string", _text.Length);
    }

    /// <summary>
    /// Throws an unexpected object key expected exception
    /// Will be ignored if the ThrowExceptions property is false
    /// </summary>
    /// <exception cref="JsonRepairError"></exception>
    private void ThrowObjectKeyExpected()
    {
        if (!ThrowExceptions) return;

        Reset();
        throw new JsonRepairError("Object key expected", _i);
    }

    /// <summary>
    /// Throws an colon expected exception
    /// Will be ignored if the ThrowExceptions property is false
    /// </summary>
    /// <exception cref="JsonRepairError"></exception>
    private void ThrowColonExpected()
    {
        if (!ThrowExceptions) return;

        Reset();
        throw new JsonRepairError("Colon expected", _i);
    }

    /// <summary>
    /// Throws an invalid unicode character exception
    /// Will be ignored if the ThrowExceptions property is false
    /// </summary>
    /// <exception cref="JsonRepairError"></exception>
    private void ThrowInvalidUnicodeCharacter(int start)
    {
        if (!ThrowExceptions) return;

        var end = start + 2;
        while (Regex.IsMatch(_text[end].ToString(), @"\w"))
        {
            end++;
        }
        var chars = _text.Substring(start, end - start);

        Reset();
        throw new JsonRepairError($"Invalid unicode character \"{chars}\"", _i);
    }

    /// <summary>
    /// Helper function that returns a description of the last gotten character
    /// </summary>
    private string Got()
    {
        return _text.CharCodeAt(_i) != '\0' ? $"but got '{_text.CharCodeAt(_i)}'" : "but reached end of input";
    }
}
