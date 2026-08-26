import { useEffect, useMemo, type ReactElement } from 'react'
import { Navigate, Route, Routes, type RouteObject } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from './auth/AuthContext'
import { Spinner } from './components/DataTable'
import { currentLanguage } from './i18n'
import MainLayout from './layout/MainLayout'
import { moduleRoutes } from './modules/registry'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import NotFoundPage from './pages/NotFoundPage'

/** Redirects to the login page until the session has been restored. */
function ProtectedRoute({ children }: { children: ReactElement }) {
  const { user, isReady } = useAuth()

  if (!isReady) {
    return (
      <div className="d-flex vh-100 align-items-center justify-content-center">
        <Spinner />
      </div>
    )
  }
  return user ? children : <Navigate to="/login" replace />
}

/** Renders a route tree contributed by a module, children included. */
function renderRoute(route: RouteObject, key: string) {
  return (
    <Route key={key} path={route.path} element={route.element}>
      {route.children?.map((child, index) => renderRoute(child, `${key}/${index}`))}
    </Route>
  )
}

export default function App() {
  const { t, i18n } = useTranslation()

  // Modules register themselves; see src/modules/registry.ts.
  const routes = useMemo(() => moduleRoutes(), [])

  // Keeps the document language and title in sync with the active locale.
  useEffect(() => {
    document.documentElement.lang = currentLanguage()
    document.title = t('app.title')
  }, [t, i18n.language])

  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <MainLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<DashboardPage />} />
        {routes.map((route, index) => renderRoute(route, route.path ?? String(index)))}
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
