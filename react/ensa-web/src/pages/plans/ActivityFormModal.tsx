import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Field, Modal, controlClass } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { useLookup } from '@/api/endpoints'
import { ActivityType } from '@/api/enums'
import {
  ACTIVITY_TYPES,
  RESOURCES,
  usePeriodLookup,
  type ActivityDto,
  type SaveActivityDto,
} from './api'

interface ActivityFormModalProps {
  /** `undefined` creates a new catalogue entry. */
  activity?: ActivityDto
  /** Pre-selects the parent, so "add a child" lands in the right place in the tree. */
  parentActivityId?: number | null
  onClose: () => void
}

/**
 * Create/edit dialog of the activity catalogue.
 *
 * An activity may hang under a parent, which is what turns the catalogue into a tree: a heading
 * such as "Periodic inspections" with the individual inspections beneath it.
 */
export default function ActivityFormModal({
  activity,
  parentActivityId,
  onClose,
}: ActivityFormModalProps) {
  const { t } = useTranslation()
  const parents = useLookup(RESOURCES.activity)
  const periods = usePeriodLookup()
  const [nameError, setNameError] = useState<string | undefined>()
  const [model, setModel] = useState<SaveActivityDto>(() => ({
    activityName: activity?.activityName ?? '',
    activityCode: activity?.activityCode ?? '',
    parentActivityId: activity?.parentActivityId ?? parentActivityId ?? null,
    activityGroupId: activity?.activityGroupId ?? null,
    activityType: activity?.activityType ?? ActivityType.Activity,
    defaultActivity: activity?.defaultActivity ?? false,
    defaultCount: activity?.defaultCount ?? 0,
    defaultStartMonthOffset: activity?.defaultStartMonthOffset ?? 0,
    defaultElementCondition: activity?.defaultElementCondition ?? 0,
    periodId: activity?.periodId ?? null,
    orderNo: activity?.orderNo ?? null,
    isActive: activity?.isActive ?? true,
  }))

  const create = useCreate<SaveActivityDto, ActivityDto>(RESOURCES.activity, { onSuccess: onClose })
  const update = useUpdate<SaveActivityDto, ActivityDto>(RESOURCES.activity, { onSuccess: onClose })

  const isBusy = create.isPending || update.isPending
  const failure = create.error ?? update.error

  function submit() {
    if (!model.activityName.trim()) {
      setNameError(t('common.required'))
      return
    }
    setNameError(undefined)

    const input: SaveActivityDto = {
      ...model,
      activityName: model.activityName.trim(),
      activityCode: model.activityCode?.trim() || null,
    }
    if (activity) update.mutate({ id: activity.id, input })
    else create.mutate(input)
  }

  return (
    <Modal
      title={activity ? t('activity.form.editTitle') : t('activity.form.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={isBusy}
      error={failure ? errorMessage(failure) : null}
      size="lg"
    >
      <div className="row g-3">
        <Field
          label={t('activity.fields.activityName')}
          htmlFor="activity-name"
          required
          error={nameError}
          className="col-md-8"
        >
          <input
            id="activity-name"
            className={controlClass('form-control', nameError)}
            value={model.activityName}
            onChange={(event) => setModel({ ...model, activityName: event.target.value })}
          />
        </Field>

        <Field
          label={t('activity.fields.activityCode')}
          htmlFor="activity-code"
          className="col-md-4"
        >
          <input
            id="activity-code"
            className="form-control"
            value={model.activityCode ?? ''}
            onChange={(event) => setModel({ ...model, activityCode: event.target.value })}
          />
        </Field>

        <Field
          label={t('activity.fields.activityType')}
          htmlFor="activity-type"
          className="col-md-4"
        >
          <select
            id="activity-type"
            className="form-select"
            value={model.activityType}
            onChange={(event) =>
              setModel({ ...model, activityType: Number(event.target.value) as ActivityType })
            }
          >
            {ACTIVITY_TYPES.map((value) => (
              <option key={value} value={value}>
                {t(`enums.activityType.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('activity.fields.parentActivity')}
          htmlFor="activity-parent"
          hint={t('activity.form.parentHint')}
          className="col-md-4"
        >
          <select
            id="activity-parent"
            className="form-select"
            value={model.parentActivityId ?? ''}
            onChange={(event) =>
              setModel({
                ...model,
                parentActivityId: event.target.value === '' ? null : Number(event.target.value),
              })
            }
          >
            <option value="">{t('activity.form.noParent')}</option>
            {parents.data?.items
              .filter((item) => item.id !== activity?.id)
              .map((item) => (
                <option key={item.id} value={item.id}>
                  {item.displayName}
                </option>
              ))}
          </select>
        </Field>

        <Field label={t('activity.fields.period')} htmlFor="activity-period" className="col-md-4">
          <select
            id="activity-period"
            className="form-select"
            value={model.periodId ?? ''}
            onChange={(event) =>
              setModel({
                ...model,
                periodId: event.target.value === '' ? null : Number(event.target.value),
              })
            }
          >
            <option value="">{t('common.none')}</option>
            {periods.data?.items.map((period) => (
              <option key={period.id} value={period.id}>
                {period.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('activity.fields.defaultCount')}
          htmlFor="activity-default-count"
          hint={t('activity.form.defaultCountHint')}
          className="col-md-3"
        >
          <input
            id="activity-default-count"
            type="number"
            min={0}
            max={12}
            className="form-control"
            value={model.defaultCount}
            onChange={(event) => setModel({ ...model, defaultCount: Number(event.target.value) || 0 })}
          />
        </Field>

        <Field
          label={t('activity.fields.defaultStartMonthOffset')}
          htmlFor="activity-default-offset"
          hint={t('activity.form.defaultOffsetHint')}
          className="col-md-3"
        >
          <input
            id="activity-default-offset"
            type="number"
            min={0}
            max={11}
            className="form-control"
            value={model.defaultStartMonthOffset}
            onChange={(event) =>
              setModel({ ...model, defaultStartMonthOffset: Number(event.target.value) || 0 })
            }
          />
        </Field>

        <Field
          label={t('activity.fields.defaultElementCondition')}
          htmlFor="activity-default-condition"
          hint={t('activity.form.defaultConditionHint')}
          className="col-md-3"
        >
          <input
            id="activity-default-condition"
            type="number"
            min={0}
            className="form-control"
            value={model.defaultElementCondition}
            onChange={(event) =>
              setModel({ ...model, defaultElementCondition: Number(event.target.value) || 0 })
            }
          />
        </Field>

        <Field label={t('activity.fields.orderNo')} htmlFor="activity-order" className="col-md-3">
          <input
            id="activity-order"
            type="number"
            min={0}
            className="form-control"
            value={model.orderNo ?? ''}
            onChange={(event) =>
              setModel({
                ...model,
                orderNo: event.target.value === '' ? null : Number(event.target.value),
              })
            }
          />
        </Field>

        <div className="col-12 d-flex flex-wrap gap-4">
          <div className="form-check">
            <input
              id="activity-default"
              type="checkbox"
              className="form-check-input"
              checked={model.defaultActivity}
              onChange={(event) => setModel({ ...model, defaultActivity: event.target.checked })}
            />
            <label className="form-check-label" htmlFor="activity-default">
              {t('activity.fields.defaultActivity')}
            </label>
          </div>
          <div className="form-check">
            <input
              id="activity-active"
              type="checkbox"
              className="form-check-input"
              checked={model.isActive ?? true}
              onChange={(event) => setModel({ ...model, isActive: event.target.checked })}
            />
            <label className="form-check-label" htmlFor="activity-active">
              {t('common.active')}
            </label>
          </div>
        </div>
      </div>
    </Modal>
  )
}
