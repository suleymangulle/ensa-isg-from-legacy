import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, SearchBar, controlClass } from '@/components/Form'
import { useCreate, useDelete, useUpdate } from '@/api/mutations'
import { errorMessage } from '@/api/http'
import { useEntity } from '@/api/endpoints'
import { SupportTicketStatus } from '@/api/enums'
import { formatDate } from '@/utils/format'
import {
  SUPPORT_TICKET,
  TICKET_STATUS_BADGE,
  useOpenTicketCount,
  useSupportTicketList,
  useTicketWorkflow,
  useUserLookup,
  type CreateSupportTicketDto,
  type SupportTicketDto,
  type SupportTicketListDto,
  type UpdateSupportTicketDto,
} from './api'
import { TICKET_STATUSES } from './helpers'

const PAGE_SIZE = 20

/** The statuses that still need someone's attention — the point of the screen. */
const OPEN_STATUSES: SupportTicketStatus[] = [
  SupportTicketStatus.Open,
  SupportTicketStatus.Answered,
]

export default function SupportTicketListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [onlyMine, setOnlyMine] = useState(false)

  const [editingId, setEditingId] = useState<number | undefined>()
  const [isEditorOpen, setIsEditorOpen] = useState(false)
  const [deleting, setDeleting] = useState<SupportTicketListDto | null>(null)

  const { data, isLoading, error } = useSupportTicketList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    filter: search || undefined,
    status: status === '' ? undefined : (Number(status) as SupportTicketStatus),
    onlyMine: onlyMine || undefined,
  })

  const openCount = useOpenTicketCount()
  const users = useUserLookup()

  /** One batched lookup feeds every "opened by" and "responder" cell. */
  const userNames = useMemo(() => {
    const map = new Map<number, string>()
    for (const user of users.data?.items ?? []) map.set(user.id, user.displayName)
    return map
  }, [users.data])

  const editing = useEntity<SupportTicketDto>(SUPPORT_TICKET, editingId)
  const remove = useDelete(SUPPORT_TICKET, { onSuccess: () => setDeleting(null) })
  const close = useTicketWorkflow('close')
  const reopen = useTicketWorkflow('reopen')

  function userLabel(id: number | null | undefined) {
    if (!id) return t('common.none')
    return userNames.get(id) ?? t('supportTicket.list.userFallback', { id })
  }

  const columns: Column<SupportTicketListDto>[] = [
    {
      key: 'topic',
      header: t('supportTicket.fields.topic'),
      render: (row) => (
        <Link to={`/support-tickets/${row.id}`} className="fw-semibold text-decoration-none">
          {row.topic}
        </Link>
      ),
    },
    {
      key: 'status',
      header: t('supportTicket.fields.status'),
      align: 'center',
      render: (row) => (
        <span className={TICKET_STATUS_BADGE[row.status]}>
          {t(`enums.supportTicketStatus.${row.status}`)}
        </span>
      ),
    },
    {
      key: 'openedByUserId',
      header: t('supportTicket.fields.openedBy'),
      render: (row) => userLabel(row.openedByUserId),
    },
    {
      key: 'responderUserId',
      header: t('supportTicket.fields.responder'),
      render: (row) => userLabel(row.responderUserId),
    },
    {
      key: 'creationTime',
      header: t('supportTicket.fields.openedAt'),
      render: (row) => formatDate(row.creationTime) ?? t('common.none'),
    },
    {
      key: 'closingDate',
      header: t('supportTicket.fields.closedAt'),
      render: (row) => formatDate(row.closingDate) ?? t('common.none'),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '280px',
      render: (row) => {
        const isOpen = OPEN_STATUSES.includes(row.status)
        return (
          <div className="d-flex justify-content-end gap-2">
            {isOpen ? (
              <button
                type="button"
                className="btn btn-sm btn-light-success"
                disabled={close.isPending}
                onClick={() => close.mutate(row.id)}
                aria-label={t('supportTicket.list.closeAria', { topic: row.topic })}
              >
                {t('supportTicket.list.close')}
              </button>
            ) : (
              <button
                type="button"
                className="btn btn-sm btn-light-warning"
                disabled={reopen.isPending}
                onClick={() => reopen.mutate(row.id)}
                aria-label={t('supportTicket.list.reopenAria', { topic: row.topic })}
              >
                {t('supportTicket.list.reopen')}
              </button>
            )}
            <button
              type="button"
              className="btn btn-sm btn-light-primary"
              onClick={() => {
                setEditingId(row.id)
                setIsEditorOpen(true)
              }}
              aria-label={t('supportTicket.list.editAria', { topic: row.topic })}
            >
              {t('common.edit')}
            </button>
            <button
              type="button"
              className="btn btn-sm btn-light-danger"
              onClick={() => setDeleting(row)}
              aria-label={t('supportTicket.list.deleteAria', { topic: row.topic })}
            >
              {t('common.delete')}
            </button>
          </div>
        )
      },
    },
  ]

  const workflowError = close.error ?? reopen.error

  return (
    <>
      <PageTitle
        title={t('supportTicket.list.title')}
        description={t('supportTicket.list.description')}
        action={
          <button
            className="btn btn-primary"
            type="button"
            onClick={() => {
              setEditingId(undefined)
              setIsEditorOpen(true)
            }}
          >
            {t('supportTicket.list.create')}
          </button>
        }
      />

      {openCount.data && (
        <div
          className="alert border-0 d-flex align-items-center gap-2"
          style={{
            backgroundColor:
              openCount.data.openCount > 0 ? 'var(--kt-warning-light)' : 'var(--kt-success-light)',
            color: openCount.data.openCount > 0 ? 'var(--kt-warning)' : 'var(--kt-success)',
          }}
        >
          <span className="fw-bold">{openCount.data.openCount}</span>
          <span>{t('supportTicket.list.openCount')}</span>
        </div>
      )}

      {workflowError && (
        <div
          className="alert border-0"
          style={{ backgroundColor: 'var(--kt-danger-light)', color: 'var(--kt-danger)' }}
          role="alert"
        >
          {errorMessage(workflowError)}
        </div>
      )}

      <div className="card">
        <div className="card-header pt-4 pb-0 border-0">
          <SearchBar
            value={search}
            onChange={(value) => {
              setSearch(value)
              setPage(1)
            }}
            placeholder={t('supportTicket.list.searchPlaceholder')}
          >
            <div>
              <label htmlFor="ticket-status-filter" className="visually-hidden">
                {t('supportTicket.filters.status')}
              </label>
              <select
                id="ticket-status-filter"
                className="form-select"
                style={{ minWidth: 200 }}
                value={status}
                onChange={(event) => {
                  setStatus(event.target.value)
                  setPage(1)
                }}
              >
                <option value="">{t('supportTicket.filters.allStatuses')}</option>
                {TICKET_STATUSES.map((value) => (
                  <option key={value} value={value}>
                    {t(`enums.supportTicketStatus.${value}`)}
                  </option>
                ))}
              </select>
            </div>
            <div className="form-check">
              <input
                id="ticket-only-mine"
                type="checkbox"
                className="form-check-input"
                checked={onlyMine}
                onChange={(event) => {
                  setOnlyMine(event.target.checked)
                  setPage(1)
                }}
              />
              <label htmlFor="ticket-only-mine" className="form-check-label">
                {t('supportTicket.filters.onlyMine')}
              </label>
            </div>
          </SearchBar>
        </div>

        <div className="card-body p-0">
          <DataTable
            label={t('supportTicket.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(row) => row.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('supportTicket.list.empty')}
          />
        </div>

        {data && data.totalCount > 0 && (
          <div className="card-footer bg-transparent border-0 pt-0">
            <Pagination
              total={data.totalCount}
              page={page}
              pageSize={PAGE_SIZE}
              onPageChange={setPage}
            />
          </div>
        )}
      </div>

      {isEditorOpen && (!editingId || editing.data) && (
        <TicketEditor
          isOpen
          ticket={editingId ? editing.data : undefined}
          onClose={() => {
            setIsEditorOpen(false)
            setEditingId(undefined)
          }}
        />
      )}

      <ConfirmDialog
        isOpen={!!deleting}
        title={t('supportTicket.list.deleteTitle')}
        message={t('supportTicket.list.deleteMessage', { topic: deleting?.topic ?? '' })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

/**
 * Create and edit dialog.
 *
 * The two inputs differ on purpose: creating a ticket takes a subject and an optional opening
 * message (the opener is the caller, taken from the token), while editing takes the subject and
 * the assigned responder. The status is never written here — close and reopen own it, because
 * they also maintain the closing date and the closing user.
 */
function TicketEditor({
  isOpen,
  ticket,
  onClose,
}: {
  isOpen: boolean
  ticket?: SupportTicketDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [topic, setTopic] = useState('')
  const [firstMessage, setFirstMessage] = useState('')
  const [responderUserId, setResponderUserId] = useState('')
  const [topicError, setTopicError] = useState<string>()

  const users = useUserLookup()

  useEffect(() => {
    if (!isOpen) return
    setTopicError(undefined)
    setTopic(ticket?.topic ?? '')
    setFirstMessage('')
    setResponderUserId(ticket?.responderUserId?.toString() ?? '')
  }, [isOpen, ticket])

  const create = useCreate<CreateSupportTicketDto, SupportTicketDto>(SUPPORT_TICKET, {
    onSuccess: onClose,
  })
  const update = useUpdate<UpdateSupportTicketDto, SupportTicketDto>(SUPPORT_TICKET, {
    onSuccess: onClose,
  })
  const mutation = ticket ? update : create

  function submit() {
    if (!topic.trim()) {
      setTopicError(t('validation.required'))
      return
    }
    setTopicError(undefined)

    if (ticket) {
      update.mutate({
        id: ticket.id,
        input: {
          topic: topic.trim(),
          responderUserId: responderUserId ? Number(responderUserId) : null,
        },
      })
    } else {
      create.mutate({ topic: topic.trim(), firstMessage: firstMessage.trim() || null })
    }
  }

  return (
    <Modal
      title={ticket ? t('supportTicket.editor.editTitle') : t('supportTicket.editor.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={submit}
      isBusy={mutation.isPending}
      error={mutation.error ? errorMessage(mutation.error) : null}
    >
      <div className="row g-3">
        <Field
          label={t('supportTicket.fields.topic')}
          htmlFor="ticket-topic"
          required
          error={topicError}
        >
          <input
            id="ticket-topic"
            type="text"
            className={controlClass('form-control', topicError)}
            value={topic}
            onChange={(event) => setTopic(event.target.value)}
          />
        </Field>

        {ticket ? (
          <Field
            label={t('supportTicket.fields.responder')}
            htmlFor="ticket-responder"
            hint={t('supportTicket.editor.responderHint')}
          >
            <select
              id="ticket-responder"
              className="form-select"
              value={responderUserId}
              onChange={(event) => setResponderUserId(event.target.value)}
            >
              <option value="">{t('supportTicket.editor.noResponder')}</option>
              {users.data?.items.map((user) => (
                <option key={user.id} value={user.id}>
                  {user.displayName}
                </option>
              ))}
            </select>
          </Field>
        ) : (
          <Field
            label={t('supportTicket.fields.firstMessage')}
            htmlFor="ticket-first-message"
            hint={t('supportTicket.editor.firstMessageHint')}
          >
            <textarea
              id="ticket-first-message"
              className="form-control"
              rows={4}
              maxLength={4000}
              value={firstMessage}
              onChange={(event) => setFirstMessage(event.target.value)}
            />
          </Field>
        )}
      </div>
    </Modal>
  )
}
