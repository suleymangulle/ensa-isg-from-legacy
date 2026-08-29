import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Input, NumberInput, TextArea } from 'rich-react-component'
import { HazardClass } from '@/api/enums'
import { Modal } from '@/components/Form'
import type { PenaltySurveyDto, SavePenaltySurveyDto } from './api'
import { EnumField, enumValues } from './components'

interface SurveyFormState {
  companyTitle: string
  facilityName: string
  facilityOwner: string
  facilityOwnerDuty: string
  facilityOwnerGsm: string
  employerNameLastName: string
  phone: string
  email: string
  address: string
  taxTaxOffice: string
  taxNumber: string
  ssiRegistrationNumber: string
  workerCount: number | null
  hazardClass: HazardClass
}

function initialState(survey?: PenaltySurveyDto): SurveyFormState {
  return {
    companyTitle: survey?.companyTitle ?? '',
    facilityName: survey?.facilityName ?? '',
    facilityOwner: survey?.facilityOwner ?? '',
    facilityOwnerDuty: survey?.facilityOwnerDuty ?? '',
    facilityOwnerGsm: survey?.facilityOwnerGsm ?? '',
    employerNameLastName: survey?.employerNameLastName ?? '',
    phone: survey?.phone ?? '',
    email: survey?.email ?? '',
    address: survey?.address ?? '',
    taxTaxOffice: survey?.taxTaxOffice ?? '',
    taxNumber: survey?.taxNumber ?? '',
    ssiRegistrationNumber: survey?.ssiRegistrationNumber ?? '',
    workerCount: survey?.workerCount ?? null,
    hazardClass: survey?.hazardClass ?? HazardClass.Unspecified,
  }
}

/**
 * Create / edit dialog for a fine-risk survey header.
 *
 * The hazard class and the head count are not decoration: the server uses exactly these two
 * fields, plus the schedule year, to resolve each answered article against the fine matrix. A
 * wrong head count therefore changes every amount on the survey.
 *
 * The city / district / neighbourhood fields of `PenaltySurveyDto` are left out: they are
 * optional, and the address is captured as free text here rather than duplicating the cascading
 * province picker that belongs to the shared reference module.
 */
export default function PenaltySurveyForm({
  isOpen,
  survey,
  onClose,
  onSubmit,
  isBusy,
  error,
}: {
  isOpen: boolean
  /** Present when editing; absent when creating. */
  survey?: PenaltySurveyDto
  onClose: () => void
  onSubmit: (input: SavePenaltySurveyDto) => void
  isBusy?: boolean
  error?: string | null
}) {
  const { t } = useTranslation()
  const [form, setForm] = useState<SurveyFormState>(() => initialState(survey))
  const [validation, setValidation] = useState<Record<string, string>>({})

  function patch(changes: Partial<SurveyFormState>) {
    setForm((current) => ({ ...current, ...changes }))
  }

  function handleSubmit() {
    const errors: Record<string, string> = {}
    if (!form.companyTitle.trim()) errors.companyTitle = t('validation.required')

    const workerCount = form.workerCount !== null ? Math.round(form.workerCount) : null
    if (workerCount !== null && (workerCount < 0 || workerCount > 1_000_000)) {
      errors.workerCount = t('finance.penaltySurvey.form.workerCountRange')
    }

    setValidation(errors)
    if (Object.keys(errors).length) return

    onSubmit({
      companyTitle: form.companyTitle.trim(),
      facilityName: form.facilityName.trim() || null,
      facilityOwner: form.facilityOwner.trim() || null,
      facilityOwnerDuty: form.facilityOwnerDuty.trim() || null,
      facilityOwnerGsm: form.facilityOwnerGsm.trim() || null,
      employerNameLastName: form.employerNameLastName.trim() || null,
      phone: form.phone.trim() || null,
      email: form.email.trim() || null,
      address: form.address.trim() || null,
      taxTaxOffice: form.taxTaxOffice.trim() || null,
      taxNumber: form.taxNumber.trim() || null,
      ssiRegistrationNumber: form.ssiRegistrationNumber.trim() || null,
      workerCount,
      hazardClass: form.hazardClass,
    })
  }

  return (
    <Modal
      title={
        survey
          ? t('finance.penaltySurvey.form.editTitle')
          : t('finance.penaltySurvey.form.createTitle')
      }
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={handleSubmit}
      isBusy={isBusy}
      error={error}
      size="xl"
    >
      <div className="row g-4">
        <Input
          id="survey-company-title"
          label={t('finance.penaltySurvey.fields.companyTitle')}
          required
          error={validation.companyTitle}
          className="col-md-6"
          value={form.companyTitle}
          onChange={(value) => patch({ companyTitle: value })}
        />

        <Input
          id="survey-facility-name"
          label={t('finance.penaltySurvey.fields.facilityName')}
          className="col-md-6"
          value={form.facilityName}
          onChange={(value) => patch({ facilityName: value })}
        />

        <EnumField
          id="survey-hazard-class"
          label={t('finance.penaltySurvey.fields.hazardClass')}
          value={form.hazardClass}
          onChange={(next) =>
            patch({ hazardClass: (next ?? HazardClass.Unspecified) as HazardClass })
          }
          values={enumValues(HazardClass)}
          translationPrefix="enums.hazardClass"
          required
          className="col-md-4"
        />

        <NumberInput
          id="survey-worker-count"
          label={t('finance.penaltySurvey.fields.workerCount')}
          error={validation.workerCount}
          helpText={t('finance.penaltySurvey.form.workerCountHint')}
          className="col-md-4"
          step={1}
          min={0}
          value={form.workerCount}
          onChange={(value) => patch({ workerCount: value })}
        />

        <Input
          id="survey-ssi"
          label={t('finance.penaltySurvey.fields.ssiRegistrationNumber')}
          className="col-md-4"
          value={form.ssiRegistrationNumber}
          onChange={(value) => patch({ ssiRegistrationNumber: value })}
        />

        <Input
          id="survey-owner"
          label={t('finance.penaltySurvey.fields.facilityOwner')}
          className="col-md-4"
          value={form.facilityOwner}
          onChange={(value) => patch({ facilityOwner: value })}
        />

        <Input
          id="survey-owner-duty"
          label={t('finance.penaltySurvey.fields.facilityOwnerDuty')}
          className="col-md-4"
          value={form.facilityOwnerDuty}
          onChange={(value) => patch({ facilityOwnerDuty: value })}
        />

        <Input
          id="survey-owner-gsm"
          label={t('finance.penaltySurvey.fields.facilityOwnerGsm')}
          type="tel"
          className="col-md-4"
          value={form.facilityOwnerGsm}
          onChange={(value) => patch({ facilityOwnerGsm: value })}
        />

        <Input
          id="survey-employer"
          label={t('finance.penaltySurvey.fields.employer')}
          className="col-md-4"
          value={form.employerNameLastName}
          onChange={(value) => patch({ employerNameLastName: value })}
        />

        <Input
          id="survey-phone"
          label={t('finance.penaltySurvey.fields.phone')}
          type="tel"
          className="col-md-4"
          value={form.phone}
          onChange={(value) => patch({ phone: value })}
        />

        <Input
          id="survey-email"
          label={t('finance.penaltySurvey.fields.email')}
          type="email"
          className="col-md-4"
          value={form.email}
          onChange={(value) => patch({ email: value })}
        />

        <Input
          id="survey-tax-office"
          label={t('finance.penaltySurvey.fields.taxOffice')}
          className="col-md-6"
          value={form.taxTaxOffice}
          onChange={(value) => patch({ taxTaxOffice: value })}
        />

        <Input
          id="survey-tax-number"
          label={t('finance.penaltySurvey.fields.taxNumber')}
          className="col-md-6"
          value={form.taxNumber}
          onChange={(value) => patch({ taxNumber: value })}
        />

        <TextArea
          id="survey-address"
          label={t('finance.penaltySurvey.fields.address')}
          className="col-12"
          rows={2}
          value={form.address}
          onChange={(value) => patch({ address: value })}
        />
      </div>
    </Modal>
  )
}
