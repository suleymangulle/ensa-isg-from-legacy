import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import EquipmentListPage from './EquipmentListPage'
import DepartmentListPage from './DepartmentListPage'

const definition: ModuleDefinition = {
  routes: [
    { path: 'equipment', element: <EquipmentListPage /> },
    { path: 'departments', element: <DepartmentListPage /> },
  ],
  nav: [
    {
      path: 'departments', labelKey: 'nav.departments', icon: '◫', group: 'workplace', order: 30,
      permission: PERMISSIONS.WorkplaceDepartment.Default,
    },
    {
      path: 'equipment', labelKey: 'nav.equipment', icon: '⚙', group: 'workplace', order: 40,
      permission: PERMISSIONS.Equipment.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
