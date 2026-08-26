import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Field, Modal, controlClass } from '@/components/Form'
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
        <Field
          label={t('training.fields.trainingName')}
          htmlFor="training-name"
          required
          error={nameError}
          className="col-md-8"
        >
          <input
            id="training-name"
            className={controlClass('form-control', nameError)}
            value={model.trainingName}
            onChange={(event) => set('trainingName', event.target.value)}
          />
        </Field>

        <Field
          label={t('training.fields.trainingCode')}
          htmlFor="training-code"
          className="col-md-4"
        >
          <input
            id="training-code"
            className="form-control"
            value={model.trainingCode ?? ''}
            onChange={(event) => set('trainingCode', event.target.value)}
          />
        </Field>

        <Field
          label={t('training.fields.trainingType')}
          htmlFor="training-type"
          className="col-md-4"
        >
          <select
            id="training-type"
            className="form-select"
            value={model.trainingType}
            onChange={(event) => set('trainingType', Number(event.target.value) as TrainingType)}
          >
            {TRAINING_TYPES.map((value) => (
              <option key={value} value={value}>
                {t(`enums.trainingType.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('training.fields.topicGroup')} htmlFor="training-group" className="col-md-4">
          <select
            id="training-group"
            className="form-select"
            value={model.topicGroup}
            onChange={(event) =>
              set('topicGroup', Number(event.target.value) as TrainingSubjectGroup)
            }
          >
            {TRAINING_SUBJECT_GROUPS.map((value) => (
              <option key={value} value={value}>
                {t(`enums.trainingSubjectGroup.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('training.fields.ibysTrainingCode')}
          htmlFor="training-ibys"
          hint={t('training.form.ibysHint')}
          className="col-md-4"
        >
          <input
            id="training-ibys"
            type="number"
            className="form-control"
            value={model.ibysTrainingCode ?? ''}
            onChange={(event) =>
              set('ibysTrainingCode', event.target.value === '' ? null : Number(event.target.value))
            }
          />
        </Field>

        <div className="col-12">
          <h3 className="h6 fw-semibold mb-2" style={{ color: 'var(--kt-gray-900)' }}>
            {t('training.form.durations')}
          </h3>
          <p className="mb-2" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
            {t('training.form.durationsHint')}
          </p>
          <div className="row g-3">
            {model.durations.map((duration) => (
              <Field
                key={duration.hazardClass}
                label={t(`enums.hazardClass.${duration.hazardClass}`)}
                htmlFor={`training-duration-${duration.hazardClass}`}
                className="col-md-4"
              >
                <input
                  id={`training-duration-${duration.hazardClass}`}
                  type="number"
                  min={0}
                  max={100000}
                  className="form-control"
                  value={duration.durationMinutes}
                  onChange={(event) =>
                    setDuration(duration.hazardClass, Number(event.target.value) || 0)
                  }
                />
              </Field>
            ))}
          </div>
        </div>

        <div className="col-12">
          <h3 className="h6 fw-semibold mb-2" style={{ color: 'var(--kt-gray-900)' }}>
            {t('training.form.planning')}
          </h3>
          <div className="row g-3">
            <Field
              label={t('training.fields.defaultCount')}
              htmlFor="training-default-count"
              hint={t('training.form.defaultCountHint')}
              className="col-md-4"
            >
              <input
                id="training-default-count"
                type="number"
                min={0}
                max={12}
                className="form-control"
                value={model.defaultCount}
                onChange={(event) => set('defaultCount', Number(event.target.value) || 0)}
              />
            </Field>

            <Field
              label={t('training.fields.defaultStartMonthOffset')}
              htmlFor="training-default-offset"
              hint={t('training.form.defaultOffsetHint')}
              className="col-md-4"
            >
              <input
                id="training-default-offset"
                type="number"
                min={0}
                max={11}
                className="form-control"
                value={model.defaultStartMonthOffset}
                onChange={(event) => set('defaultStartMonthOffset', Number(event.target.value) || 0)}
              />
            </Field>

            <Field
              label={t('training.fields.defaultElementCondition')}
              htmlFor="training-default-condition"
              hint={t('training.form.defaultConditionHint')}
              className="col-md-4"
            >
              <input
                id="training-default-condition"
                type="number"
                min={0}
                className="form-control"
                value={model.defaultElementCondition}
                onChange={(event) => set('defaultElementCondition', Number(event.target.value) || 0)}
              />
            </Field>
          </div>
        </div>

        <div className="col-12 d-flex flex-wrap gap-4">
          <Checkbox
            id="training-included"
            label={t('training.fields.includedInDefaultPlan')}
            checked={model.includedInDefaultPlan}
            onChange={(value) => set('includedInDefaultPlan', value)}
          />
          <Checkbox
            id="training-default"
            label={t('training.fields.defaultTraining')}
            checked={model.defaultTraining}
            onChange={(value) => set('defaultTraining', value)}
          />
          <Checkbox
            id="training-mandatory"
            label={t('training.fields.mandatoryTraining')}
            checked={model.mandatoryTraining}
            onChange={(value) => set('mandatoryTraining', value)}
          />
          <Checkbox
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

/** Labelled checkbox; the label is bound to the input so it is clickable and announced. */
function Checkbox({
  id,
  label,
  checked,
  onChange,
}: {
  id: string
  label: string
  checked: boolean
  onChange: (next: boolean) => void
}) {
  return (
    <div className="form-check">
      <input
        id={id}
        type="checkbox"
        className="form-check-input"
        checked={checked}
        onChange={(event) => onChange(event.target.checked)}
      />
      <label className="form-check-label" htmlFor={id}>
        {label}
      </label>
    </div>
  )
}
