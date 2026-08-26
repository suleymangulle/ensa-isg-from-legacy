import { useQuery } from '@tanstack/react-query'
import { http, type ListResult, type PagedResult } from '@/api/http'
import type { DocumentOwnerType, LookupDto } from '@/api/endpoints'

/**
 * Data layer of the documents module — documents, forms and the module archive.
 *
 * The types below mirror `Ensa.Application.Contracts/Documents/Dtos` one to one: the API
 * serialises property names camelCase and enums as numbers, so the C# property set *is* the
 * JSON contract. They live here rather than in `src/api/endpoints.ts` because that file is
 * shared by every module and only carries the DTOs several modules need.
 *
 * Note on the binary: `api/document` manages **metadata only**. There is no upload and no
 * download route, and `StorageName` / `StoragePath` are deliberately absent from `DocumentDto`
 * — handing the storage coordinates out would invite path traversal and direct object access.
 * The payload itself is served by `GET api/document/{id}/content`, always as an
 * attachment; see `documentContentPath` below.
 */

// ---------------------------------------------------------------
// Resources — `api/{resource}`, kebab-cased controller names.
// ---------------------------------------------------------------

/** `api/document` — see `DocumentController`. */
export const DOCUMENT = 'document'

/** `api/form` — see `FormController`. */
export const FORM = 'form'

/** `api/archive` — see `ArchiveController`. */
export const ARCHIVE = 'archive'

/** `api/company` — the workplace drop-down shared by all three screens. */
export const COMPANY = 'company'

// ---------------------------------------------------------------
// Document DTOs
// ---------------------------------------------------------------

/** `DocumentListDto` — the row of `GET api/document`. */
export interface DocumentListDto {
  id: number
  documentName: string
  extension?: string | null
  contentType?: string | null
  sizeBytes: number
  documentCategoryId?: number | null
  companyId?: number | null
  ownerType: DocumentOwnerType
  ownerRecordId?: number | null
  isActive: boolean
  creationTime: string
}

/** `DocumentDto` — `GET api/document/{id}`. Storage coordinates are intentionally missing. */
export interface DocumentDto {
  id: number
  tenantId?: number | null
  documentCategoryId?: number | null
  companyId?: number | null
  documentName: string
  extension?: string | null
  contentType?: string | null
  sizeBytes: number
  sha256?: string | null
  ownerType: DocumentOwnerType
  ownerRecordId?: number | null
  isActive: boolean
  creationTime: string
  lastModificationTime?: string | null
}

/** `CreateDocumentDto` / `UpdateDocumentDto` — the same field set on both. */
export interface SaveDocumentDto {
  documentName: string
  documentCategoryId?: number | null
  companyId?: number | null
  extension?: string | null
  contentType?: string | null
  sizeBytes: number
  sha256?: string | null
  ownerType: DocumentOwnerType
  ownerRecordId?: number | null
  isActive: boolean
}

/** `DocumentNavigationDto` — `GET api/document/{id}/detail`. */
export interface DocumentNavigationDto {
  document: DocumentDto
  category?: LookupDto | null
  company?: LookupDto | null
}

/** `GetDocumentListInput`. */
export interface DocumentListInput {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  documentCategoryId?: number
  companyId?: number
  ownerType?: DocumentOwnerType
  ownerRecordId?: number
  isActive?: boolean
}

// ---------------------------------------------------------------
// Form DTOs
// ---------------------------------------------------------------

/** `FormListDto` — the row of `GET api/form`. */
export interface FormListDto {
  id: number
  formName: string
  categoryId: number
  documentId?: number | null
  isActive: boolean
  defaultForm: boolean
}

/** `FormDto` — `GET api/form/{id}`. */
export interface FormDto {
  id: number
  tenantId?: number | null
  formName: string
  documentId?: number | null
  categoryId: number
  isActive: boolean
  defaultForm: boolean
  creationTime: string
  lastModificationTime?: string | null
}

/** `CreateFormDto` / `UpdateFormDto`. */
export interface SaveFormDto {
  formName: string
  categoryId: number
  documentId?: number | null
  defaultForm: boolean
  isActive: boolean
}

/** `GetFormListInput`. */
export interface FormListInput {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  categoryId?: number
  isActive?: boolean
  defaultForm?: boolean
}

// ---------------------------------------------------------------
// Archive DTOs
// ---------------------------------------------------------------

/** `ArchiveListDto` — the row of `GET api/archive`. */
export interface ArchiveListDto {
  id: number
  moduleType: DocumentOwnerType
  moduleId: number
  documentId: number
  companyId: number
  month?: number | null
  year?: number | null
  description?: string | null
  creationTime: string
}

/** `ArchiveDto` — `GET api/archive/{id}`. */
export interface ArchiveDto {
  id: number
  tenantId?: number | null
  moduleType: DocumentOwnerType
  moduleId: number
  documentId: number
  companyId: number
  lineId?: number | null
  month?: number | null
  year?: number | null
  description?: string | null
  moduleDescription?: string | null
  previousAddDate?: string | null
  previousAddedByUserId?: number | null
  creationTime: string
}

/** `CreateArchiveDto` / `UpdateArchiveDto`. */
export interface SaveArchiveDto {
  moduleType: DocumentOwnerType
  moduleId: number
  documentId: number
  companyId: number
  lineId?: number | null
  month?: number | null
  year?: number | null
  description?: string | null
  moduleDescription?: string | null
}

/** `GetArchiveListInput`. */
export interface ArchiveListInput {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  moduleType?: DocumentOwnerType
  moduleId?: number
  companyId?: number
  month?: number
  year?: number
}

// ---------------------------------------------------------------
// Queries
// ---------------------------------------------------------------

/** Drops empty values so an unset filter never reaches the query string. */
function clean(input: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(
    Object.entries(input).filter(
      ([, value]) => value !== undefined && value !== null && value !== '',
    ),
  )
}

/** `GET api/document` */
export function useDocumentList(input: DocumentListInput) {
  return useQuery({
    queryKey: [DOCUMENT, 'list', input],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<DocumentListDto>>(`/${DOCUMENT}`, {
        params: clean({ ...input }),
      })
      return data
    },
  })
}

/** `GET api/document/{id}/detail` */
export function useDocumentDetail(id: number | undefined) {
  return useQuery({
    queryKey: [DOCUMENT, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<DocumentNavigationDto>(`/${DOCUMENT}/${id}/detail`)
      return data
    },
  })
}

/**
 * `GET api/document/by-hash/{sha256}` — duplicate detection.
 *
 * The route answers `204 No Content` when nothing matches, which axios surfaces as an empty
 * body rather than as an error, so the empty case is normalised to `null` here. The query only
 * runs once a full 64-character digest is available; a partial digest would be a wasted round
 * trip that can never match.
 */
export function useDocumentByHash(sha256: string | undefined) {
  const digest = sha256?.trim().toLowerCase()
  const isComplete = !!digest && /^[0-9a-f]{64}$/.test(digest)

  return useQuery({
    queryKey: [DOCUMENT, 'by-hash', digest],
    enabled: isComplete,
    queryFn: async () => {
      const { data } = await http.get<DocumentDto | ''>(`/${DOCUMENT}/by-hash/${digest}`)
      return data && typeof data === 'object' ? data : null
    },
  })
}

/** `GET api/document/by-owner/{ownerType}/{ownerId}` */
export function useDocumentsByOwner(
  ownerType: DocumentOwnerType | undefined,
  ownerId: number | undefined,
) {
  return useQuery({
    queryKey: [DOCUMENT, 'by-owner', ownerType, ownerId],
    enabled: ownerType !== undefined && !!ownerId,
    queryFn: async () => {
      const { data } = await http.get<ListResult<DocumentListDto>>(
        `/${DOCUMENT}/by-owner/${ownerType}/${ownerId}`,
      )
      return data
    },
  })
}

/** `GET api/form` */
export function useFormList(input: FormListInput) {
  return useQuery({
    queryKey: [FORM, 'list', input],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<FormListDto>>(`/${FORM}`, {
        params: clean({ ...input }),
      })
      return data
    },
  })
}

/** `GET api/archive` */
export function useArchiveList(input: ArchiveListInput) {
  return useQuery({
    queryKey: [ARCHIVE, 'list', input],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<ArchiveListDto>>(`/${ARCHIVE}`, {
        params: clean({ ...input }),
      })
      return data
    },
  })
}

/**
 * `GET api/archive/by-module/{moduleType}/{moduleId}`
 *
 * Both path segments are mandatory, so the query stays disabled until the screen has supplied a
 * module type *and* a record id — firing without them is the 400 this guard exists to avoid.
 */
export function useArchiveByModule(
  moduleType: DocumentOwnerType | undefined,
  moduleId: number | undefined,
  month?: number,
  year?: number,
) {
  return useQuery({
    queryKey: [ARCHIVE, 'by-module', moduleType, moduleId, month, year],
    enabled: moduleType !== undefined && !!moduleId,
    queryFn: async () => {
      const { data } = await http.get<ListResult<ArchiveListDto>>(
        `/${ARCHIVE}/by-module/${moduleType}/${moduleId}`,
        { params: clean({ month, year }) },
      )
      return data
    },
  })
}

/** `GET api/company/lookup` — the workplace drop-down. */
export function useCompanyLookup(filter?: string) {
  return useQuery({
    queryKey: [COMPANY, 'lookup', filter],
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(`/${COMPANY}/lookup`, {
        params: clean({ filter }),
      })
      return data
    },
  })
}

/**
 * Path of a document's payload.
 *
 * The route requires the bearer token, so it is fetched through the shared axios instance
 * rather than linked to directly; see `downloadFile` in `@/api/download`.
 */
export function documentContentPath(id: number): string {
  return `/${DOCUMENT}/${id}/content`
}
