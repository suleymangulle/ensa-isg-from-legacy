import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, SearchBar } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import {
  HEALTH_ENDPOINTS,
  useEPrescriptionList,
  type EPrescriptionListDto,
} from './api'
import { useDebouncedValue } from './components/ReferencePickers'
import EPrescriptionFormModal from './EPrescriptionFormModal'

/**
 * E-prescriptions.
 *
 * PRIVACY. The list row carries the prescription envelope only — no medication and no
 * diagnosis. Patient lookup is an exact match on the national id: the column is encrypted and
 * a partial match would turn the screen into an identity-enumeration channel, so the field
 * only queries once a complete 11-digit number has been entered.
 */

const PAGE_SIZE = 20
const NATIONAL_ID_LENGTH = 11

type CancelledFilter = '' | 'true' | 'false'

export default function EPrescriptionListPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)

  const [nationalId, setNationalId] = useState('')
  const debouncedNationalId = useDebouncedValue(nationalId)
  const [cancelled, setCancelled] = useState<CancelledFilter>('')
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')

  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<EPrescriptionListDto | null>(null)

  const { data, isLoading, error } = useEPrescriptionList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'CreationTime DESC',
    filter: debouncedSearch,
    // Only a complete national id is sent — a partial one would never match anyway.
    patientNationalId:
      debouncedNationalId.length === NATIONAL_ID_LENGTH ? debouncedNationalId : null,
    cancelled: cancelled === '' ? null : cancelled === 'true',
    dateFrom: dateFrom || null,
    dateTo: dateTo || null,
  })

  const remove = useDelete(HEALTH_ENDPOINTS.ePrescription, {
    onSuccess: () => setPendingDelete(null),
  })

  const columns: Column<EPrescriptionListDto>[] = [
    {
      key: 'code',
      header: t('ePrescription.fields.code'),
      render: (row) => (
        <Link to={`/eprescriptions/${row.id}`} className="fw-semibold text-decoration-none">
          {row.ePrescriptionCode ?? t('ePrescription.list.noCode')}
        </Link>
      ),
    },
    {
      key: 'patient',
      header: t('ePrescription.fields.patient'),
      render: (row) => row.patientFullName ?? t('common.none'),
    },
    {
      key: 'protocolNo',
      header: t('ePrescription.fields.protocolNo'),
      render: (row) => row.protocolNo ?? t('common.none'),
    },
    {
      key: 'creationTime',
      header: t('ePrescription.fields.creationTime'),
      render: (row) => formatDate(row.creationTime) ?? t('common.none'),
    },
    {
      key: 'submissionDate',
      header: t('ePrescription.fields.submissionDate'),
      render: (row) => formatDate(row.submissionDate) ?? t('common.none'),
    },
    {
      key: 'resultCode',
      header: t('ePrescription.fields.resultCode'),
      render: (row) => row.resultCode ?? t('common.none'),
    },
    {
      key: 'status',
      header: t('ePrescription.fields.status'),
      align: 'center',
      render: (row) => (
        <span className={row.cancelled ? 'badge-light-danger' : 'badge-light-success'}>
          {row.cancelled ? t('ePrescription.status.cancelled') : t('ePrescription.status.active')}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '110px',
      render: (row) => (
        <div className="d-flex justify-content-end gap-1">
          <Link
            to={`/eprescriptions/${row.id}`}
            className="btn btn-sm btn-icon btn-light"
            aria-label={t('ePrescription.list.openDetail', {
              code: row.ePrescriptionCode ?? String(row.id),
            })}
          >
            <span aria-hidden="true">→</span>
          </Link>
          <button
            type="button"
            className="btn btn-sm btn-icon btn-light-danger"
            aria-label={t('ePrescription.list.deleteAction', {
              code: row.ePrescriptionCode ?? String(row.id),
            })}
            onClick={() => setPendingDelete(row)}
          >
            <span aria-hidden="true">✕</span>
          </button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('ePrescription.list.title')}
        description={t('ePrescription.list.description')}
        action={
          <button className="btn btn-primary" type="button" onClick={() => setIsCreateOpen(true)}>
            {t('ePrescription.list.create')}
          </button>
        }
      />

      <div className="card">
        <div className="card-header d-block py-4">
          <SearchBar
            value={search}
            onChange={(next) => {
              setSearch(next)
              setPage(1)
            }}
            placeholder={t('ePrescription.list.searchPlaceholder')}
          >
            <div className="d-flex flex-wrap gap-2">
              <div>
                <label htmlFor="prescription-filter-national-id" className="visually-hidden">
                  {t('ePrescription.filters.nationalId')}
                </label>
                <input
                  id="prescription-filter-national-id"
                  className="form-control form-control-sm"
                  style={{ minWidth: 180 }}
                  inputMode="numeric"
                  maxLength={NATIONAL_ID_LENGTH}
                  placeholder={t('ePrescription.filters.nationalId')}
                  value={nationalId}
                  onChange={(event) => {
                    setNationalId(
                      event.target.value.replace(/\D/g, '').slice(0, NATIONAL_ID_LENGTH),
                    )
                    setPage(1)
                  }}
                />
              </div>

              <div>
                <label htmlFor="prescription-filter-cancelled" className="visually-hidden">
                  {t('ePrescription.filters.status')}
                </label>
                <select
                  id="prescription-filter-cancelled"
                  className="form-select form-select-sm"
                  style={{ minWidth: 160 }}
                  value={cancelled}
                  onChange={(event) => {
                    setCancelled(event.target.value as CancelledFilter)
                    setPage(1)
                  }}
                >
                  <option value="">{t('ePrescription.filters.allStatuses')}</option>
                  <option value="false">{t('ePrescription.status.active')}</option>
                  <option value="true">{t('ePrescription.status.cancelled')}</option>
                </select>
              </div>

              <div>
                <label htmlFor="prescription-filter-from" className="visually-hidden">
                  {t('ePrescription.filters.dateFrom')}
                </label>
                <input
                  id="prescription-filter-from"
                  type="date"
                  className="form-control form-control-sm"
                  style={{ minWidth: 150 }}
                  title={t('ePrescription.filters.dateFrom')}
                  value={dateFrom}
                  onChange={(event) => {
                    setDateFrom(event.target.value)
                    setPage(1)
                  }}
                />
              </div>

              <div>
                <label htmlFor="prescription-filter-to" className="visually-hidden">
                  {t('ePrescription.filters.dateTo')}
                </label>
                <input
                  id="prescription-filter-to"
                  type="date"
                  className="form-control form-control-sm"
                  style={{ minWidth: 150 }}
                  title={t('ePrescription.filters.dateTo')}
                  value={dateTo}
                  onChange={(event) => {
                    setDateTo(event.target.value)
                    setPage(1)
                  }}
                />
              </div>
            </div>
          </SearchBar>

          <p className="mb-0 mt-2" style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
            {t('ePrescription.filters.nationalIdHint', { length: NATIONAL_ID_LENGTH })}
          </p>
        </div>

        <div className="card-body p-0">
          <DataTable
            label={t('ePrescription.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(row) => row.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('ePrescription.list.empty')}
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

      {isCreateOpen && (
        <EPrescriptionFormModal isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)} />
      )}

      <ConfirmDialog
        isOpen={pendingDelete != null}
        title={t('ePrescription.list.deleteTitle')}
        message={t('ePrescription.list.deleteMessage', {
          code: pendingDelete?.ePrescriptionCode ?? String(pendingDelete?.id ?? ''),
        })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => {
          remove.reset()
          setPendingDelete(null)
        }}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
      />
    </>
  )
}
