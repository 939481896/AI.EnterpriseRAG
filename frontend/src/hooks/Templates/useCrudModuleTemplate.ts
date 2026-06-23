import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { notification } from '@/services/notification'

/**
 * Hook template for CRUD modules.
 *
 * Copy this file and replace:
 * - `templateApi` with your domain API module
 * - query keys with domain keys from queryKeys
 * - message text with uiText domain text
 */

type TemplateItem = {
  id: string
  name: string
}

type TemplateCreateInput = {
  name: string
}

type TemplateUpdateInput = {
  name: string
}

const templateApi = {
  async list(): Promise<TemplateItem[]> {
    return []
  },
  async create(_input: TemplateCreateInput): Promise<void> {
    return
  },
  async update(_id: string, _input: TemplateUpdateInput): Promise<void> {
    return
  },
  async remove(_id: string): Promise<void> {
    return
  },
}

export function useTemplateList() {
  return useQuery({
    queryKey: ['template', 'list'],
    queryFn: () => templateApi.list(),
  })
}

export function useTemplateCreate() {
  const queryClient = useQueryClient()
  return useMutation({
    meta: { silentError: true },
    mutationFn: (input: TemplateCreateInput) => templateApi.create(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['template', 'list'] })
      notification.success('创建成功')
    },
  })
}

export function useTemplateUpdate() {
  const queryClient = useQueryClient()
  return useMutation({
    meta: { silentError: true },
    mutationFn: ({ id, input }: { id: string; input: TemplateUpdateInput }) =>
      templateApi.update(id, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['template', 'list'] })
      notification.success('更新成功')
    },
  })
}

export function useTemplateDelete() {
  const queryClient = useQueryClient()
  return useMutation({
    meta: { silentError: true },
    mutationFn: (id: string) => templateApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['template', 'list'] })
      notification.success('删除成功')
    },
  })
}
