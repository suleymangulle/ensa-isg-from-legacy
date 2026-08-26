import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import WorkPlanListPage from './WorkPlanListPage'
import WorkPlanDetailPage from './WorkPlanDetailPage'
import ActivityListPage from './ActivityListPage'
import ActivityDetailPage from './ActivityDetailPage'

/**
 * Work plan module.
 *
 * The annual work plan and the activity catalogue it is built from belong together: a plan line
 * is an activity with a month, so both screens sit in the workplace group next to the companies
 * they are drawn up for.
 */
const definition: ModuleDefinition = {
  routes: [
    { path: 'work-plans', element: <WorkPlanListPage /> },
    { path: 'work-plans/:id', element: <WorkPlanDetailPage /> },
    { path: 'activities', element: <ActivityListPage /> },
    { path: 'activities/:id', element: <ActivityDetailPage /> },
  ],
  nav: [
    {
      path: 'work-plans', labelKey: 'nav.workPlans', icon: '▤', group: 'workplace', order: 30,
      permission: PERMISSIONS.WorkPlan.Default,
    },
    {
      path: 'activities', labelKey: 'nav.activities', icon: '◇', group: 'workplace', order: 40,
      permission: PERMISSIONS.Activity.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
