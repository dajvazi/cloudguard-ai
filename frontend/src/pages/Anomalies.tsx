import { useEffect, useState } from 'react'
import { BrainCircuit, Sparkles } from 'lucide-react'
import { StatusBadge } from '../components/StatusBadge'
import { DeleteAllButton } from '../components/DeleteAllButton'
import { SelfHealingBanner } from '../components/SelfHealingBanner'
import { SelfHealingDialog } from '../components/SelfHealingDialog'
import {
  fetchAnomalies,
  purgeAnomalies,
  type Anomaly,
  type SelfHealingResult,
} from '../api/client'
import './Anomalies.css'
import '../components/SelfHealingBanner.css'

export function Anomalies() {
  const [anomalies, setAnomalies] = useState<Anomaly[]>([])
  const [loading, setLoading] = useState(true)
  const [healDialogServiceId, setHealDialogServiceId] = useState<number | null>(null)
  const [healResult, setHealResult] = useState<SelfHealingResult | null>(null)

  function load() {
    setLoading(true)
    fetchAnomalies().then(setAnomalies).catch(() => {}).finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  function handleHealComplete(result: SelfHealingResult) {
    setHealDialogServiceId(null)
    setHealResult(result)
    load()
  }

  if (loading) return <div className="page-loading">Loading...</div>

  return (
    <div className="anomalies-page">
      <header className="page-header">
        <div>
          <h1>AI Analysis</h1>
          <p>Anomaly detection and AI-powered self-healing</p>
        </div>
        <div className="page-header-actions">
          <span className="anomaly-total">{anomalies.length} detected</span>
          <DeleteAllButton
            label="Delete All"
            confirmMessage="Delete all anomalies? This action cannot be undone."
            onDelete={purgeAnomalies}
            onSuccess={() => load()}
          />
        </div>
      </header>

      {healResult && (
        <SelfHealingBanner result={healResult} onClose={() => setHealResult(null)} />
      )}

      {anomalies.length === 0 ? (
        <div className="empty-state-large">
          <BrainCircuit size={48} />
          <h3>No anomalies detected</h3>
          <p>AI analysis results will appear here</p>
        </div>
      ) : (
        <div className="anomalies-grid">
          {anomalies.map((a) => (
            <div className="anomaly-detail-card" key={a.id}>
              <div className="anomaly-detail-header">
                <div className="anomaly-detail-title">
                  <BrainCircuit size={16} />
                  <strong>{a.anomalyType || 'Unknown'}</strong>
                </div>
                <StatusBadge status={a.severity || 'Info'} size="sm" />
              </div>
              <span className="anomaly-detail-service">{a.cloudServiceName}</span>
              {a.description && (
                <p className="anomaly-detail-desc">{a.description}</p>
              )}
              <div className="anomaly-detail-footer">
                {a.aiConfidence != null && (
                  <div className="confidence-meter">
                    <span className="confidence-label">Confidence</span>
                    <div className="confidence-track">
                      <div
                        className="confidence-bar-fill"
                        style={{ width: `${a.aiConfidence}%` }}
                      />
                    </div>
                    <span className="confidence-pct">{a.aiConfidence}%</span>
                  </div>
                )}
                <button
                  type="button"
                  className="btn-heal"
                  onClick={() => setHealDialogServiceId(a.cloudServiceId)}
                >
                  <Sparkles size={13} /> Self-Heal
                </button>
              </div>
            </div>
          ))}
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
