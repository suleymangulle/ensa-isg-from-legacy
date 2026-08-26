import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, SearchBar } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { useEntity } from '@/api/endpoints'
import { TrainingSubjectGroup, TrainingType } from '@/api/enums'
import {
  RESOURCES,
  TRAINING_SUBJECT_GROUPS,
  TRAINING_TYPES,
  useTrainingList,
  type TrainingDto,
  type TrainingListDto,
} from './api'
import TrainingFormModal from './TrainingFormModal'

const PAGE_SIZE = 20

/**
 * Training catalogue.
 *
 * The catalogue is the master data every training plan line points at: a name, an İSG-KATİP
 * code, a subject group and the mandatory duration per hazard class. Topics and durations are
 * managed on the detail page, where there is room for them.
 */
export default function TrainingListPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [trainingType, setTrainingType] = useState<TrainingType | null>(null)
  const [topicGroup, setTopicGroup] = useState<TrainingSubjectGroup | null>(null)
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState<TrainingListDto | null>(null)

  const { data, isLoading, error } = useTrainingList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'TrainingName ASC',
    filter: search,
    trainingType,
    topicGroup,
  })

  // Loaded only while the edit dialog is open — the list row carries no durations.
  const { data: editing } = useEntity<TrainingDto>(RESOURCES.training, editingId ?? undefined)

  const remove = useDelete(RESOURCES.training, { onSuccess: () => setDeleting(null) })

  function resetToFirstPage<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value)
      setPage(1)
    }
  }

  const columns: Column<TrainingListDto>[] = [
    {
      key: 'trainingName',
      header: t('training.fields.trainingName'),
      render: (training) => (
        <Link to={`/trainings/${training.id}`} className="fw-semibold text-decoration-none">
          {training.trainingName}
        </Link>
      ),
    },
    {
      key: 'trainingCode',
      header: t('training.fields.trainingCode'),
      render: (training) => training.trainingCode ?? t('common.none'),
    },
    {
      key: 'trainingType',
      header: t('training.fields.trainingType'),
      render: (training) => t(`enums.trainingType.${training.trainingType}`),
    },
    {
      key: 'topicGroup',
      header: t('training.fields.topicGroup'),
      render: (training) => t(`enums.trainingSubjectGroup.${training.topicGroup}`),
    },
    {
      key: 'flags',
      header: t('training.fields.flags'),
      render: (training) => (
        <span className="d-inline-flex flex-wrap gap-1">
          {training.mandatoryTraining && (
            <span className="badge-light-danger">{t('training.flags.mandatory')}</span>
          )}
          {training.defaultTraining && (
            <span className="badge-light-info">{t('training.flags.default')}</span>
          )}
          {!training.mandatoryTraining && !training.defaultTraining && t('common.none')}
        </span>
      ),
    },
    {
      key: 'status',
      header: t('training.fields.status'),
      align: 'center',
      render: (training) => (
        <span className={training.isActive ? 'badge-light-success' : 'badge-light-danger'}>
          {training.isActive ? t('common.active') : t('common.passive')}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '120px',
      render: (training) => (
        <div className="d-flex justify-content-end gap-1">
          <button
            type="button"
            className="btn btn-sm btn-light"
            onClick={() => setEditingId(training.id)}
            aria-label={t('training.list.editAria', { name: training.trainingName })}
          >
            {t('common.edit')}
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            onClick={() => setDeleting(training)}
            aria-label={t('training.list.deleteAria', { name: training.trainingName })}
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
        title={t('training.list.title')}
        description={t('training.list.description')}
        action={
          <button className="btn btn-primary" type="button" onClick={() => setCreateOpen(true)}>
            {t('training.list.create')}
          </button>
        }
      />

      <div className="card">
        <div className="card-header">
          <SearchBar
            value={search}
            onChange={resetToFirstPage(setSearch)}
            placeholder={t('training.list.searchPlaceholder')}
          >
            <div>
              <label htmlFor="training-type-filter" className="visually-hidden">
                {t('training.fields.trainingType')}
              </label>
              <select
                id="training-type-filter"
                className="form-select"
                value={trainingType ?? ''}
                onChange={(event) =>
                  resetToFirstPage(setTrainingType)(
                    event.target.value === '' ? null : (Number(event.target.value) as TrainingType),
                  )
                }
              >
                <option value="">{t('training.list.allTypes')}</option>
                {TRAINING_TYPES.map((value) => (
                  <option key={value} value={value}>
                    {t(`enums.trainingType.${value}`)}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="training-group-filter" className="visually-hidden">
                {t('training.fields.topicGroup')}
              </label>
              <select
                id="training-group-filter"
                className="form-select"
                value={topicGroup ?? ''}
                onChange={(event) =>
                  resetToFirstPage(setTopicGroup)(
                    event.target.value === ''
                      ? null
                      : (Number(event.target.value) as TrainingSubjectGroup),
                  )
                }
              >
                <option value="">{t('training.list.allGroups')}</option>
                {TRAINING_SUBJECT_GROUPS.map((value) => (
                  <option key={value} value={value}>
                    {t(`enums.trainingSubjectGroup.${value}`)}
                  </option>
                ))}
              </select>
            </div>
          </SearchBar>
        </div>
        <div className="card-body p-0">
          <DataTable
            label={t('training.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(training) => training.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('training.list.empty')}
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

      {isCreateOpen && <TrainingFormModal isOpen onClose={() => setCreateOpen(false)} />}

      {editingId !== null && editing && (
        <TrainingFormModal isOpen training={editing} onClose={() => setEditingId(null)} />
      )}

      <ConfirmDialog
        isOpen={deleting !== null}
        title={t('training.list.deleteTitle')}
        message={t('training.list.deleteMessage', { name: deleting?.trainingName ?? '' })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}
