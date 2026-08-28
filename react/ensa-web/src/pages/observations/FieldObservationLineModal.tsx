import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Input, TextArea } from 'rich-react-component'
import { RiskCategory } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { Modal } from '@/components/Form'
import {
  useAddObservationLine,
  useEmployeeLookup,
  useUpdateObservationLine,
  type FieldObservationLineDto,
  type SaveFieldObservationLineDto,
} from './api'
import { EnumField, LookupField, enumValues, fromDateInput, toDateInput } from './components'

interface FormState {
  date: string
  deadlineDate: string
  nonConformity: string
  measures: string
  owner: string
  ownerCompanyEmployeeId?: number
  riskCategory: RiskCategory
}

function initialState(line?: FieldObservationLineDto): FormState {
  return {
    date: toDateInput(line?.date),
    deadlineDate: toDateInput(line?.deadlineDate),
    nonConformity: line?.nonConformity ?? '',
    measures: line?.measures ?? '',
    owner: line?.owner ?? '',
    ownerCompanyEmployeeId: line?.ownerCompanyEmployeeId ?? undefined,
    riskCategory: line?.riskCategory ?? RiskCategory.Unspecified,
  }
}

/** Create / edit dialog of one non-conformity line of a field observation report. */
export default function FieldObservationLineModal({
  reportId,
  companyId,
  line,
  onClose,
}: {
  reportId: number
  /** Company of the report, used to scope the responsible-employee drop-down. */
  companyId: number
  /** Absent when a new line is added. */
  line?: FieldObservationLineDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [form, setForm] = useState<FormState>(() => initialState(line))
  const [errors, setErrors] = useState<Record<string, string>>({})

  const employees = useEmployeeLookup(companyId)
  const add = useAddObservationLine(reportId, onClose)
  const update = useUpdateObservationLine(reportId, onClose)

  const pending = add.isPending || update.isPending
  const failure = add.error ?? update.error

  function patch(next: Partial<FormState>) {
    setForm((current) => ({ ...current, ...next }))
  }

  function submit() {
    const found: Record<string, string> = {}
    if (!form.nonConformity.trim()) found.nonConformity = t('validation.required')
    setErrors(found)
    if (Object.keys(found).length) return

    const payload: SaveFieldObservationLineDto = {
      date: fromDateInput(form.date),
      deadlineDate: fromDateInput(form.deadlineDate),
      nonConformity: form.nonConformity.trim(),
      measures: form.measures || null,
      owner: form.owner || null,
      ownerCompanyEmployeeId: form.ownerCompanyEmployeeId ?? null,
      riskCategory: form.riskCategory,
    }

    if (line) update.mutate({ lineId: line.id, input: payload })
    else add.mutate(payload)
  }

  return (
    <Modal
      title={line ? t('fieldObservation.lines.editTitle') : t('fieldObservation.lines.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending}
      error={failure ? errorMessage(failure) : null}
      size="lg"
    >
      <div className="row g-3">
        <TextArea
          id="line-non-conformity"
          label={t('fieldObservation.lines.nonConformity')}
          required
          error={errors.nonConformity}
          className="col-12"
          rows={3}
          value={form.nonConformity}
          onChange={(value) => patch({ nonConformity: value })}
        />

        <TextArea
          id="line-measures"
          label={t('fieldObservation.lines.measures')}
          className="col-12"
          rows={3}
          value={form.measures}
          onChange={(value) => patch({ measures: value })}
        />

        <LookupField
          id="line-owner-employee"
          className="col-md-6"
          label={t('fieldObservation.lines.ownerEmployee')}
          placeholder={t('observations.selectEmployee')}
          items={employees.data?.items}
          isLoading={employees.isLoading}
          value={form.ownerCompanyEmployeeId}
          onChange={(next) => patch({ ownerCompanyEmployeeId: next })}
        />

        <Input
          id="line-owner"
          label={t('fieldObservation.lines.owner')}
          helpText={t('fieldObservation.lines.ownerHint')}
          className="col-md-6"
          value={form.owner}
          onChange={(value) => patch({ owner: value })}
        />

        <EnumField
          id="line-risk-category"
          className="col-md-4"
          label={t('fieldObservation.lines.riskCategory')}
          translationPrefix="enums.riskCategory"
          values={enumValues(RiskCategory)}
          value={form.riskCategory}
          onChange={(next) =>
            patch({ riskCategory: (next ?? RiskCategory.Unspecified) as RiskCategory })
          }
        />

        <Input
          id="line-date"
          label={t('fieldObservation.lines.date')}
          className="col-md-4"
          value={form.date}
          onChange={(value) => patch({ date: value })}
          inputProps={{ type: 'date' }}
        />

        <Input
          id="line-deadline"
          label={t('fieldObservation.lines.deadlineDate')}
          className="col-md-4"
          value={form.deadlineDate}
          onChange={(value) => patch({ deadlineDate: value })}
          inputProps={{ type: 'date' }}
        />
      </div>
    </Modal>
  )
}
