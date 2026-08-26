import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Field, Modal, controlClass } from '@/components/Form'
import { useCreate, useUpdate } from '@/api/mutations'
import { errorMessage } from '@/api/http'
import { DocumentOwnerType } from '@/api/enums'
import {
  DOCUMENT,
  useCompanyLookup,
  useDocumentByHash,
  type DocumentDto,
  type SaveDocumentDto,
} from './api'
import { OWNER_TYPES, canHashLocally, extensionOf, sha256OfFile } from './helpers'

interface DocumentFormModalProps {
  isOpen: boolean
  /** `undefined` opens the dialog in create mode. */
  document?: DocumentDto
  onClose: () => void
}

interface FormState {
  documentName: string
  documentCategoryId: string
  companyId: string
  extension: string
  contentType: string
  sizeBytes: string
  sha256: string
  ownerType: DocumentOwnerType
  ownerRecordId: string
  isActive: boolean
}

const EMPTY: FormState = {
  documentName: '',
  documentCategoryId: '',
  companyId: '',
  extension: '',
  contentType: '',
  sizeBytes: '0',
  sha256: '',
  ownerType: DocumentOwnerType.Unspecified,
  ownerRecordId: '',
  isActive: true,
}

/** Optional integer field -> `number | null`. */
function optionalInt(value: string): number | null {
  const trimmed = value.trim()
  if (!trimmed) return null
  const parsed = Number(trimmed)
  return Number.isFinite(parsed) ? Math.trunc(parsed) : null
}

/**
 * Create and edit dialog for document metadata.
 *
 * Picking a local file fills the metadata in and computes its SHA-256 digest, which the
 * duplicate check is then run against. The bytes are never sent — there is no upload endpoint —
 * so the file input exists purely to derive an accurate name, size, MIME type and digest
 * instead of asking a human to type them.
 */
export default function DocumentFormModal({
  isOpen,
  document: existing,
  onClose,
}: DocumentFormModalProps) {
  const { t } = useTranslation()
  const isEdit = !!existing

  const [form, setForm] = useState<FormState>(EMPTY)
  const [errors, setErrors] = useState<Partial<Record<keyof FormState, string>>>({})
  const [hashError, setHashError] = useState<string | null>(null)
  const [isHashing, setIsHashing] = useState(false)

  const companies = useCompanyLookup()

  // Duplicate lookup: only meaningful while adding a second row for bytes already on file.
  const duplicate = useDocumentByHash(isEdit ? undefined : form.sha256)
  const duplicateOf = duplicate.data && duplicate.data.id !== existing?.id ? duplicate.data : null

  useEffect(() => {
    if (!isOpen) return
    setErrors({})
    setHashError(null)
    setForm(
      existing
        ? {
            documentName: existing.documentName,
            documentCategoryId: existing.documentCategoryId?.toString() ?? '',
            companyId: existing.companyId?.toString() ?? '',
            extension: existing.extension ?? '',
            contentType: existing.contentType ?? '',
            sizeBytes: existing.sizeBytes.toString(),
            sha256: existing.sha256 ?? '',
            ownerType: existing.ownerType,
            ownerRecordId: existing.ownerRecordId?.toString() ?? '',
            isActive: existing.isActive,
          }
        : EMPTY,
    )
  }, [isOpen, existing])

  const create = useCreate<SaveDocumentDto, DocumentDto>(DOCUMENT, { onSuccess: onClose })
  const update = useUpdate<SaveDocumentDto, DocumentDto>(DOCUMENT, { onSuccess: onClose })
  const mutation = isEdit ? update : create

  const saveError = useMemo(
    () => (mutation.error ? errorMessage(mutation.error) : null),
    [mutation.error],
  )

  function patch(next: Partial<FormState>) {
    setForm((current) => ({ ...current, ...next }))
  }

  async function onFileSelected(file: File | undefined) {
    if (!file) return
    setHashError(null)
    patch({
      documentName: file.name,
      extension: extensionOf(file.name) ?? '',
      contentType: file.type,
      sizeBytes: file.size.toString(),
    })

    if (!canHashLocally()) {
      setHashError(t('document.form.hashUnavailable'))
      return
    }

    setIsHashing(true)
    try {
      patch({ sha256: await sha256OfFile(file) })
    } catch {
      setHashError(t('document.form.hashFailed'))
    } finally {
      setIsHashing(false)
    }
  }

  function submit() {
    const nextErrors: Partial<Record<keyof FormState, string>> = {}
    if (!form.documentName.trim()) nextErrors.documentName = t('validation.required')

    const size = Number(form.sizeBytes)
    if (!Number.isFinite(size) || size < 0) nextErrors.sizeBytes = t('document.form.invalidSize')

    const digest = form.sha256.trim().toLowerCase()
    if (digest && !/^[0-9a-f]{64}$/.test(digest)) {
      nextErrors.sha256 = t('document.form.invalidSha256')
    }

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return

    const input: SaveDocumentDto = {
      documentName: form.documentName.trim(),
      documentCategoryId: optionalInt(form.documentCategoryId),
      companyId: optionalInt(form.companyId),
      extension: form.extension.trim() || null,
      contentType: form.contentType.trim() || null,
      sizeBytes: Math.trunc(size),
      sha256: digest || null,
      ownerType: form.ownerType,
      ownerRecordId: optionalInt(form.ownerRecordId),
      isActive: form.isActive,
    }

    if (isEdit && existing) update.mutate({ id: existing.id, input })
    else create.mutate(input)
  }

  return (
    <Modal
      title={isEdit ? t('document.form.editTitle') : t('document.form.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={submit}
      isBusy={mutation.isPending || isHashing}
      error={saveError}
      size="lg"
    >
      <div className="row g-3">
        {!isEdit && (
          <Field
            label={t('document.form.pickFile')}
            htmlFor="document-file"
            hint={t('document.form.pickFileHint')}
          >
            <input
              id="document-file"
              type="file"
              className="form-control"
              onChange={(event) => void onFileSelected(event.target.files?.[0])}
            />
          </Field>
        )}

        {hashError && (
          <div className="col-12">
            <div
              className="alert border-0 mb-0"
              style={{ backgroundColor: 'var(--kt-warning-light)', color: 'var(--kt-warning)' }}
              role="alert"
            >
              {hashError}
            </div>
          </div>
        )}

        {duplicateOf && (
          <div className="col-12">
            <div
              className="alert border-0 mb-0"
              style={{ backgroundColor: 'var(--kt-warning-light)', color: 'var(--kt-warning)' }}
              role="alert"
            >
              {t('document.form.duplicateWarning', { name: duplicateOf.documentName })}
            </div>
          </div>
        )}

        <Field
          label={t('document.fields.documentName')}
          htmlFor="document-name"
          required
          error={errors.documentName}
          className="col-md-8"
        >
          <input
            id="document-name"
            type="text"
            className={controlClass('form-control', errors.documentName)}
            value={form.documentName}
            onChange={(event) => patch({ documentName: event.target.value })}
          />
        </Field>

        <Field
          label={t('document.fields.extension')}
          htmlFor="document-extension"
          className="col-md-4"
        >
          <input
            id="document-extension"
            type="text"
            className="form-control"
            value={form.extension}
            onChange={(event) => patch({ extension: event.target.value })}
          />
        </Field>

        <Field
          label={t('document.fields.contentType')}
          htmlFor="document-content-type"
          className="col-md-6"
        >
          <input
            id="document-content-type"
            type="text"
            className="form-control"
            value={form.contentType}
            onChange={(event) => patch({ contentType: event.target.value })}
          />
        </Field>

        <Field
          label={t('document.fields.sizeBytes')}
          htmlFor="document-size"
          error={errors.sizeBytes}
          className="col-md-6"
        >
          <input
            id="document-size"
            type="number"
            min={0}
            className={controlClass('form-control', errors.sizeBytes)}
            value={form.sizeBytes}
            onChange={(event) => patch({ sizeBytes: event.target.value })}
          />
        </Field>

        <Field
          label={t('document.fields.sha256')}
          htmlFor="document-sha256"
          error={errors.sha256}
          hint={isHashing ? t('document.form.hashing') : t('document.form.sha256Hint')}
        >
          <input
            id="document-sha256"
            type="text"
            className={controlClass('form-control font-monospace', errors.sha256)}
            value={form.sha256}
            onChange={(event) => patch({ sha256: event.target.value })}
          />
        </Field>

        <Field
          label={t('document.fields.company')}
          htmlFor="document-company"
          className="col-md-6"
        >
          <select
            id="document-company"
            className="form-select"
            value={form.companyId}
            onChange={(event) => patch({ companyId: event.target.value })}
          >
            <option value="">{t('document.form.noCompany')}</option>
            {companies.data?.items.map((company) => (
              <option key={company.id} value={company.id}>
                {company.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('document.fields.category')}
          htmlFor="document-category"
          hint={t('document.form.categoryHint')}
          className="col-md-6"
        >
          <input
            id="document-category"
            type="number"
            min={1}
            className="form-control"
            value={form.documentCategoryId}
            onChange={(event) => patch({ documentCategoryId: event.target.value })}
          />
        </Field>

        <Field label={t('document.fields.ownerType')} htmlFor="document-owner-type" className="col-md-6">
          <select
            id="document-owner-type"
            className="form-select"
            value={form.ownerType}
            onChange={(event) => patch({ ownerType: Number(event.target.value) })}
          >
            {OWNER_TYPES.map((value) => (
              <option key={value} value={value}>
                {t(`enums.documentOwnerType.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('document.fields.ownerRecordId')}
          htmlFor="document-owner-record"
          hint={t('document.form.ownerRecordHint')}
          className="col-md-6"
        >
          <input
            id="document-owner-record"
            type="number"
            min={1}
            className="form-control"
            value={form.ownerRecordId}
            onChange={(event) => patch({ ownerRecordId: event.target.value })}
          />
        </Field>

        <div className="col-12">
          <div className="form-check">
            <input
              id="document-active"
              type="checkbox"
              className="form-check-input"
              checked={form.isActive}
              onChange={(event) => patch({ isActive: event.target.checked })}
            />
            <label htmlFor="document-active" className="form-check-label">
              {t('common.active')}
            </label>
          </div>
        </div>
      </div>
    </Modal>
  )
}
