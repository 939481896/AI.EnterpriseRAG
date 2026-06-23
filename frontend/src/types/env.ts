/**
 * Environment variables type definitions
 */

/** ✅ Import.meta.env typed interface */
export interface ImportMetaEnv {
  VITE_API_URL?: string
  VITE_API_BASE_URL?: string
  VITE_API_TIMEOUT?: string
  MODE: 'development' | 'production'
  DEV: boolean
  PROD: boolean
}

/** ✅ Get environment variable with type safety */
export function getEnv<K extends keyof ImportMetaEnv>(key: K): ImportMetaEnv[K] | undefined {
  const env = import.meta.env as unknown as ImportMetaEnv
  return env[key]
}

/** ✅ Get API base URL */
export function getApiBaseUrl(): string {
  return getEnv('VITE_API_BASE_URL') || getEnv('VITE_API_URL') || 'http://localhost:5243'
}

/** ✅ Get API timeout */
export function getApiTimeout(): number {
  const timeout = getEnv('VITE_API_TIMEOUT')
  return timeout ? parseInt(timeout, 10) : 300000 // Default 5 minutes
}
