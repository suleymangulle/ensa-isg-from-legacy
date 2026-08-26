import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import IbysSubmissionListPage from './IbysSubmissionListPage'
import IbysSubmissionDetailPage from './IbysSubmissionDetailPage'

const definition: ModuleDefinition = {
  routes: [
    { path: 'ibys', element: <IbysSubmissionListPage /> },
    { path: 'ibys/:id', element: <IbysSubmissionDetailPage /> },
  ],
  nav: [{
      path: 'ibys', labelKey: 'nav.ibys', icon: '⇪', group: 'records', order: 10,
      permission: PERMISSIONS.Ibys.Default,
    }],
}

export const { routes, nav } = definition
export default definition
