import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { documentApi } from '@/api/document'
import type { UploadProgressInfo } from '@/types/document'
import { notification } from '@/services/notification'
import { uiText } from '@/config/uiText'
import { getErrorMessage } from '@/types/error'
import { queryKeys } from '@/config/queryKeys'

/**
 * Document list query hook.
 * Pagination is encoded in query key for independent caching per page.
 */
export function useDocuments(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: queryKeys.document.list(page, pageSize),
    queryFn: async () => {
      const response = await documentApi.getDocuments(page, pageSize)
      return response.data || { items: [], total: 0 }
    },
  })
}

export function useUploadDocument() {
  const queryClient = useQueryClient()
  const [uploadProgress, setUploadProgress] = useState<Record<string, UploadProgressInfo>>({})

  /**
   * Upload mutation with local progress tracker.
   * Progress map key format: <file-name>-<timestamp>.
   */
  const uploadMutation = useMutation({
    meta: { silentError: true },
    mutationFn: async (file: File) => {
      const fileId = `${file.name}-${Date.now()}`
      
      setUploadProgress((prev) => ({
        ...prev,
        [fileId]: {
          file,
          progress: 0,
          status: 'uploading',
        },
      }))

      const response = await documentApi.upload(file, (progress) => {
        setUploadProgress((prev) => ({
          ...prev,
          [fileId]: {
            ...prev[fileId],
            progress,
          },
        }))
      })

      setUploadProgress((prev) => ({
        ...prev,
        [fileId]: {
          ...prev[fileId],
          progress: 100,
          status: 'success',
          response: response.data,
        },
      }))

      return response
    },
    onSuccess: () => {
      // Invalidate all document pages after upload.
      queryClient.invalidateQueries({ queryKey: queryKeys.document.all })
      notification.success(uiText.document.uploadSuccess)
    },
    onError: (error: unknown, file) => {
      const fileId = `${file.name}-${Date.now()}`
      setUploadProgress((prev) => ({
        ...prev,
        [fileId]: {
          ...prev[fileId],
          status: 'error',
          error: getErrorMessage(error),
        },
      }))
      notification.error(uiText.document.uploadFailed)
    },
  })

  return {
    upload: uploadMutation.mutate,
    uploadProgress,
    isUploading: uploadMutation.isPending,
  }
}

export function useDeleteDocument() {
  const queryClient = useQueryClient()

  return useMutation({
    meta: { silentError: true },
    mutationFn: (documentId: string) => documentApi.deleteDocument(documentId),
    onSuccess: () => {
      // Deleting one document may affect pagination totals.
      queryClient.invalidateQueries({ queryKey: queryKeys.document.all })
      notification.success(uiText.document.deleteSuccess)
    },
    onError: () => {
      notification.error(uiText.document.deleteFailed)
    },
  })
}

export function useDocumentCategories() {
  return useQuery({
    queryKey: queryKeys.document.categories,
    queryFn: async () => {
      const response = await documentApi.getCategories()
      return response.data || []
    },
  })
}
