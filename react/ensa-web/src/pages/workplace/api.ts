import { useQuery } from '@tanstack/react-query'
import { http, type ListResult, type PagedResult } from '@/api/http'
import type { EquipmentType } from '@/api/enums'

/**
 * Data layer for the workplace module: periodic inspection equipment and workplace departments.
 *
 * The DTO shapes mirror `Ensa.Application.Contracts.Risks.Dtos.EquipmentDtos` and
 * `…Companies.Dtos.WorkplaceDepartmentDtos`; the API serialises properties camelCase and enums as
 * numbers.
 */

export const RESOURCES = {
  equipment: 'equipment',
  department: 'workplace-department',
} as const

// ---------------------------------------------------------------- Equipment

/** `EquipmentListDto` — one row of the equipment table. */
export interface EquipmentListDto {
  id: number
  companyId: number
  companyName?: string | null
  equipmentName: string
  equipmentType: EquipmentType
  examinationDate?: string | null
  nextExaminationDate?: string | null
  examinationPerformedBy?: string | null
  periodId?: number | null
  /** Computed by the API — the inspection due date has passed. */
  isInspectionOverdue: boolean
  /** Days until the next inspection; negative once it is overdue. */
  remainingDays?: number | null
  isDeletable: boolean
}

/** `EquipmentDto` — the single-record read. */
export interface EquipmentDto {
  id: number
  tenantId?: number | null
  companyId: number
  equipmentName: string
  equipmentType: EquipmentType
  examinationReport?: string | null
  examinationReportDocumentId?: number | null
  examinationPerformedBy?: string | null
  examinationDate?: string | null
  nextExaminationDate?: string | null
  periodId?: number | null
  isDeletable: boolean
  isInspectionOverdue: boolean
}

/** `CreateEquipmentDto` / `UpdateEquipmentDto`. */
export interface SaveEquipmentDto {
  companyId: number
  equipmentName: string
  equipmentType: EquipmentType
  examinationReport?: string | null
  examinationReportDocumentId?: number | null
  examinationPerformedBy?: string | null
  examinationDate?: string | null
  periodId?: number | null
  isDeletable?: boolean
}

/** `EquipmentDocumentDto` — paperwork attached to a piece of equipment. */
export interface EquipmentDocumentDto {
  id: number
  equipmentId: number
  companyId: number
  documentId: number
  equipmentDocumentTypeId?: number | null
  description?: string | null
  examinationDate?: string | null
  validityDate?: string | null
}

/** `GetEquipmentListInput`. */
export interface EquipmentListRequest {
  skipCount?: number
  maxResultCount?: number
  sorting?: string
  filter?: string
  companyId?: number
  equipmentType?: EquipmentType
  periodId?: number
  onlyOverdueInspection?: boolean
}

export function useEquipmentList(request: EquipmentListRequest) {
  return useQuery({
    queryKey: [RESOURCES.equipment, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<EquipmentListDto>>(`/${RESOURCES.equipment}`, {
        params: request,
      })
      return data
    },
  })
}

/**
 * Equipment whose periodic inspection is already past due.
 *
 * This is the operationally important query in the module: an uninspected lifting device or
 * pressure vessel is a statutory finding, so the screen leads with it rather than burying it
 * behind a filter.
 */
export function useOverdueInspections() {
  return useQuery({
    queryKey: [RESOURCES.equipment, 'overdue'],
    queryFn: async () => {
      const { data } = await http.get<ListResult<EquipmentListDto>>(
        `/${RESOURCES.equipment}/overdue-inspections`,
      )
      return data
    },
  })
}

export function useEquipmentDocuments(equipmentId: number | undefined) {
  return useQuery({
    queryKey: [RESOURCES.equipment, 'documents', equipmentId],
    enabled: !!equipmentId,
    queryFn: async () => {
      const { data } = await http.get<ListResult<EquipmentDocumentDto>>(
        `/${RESOURCES.equipment}/${equipmentId}/documents`,
      )
      return data
    },
  })
}

// -------------------------------------------------------------- Departments

/** `WorkplaceDepartmentListDto`. */
export interface DepartmentListDto {
  id: number
  companyId: number
  companyName?: string | null
  departmentName: string
  /** `false` once the department is referenced by another record. */
  isDeletable: boolean
}

/** `CreateWorkplaceDepartmentDto` / `UpdateWorkplaceDepartmentDto`. */
export interface SaveDepartmentDto {
  companyId: number
  departmentName: string
}

/** `GetWorkplaceDepartmentListInput`. */
export interface DepartmentListRequest {
  skipCount?: number
  maxResultCount?: number
  sorting?: string
  filter?: string
  companyId?: number
  isDeletable?: boolean
}

export function useDepartmentList(request: DepartmentListRequest) {
  return useQuery({
    queryKey: [RESOURCES.department, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<DepartmentListDto>>(`/${RESOURCES.department}`, {
        params: request,
      })
      return data
    },
  })
}
