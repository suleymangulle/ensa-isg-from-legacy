import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { useToast } from 'rich-react-component'
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
  /**
   * Confirmation raised once the write has landed, through the library's toast queue. Each helper
   * has a sensible default — `common.saved` for a write, `common.deleted` for a delete — and
   * `null` silences it, which is what a screen wants when it reports the outcome itself.
   *
   * Failures are not raised here on purpose: a failed save belongs next to the field that caused
   * it, in the `error` prop of `Modal` or in an `ErrorPanel`, not in a message that fades.
   */
  successMessage?: string | null
}

/**
 * Resolves the confirmation a write helper raises once the server has accepted it.
 *
 * The message is translated at the moment it is shown rather than when the hook is created, so a
 * language switch mid-session cannot leave a stale sentence behind.
 */
function useSuccessNotice(message: string | null | undefined, fallbackKey: string | null) {
  const { t } = useTranslation()
  const toast = useToast()

  return () => {
    if (message === null) return
    const text = message ?? (fallbackKey ? t(fallbackKey) : null)
    if (text) toast.success(text)
  }
}

/** `POST /api/{resource}` */
export function useCreate<TInput, TResult = TInput>(
  resource: string,
  options: MutationOptions<TResult> = {},
) {
  const queryClient = useQueryClient()
  const notify = useSuccessNotice(options.successMessage, 'common.saved')

  return useMutation({
    mutationFn: async (input: TInput) => {
      const { data } = await http.post<TResult>(`/${resource}`, input)
      return data
    },
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: resourceKey(resource) })
      notify()
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
  const notify = useSuccessNotice(options.successMessage, 'common.saved')

  return useMutation({
    mutationFn: async ({ id, input }: { id: number; input: TInput }) => {
      const { data } = await http.put<TResult>(`/${resource}/${id}`, input)
      return data
    },
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: resourceKey(resource) })
      notify()
      options.onSuccess?.(result)
    },
  })
}

/** `DELETE /api/{resource}/{id}` */
export function useDelete(resource: string, options: MutationOptions<void> = {}) {
  const queryClient = useQueryClient()
  const notify = useSuccessNotice(options.successMessage, 'common.deleted')

  return useMutation({
    mutationFn: async (id: number) => {
      await http.delete(`/${resource}/${id}`)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: resourceKey(resource) })
      notify()
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
  // A workflow action is not a save: "Kaydedildi" would be wrong for a submit or an approval, so
  // it stays silent unless the caller names the sentence.
  const notify = useSuccessNotice(options.successMessage, null)

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
      notify()
      options.onSuccess?.(result)
    },
  })
}
