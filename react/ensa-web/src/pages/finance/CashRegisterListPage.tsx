import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { PageTitle, Pagination, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useEntity } from '@/api/endpoints'
import { useCreate, useDelete, useUpdate } from '@/api/mutations'
import {
  FINANCE_ENDPOINTS,
  useCashRegisterList,
  useOfficeLookup,
  type CashRegisterDto,
  type CashRegisterListDto,
  type SaveCashRegisterDto,
} from './api'
import { FilterSelect, RowActions } from './components'
import CashRegisterForm from './CashRegisterForm'

const PAGE_SIZE = 20

/**
 * Cash register list.
 *
 * The movement ledger is not shown here: `GetCashTransactionListInput.CashRegisterId` is
 * required, so a transactions request without a register answers 400. A register is picked from
 * this table first, and its movements open on the detail screen.
 */
export default function CashRegisterListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [officeId, setOfficeId] = useState('')
  const [activeFilter, setActiveFilter] = useState('')

  const [isCreating, setCreating] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState<CashRegisterListDto | null>(null)

  const offices = useOfficeLookup()

  const { data, isLoading, error } = useCashRegisterList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'CashRegisterName ASC',
    filter: search || undefined,
    officeId: officeId ? Number(officeId) : undefined,
    isActive: activeFilter === '' ? undefined : activeFilter === 'true',
  })

  const create = useCreate<SaveCashRegisterDto, CashRegisterDto>(FINANCE_ENDPOINTS.cashRegister, {
    onSuccess: () => setCreating(false),
  })
  const remove = useDelete(FINANCE_ENDPOINTS.cashRegister, { onSuccess: () => setDeleting(null) })

  const officeName = (id: number) =>
    offices.data?.items.find((item) => item.id === id)?.displayName ?? t('common.none')

  const columns: Column<CashRegisterListDto>[] = [
    {
      key: 'name',
      header: t('finance.cashRegister.fields.name'),
      render: (row) => (
        <Link to={`/cash-register/${row.id}`} className="fw-semibold text-decoration-none">
          {row.cashRegisterName}
        </Link>
      ),
    },
    {
      key: 'office',
      header: t('finance.cashRegister.fields.office'),
      render: (row) => officeName(row.officeId),
    },
    {
      key: 'headquarter',
      header: t('finance.cashRegister.fields.headquarter'),
      align: 'center',
      render: (row) => (
        <span className={row.headquarterCashRegister ? 'badge-light-primary' : 'badge-light-info'}>
          {row.headquarterCashRegister
            ? t('finance.cashRegister.headquarter.yes')
            : t('finance.cashRegister.headquarter.no')}
        </span>
      ),
    },
    {
      key: 'status',
      header: t('finance.cashRegister.fields.status'),
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
      width: '110px',
      render: (row) => (
        <RowActions
          editLabel={t('finance.cashRegister.actions.edit', { name: row.cashRegisterName })}
          deleteLabel={t('finance.cashRegister.actions.delete', { name: row.cashRegisterName })}
          onEdit={() => setEditingId(row.id)}
          onDelete={() => setDeleting(row)}
        />
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('finance.cashRegister.list.title')}
        description={t('finance.cashRegister.list.description')}
        action={
          <button className="btn btn-primary" type="button" onClick={() => setCreating(true)}>
            {t('finance.cashRegister.list.create')}
          </button>
        }
      />

      <div className="card">
        <div className="card-header border-0 pt-4 pb-0">
          <SearchBar
            value={search}
            onChange={(next) => {
              setSearch(next)
              setPage(1)
            }}
            placeholder={t('finance.cashRegister.list.searchPlaceholder')}
          >
            <FilterSelect
              id="register-filter-office"
              label={t('finance.cashRegister.fields.office')}
              value={officeId}
              onChange={(next) => {
                setOfficeId(next)
                setPage(1)
              }}
              width={220}
            >
              <option value="">{t('finance.cashRegister.filters.allOffices')}</option>
              {offices.data?.items.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.displayName}
                </option>
              ))}
            </FilterSelect>

            <FilterSelect
              id="register-filter-status"
              label={t('finance.cashRegister.fields.status')}
              value={activeFilter}
              onChange={(next) => {
                setActiveFilter(next)
                setPage(1)
              }}
            >
              <option value="">{t('common.all')}</option>
              <option value="true">{t('common.active')}</option>
              <option value="false">{t('common.passive')}</option>
            </FilterSelect>
          </SearchBar>
        </div>

        <div className="card-body p-0">
          <DataTable
            label={t('finance.cashRegister.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(row) => row.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('finance.cashRegister.list.empty')}
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

      {isCreating && (
        <CashRegisterForm
          isOpen
          onClose={() => setCreating(false)}
          onSubmit={(input) => create.mutate(input)}
          isBusy={create.isPending}
          error={create.error ? errorMessage(create.error) : null}
        />
      )}

      {editingId !== null && (
        <CashRegisterEditor registerId={editingId} onClose={() => setEditingId(null)} />
      )}

      <ConfirmDialog
        isOpen={!!deleting}
        title={t('finance.cashRegister.delete.title')}
        message={t('finance.cashRegister.delete.message', { name: deleting?.cashRegisterName ?? '' })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

/** Loads the register before opening the edit dialog, so every field round-trips. */
export function CashRegisterEditor({
  registerId,
  onClose,
}: {
  registerId: number
  onClose: () => void
}) {
  const { t } = useTranslation()
  const { data, isLoading, error } = useEntity<CashRegisterDto>(
    FINANCE_ENDPOINTS.cashRegister,
    registerId,
  )
  const update = useUpdate<SaveCashRegisterDto, CashRegisterDto>(FINANCE_ENDPOINTS.cashRegister, {
    onSuccess: onClose,
  })

  if (isLoading || error || !data) {
    return (
      <Modal
        title={t('finance.cashRegister.form.editTitle')}
        isOpen
        onClose={onClose}
        error={error ? errorMessage(error) : null}
      >
        {isLoading ? <Spinner /> : null}
      </Modal>
    )
  }

  return (
    <CashRegisterForm
      isOpen
      register={data}
      onClose={onClose}
      onSubmit={(input) => update.mutate({ id: registerId, input })}
      isBusy={update.isPending}
      error={update.error ? errorMessage(update.error) : null}
    />
  )
}
