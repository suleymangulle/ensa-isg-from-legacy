import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  CheckBox,
  Input,
  MultiSelect,
  NumberInput,
  Select,
  TextArea,
} from 'rich-react-component'
import { StaffRole, useLookup } from '@/api/endpoints'
import { useAuth } from '@/auth/AuthContext'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { Field, Modal } from '@/components/Form'
import {
  MEMBERSHIP_RESOURCES,
  fromDateInput,
  toDateInput,
  useCityLookup,
  useDistrictLookup,
  useRoleLookup,
  type CreateUserInput,
  type UpdateUserInput,
  type UserDto,
} from './api'

/** Staff roles offered by the form, in the order the administration screen lists them. */
const STAFF_ROLES: StaffRole[] = [
  StaffRole.Unspecified,
  StaffRole.OccupationalSafetySpecialist,
  StaffRole.WorkplacePhysician,
  StaffRole.OtherHealthPersonnel,
  StaffRole.OfficeStaff,
  StaffRole.Customer,
  StaffRole.OfficeAdministrator,
  StaffRole.OrganizationAdministrator,
  StaffRole.SystemAdministrator,
]

/** Editable shape of the form; every field of `UserInputDto` plus the create-only ones. */
interface FormState {
  userName: string
  password: string
  passwordRepeat: string
  roles: string[]
  name: string
  lastName: string
  email: string
  phoneNumber: string
  gsm: string
  nationalId: string
  address: string
  cityId: string
  districtId: string
  color: string
  staffRole: StaffRole
  hireDate: string
  terminationDate: string
  grossSalary: string
  partTime: boolean
  monthlyWorkDurationMinutes: string
  officeId: string
  officeAdmin: boolean
  companyId: string
  /** Create-only, host-only: the organization the user joins. */
  tenantId: string
  branchCode: string
  isActive: boolean
  /** Carried through untouched so an edit cannot silently clear it. */
  photoDocumentId: number | null
  permissionGroupId: number | null
}

function emptyState(): FormState {
  return {
    userName: '',
    password: '',
    passwordRepeat: '',
    roles: [],
    name: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    gsm: '',
    nationalId: '',
    address: '',
    cityId: '',
    districtId: '',
    color: '',
    staffRole: StaffRole.Unspecified,
    hireDate: '',
    terminationDate: '',
    grossSalary: '',
    partTime: false,
    monthlyWorkDurationMinutes: '',
    officeId: '',
    officeAdmin: false,
    companyId: '',
    tenantId: '',
    branchCode: '',
    isActive: true,
    photoDocumentId: null,
    permissionGroupId: null,
  }
}

/**
 * Seeds the form from the record being edited.
 *
 * `UpdateUserDto` is an absolute payload — the server maps it straight onto the entity — so
 * every field read back from `UserDto` has to be sent again, including the two the form does
 * not render (`photoDocumentId`, `permissionGroupId`).
 */
function stateFromUser(user: UserDto): FormState {
  return {
    ...emptyState(),
    userName: user.userName,
    name: user.name,
    lastName: user.lastName,
    email: user.email ?? '',
    phoneNumber: user.phoneNumber ?? '',
    gsm: user.gsm ?? '',
    address: user.address ?? '',
    cityId: user.cityId ? String(user.cityId) : '',
    districtId: user.districtId ? String(user.districtId) : '',
    color: user.color ?? '',
    staffRole: user.staffRole,
    hireDate: toDateInput(user.hireDate),
    terminationDate: toDateInput(user.terminationDate),
    grossSalary: user.grossSalary != null ? String(user.grossSalary) : '',
    partTime: user.partTime,
    monthlyWorkDurationMinutes:
      user.monthlyWorkDurationMinutes != null ? String(user.monthlyWorkDurationMinutes) : '',
    officeId: user.officeId ? String(user.officeId) : '',
    officeAdmin: user.officeAdmin,
    companyId: user.companyId ? String(user.companyId) : '',
    branchCode: user.branchCode ?? '',
    isActive: user.isActive,
    photoDocumentId: user.photoDocumentId ?? null,
    permissionGroupId: user.permissionGroupId ?? null,
  }
}

function optionalNumber(value: string): number | null {
  const parsed = Number(value)
  return value.trim() === '' || Number.isNaN(parsed) ? null : parsed
}

function optionalText(value: string): string | null {
  return value.trim() === '' ? null : value.trim()
}

function toPayload(state: FormState): UpdateUserInput {
  return {
    name: state.name.trim(),
    lastName: state.lastName.trim(),
    email: optionalText(state.email),
    phoneNumber: optionalText(state.phoneNumber),
    gsm: optionalText(state.gsm),
    nationalId: optionalText(state.nationalId),
    address: optionalText(state.address),
    cityId: optionalNumber(state.cityId),
    districtId: optionalNumber(state.districtId),
    photoDocumentId: state.photoDocumentId,
    color: optionalText(state.color),
    staffRole: state.staffRole,
    hireDate: fromDateInput(state.hireDate),
    terminationDate: fromDateInput(state.terminationDate),
    grossSalary: optionalNumber(state.grossSalary),
    partTime: state.partTime,
    monthlyWorkDurationMinutes: optionalNumber(state.monthlyWorkDurationMinutes),
    officeId: optionalNumber(state.officeId),
    officeAdmin: state.officeAdmin,
    companyId: optionalNumber(state.companyId),
    permissionGroupId: state.permissionGroupId,
    branchCode: optionalText(state.branchCode),
    isActive: state.isActive,
  }
}

interface UserFormModalProps {
  isOpen: boolean
  /** `undefined` opens the dialog in create mode. */
  user?: UserDto
  onClose: () => void
  onSaved: () => void
}

/**
 * Create / edit dialog for a user.
 *
 * The password appears only in create mode, only as a write-only input, and is never read back
 * from the server — changing an existing password goes through the administrative reset on the
 * detail page instead.
 */
export default function UserFormModal({ isOpen, user, onClose, onSaved }: UserFormModalProps) {
  const { t } = useTranslation()
  const isEdit = !!user

  const [state, setState] = useState<FormState>(() => (user ? stateFromUser(user) : emptyState()))
  const [errors, setErrors] = useState<Record<string, string>>({})

  const cities = useCityLookup()
  const districts = useDistrictLookup(optionalNumber(state.cityId))
  const offices = useLookup('office')
  const companies = useLookup('company')
  const roles = useRoleLookup()

  // Only a host administrator picks an organization. Everybody else is already inside one, and
  // the server would overwrite the value with their own organization regardless.
  const { user: signedInUser } = useAuth()
  const isHostCaller = signedInUser?.tenantId == null
  const organizations = useLookup('organization')

  const create = useCreate<CreateUserInput>(MEMBERSHIP_RESOURCES.user, { onSuccess: onSaved })
  const update = useUpdate<UpdateUserInput>(MEMBERSHIP_RESOURCES.user, { onSuccess: onSaved })
  const pending = isEdit ? update : create

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setState((previous) => ({ ...previous, [key]: value }))
  }

  function validate(): boolean {
    const next: Record<string, string> = {}
    if (!state.name.trim()) next.name = t('validation.required')
    if (!state.lastName.trim()) next.lastName = t('validation.required')

    if (!isEdit) {
      if (!state.userName.trim()) next.userName = t('validation.required')
      if (isHostCaller && !state.tenantId) next.tenantId = t('user.form.organizationRequired')
      if (state.password.length < 6) next.password = t('user.form.passwordTooShort')
      if (state.password !== state.passwordRepeat) {
        next.passwordRepeat = t('user.form.passwordMismatch')
      }
    }

    setErrors(next)
    return Object.keys(next).length === 0
  }

  function submit() {
    if (!validate()) return

    if (isEdit && user) {
      update.mutate({ id: user.id, input: toPayload(state) })
      return
    }

    create.mutate({
      ...toPayload(state),
      userName: state.userName.trim(),
      password: state.password,
      roles: state.roles,
      tenantId: isHostCaller ? optionalNumber(state.tenantId) : undefined,
    })
  }

  return (
    <Modal
      title={isEdit ? t('user.form.editTitle') : t('user.form.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending.isPending}
      error={pending.error ? errorMessage(pending.error) : null}
      size="xl"
    >
      <div className="row g-3">
        <div className="col-12">
          <h3 className="h6 fw-bold mb-0" style={{ color: 'var(--kt-gray-700)' }}>
            {t('user.form.sections.identity')}
          </h3>
        </div>

        {!isEdit && (
          <Input
            id="user-userName"
            label={t('user.fields.userName')}
            required
            error={errors.userName}
            helpText={t('user.form.userNameHint')}
            className="col-md-4"
            value={state.userName}
            inputProps={{ autoComplete: 'off' }}
            onChange={(value) => set('userName', value)}
          />
        )}

        <Input
          id="user-name"
          label={t('user.fields.name')}
          required
          error={errors.name}
          className="col-md-4"
          value={state.name}
          onChange={(value) => set('name', value)}
        />

        <Input
          id="user-lastName"
          label={t('user.fields.lastName')}
          required
          error={errors.lastName}
          className="col-md-4"
          value={state.lastName}
          onChange={(value) => set('lastName', value)}
        />

        <Select
          id="user-staffRole"
          label={t('user.fields.staffRole')}
          className="col-md-4"
          options={STAFF_ROLES.map((role) => ({ value: role, label: t(`enums.staffRole.${role}`) }))}
          value={state.staffRole}
          onChange={(value) => set('staffRole', value ?? StaffRole.Unspecified)}
        />

        {!isEdit && isHostCaller && (
          <Select
            id="user-tenantId"
            label={t('user.fields.organization')}
            required
            error={errors.tenantId}
            helpText={t('user.form.organizationHint')}
            className="col-md-4"
            placeholder={t('common.none')}
            options={
              organizations.data?.items.map((organization) => ({
                value: String(organization.id),
                label: organization.displayName,
              })) ?? []
            }
            value={state.tenantId === '' ? null : state.tenantId}
            onChange={(value) => set('tenantId', value ?? '')}
          />
        )}

        <Select
          id="user-isActive"
          label={t('user.fields.status')}
          className="col-md-4"
          options={[
            { value: 'true', label: t('common.active') },
            { value: 'false', label: t('common.passive') },
          ]}
          value={state.isActive ? 'true' : 'false'}
          onChange={(value) => set('isActive', value === 'true')}
        />

        {!isEdit && (
          <>
            <div className="col-12">
              <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-700)' }}>
                {t('user.form.sections.credentials')}
              </h3>
              <p className="mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
                {t('user.form.credentialsHint')}
              </p>
            </div>

            <Input
              id="user-password"
              type="password"
              label={t('user.form.initialPassword')}
              required
              error={errors.password}
              className="col-md-4"
              inputProps={{ autoComplete: 'new-password' }}
              value={state.password}
              onChange={(value) => set('password', value)}
            />

            <Input
              id="user-passwordRepeat"
              type="password"
              label={t('user.form.passwordRepeat')}
              required
              error={errors.passwordRepeat}
              className="col-md-4"
              inputProps={{ autoComplete: 'new-password' }}
              value={state.passwordRepeat}
              onChange={(value) => set('passwordRepeat', value)}
            />

            <MultiSelect
              id="user-roles"
              label={t('user.form.roles')}
              helpText={t('user.form.rolesHint')}
              className="col-md-4"
              options={(roles.data?.items ?? []).map((role) => ({
                value: role.displayName,
                label: role.displayName,
              }))}
              values={state.roles}
              onChange={(values) => set('roles', values)}
            />
          </>
        )}

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('user.form.sections.contact')}
          </h3>
        </div>

        <Input
          id="user-email"
          type="email"
          label={t('user.fields.email')}
          className="col-md-4"
          value={state.email}
          onChange={(value) => set('email', value)}
        />

        <Input
          id="user-phoneNumber"
          label={t('user.fields.phoneNumber')}
          className="col-md-4"
          value={state.phoneNumber}
          onChange={(value) => set('phoneNumber', value)}
        />

        <Input
          id="user-gsm"
          label={t('user.fields.gsm')}
          className="col-md-4"
          value={state.gsm}
          onChange={(value) => set('gsm', value)}
        />

        <Input
          id="user-nationalId"
          label={t('user.fields.nationalId')}
          helpText={isEdit ? t('user.form.nationalIdEditHint') : undefined}
          className="col-md-4"
          inputProps={{ inputMode: 'numeric' }}
          value={state.nationalId}
          onChange={(value) => set('nationalId', value)}
        />

        <Select
          id="user-cityId"
          label={t('user.fields.city')}
          className="col-md-4"
          placeholder={t('common.none')}
          options={
            cities.data?.items.map((city) => ({ value: String(city.id), label: city.displayName })) ??
            []
          }
          value={state.cityId === '' ? null : state.cityId}
          onChange={(value) => {
            set('cityId', value ?? '')
            set('districtId', '')
          }}
        />

        <Select
          id="user-districtId"
          label={t('user.fields.district')}
          className="col-md-4"
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
          id="user-address"
          label={t('user.fields.address')}
          rows={2}
          className="col-12"
          value={state.address}
          onChange={(value) => set('address', value)}
        />

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('user.form.sections.employment')}
          </h3>
        </div>

        <Select
          id="user-officeId"
          label={t('user.fields.office')}
          className="col-md-4"
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
          id="user-companyId"
          label={t('user.fields.company')}
          className="col-md-4"
          placeholder={t('common.none')}
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
          id="user-branchCode"
          label={t('user.fields.branchCode')}
          className="col-md-4"
          value={state.branchCode}
          onChange={(value) => set('branchCode', value)}
        />

        <Input
          id="user-hireDate"
          label={t('user.fields.hireDate')}
          className="col-md-4"
          value={state.hireDate}
          inputProps={{ type: 'date' }}
          onChange={(value) => set('hireDate', value)}
        />

        <Input
          id="user-terminationDate"
          label={t('user.fields.terminationDate')}
          className="col-md-4"
          value={state.terminationDate}
          inputProps={{ type: 'date' }}
          onChange={(value) => set('terminationDate', value)}
        />

        <NumberInput
          id="user-monthlyWorkDurationMinutes"
          label={t('user.fields.monthlyWorkDuration')}
          helpText={t('user.form.minutesHint')}
          className="col-md-4"
          min={0}
          value={state.monthlyWorkDurationMinutes === '' ? null : Number(state.monthlyWorkDurationMinutes)}
          onChange={(value) => set('monthlyWorkDurationMinutes', value === null ? '' : String(value))}
        />

        <NumberInput
          id="user-grossSalary"
          label={t('user.fields.grossSalary')}
          className="col-md-4"
          min={0}
          step={0.01}
          value={state.grossSalary === '' ? null : Number(state.grossSalary)}
          onChange={(value) => set('grossSalary', value === null ? '' : String(value))}
        />

        {/* No library equivalent for a native color swatch input; kept raw. */}
        <Field label={t('user.fields.color')} htmlFor="user-color" className="col-md-4">
          <input
            id="user-color"
            type="color"
            className="form-control form-control-color"
            value={state.color || '#3e97ff'}
            onChange={(event) => set('color', event.target.value)}
          />
        </Field>

        <div className="col-md-4 d-flex align-items-end">
          <div className="d-flex flex-column gap-2 pb-2">
            <CheckBox
              id="user-partTime"
              checked={state.partTime}
              onChange={(value) => set('partTime', value)}
              label={t('user.fields.partTime')}
            />
            <CheckBox
              id="user-officeAdmin"
              checked={state.officeAdmin}
              onChange={(value) => set('officeAdmin', value)}
              label={t('user.fields.officeAdmin')}
            />
          </div>
        </div>

        <div className="col-12">
          <p className="mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
            {t('user.form.elevationNote')}
          </p>
        </div>
      </div>
    </Modal>
  )
}
