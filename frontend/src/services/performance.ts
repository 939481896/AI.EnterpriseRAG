/**
 * Performance Monitoring
 *
 * Tracks:
 * - Core Web Vitals (LCP, FID, CLS)
 * - API response times
 * - Page load metrics
 * - Memory usage
 */

interface PerformanceMetric {
  name: string
  value: number
  unit: string
  timestamp: number
}

class PerformanceMonitor {
  private metrics: PerformanceMetric[] = []
  private isEnabled: boolean = true

  constructor() {
    this.isEnabled = shouldEnablePerformanceMonitoring()
  }

  /**
   * Observe Core Web Vitals
   */
  observeWebVitals() {
    if (!this.isEnabled) return

    // Largest Contentful Paint (LCP)
    if ('PerformanceObserver' in window) {
      try {
        const lcpObserver = new PerformanceObserver((entryList) => {
          const entries = entryList.getEntries()
          const lastEntry = entries[entries.length - 1] as any
          this.recordMetric('LCP', lastEntry.renderTime || lastEntry.loadTime, 'ms')
        })
        lcpObserver.observe({ entryTypes: ['largest-contentful-paint'] })
      } catch (e) {
        console.warn('[Performance] LCP observer not supported')
      }
    }

    // First Input Delay (FID) / Interaction to Next Paint (INP)
    if ('PerformanceObserver' in window) {
      try {
        const fidObserver = new PerformanceObserver((entryList) => {
          const entries = entryList.getEntries()
          entries.forEach((entry: any) => {
            this.recordMetric('INP', entry.processingDuration, 'ms')
          })
        })
        fidObserver.observe({ entryTypes: ['first-input', 'largest-contentful-paint'] })
      } catch (e) {
        console.warn('[Performance] FID observer not supported')
      }
    }

    // Cumulative Layout Shift (CLS)
    let clsValue = 0
    if ('PerformanceObserver' in window) {
      try {
        const clsObserver = new PerformanceObserver((entryList) => {
          for (const entry of entryList.getEntries()) {
            if (!(entry as any).hadRecentInput) {
              clsValue += (entry as any).value
              this.recordMetric('CLS', clsValue, 'unitless')
            }
          }
        })
        clsObserver.observe({ entryTypes: ['layout-shift'] })
      } catch (e) {
        console.warn('[Performance] CLS observer not supported')
      }
    }
  }

  /**
   * Record API timing
   */
  recordAPITiming(endpoint: string, duration: number, status: number) {
    if (!this.isEnabled) return

    this.recordMetric(`API_${status}`, duration, 'ms', {
      endpoint,
      status,
    })
  }

  /**
   * Record page transition time
   */
  recordPageTransition(fromRoute: string, toRoute: string, duration: number) {
    if (!this.isEnabled) return

    this.recordMetric('PAGE_TRANSITION', duration, 'ms', {
      from: fromRoute,
      to: toRoute,
    })
  }

  /**
   * Record component render time
   */
  recordComponentRender(componentName: string, duration: number) {
    if (!this.isEnabled) return

    this.recordMetric(`COMPONENT_RENDER`, duration, 'ms', {
      component: componentName,
    })
  }

  /**
   * Get current memory usage
   */
  getMemoryUsage(): { used: number; limit: number } | null {
    if (!(performance as any).memory) {
      return null
    }

    const memory = (performance as any).memory
    return {
      used: Math.round(memory.usedJSHeapSize / 1048576), // Convert to MB
      limit: Math.round(memory.jsHeapSizeLimit / 1048576),
    }
  }

  /**
   * Record custom metric
   */
  private recordMetric(name: string, value: number, unit: string, metadata?: any) {
    const metric: PerformanceMetric = {
      name,
      value,
      unit,
      timestamp: Date.now(),
    }

    this.metrics.push(metric)

    // Send to analytics/monitoring service
    this.sendMetric(metric, metadata)

    // Log in development
    if (import.meta.env.DEV) {
      console.log(`[Performance] ${name}: ${value}${unit}`)
    }
  }

  /**
   * Send metric to monitoring service
   */
  private sendMetric(metric: PerformanceMetric, metadata?: any) {
    // This would integrate with your analytics service
    // Example: Google Analytics, Datadog, NewRelic, etc.

    if (import.meta.env.VITE_ANALYTICS_ENABLED === 'true') {
      // Send to analytics endpoint
      const payload = {
        metric: metric.name,
        value: metric.value,
        unit: metric.unit,
        timestamp: metric.timestamp,
        metadata,
      }

      // Use beacon API for reliability (won't block page unload)
      navigator.sendBeacon('/api/metrics', JSON.stringify(payload))
    }
  }

  /**
   * Get all recorded metrics
   */
  getMetrics(): PerformanceMetric[] {
    return [...this.metrics]
  }

  /**
   * Get average metric value
   */
  getAverageMetric(metricName: string): number | null {
    const filtered = this.metrics.filter((m) => m.name === metricName)
    if (filtered.length === 0) return null

    const sum = filtered.reduce((acc, m) => acc + m.value, 0)
    return sum / filtered.length
  }

  /**
   * Clear metrics
   */
  clear() {
    this.metrics = []
  }
}

// Export singleton instance
export const performanceMonitor = new PerformanceMonitor()

/**
 * Initialize performance monitoring
 */
export function initPerformanceMonitoring() {
  performanceMonitor.observeWebVitals()

  // Log initial memory usage
  const memory = performanceMonitor.getMemoryUsage()
  if (memory) {
    console.log(`[Performance] Memory usage: ${memory.used}MB / ${memory.limit}MB`)
  }
}

/**
 * Hook for React to measure component render time
 */
export function usePerformanceTracing(componentName: string, enabled = true) {
  if (!enabled) return

  const startTime = performance.now()

  return () => {
    const duration = performance.now() - startTime
    performanceMonitor.recordComponentRender(componentName, duration)
  }
}

/**
 * Helper: Check if performance monitoring should be enabled
 */
function shouldEnablePerformanceMonitoring(): boolean {
  // Enable in all environments, but adjust logging
  return true
}
