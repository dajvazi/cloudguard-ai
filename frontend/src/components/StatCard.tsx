import type { LucideIcon } from 'lucide-react'
import './StatCard.css'

interface StatCardProps {
  label: string
  value: string | number
  subtext?: string
  icon: LucideIcon
  variant?: 'default' | 'green' | 'yellow' | 'red' | 'blue'
}

export function StatCard({ label, value, subtext, icon: Icon, variant = 'default' }: StatCardProps) {
  return (
    <div className={`stat-card stat-card--${variant}`}>
      <div className="stat-card-icon">
        <Icon size={20} />
      </div>
      <div className="stat-card-content">
        <span className="stat-card-label">{label}</span>
        <span className="stat-card-value">{value}</span>
        {subtext && <span className="stat-card-subtext">{subtext}</span>}
      </div>
    </div>
  )
}
