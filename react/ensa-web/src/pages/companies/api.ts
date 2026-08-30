import { useQuery } from '@tanstack/react-query'
import { http, type ListResult } from '@/api/http'
import type { HazardClass, WorkplaceType } from '@/api/enums'
import { ENDPOINTS, type CompanyDto, type LookupDto } from '@/api/endpoints'

/**
 * What the companies module needs beyond the shared `@/api/endpoints` hooks: the write contract
 * and the location lookups.
 *
 * The read side of this module lives in `@/api/endpoints` because the company record is used from
 * several screens; the pieces here are the ones only the form needs. Each page module keeps its
 * own copy of the city and district hooks — membership, settings and tenancy all do — because a
 * lookup shared across modules would tie their caches together for no benefit.
 */

/**
 * `CreateCompanyDto` / `UpdateCompanyDto` — the create and update payloads.
 *
 * `isActive` only exists on update: a company is created active, and the service decides that, not
 * the form.
 */
export interface CompanyInput {
  companyName: string
  ssiNumber?: string | null
  hazardClass: HazardClass
  workplaceType: WorkplaceType
  headquarterCompanyId?: number | null
  cityId: number
  districtId?: number | null
  neighborhoodId?: number | null
  address?: string | null
  phone?: string | null
  email?: string | null
  authorizedPerson?: string | null
  taxNumber?: string | null
  taxOffice?: string | null
  officeId?: number | null
  occupationCodeId?: number | null
  notes?: string | null
  isActive?: boolean
}

/**
 * `GET api/company/{id}` — one company, as the edit dialog needs it.
 *
 * The list row is a projection: it carries the few columns the table shows and not the tax office,
 * the notes or the NACE code. Editing has to start from the record itself, so the row's id is
 * exchanged for the whole company when the dialog opens.
 */
export function useCompany(id: number | undefined) {
  return useQuery({
    queryKey: [ENDPOINTS.company, 'entity', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<CompanyDto>(`/${ENDPOINTS.company}/${id}`)
      return data
    },
  })
}

/** `OccupationCodeLookupDto` — a NACE entry, which carries the hazard class it implies. */
export interface OccupationCodeLookupDto extends LookupDto {
  hazardClass: HazardClass
  tag: string
}

/** `GET api/lookup/cities` — every province. */
export function useCityLookup() {
  return useQuery({
    queryKey: ['lookup', 'cities'],
    staleTime: 60 * 60 * 1000,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>('/lookup/cities')
      return data
    },
  })
}

/** `GET api/lookup/cities/{cityId}/districts` — districts of one province. */
export function useDistrictLookup(cityId: number | null | undefined) {
  return useQuery({
    queryKey: ['lookup', 'districts', cityId],
    enabled: !!cityId,
    staleTime: 60 * 60 * 1000,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(`/lookup/cities/${cityId}/districts`)
      return data
    },
  })
}

/**
 * `GET api/lookup/occupation-codes` — NACE search.
 *
 * The catalogue is far too long to list, so the endpoint is only asked once a term is typed, the
 * same way the lookup administration screen asks it.
 */
export function useOccupationCodeLookup(filter: string) {
  return useQuery({
    queryKey: ['lookup', 'occupation-codes', filter],
    enabled: filter.trim().length > 1,
    queryFn: async () => {
      const { data } = await http.get<ListResult<OccupationCodeLookupDto>>(
        '/lookup/occupation-codes',
        { params: { filter: filter.trim() } },
      )
      return data
    },
  })
}

export function optionalNumber(value: string): number | null {
  const parsed = Number(value)
  return value.trim() === '' || Number.isNaN(parsed) ? null : parsed
}

export function optionalText(value: string): string | null {
  return value.trim() === '' ? null : value.trim()
}
