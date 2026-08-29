import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Alert, Badge, Button, Card, Select, Tabs, TextArea } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
import { useDelete } from '@/api/mutations'
import { errorMessage } from '@/api/http'
import { MessageType } from '@/api/enums'
import { formatDateTime } from '@/utils/format'
import {
  MESSAGE,
  useCompanyLookup,
  useMarkMessageRead,
  useMessageList,
  useSendMessage,
  useUnreadMessageCount,
  useUserLookup,
  type MessageFolder,
  type MessageListDto,
  type SendMessageDto,
} from './api'
import { MESSAGE_TYPES, excerpt } from './helpers'

const PAGE_SIZE = 20

const FOLDERS: MessageFolder[] = ['inbox', 'sent']

/**
 * Internal messages — the legacy `RequestMessages.aspx`.
 *
 * Both folders are the caller's own: `GET api/message/inbox` and `.../sent` derive the owner
 * from the access token, and `POST api/message` derives the sender the same way, so neither the
 * filters nor the compose form has an owner or a sender field to fill in.
 */
export default function MessageListPage() {
  const { t } = useTranslation()

  const [folder, setFolder] = useState<MessageFolder>('inbox')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [messageType, setMessageType] = useState<MessageType | null>(null)
  const [isRead, setIsRead] = useState('')

  const [isComposeOpen, setIsComposeOpen] = useState(false)
  const [reading, setReading] = useState<MessageListDto | null>(null)
  const [deleting, setDeleting] = useState<MessageListDto | null>(null)

  const { data, isLoading, error } = useMessageList(folder, {
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    filter: search || undefined,
    messageType: messageType ?? undefined,
    isRead: isRead === '' ? undefined : isRead === 'true',
  })

  const unread = useUnreadMessageCount()
  const users = useUserLookup()
  const markRead = useMarkMessageRead()
  const remove = useDelete(MESSAGE, { onSuccess: () => setDeleting(null) })

  /** One batched lookup feeds every correspondent cell; the table never asks per row. */
  const userNames = useMemo(() => {
    const map = new Map<number, string>()
    for (const user of users.data?.items ?? []) map.set(user.id, user.displayName)
    return map
  }, [users.data])

  function userLabel(id: number) {
    return userNames.get(id) ?? t('message.list.userFallback', { id })
  }

  /** Opening an inbox message marks it read — only the recipient may, so never in "sent". */
  function open(message: MessageListDto) {
    setReading(message)
    if (folder === 'inbox' && !message.isRead) markRead.mutate(message.id)
  }

  const columns: Column<MessageListDto>[] = [
    {
      key: 'correspondent',
      header: folder === 'inbox' ? t('message.fields.sender') : t('message.fields.recipient'),
      render: (row) => (
        <span className={row.isRead || folder === 'sent' ? '' : 'fw-bold'}>
          {userLabel(folder === 'inbox' ? row.senderId : row.recipientId)}
        </span>
      ),
    },
    {
      key: 'content',
      header: t('message.fields.content'),
      render: (row) => (
        <Button variant="link" className="p-0 text-start text-decoration-none"
          onClick={() => open(row)}
        >
          <span className={row.isRead || folder === 'sent' ? '' : 'fw-semibold'}>
            {excerpt(row.content)}
          </span>
        </Button>
      ),
    },
    {
      key: 'messageType',
      header: t('message.fields.messageType'),
      render: (row) => (
        <Badge variant="info">{t(`enums.messageType.${row.messageType}`)}</Badge>
      ),
    },
    {
      key: 'creationTime',
      header: t('message.fields.sentAt'),
      render: (row) => formatDateTime(row.creationTime) ?? t('common.none'),
    },
    {
      key: 'isRead',
      header: t('message.fields.readState'),
      align: 'center',
      render: (row) => (
        <Badge variant={row.isRead ? 'success' : 'warning'}>
          {row.isRead ? t('message.list.read') : t('message.list.unread')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '200px',
      render: (row) => (
        <div className="d-flex justify-content-end gap-2">
          <Button variant="light" size="sm" 
            onClick={() => open(row)}
            aria-label={t('message.list.openAria')}
          >
            {t('message.list.open')}
          </Button>
          {/*
            `DELETE api/message/{id}` is refused for anyone but the sender, so the control is
            offered in the sent folder only rather than being shown and then failing.
          */}
          {folder === 'sent' && (
            <Button variant="light" size="sm" 
              onClick={() => setDeleting(row)}
              aria-label={t('message.list.deleteAria')}
            >
              {t('common.delete')}
            </Button>
          )}
        </div>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('message.list.title')}
        description={t('message.list.description')}
        action={
          <Button variant="primary" onClick={() => setIsComposeOpen(true)}>
            {t('message.list.compose')}
          </Button>
        }
      />

      {unread.data && unread.data.unreadCount > 0 && (
        <Alert variant="warning">
          {t('message.list.unreadCount', { count: unread.data.unreadCount })}
        </Alert>
      )}

      <Card
        
        header={
          <Tabs
            items={FOLDERS.map((name) => ({ key: name, label: t(`message.folders.${name}`) }))}
            activeKey={folder}
            onChange={(key) => {
              setFolder(key as MessageFolder)
              setPage(1)
            }}
            variant="default"
          />
        }
      >
        <div className="card-body pb-0">
          <SearchBar
            value={search}
            onChange={(value) => {
              setSearch(value)
              setPage(1)
            }}
            placeholder={t('message.list.searchPlaceholder')}
          >
            <div style={{ minWidth: 220 }}>
              <label htmlFor="message-type-filter" className="visually-hidden">
                {t('message.filters.messageType')}
              </label>
              <Select<MessageType>
                id="message-type-filter"
                options={MESSAGE_TYPES.map((value) => ({
                  value,
                  label: t(`enums.messageType.${value}`),
                }))}
                value={messageType}
                placeholder={t('message.filters.allTypes')}
                onChange={(value) => {
                  setMessageType(value)
                  setPage(1)
                }}
              />
            </div>
            <div style={{ minWidth: 170 }}>
              <label htmlFor="message-read-filter" className="visually-hidden">
                {t('message.filters.readState')}
              </label>
              <Select
                id="message-read-filter"
                options={[
                  { value: 'false', label: t('message.list.unread') },
                  { value: 'true', label: t('message.list.read') },
                ]}
                value={isRead === '' ? null : isRead}
                placeholder={t('common.all')}
                onChange={(value) => {
                  setIsRead(value ?? '')
                  setPage(1)
                }}
              />
            </div>
          </SearchBar>
        </div>

        <div className="card-body p-0">
          <DataTable
            label={t(`message.folders.${folder}`)}
            columns={columns}
            rows={data?.items}
            rowKey={(row) => row.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('message.list.empty')}
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
      </Card>

      <ComposeModal isOpen={isComposeOpen} onClose={() => setIsComposeOpen(false)} />

      <Modal
        title={t('message.reader.title')}
        isOpen={!!reading}
        onClose={() => setReading(null)}
        size="lg"
      >
        {reading && (
          <>
            <dl className="row mb-3" style={{ fontSize: '0.9375rem' }}>
              <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
                {t('message.fields.sender')}
              </dt>
              <dd className="col-sm-9">{userLabel(reading.senderId)}</dd>
              <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
                {t('message.fields.recipient')}
              </dt>
              <dd className="col-sm-9">{userLabel(reading.recipientId)}</dd>
              <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
                {t('message.fields.sentAt')}
              </dt>
              <dd className="col-sm-9">{formatDateTime(reading.creationTime) ?? ''}</dd>
              <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
                {t('message.fields.messageType')}
              </dt>
              <dd className="col-sm-9">{t(`enums.messageType.${reading.messageType}`)}</dd>
            </dl>
            <p className="mb-0" style={{ whiteSpace: 'pre-wrap', color: 'var(--kt-gray-700)' }}>
              {reading.content}
            </p>
          </>
        )}
      </Modal>

      <ConfirmDialog
        isOpen={!!deleting}
        title={t('message.list.deleteTitle')}
        message={t('message.list.deleteMessage')}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

/** Compose dialog. There is no sender field — the API takes it from the access token. */
function ComposeModal({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const { t } = useTranslation()

  const [recipientId, setRecipientId] = useState<number | null>(null)
  const [content, setContent] = useState('')
  const [messageType, setMessageType] = useState<MessageType>(MessageType.UserMessage)
  const [companyId, setCompanyId] = useState<number | null>(null)
  const [errors, setErrors] = useState<{ recipientId?: string; content?: string }>({})

  const users = useUserLookup()
  const companies = useCompanyLookup()
  const send = useSendMessage({ onSuccess: onClose })

  useEffect(() => {
    if (!isOpen) return
    setRecipientId(null)
    setContent('')
    setMessageType(MessageType.UserMessage)
    setCompanyId(null)
    setErrors({})
  }, [isOpen])

  function submit() {
    const nextErrors: { recipientId?: string; content?: string } = {}
    if (!recipientId) nextErrors.recipientId = t('message.compose.recipientRequired')
    if (!content.trim()) nextErrors.content = t('validation.required')

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return

    const input: SendMessageDto = {
      recipientId: recipientId as number,
      content: content.trim(),
      messageType,
      companyId,
    }
    send.mutate(input)
  }

  return (
    <Modal
      title={t('message.compose.title')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={submit}
      isBusy={send.isPending}
      confirmLabel={t('message.compose.send')}
      error={send.error ? errorMessage(send.error) : null}
      size="lg"
    >
      <div className="row g-3">
        <Select<number>
          id="compose-recipient"
          label={t('message.fields.recipient')}
          required
          error={errors.recipientId}
          className="col-md-6"
          placeholder={t('message.compose.selectRecipient')}
          options={(users.data?.items ?? []).map((user) => ({
            value: user.id,
            label: user.displayName,
          }))}
          value={recipientId}
          onChange={setRecipientId}
        />

        <Select<MessageType>
          id="compose-type"
          label={t('message.fields.messageType')}
          className="col-md-6"
          options={MESSAGE_TYPES.map((value) => ({
            value,
            label: t(`enums.messageType.${value}`),
          }))}
          value={messageType}
          onChange={(value) => setMessageType(value ?? messageType)}
        />

        <Select<number>
          id="compose-company"
          label={t('message.fields.company')}
          helpText={t('message.compose.companyHint')}
          placeholder={t('message.compose.noCompany')}
          options={(companies.data?.items ?? []).map((company) => ({
            value: company.id,
            label: company.displayName,
          }))}
          value={companyId}
          onChange={setCompanyId}
        />

        <TextArea
          id="compose-content"
          label={t('message.fields.content')}
          required
          error={errors.content}
          rows={6}
          maxLength={4000}
          value={content}
          onChange={setContent}
        />
      </div>
    </Modal>
  )
}
