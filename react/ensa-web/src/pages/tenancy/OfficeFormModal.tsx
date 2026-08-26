import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLookup } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { Field, Modal, controlClass } from '@/components/Form'
import {
  TENANCY_RESOURCES,
  optionalNumber,
  optionalText,
  useCityLookup,
  useDistrictLookup,
  type OfficeDto,
  type OfficeInput,
} from './api'

interface FormState {
  name: string
  phone: string
  fax: string
  address: string
  cityId: string
  districtId: string
  authorizedPerson: string
  authorizedEmail: string
  companyId: string
  headquarterOffice: boolean
  isActive: boolean
}

function emptyState(): FormState {
  return {
    name: '',
    phone: '',
    fax: '',
    address: '',
    cityId: '',
    districtId: '',
    authorizedPerson: '',
    authorizedEmail: '',
    companyId: '',
    headquarterOffice: false,
    isActive: true,
  }
}

function stateFromOffice(office: OfficeDto): FormState {
  return {
    name: office.name,
    phone: office.phone ?? '',
    fax: office.fax ?? '',
    address: office.address ?? '',
    cityId: office.cityId ? String(office.cityId) : '',
    districtId: office.districtId ? String(office.districtId) : '',
    authorizedPerson: office.authorizedPerson ?? '',
    authorizedEmail: office.authorizedEmail ?? '',
    companyId: office.companyId ? String(office.companyId) : '',
    headquarterOffice: office.headquarterOffice,
    isActive: office.isActive,
  }
}

/**
 * Create / edit dialog for an office.
 *
 * Only one office per organization may carry the headquarters flag; the service refuses the
 * request when another office already holds it, and that refusal surfaces through
 * `errorMessage()` rather than being second-guessed here.
 */
export default function OfficeFormModal({
  office,
  onClose,
}: {
  office?: OfficeDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const isEdit = !!office

  const [state, setState] = useState<FormState>(() =>
    office ? stateFromOffice(office) : emptyState(),
  )
  const [nameError, setNameError] = useState<string | undefined>()

  const cities = useCityLookup()
  const districts = useDistrictLookup(optionalNumber(state.cityId))
  const companies = useLookup('company')

  const create = useCreate<OfficeInput>(TENANCY_RESOURCES.office, { onSuccess: onClose })
  const update = useUpdate<OfficeInput>(TENANCY_RESOURCES.office, { onSuccess: onClose })
  const pending = isEdit ? update : create

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setState((previous) => ({ ...previous, [key]: value }))
  }

  function submit() {
    if (!state.name.trim()) {
      setNameError(t('validation.required'))
      return
    }
    setNameError(undefined)

    const input: OfficeInput = {
      name: state.name.trim(),
      phone: optionalText(state.phone),
      fax: optionalText(state.fax),
      address: optionalText(state.address),
      cityId: optionalNumber(state.cityId),
      districtId: optionalNumber(state.districtId),
      authorizedPerson: optionalText(state.authorizedPerson),
      authorizedEmail: optionalText(state.authorizedEmail),
      companyId: optionalNumber(state.companyId),
      headquarterOffice: state.headquarterOffice,
      ...(isEdit ? { isActive: state.isActive } : {}),
    }

    if (isEdit && office) update.mutate({ id: office.id, input })
    else create.mutate(input)
  }

  return (
    <Modal
      title={isEdit ? t('office.form.editTitle') : t('office.form.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending.isPending}
      error={pending.error ? errorMessage(pending.error) : null}
      size="lg"
    >
      <div className="row g-3">
        <Field
          label={t('office.fields.name')}
          htmlFor="office-name"
          required
          error={nameError}
          className="col-md-6"
        >
          <input
            id="office-name"
            className={controlClass('form-control', nameError)}
            value={state.name}
            onChange={(event) => set('name', event.target.value)}
          />
        </Field>

        <Field
          label={t('office.fields.company')}
          htmlFor="office-companyId"
          hint={t('office.form.companyHint')}
          className="col-md-6"
        >
          <select
            id="office-companyId"
            className="form-select"
            value={state.companyId}
            onChange={(event) => set('companyId', event.target.value)}
          >
            <option value="">{t('office.form.attachedToOrganization')}</option>
            {companies.data?.items.map((company) => (
              <option key={company.id} value={company.id}>
                {company.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('office.fields.phone')} htmlFor="office-phone" className="col-md-6">
          <input
            id="office-phone"
            className="form-control"
            value={state.phone}
            onChange={(event) => set('phone', event.target.value)}
          />
        </Field>

        <Field label={t('office.fields.fax')} htmlFor="office-fax" className="col-md-6">
          <input
            id="office-fax"
            className="form-control"
            value={state.fax}
            onChange={(event) => set('fax', event.target.value)}
          />
        </Field>

        <Field label={t('office.fields.city')} htmlFor="office-cityId" className="col-md-6">
          <select
            id="office-cityId"
            className="form-select"
            value={state.cityId}
            onChange={(event) => {
              set('cityId', event.target.value)
              set('districtId', '')
            }}
          >
            <option value="">{t('common.none')}</option>
            {cities.data?.items.map((city) => (
              <option key={city.id} value={city.id}>
                {city.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('office.fields.district')} htmlFor="office-districtId" className="col-md-6">
          <select
            id="office-districtId"
            className="form-select"
            value={state.districtId}
            disabled={!state.cityId}
            onChange={(event) => set('districtId', event.target.value)}
          >
            <option value="">{t('common.none')}</option>
            {districts.data?.items.map((district) => (
              <option key={district.id} value={district.id}>
                {district.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('office.fields.address')} htmlFor="office-address" className="col-12">
          <textarea
            id="office-address"
            className="form-control"
            rows={2}
            value={state.address}
            onChange={(event) => set('address', event.target.value)}
          />
        </Field>

        <Field
          label={t('office.fields.authorizedPerson')}
          htmlFor="office-authorizedPerson"
          className="col-md-6"
        >
          <input
            id="office-authorizedPerson"
            className="form-control"
            value={state.authorizedPerson}
            onChange={(event) => set('authorizedPerson', event.target.value)}
          />
        </Field>

        <Field
          label={t('office.fields.authorizedEmail')}
          htmlFor="office-authorizedEmail"
          className="col-md-6"
        >
          <input
            id="office-authorizedEmail"
            type="email"
            className="form-control"
            value={state.authorizedEmail}
            onChange={(event) => set('authorizedEmail', event.target.value)}
          />
        </Field>

        <div className="col-md-6">
          <div className="form-check">
            <input
              id="office-headquarterOffice"
              type="checkbox"
              className="form-check-input"
              checked={state.headquarterOffice}
              onChange={(event) => set('headquarterOffice', event.target.checked)}
            />
            <label className="form-check-label" htmlFor="office-headquarterOffice">
              {t('office.fields.headquarterOffice')}
            </label>
          </div>
          <div className="form-text" style={{ color: 'var(--kt-gray-500)' }}>
            {t('office.form.headquarterHint')}
          </div>
        </div>

        {isEdit && (
          <Field label={t('office.fields.status')} htmlFor="office-isActive" className="col-md-6">
            <select
              id="office-isActive"
              className="form-select"
              value={state.isActive ? 'true' : 'false'}
              onChange={(event) => set('isActive', event.target.value === 'true')}
            >
              <option value="true">{t('common.active')}</option>
              <option value="false">{t('common.passive')}</option>
            </select>
          </Field>
        )}
      </div>
    </Modal>
  )
}
