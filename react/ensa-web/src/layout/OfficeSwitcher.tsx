import { useCallback, useEffect, useId, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Text, Tooltip } from 'rich-react-component'
import { useOffice } from '@/auth/OfficeContext'
import OfficeSwitcherPanel, { type OfficePanelPlacement } from './OfficeSwitcherPanel'

/**
 * The office (Şube) switcher that sits in the sidebar's footer.
 *
 * Two shapes, one behaviour. `OfficeSwitcher` is the expanded aside: an icon, the office name and a
 * caret. `OfficeSwitcherCompact` is the collapsed rail, where a full-width control cannot survive
 * a ~76px column — the library's own `collapsedFooter` prop exists for exactly that, and its doc
 * says so. Both open the same list; only the placement differs, which is why they share a file
 * rather than duplicating the panel, the keyboard model and every string three times.
 *
 * The trigger is a real `<button>` with `aria-haspopup="listbox"` and an accessible name that
 * includes the office currently in force, so the control announces both what it is and what it
 * says. The legacy control was an unlabelled native `<select>`; a screen reader announced it as a
 * combobox with no name at all.
 */

interface TriggerProps {
  /** Where the panel opens relative to the trigger. */
  placement: OfficePanelPlacement
  /**
   * Called once an office has actually been chosen. The shell uses it to close the mobile drawer,
   * which is what the drawer already does when a menu entry is followed (`closeMobileOnSelect`);
   * that prop is the library's, and it only covers the entries the library itself draws.
   */
  onSelected?: () => void
}

/** Shared open/close state, outside-click and escape handling for both shapes. */
function useSwitcherPopup() {
  const [isOpen, setIsOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)

  const close = useCallback(() => setIsOpen(false), [])

  /**
   * Returns focus to the trigger once the panel has actually gone.
   *
   * In an effect rather than inside `close()`, and on the frame after: choosing an office rerenders
   * the trigger — its accessible name carries the office — and focusing the node that is about to be
   * replaced leaves focus on `<body>`, which is where a keyboard user least wants to be. Waiting for
   * the commit means focusing the element that survived it.
   */
  const wasOpen = useRef(false)

  useEffect(() => {
    if (wasOpen.current && !isOpen) {
      const frame = requestAnimationFrame(() => triggerRef.current?.focus())
      wasOpen.current = isOpen
      return () => cancelAnimationFrame(frame)
    }

    wasOpen.current = isOpen
  }, [isOpen])

  useEffect(() => {
    if (!isOpen) return

    function onPointerDown(event: MouseEvent) {
      if (!containerRef.current?.contains(event.target as Node)) setIsOpen(false)
    }

    function onKeyDown(event: globalThis.KeyboardEvent) {
      if (event.key === 'Escape') {
        event.stopPropagation()
        close()
      }
    }

    document.addEventListener('mousedown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)

    return () => {
      document.removeEventListener('mousedown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [close, isOpen])

  return { isOpen, setIsOpen, close, containerRef, triggerRef }
}

/** The office name to show, or the "all offices" label, or a placeholder while it resolves. */
function useCurrentLabel(): string {
  const { t } = useTranslation()
  const { scope, activeOffice, isLoading } = useOffice()

  if (isLoading) return t('office.switcher.loading')
  if (scope === 'all') return t('office.switcher.allOffices')
  return activeOffice?.name ?? t('office.switcher.none')
}

/** The expanded aside's footer control. */
export default function OfficeSwitcher({
  placement = 'top',
  onSelected,
}: Partial<TriggerProps> = {}) {
  const { t } = useTranslation()
  const { canSwitch, isSwitching, isLoading, error } = useOffice()
  const { isOpen, setIsOpen, close, containerRef, triggerRef } = useSwitcherPopup()

  const panelId = useId()
  const labelId = useId()
  const label = useCurrentLabel()

  // One office and nothing to switch to is a control with a single choice. The legacy shell drew
  // nothing in that case either (`Model.OfisList.Count > 1`).
  //
  // A failure is the exception: the list could not be read, so there is no telling whether there
  // was anything to switch between. Hiding the control there would leave the user with a shell
  // that quietly stopped offering something it had a moment ago, and no way to ask again — so it
  // stays, and its panel carries the error and a retry.
  if (!canSwitch && error == null) return null

  return (
    <div className="ensa-office-switcher" ref={containerRef}>
      {/* The id sits on the wrapper because the library's `Text` takes no `id`, and the listbox
          needs something stable to point `aria-labelledby` at. */}
      <span id={labelId} className="ensa-office-switcher__caption">
        <Text size="sm" tone="muted">
          {t('office.switcher.label')}
        </Text>
      </span>

      <button
        ref={triggerRef}
        type="button"
        className="ensa-office-switcher__trigger"
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        aria-controls={isOpen ? panelId : undefined}
        aria-label={t('office.switcher.triggerLabel', { office: label })}
        disabled={isLoading || isSwitching}
        onClick={() => setIsOpen(open => !open)}
        onKeyDown={event => {
          if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
            event.preventDefault()
            setIsOpen(true)
          }
        }}
      >
        <span aria-hidden="true" className="ensa-office-switcher__icon">
          ▤
        </span>
        {/* Truncated with an ellipsis, and the full name in a tooltip — an office name is a place
            name and they get long. */}
        <Tooltip content={label} placement="top" wrapperClassName="ensa-office-switcher__name-wrap">
          <span className="ensa-office-switcher__name">{label}</span>
        </Tooltip>
        <span aria-hidden="true" className="ensa-office-switcher__caret">
          ⌃
        </span>
      </button>

      {isOpen && (
        <OfficeSwitcherPanel
          placement={placement}
          id={panelId}
          labelledBy={labelId}
          anchorRef={triggerRef}
          onSelected={onSelected}
          onClose={close}
        />
      )}
    </div>
  )
}

/**
 * The collapsed rail's footer control: the same switcher reduced to its icon.
 *
 * Passed as the library `Sidebar`'s `collapsedFooter`, which it renders only when supplied — so
 * when this returns `null` no empty footer region is drawn and no focusable control is left in a
 * strip the user cannot read.
 */
export function OfficeSwitcherCompact() {
  const { t } = useTranslation()
  const { canSwitch, isSwitching, isLoading, error } = useOffice()
  const { isOpen, setIsOpen, close, containerRef, triggerRef } = useSwitcherPopup()

  const panelId = useId()
  const labelId = useId()
  const label = useCurrentLabel()

  if (!canSwitch && error == null) return null

  return (
    <div className="ensa-office-switcher ensa-office-switcher--compact" ref={containerRef}>
      <span id={labelId} className="visually-hidden">
        {t('office.switcher.label')}
      </span>

      <Tooltip content={label} placement="top" wrapperClassName="d-block">
        <button
          ref={triggerRef}
          type="button"
          className="ensa-office-switcher__trigger ensa-office-switcher__trigger--icon"
          aria-haspopup="listbox"
          aria-expanded={isOpen}
          aria-controls={isOpen ? panelId : undefined}
          aria-label={t('office.switcher.triggerLabel', { office: label })}
          disabled={isLoading || isSwitching}
          onClick={() => setIsOpen(open => !open)}
          onKeyDown={event => {
            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
              event.preventDefault()
              setIsOpen(true)
            }
          }}
        >
          <span aria-hidden="true" className="ensa-office-switcher__icon">
            ▤
          </span>
        </button>
      </Tooltip>

      {isOpen && (
        <OfficeSwitcherPanel
          placement="right"
          id={panelId}
          labelledBy={labelId}
          anchorRef={triggerRef}
          onClose={close}
        />
      )}
    </div>
  )
}
