import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, SearchBar } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useCreate, useDelete, useUpdate } from '@/api/mutations'
import { useLookup } from '@/api/endpoints'
import {
  RESOURCES,
  useDepartmentList,
  type DepartmentListDto,
  type SaveDepartmentDto,
} from './api'

const PAGE_SIZE = 20

const emptyForm: SaveDepartmentDto = { companyId: 0, departmentName: '' }

export default function DepartmentListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [companyId, setCompanyId] = useState<number | ''>('')

  const [form, setForm] = useState<SaveDepartmentDto | null>(null)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [pendingDelete, setPendingDelete] = useState<DepartmentListDto | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)

  const { data, isLoading, error } = useDepartmentList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'DepartmentName ASC',
    filter: search || undefined,
    companyId: companyId === '' ? undefined : companyId,
  })

  const companies = useLookup('company')

  const closeForm = () => {
    setForm(null)
    setEditingId(null)
    setSaveError(null)
  }

  const create = useCreate<SaveDepartmentDto>(RESOURCES.department, { onSuccess: closeForm })
  const update = useUpdate<SaveDepartmentDto>(RESOURCES.department, { onSuccess: closeForm })
  const remove = useDelete(RESOURCES.department, { onSuccess: () => setPendingDelete(null) })

  function submit() {
    if (!form) return
    setSaveError(null)

    const onError = (cause: unknown) => setSaveError(errorMessage(cause))

    if (editingId === null) {
      create.mutate(form, { onError })
    } else {
      update.mutate({ id: editingId, input: form }, { onError })
    }
  }

  const columns: Column<DepartmentListDto>[] = [
    {
      key: 'departmentName',
      header: t('department.fields.departmentName'),
      render: (row) => <span className="fw-semibold">{row.departmentName}</span>,
    },
    {
      key: 'companyName',
      header: t('department.fields.companyName'),
      render: (row) => row.companyName ?? t('common.none'),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '140px',
      render: (row) => (
        <div className="d-flex justify-content-end gap-2">
          <button
            type="button"
            className="btn btn-sm btn-light"
            onClick={() => {
              setEditingId(row.id)
              setForm({ companyId: row.companyId, departmentName: row.departmentName })
            }}
            aria-label={t('common.edit')}
          >
            {t('common.edit')}
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light"
            style={{ color: 'var(--kt-danger)' }}
            disabled={!row.deletable}
            // A department already referenced by an employee or a hazard cannot be removed;
            // the button says why rather than letting the call fail.
            title={row.deletable ? undefined : t('department.notDeletable')}
            onClick={() => setPendingDelete(row)}
            aria-label={t('common.delete')}
          >
            {t('common.delete')}
          </button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('department.title')}
        description={t('department.description')}
        action={
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => {
              setEditingId(null)
              setForm({ ...emptyForm, companyId: companyId === '' ? 0 : companyId })
            }}
          >
            {t('department.create')}
          </button>
        }
      />

      <div className="card border-0 shadow-sm">
        <div className="card-body">
          <SearchBar
            value={search}
            onChange={(next) => {
              setSearch(next)
              setPage(1)
            }}
            placeholder={t('department.searchPlaceholder')}
          >
            <div style={{ minWidth: 220 }}>
              <label htmlFor="companyFilter" className="visually-hidden">
                {t('department.fields.companyName')}
              </label>
              <select
                id="companyFilter"
                className="form-select"
                value={companyId}
                onChange={(event) => {
                  setCompanyId(event.target.value === '' ? '' : Number(event.target.value))
                  setPage(1)
                }}
              >
                <option value="">{t('common.all')}</option>
                {companies.data?.items.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.displayName}
                  </option>
                ))}
              </select>
            </div>
          </SearchBar>

          <DataTable
            columns={columns}
            rows={data?.items}
            rowKey={(row) => row.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            label={t('department.title')}
          />

          <Pagination
            total={data?.totalCount ?? 0}
            page={page}
            pageSize={PAGE_SIZE}
            onPageChange={setPage}
          />
        </div>
      </div>

      <Modal
        title={editingId === null ? t('department.create') : t('department.edit')}
        isOpen={form !== null}
        onClose={closeForm}
        onSubmit={submit}
        isBusy={create.isPending || update.isPending}
        error={saveError}
      >
        {form && (
          <div className="row g-3">
            <Field
              label={t('department.fields.companyName')}
              htmlFor="departmentCompanyId"
              required
            >
              <select
                id="departmentCompanyId"
                className="form-select"
                value={form.companyId || ''}
                onChange={(event) =>
                  setForm({ ...form, companyId: Number(event.target.value) || 0 })
                }
                required
              >
                <option value="">{t('common.none')}</option>
                {companies.data?.items.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.displayName}
                  </option>
                ))}
              </select>
            </Field>

            <Field
              label={t('department.fields.departmentName')}
              htmlFor="departmentName"
              required
            >
              <input
                id="departmentName"
                className="form-control"
                value={form.departmentName}
                onChange={(event) => setForm({ ...form, departmentName: event.target.value })}
                maxLength={200}
                required
              />
            </Field>
          </div>
        )}
      </Modal>

      <ConfirmDialog
        isOpen={pendingDelete !== null}
        title={t('department.delete')}
        message={t('department.confirmDelete', { name: pendingDelete?.departmentName ?? '' })}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}
