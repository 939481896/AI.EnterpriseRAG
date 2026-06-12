/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      // 🎨 映射现有的设计 Token
      colors: {
        primary: {
          DEFAULT: '#1890ff',
          light: '#40a9ff',
          lighter: '#91d5ff',
          lightest: '#e6f7ff',
          dark: '#096dd9',
        },
        success: '#52c41a',
        warning: '#faad14',
        error: '#ff4d4f',
        info: '#1890ff',
        // 文本颜色
        text: {
          primary: '#262626',
          secondary: '#8c8c8c',
          tertiary: '#bfbfbf',
        },
        // 背景颜色
        bg: {
          body: '#ffffff',
          light: '#fafafa',
          lighter: '#f5f5f5',
        },
        // 边框颜色
        border: {
          DEFAULT: '#d9d9d9',
          light: '#f0f0f0',
          lighter: '#fafafa',
        },
      },
      // 📏 间距系统 (保持与现有设计一致)
      spacing: {
        'xs': '4px',
        'sm': '8px',
        'md': '12px',
        'base': '16px',
        'lg': '24px',
        'xl': '32px',
        '2xl': '48px',
        '3xl': '64px',
      },
      // 🔤 字体大小
      fontSize: {
        'xs': '12px',
        'sm': '13px',
        'base': '14px',
        'md': '16px',
        'lg': '18px',
        'xl': '20px',
        '2xl': '24px',
        '3xl': '32px',
      },
      // 🔘 边框圆角
      borderRadius: {
        'xs': '2px',
        'sm': '4px',
        'base': '6px',
        'md': '8px',
        'lg': '12px',
        'xl': '16px',
      },
      // 🌑 阴影
      boxShadow: {
        'xs': '0 1px 2px rgba(0, 0, 0, 0.05)',
        'sm': '0 2px 4px rgba(0, 0, 0, 0.08)',
        'base': '0 4px 8px rgba(0, 0, 0, 0.1)',
        'md': '0 8px 16px rgba(0, 0, 0, 0.12)',
        'lg': '0 12px 24px rgba(0, 0, 0, 0.15)',
      },
      // ⏱️ 过渡动画
      transitionDuration: {
        'fast': '150ms',
        'base': '200ms',
        'slow': '300ms',
      },
    },
  },
  plugins: [],
  // ⚠️ 重要：禁用 Preflight，避免与 Ant Design 冲突
  corePlugins: {
    preflight: false,
  },
}
