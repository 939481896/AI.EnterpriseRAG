import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { message } from 'antd'
import { documentApi } from '@/api/document'
import type { UploadProgressInfo } from '@/types/document'

export function useDocuments(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ['documents', page, pageSize],
    queryFn: async () => {
      const response = await documentApi.getDocuments(page, pageSize)
      return response.data || { items: [], total: 0 }
    },
  })
}

export function useUploadDocument() {
  const queryClient = useQueryClient()
  const [uploadProgress, setUploadProgress] = useState<Record<string, UploadProgressInfo>>({})

  const uploadMutation = useMutation({
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
      queryClient.invalidateQueries({ queryKey: ['documents'] })
      message.success('文档上传成功')
    },
    onError: (error: any, file) => {
      const fileId = `${file.name}-${Date.now()}`
      setUploadProgress((prev) => ({
        ...prev,
        [fileId]: {
          ...prev[fileId],
          status: 'error',
          error: error.message,
        },
      }))
      message.error('上传失败')
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
    mutationFn: (documentId: string) => documentApi.deleteDocument(documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] })
      message.success('文档已删除')
    },
    onError: () => {
      message.error('删除失败')
    },
  })
}

export function useDocumentCategories() {
  return useQuery({
    queryKey: ['documentCategories'],
    queryFn: async () => {
      const response = await documentApi.getCategories()
      return response.data || []
    },
  })
}
