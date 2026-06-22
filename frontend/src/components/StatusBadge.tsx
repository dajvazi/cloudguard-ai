import './StatusBadge.css'

interface StatusBadgeProps {
  status: string
  size?: 'sm' | 'md'
}

export function StatusBadge({ status, size = 'md' }: StatusBadgeProps) {
  const variant = getVariant(status)
  return (
    <span className={`status-badge status-badge--${variant} status-badge--${size}`}>
      <span className="status-badge-dot" />
      {status}
    </span>
  )
}

function getVariant(status: string): string {
  const s = status.toLowerCase()
  if (['healthy', 'resolved', 'completed', 'operational'].includes(s)) return 'green'
  if (['warning', 'investigating', 'pending', 'info'].includes(s)) return 'yellow'
  if (['critical', 'failed', 'error'].includes(s)) return 'red'
  if (['mitigating', 'recovering', 'inprogress', 'in_progress'].includes(s)) return 'blue'
  if (['open'].includes(s)) return 'orange'
  return 'default'
}
