import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Input, NumberInput, Select, TextArea } from 'rich-react-component'
import { errorMessage } from '@/api/http'
import { useReferenceData } from '@/api/endpoints'
import { useCreate, useUpdate } from '@/api/mutations'
import { Modal } from '@/components/Form'
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
  taxOffice: string
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
    taxOffice: '',
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
    taxOffice: organization.taxOffice ?? '',
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
      taxOffice: optionalText(state.taxOffice),
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

        <Input
          id="organization-name"
          label={t('organization.fields.name')}
          required
          error={errors.name}
          className="col-md-6"
          value={state.name}
          onChange={(value) => set('name', value)}
        />

        <Input
          id="organization-code"
          label={t('organization.fields.code')}
          required
          error={errors.code}
          helpText={t('organization.form.codeHint')}
          className="col-md-6"
          value={state.code}
          onChange={(value) => set('code', value)}
        />

        <Select
          id="organization-organizationTypeId"
          label={t('organization.fields.organizationTypeId')}
          required
          error={errors.organizationTypeId}
          className="col-md-3"
          placeholder={t('common.none')}
          options={
            organizationTypes.data?.items.map((item) => ({
              value: String(item.id),
              label: item.displayName,
            })) ?? []
          }
          value={state.organizationTypeId === '' ? null : state.organizationTypeId}
          onChange={(value) => set('organizationTypeId', value ?? '')}
        />

        <Select
          id="organization-subscriptionPlanId"
          label={t('organization.fields.subscriptionPlanId')}
          required
          error={errors.subscriptionPlanId}
          className="col-md-3"
          placeholder={t('common.none')}
          options={
            subscriptionPlans.data?.items.map((item) => ({
              value: String(item.id),
              label: item.displayName,
            })) ?? []
          }
          value={state.subscriptionPlanId === '' ? null : state.subscriptionPlanId}
          onChange={(value) => set('subscriptionPlanId', value ?? '')}
        />

        <Input
          id="organization-taxTaxOffice"
          label={t('organization.fields.taxOffice')}
          className="col-md-3"
          value={state.taxOffice}
          onChange={(value) => set('taxOffice', value)}
        />

        <Input
          id="organization-taxNumber"
          label={t('organization.fields.taxNumber')}
          className="col-md-3"
          value={state.taxNumber}
          onChange={(value) => set('taxNumber', value)}
        />

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('organization.form.sections.contact')}
          </h3>
        </div>

        <Input
          id="organization-phone"
          label={t('organization.fields.phone')}
          className="col-md-4"
          value={state.phone}
          onChange={(value) => set('phone', value)}
        />

        <Input
          id="organization-email"
          type="email"
          label={t('organization.fields.email')}
          className="col-md-4"
          value={state.email}
          onChange={(value) => set('email', value)}
        />

        <Input
          id="organization-webUrl"
          label={t('organization.fields.webUrl')}
          className="col-md-4"
          value={state.webUrl}
          onChange={(value) => set('webUrl', value)}
        />

        <Select
          id="organization-cityId"
          label={t('organization.fields.city')}
          className="col-md-4"
          placeholder={t('common.none')}
          options={
            cities.data?.items.map((city) => ({
              value: String(city.id),
              label: city.displayName,
            })) ?? []
          }
          value={state.cityId === '' ? null : state.cityId}
          onChange={(value) => {
            set('cityId', value ?? '')
            set('districtId', '')
          }}
        />

        <Select
          id="organization-districtId"
          label={t('organization.fields.district')}
          className="col-md-4"
          placeholder={t('common.none')}
          disabled={!state.cityId}
          options={
            districts.data?.items.map((district) => ({
              value: String(district.id),
              label: district.displayName,
            })) ?? []
          }
          value={state.districtId === '' ? null : state.districtId}
          onChange={(value) => set('districtId', value ?? '')}
        />

        <TextArea
          id="organization-address"
          label={t('organization.fields.address')}
          className="col-12"
          rows={2}
          value={state.address}
          onChange={(value) => set('address', value)}
        />

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('organization.form.sections.authorized')}
          </h3>
        </div>

        <Input
          id="organization-authorizedFullName"
          label={t('organization.fields.authorizedFullName')}
          className="col-md-4"
          value={state.authorizedFullName}
          onChange={(value) => set('authorizedFullName', value)}
        />

        <Input
          id="organization-authorizedPhone"
          label={t('organization.fields.authorizedPhone')}
          className="col-md-4"
          value={state.authorizedPhone}
          onChange={(value) => set('authorizedPhone', value)}
        />

        <Input
          id="organization-authorizedEmail"
          type="email"
          label={t('organization.fields.authorizedEmail')}
          className="col-md-4"
          value={state.authorizedEmail}
          onChange={(value) => set('authorizedEmail', value)}
        />

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('organization.form.sections.subscription')}
          </h3>
        </div>

        <Input
          id="organization-subscriptionStart"
          label={t('organization.fields.subscriptionStart')}
          required
          error={errors.subscriptionStart}
          className="col-md-3"
          value={state.subscriptionStart}
          onChange={(value) => set('subscriptionStart', value)}
          inputProps={{ type: 'date' }}
        />

        <Input
          id="organization-subscriptionEnd"
          label={t('organization.fields.subscriptionEnd')}
          helpText={t('organization.form.openEndedHint')}
          className="col-md-3"
          value={state.subscriptionEnd}
          onChange={(value) => set('subscriptionEnd', value)}
          inputProps={{ type: 'date' }}
        />

        <NumberInput
          id="organization-maximumUserCount"
          label={t('organization.fields.maximumUserCount')}
          helpText={t('organization.form.unlimitedHint')}
          className="col-md-3"
          min={1}
          value={state.maximumUserCount === '' ? null : Number(state.maximumUserCount)}
          onChange={(value) => set('maximumUserCount', value === null ? '' : String(value))}
        />

        <NumberInput
          id="organization-maximumCompanyCount"
          label={t('organization.fields.maximumCompanyCount')}
          helpText={t('organization.form.unlimitedHint')}
          className="col-md-3"
          min={1}
          value={state.maximumCompanyCount === '' ? null : Number(state.maximumCompanyCount)}
          onChange={(value) => set('maximumCompanyCount', value === null ? '' : String(value))}
        />

        {isEdit && (
          <Select
            id="organization-isActive"
            label={t('organization.fields.status')}
            className="col-md-3"
            options={[
              { value: 'true', label: t('common.active') },
              { value: 'false', label: t('common.passive') },
            ]}
            value={state.isActive ? 'true' : 'false'}
            onChange={(value) => set('isActive', value === 'true')}
          />
        )}
      </div>
    </Modal>
  )
}
