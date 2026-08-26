import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import RiskAssessmentListPage from './RiskAssessmentListPage'
import RiskAssessmentDetailPage from './RiskAssessmentDetailPage'
import EmergencyPlanListPage from './EmergencyPlanListPage'
import EmergencyPlanDetailPage from './EmergencyPlanDetailPage'

const definition: ModuleDefinition = {
  routes: [
    { path: 'risk-assessments', element: <RiskAssessmentListPage /> },
    { path: 'risk-assessments/:id', element: <RiskAssessmentDetailPage /> },
    { path: 'emergency-plans', element: <EmergencyPlanListPage /> },
    { path: 'emergency-plans/:id', element: <EmergencyPlanDetailPage /> },
  ],
  nav: [
    {
      path: 'risk-assessments', labelKey: 'nav.riskAssessments', icon: '⚠', group: 'ohs', order: 20,
      permission: PERMISSIONS.RiskAssessment.Default,
    },
    {
      path: 'emergency-plans', labelKey: 'nav.emergencyPlans', icon: '⚑', group: 'ohs', order: 25,
      permission: PERMISSIONS.EmergencyPlan.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
