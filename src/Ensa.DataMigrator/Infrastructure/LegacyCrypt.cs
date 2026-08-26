using System.Text;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace Ensa.DataMigrator.Infrastructure;

/// <summary>
/// Reads values the legacy application encrypted, so they can be stored as themselves again.
/// <para>
/// <b>Why this is needed.</b> Several legacy columns are already ciphertext:
/// <c>Kullanici_T.TCKimlikNo</c> (3,468 rows), <c>MedulaKullanici</c>, <c>MedulaSifre</c> and the
/// whole of <c>PeriyodikMuayeneFormu_T</c>. They look like ordinary text and read like ordinary
/// text, and a migration that treats them as such re-encrypts ciphertext: the value is then
/// unreadable by anything, and nothing complains. The first tenancy run did exactly that to 3,878
/// identity numbers — it was caught only because the doubly-encrypted values no longer fitted the
/// destination column, and the fitter said so.
/// </para>
/// <para>
/// <b>How they were encrypted.</b> <c>Utility/Crypt/CryptHelper.cs</c>: Rijndael, 256-bit key and
/// <b>256-bit block</b>, CBC with PKCS7, the key derived from a passphrase with PBKDF2 over a fixed
/// salt, 1,000 iterations. The salt and IV are the same fixed array — which is why every value
/// starts with the same 88 characters and why equal plaintexts look equal. The stored form is
/// Base64 of <c>salt(32) || iv(32) || ciphertext</c>.
/// </para>
/// <para>
/// <b>Why BouncyCastle.</b> A 256-bit block is Rijndael but not AES, and .NET's <c>Aes</c> only
/// does 128-bit blocks; <c>RijndaelManaged</c> was removed years ago. There is no way to read this
/// data with the framework's own primitives.
/// </para>
/// <para>
/// Nothing here encrypts. This class exists to <i>read</i> a legacy value once, so it can be
/// written back through the modern converter under the modern key.
/// </para>
/// </summary>
public static class LegacyCrypt
{
    /// <summary>
    /// The passphrase from <c>Businness/Constants/AllConstants.cs</c>.
    /// <para>
    /// It is a compile-time constant in the legacy source, which is to say it is not a secret from
    /// anyone who has the source. It is repeated here because reading the old data is impossible
    /// without it; it protects nothing that is not already protected by access to the database.
    /// </para>
    /// </summary>
    private const string PassPhrase = "enc-ens-crypt-654-hdf";

    /// <summary>Fixed salt and IV, both the same array in the legacy implementation.</summary>
    private static readonly byte[] Salt =
    [
        5, 54, 98, 45, 5, 5, 8, 5, 6, 4, 8, 5, 46, 87, 5, 45,
        46, 87, 8, 51, 64, 8, 4, 16, 84, 98, 51, 32, 51, 5, 87, 98
    ];

    private const int KeySizeBytes = 32;
    private const int BlockSizeBits = 256;
    private const int DerivationIterations = 1000;

    /// <summary>Length of the salt and IV the legacy format prepends before the ciphertext.</summary>
    private const int PrefixLength = 64;

    /// <summary>
    /// The Base64 prefix every legacy ciphertext begins with, because the salt and IV are fixed.
    /// Used to tell an encrypted value from a plain one without attempting a decryption.
    /// </summary>
    public static readonly string CipherPrefix = Convert.ToBase64String(Salt.Concat(Salt).ToArray())[..8];

    private static readonly byte[] Key = DeriveKey();

    /// <summary>
    /// Whether the value looks like something this class encrypted.
    /// <para>
    /// The check is a prefix rather than a try/catch around a decryption: a column holds a mixture
    /// of encrypted and plain values — 3,468 of 3,878 identity numbers are encrypted and the rest
    /// are not — and a plain value must pass through untouched rather than be mangled by a
    /// decryption that half-succeeds.
    /// </para>
    /// </summary>
    public static bool LooksEncrypted(string? value)
        => value is not null
           && value.Length > PrefixLength
           && value.StartsWith(CipherPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Returns the plaintext of a legacy value, or the value itself when it was never encrypted.
    /// </summary>
    /// <returns>
    /// The decrypted text; the original value when it is not ciphertext; <c>null</c> when the value
    /// is ciphertext that cannot be read, which the caller must count rather than ignore.
    /// </returns>
    public static string? TryDecrypt(string? value)
    {
        var current = value;

        // Repeated, because the legacy application encrypted some rows twice - saving a row that
        // had been loaded without being decrypted first. One pass leaves ciphertext sitting in a
        // field that is meant to hold an identity number, and nothing looks wrong.
        for (var pass = 0; pass < MaximumPasses && LooksEncrypted(current); pass++)
        {
            current = DecryptOnce(current!);
        }

        // Still ciphertext after several passes is not a deeper onion; it is a value this key
        // cannot read, and the caller must be able to tell that from a plain value.
        return LooksEncrypted(current) ? null : current;
    }

    /// <summary>How many times a value may have been encrypted before it is treated as unreadable.</summary>
    private const int MaximumPasses = 4;

    private static string? DecryptOnce(string value)
    {
        try
        {
            var all = Convert.FromBase64String(value);
            if (all.Length <= PrefixLength)
            {
                return null;
            }

            var iv = all[KeySizeBytes..PrefixLength];
            var cipher = all[PrefixLength..];

            var engine = new CbcBlockCipher(new RijndaelEngine(BlockSizeBits));
            var blockCipher = new PaddedBufferedBlockCipher(engine, new Pkcs7Padding());
            blockCipher.Init(false, new ParametersWithIV(new KeyParameter(Key), iv));

            var output = new byte[blockCipher.GetOutputSize(cipher.Length)];
            var written = blockCipher.ProcessBytes(cipher, 0, cipher.Length, output, 0);
            written += blockCipher.DoFinal(output, written);

            return Encoding.UTF8.GetString(output, 0, written);
        }
        catch (Exception)
        {
            // A value that begins like ciphertext but will not decrypt is corrupt, or was written
            // under a different key. Returning null lets the caller record it; guessing would put
            // rubbish into a statutory field.
            return null;
        }
    }

    private static byte[] DeriveKey()
    {
        var generator = new Pkcs5S2ParametersGenerator(new Sha1Digest());
        generator.Init(Encoding.UTF8.GetBytes(PassPhrase), Salt, DerivationIterations);

        return ((KeyParameter)generator.GenerateDerivedMacParameters(KeySizeBytes * 8)).GetKey();
    }
}
