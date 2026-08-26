import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, SearchBar, controlClass } from '@/components/Form'
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
  const [mailStatus, setMailStatus] = useState('')
  const [mailType, setMailType] = useState('')
  const [mailPriority, setMailPriority] = useState('')

  const [editingId, setEditingId] = useState<number | undefined>()
  const [isEditorOpen, setIsEditorOpen] = useState(false)
  const [detailId, setDetailId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState<MailListDto | null>(null)

  const { data, isLoading, error } = useMailList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    filter: search || undefined,
    mailStatus: mailStatus === '' ? undefined : (Number(mailStatus) as MailStatus),
    mailType: mailType === '' ? undefined : (Number(mailType) as MailType),
    mailPriority: mailPriority === '' ? undefined : (Number(mailPriority) as MailPriority),
  })

  const editing = useEntity<MailDto>(MAIL, editingId)
  const remove = useDelete(MAIL, { onSuccess: () => setDeleting(null) })
  const queue = useQueueMail()

  const columns: Column<MailListDto>[] = [
    {
      key: 'topic',
      header: t('mail.fields.topic'),
      render: (row) => (
        <button
          type="button"
          className="btn btn-link p-0 text-start text-decoration-none fw-semibold"
          onClick={() => setDetailId(row.id)}
        >
          {row.topic}
        </button>
      ),
    },
    { key: 'recipient', header: t('mail.fields.recipient'), render: (row) => row.recipient },
    { key: 'sender', header: t('mail.fields.sender'), render: (row) => row.sender },
    {
      key: 'mailStatus',
      header: t('mail.fields.mailStatus'),
      align: 'center',
      render: (row) => (
        <span className={MAIL_STATUS_BADGE[row.mailStatus]}>
          {t(`enums.mailStatus.${row.mailStatus}`)}
        </span>
      ),
    },
    {
      key: 'mailPriority',
      header: t('mail.fields.mailPriority'),
      align: 'center',
      render: (row) => (
        <span className={MAIL_PRIORITY_BADGE[row.mailPriority]}>
          {t(`enums.mailPriority.${row.mailPriority}`)}
        </span>
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
              <button
                type="button"
                className="btn btn-sm btn-light-success"
                disabled={queue.isPending}
                onClick={() => queue.mutate(row.id)}
                aria-label={t('mail.list.queueAria', { topic: row.topic })}
              >
                {t('mail.list.queue')}
              </button>
            )}
            <button
              type="button"
              className="btn btn-sm btn-light-primary"
              disabled={!isEditable}
              title={isEditable ? undefined : t('mail.list.notEditable')}
              onClick={() => {
                setEditingId(row.id)
                setIsEditorOpen(true)
              }}
              aria-label={t('mail.list.editAria', { topic: row.topic })}
            >
              {t('common.edit')}
            </button>
            <button
              type="button"
              className="btn btn-sm btn-light-danger"
              onClick={() => setDeleting(row)}
              aria-label={t('mail.list.deleteAria', { topic: row.topic })}
            >
              {t('common.delete')}
            </button>
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
            <button
              className="btn btn-primary"
              type="button"
              onClick={() => {
                setEditingId(undefined)
                setIsEditorOpen(true)
              }}
            >
              {t('mail.list.create')}
            </button>
          ) : undefined
        }
      />

      {queue.error && (
        <div
          className="alert border-0"
          style={{ backgroundColor: 'var(--kt-danger-light)', color: 'var(--kt-danger)' }}
          role="alert"
        >
          {errorMessage(queue.error)}
        </div>
      )}

      <div className="card">
        <div className="card-header p-0 px-4">
          <ul className="nav nav-tabs border-0" role="tablist">
            {TABS.map((tab) => (
              <li className="nav-item" key={tab} role="presentation">
                <button
                  type="button"
                  role="tab"
                  aria-selected={activeTab === tab}
                  className={`nav-link border-0 px-3 py-3 ${activeTab === tab ? 'active fw-semibold' : ''}`}
                  style={{
                    color: activeTab === tab ? 'var(--kt-primary)' : 'var(--kt-gray-600)',
                    borderBottom: `2px solid ${activeTab === tab ? 'var(--kt-primary)' : 'transparent'}`,
                    backgroundColor: 'transparent',
                  }}
                  onClick={() => setActiveTab(tab)}
                >
                  {t(`mail.tabs.${tab}`)}
                </button>
              </li>
            ))}
          </ul>
        </div>

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
                <div>
                  <label htmlFor="mail-status-filter" className="visually-hidden">
                    {t('mail.filters.mailStatus')}
                  </label>
                  <select
                    id="mail-status-filter"
                    className="form-select"
                    style={{ minWidth: 170 }}
                    value={mailStatus}
                    onChange={(event) => {
                      setMailStatus(event.target.value)
                      setPage(1)
                    }}
                  >
                    <option value="">{t('mail.filters.allStatuses')}</option>
                    {MAIL_STATUSES.map((value) => (
                      <option key={value} value={value}>
                        {t(`enums.mailStatus.${value}`)}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label htmlFor="mail-type-filter" className="visually-hidden">
                    {t('mail.filters.mailType')}
                  </label>
                  <select
                    id="mail-type-filter"
                    className="form-select"
                    style={{ minWidth: 170 }}
                    value={mailType}
                    onChange={(event) => {
                      setMailType(event.target.value)
                      setPage(1)
                    }}
                  >
                    <option value="">{t('mail.filters.allTypes')}</option>
                    {MAIL_TYPES.map((value) => (
                      <option key={value} value={value}>
                        {t(`enums.mailType.${value}`)}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label htmlFor="mail-priority-filter" className="visually-hidden">
                    {t('mail.filters.mailPriority')}
                  </label>
                  <select
                    id="mail-priority-filter"
                    className="form-select"
                    style={{ minWidth: 170 }}
                    value={mailPriority}
                    onChange={(event) => {
                      setMailPriority(event.target.value)
                      setPage(1)
                    }}
                  >
                    <option value="">{t('mail.filters.allPriorities')}</option>
                    {MAIL_PRIORITIES.map((value) => (
                      <option key={value} value={value}>
                        {t(`enums.mailPriority.${value}`)}
                      </option>
                    ))}
                  </select>
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
      </div>

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
        <Field
          label={t('mail.fields.sender')}
          htmlFor="mail-sender"
          required
          error={errors.sender}
          className="col-md-6"
        >
          <input
            id="mail-sender"
            type="email"
            className={controlClass('form-control', errors.sender)}
            value={state.sender}
            onChange={(event) => setState((s) => ({ ...s, sender: event.target.value }))}
          />
        </Field>

        <Field
          label={t('mail.fields.recipient')}
          htmlFor="mail-recipient"
          required
          error={errors.recipient}
          hint={t('mail.editor.recipientHint')}
          className="col-md-6"
        >
          <input
            id="mail-recipient"
            type="text"
            className={controlClass('form-control', errors.recipient)}
            value={state.recipient}
            onChange={(event) => setState((s) => ({ ...s, recipient: event.target.value }))}
          />
        </Field>

        <Field label={t('mail.fields.topic')} htmlFor="mail-topic" required error={errors.topic}>
          <input
            id="mail-topic"
            type="text"
            className={controlClass('form-control', errors.topic)}
            value={state.topic}
            onChange={(event) => setState((s) => ({ ...s, topic: event.target.value }))}
          />
        </Field>

        <Field label={t('mail.fields.mailType')} htmlFor="mail-type" className="col-md-4">
          <select
            id="mail-type"
            className="form-select"
            value={state.mailType}
            onChange={(event) => setState((s) => ({ ...s, mailType: Number(event.target.value) }))}
          >
            {MAIL_TYPES.map((value) => (
              <option key={value} value={value}>
                {t(`enums.mailType.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('mail.fields.mailPriority')} htmlFor="mail-priority" className="col-md-4">
          <select
            id="mail-priority"
            className="form-select"
            value={state.mailPriority}
            onChange={(event) =>
              setState((s) => ({ ...s, mailPriority: Number(event.target.value) }))
            }
          >
            {MAIL_PRIORITIES.map((value) => (
              <option key={value} value={value}>
                {t(`enums.mailPriority.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('mail.fields.contentFormat')} htmlFor="mail-format" className="col-md-4">
          <select
            id="mail-format"
            className="form-select"
            value={state.contentFormat}
            onChange={(event) =>
              setState((s) => ({ ...s, contentFormat: Number(event.target.value) }))
            }
          >
            {CONTENT_FORMATS.map((value) => (
              <option key={value} value={value}>
                {t(`enums.contentFormat.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('mail.fields.content')}
          htmlFor="mail-content"
          required
          error={errors.content}
        >
          <textarea
            id="mail-content"
            className={controlClass('form-control font-monospace', errors.content)}
            rows={8}
            value={state.content}
            onChange={(event) => setState((s) => ({ ...s, content: event.target.value }))}
          />
        </Field>
      </div>
    </Modal>
  )
}

/** `GET api/mail/{id}/detail` — the mail with its attachments, plus attach and detach. */
function MailDetailModal({ mailId, onClose }: { mailId: number; onClose: () => void }) {
  const { t } = useTranslation()
  const [documentId, setDocumentId] = useState('')

  const detail = useMailDetail(mailId)
  const addAttachment = useAddMailAttachment(mailId)
  const removeAttachment = useRemoveMailAttachment(mailId)

  const attachmentError = addAttachment.error ?? removeAttachment.error

  return (
    <Modal title={t('mail.detail.title')} isOpen onClose={onClose} size="lg">
      {detail.isLoading ? (
        <Spinner />
      ) : detail.error ? (
        <div
          className="alert border-0 mb-0"
          style={{ backgroundColor: 'var(--kt-danger-light)', color: 'var(--kt-danger)' }}
          role="alert"
        >
          {errorMessage(detail.error)}
        </div>
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
              <span className={MAIL_STATUS_BADGE[detail.data.mail.mailStatus]}>
                {t(`enums.mailStatus.${detail.data.mail.mailStatus}`)}
              </span>
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

          {attachmentError && (
            <div
              className="alert border-0"
              style={{ backgroundColor: 'var(--kt-danger-light)', color: 'var(--kt-danger)' }}
              role="alert"
            >
              {errorMessage(attachmentError)}
            </div>
          )}

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
                  <button
                    type="button"
                    className="btn btn-sm btn-light-danger"
                    disabled={removeAttachment.isPending}
                    onClick={() => removeAttachment.mutate(entry.attachment.id)}
                    aria-label={t('mail.detail.removeAttachmentAria')}
                  >
                    {t('mail.detail.removeAttachment')}
                  </button>
                </li>
              ))}
            </ul>
          )}

          <div className="row g-2 align-items-end">
            <Field
              label={t('mail.detail.attachDocument')}
              htmlFor="mail-attachment-document"
              hint={t('mail.detail.attachHint')}
              className="col-sm-8"
            >
              <input
                id="mail-attachment-document"
                type="number"
                min={1}
                className="form-control"
                value={documentId}
                onChange={(event) => setDocumentId(event.target.value)}
              />
            </Field>
            <div className="col-sm-4">
              <button
                type="button"
                className="btn btn-light-primary w-100"
                disabled={!documentId || addAttachment.isPending}
                onClick={() => {
                  addAttachment.mutate(
                    { documentId: Number(documentId), orderNo: 0 },
                    { onSuccess: () => setDocumentId('') },
                  )
                }}
              >
                {t('mail.detail.attach')}
              </button>
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
      <div
        className="alert border-0"
        style={{ backgroundColor: 'var(--kt-warning-light)', color: 'var(--kt-warning)' }}
      >
        {t('mail.settings.unavailable')}
      </div>

      <fieldset disabled aria-describedby="mail-settings-notice">
        <p id="mail-settings-notice" className="mb-3" style={{ color: 'var(--kt-gray-600)' }}>
          {t('mail.settings.preview')}
        </p>

        <div className="row g-3">
          <Field label={t('mail.settings.host')} htmlFor="smtp-host" className="col-md-6">
            <input id="smtp-host" type="text" className="form-control" defaultValue="" />
          </Field>

          <Field label={t('mail.settings.port')} htmlFor="smtp-port" className="col-md-3">
            <input id="smtp-port" type="number" className="form-control" defaultValue="" />
          </Field>

          <Field label={t('mail.settings.useSsl')} htmlFor="smtp-ssl" className="col-md-3">
            <select id="smtp-ssl" className="form-select" defaultValue="">
              <option value="">{t('common.none')}</option>
              <option value="true">{t('common.yes')}</option>
              <option value="false">{t('common.no')}</option>
            </select>
          </Field>

          <Field label={t('mail.settings.userName')} htmlFor="smtp-user" className="col-md-6">
            <input id="smtp-user" type="text" className="form-control" defaultValue="" />
          </Field>

          <Field
            label={t('mail.settings.password')}
            htmlFor="smtp-password"
            hint={t('mail.settings.passwordHint')}
            className="col-md-6"
          >
            <input
              id="smtp-password"
              type="password"
              className="form-control"
              autoComplete="new-password"
              placeholder={t('mail.settings.passwordPlaceholder')}
              defaultValue=""
            />
          </Field>

          <Field
            label={t('mail.settings.defaultSender')}
            htmlFor="smtp-default-sender"
            className="col-md-6"
          >
            <input id="smtp-default-sender" type="email" className="form-control" defaultValue="" />
          </Field>
        </div>
      </fieldset>
    </div>
  )
}
