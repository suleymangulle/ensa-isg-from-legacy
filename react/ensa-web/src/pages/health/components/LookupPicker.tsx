import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Input, Select } from 'rich-react-component'
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
      <Input
        type="search"
        value={term}
        placeholder={searchPlaceholder}
        disabled={disabled}
        className="mb-2"
        inputProps={{ 'aria-label': searchPlaceholder }}
        onChange={setTerm}
      />

      <Select<number>
        id={id}
        label={label}
        required={required}
        error={error}
        disabled={disabled}
        value={value}
        placeholder={isLoading ? t('common.loading') : t('common.none')}
        options={[
          ...(isSelectionMissing && value != null
            ? [{ value, label: selectedName ?? `#${value}` }]
            : []),
          ...options.map((item) => ({ value: item.id, label: item.displayName })),
        ]}
        onChange={(nextId) => {
          const match = options.find((item) => item.id === nextId)
          onChange(nextId, match?.displayName ?? null)
        }}
      />
    </div>
  )
}
