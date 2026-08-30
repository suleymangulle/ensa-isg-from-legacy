import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { CheckBox, Input, Select, TextArea } from 'rich-react-component'
import { ENDPOINTS, useLookup, type CompanyDto } from '@/api/endpoints'
import { HazardClass, WorkplaceType } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { Modal } from '@/components/Form'
import {
  optionalNumber,
  optionalText,
  useCityLookup,
  useDistrictLookup,
  useOccupationCodeLookup,
  type CompanyInput,
} from './api'

const HAZARD_CLASSES = [
  HazardClass.Unspecified,
  HazardClass.LowHazard,
  HazardClass.Hazardous,
  HazardClass.VeryHazardous,
]

const WORKPLACE_TYPES = [WorkplaceType.Headquarter, WorkplaceType.Branch]

interface FormState {
  companyName: string
  ssiNumber: string
  hazardClass: HazardClass
  workplaceType: WorkplaceType
  headquarterCompanyId: string
  occupationCodeId: string
  cityId: string
  districtId: string
  address: string
  phone: string
  email: string
  authorizedPerson: string
  taxOffice: string
  taxNumber: string
  officeId: string
  notes: string
  isActive: boolean
}

function emptyState(): FormState {
  return {
    companyName: '',
    ssiNumber: '',
    hazardClass: HazardClass.Unspecified,
    workplaceType: WorkplaceType.Headquarter,
    headquarterCompanyId: '',
    occupationCodeId: '',
    cityId: '',
    districtId: '',
    address: '',
    phone: '',
    email: '',
    authorizedPerson: '',
    taxOffice: '',
    taxNumber: '',
    officeId: '',
    notes: '',
    isActive: true,
  }
}

function stateFromCompany(company: CompanyDto): FormState {
  return {
    companyName: company.companyName,
    ssiNumber: company.ssiNumber ?? '',
    hazardClass: company.hazardClass,
    workplaceType: company.workplaceType,
    headquarterCompanyId: company.headquarterCompanyId ? String(company.headquarterCompanyId) : '',
    occupationCodeId: company.occupationCodeId ? String(company.occupationCodeId) : '',
    cityId: company.cityId ? String(company.cityId) : '',
    districtId: company.districtId ? String(company.districtId) : '',
    address: company.address ?? '',
    phone: company.phone ?? '',
    email: company.email ?? '',
    authorizedPerson: company.authorizedPerson ?? '',
    taxOffice: company.taxOffice ?? '',
    taxNumber: company.taxNumber ?? '',
    officeId: company.officeId ? String(company.officeId) : '',
    notes: company.notes ?? '',
    isActive: company.isActive,
  }
}

/**
 * Create / edit dialog for a workplace.
 *
 * <b>What it asks for is `CreateCompanyDto`, not the legacy form.</b> The legacy screen
 * (`FirmaEkleController`) wrote fifty columns in one go — pricing, invoice addresses, visit
 * durations, logo, region code, the finance contact — because that screen was the only place a
 * company record was ever touched. Those fields still exist on the entity and belong to the
 * finance and contract screens; the create contract deliberately covers identity, location and
 * contact, which is what somebody adding a workplace actually knows at that moment.
 *
 * Two rules come from the server and are not re-implemented here, only surfaced: an SSI number
 * must be unique within the organization, and a branch must name the headquarter it belongs to.
 * `CompanyManager` refuses either, and the refusal reaches the user through `errorMessage()`.
 */
export default function CompanyFormModal({
  company,
  onClose,
}: {
  company?: CompanyDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const isEdit = !!company

  const [state, setState] = useState<FormState>(() =>
    company ? stateFromCompany(company) : emptyState(),
  )
  const [nameError, setNameError] = useState<string | undefined>()
  const [cityError, setCityError] = useState<string | undefined>()
  const [districtError, setDistrictError] = useState<string | undefined>()
  const [occupationSearch, setOccupationSearch] = useState('')

  const cities = useCityLookup()
  const districts = useDistrictLookup(optionalNumber(state.cityId))
  const offices = useLookup('office')
  const headquarters = useLookup('company')
  const occupationCodes = useOccupationCodeLookup(occupationSearch)

  const create = useCreate<CompanyInput, CompanyDto>(ENDPOINTS.company, { onSuccess: onClose })
  const update = useUpdate<CompanyInput, CompanyDto>(ENDPOINTS.company, { onSuccess: onClose })
  const pending = isEdit ? update : create

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setState((previous) => ({ ...previous, [key]: value }))
  }

  /**
   * Picking a NACE code sets the hazard class it implies, because that is where the classification
   * comes from in law. It stays editable: the entity keeps a separate flag for whether a human
   * confirmed the derived class, so overriding it here is a legitimate answer rather than a
   * mistake to prevent.
   */
  function selectOccupationCode(value: string | null) {
    set('occupationCodeId', value ?? '')

    const picked = occupationCodes.data?.items.find((code) => String(code.id) === value)
    if (picked) set('hazardClass', picked.hazardClass)
  }

  function submit() {
    const name = state.companyName.trim()
    setNameError(name ? undefined : t('validation.required'))
    setCityError(state.cityId ? undefined : t('validation.required'))
    // The district is not optional, whatever the nullable column suggests: CompanyManager refuses
    // a company without one. Saying so here beats a round trip that comes back as a server error.
    setDistrictError(state.districtId ? undefined : t('validation.required'))
    if (!name || !state.cityId || !state.districtId) return

    const input: CompanyInput = {
      companyName: name,
      ssiNumber: optionalText(state.ssiNumber),
      hazardClass: state.hazardClass,
      workplaceType: state.workplaceType,
      headquarterCompanyId:
        state.workplaceType === WorkplaceType.Branch
          ? optionalNumber(state.headquarterCompanyId)
          : null,
      cityId: Number(state.cityId),
      districtId: Number(state.districtId),
      address: optionalText(state.address),
      phone: optionalText(state.phone),
      email: optionalText(state.email),
      authorizedPerson: optionalText(state.authorizedPerson),
      taxOffice: optionalText(state.taxOffice),
      taxNumber: optionalText(state.taxNumber),
      officeId: optionalNumber(state.officeId),
      occupationCodeId: optionalNumber(state.occupationCodeId),
      notes: optionalText(state.notes),
      ...(isEdit ? { isActive: state.isActive } : {}),
    }

    if (isEdit && company) update.mutate({ id: company.id, input })
    else create.mutate(input)
  }

  return (
    <Modal
      title={isEdit ? t('company.form.editTitle') : t('company.form.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending.isPending}
      error={pending.error ? errorMessage(pending.error) : null}
      size="lg"
    >
      <div className="row g-3">
        <Input
          id="company-companyName"
          label={t('company.fields.companyName')}
          required
          error={nameError}
          className="col-md-8"
          value={state.companyName}
          onChange={(value) => set('companyName', value)}
        />

        <Input
          id="company-ssiNumber"
          label={t('company.fields.ssiNumber')}
          helpText={t('company.form.ssiNumberHint')}
          className="col-md-4"
          value={state.ssiNumber}
          onChange={(value) => set('ssiNumber', value)}
        />

        <Select<WorkplaceType>
          id="company-workplaceType"
          label={t('company.fields.workplaceType')}
          className="col-md-4"
          options={WORKPLACE_TYPES.map((value) => ({
            value,
            label: t(`enums.workplaceType.${value}`),
          }))}
          value={state.workplaceType}
          onChange={(value) => set('workplaceType', value ?? WorkplaceType.Headquarter)}
        />

        <Select
          id="company-headquarterCompanyId"
          label={t('company.fields.headquarterCompany')}
          helpText={t('company.form.headquarterHint')}
          className="col-md-8"
          disabled={state.workplaceType !== WorkplaceType.Branch}
          placeholder={t('common.none')}
          options={
            headquarters.data?.items.map((item) => ({
              value: String(item.id),
              label: item.displayName,
            })) ?? []
          }
          value={state.headquarterCompanyId === '' ? null : state.headquarterCompanyId}
          onChange={(value) => set('headquarterCompanyId', value ?? '')}
        />

        <Input
          id="company-occupationSearch"
          label={t('company.form.occupationSearch')}
          helpText={t('company.form.occupationSearchHint')}
          className="col-md-6"
          value={occupationSearch}
          onChange={setOccupationSearch}
        />

        <Select
          id="company-occupationCodeId"
          label={t('company.fields.occupationCode')}
          className="col-md-6"
          placeholder={t('common.none')}
          options={
            occupationCodes.data?.items.map((code) => ({
              value: String(code.id),
              label: `${code.tag} — ${code.displayName}`,
            })) ?? []
          }
          value={state.occupationCodeId === '' ? null : state.occupationCodeId}
          onChange={selectOccupationCode}
        />

        <Select<HazardClass>
          id="company-hazardClass"
          label={t('company.fields.hazardClass')}
          helpText={t('company.form.hazardClassHint')}
          className="col-md-6"
          options={HAZARD_CLASSES.map((value) => ({
            value,
            label: t(`enums.hazardClass.${value}`),
          }))}
          value={state.hazardClass}
          onChange={(value) => set('hazardClass', value ?? HazardClass.Unspecified)}
        />

        <Select
          id="company-officeId"
          label={t('company.fields.office')}
          className="col-md-6"
          placeholder={t('common.none')}
          options={
            offices.data?.items.map((office) => ({
              value: String(office.id),
              label: office.displayName,
            })) ?? []
          }
          value={state.officeId === '' ? null : state.officeId}
          onChange={(value) => set('officeId', value ?? '')}
        />

        <Select
          id="company-cityId"
          label={t('company.fields.city')}
          required
          error={cityError}
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
          id="company-districtId"
          label={t('company.fields.district')}
          required
          error={districtError}
          className="col-md-6"
          disabled={!state.cityId}
          placeholder={t('common.none')}
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
          id="company-address"
          label={t('company.fields.address')}
          className="col-12"
          rows={2}
          value={state.address}
          onChange={(value) => set('address', value)}
        />

        <Input
          id="company-authorizedPerson"
          label={t('company.fields.authorizedPerson')}
          className="col-md-4"
          value={state.authorizedPerson}
          onChange={(value) => set('authorizedPerson', value)}
        />

        <Input
          id="company-phone"
          label={t('company.fields.phone')}
          className="col-md-4"
          value={state.phone}
          onChange={(value) => set('phone', value)}
        />

        <Input
          id="company-email"
          type="email"
          label={t('company.fields.email')}
          className="col-md-4"
          value={state.email}
          onChange={(value) => set('email', value)}
        />

        <Input
          id="company-taxOffice"
          label={t('company.fields.taxOffice')}
          className="col-md-6"
          value={state.taxOffice}
          onChange={(value) => set('taxOffice', value)}
        />

        <Input
          id="company-taxNumber"
          label={t('company.fields.taxNumber')}
          className="col-md-6"
          value={state.taxNumber}
          onChange={(value) => set('taxNumber', value)}
        />

        <TextArea
          id="company-notes"
          label={t('company.fields.note')}
          className="col-12"
          rows={2}
          value={state.notes}
          onChange={(value) => set('notes', value)}
        />

        {isEdit && (
          <CheckBox
            id="company-isActive"
            label={t('company.fields.status')}
            className="col-12"
            checked={state.isActive}
            onChange={(checked) => set('isActive', checked)}
          />
        )}
      </div>
    </Modal>
  )
}
