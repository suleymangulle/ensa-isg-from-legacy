import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { CheckBox, Input, NumberInput, Select } from 'rich-react-component'
import { Modal } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { HazardClass, TrainingSubjectGroup, TrainingType } from '@/api/enums'
import {
  HAZARD_CLASSES,
  RESOURCES,
  TRAINING_SUBJECT_GROUPS,
  TRAINING_TYPES,
  type SaveTrainingDto,
  type TrainingDto,
  type TrainingDurationDto,
} from './api'

/** An empty catalogue entry with one duration row per hazard class. */
function blankTraining(): SaveTrainingDto {
  return {
    trainingName: '',
    trainingCode: '',
    trainingType: TrainingType.BasicTraining,
    topicGroup: TrainingSubjectGroup.GeneralSubjects,
    mandatoryTraining: false,
    ibysTrainingCode: null,
    includedInDefaultPlan: false,
    defaultTraining: false,
    defaultCount: 0,
    defaultStartMonthOffset: 0,
    defaultElementCondition: 0,
    durations: HAZARD_CLASSES.map((hazardClass) => ({ hazardClass, durationMinutes: 0 })),
    isActive: true,
  }
}

/** Maps an existing entry onto the form model, filling in any missing duration row. */
function toFormModel(training: TrainingDto): SaveTrainingDto {
  const durations: TrainingDurationDto[] = HAZARD_CLASSES.map((hazardClass) => ({
    hazardClass,
    durationMinutes:
      training.durations.find((item) => item.hazardClass === hazardClass)?.durationMinutes ?? 0,
  }))

  return {
    trainingName: training.trainingName,
    trainingCode: training.trainingCode ?? '',
    trainingGroupId: training.trainingGroupId,
    trainingType: training.trainingType,
    topicGroup: training.topicGroup,
    mandatoryTraining: training.mandatoryTraining,
    ibysTrainingCode: training.ibysTrainingCode ?? null,
    includedInDefaultPlan: training.includedInDefaultPlan,
    defaultTraining: training.defaultTraining,
    defaultCount: training.defaultCount,
    defaultStartMonthOffset: training.defaultStartMonthOffset,
    defaultElementCondition: training.defaultElementCondition,
    durations,
    isActive: training.isActive,
  }
}

interface TrainingFormModalProps {
  isOpen: boolean
  /** `undefined` creates a new catalogue entry. */
  training?: TrainingDto
  onClose: () => void
  onSaved?: (training: TrainingDto) => void
}

/**
 * Create/edit dialog of the training catalogue.
 *
 * Durations are edited as one row per hazard class because that is how the API stores them —
 * the legacy screen's three flat "az/orta/çok tehlikeli süre" boxes became a child collection.
 */
export default function TrainingFormModal({
  isOpen,
  training,
  onClose,
  onSaved,
}: TrainingFormModalProps) {
  const { t } = useTranslation()
  const [model, setModel] = useState<SaveTrainingDto>(() =>
    training ? toFormModel(training) : blankTraining(),
  )
  const [nameError, setNameError] = useState<string | undefined>()

  const create = useCreate<SaveTrainingDto, TrainingDto>(RESOURCES.training, {
    onSuccess: (saved) => {
      onSaved?.(saved)
      onClose()
    },
  })
  const update = useUpdate<SaveTrainingDto, TrainingDto>(RESOURCES.training, {
    onSuccess: (saved) => {
      onSaved?.(saved)
      onClose()
    },
  })

  const isBusy = create.isPending || update.isPending
  const failure = create.error ?? update.error

  function set<K extends keyof SaveTrainingDto>(key: K, value: SaveTrainingDto[K]) {
    setModel((current) => ({ ...current, [key]: value }))
  }

  function setDuration(hazardClass: HazardClass, minutes: number) {
    setModel((current) => ({
      ...current,
      durations: current.durations.map((item) =>
        item.hazardClass === hazardClass ? { ...item, durationMinutes: minutes } : item,
      ),
    }))
  }

  function submit() {
    if (!model.trainingName.trim()) {
      setNameError(t('common.required'))
      return
    }
    setNameError(undefined)

    const input: SaveTrainingDto = {
      ...model,
      trainingName: model.trainingName.trim(),
      trainingCode: model.trainingCode?.trim() || null,
    }

    if (training) update.mutate({ id: training.id, input })
    else create.mutate(input)
  }

  return (
    <Modal
      title={training ? t('training.form.editTitle') : t('training.form.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={submit}
      isBusy={isBusy}
      error={failure ? errorMessage(failure) : null}
      size="lg"
    >
      <div className="row g-3">
        <Input
          id="training-name"
          label={t('training.fields.trainingName')}
          required
          error={nameError}
          className="col-md-8"
          value={model.trainingName}
          onChange={(value) => set('trainingName', value)}
        />

        <Input
          id="training-code"
          label={t('training.fields.trainingCode')}
          className="col-md-4"
          value={model.trainingCode ?? ''}
          onChange={(value) => set('trainingCode', value)}
        />

        <Select
          id="training-type"
          label={t('training.fields.trainingType')}
          className="col-md-4"
          options={TRAINING_TYPES.map((value) => ({
            value,
            label: t(`enums.trainingType.${value}`),
          }))}
          value={model.trainingType}
          onChange={(value) => value !== null && set('trainingType', value)}
        />

        <Select
          id="training-group"
          label={t('training.fields.topicGroup')}
          className="col-md-4"
          options={TRAINING_SUBJECT_GROUPS.map((value) => ({
            value,
            label: t(`enums.trainingSubjectGroup.${value}`),
          }))}
          value={model.topicGroup}
          onChange={(value) => value !== null && set('topicGroup', value)}
        />

        <NumberInput
          id="training-ibys"
          label={t('training.fields.ibysTrainingCode')}
          helpText={t('training.form.ibysHint')}
          className="col-md-4"
          value={model.ibysTrainingCode ?? null}
          onChange={(value) => set('ibysTrainingCode', value)}
        />

        <div className="col-12">
          <h3 className="h6 fw-semibold mb-2" style={{ color: 'var(--kt-gray-900)' }}>
            {t('training.form.durations')}
          </h3>
          <p className="mb-2" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
            {t('training.form.durationsHint')}
          </p>
          <div className="row g-3">
            {model.durations.map((duration) => (
              <NumberInput
                key={duration.hazardClass}
                id={`training-duration-${duration.hazardClass}`}
                label={t(`enums.hazardClass.${duration.hazardClass}`)}
                className="col-md-4"
                min={0}
                max={100000}
                value={duration.durationMinutes}
                onChange={(value) => setDuration(duration.hazardClass, value ?? 0)}
              />
            ))}
          </div>
        </div>

        <div className="col-12">
          <h3 className="h6 fw-semibold mb-2" style={{ color: 'var(--kt-gray-900)' }}>
            {t('training.form.planning')}
          </h3>
          <div className="row g-3">
            <NumberInput
              id="training-default-count"
              label={t('training.fields.defaultCount')}
              helpText={t('training.form.defaultCountHint')}
              className="col-md-4"
              min={0}
              max={12}
              value={model.defaultCount}
              onChange={(value) => set('defaultCount', value ?? 0)}
            />

            <NumberInput
              id="training-default-offset"
              label={t('training.fields.defaultStartMonthOffset')}
              helpText={t('training.form.defaultOffsetHint')}
              className="col-md-4"
              min={0}
              max={11}
              value={model.defaultStartMonthOffset}
              onChange={(value) => set('defaultStartMonthOffset', value ?? 0)}
            />

            <NumberInput
              id="training-default-condition"
              label={t('training.fields.defaultElementCondition')}
              helpText={t('training.form.defaultConditionHint')}
              className="col-md-4"
              min={0}
              value={model.defaultElementCondition}
              onChange={(value) => set('defaultElementCondition', value ?? 0)}
            />
          </div>
        </div>

        <div className="col-12 d-flex flex-wrap gap-4">
          <CheckBox
            id="training-included"
            label={t('training.fields.includedInDefaultPlan')}
            checked={model.includedInDefaultPlan}
            onChange={(value) => set('includedInDefaultPlan', value)}
          />
          <CheckBox
            id="training-default"
            label={t('training.fields.defaultTraining')}
            checked={model.defaultTraining}
            onChange={(value) => set('defaultTraining', value)}
          />
          <CheckBox
            id="training-mandatory"
            label={t('training.fields.mandatoryTraining')}
            checked={model.mandatoryTraining}
            onChange={(value) => set('mandatoryTraining', value)}
          />
          <CheckBox
            id="training-active"
            label={t('common.active')}
            checked={model.isActive ?? true}
            onChange={(value) => set('isActive', value)}
          />
        </div>
      </div>
    </Modal>
  )
}
