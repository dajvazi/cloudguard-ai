import { useEffect, useState } from 'react'
import {
  Server,
  ShieldCheck,
  AlertTriangle,
  CheckCircle2,
  Activity,
  Zap,
} from 'lucide-react'
import { StatCard } from '../components/StatCard'
import { StatusBadge } from '../components/StatusBadge'
import {
  fetchServices,
  fetchActiveIncidents,
  fetchAnomalies,
  type CloudService,
  type Incident,
  type Anomaly,
} from '../api/client'
import './Dashboard.css'

export function Dashboard() {
  const [services, setServices] = useState<CloudService[]>([])
  const [incidents, setIncidents] = useState<Incident[]>([])
  const [anomalies, setAnomalies] = useState<Anomaly[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function load() {
      const [svc, inc, anom] = await Promise.allSettled([
        fetchServices(),
        fetchActiveIncidents(),
        fetchAnomalies(),
      ])
      if (svc.status === 'fulfilled') setServices(svc.value)
      if (inc.status === 'fulfilled') setIncidents(inc.value)
      if (anom.status === 'fulfilled') setAnomalies(anom.value)
      setLoading(false)
    }
    load()
  }, [])

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
        <StatusBadge status="Operational" />
      </header>

      <section className="stats-grid">
        <StatCard
          icon={Server}
          label="Total Services"
          value={services.length}
          variant="default"
        />
        <StatCard
          icon={ShieldCheck}
          label="Healthy Services"
          value={healthyCount}
          subtext={`${healthyPercent}%`}
          variant="green"
        />
        <StatCard
          icon={AlertTriangle}
          label="Active Incidents"
          value={incidents.length}
          variant="red"
        />
        <StatCard
          icon={CheckCircle2}
          label="Anomalies Detected"
          value={anomalies.length}
          variant="yellow"
        />
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
          <h2>Latest AI Analysis</h2>
          <p className="card-subtitle">Recent anomaly evaluations</p>
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
                      <div
                        className="confidence-fill"
                        style={{ width: `${a.aiConfidence}%` }}
                      />
                    </div>
                    <span className="confidence-value">{a.aiConfidence}%</span>
                  </div>
                )}
              </div>
            ))}
          </div>
        </section>
      )}
    </div>
  )
}
