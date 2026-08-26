namespace Ensa.EntityFrameworkCore.Repositories.Health;

/// <summary>
/// Helper that preserves the legacy search behaviour on the SKRS reference tables (ICD-10, medications).
/// <para>
/// In the legacy system the search term was queried both as entered and with its Turkish characters folded,
/// because part of the SKRS data was loaded without Turkish characters. To preserve that behaviour the
/// repositories <c>OR</c> the two expressions together.
/// </para>
/// </summary>
internal static class TurkishSearch
{
    /// <summary>
    /// Folds Turkish characters down to their ASCII counterparts. Returns the input itself when nothing
    /// changes, so that the caller does not add a redundant second <c>LIKE</c> predicate.
    /// </summary>
    public static string Simplify(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var buffer = new char[value.Length];
        var degisti = false;

        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];
            var replacement = Simplify(character);

            if (replacement != character)
            {
                degisti = true;
            }

            buffer[i] = replacement;
        }

        return degisti ? new string(buffer) : value;
    }

    /// <summary>
    /// Escapes the characters that carry special meaning in <c>LIKE</c> patterns, guaranteeing that user
    /// input is searched for as <b>text</b> rather than as a pattern.
    /// </summary>
    public static string EscapeLike(string value)
        => value
            .Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);

    private static char Simplify(char character) => character switch
    {
        'ı' => 'i',
        'İ' => 'I',
        'ş' => 's',
        'Ş' => 'S',
        'ğ' => 'g',
        'Ğ' => 'G',
        'ü' => 'u',
        'Ü' => 'U',
        'ö' => 'o',
        'Ö' => 'O',
        'ç' => 'c',
        'Ç' => 'C',
        _ => character
    };
}
