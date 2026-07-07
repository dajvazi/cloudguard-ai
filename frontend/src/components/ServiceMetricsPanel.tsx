import { Activity, Cpu, HardDrive, Network, AlertTriangle } from 'lucide-react'
import type { Metric } from '../api/client'
import './ServiceMetricsPanel.css'

const PRIORITY_METRICS = [
  'CPUUtilization',
  'NetworkIn',
  'NetworkOut',
  'EBSWriteBytes',
  'EBSReadOps',
  'StatusCheckFailed',
  'MemoryUsage',
  'LatencyMs',
]

const GAUGE_RADIUS = 42
const GAUGE_STROKE = 7
const GAUGE_ARC = Math.PI * GAUGE_RADIUS

function formatValue(value: number | null, unit: string | null): string {
  if (value === null) return '—'
  switch (unit) {
    case 'Percent':
      return `${value.toFixed(1)}%`
    case 'Bytes':
      if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`
      if (value >= 1_000) return `${(value / 1_000).toFixed(1)}K`
      return `${value.toFixed(0)}`
    case 'Milliseconds':
      return `${value.toFixed(1)}`
    case 'Count':
      return value.toFixed(0)
    default:
      return value.toFixed(1)
  }
}

function formatRange(value: number | null, unit: string | null): string {
  if (value === null) return '—'
  switch (unit) {
    case 'Percent':
      return `${value.toFixed(0)}%`
    case 'Bytes':
      if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`
      if (value >= 1_000) return `${(value / 1_000).toFixed(0)}K`
      return `${value.toFixed(0)}`
    default:
      return value.toFixed(1)
  }
}

function metricStatus(name: string | null, value: number | null): 'ok' | 'warn' | 'critical' | 'neutral' {
  if (value === null) return 'neutral'
  const n = name ?? ''
  if (n.includes('StatusCheckFailed') || n.includes('Error')) {
    return value > 0 ? 'critical' : 'ok'
  }
  if (n === 'CPUUtilization' || n.includes('CPU')) {
    if (value >= 85) return 'critical'
    if (value >= 60) return 'warn'
    return 'ok'
  }
  if (n.includes('Memory')) {
    if (value >= 90) return 'critical'
    if (value >= 75) return 'warn'
    return 'ok'
  }
  return 'neutral'
}

function gaugePercent(name: string | null, value: number | null, max: number | null): number {
  if (value === null) return 0
  const n = name ?? ''
  if (n === 'CPUUtilization' || n.includes('Memory') || n.includes('%')) {
    return Math.min(100, Math.max(0, value))
  }
  if (n.includes('StatusCheckFailed') || n.includes('Error')) {
    return value > 0 ? 100 : 0
  }
  if (max && max > 0) return Math.min(100, (value / max) * 100)
  return Math.min(100, Math.max(8, value > 0 ? 40 : 0))
}

function metricIcon(name: string | null) {
  const n = name ?? ''
  if (n.includes('CPU')) return Cpu
  if (n.includes('Network')) return Network
  if (n.includes('EBS') || n.includes('Disk')) return HardDrive
  if (n.includes('Status') || n.includes('Error')) return AlertTriangle
  return Activity
}

function sortMetrics(metrics: Metric[]): Metric[] {
  const latestByName = new Map<string, Metric>()
  for (const m of metrics) {
    const name = m.metricName ?? 'Unknown'
    const existing = latestByName.get(name)
    const value = m.value ?? 0
    const existingValue = existing?.value ?? 0
    if (!existing || value >= existingValue)
      latestByName.set(name, m)
  }

  return [...latestByName.values()].sort((a, b) => {
    const ai = PRIORITY_METRICS.indexOf(a.metricName ?? '')
    const bi = PRIORITY_METRICS.indexOf(b.metricName ?? '')
    return (ai === -1 ? 999 : ai) - (bi === -1 ? 999 : bi)
  })
}

interface GaugeCardProps {
  metric: Metric
  compact?: boolean
}

function GaugeCard({ metric, compact }: GaugeCardProps) {
  const status = metricStatus(metric.metricName, metric.value)
  const pct = gaugePercent(metric.metricName, metric.value, metric.maximum)
  const offset = GAUGE_ARC - (pct / 100) * GAUGE_ARC
  const Icon = metricIcon(metric.metricName)
  const unitSuffix = metric.unit === 'Percent' ? '%' : metric.unit === 'Milliseconds' ? 'ms' : ''

  return (
    <div className={`gauge-card gauge-card--${status}`}>
      <div className="gauge-card-header">
        <Icon size={compact ? 11 : 12} />
        <span className="gauge-card-title">{metric.metricName || 'Metric'}</span>
      </div>

      <div className={`gauge-ring ${compact ? 'gauge-ring--compact' : ''}`}>
        <svg viewBox="0 0 100 58" className="gauge-svg">
          <path
            className="gauge-track"
            d="M 8 50 A 42 42 0 0 1 92 50"
            fill="none"
            strokeWidth={GAUGE_STROKE}
            strokeLinecap="round"
          />
          <path
            className={`gauge-fill gauge-fill--${status}`}
            d="M 8 50 A 42 42 0 0 1 92 50"
            fill="none"
            strokeWidth={GAUGE_STROKE}
            strokeLinecap="round"
            strokeDasharray={GAUGE_ARC}
            strokeDashoffset={offset}
          />
        </svg>
        <div className="gauge-center">
          <span className="gauge-value">{formatValue(metric.value, metric.unit)}</span>
          {unitSuffix && <span className="gauge-unit">{unitSuffix}</span>}
        </div>
      </div>

      <div className="gauge-range">
        <span>↓ {formatRange(metric.minimum, metric.unit)}</span>
        <span>↑ {formatRange(metric.maximum, metric.unit)}</span>
      </div>
    </div>
  )
}

interface ServiceMetricsPanelProps {
  metrics: Metric[]
  compact?: boolean
}

export function ServiceMetricsPanel({ metrics, compact = false }: ServiceMetricsPanelProps) {
  if (metrics.length === 0) {
    return <div className="metrics-panel-empty">No metrics for this resource</div>
  }

  const sorted = sortMetrics(metrics)
  const visible = compact ? sorted.slice(0, 6) : sorted

  return (
    <div className={`metrics-panel ${compact ? 'metrics-panel--compact' : ''}`}>
      <div className="metrics-panel-grid">
        {visible.map((m) => (
          <GaugeCard key={m.id} metric={m} compact={compact} />
        ))}
      </div>

      {compact && sorted.length > 6 && (
        <div className="metrics-panel-more">+{sorted.length - 6} more metrics — click to view all</div>
      )}
    </div>
  )
}
