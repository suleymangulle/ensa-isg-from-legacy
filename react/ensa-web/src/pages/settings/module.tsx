import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import ParameterListPage from './ParameterListPage'
import MenuListPage from './MenuListPage'
import LookupListPage from './LookupListPage'

const definition: ModuleDefinition = {
  routes: [
    { path: 'settings/parameters', element: <ParameterListPage /> },
    { path: 'settings/menus', element: <MenuListPage /> },
    { path: 'settings/lookups', element: <LookupListPage /> },
  ],
  nav: [
    {
      path: 'settings/parameters',
      labelKey: 'nav.parameters',
      icon: '⚙',
      group: 'admin',
      order: 60,
      permission: PERMISSIONS.Lookups.Default,
    },
    {
      path: 'settings/menus', labelKey: 'nav.menus', icon: '☰', group: 'admin', order: 70,
      permission: PERMISSIONS.Menu.Default,
    },
    {
      path: 'settings/lookups', labelKey: 'nav.lookups', icon: '⛁', group: 'admin', order: 80,
      permission: PERMISSIONS.Lookups.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
