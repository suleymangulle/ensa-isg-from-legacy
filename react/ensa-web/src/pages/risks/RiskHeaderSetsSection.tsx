import { useEffect, useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button, CheckBox, Spinner } from 'rich-react-component'
import { ErrorPanel } from '@/components/DataTable'
import { ExistingControlMeasure, ExposedPersonGroup, ImprovementAction } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useSaveHeaderSet, type HeaderSetName, type RiskAssessmentReportNavigationDto } from './api'
import { enumValues } from './helpers'

/**
 * The three checkbox groups of the report header: who is exposed, what already protects them,
 * and what should be improved. Each group is replaced wholesale by its own `PUT`, so they are
 * saved independently rather than as one large form.
 */
export default function RiskHeaderSetsSection({
  reportId,
  detail,
}: {
  reportId: number
  detail: RiskAssessmentReportNavigationDto
}) {
  const { t } = useTranslation()

  return (
    <div className="d-flex flex-column gap-5">
      <EnumSet
        reportId={reportId}
        set="exposed-groups"
        title={t('riskAssessment.sets.exposedGroups')}
        description={t('riskAssessment.sets.exposedGroupsDescription')}
        enumName="exposedPersonGroup"
        options={enumValues(ExposedPersonGroup)}
        selected={detail.exposedGroups.map((item) => item.group)}
      />

      <EnumSet
        reportId={reportId}
        set="existing-control-measures"
        title={t('riskAssessment.sets.existingControlMeasures')}
        description={t('riskAssessment.sets.existingControlMeasuresDescription')}
        enumName="existingControlMeasure"
        options={enumValues(ExistingControlMeasure)}
        selected={detail.controlMeasures.map((item) => item.measure)}
      />

      <EnumSet
        reportId={reportId}
        set="improvement-actions"
        title={t('riskAssessment.sets.improvementActions')}
        description={t('riskAssessment.sets.improvementActionsDescription')}
        enumName="improvementAction"
        options={enumValues(ImprovementAction)}
        selected={detail.improvementActions.map((item) => item.recommendation)}
      />
    </div>
  )
}

function EnumSet({
  reportId,
  set,
  title,
  description,
  enumName,
  options,
  selected,
}: {
  reportId: number
  set: HeaderSetName
  title: string
  description: string
  /** Locale sub-section under `enums`, keyed by the numeric value. */
  enumName: string
  options: number[]
  selected: number[]
}) {
  const { t } = useTranslation()
  const groupId = useId()
  const [values, setValues] = useState<number[]>(selected)

  const save = useSaveHeaderSet(reportId, set)

  // Adopt the server state whenever the detail query returns a different selection.
  const serverKey = [...selected].sort((left, right) => left - right).join(',')
  // `serverKey` is the stable identity of `selected`; the array itself is a new object per render,
  // so depending on it directly would reset the checkboxes on every render.
  useEffect(() => {
    setValues(serverKey ? serverKey.split(',').map(Number) : [])
  }, [serverKey])

  const currentKey = [...values].sort((left, right) => left - right).join(',')
  const isDirty = currentKey !== serverKey

  function toggle(value: number) {
    setValues((current) =>
      current.includes(value) ? current.filter((item) => item !== value) : [...current, value],
    )
  }

  return (
    <fieldset className="border-0 p-0 m-0">
      <legend className="h6 fw-semibold mb-1" style={{ color: 'var(--kt-gray-900)' }}>
        {title}
      </legend>
      <p className="mb-3" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
        {description}
      </p>

      {save.error && <ErrorPanel message={errorMessage(save.error)} />}

      <div className="row g-2">
        {options.map((value) => {
          const inputId = `${groupId}-${value}`
          return (
            <div className="col-md-6 col-lg-4" key={value}>
              <CheckBox
                id={inputId}
                checked={values.includes(value)}
                onChange={() => toggle(value)}
                label={t(`enums.${enumName}.${value}`)}
              />
            </div>
          )
        })}
      </div>

      <div className="d-flex align-items-center gap-3 mt-3">
        <Button variant="primary" size="sm"
          disabled={!isDirty || save.isPending}
          onClick={() => save.mutate(values)}
        >
          {save.isPending && <Spinner label={t('common.loading')} size="sm" className="me-2" />}
          {t('common.save')}
        </Button>
        {isDirty && (
          <span style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
            {t('riskAssessment.sets.unsaved')}
          </span>
        )}
      </div>
    </fieldset>
  )
}
