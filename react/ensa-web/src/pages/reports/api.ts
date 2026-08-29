import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { http, type ListResult, type PagedResult } from '@/api/http'
import { useOfficeScopeKey } from '@/auth/OfficeContext'
import type { LookupDto } from '@/api/endpoints'
import type {
  ActivityReportLineType,
  ActivityReportType,
  AssignmentType,
  HazardClass,
  StaffRole,
} from '@/api/enums'

/**
 * Data layer of the statutory reporting module.
 *
 * The types below mirror `Ensa.Application.Contracts/Reports/Dtos` one to one — the API
 * serialises property names camelCase and enums as numbers, so the C# property set *is* the
 * JSON contract. They live here rather than in `src/api/endpoints.ts` because that file is
 * shared and only carries the DTOs several modules need.
 *
 * The shared `usePagedList` helper forwards `skipCount`, `maxResultCount`, `sorting` and
 * `filter` only; all three list endpoints here accept a richer input (office, workplace, staff
 * role, report type, date range), so the module declares its own query hooks. Their cache keys
 * still start with the resource name, which is exactly what `useCreate` / `useUpdate` /
 * `useDelete` from `@/api/mutations` invalidate — writes therefore refresh these lists.
 */

// ---------------------------------------------------------------
// Resources
//
// `EnsaController` maps the `[controller]` token through a kebab-case transformer, so
// `OhsReportController` serves `api/ohs-report`. Verified against the running Swagger document.
// ---------------------------------------------------------------

export const REPORT_ENDPOINTS = {
  ohsReport: 'ohs-report',
  activityReport: 'activity-report',
  yearEndReviewReport: 'year-end-review-report',
  company: 'company',
  office: 'office',
  user: 'user',
} as const

// ---------------------------------------------------------------
// OHS report DTOs
// ---------------------------------------------------------------

/** `OhsReportListDto` — row of `GET api/ohs-report`. */
export interface OhsReportListDto {
  id: number
  officeId: number
  nationalId: string
  employeeName: string
  staffRole: StaffRole
  dutyType: AssignmentType
  totalMinutes: number
  usedMonthlyMinutes: number
  creationTime: string
}

/** `OhsReportDto` — the service-time record behind `GET api/ohs-report/{id}`. */
export interface OhsReportDto {
  id: number
  tenantId?: number | null
  creationTime: string
  creatorId?: number | null
  officeId: number
  /** The archive detail record the report was produced from. */
  moduleArchiveDetailId: number
  nationalId: string
  employeeName: string
  staffRole: StaffRole
  dutyType: AssignmentType
  totalMonthlyOvertimeDuration: number
  totalMinutes: number
  usedMonthlyMinutes: number
}

/** `OhsReportHazardClassBreakdownDto` — one hazard-class bucket of an OHS report. */
export interface OhsReportHazardClassBreakdownDto {
  hazardClass: HazardClass
  /** Workplaces of this hazard class covered by the report. */
  companyCount: number
}

/** `GetOhsReportListInput`. */
export interface OhsReportListInput {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  officeId?: number
  staffRole?: StaffRole
  dutyType?: AssignmentType
  startDate?: string
  endDate?: string
}

// ---------------------------------------------------------------
// Activity report DTOs
// ---------------------------------------------------------------

/** `ActivityReportListDto` — row of `GET api/activity-report`. */
export interface ActivityReportListDto {
  id: number
  companyId: number
  /** Resolved server-side with one batched query per page. */
  companyName?: string | null
  reportType: ActivityReportType
  reportName: string
  reportStart: string
  reportEnd: string
}

/** `ActivityReportDto` — the header behind `GET api/activity-report/{id}`. */
export interface ActivityReportDto {
  id: number
  tenantId?: number | null
  creationTime?: string | null
  creatorId?: number | null
  lastModificationTime?: string | null
  companyId: number
  reportType: ActivityReportType
  reportName: string
  reportStart: string
  reportEnd: string
}

/** `CreateActivityReportDto` / `UpdateActivityReportDto`. */
export interface SaveActivityReportDto {
  companyId: number
  reportType: ActivityReportType
  reportName: string
  reportStart: string
  reportEnd: string
}

/** `ActivityReportLineDto` — one typed data row of a report. */
export interface ActivityReportLineDto {
  id: number
  tenantId?: number | null
  creationTime?: string | null
  creatorId?: number | null
  activityReportId: number
  lineType: ActivityReportLineType
  text?: string | null
  value1?: string | null
  value2?: string | null
  value3?: string | null
  orderNo: number
}

/** `CreateActivityReportLineDto` / `UpdateActivityReportLineDto`. */
export interface SaveActivityReportLineDto {
  lineType: ActivityReportLineType
  text?: string | null
  value1?: string | null
  value2?: string | null
  value3?: string | null
  /** Display order. Zero means "append to the end". */
  orderNo: number
}

/** `ActivityReportNavigationDto` — everything the detail screen needs in one round trip. */
export interface ActivityReportNavigationDto {
  activityReport: ActivityReportDto
  company?: LookupDto | null
  lines: ActivityReportLineDto[]
}

/** `GetActivityReportListInput`. */
export interface ActivityReportListInput {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  companyId?: number
  reportType?: ActivityReportType
  startDate?: string
  endDate?: string
}

// ---------------------------------------------------------------
// Year-end review report DTOs
// ---------------------------------------------------------------

/** `YearEndReviewReportListDto` — row of `GET api/year-end-review-report`. */
export interface YearEndReviewReportListDto {
  id: number
  reportTitle: string
  companyId: number
  /** Resolved server-side with one batched query per page. */
  companyName?: string | null
  reportDate: string
  specialistFullName?: string | null
  physicianFullName?: string | null
  isActive: boolean
}

/** `YearEndReviewReportDto` — the header behind `GET api/year-end-review-report/{id}`. */
export interface YearEndReviewReportDto {
  id: number
  tenantId?: number | null
  creationTime?: string | null
  lastModificationTime?: string | null
  reportTitle: string
  companyId: number
  maleWorker?: number | null
  femaleWorker?: number | null
  childWorker?: number | null
  youngWorker?: number | null
  reportDate: string
  specialistUserId?: number | null
  /** Captured at authoring time so the report text survives user deletion. */
  specialistFullName?: string | null
  physicianUserId?: number | null
  physicianFullName?: string | null
  deputyFullName?: string | null
  isActive: boolean
}

/** `CreateYearEndReviewReportDto`; `isActive` is accepted by the update input only. */
export interface SaveYearEndReviewReportDto {
  reportTitle: string
  companyId: number
  maleWorker?: number | null
  femaleWorker?: number | null
  childWorker?: number | null
  youngWorker?: number | null
  reportDate: string
  specialistUserId?: number | null
  specialistFullName?: string | null
  physicianUserId?: number | null
  physicianFullName?: string | null
  deputyFullName?: string | null
  isActive?: boolean
}

/**
 * `YearEndReviewLineDto` — one work item. Items form a tree through `parentLineId`.
 *
 * `personAndTitle` and `resultAndComment` keep the property names the backend contract uses;
 * they are not typos on this side.
 */
export interface YearEndReviewLineDto {
  id: number
  tenantId?: number | null
  yearEndReviewReportId: number
  orderNo: number
  date?: string | null
  work?: string | null
  personAndTitle?: string | null
  repeatCount?: string | null
  usedMethod?: string | null
  resultAndComment?: string | null
  isActive: boolean
  /** Parent work item; `null` marks a root-level item. */
  parentLineId?: number | null
}

/** `CreateYearEndReviewLineDto`; `isActive` is accepted by the update input only. */
export interface SaveYearEndReviewLineDto {
  parentLineId?: number | null
  orderNo: number
  date?: string | null
  work?: string | null
  personAndTitle?: string | null
  repeatCount?: string | null
  usedMethod?: string | null
  resultAndComment?: string | null
  isActive?: boolean
}

/** `YearEndReviewLineNavigationDto` — one node of the work item tree. */
export interface YearEndReviewLineNavigationDto {
  line: YearEndReviewLineDto
  childActivities: YearEndReviewLineNavigationDto[]
}

/** `YearEndReviewReportNavigationDto` — the combined detail view. */
export interface YearEndReviewReportNavigationDto {
  yearEndReviewReport: YearEndReviewReportDto
  company?: LookupDto | null
  /** Root-level work items; each carries its own subtree. */
  activities: YearEndReviewLineNavigationDto[]
}

/** `GetYearEndReviewReportListInput`. */
export interface YearEndReviewReportListInput {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  companyId?: number
  specialistUserId?: number
  physicianUserId?: number
  isActive?: boolean
  startDate?: string
  endDate?: string
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

/** `GET api/ohs-report` */
export function useOhsReportList(input: OhsReportListInput) {
  // OHS reports are filed per office, so the office the user is working in is part of the answer
  // and therefore part of the key.
  const officeScope = useOfficeScopeKey()

  return useQuery({
    queryKey: [REPORT_ENDPOINTS.ohsReport, 'list', officeScope, input],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<OhsReportListDto>>(
        `/${REPORT_ENDPOINTS.ohsReport}`,
        { params: clean({ ...input }) },
      )
      return data
    },
  })
}

/** `GET api/ohs-report/{id}` */
export function useOhsReport(id: number | undefined) {
  return useQuery({
    queryKey: [REPORT_ENDPOINTS.ohsReport, 'entity', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<OhsReportDto>(`/${REPORT_ENDPOINTS.ohsReport}/${id}`)
      return data
    },
  })
}

/**
 * `GET api/ohs-report/office/{officeId}?from&to`
 *
 * The office is part of the route, so the query stays disabled until the screen has one —
 * firing without it would only produce a request to an unroutable URL.
 */
export function useOfficeOhsReports(officeId: number | undefined, from?: string, to?: string) {
  return useQuery({
    queryKey: [REPORT_ENDPOINTS.ohsReport, 'office', officeId, from, to],
    enabled: !!officeId,
    queryFn: async () => {
      const { data } = await http.get<ListResult<OhsReportDto>>(
        `/${REPORT_ENDPOINTS.ohsReport}/office/${officeId}`,
        { params: clean({ from, to }) },
      )
      return data
    },
  })
}

/**
 * `GET api/ohs-report/{id}/hazard-class-breakdown`
 *
 * One request for the report the user picked — never one per table row.
 */
export function useHazardClassBreakdown(reportId: number | undefined) {
  return useQuery({
    queryKey: [REPORT_ENDPOINTS.ohsReport, 'hazard-class-breakdown', reportId],
    enabled: !!reportId,
    queryFn: async () => {
      const { data } = await http.get<ListResult<OhsReportHazardClassBreakdownDto>>(
        `/${REPORT_ENDPOINTS.ohsReport}/${reportId}/hazard-class-breakdown`,
      )
      return data
    },
  })
}

/** `GET api/activity-report` */
export function useActivityReportList(input: ActivityReportListInput) {
  return useQuery({
    queryKey: [REPORT_ENDPOINTS.activityReport, 'list', input],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<ActivityReportListDto>>(
        `/${REPORT_ENDPOINTS.activityReport}`,
        { params: clean({ ...input }) },
      )
      return data
    },
  })
}

/** `GET api/activity-report/{id}/detail` */
export function useActivityReportDetail(id: number | undefined) {
  return useQuery({
    queryKey: [REPORT_ENDPOINTS.activityReport, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<ActivityReportNavigationDto>(
        `/${REPORT_ENDPOINTS.activityReport}/${id}/detail`,
      )
      return data
    },
  })
}

/** `GET api/activity-report/{id}/lines` — the flat row list, in display order. */
export function useActivityReportLines(id: number | undefined) {
  return useQuery({
    queryKey: [REPORT_ENDPOINTS.activityReport, 'lines', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<ListResult<ActivityReportLineDto>>(
        `/${REPORT_ENDPOINTS.activityReport}/${id}/lines`,
      )
      return data
    },
  })
}

/** `GET api/year-end-review-report` */
export function useYearEndReviewList(input: YearEndReviewReportListInput) {
  return useQuery({
    queryKey: [REPORT_ENDPOINTS.yearEndReviewReport, 'list', input],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<YearEndReviewReportListDto>>(
        `/${REPORT_ENDPOINTS.yearEndReviewReport}`,
        { params: clean({ ...input }) },
      )
      return data
    },
  })
}

/** `GET api/year-end-review-report/{id}/detail` */
export function useYearEndReviewDetail(id: number | undefined) {
  return useQuery({
    queryKey: [REPORT_ENDPOINTS.yearEndReviewReport, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<YearEndReviewReportNavigationDto>(
        `/${REPORT_ENDPOINTS.yearEndReviewReport}/${id}/detail`,
      )
      return data
    },
  })
}

/**
 * `GET api/year-end-review-report/company/{companyId}/current`
 *
 * The workplace is part of the route, so the query waits for one. The endpoint answers
 * `204 No Content` when the workplace has no report yet, which arrives as an empty body —
 * normalised to `null` here.
 */
export function useCurrentYearEndReview(companyId: number | undefined) {
  return useQuery({
    queryKey: [REPORT_ENDPOINTS.yearEndReviewReport, 'current', companyId],
    enabled: !!companyId,
    queryFn: async () => {
      const { data } = await http.get<YearEndReviewReportDto | ''>(
        `/${REPORT_ENDPOINTS.yearEndReviewReport}/company/${companyId}/current`,
      )
      return data ? data : null
    },
  })
}

/**
 * `GET api/company/lookup` — the workplace drop-down.
 *
 * The same query also resolves the workplace names of the list rows: neither
 * `ActivityReportListDto` nor `YearEndReviewReportListDto` carries a company name, and one
 * request per row would be indefensible. The lookup is capped server-side, so a row outside
 * the cap falls back to its id.
 */
export function useCompanyLookup(filter?: string) {
  // The workplace lookup is office-scoped server-side; keying on the scope stops one office's
  // picker from being served out of another's cache.
  const officeScope = useOfficeScopeKey()

  return useQuery({
    queryKey: [REPORT_ENDPOINTS.company, 'lookup', officeScope, filter],
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(
        `/${REPORT_ENDPOINTS.company}/lookup`,
        { params: clean({ filter }) },
      )
      return data
    },
  })
}

/** `GET api/office/lookup` — the office drop-down of the OHS report screen. */
export function useOfficeLookup(filter?: string) {
  return useQuery({
    queryKey: [REPORT_ENDPOINTS.office, 'lookup', filter],
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(`/${REPORT_ENDPOINTS.office}/lookup`, {
        params: clean({ filter }),
      })
      return data
    },
  })
}

/** `GET api/user/lookup` — the specialist and physician drop-downs. */
export function useUserLookup(filter?: string) {
  return useQuery({
    queryKey: [REPORT_ENDPOINTS.user, 'lookup', filter],
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(`/${REPORT_ENDPOINTS.user}/lookup`, {
        params: clean({ filter }),
      })
      return data
    },
  })
}

// ---------------------------------------------------------------
// Mutations
//
// `src/api/mutations.ts` covers `POST /{resource}`, `PUT /{resource}/{id}` and
// `DELETE /{resource}/{id}`; the line sub-resources need their own, so they are written here
// and invalidate the very same `[resource]` key the query hooks above are filed under.
// ---------------------------------------------------------------

/** Invalidates every query of a resource — lists, detail views and line lists alike. */
function useResourceInvalidation(resource: string) {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: [resource] })
}

/** `POST api/activity-report/{id}/lines` */
export function useAddActivityReportLine(reportId: number) {
  const invalidate = useResourceInvalidation(REPORT_ENDPOINTS.activityReport)

  return useMutation({
    mutationFn: async (input: SaveActivityReportLineDto) => {
      const { data } = await http.post<ActivityReportLineDto>(
        `/${REPORT_ENDPOINTS.activityReport}/${reportId}/lines`,
        input,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `PUT api/activity-report/{id}/lines/{lineId}` */
export function useUpdateActivityReportLine(reportId: number) {
  const invalidate = useResourceInvalidation(REPORT_ENDPOINTS.activityReport)

  return useMutation({
    mutationFn: async ({ lineId, input }: { lineId: number; input: SaveActivityReportLineDto }) => {
      const { data } = await http.put<ActivityReportLineDto>(
        `/${REPORT_ENDPOINTS.activityReport}/${reportId}/lines/${lineId}`,
        input,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `DELETE api/activity-report/{id}/lines/{lineId}` */
export function useRemoveActivityReportLine(reportId: number) {
  const invalidate = useResourceInvalidation(REPORT_ENDPOINTS.activityReport)

  return useMutation({
    mutationFn: async (lineId: number) => {
      await http.delete(`/${REPORT_ENDPOINTS.activityReport}/${reportId}/lines/${lineId}`)
    },
    onSuccess: invalidate,
  })
}

/** `POST api/year-end-review-report/{id}/lines` */
export function useAddYearEndReviewLine(reportId: number) {
  const invalidate = useResourceInvalidation(REPORT_ENDPOINTS.yearEndReviewReport)

  return useMutation({
    mutationFn: async (input: SaveYearEndReviewLineDto) => {
      const { data } = await http.post<YearEndReviewLineDto>(
        `/${REPORT_ENDPOINTS.yearEndReviewReport}/${reportId}/lines`,
        input,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `PUT api/year-end-review-report/{id}/lines/{lineId}` — re-parenting is checked server-side. */
export function useUpdateYearEndReviewLine(reportId: number) {
  const invalidate = useResourceInvalidation(REPORT_ENDPOINTS.yearEndReviewReport)

  return useMutation({
    mutationFn: async ({ lineId, input }: { lineId: number; input: SaveYearEndReviewLineDto }) => {
      const { data } = await http.put<YearEndReviewLineDto>(
        `/${REPORT_ENDPOINTS.yearEndReviewReport}/${reportId}/lines/${lineId}`,
        input,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `DELETE api/year-end-review-report/{id}/lines/{lineId}` — removes the whole subtree. */
export function useRemoveYearEndReviewLine(reportId: number) {
  const invalidate = useResourceInvalidation(REPORT_ENDPOINTS.yearEndReviewReport)

  return useMutation({
    mutationFn: async (lineId: number) => {
      await http.delete(`/${REPORT_ENDPOINTS.yearEndReviewReport}/${reportId}/lines/${lineId}`)
    },
    onSuccess: invalidate,
  })
}
