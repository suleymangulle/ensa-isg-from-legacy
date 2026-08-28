import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { CheckBox, Input, NumberInput, Select } from 'rich-react-component'
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
  documentCategoryId: number | null
  companyId: number | null
  extension: string
  contentType: string
  sizeBytes: number | null
  sha256: string
  ownerType: DocumentOwnerType
  ownerRecordId: number | null
  isActive: boolean
}

const EMPTY: FormState = {
  documentName: '',
  documentCategoryId: null,
  companyId: null,
  extension: '',
  contentType: '',
  sizeBytes: 0,
  sha256: '',
  ownerType: DocumentOwnerType.Unspecified,
  ownerRecordId: null,
  isActive: true,
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
            documentCategoryId: existing.documentCategoryId ?? null,
            companyId: existing.companyId ?? null,
            extension: existing.extension ?? '',
            contentType: existing.contentType ?? '',
            sizeBytes: existing.sizeBytes,
            sha256: existing.sha256 ?? '',
            ownerType: existing.ownerType,
            ownerRecordId: existing.ownerRecordId ?? null,
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
      sizeBytes: file.size,
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

    if (form.sizeBytes !== null && form.sizeBytes < 0) {
      nextErrors.sizeBytes = t('document.form.invalidSize')
    }

    const digest = form.sha256.trim().toLowerCase()
    if (digest && !/^[0-9a-f]{64}$/.test(digest)) {
      nextErrors.sha256 = t('document.form.invalidSha256')
    }

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return

    const input: SaveDocumentDto = {
      documentName: form.documentName.trim(),
      documentCategoryId: form.documentCategoryId,
      companyId: form.companyId,
      extension: form.extension.trim() || null,
      contentType: form.contentType.trim() || null,
      sizeBytes: Math.trunc(form.sizeBytes ?? 0),
      sha256: digest || null,
      ownerType: form.ownerType,
      ownerRecordId: form.ownerRecordId,
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

        <Input
          id="document-name"
          label={t('document.fields.documentName')}
          required
          error={errors.documentName}
          className="col-md-8"
          value={form.documentName}
          onChange={(value) => patch({ documentName: value })}
        />

        <Input
          id="document-extension"
          label={t('document.fields.extension')}
          className="col-md-4"
          value={form.extension}
          onChange={(value) => patch({ extension: value })}
        />

        <Input
          id="document-content-type"
          label={t('document.fields.contentType')}
          className="col-md-6"
          value={form.contentType}
          onChange={(value) => patch({ contentType: value })}
        />

        <NumberInput
          id="document-size"
          label={t('document.fields.sizeBytes')}
          error={errors.sizeBytes}
          className="col-md-6"
          min={0}
          value={form.sizeBytes}
          onChange={(value) => patch({ sizeBytes: value })}
        />

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

        <Select
          id="document-company"
          label={t('document.fields.company')}
          className="col-md-6"
          placeholder={t('document.form.noCompany')}
          options={(companies.data?.items ?? []).map((company) => ({
            value: company.id,
            label: company.displayName,
          }))}
          value={form.companyId}
          onChange={(value) => patch({ companyId: value })}
        />

        <NumberInput
          id="document-category"
          label={t('document.fields.category')}
          helpText={t('document.form.categoryHint')}
          className="col-md-6"
          min={1}
          value={form.documentCategoryId}
          onChange={(value) => patch({ documentCategoryId: value })}
        />

        <Select
          id="document-owner-type"
          label={t('document.fields.ownerType')}
          className="col-md-6"
          options={OWNER_TYPES.map((value) => ({
            value,
            label: t(`enums.documentOwnerType.${value}`),
          }))}
          value={form.ownerType}
          onChange={(value) => value !== null && patch({ ownerType: value })}
        />

        <NumberInput
          id="document-owner-record"
          label={t('document.fields.ownerRecordId')}
          helpText={t('document.form.ownerRecordHint')}
          className="col-md-6"
          min={1}
          value={form.ownerRecordId}
          onChange={(value) => patch({ ownerRecordId: value })}
        />

        <div className="col-12">
          <CheckBox
            id="document-active"
            label={t('common.active')}
            checked={form.isActive}
            onChange={(checked) => patch({ isActive: checked })}
          />
        </div>
      </div>
    </Modal>
  )
}
