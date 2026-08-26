namespace Ensa.Domain.Shared;

/// <summary>Constants shared by every layer.</summary>
public static class EnsaDomainSharedConsts
{
    public const string DbSchema = "ensa";
    public const string DbTablePrefix = "";

    /// <summary>The <c>TenantId</c> value of a host record — one not owned by any tenant.</summary>
    public static readonly int? HostTenantId = null;

    public static class MaxLengths
    {
        public const int Code = 32;
        public const int ShortName = 64;
        public const int Name = 128;
        public const int LongName = 256;
        public const int Description = 512;
        public const int Text = 2000;
        public const int Note = 4000;
        public const int Email = 256;
        public const int Phone = 20;
        public const int NationalId = 11;
        public const int TaxNo = 11;
        public const int Address = 512;
        public const int Url = 512;
        public const int FileName = 260;
        public const int MimeType = 128;
        public const int Iban = 34;
        public const int Color = 16;
        public const int CssClass = 128;
        public const int Guid = 64;
        public const int Xml = -1; // nvarchar(max)
    }
}
