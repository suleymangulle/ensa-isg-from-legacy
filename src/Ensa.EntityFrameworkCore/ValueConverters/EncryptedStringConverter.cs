using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ensa.EntityFrameworkCore.ValueConverters;

/// <summary>
/// AES-256/CBC column encrypter replacing the legacy <c>[EncryptColumn]</c> attribute.
/// <para>
/// It encrypts the plaintext on the way into the database and decrypts it on the way out. The ciphertext is
/// stored as <b>Base64</b> text, so the column type stays <c>nvarchar</c> and existing reporting tools keep
/// working.
/// </para>
/// <para>
/// <b>IT IS DETERMINISTIC.</b> Because <see cref="EnsaEncryptionOptions.Iv"/> is fixed, the same plaintext
/// always produces the same ciphertext. This is a deliberate choice: fields such as national id and IBAN must
/// remain usable in <c>WHERE</c> and <c>JOIN</c> clauses and in unique indexes. A random IV would make those
/// queries impossible and force the whole table to be loaded and decrypted in memory.
/// The price: equal values also look equal in the database (they are open to frequency analysis).
/// Encryption is therefore used <i>alongside</i> access control, not <i>instead of</i> it.
/// </para>
/// <para>
/// Usage (inside an IEntityTypeConfiguration):
/// <code>
/// builder.Property(x => x.NationalId)
///        .HasConversion(new EncryptedStringConverter())
///        .HasMaxLength(EncryptedStringConverter.EncryptedMaxLength(EnsaDomainSharedConsts.MaxLengths.NationalId));
/// </code>
/// </para>
/// <para>
/// <b>NULL behaviour:</b> EF Core never invokes the converter for <c>null</c> values, so <c>null</c> columns
/// stay <c>NULL</c> and unencrypted. An empty string (<c>""</c>) is likewise stored as is, unencrypted.
/// </para>
/// </summary>
public sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    /// <summary>
    /// Uses the process-wide <see cref="EnsaEncryptionOptions.Current"/> option.
    /// This is the normal usage, because <c>IEntityTypeConfiguration</c> classes have no access to DI.
    /// </summary>
    public EncryptedStringConverter()
        : this(EnsaEncryptionOptions.Current)
    {
    }

    /// <summary>Converter running with the supplied options (for tests and special scenarios).</summary>
    public EncryptedStringConverter(EnsaEncryptionOptions options)
        : this(
            (options ?? throw new ArgumentNullException(nameof(options))).ResolveKey(),
            options.ResolveIv())
    {
    }

    private EncryptedStringConverter(byte[] key, byte[] iv)
        : base(
            plain => EnsaStringCipher.Encrypt(plain, key, iv),
            cipher => EnsaStringCipher.Decrypt(cipher, key, iv))
    {
    }

    /// <summary>
    /// Computes the maximum number of characters that a given plaintext maximum length will occupy once
    /// encrypted (including Base64 expansion and AES block padding).
    /// Use this value when defining a column length; otherwise you will get a
    /// <c>String or binary data would be truncated</c> error.
    /// </summary>
    /// <param name="plainMaxLength">Maximum number of characters of the plaintext.</param>
    public static int EncryptedMaxLength(int plainMaxLength)
    {
        // At most 4 bytes per character is assumed for UTF-8 (a safe upper bound).
        var plainBytes = Math.Max(plainMaxLength, 1) * 4;

        // AES-CBC + PKCS7: always rounds up to the next full block, and adds one block when already full.
        var cipherBytes = ((plainBytes / 16) + 1) * 16;

        // Base64: every 3 bytes become 4 characters.
        return ((cipherBytes + 2) / 3) * 4;
    }
}

/// <summary>
/// AES-256/CBC helpers used by <see cref="EncryptedStringConverter"/>.
/// <para>
/// The methods are <c>static</c> because they are embedded into EF Core's expression trees and are called
/// again for every query and every save.
/// A new <see cref="Aes"/> instance is created on each call — <see cref="Aes"/> instances are not thread
/// safe, whereas a DbContext may be used in parallel.
/// </para>
/// </summary>
public static class EnsaStringCipher
{
    /// <summary>Encrypts the plaintext and returns it as Base64.</summary>
    public static string Encrypt(string? plainText, byte[] key, byte[] iv)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText ?? string.Empty;
        }

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var cipherBytes = aes.EncryptCbc(Encoding.UTF8.GetBytes(plainText), iv, PaddingMode.PKCS7);
        return Convert.ToBase64String(cipherBytes);
    }

    /// <summary>
    /// Decrypts a Base64 ciphertext.
    /// <para>
    /// If the value is not Base64 or cannot be decrypted it is returned <b>as is</b>. This keeps rows that
    /// were migrated from the legacy system but not yet encrypted from crashing the application
    /// (gradual migration tolerance).
    /// </para>
    /// </summary>
    public static string Decrypt(string? cipherText, byte[] key, byte[] iv)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return cipherText ?? string.Empty;
        }

        try
        {
            var cipherBytes = Convert.FromBase64String(cipherText);
            if (cipherBytes.Length == 0 || cipherBytes.Length % 16 != 0)
            {
                return cipherText;
            }

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var plainBytes = aes.DecryptCbc(cipherBytes, iv, PaddingMode.PKCS7);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (FormatException)
        {
            return cipherText;
        }
        catch (CryptographicException)
        {
            return cipherText;
        }
    }
}

/// <summary>
/// Fluent API shortcuts for <see cref="EncryptedStringConverter"/>.
/// </summary>
public static class EncryptedPropertyBuilderExtensions
{
    /// <summary>
    /// Configures the property as an encrypted column and sets the maximum length automatically according to
    /// its encrypted form.
    /// </summary>
    /// <param name="propertyBuilder">The property being configured.</param>
    /// <param name="plainMaxLength">Maximum number of characters of the plaintext.</param>
    /// <remarks>
    /// The same method is used for <c>string?</c> properties as well; a nullable annotation does not create
    /// a distinct type at runtime.
    /// </remarks>
    public static PropertyBuilder<string> IsEncrypted(
        this PropertyBuilder<string> propertyBuilder,
        int plainMaxLength)
        => propertyBuilder
            .HasConversion(new EncryptedStringConverter())
            .HasMaxLength(EncryptedStringConverter.EncryptedMaxLength(plainMaxLength));
}
