import { useEffect } from 'react'

/**
 * Makes the library's toast stack audible.
 *
 * `useToast` is how every write reports itself — "Kaydedildi", "Silindi" — and the library renders
 * that stack into a plain `div.toast-container` with no live region around it. A sighted user sees
 * the confirmation; a screen reader user is told nothing at all, which is the one case where the
 * message actually matters, because they cannot see the row appear in the table behind it.
 *
 * Marking the container once, on mount, is enough: the library keeps the same element for the life
 * of the provider and only adds and removes children inside it, which is exactly what a live region
 * is for. `polite` rather than `assertive` — a save confirmation waits for a pause, it does not
 * interrupt what the user is reading.
 *
 * Delete this the day `ToastProvider` marks its own container. Rendered once, under the provider,
 * in `src/main.tsx`.
 */
export default function ToastRegion() {
  useEffect(() => {
    const container = document.querySelector('.toast-container')
    if (!container) return

    container.setAttribute('role', 'status')
    container.setAttribute('aria-live', 'polite')
    container.setAttribute('aria-atomic', 'false')
  }, [])

  return null
}
