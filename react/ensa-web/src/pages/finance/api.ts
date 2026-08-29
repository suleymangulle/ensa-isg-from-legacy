import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { http, type ListResult, type PagedRequest, type PagedResult } from '@/api/http'
import { useOfficeScopeKey } from '@/auth/OfficeContext'
import type { LookupDto } from '@/api/endpoints'
import type {
  CashTransactionType,
  EmployeeCountRange,
  HazardClass,
  InvoiceType,
  SourceModule,
} from '@/api/enums'

/**
 * Data layer of the finance module — invoices, cash registers, statutory fines and the
 * fine-risk surveys built on top of them.
 *
 * The shared `usePagedList` only forwards `skipCount` / `maxResultCount` / `sorting` / `filter`,
 * while every list endpoint here takes a richer input (workplace, office, invoice type, date
 * range, hazard class, …), so the module declares its own query hooks. Cache keys still start
 * with the resource name, which is what `useCreate` / `useUpdate` / `useDelete` from
 * `@/api/mutations` invalidate — writes therefore refresh these lists.
 *
 * Money is never computed here. Totals, VAT, grand totals, the amount in words, register
 * balances and fine exposure all arrive on the DTO already calculated by the server.
 */

// ---------------------------------------------------------------
// Endpoints — verified against InvoiceController, CashRegisterController and
// PenaltyController, and cross-checked against the running Swagger document. `EnsaController`
// maps `[controller]` through a kebab-case token transformer, so `CashRegisterController`
// serves `api/cash-register`.
// ---------------------------------------------------------------

export const FINANCE_ENDPOINTS = {
  invoice: 'invoice',
  cashRegister: 'cash-register',
  penalty: 'penalty',
  /** Surveys are served by the penalty controller under a sub-route of their own. */
  penaltySurvey: 'penalty/surveys',
  company: 'company',
  office: 'office',
} as const

// ---------------------------------------------------------------
// Invoice DTOs — mirrored from Ensa.Application.Contracts/Finance/Dtos/InvoiceDtos.cs
// ---------------------------------------------------------------

/** `InvoiceListDto` — row of `GET api/invoice`. */
export interface InvoiceListDto {
  id: number
  invoiceNo: string
  companyId: number
  accountCurrentName: string
  invoiceDate: string
  invoiceType: InvoiceType
  sourceModule: SourceModule
  officeId?: number | null
  total: number
  vatTotal: number
  generalTotal: number
}

/** `InvoiceDto` — header behind `GET api/invoice/{id}`. */
export interface InvoiceDto {
  id: number
  tenantId?: number | null
  invoiceNo: string
  companyId: number
  invoiceDate: string
  invoiceType: InvoiceType
  sourceModule: SourceModule
  officeId?: number | null
  accountCurrentName: string
  invoiceDescription?: string | null
  /** Grand total spelled out in Turkish. Produced by `IInvoiceManager.AmountToWords`. */
  inWords?: string | null
  total: number
  vatTotal: number
  generalTotal: number
  creationTime?: string | null
}

/** `InvoiceLineDto` — one line of an invoice; every amount is server-computed. */
export interface InvoiceLineDto {
  id: number
  tenantId?: number | null
  invoiceId: number
  serviceItemId?: number | null
  lineDescription: string
  count: number
  unit: string
  unitPrice: number
  totalAmount: number
  vatRate: number
  vatAmount: number
  grossWithVatAmount: number
  companyId?: number | null
  orderNo: number
}

/**
 * `CreateInvoiceDto` / `UpdateInvoiceDto`.
 *
 * Lines are deliberately absent: an invoice is created as an empty header and its lines are
 * managed through the line endpoints, each of which re-runs the total calculation server-side.
 * `invoiceNo` may be omitted so the server allocates the next number from its atomic counter.
 */
export interface SaveInvoiceDto {
  invoiceNo?: string | null
  companyId: number
  invoiceDate: string
  invoiceType: InvoiceType
  sourceModule: SourceModule
  officeId?: number | null
  accountCurrentName: string
  invoiceDescription?: string | null
}

/** `CreateInvoiceLineDto` / `UpdateInvoiceLineDto`. Line and header totals are computed server-side. */
export interface SaveInvoiceLineDto {
  serviceItemId?: number | null
  lineDescription: string
  count: number
  unit: string
  unitPrice: number
  vatRate: number
  companyId?: number | null
  /** Display order on the invoice. Zero means "append to the end". */
  orderNo: number
}

/** `InvoiceLineNavigationDto` — a line together with the service card it was priced from. */
export interface InvoiceLineNavigationDto {
  line: InvoiceLineDto
  serviceItem?: LookupDto | null
}

/** `InvoiceNavigationDto` — `GET api/invoice/{id}/detail`, everything the print view needs. */
export interface InvoiceNavigationDto {
  invoice: InvoiceDto
  company?: LookupDto | null
  office?: LookupDto | null
  lines: InvoiceLineNavigationDto[]
}

/** `GetInvoiceListInput`. */
export interface GetInvoiceListInput extends PagedRequest {
  companyId?: number
  officeId?: number
  invoiceType?: InvoiceType
  sourceModule?: SourceModule
  startDate?: string
  endDate?: string
}

/** `CompanyBalanceDto` — outstanding invoice balance of one workplace. */
export interface CompanyBalanceDto {
  companyId: number
  /** Sales invoices minus purchase / return invoices. Positive means the workplace owes money. */
  balance: number
  calculatedAt: string
}

/** `GeneratedInvoiceNumberDto` — a freshly allocated, not-yet-persisted invoice number. */
export interface GeneratedInvoiceNumberDto {
  invoiceNo: string
  officeId?: number | null
  year: number
}

// ---------------------------------------------------------------
// Cash register DTOs — CashRegisterDtos.cs
// ---------------------------------------------------------------

/** `CashRegisterListDto` — row of `GET api/cash-register`. */
export interface CashRegisterListDto {
  id: number
  cashRegisterName: string
  officeId: number
  isHeadquarterCashRegister: boolean
  isActive: boolean
}

/** `CashRegisterDto`. */
export interface CashRegisterDto {
  id: number
  tenantId?: number | null
  cashRegisterName: string
  officeId: number
  isHeadquarterCashRegister: boolean
  isActive: boolean
}

/** `CreateCashRegisterDto` / `UpdateCashRegisterDto`. */
export interface SaveCashRegisterDto {
  cashRegisterName: string
  officeId: number
  isHeadquarterCashRegister: boolean
  /** Only read by the update endpoint; the create endpoint ignores it. */
  isActive?: boolean
}

/**
 * `CashTransactionDto` — one cash movement.
 *
 * The ledger is append-only: a movement is never edited and never hard-deleted, only voided,
 * after which `isActive` is `false` and it stops counting towards the balance.
 */
export interface CashTransactionDto {
  id: number
  tenantId?: number | null
  cashRegisterId: number
  paymentMethodId: number
  operationType: CashTransactionType
  operationAmount: number
  description?: string | null
  sourceModule: SourceModule
  sourceRecordId?: number | null
  exitItemId?: number | null
  operationDate?: string | null
  isActive: boolean
  creationTime?: string | null
}

/** `CreateCashTransactionDto`. There is no update counterpart by design. */
export interface CreateCashTransactionDto {
  cashRegisterId: number
  paymentMethodId: number
  operationType: CashTransactionType
  operationAmount: number
  description?: string | null
  sourceModule: SourceModule
  sourceRecordId?: number | null
  /** Expense category — only meaningful for an outflow. */
  exitItemId?: number | null
  operationDate?: string | null
}

/** `GetCashRegisterListInput`. */
export interface GetCashRegisterListInput extends PagedRequest {
  officeId?: number
  isHeadquarterCashRegister?: boolean
  isActive?: boolean
}

/** `GetCashTransactionListInput` — `cashRegisterId` is required; without it the API answers 400. */
export interface GetCashTransactionListInput extends PagedRequest {
  cashRegisterId: number
  operationType?: CashTransactionType
  sourceModule?: SourceModule
  startDate?: string
  endDate?: string
  includeVoided?: boolean
}

/** `CashRegisterBalanceDto` — balance at a point in time, voided movements excluded. */
export interface CashRegisterBalanceDto {
  cashRegisterId: number
  cashRegisterName: string
  balance: number
  asOf: string
}

/** `CashRegisterNavigationDto` — `GET api/cash-register/{id}/detail`. */
export interface CashRegisterNavigationDto {
  cashRegister: CashRegisterDto
  office?: LookupDto | null
  /** Balance at the moment of the query, computed by the server. */
  balance: number
  latestTransactions: CashTransactionDto[]
}

// ---------------------------------------------------------------
// Penalty DTOs — PenaltyDtos.cs
// ---------------------------------------------------------------

/** `PenaltyListDto` — row of `GET api/penalty`. */
export interface PenaltyListDto {
  id: number
  treeNodeCode?: string | null
  lawArticle: string
  penaltyArticle: string
  multiplierCalculate: boolean
  isActive: boolean
}

/** `PenaltyDto` — a host catalogue record, so it carries no tenant id. */
export interface PenaltyDto {
  id: number
  treeNodeCode?: string | null
  lawArticle: string
  penaltyArticle: string
  lawArticleReferencedOffence?: string | null
  /** Whether the amount is multiplied by the head count of the workplace. */
  multiplierCalculate: boolean
  isActive: boolean
}

/** `CreatePenaltyDto` / `UpdatePenaltyDto`. */
export interface SavePenaltyDto {
  treeNodeCode?: string | null
  lawArticle: string
  penaltyArticle: string
  lawArticleReferencedOffence?: string | null
  multiplierCalculate: boolean
  /** Only read by the update endpoint. */
  isActive?: boolean
}

/** `PenaltyAmountDto` — one cell of the hazard class x head-count band x year matrix. */
export interface PenaltyAmountDto {
  id: number
  penaltyId: number
  hazardClass: HazardClass
  employeeCountRange: EmployeeCountRange
  amount: number
  validityYear: number
}

/** `CreatePenaltyAmountDto` / `UpdatePenaltyAmountDto`. */
export interface SavePenaltyAmountDto {
  hazardClass: HazardClass
  employeeCountRange: EmployeeCountRange
  amount: number
  validityYear: number
}

/** `PenaltyNavigationDto` — `GET api/penalty/{id}/detail`. */
export interface PenaltyNavigationDto {
  penalty: PenaltyDto
  amounts: PenaltyAmountDto[]
}

/** `GetPenaltyListInput`. */
export interface GetPenaltyListInput extends PagedRequest {
  isActive?: boolean
  multiplierCalculate?: boolean
}

/** `ApplicablePenaltyAmountDto` — the amount that applies to one workplace profile in one year. */
export interface ApplicablePenaltyAmountDto {
  penaltyId: number
  hazardClass: HazardClass
  employeeCountRange: EmployeeCountRange
  year: number
  amount: number
}

// ---------------------------------------------------------------
// Penalty survey DTOs
// ---------------------------------------------------------------

/** `PenaltySurveyListDto` — row of `GET api/penalty/surveys`. */
export interface PenaltySurveyListDto {
  id: number
  companyTitle: string
  facilityName?: string | null
  hazardClass: HazardClass
  workerCount?: number | null
  creationTime: string
}

/** `PenaltySurveyDto` — survey header, filled in for a prospective customer. */
export interface PenaltySurveyDto {
  id: number
  tenantId?: number | null
  companyTitle: string
  facilityName?: string | null
  facilityOwner?: string | null
  facilityOwnerDuty?: string | null
  facilityOwnerGsm?: string | null
  employerNameLastName?: string | null
  phone?: string | null
  fax?: string | null
  email?: string | null
  cityId?: number | null
  districtId?: number | null
  neighborhoodId?: number | null
  address?: string | null
  invoiceAddress?: string | null
  taxOffice?: string | null
  taxNumber?: string | null
  workerCount?: number | null
  ssiRegistrationNumber?: string | null
  hazardClass: HazardClass
  logoDocumentId?: number | null
  creationTime?: string | null
}

/** `CreatePenaltySurveyDto` / `UpdatePenaltySurveyDto`. */
export interface SavePenaltySurveyDto {
  companyTitle: string
  facilityName?: string | null
  facilityOwner?: string | null
  facilityOwnerDuty?: string | null
  facilityOwnerGsm?: string | null
  employerNameLastName?: string | null
  phone?: string | null
  fax?: string | null
  email?: string | null
  cityId?: number | null
  districtId?: number | null
  neighborhoodId?: number | null
  address?: string | null
  invoiceAddress?: string | null
  taxOffice?: string | null
  taxNumber?: string | null
  workerCount?: number | null
  ssiRegistrationNumber?: string | null
  hazardClass: HazardClass
  logoDocumentId?: number | null
}

/** `PenaltySurveyLineDto` — one answered fine article inside a survey. */
export interface PenaltySurveyLineDto {
  id: number
  tenantId?: number | null
  penaltySurveyId: number
  penaltyId: number
  /** `true` means the workplace is in breach of this article. */
  surveyAnswer: boolean
  /** Resolved from the fine catalogue on the server; never taken from the client. */
  penaltyAmount: number
  multiplier: number
  multiplierCalculate: boolean
  creationTime?: string | null
}

/**
 * `CreatePenaltySurveyLineDto` / `UpdatePenaltySurveyLineDto`.
 *
 * The amount is intentionally absent: the server resolves it from the amount matrix using the
 * survey's own hazard class and head count, so a client cannot inflate a fine exposure figure.
 */
export interface SavePenaltySurveyLineDto {
  penaltyId: number
  surveyAnswer: boolean
  /** Year whose fine schedule applies. The server defaults to the current year. */
  year?: number | null
}

/** `GetPenaltySurveyListInput`. */
export interface GetPenaltySurveyListInput extends PagedRequest {
  hazardClass?: HazardClass
  cityId?: number
}

/** `GetPenaltySurveyLineListInput` — `penaltySurveyId` is required; without it the API answers 400. */
export interface GetPenaltySurveyLineListInput extends PagedRequest {
  penaltySurveyId: number
  surveyAnswer?: boolean
}

/** `PenaltySurveyTotalDto` — exposure computed from the answered lines. */
export interface PenaltySurveyTotalDto {
  penaltySurveyId: number
  lineCount: number
  violationCount: number
  /** Sum of the breached articles, head-count multiplier applied where applicable. */
  totalAmount: number
}

// ---------------------------------------------------------------
// Query helpers
// ---------------------------------------------------------------

/**
 * Drops empty values and upper-cases the first letter of every key, matching the PascalCase
 * property names the API model binder expects (`@/api/endpoints` does the same).
 *
 * Unlike a naive version this keeps `false`: `IsActive=false` and `SurveyAnswer=false` are
 * meaningful filters here, not "unset".
 */
function toQuery(input: Record<string, unknown>): Record<string, unknown> {
  const params: Record<string, unknown> = {}

  for (const [key, value] of Object.entries(input)) {
    if (value === undefined || value === null || value === '') continue
    params[key.charAt(0).toUpperCase() + key.slice(1)] = value
  }

  // `SkipCount` and `MaxResultCount` must survive even when they are zero / defaulted.
  params.SkipCount = input.skipCount ?? 0
  params.MaxResultCount = input.maxResultCount ?? 20
  return params
}

/**
 * Every finance list goes through here, and the office scope is part of the key.
 *
 * Invoices and cash registers are office-scoped server-side, so a page cached under one office must
 * never be handed to a screen reading under another. The scope is in the key rather than only in
 * the request, because the request is what produced the cached page in the first place.
 */
function usePagedResource<TRow, TInput extends PagedRequest>(
  resource: string,
  input: TInput,
  enabled = true,
) {
  const officeScope = useOfficeScopeKey()

  return useQuery({
    queryKey: [resource, 'list', officeScope, input],
    enabled,
    queryFn: async () => {
      const { data } = await http.get<PagedResult<TRow>>(`/${resource}`, {
        params: toQuery(input as Record<string, unknown>),
      })
      return data
    },
  })
}

function useDetail<T>(resource: string, id: number | undefined) {
  return useQuery({
    queryKey: [resource, 'detail', id],
    enabled: Number.isFinite(id) && (id ?? 0) > 0,
    queryFn: async () => {
      const { data } = await http.get<T>(`/${resource}/${id}/detail`)
      return data
    },
  })
}

// ------------------------------------------------------------------ Invoices

/**
 * Paged invoice list (`GET api/invoice`).
 *
 * `enabled` lets a screen that lists the invoices *of one workplace* hold the request back until
 * a workplace has been picked, instead of pulling the whole register first.
 */
export function useInvoiceList(input: GetInvoiceListInput, enabled = true) {
  return usePagedResource<InvoiceListDto, GetInvoiceListInput>(
    FINANCE_ENDPOINTS.invoice,
    input,
    enabled,
  )
}

/** Combined invoice detail / print view (`GET api/invoice/{id}/detail`). */
export function useInvoiceDetail(id: number | undefined) {
  return useDetail<InvoiceNavigationDto>(FINANCE_ENDPOINTS.invoice, id)
}

/** Lines of one invoice, in display order (`GET api/invoice/{id}/lines`). */
export function useInvoiceLines(invoiceId: number | undefined) {
  return useQuery({
    queryKey: [FINANCE_ENDPOINTS.invoice, 'lines', invoiceId],
    enabled: Number.isFinite(invoiceId) && (invoiceId ?? 0) > 0,
    queryFn: async () => {
      const { data } = await http.get<ListResult<InvoiceLineDto>>(
        `/${FINANCE_ENDPOINTS.invoice}/${invoiceId}/lines`,
      )
      return data
    },
  })
}

/**
 * Invoice balance of one workplace (`GET api/invoice/company/{companyId}/balance`).
 *
 * The balance is a server-side aggregate; the screen displays it and never recomputes it.
 */
export function useCompanyBalance(companyId: number | undefined) {
  return useQuery({
    queryKey: [FINANCE_ENDPOINTS.invoice, 'company-balance', companyId],
    enabled: Number.isFinite(companyId) && (companyId ?? 0) > 0,
    queryFn: async () => {
      const { data } = await http.get<CompanyBalanceDto>(
        `/${FINANCE_ENDPOINTS.invoice}/company/${companyId}/balance`,
      )
      return data
    },
  })
}

/**
 * Allocates the next invoice number for an office and year
 * (`GET api/invoice/next-number?year=YYYY&officeId=…`).
 *
 * Deliberately a mutation rather than a query: the number comes from an atomic counter and must
 * be requested on an explicit user action, never speculatively on render. `year` is required —
 * omitting it answers 400 by design — and the result is the only invoice number the UI is
 * allowed to put in the form.
 */
export function useGenerateInvoiceNumber() {
  return useMutation({
    mutationFn: async ({ year, officeId }: { year: number; officeId?: number | null }) => {
      const { data } = await http.get<GeneratedInvoiceNumberDto>(
        `/${FINANCE_ENDPOINTS.invoice}/next-number`,
        { params: { year, officeId: officeId || undefined } },
      )
      return data
    },
  })
}

// -------------------------------------------------------------- Cash registers

/** Paged cash register list (`GET api/cash-register`). */
export function useCashRegisterList(input: GetCashRegisterListInput) {
  return usePagedResource<CashRegisterListDto, GetCashRegisterListInput>(
    FINANCE_ENDPOINTS.cashRegister,
    input,
  )
}

/** Combined cash register detail (`GET api/cash-register/{id}/detail`). */
export function useCashRegisterDetail(id: number | undefined) {
  return useDetail<CashRegisterNavigationDto>(FINANCE_ENDPOINTS.cashRegister, id)
}

/**
 * Balance of one register, optionally as of a past instant
 * (`GET api/cash-register/{id}/balance?asOf=…`).
 */
export function useCashRegisterBalance(id: number | undefined, asOf?: string) {
  return useQuery({
    queryKey: [FINANCE_ENDPOINTS.cashRegister, 'balance', id, asOf ?? null],
    enabled: Number.isFinite(id) && (id ?? 0) > 0,
    queryFn: async () => {
      const { data } = await http.get<CashRegisterBalanceDto>(
        `/${FINANCE_ENDPOINTS.cashRegister}/${id}/balance`,
        { params: { asOf: asOf || undefined } },
      )
      return data
    },
  })
}

/**
 * Paged movements of one register (`GET api/cash-register/transactions`).
 *
 * `cashRegisterId` is required by `GetCashTransactionListInput`, so the query stays disabled
 * until a register has been picked rather than firing a request that answers 400.
 */
export function useCashTransactionList(input: GetCashTransactionListInput) {
  return usePagedResource<CashTransactionDto, GetCashTransactionListInput>(
    `${FINANCE_ENDPOINTS.cashRegister}/transactions`,
    input,
    input.cashRegisterId > 0,
  )
}

// ------------------------------------------------------------------ Penalties

/** Paged fine catalogue (`GET api/penalty`). */
export function usePenaltyList(input: GetPenaltyListInput) {
  return usePagedResource<PenaltyListDto, GetPenaltyListInput>(FINANCE_ENDPOINTS.penalty, input)
}

/** A fine article with its full amount matrix (`GET api/penalty/{id}/detail`). */
export function usePenaltyDetail(id: number | undefined) {
  return useDetail<PenaltyNavigationDto>(FINANCE_ENDPOINTS.penalty, id)
}

/**
 * The amount that applies to one workplace profile in one year
 * (`GET api/penalty/{id}/applicable-amount`).
 *
 * The lookup runs only once the caller has a complete profile — the endpoint requires all three
 * of `hazardClass`, `range` and `year` and answers 400 otherwise.
 */
export function useApplicablePenaltyAmount(
  penaltyId: number | undefined,
  hazardClass: HazardClass | undefined,
  range: EmployeeCountRange | undefined,
  year: number | undefined,
  enabled: boolean,
) {
  return useQuery({
    queryKey: [FINANCE_ENDPOINTS.penalty, 'applicable-amount', penaltyId, hazardClass, range, year],
    enabled:
      enabled &&
      (penaltyId ?? 0) > 0 &&
      hazardClass !== undefined &&
      range !== undefined &&
      (year ?? 0) > 0,
    queryFn: async () => {
      const { data } = await http.get<ApplicablePenaltyAmountDto>(
        `/${FINANCE_ENDPOINTS.penalty}/${penaltyId}/applicable-amount`,
        { params: { hazardClass, range, year } },
      )
      return data
    },
  })
}

/** Paged fine-risk survey list (`GET api/penalty/surveys`). */
export function usePenaltySurveyList(input: GetPenaltySurveyListInput) {
  return usePagedResource<PenaltySurveyListDto, GetPenaltySurveyListInput>(
    FINANCE_ENDPOINTS.penaltySurvey,
    input,
  )
}

/** One survey header (`GET api/penalty/surveys/{surveyId}`). */
export function usePenaltySurvey(surveyId: number | undefined) {
  return useQuery({
    queryKey: [FINANCE_ENDPOINTS.penaltySurvey, 'entity', surveyId],
    enabled: Number.isFinite(surveyId) && (surveyId ?? 0) > 0,
    queryFn: async () => {
      const { data } = await http.get<PenaltySurveyDto>(
        `/${FINANCE_ENDPOINTS.penaltySurvey}/${surveyId}`,
      )
      return data
    },
  })
}

/**
 * Paged answer lines of a survey (`GET api/penalty/surveys/lines`).
 *
 * `penaltySurveyId` is required, so the query stays disabled until a survey is known.
 */
export function usePenaltySurveyLines(input: GetPenaltySurveyLineListInput) {
  return usePagedResource<PenaltySurveyLineDto, GetPenaltySurveyLineListInput>(
    `${FINANCE_ENDPOINTS.penaltySurvey}/lines`,
    input,
    input.penaltySurveyId > 0,
  )
}

/** Total fine exposure of a survey (`GET api/penalty/surveys/{surveyId}/total`). */
export function usePenaltySurveyTotal(surveyId: number | undefined) {
  return useQuery({
    queryKey: [FINANCE_ENDPOINTS.penaltySurvey, 'total', surveyId],
    enabled: Number.isFinite(surveyId) && (surveyId ?? 0) > 0,
    queryFn: async () => {
      const { data } = await http.get<PenaltySurveyTotalDto>(
        `/${FINANCE_ENDPOINTS.penaltySurvey}/${surveyId}/total`,
      )
      return data
    },
  })
}

// -------------------------------------------------------------------- Lookups

function useLookupList(resource: string, filter?: string) {
  const officeScope = useOfficeScopeKey()

  return useQuery({
    queryKey: [resource, 'lookup', officeScope, filter ?? ''],
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(`/${resource}/lookup`, {
        params: { filter: filter || undefined },
      })
      return data
    },
  })
}

/** Workplace drop-down (`GET api/company/lookup`). */
export function useCompanyLookup(filter?: string) {
  return useLookupList(FINANCE_ENDPOINTS.company, filter)
}

/** Office drop-down (`GET api/office/lookup`). */
export function useOfficeLookup(filter?: string) {
  return useLookupList(FINANCE_ENDPOINTS.office, filter)
}

/** Cash register drop-down (`GET api/cash-register/lookup`). */
export function useCashRegisterLookup(filter?: string) {
  return useLookupList(FINANCE_ENDPOINTS.cashRegister, filter)
}

// ---------------------------------------------------------------
// Child-collection mutations
//
// `useCreate` / `useUpdate` / `useDelete` from `@/api/mutations` only cover `/{resource}` and
// `/{resource}/{id}`. Invoice lines, fine amounts, survey lines and cash movements all hang off
// a parent id, so they get their own mutations here. Each invalidates the parent resource key,
// which is what the list, detail, lines and total queries above are stored under — so a line
// change refreshes the server-computed header totals rather than leaving a stale figure on
// screen.
// ---------------------------------------------------------------

function useChildMutation<TVariables, TResult>(
  resources: string[],
  request: (variables: TVariables) => Promise<TResult>,
  onDone?: () => void,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: request,
    onSuccess: async () => {
      await Promise.all(
        resources.map((resource) => queryClient.invalidateQueries({ queryKey: [resource] })),
      )
      onDone?.()
    },
  })
}

/** `POST api/invoice/{id}/lines` */
export function useAddInvoiceLine(invoiceId: number, onDone?: () => void) {
  return useChildMutation<SaveInvoiceLineDto, InvoiceLineDto>(
    [FINANCE_ENDPOINTS.invoice],
    async (input) => {
      const { data } = await http.post<InvoiceLineDto>(
        `/${FINANCE_ENDPOINTS.invoice}/${invoiceId}/lines`,
        input,
      )
      return data
    },
    onDone,
  )
}

/** `PUT api/invoice/{id}/lines/{lineId}` */
export function useUpdateInvoiceLine(invoiceId: number, onDone?: () => void) {
  return useChildMutation<{ lineId: number; input: SaveInvoiceLineDto }, InvoiceLineDto>(
    [FINANCE_ENDPOINTS.invoice],
    async ({ lineId, input }) => {
      const { data } = await http.put<InvoiceLineDto>(
        `/${FINANCE_ENDPOINTS.invoice}/${invoiceId}/lines/${lineId}`,
        input,
      )
      return data
    },
    onDone,
  )
}

/** `DELETE api/invoice/{id}/lines/{lineId}` */
export function useRemoveInvoiceLine(invoiceId: number, onDone?: () => void) {
  return useChildMutation<number, void>(
    [FINANCE_ENDPOINTS.invoice],
    async (lineId) => {
      await http.delete(`/${FINANCE_ENDPOINTS.invoice}/${invoiceId}/lines/${lineId}`)
    },
    onDone,
  )
}

/** `POST api/cash-register/transactions` */
export function useAddCashTransaction(onDone?: () => void) {
  return useChildMutation<CreateCashTransactionDto, CashTransactionDto>(
    [FINANCE_ENDPOINTS.cashRegister, `${FINANCE_ENDPOINTS.cashRegister}/transactions`],
    async (input) => {
      const { data } = await http.post<CashTransactionDto>(
        `/${FINANCE_ENDPOINTS.cashRegister}/transactions`,
        input,
      )
      return data
    },
    onDone,
  )
}

/**
 * `POST api/cash-register/transactions/{transactionId}/void`
 *
 * The only way to undo a movement: the ledger is append-only, so the row stays and simply stops
 * counting towards the balance.
 */
export function useVoidCashTransaction(onDone?: () => void) {
  return useChildMutation<number, CashTransactionDto>(
    [FINANCE_ENDPOINTS.cashRegister, `${FINANCE_ENDPOINTS.cashRegister}/transactions`],
    async (transactionId) => {
      const { data } = await http.post<CashTransactionDto>(
        `/${FINANCE_ENDPOINTS.cashRegister}/transactions/${transactionId}/void`,
        {},
      )
      return data
    },
    onDone,
  )
}

/** `POST api/penalty/{id}/amounts` */
export function useAddPenaltyAmount(penaltyId: number, onDone?: () => void) {
  return useChildMutation<SavePenaltyAmountDto, PenaltyAmountDto>(
    [FINANCE_ENDPOINTS.penalty],
    async (input) => {
      const { data } = await http.post<PenaltyAmountDto>(
        `/${FINANCE_ENDPOINTS.penalty}/${penaltyId}/amounts`,
        input,
      )
      return data
    },
    onDone,
  )
}

/** `PUT api/penalty/{id}/amounts/{amountId}` */
export function useUpdatePenaltyAmount(penaltyId: number, onDone?: () => void) {
  return useChildMutation<{ amountId: number; input: SavePenaltyAmountDto }, PenaltyAmountDto>(
    [FINANCE_ENDPOINTS.penalty],
    async ({ amountId, input }) => {
      const { data } = await http.put<PenaltyAmountDto>(
        `/${FINANCE_ENDPOINTS.penalty}/${penaltyId}/amounts/${amountId}`,
        input,
      )
      return data
    },
    onDone,
  )
}

/** `DELETE api/penalty/{id}/amounts/{amountId}` */
export function useRemovePenaltyAmount(penaltyId: number, onDone?: () => void) {
  return useChildMutation<number, void>(
    [FINANCE_ENDPOINTS.penalty],
    async (amountId) => {
      await http.delete(`/${FINANCE_ENDPOINTS.penalty}/${penaltyId}/amounts/${amountId}`)
    },
    onDone,
  )
}

/** `POST api/penalty/surveys/{surveyId}/lines` */
export function useAddPenaltySurveyLine(surveyId: number, onDone?: () => void) {
  return useChildMutation<SavePenaltySurveyLineDto, PenaltySurveyLineDto>(
    [FINANCE_ENDPOINTS.penaltySurvey, `${FINANCE_ENDPOINTS.penaltySurvey}/lines`],
    async (input) => {
      const { data } = await http.post<PenaltySurveyLineDto>(
        `/${FINANCE_ENDPOINTS.penaltySurvey}/${surveyId}/lines`,
        input,
      )
      return data
    },
    onDone,
  )
}

/** `PUT api/penalty/surveys/{surveyId}/lines/{lineId}` */
export function useUpdatePenaltySurveyLine(surveyId: number, onDone?: () => void) {
  return useChildMutation<
    { lineId: number; input: SavePenaltySurveyLineDto },
    PenaltySurveyLineDto
  >(
    [FINANCE_ENDPOINTS.penaltySurvey, `${FINANCE_ENDPOINTS.penaltySurvey}/lines`],
    async ({ lineId, input }) => {
      const { data } = await http.put<PenaltySurveyLineDto>(
        `/${FINANCE_ENDPOINTS.penaltySurvey}/${surveyId}/lines/${lineId}`,
        input,
      )
      return data
    },
    onDone,
  )
}

/** `DELETE api/penalty/surveys/{surveyId}/lines/{lineId}` */
export function useRemovePenaltySurveyLine(surveyId: number, onDone?: () => void) {
  return useChildMutation<number, void>(
    [FINANCE_ENDPOINTS.penaltySurvey, `${FINANCE_ENDPOINTS.penaltySurvey}/lines`],
    async (lineId) => {
      await http.delete(`/${FINANCE_ENDPOINTS.penaltySurvey}/${surveyId}/lines/${lineId}`)
    },
    onDone,
  )
}
