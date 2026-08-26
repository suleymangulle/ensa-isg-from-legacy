import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import UserListPage from './UserListPage'
import UserDetailPage from './UserDetailPage'
import RoleListPage from './RoleListPage'
import PermissionMatrixPage from './PermissionMatrixPage'

const definition: ModuleDefinition = {
  routes: [
    { path: 'users', element: <UserListPage /> },
    { path: 'users/:id', element: <UserDetailPage /> },
    { path: 'roles', element: <RoleListPage /> },
    { path: 'permissions', element: <PermissionMatrixPage /> },
  ],
  nav: [
    {
      path: 'users', labelKey: 'nav.users', icon: '◉', group: 'admin', order: 10,
      permission: PERMISSIONS.User.Default,
    },
    {
      path: 'roles', labelKey: 'nav.roles', icon: '◈', group: 'admin', order: 20,
      permission: PERMISSIONS.Role.Default,
    },
    {
      path: 'permissions', labelKey: 'nav.permissions', icon: '⚿', group: 'admin', order: 30,
      permission: PERMISSIONS.Permission.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
