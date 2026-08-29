import {
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type CSSProperties,
  type KeyboardEvent,
  type RefObject,
} from 'react'
import { useTranslation } from 'react-i18next'
import { Spinner, Text } from 'rich-react-component'
import { ALL_OFFICES, type OfficeScopeValue } from '@/auth/officeStore'
import { useOffice } from '@/auth/OfficeContext'
import { ErrorPanel } from '@/components/DataTable'
import { errorMessage } from '@/api/http'

/**
 * The office list the switcher opens.
 *
 * A listbox rather than a menu: the user is picking a **value** that stays selected, not firing a
 * command, and that difference is what a screen reader announces — `aria-selected` on the active
 * row, and the whole list described as a listbox. The library's `Menu` and `Popover` are the other
 * shape (commands, and only `top`/`bottom` placement), which is why this is drawn here instead.
 *
 * Placement is a prop rather than a media query: the shell already knows which of its three
 * arrangements it is in, and deriving it again here would put the breakpoint in two places.
 */
export type OfficePanelPlacement = 'top' | 'right' | 'bottom'

export interface OfficeSwitcherPanelProps {
  placement: OfficePanelPlacement
  onClose: () => void
  /** Id of the element the trigger points `aria-controls` at. */
  id: string
  labelledBy: string
  /** The trigger the panel is positioned against. */
  anchorRef: RefObject<HTMLButtonElement | null>
  /**
   * Called after the office actually changed — not when the panel merely closes. The drawer uses it
   * to get out of the way, the way it already does when a menu entry is followed.
   */
  onSelected?: () => void
}

/** Gap between the trigger and the panel. */
const OFFSET = 6

/**
 * Below this much room, the requested side is not worth using.
 *
 * Roughly two rows and the panel's own padding: less than that and the list is a scrollbar with a
 * sliver of content in it, which is worse than opening the other way.
 */
const MINIMUM_SPACE = 140

/**
 * Positions the panel against its trigger, in viewport coordinates.
 *
 * It has to be `position: fixed` rather than absolute, and that is not a preference. The library's
 * collapsed rail keeps `overflow: hidden` on `.rrc-sidebar__panel` — that is how it hides the
 * labels — so an absolutely positioned panel inside the footer is clipped out of sight, and worse:
 * moving focus into it makes the browser scroll that hidden box sideways, dragging the whole rail's
 * contents off screen with it. The library solves the same problem the same way for its own
 * submenu flyout (`.rrc-sidebar__flyout` is `position: fixed`).
 *
 * Measured on open and on resize only. Nothing here animates — the sidebar's own CSS transition
 * still owns the movement, and a panel is closed while the rail is being collapsed anyway.
 */
function usePanelPosition(
  anchorRef: RefObject<HTMLButtonElement | null>,
  placement: OfficePanelPlacement,
): CSSProperties | undefined {
  const [style, setStyle] = useState<CSSProperties>()

  useLayoutEffect(() => {
    function place() {
      const anchor = anchorRef.current
      if (!anchor) return

      const rect = anchor.getBoundingClientRect()

      const viewportHeight = window.innerHeight

      if (placement === 'right') {
        // Measured from the rail's edge rather than the trigger's: the icon is centred in a 76px
        // column, so anchoring to it would leave the panel overlapping the menu it belongs beside.
        const rail = anchor.closest('.rrc-sidebar')?.getBoundingClientRect()

        setStyle({
          position: 'fixed',
          insetInlineStart: (rail?.right ?? rect.right) + OFFSET,
          // Bottom-aligned to the trigger, so a long list grows up into the viewport rather than
          // off the bottom of it.
          insetBlockEnd: Math.max(OFFSET, viewportHeight - rect.bottom),
          maxBlockSize: Math.max(MINIMUM_SPACE, rect.bottom - OFFSET),
        })
        return
      }

      // Which way there is actually room to open.
      //
      // The requested side is a preference, not an instruction: this control lives at the bottom of
      // the aside, and in the mobile drawer that is also the bottom of the screen — where opening
      // downward, the sensible default for a drawer, puts the list past the edge of the viewport
      // where it cannot be read or scrolled to.
      const spaceAbove = rect.top - OFFSET
      const spaceBelow = viewportHeight - rect.bottom - OFFSET

      const preferAbove = placement !== 'bottom'
      const openAbove = (preferAbove ? spaceAbove : spaceBelow) >= MINIMUM_SPACE
        ? preferAbove
        : spaceAbove > spaceBelow

      setStyle({
        position: 'fixed',
        insetInlineStart: rect.left,
        width: rect.width,
        maxBlockSize: Math.max(MINIMUM_SPACE, openAbove ? spaceAbove : spaceBelow),
        ...(openAbove
          ? { insetBlockEnd: viewportHeight - rect.top + OFFSET }
          : { insetBlockStart: rect.bottom + OFFSET }),
      })
    }

    place()
    window.addEventListener('resize', place)
    return () => window.removeEventListener('resize', place)
  }, [anchorRef, placement])

  return style
}

/** One selectable row: a real office, or the "all offices" scope. */
interface Option {
  value: OfficeScopeValue
  label: string
  headquarters: boolean
}

export default function OfficeSwitcherPanel({
  placement,
  onClose,
  id,
  labelledBy,
  anchorRef,
  onSelected,
}: OfficeSwitcherPanelProps) {
  const { t } = useTranslation()
  const {
    offices,
    scope,
    allOfficesAllowed,
    isLoading,
    error,
    isSwitching,
    selectOffice,
    retry,
  } = useOffice()

  const options = useMemo<Option[]>(() => {
    const rows: Option[] = offices.map(office => ({
      value: office.id,
      label: office.name,
      headquarters: office.isHeadquarterOffice,
    }))

    // "Tüm Şubeler" leads, as it did in the legacy control, and only when the server granted it.
    return allOfficesAllowed
      ? [{ value: ALL_OFFICES, label: t('office.switcher.allOffices'), headquarters: false }, ...rows]
      : rows
  }, [allOfficesAllowed, offices, t])

  const position = usePanelPosition(anchorRef, placement)

  const selectedIndex = options.findIndex(option => option.value === scope)
  const [activeIndex, setActiveIndex] = useState(selectedIndex >= 0 ? selectedIndex : 0)
  const listRef = useRef<HTMLUListElement>(null)

  // Focus lands on the list itself, so the first arrow key moves inside it rather than out of it.
  useEffect(() => {
    listRef.current?.focus()
  }, [])

  async function choose(option: Option) {
    // Choosing the office already in force is not a no-op that costs nothing — it would clear the
    // cache and refetch every visible screen for no change at all.
    if (option.value !== scope) {
      await selectOffice(option.value)
      onSelected?.()
    }
    onClose()
  }

  function handleKeyDown(event: KeyboardEvent<HTMLUListElement>) {
    if (options.length === 0) return

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault()
        setActiveIndex(index => (index + 1) % options.length)
        break
      case 'ArrowUp':
        event.preventDefault()
        setActiveIndex(index => (index - 1 + options.length) % options.length)
        break
      case 'Home':
        event.preventDefault()
        setActiveIndex(0)
        break
      case 'End':
        event.preventDefault()
        setActiveIndex(options.length - 1)
        break
      case 'Enter':
      case ' ':
        event.preventDefault()
        if (!isSwitching) void choose(options[activeIndex])
        break
      default:
        break
    }
  }

  return (
    <div
      className={`ensa-office-switcher__panel ensa-office-switcher__panel--${placement}`}
      style={position}
      role="presentation"
      // Hidden until it has been measured, so it never paints once at the wrong place and then
      // jumps. One frame, not a flicker.
      hidden={position === undefined}
    >
      {isLoading && (
        <div className="p-3 text-center">
          <Spinner label={t('office.switcher.loading')} />
        </div>
      )}

      {!isLoading && error != null && (
        <div className="p-2">
          {/* The active office is deliberately left alone on a failure: a switcher that cannot read
              its list must not also decide the user is now somewhere else. */}
          <ErrorPanel message={errorMessage(error)} flush />
          <button type="button" className="ensa-office-switcher__retry" onClick={retry}>
            {t('office.switcher.retry')}
          </button>
        </div>
      )}

      {!isLoading && error == null && options.length === 0 && (
        <div className="p-3">
          <Text size="sm" tone="muted">
            {t('office.switcher.empty')}
          </Text>
        </div>
      )}

      {!isLoading && error == null && options.length > 0 && (
        <ul
          ref={listRef}
          id={id}
          role="listbox"
          tabIndex={-1}
          aria-labelledby={labelledBy}
          aria-activedescendant={`${id}-option-${activeIndex}`}
          aria-busy={isSwitching || undefined}
          className="ensa-office-switcher__list"
          onKeyDown={handleKeyDown}
        >
          {options.map((option, index) => {
            const isSelected = option.value === scope

            return (
              // eslint-disable-next-line jsx-a11y/click-events-have-key-events -- the listbox owns
              // the keyboard model; an option is not separately focusable.
              <li
                key={String(option.value)}
                id={`${id}-option-${index}`}
                role="option"
                aria-selected={isSelected}
                aria-disabled={isSwitching || undefined}
                className={[
                  'ensa-office-switcher__option',
                  isSelected ? 'ensa-office-switcher__option--selected' : '',
                  index === activeIndex ? 'ensa-office-switcher__option--active' : '',
                ]
                  .filter(Boolean)
                  .join(' ')}
                onMouseEnter={() => setActiveIndex(index)}
                onClick={() => {
                  if (!isSwitching) void choose(option)
                }}
              >
                {/* A check, not colour alone: the active row has to be distinguishable without it. */}
                <span aria-hidden="true" className="ensa-office-switcher__check">
                  {isSelected ? '✓' : ''}
                </span>
                <span className="ensa-office-switcher__option-label" title={option.label}>
                  {option.label}
                </span>
                {option.headquarters && (
                  <span className="ensa-office-switcher__badge">
                    {t('office.switcher.headquarters')}
                  </span>
                )}
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}
