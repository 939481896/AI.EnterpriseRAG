export enum DocumentStatus {
  Pending = 0,
  Processing = 1,
  Completed = 2,
  Failed = 3,
}

export interface Document {
  id: string
  name: string
  fileType: string
  fileSize: number
  storagePath: string
  status: DocumentStatus
  createTime: string
  completeTime?: string
  uploadedBy: string
  tenantId?: string
  isPublic: boolean
  categoryId?: number
  categoryName?: string
}

export interface UploadProgressInfo {
  file: File
  progress: number
  status: 'uploading' | 'success' | 'error'
  response?: any
  error?: string
}

export interface DocumentCategory {
  id: number
  name: string
  description?: string
  documentCount: number
}
