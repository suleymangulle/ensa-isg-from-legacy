import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Alert, Badge, Button, Card, TextArea } from 'rich-react-component'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import { errorMessage } from '@/api/http'
import { SupportTicketStatus } from '@/api/enums'
import { formatDate, formatDateTime } from '@/utils/format'
import {
  TICKET_STATUS_BADGE,
  useAddTicketMessage,
  useSupportTicketDetail,
  useTicketWorkflow,
  type SupportTicketMessageDto,
  type SupportTicketNavigationDto,
} from './api'

/** Statuses that still accept a reply. The API refuses a message on a closed ticket. */
const OPEN_STATUSES: SupportTicketStatus[] = [
  SupportTicketStatus.Open,
  SupportTicketStatus.Answered,
]

/**
 * `GET api/support-ticket/{id}/detail` — the ticket, the people involved and the whole thread
 * in one round trip, plus the reply box and the open/close workflow.
 */
export default function SupportTicketDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const ticketId = Number(id)

  const { data, isLoading, error } = useSupportTicketDetail(ticketId)

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  return <TicketDetail detail={data} ticketId={ticketId} />
}

function TicketDetail({
  detail,
  ticketId,
}: {
  detail: SupportTicketNavigationDto
  ticketId: number
}) {
  const { t } = useTranslation()
  const [reply, setReply] = useState('')

  const ticket = detail.supportTicket
  const isOpen = OPEN_STATUSES.includes(ticket.status)

  const addMessage = useAddTicketMessage(ticketId)
  const close = useTicketWorkflow('close')
  const reopen = useTicketWorkflow('reopen')

  const actionError = addMessage.error ?? close.error ?? reopen.error

  function send() {
    const text = reply.trim()
    if (!text) return
    addMessage.mutate(text, { onSuccess: () => setReply('') })
  }

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/support-tickets" className="text-decoration-none">
              {t('supportTicket.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {ticket.topic}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={ticket.topic || t('supportTicket.detail.fallbackTitle')}
        description={t('supportTicket.detail.openedBy', {
          name: detail.openedByUser?.displayName ?? t('common.none'),
          date: formatDate(ticket.creationTime) ?? '',
        })}
        action={
          isOpen ? (
            <Button variant="light" 
              disabled={close.isPending}
              onClick={() => close.mutate(ticketId)}
            >
              {t('supportTicket.list.close')}
            </Button>
          ) : (
            <Button variant="light" 
              disabled={reopen.isPending}
              onClick={() => reopen.mutate(ticketId)}
            >
              {t('supportTicket.list.reopen')}
            </Button>
          )
        }
      />

      {actionError && <Alert variant="danger">{errorMessage(actionError)}</Alert>}

      <Card
        className="mb-4"
      >
          <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
            <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
              {t('supportTicket.fields.status')}
            </dt>
            <dd className="col-sm-9">
              <Badge variant={TICKET_STATUS_BADGE[ticket.status]}>
                {t(`enums.supportTicketStatus.${ticket.status}`)}
              </Badge>
            </dd>

            <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
              {t('supportTicket.fields.responder')}
            </dt>
            <dd className="col-sm-9">{detail.responderUser?.displayName ?? t('common.none')}</dd>

            <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
              {t('supportTicket.fields.closedAt')}
            </dt>
            <dd className="col-sm-9">{formatDate(ticket.closingDate) ?? t('common.none')}</dd>
          </dl>
        
      </Card>

      <Card
        header={
          <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-800)' }}>
            {t('supportTicket.detail.thread')}
          </h2>
        
        }
        footer={
        <div className="bg-transparent">
          {isOpen ? (
            <form
              onSubmit={(event) => {
                event.preventDefault()
                send()
              }}
            >
              <TextArea
                id="ticket-reply"
                label={t('supportTicket.detail.replyLabel')}
                rows={3}
                maxLength={4000}
                value={reply}
                onChange={setReply}
              />
              <Button variant="primary"
                type="submit"
                disabled={addMessage.isPending || !reply.trim()}
              >
                {t('supportTicket.detail.send')}
              </Button>
            </form>
          ) : (
            <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
              {t('supportTicket.detail.closedNotice')}
            </p>
          )}
        
        </div>
        }
      >
          {detail.messages.length === 0 ? (
            <p className="mb-0 text-center py-4" style={{ color: 'var(--kt-gray-500)' }}>
              {t('supportTicket.detail.emptyThread')}
            </p>
          ) : (
            <ul className="list-unstyled mb-0 d-flex flex-column gap-3">
              {detail.messages.map((message) => (
                <ThreadEntry key={message.id} message={message} detail={detail} />
              ))}
            </ul>
          )}
        
      </Card>
    </>
  )
}

/**
 * One entry of the thread.
 *
 * The author is resolved from the two people the navigation DTO already carries; there is no
 * per-message user request, and an id that matches neither falls back to its number rather than
 * to a blank.
 */
function ThreadEntry({
  message,
  detail,
}: {
  message: SupportTicketMessageDto
  detail: SupportTicketNavigationDto
}) {
  const { t } = useTranslation()

  const author =
    message.senderUserId === detail.openedByUser?.id
      ? detail.openedByUser.displayName
      : message.senderUserId === detail.responderUser?.id
        ? detail.responderUser.displayName
        : t('supportTicket.list.userFallback', { id: message.senderUserId })

  const isOpener = message.senderUserId === detail.openedByUser?.id

  return (
    <li
      className="p-3"
      style={{
        borderRadius: '0.5rem',
        backgroundColor: isOpener ? 'var(--kt-gray-100)' : 'var(--kt-primary-light)',
      }}
    >
      <div className="d-flex flex-wrap align-items-center gap-2 mb-2">
        <span className="fw-semibold" style={{ color: 'var(--kt-gray-900)' }}>
          {author}
        </span>
        <span style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
          {formatDateTime(message.creationTime) ?? ''}
        </span>
        {!message.isRead && (
          <Badge variant="warning">{t('supportTicket.detail.unread')}</Badge>
        )}
      </div>
      <p className="mb-0" style={{ whiteSpace: 'pre-wrap', color: 'var(--kt-gray-700)' }}>
        {message.message}
      </p>
    </li>
  )
}
