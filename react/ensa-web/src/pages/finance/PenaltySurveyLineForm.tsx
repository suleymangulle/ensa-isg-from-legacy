import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Input, NumberInput, Select } from 'rich-react-component'
import { Field, Modal, controlClass } from '@/components/Form'
import { usePenaltyList, type PenaltySurveyLineDto, type SavePenaltySurveyLineDto } from './api'

const ARTICLE_PAGE_SIZE = 50

/**
 * Answer dialog for one fine article inside a survey.
 *
 * The amount is deliberately absent from the form. `CreatePenaltySurveyLineDto` carries only the
 * article, the answer and the schedule year; the server resolves the figure from the fine matrix
 * using the survey's own hazard class and head count, so a client cannot inflate or deflate a
 * fine exposure.
 *
 * The article picker is fed by the paged catalogue rather than a lookup endpoint, because
 * `api/penalty` exposes no `/lookup` route. It searches server-side through `Filter` and shows
 * the first fifty matches, which keeps a catalogue of hundreds of articles usable without
 * downloading all of it.
 */
export default function PenaltySurveyLineForm({
  isOpen,
  line,
  onClose,
  onSubmit,
  isBusy,
  error,
}: {
  isOpen: boolean
  /** Present when editing an answer; absent when adding one. */
  line?: PenaltySurveyLineDto
  onClose: () => void
  onSubmit: (input: SavePenaltySurveyLineDto) => void
  isBusy?: boolean
  error?: string | null
}) {
  const { t } = useTranslation()
  const [articleSearch, setArticleSearch] = useState('')
  const [penaltyId, setPenaltyId] = useState<number | undefined>(line?.penaltyId)
  const [surveyAnswer, setSurveyAnswer] = useState(line?.surveyAnswer ?? true)
  const [year, setYear] = useState<number | null>(new Date().getFullYear())
  const [validation, setValidation] = useState<Record<string, string>>({})

  const articles = usePenaltyList({
    skipCount: 0,
    maxResultCount: ARTICLE_PAGE_SIZE,
    sorting: 'LawArticle ASC',
    filter: articleSearch || undefined,
    isActive: true,
  })

  function handleSubmit() {
    const errors: Record<string, string> = {}
    if (!penaltyId) errors.penaltyId = t('validation.required')

    const parsedYear = year !== null ? Math.round(year) : null
    if (parsedYear !== null && (parsedYear < 2000 || parsedYear > 2200)) {
      errors.year = t('finance.penalty.amount.yearRange')
    }

    setValidation(errors)
    if (Object.keys(errors).length) return

    onSubmit({
      penaltyId: penaltyId as number,
      surveyAnswer,
      year: parsedYear,
    })
  }

  return (
    <Modal
      title={
        line
          ? t('finance.penaltySurvey.line.editTitle')
          : t('finance.penaltySurvey.line.createTitle')
      }
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={handleSubmit}
      isBusy={isBusy}
      error={error}
      size="lg"
    >
      <div className="row g-4">
        <Input
          id="survey-line-search"
          label={t('finance.penaltySurvey.line.fields.articleSearch')}
          type="search"
          value={articleSearch}
          placeholder={t('finance.penaltySurvey.line.searchPlaceholder')}
          onChange={setArticleSearch}
        />

        <Field
          label={t('finance.penaltySurvey.line.fields.penalty')}
          htmlFor="survey-line-penalty"
          required
          error={validation.penaltyId}
          hint={t('finance.penaltySurvey.line.articleHint', { count: ARTICLE_PAGE_SIZE })}
        >
          <select
            id="survey-line-penalty"
            className={controlClass('form-select', validation.penaltyId)}
            value={penaltyId ?? ''}
            size={6}
            disabled={articles.isLoading}
            aria-invalid={validation.penaltyId ? true : undefined}
            onChange={(event) =>
              setPenaltyId(event.target.value ? Number(event.target.value) : undefined)
            }
          >
            <option value="">
              {articles.isLoading ? t('common.loading') : t('finance.penaltySurvey.line.selectArticle')}
            </option>
            {articles.data?.items.map((item) => (
              <option key={item.id} value={item.id}>
                {[item.treeNodeCode, item.lawArticle, item.penaltyArticle]
                  .filter(Boolean)
                  .join(' — ')}
              </option>
            ))}
          </select>
        </Field>

        <Select<string>
          id="survey-line-answer"
          label={t('finance.penaltySurvey.line.fields.surveyAnswer')}
          required
          className="col-md-6"
          options={[
            { value: 'true', label: t('finance.penaltySurvey.answer.violation') },
            { value: 'false', label: t('finance.penaltySurvey.answer.compliant') },
          ]}
          value={surveyAnswer ? 'true' : 'false'}
          onChange={(next) => setSurveyAnswer(next === 'true')}
        />

        <NumberInput
          id="survey-line-year"
          label={t('finance.penaltySurvey.line.fields.year')}
          error={validation.year}
          helpText={t('finance.penaltySurvey.line.yearHint')}
          className="col-md-6"
          step={1}
          min={2000}
          max={2200}
          value={year}
          onChange={setYear}
        />
      </div>

      <p className="mt-4 mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
        {t('finance.penaltySurvey.line.serverResolvesHint')}
      </p>
    </Modal>
  )
}
