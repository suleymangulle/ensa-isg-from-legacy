import { useEffect, useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { LookupDto } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import {
  REFERENCE_MIN_TERM_LENGTH,
  useIcd10Search,
  useMedicationDoseUnits,
  useMedicationFrequencyUnits,
  useMedicationRoutes,
  useMedicationSearch,
  type Icd10LookupDto,
  type MedicationLookupDto,
} from '../api'

/**
 * Searchable pickers over the read-only SKRS reference catalogue.
 *
 * The catalogues are large, so both search endpoints require a term. The input is debounced
 * rather than queried on every keystroke, and nothing is fetched until the term is long
 * enough — a per-character request against a national code catalogue is both slow and a
 * needless load on the API.
 */

/** Milliseconds a picker waits after the last keystroke before it searches. */
const DEBOUNCE_MS = 350

/** Returns `value` only once it has stopped changing for `delay` milliseconds. */
export function useDebouncedValue<T>(value: T, delay = DEBOUNCE_MS): T {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delay)
    return () => window.clearTimeout(timer)
  }, [value, delay])

  return debounced
}

interface SearchShellProps {
  label: string
  placeholder: string
  hint: string
  term: string
  onTermChange: (next: string) => void
  isLoading: boolean
  error: unknown
  children: React.ReactNode
}

/** Shared chrome of a search picker: labelled input, hint, busy and error states. */
function SearchShell({
  label,
  placeholder,
  hint,
  term,
  onTermChange,
  isLoading,
  error,
  children,
}: SearchShellProps) {
  const { t } = useTranslation()
  const inputId = useId()
  const isTermTooShort = term.trim().length < REFERENCE_MIN_TERM_LENGTH

  return (
    <div>
      <label htmlFor={inputId} className="form-label fw-semibold">
        {label}
      </label>
      <input
        id={inputId}
        type="search"
        className="form-control"
        value={term}
        placeholder={placeholder}
        onChange={(event) => onTermChange(event.target.value)}
      />

      {isTermTooShort ? (
        <div className="form-text" style={{ color: 'var(--kt-gray-500)' }}>
          {hint}
        </div>
      ) : error ? (
        <div className="form-text" style={{ color: 'var(--kt-danger)' }} role="alert">
          {errorMessage(error)}
        </div>
      ) : isLoading ? (
        <div className="form-text" style={{ color: 'var(--kt-gray-500)' }}>
          {t('common.loading')}
        </div>
      ) : (
        children
      )}
    </div>
  )
}

/** Scrolling result list shared by both search pickers. */
function ResultList({ children, isEmpty }: { children: React.ReactNode; isEmpty: boolean }) {
  const { t } = useTranslation()

  if (isEmpty) {
    return (
      <div className="form-text" style={{ color: 'var(--kt-gray-500)' }}>
        {t('table.empty')}
      </div>
    )
  }

  return (
    <ul
      className="list-unstyled mt-2 mb-0 border rounded"
      style={{ maxHeight: 220, overflowY: 'auto' }}
    >
      {children}
    </ul>
  )
}

/** One selectable row of a picker result list. */
function ResultRow({
  onSelect,
  ariaLabel,
  children,
}: {
  onSelect: () => void
  ariaLabel: string
  children: React.ReactNode
}) {
  return (
    <li>
      <button
        type="button"
        className="btn btn-link text-decoration-none text-start w-100 px-3 py-2"
        style={{ color: 'var(--kt-gray-800)' }}
        aria-label={ariaLabel}
        onClick={onSelect}
      >
        {children}
      </button>
    </li>
  )
}

/** ICD-10 diagnosis picker — searches by code or name fragment. */
export function Icd10Picker({ onSelect }: { onSelect: (diagnosis: Icd10LookupDto) => void }) {
  const { t } = useTranslation()
  const [term, setTerm] = useState('')
  const debouncedTerm = useDebouncedValue(term)
  const { data, isLoading, error } = useIcd10Search(debouncedTerm)

  return (
    <SearchShell
      label={t('medicalReference.icd10.label')}
      placeholder={t('medicalReference.icd10.placeholder')}
      hint={t('medicalReference.minTerm', { count: REFERENCE_MIN_TERM_LENGTH })}
      term={term}
      onTermChange={setTerm}
      isLoading={isLoading}
      error={error}
    >
      <ResultList isEmpty={!data?.items.length}>
        {data?.items.map((item) => (
          <ResultRow
            key={item.id}
            ariaLabel={t('medicalReference.icd10.select', { code: item.code, name: item.name })}
            onSelect={() => {
              onSelect(item)
              setTerm('')
            }}
          >
            <span className="badge-light-primary me-2">{item.code}</span>
            {item.name}
          </ResultRow>
        ))}
      </ResultList>
    </SearchShell>
  )
}

/** Medication picker — searches the SKRS catalogue by exact barcode or name fragment. */
export function MedicationPicker({
  onSelect,
}: {
  onSelect: (medication: MedicationLookupDto) => void
}) {
  const { t } = useTranslation()
  const [term, setTerm] = useState('')
  const debouncedTerm = useDebouncedValue(term)
  const { data, isLoading, error } = useMedicationSearch(debouncedTerm)

  return (
    <SearchShell
      label={t('medicalReference.medication.label')}
      placeholder={t('medicalReference.medication.placeholder')}
      hint={t('medicalReference.minTerm', { count: REFERENCE_MIN_TERM_LENGTH })}
      term={term}
      onTermChange={setTerm}
      isLoading={isLoading}
      error={error}
    >
      <ResultList isEmpty={!data?.items.length}>
        {data?.items.map((item) => (
          <ResultRow
            key={item.id}
            ariaLabel={t('medicalReference.medication.select', { name: item.medicationName })}
            onSelect={() => {
              onSelect(item)
              setTerm('')
            }}
          >
            <span className="fw-semibold d-block">{item.medicationName}</span>
            <small style={{ color: 'var(--kt-gray-500)' }}>
              {[item.barcode, item.atcCode, item.generatorCompanyName].filter(Boolean).join(' · ')}
            </small>
          </ResultRow>
        ))}
      </ResultList>
    </SearchShell>
  )
}

interface ReferenceSelectProps {
  id: string
  label: string
  value: number
  onChange: (next: number) => void
  items: LookupDto[] | undefined
  isLoading?: boolean
  disabled?: boolean
  className?: string
}

/** Drop-down over one of the three fixed SKRS code lists. */
export function ReferenceSelect({
  id,
  label,
  value,
  onChange,
  items,
  isLoading,
  disabled,
  className,
}: ReferenceSelectProps) {
  const { t } = useTranslation()

  return (
    <div className={className ?? 'col-12'}>
      <label htmlFor={id} className="form-label fw-semibold">
        {label}
      </label>
      <select
        id={id}
        className="form-select"
        value={value || ''}
        disabled={disabled || isLoading}
        onChange={(event) => onChange(Number(event.target.value))}
      >
        <option value="">{isLoading ? t('common.loading') : t('common.none')}</option>
        {items?.map((item) => (
          <option key={item.id} value={item.id}>
            {item.displayName}
          </option>
        ))}
      </select>
    </div>
  )
}

/** The three medication code lists, fetched once and shared by every medication line. */
export function useMedicationCodeLists() {
  const routes = useMedicationRoutes()
  const doseUnits = useMedicationDoseUnits()
  const frequencyUnits = useMedicationFrequencyUnits()

  return {
    routes: routes.data?.items,
    doseUnits: doseUnits.data?.items,
    frequencyUnits: frequencyUnits.data?.items,
    isLoading: routes.isLoading || doseUnits.isLoading || frequencyUnits.isLoading,
  }
}
