import { useMutation, useQueryClient } from '@tanstack/react-query'
import { http } from './http'

/**
 * Write helpers shared by every module.
 *
 * They exist so a create, an update and a delete look the same everywhere and invalidate the
 * same cache keys as `usePagedList` and `useEntity` populate — a hand-rolled mutation that
 * forgets to invalidate leaves a stale list on screen, which reads as a lost save.
 */

/** Cache key used by `usePagedList` and `useEntity` for a resource. */
export function resourceKey(resource: string) {
  return [resource] as const
}

interface MutationOptions<TResult> {
  /** Called after the cache has been invalidated. */
  onSuccess?: (result: TResult) => void
}

/** `POST /api/{resource}` */
export function useCreate<TInput, TResult = TInput>(
  resource: string,
  options: MutationOptions<TResult> = {},
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (input: TInput) => {
      const { data } = await http.post<TResult>(`/${resource}`, input)
      return data
    },
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: resourceKey(resource) })
      options.onSuccess?.(result)
    },
  })
}

/** `PUT /api/{resource}/{id}` */
export function useUpdate<TInput, TResult = TInput>(
  resource: string,
  options: MutationOptions<TResult> = {},
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, input }: { id: number; input: TInput }) => {
      const { data } = await http.put<TResult>(`/${resource}/${id}`, input)
      return data
    },
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: resourceKey(resource) })
      options.onSuccess?.(result)
    },
  })
}

/** `DELETE /api/{resource}/{id}` */
export function useDelete(resource: string, options: MutationOptions<void> = {}) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: number) => {
      await http.delete(`/${resource}/${id}`)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: resourceKey(resource) })
      options.onSuccess?.()
    },
  })
}

/**
 * `POST /api/{path}` for the workflow endpoints that are neither a create nor an update —
 * submit, approve, reject, cancel and so on. `invalidates` names the resources whose lists the
 * action changes.
 */
export function useAction<TInput = void, TResult = void>(
  path: (input: TInput) => string,
  invalidates: string[],
  options: MutationOptions<TResult> = {},
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (input: TInput) => {
      const { data } = await http.post<TResult>(`/${path(input)}`, input ?? {})
      return data
    },
    onSuccess: async (result) => {
      await Promise.all(
        invalidates.map((resource) =>
          queryClient.invalidateQueries({ queryKey: resourceKey(resource) }),
        ),
      )
      options.onSuccess?.(result)
    },
  })
}
