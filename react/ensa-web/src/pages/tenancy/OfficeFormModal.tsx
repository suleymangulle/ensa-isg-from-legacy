import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { CheckBox, Input, Select, TextArea } from 'rich-react-component'
import { useLookup } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { Modal } from '@/components/Form'
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
  isHeadquarterOffice: boolean
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
    isHeadquarterOffice: false,
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
    isHeadquarterOffice: office.isHeadquarterOffice,
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
      isHeadquarterOffice: state.isHeadquarterOffice,
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
        <Input
          id="office-name"
          label={t('office.fields.name')}
          required
          error={nameError}
          className="col-md-6"
          value={state.name}
          onChange={(value) => set('name', value)}
        />

        <Select
          id="office-companyId"
          label={t('office.fields.company')}
          helpText={t('office.form.companyHint')}
          className="col-md-6"
          placeholder={t('office.form.attachedToOrganization')}
          options={
            companies.data?.items.map((company) => ({
              value: String(company.id),
              label: company.displayName,
            })) ?? []
          }
          value={state.companyId === '' ? null : state.companyId}
          onChange={(value) => set('companyId', value ?? '')}
        />

        <Input
          id="office-phone"
          label={t('office.fields.phone')}
          className="col-md-6"
          value={state.phone}
          onChange={(value) => set('phone', value)}
        />

        <Input
          id="office-fax"
          label={t('office.fields.fax')}
          className="col-md-6"
          value={state.fax}
          onChange={(value) => set('fax', value)}
        />

        <Select
          id="office-cityId"
          label={t('office.fields.city')}
          className="col-md-6"
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
          id="office-districtId"
          label={t('office.fields.district')}
          className="col-md-6"
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
          id="office-address"
          label={t('office.fields.address')}
          className="col-12"
          rows={2}
          value={state.address}
          onChange={(value) => set('address', value)}
        />

        <Input
          id="office-authorizedPerson"
          label={t('office.fields.authorizedPerson')}
          className="col-md-6"
          value={state.authorizedPerson}
          onChange={(value) => set('authorizedPerson', value)}
        />

        <Input
          id="office-authorizedEmail"
          type="email"
          label={t('office.fields.authorizedEmail')}
          className="col-md-6"
          value={state.authorizedEmail}
          onChange={(value) => set('authorizedEmail', value)}
        />

        <div className="col-md-6">
          <CheckBox
            id="office-headquarterOffice"
            checked={state.isHeadquarterOffice}
            onChange={(checked) => set('isHeadquarterOffice', checked)}
            label={t('office.fields.headquarterOffice')}
            helpText={t('office.form.headquarterHint')}
          />
        </div>

        {isEdit && (
          <Select
            id="office-isActive"
            label={t('office.fields.status')}
            className="col-md-6"
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
