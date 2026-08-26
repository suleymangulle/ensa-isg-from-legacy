import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { LookupDto } from '@/api/endpoints'
import { useDebouncedValue } from './ReferencePickers'

/**
 * Company / employee / physician picker.
 *
 * The lookup endpoints cap their result set, so the picker pairs a debounced search box with
 * the drop-down instead of trying to load every record. The currently selected record is kept
 * as an extra option so a selection made under one search term survives the next one.
 */
interface LookupPickerProps {
  id: string
  label: string
  value: number | null
  /** Display name of the current selection, so it stays visible when the search changes. */
  selectedName?: string | null
  onChange: (id: number | null, displayName: string | null) => void
  /** Called with the debounced term; the caller runs the lookup query. */
  onSearch: (term: string) => void
  items: LookupDto[] | undefined
  isLoading?: boolean
  required?: boolean
  disabled?: boolean
  error?: string
  className?: string
  searchPlaceholder: string
}

export default function LookupPicker({
  id,
  label,
  value,
  selectedName,
  onChange,
  onSearch,
  items,
  isLoading,
  required,
  disabled,
  error,
  className,
  searchPlaceholder,
}: LookupPickerProps) {
  const { t } = useTranslation()
  const [term, setTerm] = useState('')
  const debouncedTerm = useDebouncedValue(term)

  // The parent owns the query, so the debounced term is pushed up rather than fetched here.
  useEffect(() => {
    onSearch(debouncedTerm)
    // `onSearch` is a plain setter from the caller; re-running on identity changes would loop.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedTerm])

  const options = items ?? []
  const isSelectionMissing = value != null && !options.some((item) => item.id === value)

  return (
    <div className={className ?? 'col-12'}>
      <label htmlFor={id} className="form-label fw-semibold">
        {label}
        {required && (
          <span style={{ color: 'var(--kt-danger)' }} aria-hidden="true">
            {' *'}
          </span>
        )}
      </label>

      <input
        type="search"
        className="form-control form-control-sm mb-2"
        value={term}
        placeholder={searchPlaceholder}
        disabled={disabled}
        aria-label={searchPlaceholder}
        onChange={(event) => setTerm(event.target.value)}
      />

      <select
        id={id}
        className={error ? 'form-select is-invalid' : 'form-select'}
        value={value ?? ''}
        disabled={disabled}
        onChange={(event) => {
          const nextId = event.target.value ? Number(event.target.value) : null
          const match = options.find((item) => item.id === nextId)
          onChange(nextId, match?.displayName ?? null)
        }}
      >
        <option value="">{isLoading ? t('common.loading') : t('common.none')}</option>
        {isSelectionMissing && (
          <option value={value}>{selectedName ?? `#${value}`}</option>
        )}
        {options.map((item) => (
          <option key={item.id} value={item.id}>
            {item.displayName}
          </option>
        ))}
      </select>

      {error && (
        <div className="invalid-feedback d-block" role="alert">
          {error}
        </div>
      )}
    </div>
  )
}
