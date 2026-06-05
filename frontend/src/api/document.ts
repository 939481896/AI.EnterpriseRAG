import apiClient from './client'
import type { Document, DocumentCategory, ApiResponse } from '@/types/document'

export const documentApi = {
  /**
   * Upload document
   */
  upload: async (
    file: File,
    onProgress?: (progress: number) => void
  ): Promise<ApiResponse<Document>> => {
    const formData = new FormData()
    formData.append('file', file)

    return apiClient.post('/api/document/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent) => {
        if (progressEvent.total && onProgress) {
          const progress = Math.round((progressEvent.loaded * 100) / progressEvent.total)
          onProgress(progress)
        }
      },
    })
  },

  /**
   * Get user's documents
   */
  getDocuments: async (page = 1, pageSize = 20): Promise<ApiResponse<{
    items: Document[]
    total: number
  }>> => {
    return apiClient.get('/api/document/list', {
      params: { page, pageSize },
    })
  },

  /**
   * Delete document
   */
  deleteDocument: async (documentId: string): Promise<ApiResponse> => {
    return apiClient.delete(`/api/document/${documentId}`)
  },

  /**
   * Get document categories
   */
  getCategories: async (): Promise<ApiResponse<DocumentCategory[]>> => {
    return apiClient.get('/api/document/categories')
  },

  /**
   * Get document preview URL
   */
  getPreviewUrl: (documentId: string): string => {
    return `${apiClient.defaults.baseURL}/api/document/${documentId}/preview`
  },
}
