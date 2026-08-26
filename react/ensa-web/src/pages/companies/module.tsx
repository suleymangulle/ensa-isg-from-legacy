import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import CompanyListPage from './CompanyListPage'
import CompanyDetailPage from './CompanyDetailPage'
import EmployeeListPage from './EmployeeListPage'

const definition: ModuleDefinition = {
  routes: [
    { path: 'companies', element: <CompanyListPage /> },
    { path: 'companies/:id', element: <CompanyDetailPage /> },
    { path: 'employees', element: <EmployeeListPage /> },
  ],
  nav: [
    { path: 'companies', labelKey: 'nav.companies', icon: '▦', group: 'workplace', order: 10,
      permission: PERMISSIONS.Company.Default,
    },
    { path: 'employees', labelKey: 'nav.employees', icon: '☰', group: 'workplace', order: 20,
      permission: PERMISSIONS.CompanyEmployee.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
