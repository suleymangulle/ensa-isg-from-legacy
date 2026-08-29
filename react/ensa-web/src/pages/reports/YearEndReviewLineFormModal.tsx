import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Select, Switch } from 'rich-react-component'
import { errorMessage } from '@/api/http'
import { Modal } from '@/components/Form'
import {
  useAddYearEndReviewLine,
  useUpdateYearEndReviewLine,
  type SaveYearEndReviewLineDto,
  type YearEndReviewLineDto,
} from './api'
import { TextField, toDateInput } from './components'

/** One entry of the parent drop-down: the work item plus its depth in the tree. */
export interface ParentOption {
  id: number
  label: string
  depth: number
}

interface FormState {
  parentLineId?: number
  orderNo: string
  date: string
  work: string
  personVeTitle: string
  repeatCount: string
  usedMethod: string
  resultVeComment: string
  isActive: boolean
}

function initialState(line?: YearEndReviewLineDto, defaultParentId?: number): FormState {
  return {
    parentLineId: line ? (line.parentLineId ?? undefined) : defaultParentId,
    orderNo: line ? String(line.orderNo) : '0',
    date: toDateInput(line?.date),
    work: line?.work ?? '',
    personVeTitle: line?.personVeTitle ?? '',
    repeatCount: line?.repeatCount ?? '',
    usedMethod: line?.usedMethod ?? '',
    resultVeComment: line?.resultVeComment ?? '',
    isActive: line?.isActive ?? true,
  }
}

/**
 * Create / edit dialog of one year-end review work item.
 *
 * Work items form a tree, so the dialog offers a parent. The item being edited is kept out of
 * its own parent list; deeper cycles are rejected by the API, whose message is surfaced through
 * `errorMessage()` rather than guessed at here.
 */
export default function YearEndReviewLineFormModal({
  reportId,
  line,
  defaultParentId,
  parents,
  onClose,
}: {
  reportId: number
  /** Absent for a create. */
  line?: YearEndReviewLineDto
  /** Pre-selected parent when the dialog was opened from a "add child" button. */
  defaultParentId?: number
  parents: ParentOption[]
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [form, setForm] = useState<FormState>(() => initialState(line, defaultParentId))
  const [errors, setErrors] = useState<Record<string, string>>({})

  const add = useAddYearEndReviewLine(reportId)
  const update = useUpdateYearEndReviewLine(reportId)

  const pending = add.isPending || update.isPending
  const failure = add.error ?? update.error

  function patch(next: Partial<FormState>) {
    setForm((current) => ({ ...current, ...next }))
  }

  function submit() {
    const found: Record<string, string> = {}
    if (!form.work.trim()) found.work = t('validation.required')
    setErrors(found)
    if (Object.keys(found).length) return

    const payload: SaveYearEndReviewLineDto = {
      parentLineId: form.parentLineId ?? null,
      orderNo: form.orderNo === '' ? 0 : Number(form.orderNo),
      date: form.date || null,
      work: form.work.trim(),
      personVeTitle: form.personVeTitle || null,
      repeatCount: form.repeatCount || null,
      usedMethod: form.usedMethod || null,
      resultVeComment: form.resultVeComment || null,
    }

    if (line) {
      update.mutate(
        { lineId: line.id, input: { ...payload, isActive: form.isActive } },
        { onSuccess: onClose },
      )
    } else {
      add.mutate(payload, { onSuccess: onClose })
    }
  }

  const selectableParents = parents.filter((parent) => parent.id !== line?.id)

  return (
    <Modal
      title={
        line ? t('reports.yearEnd.lineForm.editTitle') : t('reports.yearEnd.lineForm.createTitle')
      }
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending}
      error={failure ? errorMessage(failure) : null}
      size="lg"
    >
      <div className="row g-3">
        <Select<number>
          id="year-end-line-parent"
          className="col-md-8"
          label={t('reports.yearEnd.fields.parentLine')}
          helpText={t('reports.yearEnd.lineForm.parentHint')}
          placeholder={t('reports.yearEnd.lineForm.noParent')}
          options={selectableParents.map((parent) => ({
            value: parent.id,
            label: `${'— '.repeat(parent.depth)}${parent.label}`,
          }))}
          value={form.parentLineId ?? null}
          onChange={(next) => patch({ parentLineId: next ?? undefined })}
        />

        <TextField
          id="year-end-line-order"
          className="col-md-4"
          type="number"
          min={0}
          label={t('reports.yearEnd.fields.orderNo')}
          hint={t('reports.yearEnd.lineForm.orderHint')}
          value={form.orderNo}
          onChange={(next) => patch({ orderNo: next })}
        />

        <TextField
          id="year-end-line-work"
          className="col-12"
          rows={2}
          label={t('reports.yearEnd.fields.work')}
          required
          error={errors.work}
          value={form.work}
          onChange={(next) => patch({ work: next })}
        />

        <TextField
          id="year-end-line-date"
          className="col-md-4"
          type="date"
          label={t('reports.yearEnd.fields.date')}
          value={form.date}
          onChange={(next) => patch({ date: next })}
        />
        <TextField
          id="year-end-line-person"
          className="col-md-4"
          label={t('reports.yearEnd.fields.personVeTitle')}
          value={form.personVeTitle}
          onChange={(next) => patch({ personVeTitle: next })}
        />
        <TextField
          id="year-end-line-repeat"
          className="col-md-4"
          label={t('reports.yearEnd.fields.repeatCount')}
          value={form.repeatCount}
          onChange={(next) => patch({ repeatCount: next })}
        />

        <TextField
          id="year-end-line-method"
          className="col-12"
          rows={2}
          label={t('reports.yearEnd.fields.usedMethod')}
          value={form.usedMethod}
          onChange={(next) => patch({ usedMethod: next })}
        />
        <TextField
          id="year-end-line-result"
          className="col-12"
          rows={2}
          label={t('reports.yearEnd.fields.resultVeComment')}
          value={form.resultVeComment}
          onChange={(next) => patch({ resultVeComment: next })}
        />

        {line && (
          <div className="col-md-4 mb-3">
            <label htmlFor="year-end-line-active" className="form-label">
              {t('reports.yearEnd.fields.status')}
            </label>
            <Switch
              id="year-end-line-active"
              className="mt-2"
              checked={form.isActive}
              onChange={(checked) => patch({ isActive: checked })}
              label={form.isActive ? t('common.active') : t('common.passive')}
            />
          </div>
        )}
      </div>
    </Modal>
  )
}
