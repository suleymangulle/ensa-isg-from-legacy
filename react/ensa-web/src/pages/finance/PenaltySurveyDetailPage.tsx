import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card } from 'rich-react-component'
import DataTable, { ErrorPanel, PageTitle, Pagination, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { formatDate } from '@/utils/format'
import {
  useAddPenaltySurveyLine,
  usePenaltySurvey,
  usePenaltySurveyLines,
  usePenaltySurveyTotal,
  useRemovePenaltySurveyLine,
  useUpdatePenaltySurveyLine,
  type PenaltySurveyLineDto,
} from './api'
import {
  Breadcrumb,
  FilterSelect,
  MoneyCell,
  MoneyStat,
  RowActions,
  Term,
} from './components'
import { formatNumber, formatQuantity } from '@/utils/format'
import { PenaltySurveyEditor } from './PenaltiesPage'
import PenaltySurveyLineForm from './PenaltySurveyLineForm'

const PAGE_SIZE = 20

/**
 * One fine-risk survey: the prospective customer's profile, the answered articles and the total
 * exposure — the modern equivalent of the legacy `CezaAnketi` screen.
 *
 * Both money figures on this page are server-side aggregates. Each line's `penaltyAmount` is
 * resolved from the fine matrix by `IPenaltySurveyAppService` using the survey's own hazard
 * class and head count, and the headline total comes from `GET /surveys/{id}/total`. Nothing is
 * summed in the browser, which is why answering an article refreshes the total rather than
 * nudging a local counter.
 */
export default function PenaltySurveyDetailPage() {
  const { t } = useTranslation()
  const { surveyId: surveyIdParam } = useParams()
  const surveyId = Number(surveyIdParam)

  const [page, setPage] = useState(1)
  const [answerFilter, setAnswerFilter] = useState('')

  const [isEditing, setEditing] = useState(false)
  const [isAdding, setAdding] = useState(false)
  const [editingLine, setEditingLine] = useState<PenaltySurveyLineDto | null>(null)
  const [deletingLine, setDeletingLine] = useState<PenaltySurveyLineDto | null>(null)

  const survey = usePenaltySurvey(surveyId)
  const total = usePenaltySurveyTotal(surveyId)
  const lines = usePenaltySurveyLines({
    penaltySurveyId: surveyId,
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'Id ASC',
    surveyAnswer: answerFilter === '' ? undefined : answerFilter === 'true',
  })

  const addLine = useAddPenaltySurveyLine(surveyId, () => setAdding(false))
  const updateLine = useUpdatePenaltySurveyLine(surveyId, () => setEditingLine(null))
  const removeLine = useRemovePenaltySurveyLine(surveyId, () => setDeletingLine(null))

  if (survey.isLoading) return <Spinner />
  if (survey.error) return <ErrorPanel message={errorMessage(survey.error)} />
  if (!survey.data) return <ErrorPanel message={t('errors.notFound')} />

  const header = survey.data
  const none = t('common.none')
  const currency = t('finance.common.currency')

  const columns: Column<PenaltySurveyLineDto>[] = [
    {
      key: 'penaltyId',
      header: t('finance.penaltySurvey.line.fields.penalty'),
      render: (row) => t('finance.penaltySurvey.line.articleRef', { id: row.penaltyId }),
    },
    {
      key: 'surveyAnswer',
      header: t('finance.penaltySurvey.line.fields.surveyAnswer'),
      align: 'center',
      render: (row) => (
        <Badge variant={row.surveyAnswer ? 'danger' : 'success'}>
          {row.surveyAnswer
            ? t('finance.penaltySurvey.answer.violation')
            : t('finance.penaltySurvey.answer.compliant')}
        </Badge>
      ),
    },
    {
      key: 'multiplier',
      header: t('finance.penaltySurvey.line.fields.multiplier'),
      align: 'end',
      render: (row) =>
        row.multiplierCalculate ? (formatQuantity(row.multiplier) ?? none) : none,
    },
    {
      key: 'penaltyAmount',
      header: t('finance.penaltySurvey.line.fields.amountWithCurrency'),
      align: 'end',
      render: (row) => (
        <MoneyCell
          value={row.penaltyAmount}
          color={row.surveyAnswer ? 'var(--kt-danger)' : undefined}
          bold={row.surveyAnswer}
        />
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '110px',
      render: (row) => (
        <RowActions
          editLabel={t('finance.penaltySurvey.line.actions.edit', { id: row.penaltyId })}
          deleteLabel={t('finance.penaltySurvey.line.actions.delete', { id: row.penaltyId })}
          onEdit={() => setEditingLine(row)}
          onDelete={() => setDeletingLine(row)}
        />
      ),
    },
  ]

  return (
    <>
      <Breadcrumb
        items={[{ label: t('finance.penalty.list.title'), to: '/penalties?tab=surveys' }]}
        current={header.companyTitle}
      />

      <PageTitle
        title={header.companyTitle}
        description={header.facilityName ?? undefined}
        action={
          <div className="d-flex gap-2">
            <Button variant="light" 
              onClick={() => setEditing(true)}
            >
              {t('common.edit')}
            </Button>
            <Button variant="primary" onClick={() => setAdding(true)}>
              {t('finance.penaltySurvey.line.create')}
            </Button>
          </div>
        }
      />

      <div className="row g-4">
        <div className="col-12 col-xl-5">
          <Card
            className="h-100"
          >
              <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
                {t('finance.penaltySurvey.detail.profileSection')}
              </h2>
              <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
                <Term label={t('finance.penaltySurvey.fields.hazardClass')}>
                  {t(`enums.hazardClass.${header.hazardClass}`)}
                </Term>
                <Term label={t('finance.penaltySurvey.fields.workerCount')}>
                  {formatNumber(header.workerCount) ?? none}
                </Term>
                <Term label={t('finance.penaltySurvey.fields.ssiRegistrationNumber')}>
                  {header.ssiRegistrationNumber || none}
                </Term>
                <Term label={t('finance.penaltySurvey.fields.facilityOwner')}>
                  {header.facilityOwner || none}
                </Term>
                <Term label={t('finance.penaltySurvey.fields.phone')}>
                  {header.phone || header.facilityOwnerGsm || none}
                </Term>
                <Term label={t('finance.penaltySurvey.fields.email')}>
                  {header.email || none}
                </Term>
                <Term label={t('finance.penaltySurvey.fields.address')}>
                  {header.address || none}
                </Term>
                <Term label={t('finance.penaltySurvey.fields.creationTime')}>
                  {formatDate(header.creationTime) ?? none}
                </Term>
              </dl>
            
          </Card>
        </div>

        <div className="col-12 col-xl-7">
          <Card
            className="h-100"
          >
              <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
                {t('finance.penaltySurvey.detail.exposureSection')}
              </h2>

              {total.error ? (
                <ErrorPanel message={errorMessage(total.error)} />
              ) : (
                <div className="row g-3">
                  <div className="col-sm-4">
                    <div className="px-4 py-3 rounded" style={{ backgroundColor: 'var(--kt-gray-100)' }}>
                      <div style={{ color: 'var(--kt-gray-600)', fontSize: '0.8125rem' }}>
                        {t('finance.penaltySurvey.detail.lineCount')}
                      </div>
                      <div className="fw-semibold" style={{ fontSize: '1.125rem' }}>
                        {formatNumber(total.data?.lineCount) ?? none}
                      </div>
                    </div>
                  </div>
                  <div className="col-sm-4">
                    <div
                      className="px-4 py-3 rounded"
                      style={{ backgroundColor: 'var(--kt-danger-light)' }}
                    >
                      <div style={{ color: 'var(--kt-gray-600)', fontSize: '0.8125rem' }}>
                        {t('finance.penaltySurvey.detail.violationCount')}
                      </div>
                      <div
                        className="fw-semibold"
                        style={{ color: 'var(--kt-danger)', fontSize: '1.125rem' }}
                      >
                        {formatNumber(total.data?.violationCount) ?? none}
                      </div>
                    </div>
                  </div>
                  <div className="col-sm-4">
                    <MoneyStat
                      label={t('finance.penaltySurvey.detail.totalAmount')}
                      value={total.data?.totalAmount}
                      currency={currency}
                      tone="warning"
                      emphasis
                    />
                  </div>
                </div>
              )}

              <p
                className="mt-4 mb-0"
                style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}
              >
                {t('finance.penaltySurvey.detail.exposureHint')}
              </p>
            
          </Card>
        </div>
      </div>

      <Card
        className="mt-4"
        header={
          <div className="d-flex flex-wrap align-items-center gap-2">
            <h2 className="h6 fw-semibold mb-0 me-auto" style={{ color: 'var(--kt-gray-900)' }}>
              {t('finance.penaltySurvey.detail.linesSection')}
            </h2>
            <FilterSelect
              id="survey-line-filter-answer"
              label={t('finance.penaltySurvey.line.fields.surveyAnswer')}
              value={answerFilter}
              onChange={(next) => {
                setAnswerFilter(next)
                setPage(1)
              }}
              width={200}
            >
              <option value="">{t('finance.penaltySurvey.filters.allAnswers')}</option>
              <option value="true">{t('finance.penaltySurvey.answer.violation')}</option>
              <option value="false">{t('finance.penaltySurvey.answer.compliant')}</option>
            </FilterSelect>
          </div>
        }
        footer={
          lines.data && lines.data.totalCount > 0 ? (
            <Pagination
              total={lines.data.totalCount}
              page={page}
              pageSize={PAGE_SIZE}
              onPageChange={setPage}
            />
          ) : undefined
        }
      >
        <DataTable
          label={t('finance.penaltySurvey.detail.linesSection')}
          columns={columns}
          rows={lines.data?.items}
          rowKey={(row) => row.id}
          isLoading={lines.isLoading}
          error={lines.error ? errorMessage(lines.error) : null}
          emptyMessage={t('finance.penaltySurvey.line.empty')}
        />
      </Card>

      {isEditing && <PenaltySurveyEditor surveyId={surveyId} onClose={() => setEditing(false)} />}

      {isAdding && (
        <PenaltySurveyLineForm
          isOpen
          onClose={() => setAdding(false)}
          onSubmit={(input) => addLine.mutate(input)}
          isBusy={addLine.isPending}
          error={addLine.error ? errorMessage(addLine.error) : null}
        />
      )}

      {editingLine && (
        <PenaltySurveyLineForm
          isOpen
          line={editingLine}
          onClose={() => setEditingLine(null)}
          onSubmit={(input) => updateLine.mutate({ lineId: editingLine.id, input })}
          isBusy={updateLine.isPending}
          error={updateLine.error ? errorMessage(updateLine.error) : null}
        />
      )}

      <ConfirmDialog
        isOpen={!!deletingLine}
        title={t('finance.penaltySurvey.line.delete.title')}
        message={t('finance.penaltySurvey.line.delete.message')}
        onCancel={() => setDeletingLine(null)}
        onConfirm={() => deletingLine && removeLine.mutate(deletingLine.id)}
        isBusy={removeLine.isPending}
        error={removeLine.error ? errorMessage(removeLine.error) : null}
      />
    </>
  )
}
