import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { ErrorPanel, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, controlClass } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { HAZARD_CLASS_BADGE, useLookup } from '@/api/endpoints'
import { HazardClass } from '@/api/enums'
import {
  HAZARD_CLASSES,
  RESOURCES,
  useDeleteTopic,
  useEmployeeLookup,
  useSaveTopic,
  useTrainingDetail,
  useTrainingValidity,
  type SaveTrainingTopicDto,
  type TrainingDto,
  type TrainingTopicDto,
} from './api'
import TrainingFormModal from './TrainingFormModal'

/**
 * A training with everything hanging off it: the hazard-class durations, the topics that make
 * up the remote-learning deck, the attached exams, and the statutory validity the API computes.
 */
export default function TrainingDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const trainingId = Number(id)

  const { data, isLoading, error } = useTrainingDetail(trainingId)
  const [isEditOpen, setEditOpen] = useState(false)
  const [editingTopic, setEditingTopic] = useState<TrainingTopicDto | null>(null)
  const [isTopicCreateOpen, setTopicCreateOpen] = useState(false)
  const [deletingTopic, setDeletingTopic] = useState<TrainingTopicDto | null>(null)

  const removeTopic = useDeleteTopic(trainingId)

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const training = data.training

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/trainings" className="text-decoration-none">
              {t('training.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {training.trainingName}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={training.trainingName}
        description={
          training.trainingCode
            ? t('training.detail.code', { value: training.trainingCode })
            : undefined
        }
        action={
          <button className="btn btn-light-primary" type="button" onClick={() => setEditOpen(true)}>
            {t('common.edit')}
          </button>
        }
      />

      <div className="row g-4">
        <div className="col-lg-7">
          <GeneralCard training={training} groupName={data.trainingGroup?.displayName} />
        </div>
        <div className="col-lg-5">
          <ValidityCard training={training} />
        </div>

        <div className="col-12">
          <div className="card">
            <div className="card-header d-flex align-items-center justify-content-between">
              <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
                {t('training.detail.topics')}
              </h2>
              <button
                type="button"
                className="btn btn-sm btn-primary"
                onClick={() => setTopicCreateOpen(true)}
              >
                {t('training.topic.create')}
              </button>
            </div>
            <div className="card-body p-0">
              <TopicTable
                topics={data.topics}
                onEdit={setEditingTopic}
                onDelete={setDeletingTopic}
              />
            </div>
          </div>
        </div>

        {data.exams.length > 0 && (
          <div className="col-12">
            <div className="card">
              <div className="card-header">
                <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
                  {t('training.detail.exams')}
                </h2>
              </div>
              <div className="card-body">
                <ul className="list-unstyled mb-0 d-flex flex-wrap gap-2">
                  {data.exams.map((exam) => (
                    <li key={exam.id}>
                      <span className="badge-light-primary">{exam.displayName}</span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </div>
        )}
      </div>

      {isEditOpen && (
        <TrainingFormModal isOpen training={training} onClose={() => setEditOpen(false)} />
      )}

      {isTopicCreateOpen && (
        <TopicFormModal trainingId={trainingId} onClose={() => setTopicCreateOpen(false)} />
      )}

      {editingTopic && (
        <TopicFormModal
          trainingId={trainingId}
          topic={editingTopic}
          onClose={() => setEditingTopic(null)}
        />
      )}

      <ConfirmDialog
        isOpen={deletingTopic !== null}
        title={t('training.topic.deleteTitle')}
        message={t('training.topic.deleteMessage', { name: deletingTopic?.topicTitle ?? '' })}
        onCancel={() => setDeletingTopic(null)}
        onConfirm={() =>
          deletingTopic &&
          removeTopic.mutate(deletingTopic.id, { onSuccess: () => setDeletingTopic(null) })
        }
        isBusy={removeTopic.isPending}
        error={removeTopic.error ? errorMessage(removeTopic.error) : null}
      />
    </>
  )
}

/** Header facts of the catalogue entry, including the duration table. */
function GeneralCard({ training, groupName }: { training: TrainingDto; groupName?: string }) {
  const { t } = useTranslation()
  const none = t('common.none')

  return (
    <div className="card h-100">
      <div className="card-header">
        <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
          {t('training.detail.general')}
        </h2>
      </div>
      <div className="card-body">
        <dl className="row mb-4" style={{ fontSize: '0.9375rem' }}>
          <Term label={t('training.fields.trainingType')}>
            {t(`enums.trainingType.${training.trainingType}`)}
          </Term>
          <Term label={t('training.fields.topicGroup')}>
            {t(`enums.trainingSubjectGroup.${training.topicGroup}`)}
          </Term>
          <Term label={t('training.fields.trainingGroup')}>{groupName ?? none}</Term>
          <Term label={t('training.fields.ibysTrainingCode')}>
            {training.ibysTrainingCode ?? none}
          </Term>
          <Term label={t('training.fields.includedInDefaultPlan')}>
            {training.includedInDefaultPlan ? t('common.yes') : t('common.no')}
          </Term>
          <Term label={t('training.fields.defaultTraining')}>
            {training.defaultTraining ? t('common.yes') : t('common.no')}
          </Term>
          <Term label={t('training.fields.mandatoryTraining')}>
            {training.mandatoryTraining ? t('common.yes') : t('common.no')}
          </Term>
          <Term label={t('training.fields.defaultCount')}>{training.defaultCount}</Term>
          <Term label={t('training.fields.defaultStartMonthOffset')}>
            {training.defaultStartMonthOffset}
          </Term>
          <Term label={t('training.fields.defaultElementCondition')}>
            {training.defaultElementCondition}
          </Term>
          <Term label={t('training.fields.status')}>
            <span className={training.isActive ? 'badge-light-success' : 'badge-light-danger'}>
              {training.isActive ? t('common.active') : t('common.passive')}
            </span>
          </Term>
        </dl>

        <h3 className="h6 fw-semibold mb-2" style={{ color: 'var(--kt-gray-900)' }}>
          {t('training.detail.durations')}
        </h3>
        <DurationList durations={training.durations} />
      </div>
    </div>
  )
}

/** Duration badges, one per hazard class, in the statutory order. */
function DurationList({
  durations,
}: {
  durations: { hazardClass: HazardClass; durationMinutes: number }[]
}) {
  const { t } = useTranslation()

  return (
    <ul className="list-unstyled mb-0 d-flex flex-wrap gap-2">
      {HAZARD_CLASSES.map((hazardClass) => {
        const minutes =
          durations.find((item) => item.hazardClass === hazardClass)?.durationMinutes ?? 0
        return (
          <li key={hazardClass}>
            <span className={HAZARD_CLASS_BADGE[hazardClass]}>
              {t(`enums.hazardClass.${hazardClass}`)}: {t('training.minutes', { count: minutes })}
            </span>
          </li>
        )
      })}
    </ul>
  )
}

/**
 * Statutory validity check.
 *
 * `GET api/training/{id}/validity` answers for one employee, so the panel asks for a workplace
 * and an employee first and only then fires the request — the renewal interval (three years for
 * a low-hazard workplace, two for hazardous, one for very hazardous) is the API's to decide.
 */
function ValidityCard({ training }: { training: TrainingDto }) {
  const { t } = useTranslation()
  const [companyId, setCompanyId] = useState<number | null>(null)
  const [employeeId, setEmployeeId] = useState<number | null>(null)
  const [hazardClass, setHazardClass] = useState<HazardClass>(HazardClass.LowHazard)

  const companies = useLookup(RESOURCES.company)
  const employees = useEmployeeLookup(companyId ?? undefined)
  const validity = useTrainingValidity(training.id, employeeId ?? undefined, hazardClass)

  return (
    <div className="card h-100">
      <div className="card-header">
        <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
          {t('training.validity.title')}
        </h2>
      </div>
      <div className="card-body">
        <p style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
          {t('training.validity.description')}
        </p>

        <div className="row g-3">
          <Field
            label={t('training.validity.company')}
            htmlFor="validity-company"
            className="col-12"
          >
            <select
              id="validity-company"
              className="form-select"
              value={companyId ?? ''}
              onChange={(event) => {
                setCompanyId(event.target.value === '' ? null : Number(event.target.value))
                setEmployeeId(null)
              }}
            >
              <option value="">{t('training.validity.selectCompany')}</option>
              {companies.data?.items.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.displayName}
                </option>
              ))}
            </select>
          </Field>

          <Field
            label={t('training.validity.employee')}
            htmlFor="validity-employee"
            className="col-12"
          >
            <select
              id="validity-employee"
              className="form-select"
              value={employeeId ?? ''}
              disabled={!companyId}
              onChange={(event) =>
                setEmployeeId(event.target.value === '' ? null : Number(event.target.value))
              }
            >
              <option value="">{t('training.validity.selectEmployee')}</option>
              {employees.data?.items.map((employee) => (
                <option key={employee.id} value={employee.id}>
                  {employee.displayName}
                </option>
              ))}
            </select>
          </Field>

          <Field
            label={t('training.validity.hazardClass')}
            htmlFor="validity-hazard"
            className="col-12"
          >
            <select
              id="validity-hazard"
              className="form-select"
              value={hazardClass}
              onChange={(event) => setHazardClass(Number(event.target.value) as HazardClass)}
            >
              {HAZARD_CLASSES.map((value) => (
                <option key={value} value={value}>
                  {t(`enums.hazardClass.${value}`)}
                </option>
              ))}
            </select>
          </Field>
        </div>

        <div className="mt-4 pt-3" style={{ borderTop: '1px solid var(--kt-border-color)' }}>
          {!employeeId && (
            <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
              {t('training.validity.awaitingSelection')}
            </p>
          )}
          {employeeId && validity.isLoading && <Spinner />}
          {employeeId && validity.error && <ErrorPanel message={errorMessage(validity.error)} />}
          {employeeId && validity.data && (
            <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
              <Term label={t('training.validity.state')}>
                <span
                  className={
                    validity.data.isValid ? 'badge-light-success' : 'badge-light-danger'
                  }
                >
                  {validity.data.isValid
                    ? t('training.validity.valid')
                    : t('training.validity.expired')}
                </span>
              </Term>
              <Term label={t('training.validity.mandatoryDuration')}>
                {t('training.minutes', { count: validity.data.mandatoryDurationMinutes })}
              </Term>
            </dl>
          )}
        </div>
      </div>
    </div>
  )
}

/** The topics of the training, in display order. */
function TopicTable({
  topics,
  onEdit,
  onDelete,
}: {
  topics: TrainingTopicDto[]
  onEdit: (topic: TrainingTopicDto) => void
  onDelete: (topic: TrainingTopicDto) => void
}) {
  const { t } = useTranslation()

  const columns: Column<TrainingTopicDto>[] = [
    {
      key: 'topicOrder',
      header: t('training.topic.fields.order'),
      width: '80px',
      align: 'center',
      render: (topic) => topic.topicOrder,
    },
    {
      key: 'topicTitle',
      header: t('training.topic.fields.title'),
      render: (topic) => <span className="fw-semibold">{topic.topicTitle}</span>,
    },
    {
      key: 'pages',
      header: t('training.topic.fields.pageCount'),
      align: 'end',
      render: (topic) => topic.presentationPageCount,
    },
    {
      key: 'durations',
      header: t('training.detail.durations'),
      render: (topic) => <DurationList durations={topic.durations} />,
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '120px',
      render: (topic) => (
        <div className="d-flex justify-content-end gap-1">
          <button
            type="button"
            className="btn btn-sm btn-light"
            onClick={() => onEdit(topic)}
            aria-label={t('training.topic.editAria', { name: topic.topicTitle })}
          >
            {t('common.edit')}
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            onClick={() => onDelete(topic)}
            aria-label={t('training.topic.deleteAria', { name: topic.topicTitle })}
          >
            {t('common.delete')}
          </button>
        </div>
      ),
    },
  ]

  return (
    <DataTable
      label={t('training.detail.topics')}
      columns={columns}
      rows={topics}
      rowKey={(topic) => topic.id}
      emptyMessage={t('training.topic.empty')}
    />
  )
}

/** Create/edit dialog of a topic, durations included. */
function TopicFormModal({
  trainingId,
  topic,
  onClose,
}: {
  trainingId: number
  topic?: TrainingTopicDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const save = useSaveTopic(trainingId)
  const [titleError, setTitleError] = useState<string | undefined>()
  const [model, setModel] = useState<SaveTrainingTopicDto>(() => ({
    topicTitle: topic?.topicTitle ?? '',
    presentationAddress: topic?.presentationAddress ?? '',
    presentationPageCount: topic?.presentationPageCount ?? 0,
    topicOrder: topic?.topicOrder ?? 0,
    durations: HAZARD_CLASSES.map((hazardClass) => ({
      hazardClass,
      durationMinutes:
        topic?.durations.find((item) => item.hazardClass === hazardClass)?.durationMinutes ?? 0,
    })),
  }))

  function submit() {
    if (!model.topicTitle.trim()) {
      setTitleError(t('common.required'))
      return
    }
    setTitleError(undefined)
    save.mutate(
      {
        topicId: topic?.id,
        input: {
          ...model,
          topicTitle: model.topicTitle.trim(),
          presentationAddress: model.presentationAddress?.trim() || null,
        },
      },
      { onSuccess: onClose },
    )
  }

  return (
    <Modal
      title={topic ? t('training.topic.editTitle') : t('training.topic.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={save.isPending}
      error={save.error ? errorMessage(save.error) : null}
      size="lg"
    >
      <div className="row g-3">
        <Field
          label={t('training.topic.fields.title')}
          htmlFor="topic-title"
          required
          error={titleError}
          className="col-md-8"
        >
          <input
            id="topic-title"
            className={controlClass('form-control', titleError)}
            value={model.topicTitle}
            onChange={(event) => setModel({ ...model, topicTitle: event.target.value })}
          />
        </Field>

        <Field
          label={t('training.topic.fields.order')}
          htmlFor="topic-order"
          className="col-md-4"
        >
          <input
            id="topic-order"
            type="number"
            min={0}
            className="form-control"
            value={model.topicOrder}
            onChange={(event) => setModel({ ...model, topicOrder: Number(event.target.value) || 0 })}
          />
        </Field>

        <Field
          label={t('training.topic.fields.presentationAddress')}
          htmlFor="topic-address"
          className="col-md-8"
        >
          <input
            id="topic-address"
            className="form-control"
            value={model.presentationAddress ?? ''}
            onChange={(event) => setModel({ ...model, presentationAddress: event.target.value })}
          />
        </Field>

        <Field
          label={t('training.topic.fields.pageCount')}
          htmlFor="topic-pages"
          className="col-md-4"
        >
          <input
            id="topic-pages"
            type="number"
            min={0}
            className="form-control"
            value={model.presentationPageCount}
            onChange={(event) =>
              setModel({ ...model, presentationPageCount: Number(event.target.value) || 0 })
            }
          />
        </Field>

        <div className="col-12">
          <h3 className="h6 fw-semibold mb-2" style={{ color: 'var(--kt-gray-900)' }}>
            {t('training.detail.durations')}
          </h3>
          <div className="row g-3">
            {model.durations.map((duration) => (
              <Field
                key={duration.hazardClass}
                label={t(`enums.hazardClass.${duration.hazardClass}`)}
                htmlFor={`topic-duration-${duration.hazardClass}`}
                className="col-md-4"
              >
                <input
                  id={`topic-duration-${duration.hazardClass}`}
                  type="number"
                  min={0}
                  className="form-control"
                  value={duration.durationMinutes}
                  onChange={(event) =>
                    setModel({
                      ...model,
                      durations: model.durations.map((item) =>
                        item.hazardClass === duration.hazardClass
                          ? { ...item, durationMinutes: Number(event.target.value) || 0 }
                          : item,
                      ),
                    })
                  }
                />
              </Field>
            ))}
          </div>
        </div>
      </div>
    </Modal>
  )
}

/** One `<dt>`/`<dd>` pair of a definition list. */
function Term({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <>
      <dt className="col-sm-5" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
        {label}
      </dt>
      <dd className="col-sm-7">{children}</dd>
    </>
  )
}
