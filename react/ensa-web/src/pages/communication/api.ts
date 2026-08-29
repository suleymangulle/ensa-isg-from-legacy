import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type BadgeVariant } from 'rich-react-component'
import { http, type ListResult, type PagedResult } from '@/api/http'
import type {
  ContentFormat,
  LookupDto,
  MailPriority,
  MailStatus,
  MailType,
  MessageType,
  SupportTicketStatus,
  VisitType,
} from '@/api/endpoints'

/**
 * Data layer of the communication module — visits, support tickets, in-app messages and the
 * outbound mail queue.
 *
 * The types mirror `Ensa.Application.Contracts/Communication/Dtos` one to one: the API
 * serialises property names camelCase and enums as numbers, so the C# property set *is* the
 * JSON contract.
 *
 * Two identity rules from the controllers are worth repeating, because they shape the forms:
 * a message's sender and a ticket's opener come from the access token and are never sent by the
 * client, and the message folders (`inbox` / `sent`) are the caller's own — there is no owner
 * parameter to pass.
 */

// ---------------------------------------------------------------
// Resources — `api/{resource}`, kebab-cased controller names.
// ---------------------------------------------------------------

/** `api/visit` — see `VisitController`. */
export const VISIT = 'visit'

/** `api/support-ticket` — see `SupportTicketController`. */
export const SUPPORT_TICKET = 'support-ticket'

/** `api/message` — see `MessageController`. */
export const MESSAGE = 'message'

/** `api/mail` — see `MailController`. */
export const MAIL = 'mail'

/** `api/company` — the workplace drop-down. */
export const COMPANY = 'company'

/** `api/user` — the user drop-down used by messages, tickets and the visit filter. */
export const USER = 'user'

// ---------------------------------------------------------------
// Badge colours
//
// Only the colour mapping stays in code; the labels come from the locale bundle.
// ---------------------------------------------------------------

export const TICKET_STATUS_BADGE: Record<SupportTicketStatus, BadgeVariant> = {
  0: 'warning',
  1: 'info',
  2: 'success',
  3: 'danger',
}

export const MAIL_STATUS_BADGE: Record<MailStatus, BadgeVariant> = {
  0: 'primary',
  1: 'info',
  2: 'success',
  3: 'danger',
  4: 'warning',
}

export const MAIL_PRIORITY_BADGE: Record<MailPriority, BadgeVariant> = {
  0: 'primary',
  1: 'info',
  2: 'danger',
}

// ---------------------------------------------------------------
// Visit DTOs
// ---------------------------------------------------------------

/** `VisitDto` — `GET api/visit/{id}`. */
export interface VisitDto {
  id: number
  tenantId?: number | null
  companyId: number
  userId: number
  visitDate: string
  start?: string | null
  end?: string | null
  operationType: VisitType
  description?: string | null
  color?: string | null
  scheduledWeek?: number | null
  scheduledMonth?: number | null
  regionCode?: number | null
  otherCompanyDistanceKm?: number | null
  isCompleted: boolean
}

/** `CreateVisitDto` — `userId` defaults to the caller when omitted. */
export interface CreateVisitDto {
  companyId: number
  userId?: number | null
  visitDate: string
  start?: string | null
  end?: string | null
  operationType: VisitType
  description?: string | null
  color?: string | null
  scheduledWeek?: number | null
  scheduledMonth?: number | null
  regionCode?: number | null
  otherCompanyDistanceKm?: number | null
}

/** `UpdateVisitDto` — the create input plus the "it actually happened" flag. */
export interface UpdateVisitDto extends CreateVisitDto {
  isCompleted: boolean
}

/**
 * `VisitCalendarDto` — `GET api/visit/calendar`.
 *
 * This is the shape the visit screen renders: unlike `VisitListDto` it carries the workplace
 * name and the visiting user's name, so a day agenda can be drawn without resolving ids per row.
 */
export interface VisitCalendarDto {
  id: number
  title: string
  start: string
  end: string
  color?: string | null
  companyId: number
  companyName?: string | null
  userId: number
  userFullName?: string | null
  operationType: VisitType
  isCompleted: boolean
}

// ---------------------------------------------------------------
// Support ticket DTOs
// ---------------------------------------------------------------

/** `SupportTicketListDto` — the row of `GET api/support-ticket`. */
export interface SupportTicketListDto {
  id: number
  topic: string
  openedByUserId: number
  responderUserId?: number | null
  status: SupportTicketStatus
  creationTime: string
  closingDate?: string | null
}

/** `SupportTicketDto` — `GET api/support-ticket/{id}`. */
export interface SupportTicketDto {
  id: number
  tenantId?: number | null
  topic: string
  openedByUserId: number
  responderUserId?: number | null
  closedByUserId?: number | null
  status: SupportTicketStatus
  closingDate?: string | null
  creationTime: string
}

/** `SupportTicketMessageDto` — one entry of a ticket thread. */
export interface SupportTicketMessageDto {
  id: number
  tenantId?: number | null
  supportTicketId: number
  message: string
  senderUserId: number
  fieldUserId: number
  isRead: boolean
  creationTime: string
}

/** `CreateSupportTicketDto` — there is no opener field; the caller opens the ticket. */
export interface CreateSupportTicketDto {
  topic: string
  firstMessage?: string | null
}

/** `UpdateSupportTicketDto` — the status is driven by close/reopen, never written here. */
export interface UpdateSupportTicketDto {
  topic: string
  responderUserId?: number | null
}

/** `SupportTicketNavigationDto` — `GET api/support-ticket/{id}/detail`. */
export interface SupportTicketNavigationDto {
  supportTicket: SupportTicketDto
  openedByUser?: LookupDto | null
  responderUser?: LookupDto | null
  messages: SupportTicketMessageDto[]
}

/** `OpenSupportTicketCountDto` — `GET api/support-ticket/open-count`. */
export interface OpenSupportTicketCountDto {
  userId: number
  openCount: number
}

/** `GetSupportTicketListInput`. */
export interface SupportTicketListInput {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  status?: SupportTicketStatus
  openedByUserId?: number
  responderUserId?: number
  startDate?: string
  endDate?: string
  onlyMine?: boolean
}

// ---------------------------------------------------------------
// Message DTOs
// ---------------------------------------------------------------

/** `MessageListDto` — the row of `GET api/message/inbox` and `.../sent`. */
export interface MessageListDto {
  id: number
  messageType: MessageType
  content: string
  senderId: number
  recipientId: number
  companyId?: number | null
  isRead: boolean
  readDate?: string | null
  creationTime: string
}

/** `MessageDto` — `GET api/message/{id}`. */
export interface MessageDto {
  id: number
  tenantId?: number | null
  messageType: MessageType
  content: string
  recipientId: number
  senderId: number
  companyId?: number | null
  isRead: boolean
  readDate?: string | null
  creationTime: string
}

/** `SendMessageDto` — deliberately no sender field. */
export interface SendMessageDto {
  recipientId: number
  content: string
  messageType: MessageType
  companyId?: number | null
}

/** `UnreadMessageCountDto` — `GET api/message/unread-count`. */
export interface UnreadMessageCountDto {
  userId: number
  unreadCount: number
}

/** `GetMessageListInput`. */
export interface MessageListInput {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  messageType?: MessageType
  companyId?: number
  correspondentUserId?: number
  isRead?: boolean
  startDate?: string
  endDate?: string
}

// ---------------------------------------------------------------
// Mail DTOs
// ---------------------------------------------------------------

/** `MailListDto` — the row of `GET api/mail`. */
export interface MailListDto {
  id: number
  sender: string
  recipient: string
  topic: string
  mailStatus: MailStatus
  mailPriority: MailPriority
  mailType: MailType
  submissionDate?: string | null
  attemptCount: number
  creationTime: string
}

/** `MailDto` — `GET api/mail/{id}`. */
export interface MailDto {
  id: number
  tenantId?: number | null
  sender: string
  recipient: string
  topic: string
  content: string
  contentFormat: ContentFormat
  mailPriority: MailPriority
  mailType: MailType
  mailStatus: MailStatus
  errorMessage?: string | null
  submissionDate?: string | null
  attemptCount: number
  creationTime: string
}

/** `MailAttachmentDto`. */
export interface MailAttachmentDto {
  id: number
  tenantId?: number | null
  mailId: number
  documentId: number
  orderNo: number
  creationTime: string
}

/** `MailAttachmentNavigationDto` — an attachment row with the file it points at. */
export interface MailAttachmentNavigationDto {
  attachment: MailAttachmentDto
  document?: LookupDto | null
}

/** `MailNavigationDto` — `GET api/mail/{id}/detail`. */
export interface MailNavigationDto {
  mail: MailDto
  attachments: MailAttachmentNavigationDto[]
}

/** `CreateMailDto` / `UpdateMailDto` — the status is never written directly. */
export interface SaveMailDto {
  sender: string
  recipient: string
  topic: string
  content: string
  contentFormat: ContentFormat
  mailPriority: MailPriority
  mailType: MailType
}

/** `AddMailAttachmentDto`. */
export interface AddMailAttachmentDto {
  documentId: number
  orderNo: number
}

/** `GetMailListInput`. */
export interface MailListInput {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  mailStatus?: MailStatus
  mailType?: MailType
  mailPriority?: MailPriority
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

/**
 * `GET api/visit/calendar` — visits inside a date range.
 *
 * Both ends of the range are mandatory on the controller and the span is capped server-side at
 * 366 days, so the screen always sends an explicit, bounded range rather than relying on a
 * default that does not exist.
 */
export function useVisitCalendar(from: string, to: string, userId?: number) {
  return useQuery({
    queryKey: [VISIT, 'calendar', from, to, userId],
    enabled: !!from && !!to,
    queryFn: async () => {
      const { data } = await http.get<ListResult<VisitCalendarDto>>(`/${VISIT}/calendar`, {
        params: clean({ from, to, userId }),
      })
      return data
    },
  })
}

/** `GET api/support-ticket` */
export function useSupportTicketList(input: SupportTicketListInput) {
  return useQuery({
    queryKey: [SUPPORT_TICKET, 'list', input],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<SupportTicketListDto>>(`/${SUPPORT_TICKET}`, {
        params: clean({ ...input }),
      })
      return data
    },
  })
}

/** `GET api/support-ticket/{id}/detail` */
export function useSupportTicketDetail(id: number | undefined) {
  return useQuery({
    queryKey: [SUPPORT_TICKET, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<SupportTicketNavigationDto>(
        `/${SUPPORT_TICKET}/${id}/detail`,
      )
      return data
    },
  })
}

/** `GET api/support-ticket/open-count` — the caller's own open tickets. */
export function useOpenTicketCount() {
  return useQuery({
    queryKey: [SUPPORT_TICKET, 'open-count'],
    queryFn: async () => {
      const { data } = await http.get<OpenSupportTicketCountDto>(`/${SUPPORT_TICKET}/open-count`)
      return data
    },
  })
}

/** Which of the caller's two message folders is being read. */
export type MessageFolder = 'inbox' | 'sent'

/** `GET api/message/inbox` and `GET api/message/sent` — always the caller's own folders. */
export function useMessageList(folder: MessageFolder, input: MessageListInput) {
  return useQuery({
    queryKey: [MESSAGE, folder, input],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<MessageListDto>>(`/${MESSAGE}/${folder}`, {
        params: clean({ ...input }),
      })
      return data
    },
  })
}

/** `GET api/message/unread-count` */
export function useUnreadMessageCount() {
  return useQuery({
    queryKey: [MESSAGE, 'unread-count'],
    queryFn: async () => {
      const { data } = await http.get<UnreadMessageCountDto>(`/${MESSAGE}/unread-count`)
      return data
    },
  })
}

/** `GET api/mail` */
export function useMailList(input: MailListInput) {
  return useQuery({
    queryKey: [MAIL, 'list', input],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<MailListDto>>(`/${MAIL}`, {
        params: clean({ ...input }),
      })
      return data
    },
  })
}

/** `GET api/mail/{id}/detail` — the mail with its attachments resolved to file names. */
export function useMailDetail(id: number | undefined) {
  return useQuery({
    queryKey: [MAIL, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<MailNavigationDto>(`/${MAIL}/${id}/detail`)
      return data
    },
  })
}

/** `GET api/company/lookup` */
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

/** `GET api/user/lookup` */
export function useUserLookup(filter?: string) {
  return useQuery({
    queryKey: [USER, 'lookup', filter],
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(`/${USER}/lookup`, {
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
// `DELETE /{resource}/{id}`. The workflow routes below need their own, and each invalidates the
// very `[resource]` key the query hooks above are filed under.
// ---------------------------------------------------------------

/** Invalidates every query of a resource — lists, detail views and counters alike. */
function useResourceInvalidation(resource: string) {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: [resource] })
}

/** `POST api/support-ticket/{id}/messages` — refused by the API on a closed ticket. */
export function useAddTicketMessage(ticketId: number) {
  const invalidate = useResourceInvalidation(SUPPORT_TICKET)

  return useMutation({
    mutationFn: async (message: string) => {
      const { data } = await http.post<SupportTicketMessageDto>(
        `/${SUPPORT_TICKET}/${ticketId}/messages`,
        { message },
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST api/support-ticket/{id}/close` and `.../reopen`. */
export function useTicketWorkflow(action: 'close' | 'reopen') {
  const invalidate = useResourceInvalidation(SUPPORT_TICKET)

  return useMutation({
    mutationFn: async (ticketId: number) => {
      const { data } = await http.post<SupportTicketDto>(
        `/${SUPPORT_TICKET}/${ticketId}/${action}`,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST api/message` — the sender is taken from the access token. */
export function useSendMessage(options: { onSuccess?: () => void } = {}) {
  const invalidate = useResourceInvalidation(MESSAGE)

  return useMutation({
    mutationFn: async (input: SendMessageDto) => {
      const { data } = await http.post<MessageDto>(`/${MESSAGE}`, input)
      return data
    },
    onSuccess: async () => {
      await invalidate()
      options.onSuccess?.()
    },
  })
}

/** `POST api/message/{id}/read` — only the recipient may mark a message read. */
export function useMarkMessageRead() {
  const invalidate = useResourceInvalidation(MESSAGE)

  return useMutation({
    mutationFn: async (messageId: number) => {
      const { data } = await http.post<MessageDto>(`/${MESSAGE}/${messageId}/read`)
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST api/mail/{id}/queue` — hands a draft or failed mail to the sending queue. */
export function useQueueMail() {
  const invalidate = useResourceInvalidation(MAIL)

  return useMutation({
    mutationFn: async (mailId: number) => {
      const { data } = await http.post<MailDto>(`/${MAIL}/${mailId}/queue`)
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST api/mail/{id}/attachments` — the document must already exist in the store. */
export function useAddMailAttachment(mailId: number) {
  const invalidate = useResourceInvalidation(MAIL)

  return useMutation({
    mutationFn: async (input: AddMailAttachmentDto) => {
      const { data } = await http.post<MailAttachmentDto>(
        `/${MAIL}/${mailId}/attachments`,
        input,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `DELETE api/mail/{id}/attachments/{attachmentId}` */
export function useRemoveMailAttachment(mailId: number) {
  const invalidate = useResourceInvalidation(MAIL)

  return useMutation({
    mutationFn: async (attachmentId: number) => {
      await http.delete(`/${MAIL}/${mailId}/attachments/${attachmentId}`)
    },
    onSuccess: invalidate,
  })
}
