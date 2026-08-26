import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import VisitListPage from './VisitListPage'
import SupportTicketListPage from './SupportTicketListPage'
import SupportTicketDetailPage from './SupportTicketDetailPage'
import MessageListPage from './MessageListPage'
import MailListPage from './MailListPage'

/**
 * Sidebar orders start at 100 rather than 50: the reports module already occupies 60, 70 and 80
 * inside the `records` group, and interleaving four communication entries through three report
 * entries would read as an accident. Documents keeps 20-40, below reports.
 */
const definition: ModuleDefinition = {
  routes: [
    { path: 'visits', element: <VisitListPage /> },
    { path: 'support-tickets', element: <SupportTicketListPage /> },
    { path: 'support-tickets/:id', element: <SupportTicketDetailPage /> },
    { path: 'messages', element: <MessageListPage /> },
    { path: 'mail', element: <MailListPage /> },
  ],
  nav: [
    { path: 'visits', labelKey: 'nav.visits', icon: '🗓', group: 'records', order: 100,
      permission: PERMISSIONS.Visit.Default,
    },
    { path: 'support-tickets', labelKey: 'nav.supportTickets', icon: '🛟', group: 'records', order: 110,
      permission: PERMISSIONS.SupportTicket.Default,
    },
    { path: 'messages', labelKey: 'nav.messages', icon: '✉', group: 'records', order: 120,
      permission: PERMISSIONS.Message.Default,
    },
    { path: 'mail', labelKey: 'nav.mail', icon: '📧', group: 'records', order: 130,
      permission: PERMISSIONS.Mail.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
