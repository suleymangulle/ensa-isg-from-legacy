import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import IncidentListPage from './IncidentListPage'
import IncidentDetailPage from './IncidentDetailPage'
import CorrectiveActionListPage from './CorrectiveActionListPage'
import CorrectiveActionDetailPage from './CorrectiveActionDetailPage'
import FieldObservationListPage from './FieldObservationListPage'
import FieldObservationDetailPage from './FieldObservationDetailPage'

/**
 * Incidents, corrective / preventive actions (DOF) and field observation reports — the three
 * record types that feed each other: an observation line raises an action, an incident produces
 * one, and both are tracked against a deadline.
 */
const definition: ModuleDefinition = {
  routes: [
    { path: 'incidents', element: <IncidentListPage /> },
    { path: 'incidents/:id', element: <IncidentDetailPage /> },
    { path: 'corrective-actions', element: <CorrectiveActionListPage /> },
    { path: 'corrective-actions/:id', element: <CorrectiveActionDetailPage /> },
    { path: 'field-observations', element: <FieldObservationListPage /> },
    { path: 'field-observations/:id', element: <FieldObservationDetailPage /> },
  ],
  nav: [
    {
      path: 'incidents', labelKey: 'nav.incidents', icon: '⚡', group: 'ohs', order: 40,
      permission: PERMISSIONS.Incident.Default,
    },
    {
      path: 'corrective-actions',
      labelKey: 'nav.correctiveActions',
      icon: '✔',
      group: 'ohs',
      order: 50,
      permission: PERMISSIONS.CorrectiveAction.Default,
    },
    {
      path: 'field-observations',
      labelKey: 'nav.fieldObservations',
      icon: '◎',
      group: 'ohs',
      order: 60,
      permission: PERMISSIONS.FieldObservation.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
