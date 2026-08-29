import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { http, type ListResult, type PagedResult } from '@/api/http'
import { useOfficeScopeKey } from '@/auth/OfficeContext'
import { resourceKey } from '@/api/mutations'
import type { LookupDto } from '@/api/endpoints'
import type { PermissionRestrictionMode, PermissionType, StaffRole } from '@/api/enums'

/**
 * Data layer of the membership module — users, roles and the permission catalogue.
 *
 * The shared helpers in `@/api/endpoints` only cover the plain paged list and the plain
 * `GET /{resource}/{id}`; membership needs filtered lists and a handful of sub-resource
 * endpoints (`/user/{id}/roles`, `/permission/user/{id}`) that are `PUT` rather than `POST`,
 * so those live here. Every query key starts with the same resource string the shared
 * `useCreate` / `useUpdate` / `useDelete` invalidate, so a write from either side refreshes
 * both.
 */

export const MEMBERSHIP_RESOURCES = {
  user: 'user',
  role: 'role',
  permission: 'permission',
} as const

// ---------------------------------------------------------------
// DTOs — mirrored from Ensa.Application.Contracts/Membership/Dtos
// ---------------------------------------------------------------

/** `UserListDto` — row of `GET api/user`. */
export interface UserListDto {
  id: number
  userName: string
  name: string
  lastName: string
  fullName: string
  email?: string | null
  phoneNumber?: string | null
  gsm?: string | null
  staffRole: StaffRole
  officeId?: number | null
  companyId?: number | null
  isActive: boolean
  officeAdmin: boolean
  organizationAdmin: boolean
  hireDate?: string | null
  terminationDate?: string | null
}

/**
 * `UserDto` — `GET api/user/{id}`.
 *
 * Carries no credential of any kind: the password hash, the security stamp and the national id
 * are deliberately absent from the read model.
 */
export interface UserDto {
  id: number
  tenantId?: number | null
  userName: string
  name: string
  lastName: string
  fullName: string
  email?: string | null
  emailConfirmed: boolean
  phoneNumber?: string | null
  gsm?: string | null
  address?: string | null
  cityId?: number | null
  districtId?: number | null
  photoDocumentId?: number | null
  color?: string | null
  staffRole: StaffRole
  hireDate?: string | null
  terminationDate?: string | null
  grossSalary?: number | null
  partTime: boolean
  monthlyWorkDurationMinutes?: number | null
  officeId?: number | null
  officeAdmin: boolean
  companyId?: number | null
  permissionGroupId?: number | null
  isActive: boolean
  organizationAdmin: boolean
  systemAdministrator: boolean
  isContractApproved: boolean
  mustChangePassword: boolean
  medicalSpecialtyCode?: string | null
  lockoutEnd?: string | null
}

/** `UserOfficeAssignmentDto` — one office assignment with its monthly commitment. */
export interface UserOfficeAssignmentDto {
  id: number
  officeId: number
  officeName: string
  monthlyWorkDurationMinutes: number
}

/** `UserNavigationDto` — `GET api/user/{id}/detail`. */
export interface UserNavigationDto {
  user: UserDto
  organization?: LookupDto | null
  office?: LookupDto | null
  offices: LookupDto[]
  officeAssignments: UserOfficeAssignmentDto[]
  roles: LookupDto[]
  permissions: PermissionDto[]
  userType?: LookupDto | null
  staffRole: StaffRole
  city?: LookupDto | null
  district?: LookupDto | null
  organizationIds: number[]
  photoSizeBytes?: number | null
}

/**
 * `UserInputDto` — the fields shared by create and update.
 *
 * The update payload is **absolute**: the server maps it straight onto the entity, so a field
 * left out of the request is cleared. The edit form therefore round-trips every field it reads
 * back from `UserDto`, whether or not it renders an input for it.
 */
export interface UserInput {
  name: string
  lastName: string
  email?: string | null
  phoneNumber?: string | null
  gsm?: string | null
  nationalId?: string | null
  address?: string | null
  cityId?: number | null
  districtId?: number | null
  photoDocumentId?: number | null
  color?: string | null
  staffRole: StaffRole
  hireDate?: string | null
  terminationDate?: string | null
  grossSalary?: number | null
  partTime: boolean
  monthlyWorkDurationMinutes?: number | null
  officeId?: number | null
  officeAdmin: boolean
  companyId?: number | null
  permissionGroupId?: number | null
  medicalSpecialtyCode?: string | null
  isActive: boolean
}

/** `CreateUserDto` — the only payload in the whole API that carries a password. */
export interface CreateUserInput extends UserInput {
  userName: string
  password: string
  roles: string[]
  /**
   * Organization the user joins. Honoured only for a host caller; for a caller inside an
   * organization the server ignores it and uses that caller's own organization.
   */
  tenantId?: number | null
}

/** `UpdateUserDto` — no password, no user name; both are immutable through this endpoint. */
export type UpdateUserInput = UserInput

/** `RoleListDto` — row of `GET api/role`. */
export interface RoleListDto {
  id: number
  name: string
  description?: string | null
  isStatic: boolean
  isDefault: boolean
  tenantId?: number | null
}

/** `RoleDto` — `GET api/role/{id}`. */
export interface RoleDto extends RoleListDto {
  userCount: number
}

/** `CreateRoleDto` / `UpdateRoleDto`. */
export interface RoleInput {
  name: string
  description?: string | null
  isDefault: boolean
}

/** `PermissionDto` — one entry of the seeded permission catalogue. */
export interface PermissionDto {
  id: number
  parentPermissionId?: number | null
  permissionType: PermissionType
  permissionTarget: string
  permissionName: string
  permissionDescription?: string | null
  redMessage?: string | null
  permissionRestrictionMode: PermissionRestrictionMode
  sortOrder: number
}

/** `PermissionTreeNodeDto` — a catalogue entry together with its children. */
export interface PermissionTreeNodeDto {
  id: number
  parentPermissionId?: number | null
  permissionType: PermissionType
  permissionTarget: string
  permissionName: string
  permissionDescription?: string | null
  permissionRestrictionMode: PermissionRestrictionMode
  sortOrder: number
  children: PermissionTreeNodeDto[]
  hasChildren: boolean
}

/** `PermissionTreeDto` — `GET api/permission/tree`; the whole catalogue in one call. */
export interface PermissionTreeDto {
  roots: PermissionTreeNodeDto[]
  totalCount: number
}

/** `UserPermissionsDto` — `GET api/permission/user/{userId}`. */
export interface UserPermissionsDto {
  userId: number
  effectivePermissions: PermissionDto[]
  grantedPermissionIds: number[]
  deniedPermissionIds: number[]
  systemAdministrator: boolean
}

/** `UpdateUserPermissionsDto` — both lists are absolute; anything unlisted is removed. */
export interface UpdateUserPermissionsInput {
  grantedPermissionIds: number[]
  deniedPermissionIds: number[]
}

// ---------------------------------------------------------------
// Queries
// ---------------------------------------------------------------

export interface UserListRequest {
  page: number
  pageSize: number
  filter: string
  staffRole?: StaffRole
  isActive?: boolean
}

/** `GET api/user` — paged, filtered by free text, staff role and active state. */
export function useUserList(request: UserListRequest) {
  // Who works where is an office question — the list is filtered by office assignment server-side.
  const officeScope = useOfficeScopeKey()

  return useQuery({
    queryKey: [MEMBERSHIP_RESOURCES.user, 'list', officeScope, request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<UserListDto>>('/user', {
        params: {
          SkipCount: (request.page - 1) * request.pageSize,
          MaxResultCount: request.pageSize,
          Sorting: 'Name ASC',
          Filter: request.filter || undefined,
          StaffRole: request.staffRole,
          IsActive: request.isActive,
        },
      })
      return data
    },
  })
}

/** `GET api/user/{id}/detail` — organization, offices, roles and effective permissions at once. */
export function useUserDetail(id: number | undefined) {
  return useQuery({
    queryKey: [MEMBERSHIP_RESOURCES.user, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<UserNavigationDto>(`/user/${id}/detail`)
      return data
    },
  })
}

/** `GET api/user/lookup` — at most 50 rows, used by the permission screen's user picker. */
export function useUserLookup(filter?: string) {
  return useQuery({
    queryKey: [MEMBERSHIP_RESOURCES.user, 'lookup', filter],
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>('/user/lookup', {
        params: { filter: filter || undefined },
      })
      return data
    },
  })
}

export interface RoleListRequest {
  page: number
  pageSize: number
  filter: string
}

/** `GET api/role` — paged role list. */
export function useRoleList(request: RoleListRequest) {
  return useQuery({
    queryKey: [MEMBERSHIP_RESOURCES.role, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<RoleListDto>>('/role', {
        params: {
          SkipCount: (request.page - 1) * request.pageSize,
          MaxResultCount: request.pageSize,
          Sorting: 'Name ASC',
          Filter: request.filter || undefined,
        },
      })
      return data
    },
  })
}

/** `GET api/role/lookup` — the role picker of the role-assignment dialog. */
export function useRoleLookup() {
  return useQuery({
    queryKey: [MEMBERSHIP_RESOURCES.role, 'lookup'],
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>('/role/lookup')
      return data
    },
  })
}

/**
 * `GET api/permission/tree` — the complete catalogue in a single request.
 *
 * The matrix renders roughly 170 permissions; fetching them per row would be 170 requests, so
 * the tree endpoint exists precisely to avoid that. The catalogue is seeded and immutable at
 * runtime, hence the long stale time.
 */
export function usePermissionTree() {
  return useQuery({
    queryKey: [MEMBERSHIP_RESOURCES.permission, 'tree'],
    staleTime: 10 * 60 * 1000,
    queryFn: async () => {
      const { data } = await http.get<PermissionTreeDto>('/permission/tree')
      return data
    },
  })
}

/** `GET api/permission/user/{userId}` — effective set plus the explicit overrides behind it. */
export function useUserPermissions(userId: number | undefined) {
  return useQuery({
    queryKey: [MEMBERSHIP_RESOURCES.permission, 'user', userId],
    enabled: !!userId,
    queryFn: async () => {
      const { data } = await http.get<UserPermissionsDto>(`/permission/user/${userId}`)
      return data
    },
  })
}

// ---------------------------------------------------------------
// Mutations
//
// `useCreate` / `useUpdate` / `useDelete` from @/api/mutations cover `/user` and `/role`
// themselves. What follows are the sub-resource endpoints they cannot express, written against
// the same cache keys so a role change or a reset refreshes the lists as well.
// ---------------------------------------------------------------

/** `PUT api/user/{id}/roles` — replaces the whole role set of the user. */
export function useAssignRoles(userId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (roles: string[]) => {
      await http.put(`/user/${userId}/roles`, { roles })
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: resourceKey(MEMBERSHIP_RESOURCES.user) })
      await queryClient.invalidateQueries({
        queryKey: resourceKey(MEMBERSHIP_RESOURCES.permission),
      })
    },
  })
}

/**
 * `POST api/user/{id}/reset-password` — administrative reset.
 *
 * The server rotates the security stamp, so every outstanding refresh token of that user stops
 * working. The new password is write-only: it is never echoed back and never rendered.
 */
export function useResetPassword(userId: number) {
  return useMutation({
    mutationFn: async (newPassword: string) => {
      await http.post(`/user/${userId}/reset-password`, { newPassword })
    },
  })
}

/** `PUT api/user/{id}/active-state` — activates or deactivates the user. */
export function useSetUserActiveState() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, isActive }: { id: number; isActive: boolean }) => {
      await http.put(`/user/${id}/active-state`, { isActive })
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: resourceKey(MEMBERSHIP_RESOURCES.user) })
    },
  })
}

/** `PUT api/permission/user/{userId}` — replaces the explicit grant/deny overrides. */
export function useSaveUserPermissions(userId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (input: UpdateUserPermissionsInput) => {
      await http.put(`/permission/user/${userId}`, input)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: resourceKey(MEMBERSHIP_RESOURCES.permission),
      })
      await queryClient.invalidateQueries({ queryKey: resourceKey(MEMBERSHIP_RESOURCES.user) })
    },
  })
}

// ---------------------------------------------------------------
// Reference data
//
// `GET api/lookup/*` belongs to the settings module, but the user form needs the province and
// district lists to edit an address. The two hooks are repeated here rather than imported
// across module folders, so membership keeps working on its own (see MODULES.md).
// ---------------------------------------------------------------

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

// ---------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------

/** ISO timestamp -> `yyyy-MM-dd`, the value an `<input type="date">` expects. */
export function toDateInput(value: string | null | undefined): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '' : date.toISOString().slice(0, 10)
}

/** `<input type="date">` value -> the ISO date the API binds, or `null` when cleared. */
export function fromDateInput(value: string): string | null {
  return value ? value : null
}

/** Flattens the permission tree depth-first, keeping the catalogue order. */
export function flattenPermissions(nodes: PermissionTreeNodeDto[]): PermissionTreeNodeDto[] {
  return nodes.flatMap((node) => [node, ...flattenPermissions(node.children)])
}
