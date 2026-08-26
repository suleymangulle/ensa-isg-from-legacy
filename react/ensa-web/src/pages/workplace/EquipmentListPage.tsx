import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, SearchBar, controlClass } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useCreate, useDelete, useUpdate } from '@/api/mutations'
import { EquipmentType } from '@/api/enums'
import { useLookup } from '@/api/endpoints'
import { formatDate } from '@/utils/format'
import {
  RESOURCES,
  useEquipmentList,
  useOverdueInspections,
  type EquipmentListDto,
  type SaveEquipmentDto,
} from './api'

const PAGE_SIZE = 20

/** Equipment types offered in the form, in the order the enum declares them. */
const EQUIPMENT_TYPES = Object.values(EquipmentType).filter(
  (value): value is EquipmentType => typeof value === 'number',
)

const emptyForm: SaveEquipmentDto = {
  companyId: 0,
  equipmentName: '',
  equipmentType: EquipmentType.Unspecified,
  examinationPerformedBy: '',
  examinationDate: '',
}

export default function EquipmentListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [onlyOverdue, setOnlyOverdue] = useState(false)

  const [form, setForm] = useState<SaveEquipmentDto | null>(null)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [pendingDelete, setPendingDelete] = useState<EquipmentListDto | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)

  const { data, isLoading, error } = useEquipmentList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'NextExaminationDate ASC',
    filter: search || undefined,
    onlyOverdueInspection: onlyOverdue || undefined,
  })

  const overdue = useOverdueInspections()
  const companies = useLookup('company')

  const closeForm = () => {
    setForm(null)
    setEditingId(null)
    setSaveError(null)
  }

  const create = useCreate<SaveEquipmentDto>(RESOURCES.equipment, { onSuccess: closeForm })
  const update = useUpdate<SaveEquipmentDto>(RESOURCES.equipment, { onSuccess: closeForm })
  const remove = useDelete(RESOURCES.equipment, { onSuccess: () => setPendingDelete(null) })

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

  const columns: Column<EquipmentListDto>[] = [
    {
      key: 'equipmentName',
      header: t('equipment.fields.equipmentName'),
      render: (row) => <span className="fw-semibold">{row.equipmentName}</span>,
    },
    {
      key: 'equipmentType',
      header: t('equipment.fields.equipmentType'),
      render: (row) => t(`enums.equipmentType.${row.equipmentType}`),
    },
    {
      key: 'companyName',
      header: t('equipment.fields.companyName'),
      render: (row) => row.companyName ?? t('common.none'),
    },
    {
      key: 'examinationDate',
      header: t('equipment.fields.examinationDate'),
      render: (row) => formatDate(row.examinationDate) ?? t('common.none'),
    },
    {
      key: 'nextExaminationDate',
      header: t('equipment.fields.nextExaminationDate'),
      render: (row) => {
        const date = formatDate(row.nextExaminationDate) ?? t('common.none')
        if (!row.isInspectionOverdue) return date

        // An overdue periodic inspection is a statutory finding, so it is called out rather
        // than left for the reader to work out from the date.
        return (
          <span className="d-inline-flex align-items-center gap-2">
            <span>{date}</span>
            <span className="badge-light-danger">{t('equipment.overdue')}</span>
          </span>
        )
      },
    },
    {
      key: 'remainingDays',
      header: t('equipment.fields.remainingDays'),
      align: 'end',
      render: (row) =>
        row.remainingDays === null || row.remainingDays === undefined
          ? t('common.none')
          : t('equipment.days', { count: row.remainingDays }),
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
              setForm({
                companyId: row.companyId,
                equipmentName: row.equipmentName,
                equipmentType: row.equipmentType,
                examinationPerformedBy: row.examinationPerformedBy ?? '',
                examinationDate: row.examinationDate?.slice(0, 10) ?? '',
                periodId: row.periodId ?? null,
              })
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
            title={row.deletable ? undefined : t('equipment.notDeletable')}
            onClick={() => setPendingDelete(row)}
            aria-label={t('common.delete')}
          >
            {t('common.delete')}
          </button>
        </div>
      ),
    },
  ]

  const overdueCount = overdue.data?.items.length ?? 0

  return (
    <>
      <PageTitle
        title={t('equipment.title')}
        description={t('equipment.description')}
        action={
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => {
              setEditingId(null)
              setForm({ ...emptyForm })
            }}
          >
            {t('equipment.create')}
          </button>
        }
      />

      {overdueCount > 0 && !onlyOverdue && (
        <div
          className="alert border-0 d-flex flex-wrap align-items-center justify-content-between gap-2"
          style={{ backgroundColor: 'var(--kt-danger-light)', color: 'var(--kt-danger)' }}
          role="status"
        >
          <span>{t('equipment.overdueSummary', { count: overdueCount })}</span>
          <button
            type="button"
            className="btn btn-sm btn-light"
            onClick={() => {
              setOnlyOverdue(true)
              setPage(1)
            }}
          >
            {t('equipment.showOverdue')}
          </button>
        </div>
      )}

      <div className="card border-0 shadow-sm">
        <div className="card-body">
          <SearchBar
            value={search}
            onChange={(next) => {
              setSearch(next)
              setPage(1)
            }}
            placeholder={t('equipment.searchPlaceholder')}
          >
            <div className="form-check ms-2">
              <input
                id="onlyOverdue"
                className="form-check-input"
                type="checkbox"
                checked={onlyOverdue}
                onChange={(event) => {
                  setOnlyOverdue(event.target.checked)
                  setPage(1)
                }}
              />
              <label className="form-check-label" htmlFor="onlyOverdue">
                {t('equipment.onlyOverdue')}
              </label>
            </div>
          </SearchBar>

          <DataTable
            columns={columns}
            rows={data?.items}
            rowKey={(row) => row.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            label={t('equipment.title')}
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
        title={editingId === null ? t('equipment.create') : t('equipment.edit')}
        isOpen={form !== null}
        onClose={closeForm}
        onSubmit={submit}
        isBusy={create.isPending || update.isPending}
        error={saveError}
        size="lg"
      >
        {form && (
          <div className="row g-3">
            <Field
              label={t('equipment.fields.companyName')}
              htmlFor="companyId"
              required
              className="col-md-6"
            >
              <select
                id="companyId"
                className={controlClass('form-select')}
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
              label={t('equipment.fields.equipmentType')}
              htmlFor="equipmentType"
              required
              className="col-md-6"
            >
              <select
                id="equipmentType"
                className="form-select"
                value={form.equipmentType}
                onChange={(event) =>
                  setForm({ ...form, equipmentType: Number(event.target.value) as EquipmentType })
                }
              >
                {EQUIPMENT_TYPES.map((value) => (
                  <option key={value} value={value}>
                    {t(`enums.equipmentType.${value}`)}
                  </option>
                ))}
              </select>
            </Field>

            <Field
              label={t('equipment.fields.equipmentName')}
              htmlFor="equipmentName"
              required
              className="col-12"
            >
              <input
                id="equipmentName"
                className="form-control"
                value={form.equipmentName}
                onChange={(event) => setForm({ ...form, equipmentName: event.target.value })}
                maxLength={200}
                required
              />
            </Field>

            <Field
              label={t('equipment.fields.examinationDate')}
              htmlFor="examinationDate"
              className="col-md-6"
              hint={t('equipment.nextDateHint')}
            >
              <input
                id="examinationDate"
                type="date"
                className="form-control"
                value={form.examinationDate ?? ''}
                onChange={(event) => setForm({ ...form, examinationDate: event.target.value })}
              />
            </Field>

            <Field
              label={t('equipment.fields.examinationPerformedBy')}
              htmlFor="examinationPerformedBy"
              className="col-md-6"
            >
              <input
                id="examinationPerformedBy"
                className="form-control"
                value={form.examinationPerformedBy ?? ''}
                onChange={(event) =>
                  setForm({ ...form, examinationPerformedBy: event.target.value })
                }
                maxLength={200}
              />
            </Field>

            <Field
              label={t('equipment.fields.examinationReport')}
              htmlFor="examinationReport"
              className="col-12"
            >
              <textarea
                id="examinationReport"
                className="form-control"
                rows={3}
                value={form.examinationReport ?? ''}
                onChange={(event) => setForm({ ...form, examinationReport: event.target.value })}
              />
            </Field>
          </div>
        )}
      </Modal>

      <ConfirmDialog
        isOpen={pendingDelete !== null}
        title={t('equipment.delete')}
        message={t('equipment.confirmDelete', { name: pendingDelete?.equipmentName ?? '' })}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}
