import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type AppLocale = 'zh-CN' | 'en-US'

const DEFAULT_LOCALE: AppLocale = 'zh-CN'

export const dayjsLocaleMap: Record<AppLocale, string> = {
  'zh-CN': 'zh-cn',
  'en-US': 'en',
}

interface LocaleState {
  locale: AppLocale
  setLocale: (locale: AppLocale) => void
}

export const useLocaleStore = create<LocaleState>()(
  persist(
    (set) => ({
      locale: DEFAULT_LOCALE,
      setLocale: (locale) => {
        set({ locale })
      },
    }),
    {
      name: 'locale-storage',
      partialize: (state) => ({ locale: state.locale }),
    }
  )
)

export function getLocale(): AppLocale {
  return useLocaleStore.getState().locale
}
