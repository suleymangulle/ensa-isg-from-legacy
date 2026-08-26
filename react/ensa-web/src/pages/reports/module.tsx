import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import OhsReportPage from './OhsReportPage'
import ActivityReportListPage from './ActivityReportListPage'
import ActivityReportDetailPage from './ActivityReportDetailPage'
import YearEndReviewListPage from './YearEndReviewListPage'
import YearEndReviewDetailPage from './YearEndReviewDetailPage'

/**
 * Statutory reporting module.
 *
 * Three screens: the OHS control report (read-only, produced by the reporting engine), the
 * specialist activity report with its data rows, and the annual year-end review report with its
 * work item tree. The detail routes carry no sidebar entry — they are reached from their list.
 */
const definition: ModuleDefinition = {
  routes: [
    { path: 'reports/ohs', element: <OhsReportPage /> },
    { path: 'reports/activities', element: <ActivityReportListPage /> },
    { path: 'reports/activities/:id', element: <ActivityReportDetailPage /> },
    { path: 'reports/year-end', element: <YearEndReviewListPage /> },
    { path: 'reports/year-end/:id', element: <YearEndReviewDetailPage /> },
  ],
  nav: [
    {
      path: 'reports/ohs', labelKey: 'nav.ohsReports', icon: '◈', group: 'records', order: 60,
      permission: PERMISSIONS.Report.Default,
    },
    {
      path: 'reports/activities',
      labelKey: 'nav.activityReports',
      icon: '▤',
      group: 'records',
      order: 70,
      permission: PERMISSIONS.Report.Default,
    },
    {
      path: 'reports/year-end',
      labelKey: 'nav.yearEndReports',
      icon: '◎',
      group: 'records',
      order: 80,
      permission: PERMISSIONS.Report.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
