import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import OrganizationListPage from './OrganizationListPage'
import OrganizationDetailPage from './OrganizationDetailPage'
import OfficeListPage from './OfficeListPage'
import OfficeDetailPage from './OfficeDetailPage'

const definition: ModuleDefinition = {
  routes: [
    { path: 'organizations', element: <OrganizationListPage /> },
    { path: 'organizations/:id', element: <OrganizationDetailPage /> },
    { path: 'offices', element: <OfficeListPage /> },
    { path: 'offices/:id', element: <OfficeDetailPage /> },
  ],
  nav: [
    {
      path: 'organizations', labelKey: 'nav.organizations', icon: '⌂', group: 'admin', order: 40,
      permission: PERMISSIONS.Tenant.Default,
    },
    {
      path: 'offices', labelKey: 'nav.offices', icon: '▤', group: 'admin', order: 50,
      permission: PERMISSIONS.Office.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
