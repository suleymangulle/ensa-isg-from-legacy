import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Input } from 'rich-react-component'
import type { IncidentPersonRole } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { Modal } from '@/components/Form'
import { useAddIncidentPerson, useEmployeeLookup, type CreateIncidentPersonDto } from './api'
import { LookupField } from './components'

/** Splits a lookup display name into a first name and a last name. */
function splitFullName(displayName: string): { name: string; lastName: string } {
  const parts = displayName.trim().split(/\s+/)
  if (parts.length < 2) return { name: displayName.trim(), lastName: '' }
  return { name: parts.slice(0, -1).join(' '), lastName: parts[parts.length - 1] }
}

/**
 * Adds one person to an incident, in the role of the section the dialog was opened from.
 *
 * There is no update endpoint for incident persons (the controller exposes POST and DELETE only),
 * so a correction is made by removing the row and adding it again.
 */
export default function IncidentPersonModal({
  incidentId,
  companyId,
  role,
  onClose,
}: {
  incidentId: number
  companyId: number
  role: IncidentPersonRole
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [employeeId, setEmployeeId] = useState<number | undefined>(undefined)
  const [name, setName] = useState('')
  const [lastName, setLastName] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})

  const employees = useEmployeeLookup(companyId)
  const add = useAddIncidentPerson(incidentId, onClose)

  function submit() {
    const found: Record<string, string> = {}
    if (!name.trim()) found.name = t('validation.required')
    if (!lastName.trim()) found.lastName = t('validation.required')
    setErrors(found)
    if (Object.keys(found).length) return

    const payload: CreateIncidentPersonDto = {
      personType: role,
      companyEmployeeId: employeeId ?? null,
      name: name.trim(),
      lastName: lastName.trim(),
    }
    add.mutate(payload)
  }

  return (
    <Modal
      title={t(`incident.persons.add.${role}`)}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={add.isPending}
      error={add.error ? errorMessage(add.error) : null}
    >
      <div className="row g-3">
        <LookupField
          id="incident-person-employee"
          label={t('incident.persons.employee')}
          placeholder={t('observations.selectEmployee')}
          hint={t('incident.persons.employeeHint')}
          items={employees.data?.items}
          isLoading={employees.isLoading}
          value={employeeId}
          onChange={(next) => {
            setEmployeeId(next)
            const match = employees.data?.items.find((item) => item.id === next)
            if (match) {
              const split = splitFullName(match.displayName)
              setName(split.name)
              setLastName(split.lastName)
            }
          }}
        />

        <Input
          id="incident-person-name"
          label={t('incident.persons.name')}
          required
          error={errors.name}
          className="col-md-6"
          value={name}
          onChange={setName}
        />

        <Input
          id="incident-person-last-name"
          label={t('incident.persons.lastName')}
          required
          error={errors.lastName}
          className="col-md-6"
          value={lastName}
          onChange={setLastName}
        />
      </div>
    </Modal>
  )
}
