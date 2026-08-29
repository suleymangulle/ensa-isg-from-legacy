import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Badge,
  Button,
  Card,
  CheckBox,
  Input,
  Select,
  TextArea,
} from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
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
            <Badge variant="danger">{t('equipment.overdue')}</Badge>
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
          <Button variant="light" size="sm"
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
          </Button>
          <Button variant="light" size="sm"
            style={{ color: 'var(--kt-danger)' }}
            disabled={!row.isDeletable}
            title={row.isDeletable ? undefined : t('equipment.notDeletable')}
            onClick={() => setPendingDelete(row)}
            aria-label={t('common.delete')}
          >
            {t('common.delete')}
          </Button>
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
          <Button variant="primary"
            onClick={() => {
              setEditingId(null)
              setForm({ ...emptyForm })
            }}
          >
            {t('equipment.create')}
          </Button>
        }
      />

      {overdueCount > 0 && !onlyOverdue && (
        <div
          className="alert border-0 d-flex flex-wrap align-items-center justify-content-between gap-2"
          style={{ backgroundColor: 'var(--kt-danger-light)', color: 'var(--kt-danger)' }}
          role="status"
        >
          <span>{t('equipment.overdueSummary', { count: overdueCount })}</span>
          <Button variant="light" size="sm"
            onClick={() => {
              setOnlyOverdue(true)
              setPage(1)
            }}
          >
            {t('equipment.showOverdue')}
          </Button>
        </div>
      )}

      <Card
        className="border-0 shadow-sm"
      >
          <SearchBar
            value={search}
            onChange={(next) => {
              setSearch(next)
              setPage(1)
            }}
            placeholder={t('equipment.searchPlaceholder')}
          >
            <CheckBox
              id="onlyOverdue"
              className="ms-2"
              label={t('equipment.onlyOverdue')}
              checked={onlyOverdue}
              onChange={(checked) => {
                setOnlyOverdue(checked)
                setPage(1)
              }}
            />
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
        
      </Card>

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
            <Select<number>
              id="companyId"
              label={t('equipment.fields.companyName')}
              required
              className="col-md-6"
              placeholder={t('common.none')}
              options={(companies.data?.items ?? []).map((item) => ({
                value: item.id,
                label: item.displayName,
              }))}
              value={form.companyId || null}
              onChange={(value) => setForm({ ...form, companyId: value ?? 0 })}
            />

            <Select<number>
              id="equipmentType"
              label={t('equipment.fields.equipmentType')}
              required
              className="col-md-6"
              options={EQUIPMENT_TYPES.map((value) => ({
                value,
                label: t(`enums.equipmentType.${value}`),
              }))}
              value={form.equipmentType}
              onChange={(value) =>
                setForm({ ...form, equipmentType: (value ?? 0) as EquipmentType })
              }
            />

            <Input
              id="equipmentName"
              label={t('equipment.fields.equipmentName')}
              required
              className="col-12"
              value={form.equipmentName}
              onChange={(value) => setForm({ ...form, equipmentName: value })}
              maxLength={200}
            />

            <Input
              id="examinationDate"
              label={t('equipment.fields.examinationDate')}
              className="col-md-6"
              helpText={t('equipment.nextDateHint')}
              value={form.examinationDate ?? ''}
              onChange={(value) => setForm({ ...form, examinationDate: value })}
              inputProps={{ type: 'date' }}
            />

            <Input
              id="examinationPerformedBy"
              label={t('equipment.fields.examinationPerformedBy')}
              className="col-md-6"
              value={form.examinationPerformedBy ?? ''}
              onChange={(value) => setForm({ ...form, examinationPerformedBy: value })}
              maxLength={200}
            />

            <TextArea
              id="examinationReport"
              label={t('equipment.fields.examinationReport')}
              className="col-12"
              rows={3}
              value={form.examinationReport ?? ''}
              onChange={(value) => setForm({ ...form, examinationReport: value })}
            />
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
