import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, SearchBar, controlClass } from '@/components/Form'
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
  const [messageType, setMessageType] = useState('')
  const [isRead, setIsRead] = useState('')

  const [isComposeOpen, setIsComposeOpen] = useState(false)
  const [reading, setReading] = useState<MessageListDto | null>(null)
  const [deleting, setDeleting] = useState<MessageListDto | null>(null)

  const { data, isLoading, error } = useMessageList(folder, {
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    filter: search || undefined,
    messageType: messageType === '' ? undefined : (Number(messageType) as MessageType),
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
        <button
          type="button"
          className="btn btn-link p-0 text-start text-decoration-none"
          onClick={() => open(row)}
        >
          <span className={row.isRead || folder === 'sent' ? '' : 'fw-semibold'}>
            {excerpt(row.content)}
          </span>
        </button>
      ),
    },
    {
      key: 'messageType',
      header: t('message.fields.messageType'),
      render: (row) => (
        <span className="badge-light-info">{t(`enums.messageType.${row.messageType}`)}</span>
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
        <span className={row.isRead ? 'badge-light-success' : 'badge-light-warning'}>
          {row.isRead ? t('message.list.read') : t('message.list.unread')}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '200px',
      render: (row) => (
        <div className="d-flex justify-content-end gap-2">
          <button
            type="button"
            className="btn btn-sm btn-light-primary"
            onClick={() => open(row)}
            aria-label={t('message.list.openAria')}
          >
            {t('message.list.open')}
          </button>
          {/*
            `DELETE api/message/{id}` is refused for anyone but the sender, so the control is
            offered in the sent folder only rather than being shown and then failing.
          */}
          {folder === 'sent' && (
            <button
              type="button"
              className="btn btn-sm btn-light-danger"
              onClick={() => setDeleting(row)}
              aria-label={t('message.list.deleteAria')}
            >
              {t('common.delete')}
            </button>
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
          <button className="btn btn-primary" type="button" onClick={() => setIsComposeOpen(true)}>
            {t('message.list.compose')}
          </button>
        }
      />

      {unread.data && unread.data.unreadCount > 0 && (
        <div
          className="alert border-0"
          style={{ backgroundColor: 'var(--kt-warning-light)', color: 'var(--kt-warning)' }}
        >
          {t('message.list.unreadCount', { count: unread.data.unreadCount })}
        </div>
      )}

      <div className="card">
        <div className="card-header p-0 px-4">
          <ul className="nav nav-tabs border-0" role="tablist">
            {FOLDERS.map((name) => (
              <li className="nav-item" key={name} role="presentation">
                <button
                  type="button"
                  role="tab"
                  aria-selected={folder === name}
                  className={`nav-link border-0 px-3 py-3 ${folder === name ? 'active fw-semibold' : ''}`}
                  style={{
                    color: folder === name ? 'var(--kt-primary)' : 'var(--kt-gray-600)',
                    borderBottom: `2px solid ${folder === name ? 'var(--kt-primary)' : 'transparent'}`,
                    backgroundColor: 'transparent',
                  }}
                  onClick={() => {
                    setFolder(name)
                    setPage(1)
                  }}
                >
                  {t(`message.folders.${name}`)}
                </button>
              </li>
            ))}
          </ul>
        </div>

        <div className="card-body pb-0">
          <SearchBar
            value={search}
            onChange={(value) => {
              setSearch(value)
              setPage(1)
            }}
            placeholder={t('message.list.searchPlaceholder')}
          >
            <div>
              <label htmlFor="message-type-filter" className="visually-hidden">
                {t('message.filters.messageType')}
              </label>
              <select
                id="message-type-filter"
                className="form-select"
                style={{ minWidth: 220 }}
                value={messageType}
                onChange={(event) => {
                  setMessageType(event.target.value)
                  setPage(1)
                }}
              >
                <option value="">{t('message.filters.allTypes')}</option>
                {MESSAGE_TYPES.map((value) => (
                  <option key={value} value={value}>
                    {t(`enums.messageType.${value}`)}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="message-read-filter" className="visually-hidden">
                {t('message.filters.readState')}
              </label>
              <select
                id="message-read-filter"
                className="form-select"
                style={{ minWidth: 170 }}
                value={isRead}
                onChange={(event) => {
                  setIsRead(event.target.value)
                  setPage(1)
                }}
              >
                <option value="">{t('common.all')}</option>
                <option value="false">{t('message.list.unread')}</option>
                <option value="true">{t('message.list.read')}</option>
              </select>
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
      </div>

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

  const [recipientId, setRecipientId] = useState('')
  const [content, setContent] = useState('')
  const [messageType, setMessageType] = useState<MessageType>(MessageType.UserMessage)
  const [companyId, setCompanyId] = useState('')
  const [errors, setErrors] = useState<{ recipientId?: string; content?: string }>({})

  const users = useUserLookup()
  const companies = useCompanyLookup()
  const send = useSendMessage({ onSuccess: onClose })

  useEffect(() => {
    if (!isOpen) return
    setRecipientId('')
    setContent('')
    setMessageType(MessageType.UserMessage)
    setCompanyId('')
    setErrors({})
  }, [isOpen])

  function submit() {
    const nextErrors: { recipientId?: string; content?: string } = {}
    if (!recipientId) nextErrors.recipientId = t('message.compose.recipientRequired')
    if (!content.trim()) nextErrors.content = t('validation.required')

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return

    const input: SendMessageDto = {
      recipientId: Number(recipientId),
      content: content.trim(),
      messageType,
      companyId: companyId ? Number(companyId) : null,
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
        <Field
          label={t('message.fields.recipient')}
          htmlFor="compose-recipient"
          required
          error={errors.recipientId}
          className="col-md-6"
        >
          <select
            id="compose-recipient"
            className={controlClass('form-select', errors.recipientId)}
            value={recipientId}
            onChange={(event) => setRecipientId(event.target.value)}
          >
            <option value="">{t('message.compose.selectRecipient')}</option>
            {users.data?.items.map((user) => (
              <option key={user.id} value={user.id}>
                {user.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('message.fields.messageType')}
          htmlFor="compose-type"
          className="col-md-6"
        >
          <select
            id="compose-type"
            className="form-select"
            value={messageType}
            onChange={(event) => setMessageType(Number(event.target.value))}
          >
            {MESSAGE_TYPES.map((value) => (
              <option key={value} value={value}>
                {t(`enums.messageType.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('message.fields.company')}
          htmlFor="compose-company"
          hint={t('message.compose.companyHint')}
        >
          <select
            id="compose-company"
            className="form-select"
            value={companyId}
            onChange={(event) => setCompanyId(event.target.value)}
          >
            <option value="">{t('message.compose.noCompany')}</option>
            {companies.data?.items.map((company) => (
              <option key={company.id} value={company.id}>
                {company.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('message.fields.content')}
          htmlFor="compose-content"
          required
          error={errors.content}
        >
          <textarea
            id="compose-content"
            className={controlClass('form-control', errors.content)}
            rows={6}
            maxLength={4000}
            value={content}
            onChange={(event) => setContent(event.target.value)}
          />
        </Field>
      </div>
    </Modal>
  )
}
