import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, CheckBox, NumberInput, Select } from 'rich-react-component'
import DataTable, { ErrorPanel, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { Modal } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useLookup } from '@/api/endpoints'
import {
  RESOURCES,
  useEmployeeLookup,
  useEmployeeProgressList,
  useProgressDetail,
  useSaveTopicProgress,
  useStartProgress,
  useSubmitExam,
  useTrainingDetail,
  type EmployeeTrainingProgressDto,
} from './api'

/**
 * Distance-learning progress per employee.
 *
 * The record is the statutory evidence that an employee sat a training, so the completion state
 * is the loudest thing on the row. The API exposes progress per employee rather than as a global
 * paged list, which is why the screen asks for a workplace and an employee first.
 */
export default function TrainingProgressPage() {
  const { t } = useTranslation()
  const [companyId, setCompanyId] = useState<number | null>(null)
  const [employeeId, setEmployeeId] = useState<number | null>(null)
  const [isStartOpen, setStartOpen] = useState(false)
  const [savingProgress, setSavingProgress] = useState<EmployeeTrainingProgressDto | null>(null)
  const [examFor, setExamFor] = useState<EmployeeTrainingProgressDto | null>(null)
  const [detailId, setDetailId] = useState<number | null>(null)

  const companies = useLookup(RESOURCES.company)
  const employees = useEmployeeLookup(companyId ?? undefined)
  const progress = useEmployeeProgressList(employeeId ?? undefined)

  // One lookup request resolves every training name on the table — never one call per row.
  const trainings = useLookup(RESOURCES.training)
  const trainingNames = useMemo(() => {
    const map = new Map<number, string>()
    for (const training of trainings.data?.items ?? []) map.set(training.id, training.displayName)
    return map
  }, [trainings.data])

  const rows = progress.data?.items ?? []
  const completedCount = rows.filter((row) => row.latestTestCompleted).length

  const columns: Column<EmployeeTrainingProgressDto>[] = [
    {
      key: 'training',
      header: t('trainingProgress.fields.training'),
      render: (row) => (
        <span className="fw-semibold">
          {trainingNames.get(row.trainingId) ?? t('trainingProgress.unknownTraining')}
        </span>
      ),
    },
    {
      key: 'completion',
      header: t('trainingProgress.fields.completion'),
      align: 'center',
      render: (row) => (
        <Badge variant={row.latestTestCompleted ? 'success' : 'danger'}>
          {row.latestTestCompleted
            ? t('trainingProgress.completed')
            : t('trainingProgress.incomplete')}
        </Badge>
      ),
    },
    {
      key: 'firstTest',
      header: t('trainingProgress.fields.firstTest'),
      align: 'center',
      render: (row) => <TestCell completed={row.firstTestCompleted} score={row.firstTestNote} />,
    },
    {
      key: 'finalTest',
      header: t('trainingProgress.fields.finalTest'),
      align: 'center',
      render: (row) => <TestCell completed={row.latestTestCompleted} score={row.latestTestNote} />,
    },
    {
      key: 'elapsed',
      header: t('trainingProgress.fields.elapsed'),
      align: 'end',
      render: (row) => <Duration seconds={row.elapsedDurationSeconds} />,
    },
    {
      key: 'activePage',
      header: t('trainingProgress.fields.activePage'),
      align: 'end',
      render: (row) => row.activePage,
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '260px',
      render: (row) => (
        <div className="d-flex justify-content-end flex-wrap gap-1">
          <Button variant="light" size="sm"
            onClick={() => setDetailId(row.id)}
            aria-label={t('trainingProgress.detailAria', {
              name: trainingNames.get(row.trainingId) ?? '',
            })}
          >
            {t('common.detail')}
          </Button>
          <Button variant="light" size="sm" 
            onClick={() => setSavingProgress(row)}
            aria-label={t('trainingProgress.saveProgressAria', {
              name: trainingNames.get(row.trainingId) ?? '',
            })}
          >
            {t('trainingProgress.saveProgress')}
          </Button>
          <Button variant="light" size="sm" 
            onClick={() => setExamFor(row)}
            aria-label={t('trainingProgress.submitExamAria', {
              name: trainingNames.get(row.trainingId) ?? '',
            })}
          >
            {t('trainingProgress.submitExam')}
          </Button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('trainingProgress.title')}
        description={t('trainingProgress.description')}
        action={
          <Button variant="primary"
            disabled={!employeeId}
            onClick={() => setStartOpen(true)}
          >
            {t('trainingProgress.start')}
          </Button>
        }
      />

      <Card
        className="mb-4"
      >
          <div className="row g-3">
            <Select
              id="progress-company"
              label={t('trainingProgress.fields.company')}
              className="col-md-4"
              placeholder={t('trainingProgress.selectCompany')}
              options={
                companies.data?.items.map((company) => ({
                  value: company.id,
                  label: company.displayName,
                })) ?? []
              }
              value={companyId}
              onChange={(value) => {
                setCompanyId(value)
                setEmployeeId(null)
              }}
            />

            <Select
              id="progress-employee"
              label={t('trainingProgress.fields.employee')}
              className="col-md-4"
              placeholder={t('trainingProgress.selectEmployee')}
              disabled={!companyId}
              options={
                employees.data?.items.map((employee) => ({
                  value: employee.id,
                  label: employee.displayName,
                })) ?? []
              }
              value={employeeId}
              onChange={(value) => setEmployeeId(value)}
            />

            {employeeId && rows.length > 0 && (
              <div className="col-md-4 d-flex align-items-end">
                <p className="mb-2 fw-semibold" style={{ color: 'var(--kt-gray-700)' }}>
                  {t('trainingProgress.summary', {
                    completed: completedCount,
                    total: rows.length,
                  })}
                </p>
              </div>
            )}
          </div>
        
      </Card>

      {!employeeId ? (
        <Card>
          <div className="text-center py-5" style={{ color: 'var(--kt-gray-500)' }}>
            {t('trainingProgress.awaitingSelection')}
          </div>
        </Card>
      ) : (
        <Card
          
        >
            <DataTable
              label={t('trainingProgress.title')}
              columns={columns}
              rows={rows}
              rowKey={(row) => row.id}
              isLoading={progress.isLoading}
              error={progress.error ? errorMessage(progress.error) : null}
              emptyMessage={t('trainingProgress.empty')}
            />
          
        </Card>
      )}

      {isStartOpen && employeeId && (
        <StartModal employeeId={employeeId} onClose={() => setStartOpen(false)} />
      )}

      {savingProgress && (
        <SaveProgressModal record={savingProgress} onClose={() => setSavingProgress(null)} />
      )}

      {examFor && <ExamModal record={examFor} onClose={() => setExamFor(null)} />}

      {detailId !== null && <DetailModal id={detailId} onClose={() => setDetailId(null)} />}
    </>
  )
}

/** Pass/fail chip with the recorded score. */
function TestCell({ completed, score }: { completed: boolean; score?: number | null }) {
  const { t } = useTranslation()
  return (
    <Badge variant={completed ? 'success' : 'warning'}>
      {completed ? t('trainingProgress.passed') : t('trainingProgress.notTaken')}
      {score != null && ` · ${score}`}
    </Badge>
  )
}

/** Seconds rendered as hours and minutes in the active language. */
function Duration({ seconds }: { seconds: number }) {
  const { t } = useTranslation()
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  return <>{t('trainingProgress.duration', { hours, minutes })}</>
}

/** Starts (or resumes) a remote training for the selected employee. */
function StartModal({ employeeId, onClose }: { employeeId: number; onClose: () => void }) {
  const { t } = useTranslation()
  const trainings = useLookup(RESOURCES.training)
  const start = useStartProgress()
  const [trainingId, setTrainingId] = useState<number>(0)
  const [topicId, setTopicId] = useState<number | null>(null)
  const [trainingError, setTrainingError] = useState<string | undefined>()

  // Topics are optional: progress can be tracked for the training as a whole.
  const detail = useTrainingDetail(trainingId || undefined)

  return (
    <Modal
      title={t('trainingProgress.startTitle')}
      isOpen
      onClose={onClose}
      onSubmit={() => {
        if (!trainingId) {
          setTrainingError(t('common.required'))
          return
        }
        setTrainingError(undefined)
        start.mutate(
          { companyEmployeeId: employeeId, trainingId, trainingTopicId: topicId },
          { onSuccess: onClose },
        )
      }}
      isBusy={start.isPending}
      confirmLabel={t('trainingProgress.start')}
      error={start.error ? errorMessage(start.error) : null}
    >
      <div className="row g-3">
        <Select
          id="start-training"
          label={t('trainingProgress.fields.training')}
          required
          error={trainingError}
          placeholder={t('trainingProgress.selectTraining')}
          options={
            trainings.data?.items.map((training) => ({
              value: training.id,
              label: training.displayName,
            })) ?? []
          }
          value={trainingId || null}
          onChange={(value) => {
            setTrainingId(value ?? 0)
            setTopicId(null)
          }}
        />

        <Select
          id="start-topic"
          label={t('trainingProgress.fields.topic')}
          helpText={t('trainingProgress.topicHint')}
          placeholder={t('trainingProgress.wholeTraining')}
          disabled={!trainingId || !detail.data?.topics.length}
          options={
            detail.data?.topics.map((topic) => ({
              value: topic.id,
              label: topic.topicTitle,
            })) ?? []
          }
          value={topicId}
          onChange={(value) => setTopicId(value)}
        />
      </div>
    </Modal>
  )
}

/** Records elapsed time and the page the employee reached. */
function SaveProgressModal({
  record,
  onClose,
}: {
  record: EmployeeTrainingProgressDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const save = useSaveTopicProgress()
  const [minutes, setMinutes] = useState(Math.floor(record.elapsedDurationSeconds / 60))
  const [activePage, setActivePage] = useState(record.activePage)

  return (
    <Modal
      title={t('trainingProgress.saveProgressTitle')}
      isOpen
      onClose={onClose}
      onSubmit={() =>
        save.mutate(
          {
            id: record.id,
            input: {
              trainingTopicId: record.trainingTopicId ?? null,
              elapsedDurationSeconds: Math.max(0, minutes) * 60,
              activePage: Math.max(0, activePage),
            },
          },
          { onSuccess: onClose },
        )
      }
      isBusy={save.isPending}
      error={save.error ? errorMessage(save.error) : null}
    >
      <div className="row g-3">
        <NumberInput
          id="progress-minutes"
          label={t('trainingProgress.fields.elapsedMinutes')}
          helpText={t('trainingProgress.elapsedHint')}
          className="col-md-6"
          min={0}
          value={minutes}
          onChange={(value) => setMinutes(value ?? 0)}
        />

        <NumberInput
          id="progress-page"
          label={t('trainingProgress.fields.activePage')}
          className="col-md-6"
          min={0}
          value={activePage}
          onChange={(value) => setActivePage(value ?? 0)}
        />
      </div>
    </Modal>
  )
}

/** Records an exam attempt — the pre-test or the final test — with its score. */
function ExamModal({
  record,
  onClose,
}: {
  record: EmployeeTrainingProgressDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const submit = useSubmitExam()
  const [isFirstTest, setFirstTest] = useState(!record.firstTestCompleted)
  const [score, setScore] = useState(0)
  const [completed, setCompleted] = useState(true)

  return (
    <Modal
      title={t('trainingProgress.submitExamTitle')}
      isOpen
      onClose={onClose}
      onSubmit={() =>
        submit.mutate(
          { id: record.id, input: { isFirstTest, score, completed } },
          { onSuccess: onClose },
        )
      }
      isBusy={submit.isPending}
      confirmLabel={t('trainingProgress.submitExam')}
      error={submit.error ? errorMessage(submit.error) : null}
    >
      <div className="row g-3">
        <Select
          id="exam-type"
          label={t('trainingProgress.fields.examType')}
          className="col-md-6"
          options={[
            { value: 'first', label: t('trainingProgress.firstTest') },
            { value: 'final', label: t('trainingProgress.finalTest') },
          ]}
          value={isFirstTest ? 'first' : 'final'}
          onChange={(value) => setFirstTest(value === 'first')}
        />

        <NumberInput
          id="exam-score"
          label={t('trainingProgress.fields.score')}
          className="col-md-6"
          min={0}
          max={100}
          value={score}
          onChange={(value) => setScore(value ?? 0)}
        />

        <div className="col-12">
          <CheckBox
            id="exam-completed"
            checked={completed}
            onChange={setCompleted}
            label={t('trainingProgress.countsAsPassed')}
          />
        </div>
      </div>
    </Modal>
  )
}

/** Read-only view with the remaining statutory time the API computes. */
function DetailModal({ id, onClose }: { id: number; onClose: () => void }) {
  const { t } = useTranslation()
  const { data, isLoading, error } = useProgressDetail(id)
  const none = t('common.none')

  return (
    <Modal title={t('trainingProgress.detailTitle')} isOpen onClose={onClose}>
      {isLoading && <Spinner />}
      {error && <ErrorPanel message={errorMessage(error)} />}
      {data && (
        <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
          <dt className="col-6" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
            {t('trainingProgress.fields.employee')}
          </dt>
          <dd className="col-6">{data.employee?.displayName ?? none}</dd>

          <dt className="col-6" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
            {t('trainingProgress.fields.training')}
          </dt>
          <dd className="col-6">{data.training?.displayName ?? none}</dd>

          <dt className="col-6" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
            {t('trainingProgress.fields.elapsed')}
          </dt>
          <dd className="col-6">
            <Duration seconds={data.progress.elapsedDurationSeconds} />
          </dd>

          <dt className="col-6" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
            {t('trainingProgress.fields.remaining')}
          </dt>
          <dd className="col-6">
            <Duration seconds={data.remainingDurationSeconds} />
          </dd>

          <dt className="col-6" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
            {t('trainingProgress.fields.completion')}
          </dt>
          <dd className="col-6">
            <Badge variant={data.progress.latestTestCompleted ? 'success' : 'danger'}
            >
              {data.progress.latestTestCompleted
                ? t('trainingProgress.completed')
                : t('trainingProgress.incomplete')}
            </Badge>
          </dd>
        </dl>
      )}
    </Modal>
  )
}
