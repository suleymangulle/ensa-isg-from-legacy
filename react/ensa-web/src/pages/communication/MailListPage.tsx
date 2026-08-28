import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Alert, Badge, Button, Card, Input, NumberInput, Select, Tabs, TextArea } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
import { useCreate, useDelete, useUpdate } from '@/api/mutations'
import { errorMessage } from '@/api/http'
import { useEntity } from '@/api/endpoints'
import { ContentFormat, MailPriority, MailStatus, MailType } from '@/api/enums'
import { formatDateTime } from '@/utils/format'
import {
  MAIL,
  MAIL_PRIORITY_BADGE,
  MAIL_STATUS_BADGE,
  useAddMailAttachment,
  useMailDetail,
  useMailList,
  useQueueMail,
  useRemoveMailAttachment,
  type MailDto,
  type MailListDto,
  type SaveMailDto,
} from './api'
import { CONTENT_FORMATS, MAIL_PRIORITIES, MAIL_STATUSES, MAIL_TYPES } from './helpers'

const PAGE_SIZE = 20

const TABS = ['log', 'settings'] as const
type TabKey = (typeof TABS)[number]

/** Statuses whose body may still be edited; the API refuses an edit once the mail was sent. */
const EDITABLE_STATUSES: MailStatus[] = [MailStatus.Draft, MailStatus.Failed]

/**
 * Outbound mail — the queue log plus the SMTP settings panel.
 *
 * `api/mail` has no "send" route by design: the API owns the queue and a background worker does
 * the delivery, reporting back through `mark-sent` and `mark-failed`. The screen therefore
 * offers *queue*, not *send*.
 */
export default function MailListPage() {
  const { t } = useTranslation()
  const [activeTab, setActiveTab] = useState<TabKey>('log')

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [mailStatus, setMailStatus] = useState<MailStatus | null>(null)
  const [mailType, setMailType] = useState<MailType | null>(null)
  const [mailPriority, setMailPriority] = useState<MailPriority | null>(null)

  const [editingId, setEditingId] = useState<number | undefined>()
  const [isEditorOpen, setIsEditorOpen] = useState(false)
  const [detailId, setDetailId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState<MailListDto | null>(null)

  const { data, isLoading, error } = useMailList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    filter: search || undefined,
    mailStatus: mailStatus ?? undefined,
    mailType: mailType ?? undefined,
    mailPriority: mailPriority ?? undefined,
  })

  const editing = useEntity<MailDto>(MAIL, editingId)
  const remove = useDelete(MAIL, { onSuccess: () => setDeleting(null) })
  const queue = useQueueMail()

  const columns: Column<MailListDto>[] = [
    {
      key: 'topic',
      header: t('mail.fields.topic'),
      render: (row) => (
        <Button variant="link" className="p-0 text-start text-decoration-none fw-semibold"
          onClick={() => setDetailId(row.id)}
        >
          {row.topic}
        </Button>
      ),
    },
    { key: 'recipient', header: t('mail.fields.recipient'), render: (row) => row.recipient },
    { key: 'sender', header: t('mail.fields.sender'), render: (row) => row.sender },
    {
      key: 'mailStatus',
      header: t('mail.fields.mailStatus'),
      align: 'center',
      render: (row) => (
        <Badge variant={MAIL_STATUS_BADGE[row.mailStatus]}>
          {t(`enums.mailStatus.${row.mailStatus}`)}
        </Badge>
      ),
    },
    {
      key: 'mailPriority',
      header: t('mail.fields.mailPriority'),
      align: 'center',
      render: (row) => (
        <Badge variant={MAIL_PRIORITY_BADGE[row.mailPriority]}>
          {t(`enums.mailPriority.${row.mailPriority}`)}
        </Badge>
      ),
    },
    {
      key: 'mailType',
      header: t('mail.fields.mailType'),
      render: (row) => t(`enums.mailType.${row.mailType}`),
    },
    {
      key: 'attemptCount',
      header: t('mail.fields.attemptCount'),
      align: 'end',
      render: (row) => row.attemptCount,
    },
    {
      key: 'submissionDate',
      header: t('mail.fields.submissionDate'),
      render: (row) => formatDateTime(row.submissionDate) ?? t('common.none'),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '260px',
      render: (row) => {
        const isEditable = EDITABLE_STATUSES.includes(row.mailStatus)
        return (
          <div className="d-flex justify-content-end gap-2">
            {isEditable && (
              <Button variant="light" size="sm" 
                disabled={queue.isPending}
                onClick={() => queue.mutate(row.id)}
                aria-label={t('mail.list.queueAria', { topic: row.topic })}
              >
                {t('mail.list.queue')}
              </Button>
            )}
            <Button variant="light" size="sm" 
              disabled={!isEditable}
              title={isEditable ? undefined : t('mail.list.notEditable')}
              onClick={() => {
                setEditingId(row.id)
                setIsEditorOpen(true)
              }}
              aria-label={t('mail.list.editAria', { topic: row.topic })}
            >
              {t('common.edit')}
            </Button>
            <Button variant="light" size="sm" 
              onClick={() => setDeleting(row)}
              aria-label={t('mail.list.deleteAria', { topic: row.topic })}
            >
              {t('common.delete')}
            </Button>
          </div>
        )
      },
    },
  ]

  return (
    <>
      <PageTitle
        title={t('mail.list.title')}
        description={t('mail.list.description')}
        action={
          activeTab === 'log' ? (
            <Button variant="primary"
              onClick={() => {
                setEditingId(undefined)
                setIsEditorOpen(true)
              }}
            >
              {t('mail.list.create')}
            </Button>
          ) : undefined
        }
      />

      {queue.error && <Alert variant="danger">{errorMessage(queue.error)}</Alert>}

      <Card
        
        header={
          <Tabs
            items={TABS.map((tab) => ({ key: tab, label: t(`mail.tabs.${tab}`) }))}
            activeKey={activeTab}
            onChange={(key) => setActiveTab(key as TabKey)}
            variant="default"
          />
        }
      >
        {activeTab === 'log' ? (
          <>
            <div className="card-body pb-0">
              <SearchBar
                value={search}
                onChange={(value) => {
                  setSearch(value)
                  setPage(1)
                }}
                placeholder={t('mail.list.searchPlaceholder')}
              >
                <div style={{ minWidth: 170 }}>
                  <label htmlFor="mail-status-filter" className="visually-hidden">
                    {t('mail.filters.mailStatus')}
                  </label>
                  <Select<MailStatus>
                    id="mail-status-filter"
                    options={MAIL_STATUSES.map((value) => ({
                      value,
                      label: t(`enums.mailStatus.${value}`),
                    }))}
                    value={mailStatus}
                    placeholder={t('mail.filters.allStatuses')}
                    onChange={(value) => {
                      setMailStatus(value)
                      setPage(1)
                    }}
                  />
                </div>
                <div style={{ minWidth: 170 }}>
                  <label htmlFor="mail-type-filter" className="visually-hidden">
                    {t('mail.filters.mailType')}
                  </label>
                  <Select<MailType>
                    id="mail-type-filter"
                    options={MAIL_TYPES.map((value) => ({
                      value,
                      label: t(`enums.mailType.${value}`),
                    }))}
                    value={mailType}
                    placeholder={t('mail.filters.allTypes')}
                    onChange={(value) => {
                      setMailType(value)
                      setPage(1)
                    }}
                  />
                </div>
                <div style={{ minWidth: 170 }}>
                  <label htmlFor="mail-priority-filter" className="visually-hidden">
                    {t('mail.filters.mailPriority')}
                  </label>
                  <Select<MailPriority>
                    id="mail-priority-filter"
                    options={MAIL_PRIORITIES.map((value) => ({
                      value,
                      label: t(`enums.mailPriority.${value}`),
                    }))}
                    value={mailPriority}
                    placeholder={t('mail.filters.allPriorities')}
                    onChange={(value) => {
                      setMailPriority(value)
                      setPage(1)
                    }}
                  />
                </div>
              </SearchBar>
            </div>

            <div className="card-body p-0">
              <DataTable
                label={t('mail.list.title')}
                columns={columns}
                rows={data?.items}
                rowKey={(row) => row.id}
                isLoading={isLoading}
                error={error ? errorMessage(error) : null}
                emptyMessage={t('mail.list.empty')}
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
          </>
        ) : (
          <MailSettingsPanel />
        )}
      </Card>

      {isEditorOpen && (!editingId || editing.data) && (
        <MailEditor
          isOpen
          mail={editingId ? editing.data : undefined}
          onClose={() => {
            setIsEditorOpen(false)
            setEditingId(undefined)
          }}
        />
      )}

      {detailId !== null && (
        <MailDetailModal mailId={detailId} onClose={() => setDetailId(null)} />
      )}

      <ConfirmDialog
        isOpen={!!deleting}
        title={t('mail.list.deleteTitle')}
        message={t('mail.list.deleteMessage', { topic: deleting?.topic ?? '' })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

interface EditorState {
  sender: string
  recipient: string
  topic: string
  content: string
  contentFormat: ContentFormat
  mailPriority: MailPriority
  mailType: MailType
}

const EMPTY_EDITOR: EditorState = {
  sender: '',
  recipient: '',
  topic: '',
  content: '',
  contentFormat: ContentFormat.PlainText,
  mailPriority: MailPriority.Normal,
  mailType: MailType.Normal,
}

/** Create and edit dialog. New mails always start as a draft; the status is never written. */
function MailEditor({
  isOpen,
  mail,
  onClose,
}: {
  isOpen: boolean
  mail?: MailDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [state, setState] = useState<EditorState>(EMPTY_EDITOR)
  const [errors, setErrors] = useState<Partial<Record<keyof EditorState, string>>>({})

  useEffect(() => {
    if (!isOpen) return
    setErrors({})
    setState(
      mail
        ? {
            sender: mail.sender,
            recipient: mail.recipient,
            topic: mail.topic,
            content: mail.content,
            contentFormat: mail.contentFormat,
            mailPriority: mail.mailPriority,
            mailType: mail.mailType,
          }
        : EMPTY_EDITOR,
    )
  }, [isOpen, mail])

  const create = useCreate<SaveMailDto, MailDto>(MAIL, { onSuccess: onClose })
  const update = useUpdate<SaveMailDto, MailDto>(MAIL, { onSuccess: onClose })
  const mutation = mail ? update : create

  function submit() {
    const nextErrors: Partial<Record<keyof EditorState, string>> = {}
    if (!state.sender.trim()) nextErrors.sender = t('validation.required')
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(state.sender.trim())) {
      nextErrors.sender = t('mail.editor.invalidSender')
    }
    if (!state.recipient.trim()) nextErrors.recipient = t('validation.required')
    if (!state.topic.trim()) nextErrors.topic = t('validation.required')
    if (!state.content.trim()) nextErrors.content = t('validation.required')

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return

    const input: SaveMailDto = {
      sender: state.sender.trim(),
      recipient: state.recipient.trim(),
      topic: state.topic.trim(),
      content: state.content,
      contentFormat: state.contentFormat,
      mailPriority: state.mailPriority,
      mailType: state.mailType,
    }

    if (mail) update.mutate({ id: mail.id, input })
    else create.mutate(input)
  }

  return (
    <Modal
      title={mail ? t('mail.editor.editTitle') : t('mail.editor.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={submit}
      isBusy={mutation.isPending}
      error={mutation.error ? errorMessage(mutation.error) : null}
      size="lg"
    >
      <div className="row g-3">
        <Input
          id="mail-sender"
          type="email"
          label={t('mail.fields.sender')}
          required
          error={errors.sender}
          className="col-md-6"
          value={state.sender}
          onChange={(value) => setState((s) => ({ ...s, sender: value }))}
        />

        <Input
          id="mail-recipient"
          type="text"
          label={t('mail.fields.recipient')}
          required
          error={errors.recipient}
          helpText={t('mail.editor.recipientHint')}
          className="col-md-6"
          value={state.recipient}
          onChange={(value) => setState((s) => ({ ...s, recipient: value }))}
        />

        <Input
          id="mail-topic"
          type="text"
          label={t('mail.fields.topic')}
          required
          error={errors.topic}
          value={state.topic}
          onChange={(value) => setState((s) => ({ ...s, topic: value }))}
        />

        <Select<MailType>
          id="mail-type"
          label={t('mail.fields.mailType')}
          className="col-md-4"
          options={MAIL_TYPES.map((value) => ({ value, label: t(`enums.mailType.${value}`) }))}
          value={state.mailType}
          onChange={(value) => setState((s) => ({ ...s, mailType: value ?? s.mailType }))}
        />

        <Select<MailPriority>
          id="mail-priority"
          label={t('mail.fields.mailPriority')}
          className="col-md-4"
          options={MAIL_PRIORITIES.map((value) => ({
            value,
            label: t(`enums.mailPriority.${value}`),
          }))}
          value={state.mailPriority}
          onChange={(value) => setState((s) => ({ ...s, mailPriority: value ?? s.mailPriority }))}
        />

        <Select<ContentFormat>
          id="mail-format"
          label={t('mail.fields.contentFormat')}
          className="col-md-4"
          options={CONTENT_FORMATS.map((value) => ({
            value,
            label: t(`enums.contentFormat.${value}`),
          }))}
          value={state.contentFormat}
          onChange={(value) => setState((s) => ({ ...s, contentFormat: value ?? s.contentFormat }))}
        />

        <TextArea
          id="mail-content"
          label={t('mail.fields.content')}
          required
          error={errors.content}
          rows={8}
          value={state.content}
          onChange={(value) => setState((s) => ({ ...s, content: value }))}
        />
      </div>
    </Modal>
  )
}

/** `GET api/mail/{id}/detail` — the mail with its attachments, plus attach and detach. */
function MailDetailModal({ mailId, onClose }: { mailId: number; onClose: () => void }) {
  const { t } = useTranslation()
  const [documentId, setDocumentId] = useState<number | null>(null)

  const detail = useMailDetail(mailId)
  const addAttachment = useAddMailAttachment(mailId)
  const removeAttachment = useRemoveMailAttachment(mailId)

  const attachmentError = addAttachment.error ?? removeAttachment.error

  return (
    <Modal title={t('mail.detail.title')} isOpen onClose={onClose} size="lg">
      {detail.isLoading ? (
        <Spinner />
      ) : detail.error ? (
        <Alert variant="danger" className="mb-0">
          {errorMessage(detail.error)}
        </Alert>
      ) : detail.data ? (
        <>
          <dl className="row" style={{ fontSize: '0.9375rem' }}>
            <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
              {t('mail.fields.topic')}
            </dt>
            <dd className="col-sm-9 fw-semibold">{detail.data.mail.topic}</dd>
            <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
              {t('mail.fields.sender')}
            </dt>
            <dd className="col-sm-9">{detail.data.mail.sender}</dd>
            <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
              {t('mail.fields.recipient')}
            </dt>
            <dd className="col-sm-9">{detail.data.mail.recipient}</dd>
            <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
              {t('mail.fields.mailStatus')}
            </dt>
            <dd className="col-sm-9">
              <Badge variant={MAIL_STATUS_BADGE[detail.data.mail.mailStatus]}>
                {t(`enums.mailStatus.${detail.data.mail.mailStatus}`)}
              </Badge>
            </dd>
            {detail.data.mail.errorMessage && (
              <>
                <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
                  {t('mail.fields.errorMessage')}
                </dt>
                <dd className="col-sm-9" style={{ color: 'var(--kt-danger)' }}>
                  {detail.data.mail.errorMessage}
                </dd>
              </>
            )}
          </dl>

          <div
            className="p-3 mb-3"
            style={{ backgroundColor: 'var(--kt-gray-100)', borderRadius: '0.5rem' }}
          >
            <p
              className="mb-0"
              style={{ whiteSpace: 'pre-wrap', color: 'var(--kt-gray-700)', fontSize: '0.9375rem' }}
            >
              {detail.data.mail.content}
            </p>
          </div>

          <h3 className="h6 fw-semibold" style={{ color: 'var(--kt-gray-800)' }}>
            {t('mail.detail.attachments')}
          </h3>

          {attachmentError && <Alert variant="danger">{errorMessage(attachmentError)}</Alert>}

          {detail.data.attachments.length === 0 ? (
            <p className="mb-3" style={{ color: 'var(--kt-gray-500)' }}>
              {t('mail.detail.noAttachments')}
            </p>
          ) : (
            <ul className="list-unstyled mb-3 d-flex flex-column gap-2">
              {detail.data.attachments.map((entry) => (
                <li
                  key={entry.attachment.id}
                  className="d-flex align-items-center justify-content-between gap-2 p-2"
                  style={{ border: '1px solid var(--kt-border-color)', borderRadius: '0.425rem' }}
                >
                  <span>
                    {entry.document?.displayName ??
                      t('mail.detail.documentFallback', { id: entry.attachment.documentId })}
                  </span>
                  <Button variant="light" size="sm" 
                    disabled={removeAttachment.isPending}
                    onClick={() => removeAttachment.mutate(entry.attachment.id)}
                    aria-label={t('mail.detail.removeAttachmentAria')}
                  >
                    {t('mail.detail.removeAttachment')}
                  </Button>
                </li>
              ))}
            </ul>
          )}

          <div className="row g-2 align-items-end">
            <NumberInput
              id="mail-attachment-document"
              label={t('mail.detail.attachDocument')}
              helpText={t('mail.detail.attachHint')}
              className="col-sm-8"
              min={1}
              value={documentId}
              onChange={setDocumentId}
            />
            <div className="col-sm-4">
              <Button variant="light" className="w-100"
                disabled={!documentId || addAttachment.isPending}
                onClick={() => {
                  addAttachment.mutate(
                    { documentId: documentId as number, orderNo: 0 },
                    { onSuccess: () => setDocumentId(null) },
                  )
                }}
              >
                {t('mail.detail.attach')}
              </Button>
            </div>
          </div>
        </>
      ) : null}
    </Modal>
  )
}

/**
 * SMTP settings.
 *
 * **There is no settings endpoint.** `MailController` exposes thirteen routes and none of them
 * reads or writes `EmailSettings`, so nothing here is wired up: the form is rendered disabled to
 * document the shape the panel will take, and no request is made. Inventing a route would be the
 * exact failure `tools/api-tests/frontend_routes.py` exists to catch.
 *
 * The password stays write-only whichever way it lands: the API is not to return a stored value,
 * so the field is always rendered empty and would only ever be sent when someone types into it.
 */
function MailSettingsPanel() {
  const { t } = useTranslation()

  return (
    <div className="card-body">
      <Alert variant="warning">{t('mail.settings.unavailable')}</Alert>

      <fieldset disabled aria-describedby="mail-settings-notice">
        <p id="mail-settings-notice" className="mb-3" style={{ color: 'var(--kt-gray-600)' }}>
          {t('mail.settings.preview')}
        </p>

        <div className="row g-3">
          <Input id="smtp-host" type="text" label={t('mail.settings.host')} className="col-md-6" />

          <NumberInput id="smtp-port" label={t('mail.settings.port')} className="col-md-3" />

          <Select
            id="smtp-ssl"
            label={t('mail.settings.useSsl')}
            className="col-md-3"
            placeholder={t('common.none')}
            options={[
              { value: 'true', label: t('common.yes') },
              { value: 'false', label: t('common.no') },
            ]}
          />

          <Input id="smtp-user" type="text" label={t('mail.settings.userName')} className="col-md-6" />

          <Input
            id="smtp-password"
            type="password"
            label={t('mail.settings.password')}
            helpText={t('mail.settings.passwordHint')}
            className="col-md-6"
            inputProps={{ autoComplete: 'new-password' }}
            placeholder={t('mail.settings.passwordPlaceholder')}
          />

          <Input
            id="smtp-default-sender"
            type="email"
            label={t('mail.settings.defaultSender')}
            className="col-md-6"
          />
        </div>
      </fieldset>
    </div>
  )
}
