import { PERMISSIONS } from '@/api/permissions'
import type { ModuleDefinition } from '@/modules/registry'
import MedicalExaminationListPage from './MedicalExaminationListPage'
import MedicalExaminationDetailPage from './MedicalExaminationDetailPage'
import EPrescriptionListPage from './EPrescriptionListPage'
import EPrescriptionDetailPage from './EPrescriptionDetailPage'

const definition: ModuleDefinition = {
  routes: [
    { path: 'medical-examinations', element: <MedicalExaminationListPage /> },
    { path: 'medical-examinations/:id', element: <MedicalExaminationDetailPage /> },
    { path: 'eprescriptions', element: <EPrescriptionListPage /> },
    { path: 'eprescriptions/:id', element: <EPrescriptionDetailPage /> },
  ],
  nav: [
    {
      path: 'medical-examinations',
      labelKey: 'nav.medicalExaminations',
      icon: '✚',
      group: 'ohs',
      order: 30,
      permission: PERMISSIONS.MedicalExamination.Default,
    },
    {
      path: 'eprescriptions',
      labelKey: 'nav.ePrescriptions',
      icon: '℞',
      group: 'ohs',
      order: 40,
      permission: PERMISSIONS.EPrescription.Default,
    },
  ],
}

export const { routes, nav } = definition
export default definition
