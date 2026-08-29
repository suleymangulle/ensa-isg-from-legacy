import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { CheckBox, Input, NumberInput, Select } from 'rich-react-component'
import { Modal } from '@/components/Form'
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
        <Input
          id="activity-name"
          label={t('activity.fields.activityName')}
          required
          error={nameError}
          className="col-md-8"
          value={model.activityName}
          onChange={(value) => setModel({ ...model, activityName: value })}
        />

        <Input
          id="activity-code"
          label={t('activity.fields.activityCode')}
          className="col-md-4"
          value={model.activityCode ?? ''}
          onChange={(value) => setModel({ ...model, activityCode: value })}
        />

        <Select
          id="activity-type"
          label={t('activity.fields.activityType')}
          className="col-md-4"
          options={ACTIVITY_TYPES.map((value) => ({ value, label: t(`enums.activityType.${value}`) }))}
          value={model.activityType}
          onChange={(value) => setModel({ ...model, activityType: value ?? ActivityType.Activity })}
        />

        <Select
          id="activity-parent"
          label={t('activity.fields.parentActivity')}
          helpText={t('activity.form.parentHint')}
          className="col-md-4"
          placeholder={t('activity.form.noParent')}
          options={
            parents.data?.items
              .filter((item) => item.id !== activity?.id)
              .map((item) => ({ value: item.id, label: item.displayName })) ?? []
          }
          value={model.parentActivityId ?? null}
          onChange={(value) => setModel({ ...model, parentActivityId: value })}
        />

        <Select
          id="activity-period"
          label={t('activity.fields.period')}
          className="col-md-4"
          placeholder={t('common.none')}
          options={periods.data?.items.map((period) => ({ value: period.id, label: period.displayName })) ?? []}
          value={model.periodId ?? null}
          onChange={(value) => setModel({ ...model, periodId: value })}
        />

        <NumberInput
          id="activity-default-count"
          label={t('activity.fields.defaultCount')}
          helpText={t('activity.form.defaultCountHint')}
          className="col-md-3"
          min={0}
          max={12}
          value={model.defaultCount}
          onChange={(value) => setModel({ ...model, defaultCount: value ?? 0 })}
        />

        <NumberInput
          id="activity-default-offset"
          label={t('activity.fields.defaultStartMonthOffset')}
          helpText={t('activity.form.defaultOffsetHint')}
          className="col-md-3"
          min={0}
          max={11}
          value={model.defaultStartMonthOffset}
          onChange={(value) => setModel({ ...model, defaultStartMonthOffset: value ?? 0 })}
        />

        <NumberInput
          id="activity-default-condition"
          label={t('activity.fields.defaultElementCondition')}
          helpText={t('activity.form.defaultConditionHint')}
          className="col-md-3"
          min={0}
          value={model.defaultElementCondition}
          onChange={(value) => setModel({ ...model, defaultElementCondition: value ?? 0 })}
        />

        <NumberInput
          id="activity-order"
          label={t('activity.fields.orderNo')}
          className="col-md-3"
          min={0}
          value={model.orderNo ?? null}
          onChange={(value) => setModel({ ...model, orderNo: value })}
        />

        <div className="col-12 d-flex flex-wrap gap-4">
          <CheckBox
            id="activity-default"
            checked={model.defaultActivity}
            onChange={(value) => setModel({ ...model, defaultActivity: value })}
            label={t('activity.fields.defaultActivity')}
          />
          <CheckBox
            id="activity-active"
            checked={model.isActive ?? true}
            onChange={(value) => setModel({ ...model, isActive: value })}
            label={t('common.active')}
          />
        </div>
      </div>
    </Modal>
  )
}
