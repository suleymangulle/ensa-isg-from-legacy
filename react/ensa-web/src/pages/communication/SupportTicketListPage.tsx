import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Alert, Badge, Button, Card, CheckBox, Input, Select, TextArea } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
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
  const [status, setStatus] = useState<SupportTicketStatus | null>(null)
  const [onlyMine, setOnlyMine] = useState(false)

  const [editingId, setEditingId] = useState<number | undefined>()
  const [isEditorOpen, setIsEditorOpen] = useState(false)
  const [deleting, setDeleting] = useState<SupportTicketListDto | null>(null)

  const { data, isLoading, error } = useSupportTicketList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    filter: search || undefined,
    status: status ?? undefined,
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
        <Badge variant={TICKET_STATUS_BADGE[row.status]}>
          {t(`enums.supportTicketStatus.${row.status}`)}
        </Badge>
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
              <Button variant="light" size="sm" 
                disabled={close.isPending}
                onClick={() => close.mutate(row.id)}
                aria-label={t('supportTicket.list.closeAria', { topic: row.topic })}
              >
                {t('supportTicket.list.close')}
              </Button>
            ) : (
              <Button variant="light" size="sm" 
                disabled={reopen.isPending}
                onClick={() => reopen.mutate(row.id)}
                aria-label={t('supportTicket.list.reopenAria', { topic: row.topic })}
              >
                {t('supportTicket.list.reopen')}
              </Button>
            )}
            <Button variant="light" size="sm" 
              onClick={() => {
                setEditingId(row.id)
                setIsEditorOpen(true)
              }}
              aria-label={t('supportTicket.list.editAria', { topic: row.topic })}
            >
              {t('common.edit')}
            </Button>
            <Button variant="light" size="sm" 
              onClick={() => setDeleting(row)}
              aria-label={t('supportTicket.list.deleteAria', { topic: row.topic })}
            >
              {t('common.delete')}
            </Button>
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
          <Button variant="primary"
            onClick={() => {
              setEditingId(undefined)
              setIsEditorOpen(true)
            }}
          >
            {t('supportTicket.list.create')}
          </Button>
        }
      />

      {openCount.data && (
        <Alert
          variant={openCount.data.openCount > 0 ? 'warning' : 'success'}
          className="d-flex align-items-center gap-2"
        >
          <span className="fw-bold">{openCount.data.openCount}</span>
          <span>{t('supportTicket.list.openCount')}</span>
        </Alert>
      )}

      {workflowError && <Alert variant="danger">{errorMessage(workflowError)}</Alert>}

      <Card
        
        header={
          <SearchBar
            value={search}
            onChange={(value) => {
              setSearch(value)
              setPage(1)
            }}
            placeholder={t('supportTicket.list.searchPlaceholder')}
          >
            <div style={{ minWidth: 200 }}>
              <label htmlFor="ticket-status-filter" className="visually-hidden">
                {t('supportTicket.filters.status')}
              </label>
              <Select<SupportTicketStatus>
                id="ticket-status-filter"
                options={TICKET_STATUSES.map((value) => ({
                  value,
                  label: t(`enums.supportTicketStatus.${value}`),
                }))}
                value={status}
                placeholder={t('supportTicket.filters.allStatuses')}
                onChange={(value) => {
                  setStatus(value)
                  setPage(1)
                }}
              />
            </div>
            <CheckBox
              id="ticket-only-mine"
              label={t('supportTicket.filters.onlyMine')}
              checked={onlyMine}
              onChange={(checked) => {
                setOnlyMine(checked)
                setPage(1)
              }}
            />
          </SearchBar>
        }
        footer={
          data && data.totalCount > 0 ? (
            <Pagination
              total={data.totalCount}
              page={page}
              pageSize={PAGE_SIZE}
              onPageChange={setPage}
            />
          ) : undefined
        }
      >
        <DataTable
          label={t('supportTicket.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(row) => row.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('supportTicket.list.empty')}
        />
      </Card>

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
  const [responderUserId, setResponderUserId] = useState<number | null>(null)
  const [topicError, setTopicError] = useState<string>()

  const users = useUserLookup()

  useEffect(() => {
    if (!isOpen) return
    setTopicError(undefined)
    setTopic(ticket?.topic ?? '')
    setFirstMessage('')
    setResponderUserId(ticket?.responderUserId ?? null)
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
          responderUserId,
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
        <Input
          id="ticket-topic"
          type="text"
          label={t('supportTicket.fields.topic')}
          required
          error={topicError}
          value={topic}
          onChange={setTopic}
        />

        {ticket ? (
          <Select<number>
            id="ticket-responder"
            label={t('supportTicket.fields.responder')}
            helpText={t('supportTicket.editor.responderHint')}
            placeholder={t('supportTicket.editor.noResponder')}
            options={(users.data?.items ?? []).map((user) => ({
              value: user.id,
              label: user.displayName,
            }))}
            value={responderUserId}
            onChange={setResponderUserId}
          />
        ) : (
          <TextArea
            id="ticket-first-message"
            label={t('supportTicket.fields.firstMessage')}
            helpText={t('supportTicket.editor.firstMessageHint')}
            rows={4}
            maxLength={4000}
            value={firstMessage}
            onChange={setFirstMessage}
          />
        )}
      </div>
    </Modal>
  )
}
