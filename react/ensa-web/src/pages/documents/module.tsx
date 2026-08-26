import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import DocumentListPage from './DocumentListPage'
import DocumentDetailPage from './DocumentDetailPage'
import FormListPage from './FormListPage'
import ArchiveListPage from './ArchiveListPage'

const definition: ModuleDefinition = {
  routes: [
    { path: 'documents', element: <DocumentListPage /> },
    { path: 'documents/:id/detail', element: <DocumentDetailPage /> },
    { path: 'forms', element: <FormListPage /> },
    { path: 'archive', element: <ArchiveListPage /> },
  ],
  nav: [
    { path: 'documents', labelKey: 'nav.documents', icon: '🗎', group: 'records', order: 20,
      permission: PERMISSIONS.Document.Default,
    },
    { path: 'forms', labelKey: 'nav.forms', icon: '🗒', group: 'records', order: 30,
      permission: PERMISSIONS.Form.Default,
    },
    { path: 'archive', labelKey: 'nav.archive', icon: '🗄', group: 'records', order: 40,
      permission: PERMISSIONS.Document.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
