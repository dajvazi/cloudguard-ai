import { useEffect, useState, useCallback } from 'react'
import {
  Server,
  ShieldCheck,
  AlertTriangle,
  CheckCircle2,
  Activity,
  Zap,
  RefreshCw,
  Sparkles,
  Cloud,
} from 'lucide-react'
import { StatCard } from '../components/StatCard'
import { StatusBadge } from '../components/StatusBadge'
import { CloudImportDialog } from '../components/CloudImportDialog'
import {
  fetchServices,
  fetchActiveIncidents,
  fetchAnomalies,
  triggerSelfHealingFromAnomaly,
  type CloudService,
  type Incident,
  type Anomaly,
  type SelfHealingResult,
} from '../api/client'
import './Dashboard.css'

export function Dashboard() {
  const [services, setServices] = useState<CloudService[]>([])
  const [incidents, setIncidents] = useState<Incident[]>([])
  const [anomalies, setAnomalies] = useState<Anomaly[]>([])
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [healingId, setHealingId] = useState<number | null>(null)
  const [healResult, setHealResult] = useState<SelfHealingResult | null>(null)
  const [cloudImportOpen, setCloudImportOpen] = useState(false)

  const load = useCallback(async (showRefresh = false) => {
    if (showRefresh) setRefreshing(true)
    else setLoading(true)

    const [svc, inc, anom] = await Promise.allSettled([
      fetchServices(),
      fetchActiveIncidents(),
      fetchAnomalies(),
    ])
    if (svc.status === 'fulfilled') setServices(svc.value)
    if (inc.status === 'fulfilled') setIncidents(inc.value)
    if (anom.status === 'fulfilled') setAnomalies(anom.value)
    setLoading(false)
    setRefreshing(false)
  }, [])

  useEffect(() => { load() }, [load])

  const handleAutoHeal = async (anomalyId: number) => {
    setHealingId(anomalyId)
    setHealResult(null)
    try {
      const result = await triggerSelfHealingFromAnomaly(anomalyId)
      setHealResult(result)
      await load(true)
    } catch {
      setHealResult({ success: false, message: 'Self-healing failed', anomalyId: null, incidentId: null, recoveryActionId: null, aiAnalysis: null })
    }
    setHealingId(null)
  }

  const healthyCount = services.filter((s) => s.status === 'Healthy').length
  const healthyPercent = services.length > 0
    ? Math.round((healthyCount / services.length) * 100)
    : 0

  if (loading) {
    return <div className="page-loading">Duke ngarkuar...</div>
  }

  return (
    <div className="dashboard">
      <header className="page-header">
        <div>
          <h1>Operations Overview</h1>
          <p>Real-time cloud health, anomaly detection, and automated remediation</p>
        </div>
        <div className="header-actions">
          <button
            className="btn-secondary"
            onClick={() => setCloudImportOpen(true)}
          >
            <Cloud size={14} />
            Import Cloud
          </button>
          <button
            className="btn-icon"
            onClick={() => load(true)}
            disabled={refreshing}
            title="Refresh data"
          >
            <RefreshCw size={16} className={refreshing ? 'spinner' : ''} />
          </button>
          <StatusBadge status="Operational" />
        </div>
      </header>

      {healResult && (
        <div className={`toast toast--${healResult.success ? 'success' : 'error'}`}>
          <CheckCircle2 size={16} />
          <div className="toast-content">
            <strong>{healResult.success ? 'Self-Healing Complete' : 'Self-Healing Failed'}</strong>
            <span>{healResult.message}</span>
            {healResult.aiAnalysis && (
              <span className="toast-detail">
                Action: {healResult.aiAnalysis.actionType} · {healResult.aiAnalysis.rootCause}
              </span>
            )}
          </div>
          <button className="toast-close" onClick={() => setHealResult(null)}>
            <Zap size={14} />
          </button>
        </div>
      )}

      <section className="stats-grid">
        <StatCard icon={Server} label="Total Services" value={services.length} variant="default" />
        <StatCard icon={ShieldCheck} label="Healthy Services" value={healthyCount} subtext={`${healthyPercent}%`} variant="green" />
        <StatCard icon={AlertTriangle} label="Active Incidents" value={incidents.length} variant="red" />
        <StatCard icon={CheckCircle2} label="Anomalies Detected" value={anomalies.length} variant="yellow" />
      </section>

      <div className="dashboard-grid">
        <section className="card">
          <h2>Service Health Overview</h2>
          <p className="card-subtitle">Status across platform services</p>
          <div className="services-table">
            <div className="table-header">
              <span>Service</span>
              <span>Type</span>
              <span>Status</span>
            </div>
            {services.slice(0, 10).map((svc) => (
              <div className="table-row" key={svc.id}>
                <span className="service-name">{svc.name}</span>
                <span className="service-type">{svc.type}</span>
                <StatusBadge status={svc.status} size="sm" />
              </div>
            ))}
            {services.length > 10 && (
              <p className="table-more">+{services.length - 10} more services</p>
            )}
          </div>
        </section>

        <section className="card">
          <h2>Incident Center</h2>
          <p className="card-subtitle">
            <Activity size={14} />
            {incidents.length} active
          </p>
          {incidents.length === 0 ? (
            <div className="empty-state">
              <Zap size={32} />
              <p>No active incidents</p>
            </div>
          ) : (
            <div className="incidents-list">
              {incidents.map((inc) => (
                <div className="incident-item" key={inc.id}>
                  <div className="incident-info">
                    <strong>{inc.title}</strong>
                    <span className="incident-meta">
                      {inc.cloudServiceName}
                      {inc.rootCause && ` · ${inc.rootCause}`}
                    </span>
                  </div>
                  <div className="incident-actions">
                    <StatusBadge status={inc.severity || 'Info'} size="sm" />
                    <StatusBadge status={inc.status} size="sm" />
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>

      {anomalies.length > 0 && (
        <section className="card">
          <div className="card-header-row">
            <div>
              <h2>Latest AI Analysis</h2>
              <p className="card-subtitle">Recent anomaly evaluations</p>
            </div>
          </div>
          <div className="anomalies-preview">
            {anomalies.slice(0, 3).map((a) => (
              <div className="anomaly-card" key={a.id}>
                <div className="anomaly-header">
                  <StatusBadge status={a.severity || 'Info'} size="sm" />
                  <span className="anomaly-service">{a.cloudServiceName}</span>
                </div>
                <p className="anomaly-desc">{a.description || a.anomalyType}</p>
                {a.aiConfidence != null && (
                  <div className="anomaly-confidence">
                    <span>AI Confidence</span>
                    <div className="confidence-bar">
                      <div className="confidence-fill" style={{ width: `${a.aiConfidence}%` }} />
                    </div>
                    <span className="confidence-value">{a.aiConfidence}%</span>
                  </div>
                )}
                <button
                  className="btn-heal"
                  onClick={() => handleAutoHeal(a.id)}
                  disabled={healingId !== null}
                >
                  {healingId === a.id ? (
                    <><RefreshCw size={13} className="spinner" /> Healing...</>
                  ) : (
                    <><Sparkles size={13} /> Trigger Auto-Heal</>
                  )}
                </button>
              </div>
            ))}
          </div>
        </section>
      )}
      <CloudImportDialog
        open={cloudImportOpen}
        onClose={() => setCloudImportOpen(false)}
        onSuccess={() => { setCloudImportOpen(false); load(true) }}
      />
    </div>
  )
}
