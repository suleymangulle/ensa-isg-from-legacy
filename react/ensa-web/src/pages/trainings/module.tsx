import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import TrainingPlanPage from './TrainingPlanPage'
import TrainingPlanListPage from './TrainingPlanListPage'
import TrainingPlanDetailPage from './TrainingPlanDetailPage'
import TrainingListPage from './TrainingListPage'
import TrainingDetailPage from './TrainingDetailPage'
import TrainingProgressPage from './TrainingProgressPage'

/**
 * Training module.
 *
 * `/training-plans` stays the operational cross-plan line list; the plan headers live one level
 * down at `/training-plans/plans`, so the sidebar entry keeps pointing at the screen people use
 * daily and the plan administration is reached from it.
 */
const definition: ModuleDefinition = {
  routes: [
    { path: 'trainings', element: <TrainingListPage /> },
    { path: 'trainings/:id', element: <TrainingDetailPage /> },
    { path: 'training-plans', element: <TrainingPlanPage /> },
    { path: 'training-plans/plans', element: <TrainingPlanListPage /> },
    { path: 'training-plans/plans/:id', element: <TrainingPlanDetailPage /> },
    { path: 'training-progress', element: <TrainingProgressPage /> },
  ],
  nav: [
    {
      path: 'training-plans', labelKey: 'nav.trainingPlans', icon: '◈', group: 'ohs', order: 10,
      permission: PERMISSIONS.TrainingPlan.Default,
    },
    {
      path: 'trainings', labelKey: 'nav.trainings', icon: '▣', group: 'ohs', order: 20,
      permission: PERMISSIONS.Training.Default,
    },
    {
      path: 'training-progress',
      labelKey: 'nav.trainingProgress',
      icon: '◉',
      group: 'ohs',
      order: 30,
      permission: PERMISSIONS.Training.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
