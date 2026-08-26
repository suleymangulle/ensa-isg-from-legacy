using AutoMapper;
using Ensa.Application.Contracts.Documents.Dtos;
using Ensa.Domain.Documents;

namespace Ensa.Application.Documents;

/// <summary>
/// Mappings for the document module (document metadata, forms, archive entries).
/// <para>
/// The binary-bearing members of <see cref="Document"/> - <c>Content</c>,
/// <c>StorageName</c> and <c>StoragePath</c> - are never written from a request payload.
/// They are storage coordinates owned by the storage layer, so the input maps ignore them
/// explicitly and the app service assigns the storage name itself.
/// </para>
/// </summary>
public class DocumentsAutoMapperProfile : Profile
{
    public DocumentsAutoMapperProfile()
    {
        // --------------------------------------------------------- Document

        CreateMap<Document, DocumentDto>();

        CreateMap<Document, DocumentListDto>();

        CreateMap<DocumentInputDto, Document>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore())
            // Storage coordinates - owned by the storage layer, never by the caller.
            .ForMember(d => d.Content, o => o.Ignore())
            .ForMember(d => d.StorageName, o => o.Ignore())
            .ForMember(d => d.StoragePath, o => o.Ignore());

        CreateMap<CreateDocumentDto, Document>().IncludeBase<DocumentInputDto, Document>();

        CreateMap<UpdateDocumentDto, Document>().IncludeBase<DocumentInputDto, Document>();

        // ------------------------------------------------------------- Form

        CreateMap<Form, FormDto>();

        CreateMap<Form, FormListDto>();

        CreateMap<FormInputDto, Form>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore());

        CreateMap<CreateFormDto, Form>().IncludeBase<FormInputDto, Form>();

        CreateMap<UpdateFormDto, Form>().IncludeBase<FormInputDto, Form>();

        // ---------------------------------------------------------- Archive

        CreateMap<Archive, ArchiveDto>();

        CreateMap<Archive, ArchiveListDto>();

        CreateMap<ArchiveInputDto, Archive>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreationTime, o => o.Ignore())
            .ForMember(d => d.CreatorId, o => o.Ignore())
            .ForMember(d => d.LastModificationTime, o => o.Ignore())
            .ForMember(d => d.LastModifierId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletionTime, o => o.Ignore())
            .ForMember(d => d.DeleterId, o => o.Ignore())
            // Migration-only columns: they record what the pre-migration system said and must
            // never be rewritten through the API.
            .ForMember(d => d.PreviousAddDate, o => o.Ignore())
            .ForMember(d => d.PreviousAddedByUserId, o => o.Ignore());

        CreateMap<CreateArchiveDto, Archive>().IncludeBase<ArchiveInputDto, Archive>();

        CreateMap<UpdateArchiveDto, Archive>().IncludeBase<ArchiveInputDto, Archive>();
    }
}
