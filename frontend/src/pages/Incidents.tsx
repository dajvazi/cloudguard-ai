import { useEffect, useState } from 'react'
import { AlertTriangle } from 'lucide-react'
import { StatusBadge } from '../components/StatusBadge'
import { fetchAllIncidents, type Incident } from '../api/client'
import './Incidents.css'

export function Incidents() {
  const [incidents, setIncidents] = useState<Incident[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetchAllIncidents().then(setIncidents).catch(() => {}).finally(() => setLoading(false))
  }, [])

  if (loading) return <div className="page-loading">Duke ngarkuar...</div>

  const active = incidents.filter((i) => i.status !== 'Resolved')
  const resolved = incidents.filter((i) => i.status === 'Resolved')

  return (
    <div className="incidents-page">
      <header className="page-header">
        <div>
          <h1>Incidents</h1>
          <p>Live incidents detected across the fleet</p>
        </div>
        <span className="incidents-count">{active.length} active</span>
      </header>

      {incidents.length === 0 ? (
        <div className="empty-state-large">
          <AlertTriangle size={48} />
          <h3>No incidents recorded</h3>
          <p>Incidents will appear here when anomalies trigger them</p>
        </div>
      ) : (
        <div className="incidents-table-wrap">
          <table className="incidents-table">
            <thead>
              <tr>
                <th>Service</th>
                <th>Title</th>
                <th>Severity</th>
                <th>Status</th>
                <th>Root Cause</th>
                <th>Detected</th>
              </tr>
            </thead>
            <tbody>
              {active.map((inc) => (
                <tr key={inc.id} className="incident-row incident-row--active">
                  <td className="inc-service">{inc.cloudServiceName}</td>
                  <td className="inc-title">{inc.title}</td>
                  <td><StatusBadge status={inc.severity || 'Info'} size="sm" /></td>
                  <td><StatusBadge status={inc.status} size="sm" /></td>
                  <td className="inc-cause">{inc.rootCause || '—'}</td>
                  <td className="inc-time">{formatTime(inc.createdAt)}</td>
                </tr>
              ))}
              {resolved.map((inc) => (
                <tr key={inc.id} className="incident-row incident-row--resolved">
                  <td className="inc-service">{inc.cloudServiceName}</td>
                  <td className="inc-title">{inc.title}</td>
                  <td><StatusBadge status={inc.severity || 'Info'} size="sm" /></td>
                  <td><StatusBadge status={inc.status} size="sm" /></td>
                  <td className="inc-cause">{inc.rootCause || '—'}</td>
                  <td className="inc-time">{formatTime(inc.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function formatTime(iso: string): string {
  const d = new Date(iso)
  const now = Date.now()
  const diff = now - d.getTime()
  if (diff < 3600000) return `${Math.round(diff / 60000)} min ago`
  if (diff < 86400000) return `${Math.round(diff / 3600000)}h ago`
  return d.toLocaleDateString()
}
