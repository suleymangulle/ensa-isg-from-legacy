import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AppearanceProvider, ToastProvider, createAppearanceInitScript } from 'rich-react-component'
// Must be imported before any component so translations are ready on first render.
import './i18n'
import App from './App'
import { AuthProvider } from './auth/AuthContext'
import { OfficeProvider } from './auth/OfficeContext'
import ToastRegion from './components/ToastRegion'
import { APPEARANCE_STORAGE_KEY, ENSA_COLOR_SCHEME_ID } from './styles/appearance'
// Stylesheet order is the library's documented one and is load-bearing:
// Bootstrap first, then the library's own skin, then this application's
// overrides. Swapping any two of them silently changes which rule wins.
import './styles/metronic.scss'
import 'rich-react-component/style.css'
import './styles/ensa.scss'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, refetchOnWindowFocus: false, staleTime: 30_000 },
  },
})

/**
 * Applies the stored theme before React's first paint.
 *
 * `AppearanceProvider` reads storage in an effect — deliberately, so that a
 * server render and the first client render agree — which means the page
 * would otherwise paint light and flip to dark one frame later. The library
 * returns this snippet as a string and never injects it; where it runs is the
 * application's decision, and for a client-rendered SPA that is here, before
 * the root is created. An appended inline script executes synchronously.
 */
function applyStoredAppearance() {
  const script = document.createElement('script')
  script.textContent = createAppearanceInitScript({
    storageKey: APPEARANCE_STORAGE_KEY,
    target: 'documentElement',
    defaultMode: 'system',
  })
  document.head.appendChild(script)
}

applyStoredAppearance()

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <AppearanceProvider
      defaultMode="system"
      defaultSidebarPresentation="grouped"
      defaultSidebarTone="auto"
      defaultColorSchemeId={ENSA_COLOR_SCHEME_ID}
      storage={window.localStorage}
      storageKey={APPEARANCE_STORAGE_KEY}
      // The provider writes `data-bs-theme` and the scheme variables onto the
      // document element, so Bootstrap's own components, the library's and
      // this application's inline `var(--kt-*)` styles all switch together.
      applyTo="documentElement"
    >
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <AuthProvider>
            {/* Inside AuthProvider because it needs a session to ask which offices are the
                caller's, and inside QueryClientProvider because switching office is, on this
                client, a cache operation. */}
            <OfficeProvider>
              <ToastProvider>
                <ToastRegion />
                <App />
              </ToastProvider>
            </OfficeProvider>
          </AuthProvider>
        </BrowserRouter>
      </QueryClientProvider>
    </AppearanceProvider>
  </React.StrictMode>,
)
