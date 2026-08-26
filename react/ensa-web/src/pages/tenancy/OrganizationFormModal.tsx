import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/http'
import { useReferenceData } from '@/api/endpoints'
import { useCreate, useUpdate } from '@/api/mutations'
import { Field, Modal, controlClass } from '@/components/Form'
import {
  TENANCY_RESOURCES,
  optionalNumber,
  optionalText,
  toDateInput,
  useCityLookup,
  useDistrictLookup,
  type OrganizationDto,
  type OrganizationInput,
} from './api'

interface FormState {
  name: string
  code: string
  organizationTypeId: string
  subscriptionPlanId: string
  taxTaxOffice: string
  taxNumber: string
  address: string
  cityId: string
  districtId: string
  phone: string
  email: string
  webUrl: string
  authorizedFullName: string
  authorizedPhone: string
  authorizedEmail: string
  subscriptionStart: string
  subscriptionEnd: string
  maximumUserCount: string
  maximumCompanyCount: string
  isActive: boolean
  /** Carried through untouched so an edit cannot silently clear it. */
  logoDocumentId: number | null
}

function emptyState(): FormState {
  return {
    name: '',
    code: '',
    organizationTypeId: '',
    subscriptionPlanId: '',
    taxTaxOffice: '',
    taxNumber: '',
    address: '',
    cityId: '',
    districtId: '',
    phone: '',
    email: '',
    webUrl: '',
    authorizedFullName: '',
    authorizedPhone: '',
    authorizedEmail: '',
    subscriptionStart: new Date().toISOString().slice(0, 10),
    subscriptionEnd: '',
    maximumUserCount: '',
    maximumCompanyCount: '',
    isActive: true,
    logoDocumentId: null,
  }
}

/**
 * Seeds the form from the record being edited.
 *
 * `UpdateOrganizationDto` is an absolute payload, so every field read back has to be sent
 * again — including `logoDocumentId`, which the form does not render.
 */
function stateFromOrganization(organization: OrganizationDto): FormState {
  return {
    name: organization.name,
    code: organization.code,
    organizationTypeId: String(organization.organizationTypeId),
    subscriptionPlanId: String(organization.subscriptionPlanId),
    taxTaxOffice: organization.taxTaxOffice ?? '',
    taxNumber: organization.taxNumber ?? '',
    address: organization.address ?? '',
    cityId: organization.cityId ? String(organization.cityId) : '',
    districtId: organization.districtId ? String(organization.districtId) : '',
    phone: organization.phone ?? '',
    email: organization.email ?? '',
    webUrl: organization.webUrl ?? '',
    authorizedFullName: organization.authorizedFullName ?? '',
    authorizedPhone: organization.authorizedPhone ?? '',
    authorizedEmail: organization.authorizedEmail ?? '',
    subscriptionStart: toDateInput(organization.subscriptionStart),
    subscriptionEnd: toDateInput(organization.subscriptionEnd),
    maximumUserCount:
      organization.maximumUserCount != null ? String(organization.maximumUserCount) : '',
    maximumCompanyCount:
      organization.maximumCompanyCount != null ? String(organization.maximumCompanyCount) : '',
    isActive: organization.isActive,
    logoDocumentId: organization.logoDocumentId ?? null,
  }
}

export default function OrganizationFormModal({
  organization,
  onClose,
}: {
  organization?: OrganizationDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const isEdit = !!organization

  const [state, setState] = useState<FormState>(() =>
    organization ? stateFromOrganization(organization) : emptyState(),
  )
  const [errors, setErrors] = useState<Record<string, string>>({})

  const cities = useCityLookup()
  const organizationTypes = useReferenceData('organization-types')
  const subscriptionPlans = useReferenceData('subscription-plans')
  const districts = useDistrictLookup(optionalNumber(state.cityId))

  const create = useCreate<OrganizationInput>(TENANCY_RESOURCES.organization, {
    onSuccess: onClose,
  })
  const update = useUpdate<OrganizationInput>(TENANCY_RESOURCES.organization, {
    onSuccess: onClose,
  })
  const pending = isEdit ? update : create

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setState((previous) => ({ ...previous, [key]: value }))
  }

  function submit() {
    const next: Record<string, string> = {}
    if (!state.name.trim()) next.name = t('validation.required')
    if (!state.code.trim()) next.code = t('validation.required')
    if (!optionalNumber(state.organizationTypeId)) {
      next.organizationTypeId = t('organization.form.positiveIdRequired')
    }
    if (!optionalNumber(state.subscriptionPlanId)) {
      next.subscriptionPlanId = t('organization.form.positiveIdRequired')
    }
    if (!state.subscriptionStart) next.subscriptionStart = t('validation.required')

    setErrors(next)
    if (Object.keys(next).length > 0) return

    const input: OrganizationInput = {
      name: state.name.trim(),
      code: state.code.trim(),
      organizationTypeId: Number(state.organizationTypeId),
      subscriptionPlanId: Number(state.subscriptionPlanId),
      taxTaxOffice: optionalText(state.taxTaxOffice),
      taxNumber: optionalText(state.taxNumber),
      address: optionalText(state.address),
      cityId: optionalNumber(state.cityId),
      districtId: optionalNumber(state.districtId),
      phone: optionalText(state.phone),
      email: optionalText(state.email),
      webUrl: optionalText(state.webUrl),
      authorizedFullName: optionalText(state.authorizedFullName),
      authorizedPhone: optionalText(state.authorizedPhone),
      authorizedEmail: optionalText(state.authorizedEmail),
      logoDocumentId: state.logoDocumentId,
      subscriptionStart: state.subscriptionStart,
      subscriptionEnd: state.subscriptionEnd || null,
      maximumUserCount: optionalNumber(state.maximumUserCount),
      maximumCompanyCount: optionalNumber(state.maximumCompanyCount),
      ...(isEdit ? { isActive: state.isActive } : {}),
    }

    if (isEdit && organization) update.mutate({ id: organization.id, input })
    else create.mutate(input)
  }

  return (
    <Modal
      title={isEdit ? t('organization.form.editTitle') : t('organization.form.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending.isPending}
      error={pending.error ? errorMessage(pending.error) : null}
      size="xl"
    >
      <div className="row g-3">
        <div className="col-12">
          <h3 className="h6 fw-bold mb-0" style={{ color: 'var(--kt-gray-700)' }}>
            {t('organization.form.sections.identity')}
          </h3>
        </div>

        <Field
          label={t('organization.fields.name')}
          htmlFor="organization-name"
          required
          error={errors.name}
          className="col-md-6"
        >
          <input
            id="organization-name"
            className={controlClass('form-control', errors.name)}
            value={state.name}
            onChange={(event) => set('name', event.target.value)}
          />
        </Field>

        <Field
          label={t('organization.fields.code')}
          htmlFor="organization-code"
          required
          error={errors.code}
          hint={t('organization.form.codeHint')}
          className="col-md-6"
        >
          <input
            id="organization-code"
            className={controlClass('form-control', errors.code)}
            value={state.code}
            onChange={(event) => set('code', event.target.value)}
          />
        </Field>

        <Field
          label={t('organization.fields.organizationTypeId')}
          htmlFor="organization-organizationTypeId"
          required
          error={errors.organizationTypeId}
          className="col-md-3"
        >
          <select
            id="organization-organizationTypeId"
            className={controlClass('form-select', errors.organizationTypeId)}
            value={state.organizationTypeId}
            onChange={(event) => set('organizationTypeId', event.target.value)}
          >
            <option value="">{t('common.none')}</option>
            {organizationTypes.data?.items.map((item) => (
              <option key={item.id} value={item.id}>
                {item.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('organization.fields.subscriptionPlanId')}
          htmlFor="organization-subscriptionPlanId"
          required
          error={errors.subscriptionPlanId}
          className="col-md-3"
        >
          <select
            id="organization-subscriptionPlanId"
            className={controlClass('form-select', errors.subscriptionPlanId)}
            value={state.subscriptionPlanId}
            onChange={(event) => set('subscriptionPlanId', event.target.value)}
          >
            <option value="">{t('common.none')}</option>
            {subscriptionPlans.data?.items.map((item) => (
              <option key={item.id} value={item.id}>
                {item.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('organization.fields.taxOffice')}
          htmlFor="organization-taxTaxOffice"
          className="col-md-3"
        >
          <input
            id="organization-taxTaxOffice"
            className="form-control"
            value={state.taxTaxOffice}
            onChange={(event) => set('taxTaxOffice', event.target.value)}
          />
        </Field>

        <Field
          label={t('organization.fields.taxNumber')}
          htmlFor="organization-taxNumber"
          className="col-md-3"
        >
          <input
            id="organization-taxNumber"
            className="form-control"
            value={state.taxNumber}
            onChange={(event) => set('taxNumber', event.target.value)}
          />
        </Field>

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('organization.form.sections.contact')}
          </h3>
        </div>

        <Field
          label={t('organization.fields.phone')}
          htmlFor="organization-phone"
          className="col-md-4"
        >
          <input
            id="organization-phone"
            className="form-control"
            value={state.phone}
            onChange={(event) => set('phone', event.target.value)}
          />
        </Field>

        <Field
          label={t('organization.fields.email')}
          htmlFor="organization-email"
          className="col-md-4"
        >
          <input
            id="organization-email"
            type="email"
            className="form-control"
            value={state.email}
            onChange={(event) => set('email', event.target.value)}
          />
        </Field>

        <Field
          label={t('organization.fields.webUrl')}
          htmlFor="organization-webUrl"
          className="col-md-4"
        >
          <input
            id="organization-webUrl"
            className="form-control"
            value={state.webUrl}
            onChange={(event) => set('webUrl', event.target.value)}
          />
        </Field>

        <Field
          label={t('organization.fields.city')}
          htmlFor="organization-cityId"
          className="col-md-4"
        >
          <select
            id="organization-cityId"
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

        <Field
          label={t('organization.fields.district')}
          htmlFor="organization-districtId"
          className="col-md-4"
        >
          <select
            id="organization-districtId"
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

        <Field
          label={t('organization.fields.address')}
          htmlFor="organization-address"
          className="col-12"
        >
          <textarea
            id="organization-address"
            className="form-control"
            rows={2}
            value={state.address}
            onChange={(event) => set('address', event.target.value)}
          />
        </Field>

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('organization.form.sections.authorized')}
          </h3>
        </div>

        <Field
          label={t('organization.fields.authorizedFullName')}
          htmlFor="organization-authorizedFullName"
          className="col-md-4"
        >
          <input
            id="organization-authorizedFullName"
            className="form-control"
            value={state.authorizedFullName}
            onChange={(event) => set('authorizedFullName', event.target.value)}
          />
        </Field>

        <Field
          label={t('organization.fields.authorizedPhone')}
          htmlFor="organization-authorizedPhone"
          className="col-md-4"
        >
          <input
            id="organization-authorizedPhone"
            className="form-control"
            value={state.authorizedPhone}
            onChange={(event) => set('authorizedPhone', event.target.value)}
          />
        </Field>

        <Field
          label={t('organization.fields.authorizedEmail')}
          htmlFor="organization-authorizedEmail"
          className="col-md-4"
        >
          <input
            id="organization-authorizedEmail"
            type="email"
            className="form-control"
            value={state.authorizedEmail}
            onChange={(event) => set('authorizedEmail', event.target.value)}
          />
        </Field>

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('organization.form.sections.subscription')}
          </h3>
        </div>

        <Field
          label={t('organization.fields.subscriptionStart')}
          htmlFor="organization-subscriptionStart"
          required
          error={errors.subscriptionStart}
          className="col-md-3"
        >
          <input
            id="organization-subscriptionStart"
            type="date"
            className={controlClass('form-control', errors.subscriptionStart)}
            value={state.subscriptionStart}
            onChange={(event) => set('subscriptionStart', event.target.value)}
          />
        </Field>

        <Field
          label={t('organization.fields.subscriptionEnd')}
          htmlFor="organization-subscriptionEnd"
          hint={t('organization.form.openEndedHint')}
          className="col-md-3"
        >
          <input
            id="organization-subscriptionEnd"
            type="date"
            className="form-control"
            value={state.subscriptionEnd}
            onChange={(event) => set('subscriptionEnd', event.target.value)}
          />
        </Field>

        <Field
          label={t('organization.fields.maximumUserCount')}
          htmlFor="organization-maximumUserCount"
          hint={t('organization.form.unlimitedHint')}
          className="col-md-3"
        >
          <input
            id="organization-maximumUserCount"
            type="number"
            min={1}
            className="form-control"
            value={state.maximumUserCount}
            onChange={(event) => set('maximumUserCount', event.target.value)}
          />
        </Field>

        <Field
          label={t('organization.fields.maximumCompanyCount')}
          htmlFor="organization-maximumCompanyCount"
          hint={t('organization.form.unlimitedHint')}
          className="col-md-3"
        >
          <input
            id="organization-maximumCompanyCount"
            type="number"
            min={1}
            className="form-control"
            value={state.maximumCompanyCount}
            onChange={(event) => set('maximumCompanyCount', event.target.value)}
          />
        </Field>

        {isEdit && (
          <Field
            label={t('organization.fields.status')}
            htmlFor="organization-isActive"
            className="col-md-3"
          >
            <select
              id="organization-isActive"
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
