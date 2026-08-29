import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button, Card, Input, Select } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
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
          <Button variant="light" size="sm"
            onClick={() => {
              setEditingId(row.id)
              setForm({ companyId: row.companyId, departmentName: row.departmentName })
            }}
            aria-label={t('common.edit')}
          >
            {t('common.edit')}
          </Button>
          <Button variant="light" size="sm"
            style={{ color: 'var(--kt-danger)' }}
            disabled={!row.isDeletable}
            // A department already referenced by an employee or a hazard cannot be removed;
            // the button says why rather than letting the call fail.
            title={row.isDeletable ? undefined : t('department.notDeletable')}
            onClick={() => setPendingDelete(row)}
            aria-label={t('common.delete')}
          >
            {t('common.delete')}
          </Button>
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
          <Button variant="primary"
            onClick={() => {
              setEditingId(null)
              setForm({ ...emptyForm, companyId: companyId === '' ? 0 : companyId })
            }}
          >
            {t('department.create')}
          </Button>
        }
      />

      <Card
        className="border-0 shadow-sm"
      >
          <SearchBar
            value={search}
            onChange={(next) => {
              setSearch(next)
              setPage(1)
            }}
            placeholder={t('department.searchPlaceholder')}
          >
            <div style={{ minWidth: 220 }}>
              <Select<number>
                id="companyFilter"
                placeholder={t('common.all')}
                options={(companies.data?.items ?? []).map((item) => ({
                  value: item.id,
                  label: item.displayName,
                }))}
                value={companyId === '' ? null : companyId}
                onChange={(value) => {
                  setCompanyId(value ?? '')
                  setPage(1)
                }}
              />
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
        
      </Card>

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
            <Select<number>
              id="departmentCompanyId"
              label={t('department.fields.companyName')}
              required
              placeholder={t('common.none')}
              options={(companies.data?.items ?? []).map((item) => ({
                value: item.id,
                label: item.displayName,
              }))}
              value={form.companyId || null}
              onChange={(value) => setForm({ ...form, companyId: value ?? 0 })}
            />

            <Input
              id="departmentName"
              label={t('department.fields.departmentName')}
              required
              value={form.departmentName}
              onChange={(value) => setForm({ ...form, departmentName: value })}
              maxLength={200}
            />
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
