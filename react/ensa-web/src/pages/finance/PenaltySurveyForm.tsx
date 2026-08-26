import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { HazardClass } from '@/api/enums'
import { Field, Modal, controlClass } from '@/components/Form'
import type { PenaltySurveyDto, SavePenaltySurveyDto } from './api'
import { EnumField, enumValues, parseDecimal } from './components'

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
  workerCount: string
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
    workerCount: survey?.workerCount != null ? String(survey.workerCount) : '',
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

    const workerCount = form.workerCount ? Math.round(parseDecimal(form.workerCount)) : null
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
        <Field
          label={t('finance.penaltySurvey.fields.companyTitle')}
          htmlFor="survey-company-title"
          required
          error={validation.companyTitle}
          className="col-md-6"
        >
          <input
            id="survey-company-title"
            type="text"
            className={controlClass('form-control', validation.companyTitle)}
            value={form.companyTitle}
            aria-invalid={validation.companyTitle ? true : undefined}
            onChange={(event) => patch({ companyTitle: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.penaltySurvey.fields.facilityName')}
          htmlFor="survey-facility-name"
          className="col-md-6"
        >
          <input
            id="survey-facility-name"
            type="text"
            className="form-control"
            value={form.facilityName}
            onChange={(event) => patch({ facilityName: event.target.value })}
          />
        </Field>

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

        <Field
          label={t('finance.penaltySurvey.fields.workerCount')}
          htmlFor="survey-worker-count"
          error={validation.workerCount}
          hint={t('finance.penaltySurvey.form.workerCountHint')}
          className="col-md-4"
        >
          <input
            id="survey-worker-count"
            type="number"
            step="1"
            min="0"
            className={controlClass('form-control text-end', validation.workerCount)}
            value={form.workerCount}
            aria-invalid={validation.workerCount ? true : undefined}
            onChange={(event) => patch({ workerCount: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.penaltySurvey.fields.ssiRegistrationNumber')}
          htmlFor="survey-ssi"
          className="col-md-4"
        >
          <input
            id="survey-ssi"
            type="text"
            className="form-control"
            value={form.ssiRegistrationNumber}
            onChange={(event) => patch({ ssiRegistrationNumber: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.penaltySurvey.fields.facilityOwner')}
          htmlFor="survey-owner"
          className="col-md-4"
        >
          <input
            id="survey-owner"
            type="text"
            className="form-control"
            value={form.facilityOwner}
            onChange={(event) => patch({ facilityOwner: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.penaltySurvey.fields.facilityOwnerDuty')}
          htmlFor="survey-owner-duty"
          className="col-md-4"
        >
          <input
            id="survey-owner-duty"
            type="text"
            className="form-control"
            value={form.facilityOwnerDuty}
            onChange={(event) => patch({ facilityOwnerDuty: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.penaltySurvey.fields.facilityOwnerGsm')}
          htmlFor="survey-owner-gsm"
          className="col-md-4"
        >
          <input
            id="survey-owner-gsm"
            type="tel"
            className="form-control"
            value={form.facilityOwnerGsm}
            onChange={(event) => patch({ facilityOwnerGsm: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.penaltySurvey.fields.employer')}
          htmlFor="survey-employer"
          className="col-md-4"
        >
          <input
            id="survey-employer"
            type="text"
            className="form-control"
            value={form.employerNameLastName}
            onChange={(event) => patch({ employerNameLastName: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.penaltySurvey.fields.phone')}
          htmlFor="survey-phone"
          className="col-md-4"
        >
          <input
            id="survey-phone"
            type="tel"
            className="form-control"
            value={form.phone}
            onChange={(event) => patch({ phone: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.penaltySurvey.fields.email')}
          htmlFor="survey-email"
          className="col-md-4"
        >
          <input
            id="survey-email"
            type="email"
            className="form-control"
            value={form.email}
            onChange={(event) => patch({ email: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.penaltySurvey.fields.taxOffice')}
          htmlFor="survey-tax-office"
          className="col-md-6"
        >
          <input
            id="survey-tax-office"
            type="text"
            className="form-control"
            value={form.taxTaxOffice}
            onChange={(event) => patch({ taxTaxOffice: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.penaltySurvey.fields.taxNumber')}
          htmlFor="survey-tax-number"
          className="col-md-6"
        >
          <input
            id="survey-tax-number"
            type="text"
            className="form-control"
            value={form.taxNumber}
            onChange={(event) => patch({ taxNumber: event.target.value })}
          />
        </Field>

        <Field label={t('finance.penaltySurvey.fields.address')} htmlFor="survey-address">
          <textarea
            id="survey-address"
            className="form-control"
            rows={2}
            value={form.address}
            onChange={(event) => patch({ address: event.target.value })}
          />
        </Field>
      </div>
    </Modal>
  )
}
