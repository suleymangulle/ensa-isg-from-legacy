import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column, ErrorPanel } from '@/components/DataTable'
import { ConfirmDialog, SearchBar } from '@/components/Form'
import { useDelete } from '@/api/mutations'
import { errorMessage } from '@/api/http'
import { downloadFile } from '@/api/download'
import { useEntity } from '@/api/endpoints'
import { DocumentOwnerType } from '@/api/enums'
import { formatDate, formatFileSize } from '@/utils/format'
import DocumentFormModal from './DocumentFormModal'
import {
  documentContentPath,
  DOCUMENT, useCompanyLookup, useDocumentList, type DocumentDto, type DocumentListDto,
} from './api'
import { OWNER_TYPES } from './helpers'

const PAGE_SIZE = 20

export default function DocumentListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [ownerType, setOwnerType] = useState('')
  const [companyId, setCompanyId] = useState('')
  const [editingId, setEditingId] = useState<number | undefined>()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [deleting, setDeleting] = useState<DocumentListDto | null>(null)

  const companies = useCompanyLookup()

  const { data, isLoading, error } = useDocumentList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    filter: search || undefined,
    ownerType: ownerType === '' ? undefined : (Number(ownerType) as DocumentOwnerType),
    companyId: companyId === '' ? undefined : Number(companyId),
  })

  // The edit dialog needs the full record: the list row carries neither the digest nor the
  // category, and posting an update built from a partial row would silently blank them.
  const editing = useEntity<DocumentDto>(DOCUMENT, editingId)

  const remove = useDelete(DOCUMENT, { onSuccess: () => setDeleting(null) })

  function resetPage<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value)
      setPage(1)
    }
  }

  const [downloadingId, setDownloadingId] = useState<number | null>(null)
  const [downloadError, setDownloadError] = useState<string | null>(null)

  async function download(id: number, fileName: string) {
    setDownloadingId(id)
    setDownloadError(null)
    try {
      await downloadFile(documentContentPath(id), fileName)
    } catch (cause) {
      setDownloadError(errorMessage(cause))
    } finally {
      setDownloadingId(null)
    }
  }

  const columns: Column<DocumentListDto>[] = [
    {
      key: 'documentName',
      header: t('document.fields.documentName'),
      render: (row) => (
        <Link to={`/documents/${row.id}/detail`} className="fw-semibold text-decoration-none">
          {row.documentName}
        </Link>
      ),
    },
    {
      key: 'extension',
      header: t('document.fields.extension'),
      render: (row) =>
        row.extension ? (
          <span className="badge-light-primary text-uppercase">{row.extension}</span>
        ) : (
          t('common.none')
        ),
    },
    {
      key: 'sizeBytes',
      header: t('document.fields.sizeBytes'),
      align: 'end',
      render: (row) => formatFileSize(row.sizeBytes) ?? t('common.none'),
    },
    {
      key: 'ownerType',
      header: t('document.fields.ownerType'),
      render: (row) => t(`enums.documentOwnerType.${row.ownerType}`),
    },
    {
      key: 'ownerRecordId',
      header: t('document.fields.ownerRecordId'),
      align: 'end',
      render: (row) => row.ownerRecordId ?? t('common.none'),
    },
    {
      key: 'creationTime',
      header: t('document.fields.creationTime'),
      render: (row) => formatDate(row.creationTime) ?? t('common.none'),
    },
    {
      key: 'isActive',
      header: t('document.fields.status'),
      align: 'center',
      render: (row) => (
        <span className={row.isActive ? 'badge-light-success' : 'badge-light-danger'}>
          {row.isActive ? t('common.active') : t('common.passive')}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '190px',
      render: (row) => (
        <div className="d-flex justify-content-end gap-2">
          {/*
            The bearer token cannot travel on a plain anchor, so the file is fetched through the
            shared axios instance and handed to the browser; see `@/api/download`.
          */}
          <button
            type="button"
            className="btn btn-sm btn-light"
            title={t('document.list.download')}
            aria-label={t('document.list.download')}
            disabled={downloadingId === row.id}
            onClick={() => download(row.id, row.documentName)}
          >
            {downloadingId === row.id ? '…' : '⭳'}
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-primary"
            onClick={() => {
              setEditingId(row.id)
              setIsFormOpen(true)
            }}
            aria-label={t('document.list.editAria', { name: row.documentName })}
          >
            {t('common.edit')}
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            onClick={() => setDeleting(row)}
            aria-label={t('document.list.deleteAria', { name: row.documentName })}
          >
            {t('common.delete')}
          </button>
        </div>
      ),
    },
  ]

  return (
    <>
      {downloadError && <ErrorPanel message={downloadError} />}

      <PageTitle
        title={t('document.list.title')}
        description={t('document.list.description')}
        action={
          <button
            className="btn btn-primary"
            type="button"
            onClick={() => {
              setEditingId(undefined)
              setIsFormOpen(true)
            }}
          >
            {t('document.list.create')}
          </button>
        }
      />

      <div
        className="alert border-0 d-flex align-items-center gap-2"
        style={{ backgroundColor: 'var(--kt-primary-light)', color: 'var(--kt-primary)' }}
      >
        <span aria-hidden="true">ℹ</span>
        <span>{t('document.list.metadataOnlyNotice')}</span>
      </div>

      <div className="card">
        <div className="card-header pt-4 pb-0 border-0">
          <SearchBar
            value={search}
            onChange={resetPage(setSearch)}
            placeholder={t('document.list.searchPlaceholder')}
          >
            <div>
              <label htmlFor="document-owner-filter" className="visually-hidden">
                {t('document.filters.ownerType')}
              </label>
              <select
                id="document-owner-filter"
                className="form-select"
                style={{ minWidth: 200 }}
                value={ownerType}
                onChange={(event) => resetPage(setOwnerType)(event.target.value)}
              >
                <option value="">{t('document.filters.allOwnerTypes')}</option>
                {OWNER_TYPES.map((value) => (
                  <option key={value} value={value}>
                    {t(`enums.documentOwnerType.${value}`)}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="document-company-filter" className="visually-hidden">
                {t('document.filters.company')}
              </label>
              <select
                id="document-company-filter"
                className="form-select"
                style={{ minWidth: 200 }}
                value={companyId}
                onChange={(event) => resetPage(setCompanyId)(event.target.value)}
              >
                <option value="">{t('document.filters.allCompanies')}</option>
                {companies.data?.items.map((company) => (
                  <option key={company.id} value={company.id}>
                    {company.displayName}
                  </option>
                ))}
              </select>
            </div>
          </SearchBar>
        </div>

        <div className="card-body p-0">
          <DataTable
            label={t('document.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(row) => row.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('document.list.empty')}
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

      {isFormOpen && (!editingId || editing.data) && (
        <DocumentFormModal
          isOpen
          document={editingId ? editing.data : undefined}
          onClose={() => {
            setIsFormOpen(false)
            setEditingId(undefined)
          }}
        />
      )}

      <ConfirmDialog
        isOpen={!!deleting}
        title={t('document.list.deleteTitle')}
        message={t('document.list.deleteMessage', { name: deleting?.documentName ?? '' })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}
