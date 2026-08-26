import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ActivityReportLineType } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { Modal } from '@/components/Form'
import {
  useAddActivityReportLine,
  useUpdateActivityReportLine,
  type ActivityReportLineDto,
  type SaveActivityReportLineDto,
} from './api'
import { EnumField, TextField, enumValues } from './components'

interface FormState {
  lineType?: ActivityReportLineType
  text: string
  value1: string
  value2: string
  value3: string
  orderNo: string
}

function initialState(line?: ActivityReportLineDto): FormState {
  return {
    lineType: line?.lineType,
    text: line?.text ?? '',
    value1: line?.value1 ?? '',
    value2: line?.value2 ?? '',
    value3: line?.value3 ?? '',
    orderNo: line ? String(line.orderNo) : '0',
  }
}

/**
 * Create / edit dialog of one activity report data row.
 *
 * The row is typed: `lineType` says what the three value slots mean (visit count, trained
 * employees, incidents and so on), which is why the type is mandatory and the values are free
 * text — that is exactly the shape the contract defines.
 */
export default function ActivityReportLineFormModal({
  reportId,
  line,
  onClose,
}: {
  reportId: number
  /** Absent for a create. */
  line?: ActivityReportLineDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [form, setForm] = useState<FormState>(() => initialState(line))
  const [errors, setErrors] = useState<Record<string, string>>({})

  const add = useAddActivityReportLine(reportId)
  const update = useUpdateActivityReportLine(reportId)

  const pending = add.isPending || update.isPending
  const failure = add.error ?? update.error

  function patch(next: Partial<FormState>) {
    setForm((current) => ({ ...current, ...next }))
  }

  function submit() {
    const found: Record<string, string> = {}
    if (!form.lineType) found.lineType = t('validation.required')
    setErrors(found)
    if (Object.keys(found).length) return

    const payload: SaveActivityReportLineDto = {
      lineType: form.lineType!,
      text: form.text || null,
      value1: form.value1 || null,
      value2: form.value2 || null,
      value3: form.value3 || null,
      orderNo: form.orderNo === '' ? 0 : Number(form.orderNo),
    }

    if (line) {
      update.mutate({ lineId: line.id, input: payload }, { onSuccess: onClose })
    } else {
      add.mutate(payload, { onSuccess: onClose })
    }
  }

  return (
    <Modal
      title={
        line
          ? t('reports.activity.lineForm.editTitle')
          : t('reports.activity.lineForm.createTitle')
      }
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending}
      error={failure ? errorMessage(failure) : null}
      size="lg"
    >
      <div className="row g-3">
        <EnumField
          id="activity-line-type"
          className="col-md-8"
          label={t('reports.activity.fields.lineType')}
          values={enumValues(ActivityReportLineType)}
          translationPrefix="enums.activityReportLineType"
          placeholder={t('reports.activity.lineForm.selectType')}
          required
          error={errors.lineType}
          value={form.lineType}
          onChange={(next) => patch({ lineType: next as ActivityReportLineType | undefined })}
        />

        <TextField
          id="activity-line-order"
          className="col-md-4"
          type="number"
          min={0}
          label={t('reports.activity.fields.orderNo')}
          hint={t('reports.activity.lineForm.orderHint')}
          value={form.orderNo}
          onChange={(next) => patch({ orderNo: next })}
        />

        <TextField
          id="activity-line-text"
          className="col-12"
          rows={3}
          label={t('reports.activity.fields.text')}
          value={form.text}
          onChange={(next) => patch({ text: next })}
        />

        <TextField
          id="activity-line-value1"
          className="col-md-4"
          label={t('reports.activity.fields.value1')}
          value={form.value1}
          onChange={(next) => patch({ value1: next })}
        />
        <TextField
          id="activity-line-value2"
          className="col-md-4"
          label={t('reports.activity.fields.value2')}
          value={form.value2}
          onChange={(next) => patch({ value2: next })}
        />
        <TextField
          id="activity-line-value3"
          className="col-md-4"
          label={t('reports.activity.fields.value3')}
          value={form.value3}
          onChange={(next) => patch({ value3: next })}
        />
      </div>
    </Modal>
  )
}
