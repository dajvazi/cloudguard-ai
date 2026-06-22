import { useEffect, useState } from 'react'
import { BrainCircuit } from 'lucide-react'
import { StatusBadge } from '../components/StatusBadge'
import { fetchAnomalies, type Anomaly } from '../api/client'
import './Anomalies.css'

export function Anomalies() {
  const [anomalies, setAnomalies] = useState<Anomaly[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetchAnomalies().then(setAnomalies).catch(() => {}).finally(() => setLoading(false))
  }, [])

  if (loading) return <div className="page-loading">Duke ngarkuar...</div>

  return (
    <div className="anomalies-page">
      <header className="page-header">
        <div>
          <h1>AI Analysis</h1>
          <p>Anomaly detection and AI-powered evaluation</p>
        </div>
        <span className="anomaly-total">{anomalies.length} detected</span>
      </header>

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
                <span className="anomaly-detail-time">
                  {new Date(a.detectedAt).toLocaleString()}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
