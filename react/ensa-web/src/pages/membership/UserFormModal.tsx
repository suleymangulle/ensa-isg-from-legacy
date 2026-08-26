import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { StaffRole, useLookup } from '@/api/endpoints'
import { useAuth } from '@/auth/AuthContext'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { Field, Modal, controlClass } from '@/components/Form'
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
          <Field
            label={t('user.fields.userName')}
            htmlFor="user-userName"
            required
            error={errors.userName}
            hint={t('user.form.userNameHint')}
            className="col-md-4"
          >
            <input
              id="user-userName"
              className={controlClass('form-control', errors.userName)}
              value={state.userName}
              autoComplete="off"
              onChange={(event) => set('userName', event.target.value)}
            />
          </Field>
        )}

        <Field
          label={t('user.fields.name')}
          htmlFor="user-name"
          required
          error={errors.name}
          className="col-md-4"
        >
          <input
            id="user-name"
            className={controlClass('form-control', errors.name)}
            value={state.name}
            onChange={(event) => set('name', event.target.value)}
          />
        </Field>

        <Field
          label={t('user.fields.lastName')}
          htmlFor="user-lastName"
          required
          error={errors.lastName}
          className="col-md-4"
        >
          <input
            id="user-lastName"
            className={controlClass('form-control', errors.lastName)}
            value={state.lastName}
            onChange={(event) => set('lastName', event.target.value)}
          />
        </Field>

        <Field label={t('user.fields.staffRole')} htmlFor="user-staffRole" className="col-md-4">
          <select
            id="user-staffRole"
            className="form-select"
            value={state.staffRole}
            onChange={(event) => set('staffRole', Number(event.target.value) as StaffRole)}
          >
            {STAFF_ROLES.map((role) => (
              <option key={role} value={role}>
                {t(`enums.staffRole.${role}`)}
              </option>
            ))}
          </select>
        </Field>

        {!isEdit && isHostCaller && (
          <Field
            label={t('user.fields.organization')}
            htmlFor="user-tenantId"
            required
            error={errors.tenantId}
            hint={t('user.form.organizationHint')}
            className="col-md-4"
          >
            <select
              id="user-tenantId"
              className={controlClass('form-select', errors.tenantId)}
              value={state.tenantId}
              onChange={(event) => set('tenantId', event.target.value)}
            >
              <option value="">{t('common.none')}</option>
              {organizations.data?.items.map((organization) => (
                <option key={organization.id} value={organization.id}>
                  {organization.displayName}
                </option>
              ))}
            </select>
          </Field>
        )}

        <Field label={t('user.fields.status')} htmlFor="user-isActive" className="col-md-4">
          <select
            id="user-isActive"
            className="form-select"
            value={state.isActive ? 'true' : 'false'}
            onChange={(event) => set('isActive', event.target.value === 'true')}
          >
            <option value="true">{t('common.active')}</option>
            <option value="false">{t('common.passive')}</option>
          </select>
        </Field>

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

            <Field
              label={t('user.form.initialPassword')}
              htmlFor="user-password"
              required
              error={errors.password}
              className="col-md-4"
            >
              <input
                id="user-password"
                type="password"
                autoComplete="new-password"
                className={controlClass('form-control', errors.password)}
                value={state.password}
                onChange={(event) => set('password', event.target.value)}
              />
            </Field>

            <Field
              label={t('user.form.passwordRepeat')}
              htmlFor="user-passwordRepeat"
              required
              error={errors.passwordRepeat}
              className="col-md-4"
            >
              <input
                id="user-passwordRepeat"
                type="password"
                autoComplete="new-password"
                className={controlClass('form-control', errors.passwordRepeat)}
                value={state.passwordRepeat}
                onChange={(event) => set('passwordRepeat', event.target.value)}
              />
            </Field>

            <Field
              label={t('user.form.roles')}
              htmlFor="user-roles"
              hint={t('user.form.rolesHint')}
              className="col-md-4"
            >
              <select
                id="user-roles"
                multiple
                className="form-select"
                size={4}
                value={state.roles}
                onChange={(event) =>
                  set(
                    'roles',
                    Array.from(event.target.selectedOptions).map((option) => option.value),
                  )
                }
              >
                {roles.data?.items.map((role) => (
                  <option key={role.id} value={role.displayName}>
                    {role.displayName}
                  </option>
                ))}
              </select>
            </Field>
          </>
        )}

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('user.form.sections.contact')}
          </h3>
        </div>

        <Field label={t('user.fields.email')} htmlFor="user-email" className="col-md-4">
          <input
            id="user-email"
            type="email"
            className="form-control"
            value={state.email}
            onChange={(event) => set('email', event.target.value)}
          />
        </Field>

        <Field label={t('user.fields.phoneNumber')} htmlFor="user-phoneNumber" className="col-md-4">
          <input
            id="user-phoneNumber"
            className="form-control"
            value={state.phoneNumber}
            onChange={(event) => set('phoneNumber', event.target.value)}
          />
        </Field>

        <Field label={t('user.fields.gsm')} htmlFor="user-gsm" className="col-md-4">
          <input
            id="user-gsm"
            className="form-control"
            value={state.gsm}
            onChange={(event) => set('gsm', event.target.value)}
          />
        </Field>

        <Field
          label={t('user.fields.nationalId')}
          htmlFor="user-nationalId"
          hint={isEdit ? t('user.form.nationalIdEditHint') : undefined}
          className="col-md-4"
        >
          <input
            id="user-nationalId"
            className="form-control"
            inputMode="numeric"
            value={state.nationalId}
            onChange={(event) => set('nationalId', event.target.value)}
          />
        </Field>

        <Field label={t('user.fields.city')} htmlFor="user-cityId" className="col-md-4">
          <select
            id="user-cityId"
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

        <Field label={t('user.fields.district')} htmlFor="user-districtId" className="col-md-4">
          <select
            id="user-districtId"
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

        <Field label={t('user.fields.address')} htmlFor="user-address" className="col-12">
          <textarea
            id="user-address"
            className="form-control"
            rows={2}
            value={state.address}
            onChange={(event) => set('address', event.target.value)}
          />
        </Field>

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('user.form.sections.employment')}
          </h3>
        </div>

        <Field label={t('user.fields.office')} htmlFor="user-officeId" className="col-md-4">
          <select
            id="user-officeId"
            className="form-select"
            value={state.officeId}
            onChange={(event) => set('officeId', event.target.value)}
          >
            <option value="">{t('common.none')}</option>
            {offices.data?.items.map((office) => (
              <option key={office.id} value={office.id}>
                {office.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('user.fields.company')} htmlFor="user-companyId" className="col-md-4">
          <select
            id="user-companyId"
            className="form-select"
            value={state.companyId}
            onChange={(event) => set('companyId', event.target.value)}
          >
            <option value="">{t('common.none')}</option>
            {companies.data?.items.map((company) => (
              <option key={company.id} value={company.id}>
                {company.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('user.fields.branchCode')} htmlFor="user-branchCode" className="col-md-4">
          <input
            id="user-branchCode"
            className="form-control"
            value={state.branchCode}
            onChange={(event) => set('branchCode', event.target.value)}
          />
        </Field>

        <Field label={t('user.fields.hireDate')} htmlFor="user-hireDate" className="col-md-4">
          <input
            id="user-hireDate"
            type="date"
            className="form-control"
            value={state.hireDate}
            onChange={(event) => set('hireDate', event.target.value)}
          />
        </Field>

        <Field
          label={t('user.fields.terminationDate')}
          htmlFor="user-terminationDate"
          className="col-md-4"
        >
          <input
            id="user-terminationDate"
            type="date"
            className="form-control"
            value={state.terminationDate}
            onChange={(event) => set('terminationDate', event.target.value)}
          />
        </Field>

        <Field
          label={t('user.fields.monthlyWorkDuration')}
          htmlFor="user-monthlyWorkDurationMinutes"
          hint={t('user.form.minutesHint')}
          className="col-md-4"
        >
          <input
            id="user-monthlyWorkDurationMinutes"
            type="number"
            min={0}
            className="form-control"
            value={state.monthlyWorkDurationMinutes}
            onChange={(event) => set('monthlyWorkDurationMinutes', event.target.value)}
          />
        </Field>

        <Field label={t('user.fields.grossSalary')} htmlFor="user-grossSalary" className="col-md-4">
          <input
            id="user-grossSalary"
            type="number"
            min={0}
            step="0.01"
            className="form-control"
            value={state.grossSalary}
            onChange={(event) => set('grossSalary', event.target.value)}
          />
        </Field>

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
            <div className="form-check">
              <input
                id="user-partTime"
                type="checkbox"
                className="form-check-input"
                checked={state.partTime}
                onChange={(event) => set('partTime', event.target.checked)}
              />
              <label className="form-check-label" htmlFor="user-partTime">
                {t('user.fields.partTime')}
              </label>
            </div>
            <div className="form-check">
              <input
                id="user-officeAdmin"
                type="checkbox"
                className="form-check-input"
                checked={state.officeAdmin}
                onChange={(event) => set('officeAdmin', event.target.checked)}
              />
              <label className="form-check-label" htmlFor="user-officeAdmin">
                {t('user.fields.officeAdmin')}
              </label>
            </div>
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
