import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import InvoiceListPage from './InvoiceListPage'
import InvoiceDetailPage from './InvoiceDetailPage'
import InvoicePrintPage from './InvoicePrintPage'
import CashRegisterListPage from './CashRegisterListPage'
import CashRegisterDetailPage from './CashRegisterDetailPage'
import PenaltiesPage from './PenaltiesPage'
import PenaltyDetailPage from './PenaltyDetailPage'
import PenaltySurveyDetailPage from './PenaltySurveyDetailPage'
import CompanyBalancePage from './CompanyBalancePage'

/**
 * Finance module — the rewrite of the legacy "Muhasebe" section.
 *
 * Four screens carry a sidebar entry; the detail, print and survey routes hang off them and are
 * reached from a table row. `penalties/surveys/:surveyId` is declared before `penalties/:id`
 * would ever match it, because React Router ranks the static `surveys` segment above the dynamic
 * one regardless of order — the explicit ordering here is for a human reader.
 */
const definition: ModuleDefinition = {
  routes: [
    { path: 'invoices', element: <InvoiceListPage /> },
    { path: 'invoices/:id', element: <InvoiceDetailPage /> },
    { path: 'invoices/:id/print', element: <InvoicePrintPage /> },

    { path: 'cash-register', element: <CashRegisterListPage /> },
    { path: 'cash-register/:id', element: <CashRegisterDetailPage /> },

    { path: 'penalties', element: <PenaltiesPage /> },
    { path: 'penalties/surveys/:surveyId', element: <PenaltySurveyDetailPage /> },
    { path: 'penalties/:id', element: <PenaltyDetailPage /> },

    { path: 'finance/balances', element: <CompanyBalancePage /> },
  ],
  nav: [
    { path: 'invoices', labelKey: 'nav.invoices', icon: '🧾', group: 'finance', order: 10,
      permission: PERMISSIONS.Invoice.Default,
    },
    { path: 'cash-register', labelKey: 'nav.cashRegister', icon: '💰', group: 'finance', order: 20,
      permission: PERMISSIONS.CashRegister.Default,
    },
    { path: 'penalties', labelKey: 'nav.penalties', icon: '⚖', group: 'finance', order: 30,
      permission: PERMISSIONS.Penalty.Default,
    },
    {
      path: 'finance/balances',
      labelKey: 'nav.companyBalances',
      icon: '📊',
      group: 'finance',
      order: 40,
      permission: PERMISSIONS.Invoice.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
