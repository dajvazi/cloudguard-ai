import { useEffect, useState, useCallback } from 'react'
import { AlertTriangle, Sparkles } from 'lucide-react'
import { StatusBadge } from '../components/StatusBadge'
import { DeleteAllButton } from '../components/DeleteAllButton'
import { SelfHealingBanner } from '../components/SelfHealingBanner'
import { SelfHealingDialog } from '../components/SelfHealingDialog'
import {
  fetchAllIncidents,
  purgeIncidents,
  type Incident,
  type SelfHealingResult,
} from '../api/client'
import './Incidents.css'
import '../components/SelfHealingBanner.css'

export function Incidents() {
  const [incidents, setIncidents] = useState<Incident[]>([])
  const [loading, setLoading] = useState(true)
  const [healDialogServiceId, setHealDialogServiceId] = useState<number | null>(null)
  const [healResult, setHealResult] = useState<SelfHealingResult | null>(null)

  const load = useCallback(async () => {
    try { setIncidents(await fetchAllIncidents()) } catch { /* empty */ }
    setLoading(false)
  }, [])

  useEffect(() => { load() }, [load])

  function handleHealComplete(result: SelfHealingResult) {
    setHealDialogServiceId(null)
    setHealResult(result)
    load()
  }

  if (loading) return <div className="page-loading">Loading...</div>

  const active = incidents.filter((i) => i.status !== 'Resolved')
  const resolved = incidents.filter((i) => i.status === 'Resolved')

  return (
    <div className="incidents-page">
      <header className="page-header">
        <div>
          <h1>Incidents</h1>
          <p>Live incidents detected across the fleet</p>
        </div>
        <div className="page-header-actions">
          <span className="incidents-count">{active.length} active</span>
          <DeleteAllButton
            label="Delete All"
            confirmMessage="Delete all incidents and linked recovery actions? This action cannot be undone."
            onDelete={purgeIncidents}
            onSuccess={() => load()}
          />
        </div>
      </header>

      {healResult && (
        <SelfHealingBanner result={healResult} onClose={() => setHealResult(null)} />
      )}

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
                <th>Action</th>
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
                  <td>
                    <button
                      className="btn-heal-sm"
                      onClick={() => setHealDialogServiceId(inc.cloudServiceId)}
                    >
                      <Sparkles size={12} />
                      <span>Self-Heal</span>
                    </button>
                  </td>
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
                  <td className="inc-resolved-action">—</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <SelfHealingDialog
        open={healDialogServiceId !== null}
        serviceId={healDialogServiceId}
        onClose={() => setHealDialogServiceId(null)}
        onComplete={handleHealComplete}
      />
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
