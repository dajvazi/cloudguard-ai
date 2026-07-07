import { useEffect, useState } from 'react'
import {
  Wrench,
  ArrowRight,
  Activity,
  BrainCircuit,
  AlertTriangle,
  RefreshCw,
  ShieldCheck,
} from 'lucide-react'
import { StatusBadge } from '../components/StatusBadge'
import { DeleteAllButton } from '../components/DeleteAllButton'
import { fetchRecoveryActions, purgeRecoveryActions, type RecoveryAction } from '../api/client'
import './Recovery.css'

const pipelineSteps = [
  { icon: Activity, label: 'Metrics Collection', desc: 'Gathering data' },
  { icon: BrainCircuit, label: 'AI Analysis', desc: 'Model evaluation' },
  { icon: AlertTriangle, label: 'Anomaly Detection', desc: 'Pattern matching' },
  { icon: AlertTriangle, label: 'Incident Creation', desc: 'Auto-triggered' },
  { icon: Wrench, label: 'Recovery Engine', desc: 'Select playbook' },
  { icon: RefreshCw, label: 'Service Restart', desc: 'Execute action' },
  { icon: ShieldCheck, label: 'Healthy State', desc: 'Verified' },
]

export function Recovery() {
  const [actions, setActions] = useState<RecoveryAction[]>([])
  const [loading, setLoading] = useState(true)

  function load() {
    setLoading(true)
    fetchRecoveryActions().then(setActions).catch(() => {}).finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  if (loading) return <div className="page-loading">Loading...</div>

  return (
    <div className="recovery-page">
      <header className="page-header">
        <div>
          <h1>Recovery Actions</h1>
          <p>Self-healing pipeline and automated remediation</p>
        </div>
        <DeleteAllButton
          label="Delete All"
            confirmMessage="Delete all recovery actions? This action cannot be undone."
          onDelete={purgeRecoveryActions}
          onSuccess={() => load()}
        />
      </header>

      <section className="pipeline-section">
        <h2>Self-Healing Pipeline</h2>
        <p className="pipeline-subtitle">End-to-end automated remediation flow</p>
        <div className="pipeline">
          {pipelineSteps.map((step, i) => (
            <div className="pipeline-step" key={i}>
              <div className="pipeline-step-icon">
                <step.icon size={18} />
              </div>
              <span className="pipeline-step-label">{step.label}</span>
              <span className="pipeline-step-desc">{step.desc}</span>
              {i < pipelineSteps.length - 1 && (
                <ArrowRight size={14} className="pipeline-arrow" />
              )}
            </div>
          ))}
        </div>
      </section>

      <section className="recovery-list-section">
        <h2>Recovery History</h2>
        {actions.length === 0 ? (
          <div className="empty-state-large">
            <Wrench size={48} />
            <h3>No recovery actions yet</h3>
            <p>Actions will appear here when the self-healing pipeline executes</p>
          </div>
        ) : (
          <div className="recovery-table-wrap">
            <table className="recovery-table">
              <thead>
                <tr>
                  <th>Incident</th>
                  <th>Action Type</th>
                  <th>Status</th>
                  <th>Description</th>
                  <th>Executed At</th>
                </tr>
              </thead>
              <tbody>
                {actions.map((a) => (
                  <tr key={a.id}>
                    <td className="rec-incident">INC-{a.incidentId}</td>
                    <td className="rec-type">{a.actionType || '—'}</td>
                    <td><StatusBadge status={a.actionStatus} size="sm" /></td>
                    <td className="rec-desc">{a.description || '—'}</td>
                    <td className="rec-time">{new Date(a.executedAt).toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  )
}
