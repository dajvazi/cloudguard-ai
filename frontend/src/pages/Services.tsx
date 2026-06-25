import { useEffect, useState, useMemo } from 'react'
import { Upload, Cloud, ChevronDown, ChevronUp, RefreshCw, Sparkles } from 'lucide-react'
import { StatusBadge } from '../components/StatusBadge'
import { SelfHealingBanner } from '../components/SelfHealingBanner'
import { TerraformUploadDialog } from '../components/TerraformUploadDialog'
import { CloudImportDialog } from '../components/CloudImportDialog'
import { ServiceMetricsPanel } from '../components/ServiceMetricsPanel'
import { DeleteAllButton } from '../components/DeleteAllButton'
import {
  fetchServices,
  fetchMetrics,
  purgeMetrics,
  purgeServices,
  purgeAws,
  purgeTerraform,
  reevaluateAwsHealth,
  triggerSelfHealing,
  type CloudService,
  type Metric,
  type SelfHealingResult,
} from '../api/client'
import '../components/SelfHealingBanner.css'
import './Services.css'

export function Services() {
  const [services, setServices] = useState<CloudService[]>([])
  const [metrics, setMetrics] = useState<Metric[]>([])
  const [loading, setLoading] = useState(true)
  const [terraformOpen, setTerraformOpen] = useState(false)
  const [cloudOpen, setCloudOpen] = useState(false)
  const [expandedId, setExpandedId] = useState<number | null>(null)
  const [evaluating, setEvaluating] = useState(false)
  const [evalMessage, setEvalMessage] = useState<string | null>(null)
  const [healingServiceId, setHealingServiceId] = useState<number | null>(null)
  const [healResult, setHealResult] = useState<SelfHealingResult | null>(null)

  const metricsByService = useMemo(() => {
    const map: Record<number, Metric[]> = {}
    for (const m of metrics) {
      if (!map[m.cloudServiceId]) map[m.cloudServiceId] = []
      map[m.cloudServiceId].push(m)
    }
    return map
  }, [metrics])

  async function load() {
    setLoading(true)
    try {
      const [svc, met] = await Promise.allSettled([fetchServices(), fetchMetrics()])
      if (svc.status === 'fulfilled') setServices(svc.value)
      if (met.status === 'fulfilled') setMetrics(met.value)
    } catch { /* empty */ }
    setLoading(false)
  }

  useEffect(() => { load() }, [])

  async function handleReevaluate() {
    setEvaluating(true)
    setEvalMessage(null)
    try {
      const result = await reevaluateAwsHealth()
      setEvalMessage(
        `U krijuan ${result.incidentsCreated} incidente, ${result.anomaliesCreated} anomalí`
      )
      await load()
    } catch {
      setEvalMessage('Vlerësimi dështoi')
    }
    setEvaluating(false)
  }

  async function handleHeal(serviceId: number) {
    setHealingServiceId(serviceId)
    setHealResult(null)
    try {
      const result = await triggerSelfHealing(serviceId)
      setHealResult(result)
      await load()
    } catch {
      setHealResult({
        success: false,
        message: 'Self-healing failed',
        anomalyId: null,
        incidentId: null,
        recoveryActionId: null,
        runbookId: null,
        ssmCommandId: null,
        executionOutput: null,
        executedViaSsm: false,
        aiAnalysis: null,
      })
    }
    setHealingServiceId(null)
  }

  const needsHeal = (status: string) =>
    status === 'Critical' || status === 'Warning' || status === 'Recovering'

  const servicesWithMetrics = services.filter((s) => (metricsByService[s.id]?.length ?? 0) > 0)
  const servicesWithoutMetrics = services.filter((s) => (metricsByService[s.id]?.length ?? 0) === 0)

  if (loading) return <div className="page-loading">Duke ngarkuar...</div>

  return (
    <div className="services-page">
      <header className="page-header">
        <div>
          <h1>Cloud Services</h1>
          <p>Monitorim i metrikave për çdo resource — CPU, Network, Disk, Status</p>
        </div>
        <div className="header-btns">
          <button className="upload-btn" onClick={() => setCloudOpen(true)}>
            <Cloud size={16} />
            Import Cloud
          </button>
          <button className="upload-btn upload-btn--secondary" onClick={() => setTerraformOpen(true)}>
            <Upload size={16} />
            Import Terraform
          </button>
        </div>
      </header>

      <section className="delete-actions-bar">
        <span className="delete-actions-label">Veprime:</span>
        <div className="delete-actions-group">
          <button
            type="button"
            className="btn-reevaluate"
            onClick={handleReevaluate}
            disabled={evaluating}
          >
            <RefreshCw size={14} className={evaluating ? 'spinner' : ''} />
            Kontrollo Incidentet
          </button>
          <DeleteAllButton
            label="Metrics"
            variant="compact"
            confirmMessage="Fshi të gjitha metrikat?"
            onDelete={purgeMetrics}
            onSuccess={() => load()}
          />
          <DeleteAllButton
            label="AWS"
            variant="compact"
            confirmMessage="Fshi të gjitha AWS services dhe metrikat e tyre?"
            onDelete={purgeAws}
            onSuccess={() => load()}
          />
          <DeleteAllButton
            label="Terraform"
            variant="compact"
            confirmMessage="Fshi të gjitha të dhënat Terraform (uploads, services, resources)?"
            onDelete={purgeTerraform}
            onSuccess={() => load()}
          />
          <DeleteAllButton
            label="All Services"
            variant="compact"
            confirmMessage="Fshi TË GJITHA services, metrikat, anomalitë dhe incidentet? Ky veprim është shumë destruktiv."
            onDelete={purgeServices}
            onSuccess={() => load()}
          />
        </div>
        {evalMessage && <span className="reevaluate-msg">{evalMessage}</span>}
      </section>

      {healResult && (
        <SelfHealingBanner result={healResult} onClose={() => setHealResult(null)} />
      )}

      {services.length === 0 ? (
        <div className="empty-state-large">
          <Cloud size={48} />
          <h3>No services discovered</h3>
          <p>Import from AWS CloudWatch or upload a Terraform file to discover your infrastructure</p>
          <div className="header-btns">
            <button className="upload-btn" onClick={() => setCloudOpen(true)}>
              <Cloud size={16} />
              Import Cloud
            </button>
            <button className="upload-btn upload-btn--secondary" onClick={() => setTerraformOpen(true)}>
              <Upload size={16} />
              Import Terraform
            </button>
          </div>
        </div>
      ) : (
        <div className="services-list">
          {servicesWithMetrics.map((svc) => {
            const svcMetrics = metricsByService[svc.id] ?? []
            const expanded = expandedId === svc.id

            return (
              <div className="service-card service-card--monitored" key={svc.id}>
                <div
                  className="service-card-header"
                  onClick={() => setExpandedId(expanded ? null : svc.id)}
                >
                  <div className="service-card-info">
                    <strong>{svc.name}</strong>
                    <span className="service-card-type">{svc.type}</span>
                    {svc.sourceKind && (
                      <span className={`source-badge source-badge--${svc.sourceKind}`}>
                        {svc.sourceKind}
                      </span>
                    )}
                    <span className="metric-count-badge">{svcMetrics.length} metrics</span>
                  </div>
                  <div className="service-card-right">
                    <StatusBadge status={svc.status} size="sm" />
                    {expanded ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                  </div>
                </div>

                {svc.description && (
                  <p className="service-card-desc">{svc.description}</p>
                )}

                <ServiceMetricsPanel metrics={svcMetrics} compact={!expanded} />

                {needsHeal(svc.status) && (
                  <div className="service-heal-row">
                    <button
                      type="button"
                      className="btn-heal"
                      onClick={(e) => { e.stopPropagation(); handleHeal(svc.id) }}
                      disabled={healingServiceId !== null}
                    >
                      {healingServiceId === svc.id ? (
                        <><RefreshCw size={13} className="spinner" /> Healing...</>
                      ) : (
                        <><Sparkles size={13} /> Self-Heal via SSM</>
                      )}
                    </button>
                  </div>
                )}

                {expanded && (
                  <div className="metrics-detail-table-wrap">
                    <table className="metrics-detail-table">
                      <thead>
                        <tr>
                          <th>Metric</th>
                          <th>Avg</th>
                          <th>Max</th>
                          <th>Min</th>
                          <th>Unit</th>
                          <th>Time</th>
                        </tr>
                      </thead>
                      <tbody>
                        {svcMetrics.map((m) => (
                          <tr key={m.id}>
                            <td>{m.metricName || '—'}</td>
                            <td className="val">{m.value?.toFixed(2) ?? '—'}</td>
                            <td>{m.maximum?.toFixed(2) ?? '—'}</td>
                            <td>{m.minimum?.toFixed(2) ?? '—'}</td>
                            <td>{m.unit || '—'}</td>
                            <td>{new Date(m.recordedAt).toLocaleString()}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            )
          })}

          {servicesWithoutMetrics.map((svc) => (
            <div className="service-card" key={svc.id}>
              <div className="service-card-header">
                <div className="service-card-info">
                  <strong>{svc.name}</strong>
                  <span className="service-card-type">{svc.type}</span>
                  {svc.sourceKind && (
                    <span className={`source-badge source-badge--${svc.sourceKind}`}>
                      {svc.sourceKind}
                    </span>
                  )}
                </div>
                <StatusBadge status={svc.status} size="sm" />
              </div>
              {svc.description && <p className="service-card-desc">{svc.description}</p>}
              <div className="metrics-panel-empty">
                Nuk ka metrika — importo nga AWS CloudWatch
              </div>
            </div>
          ))}
        </div>
      )}

      <TerraformUploadDialog
        open={terraformOpen}
        onClose={() => setTerraformOpen(false)}
        onSuccess={() => { setTerraformOpen(false); load() }}
      />
      <CloudImportDialog
        open={cloudOpen}
        onClose={() => setCloudOpen(false)}
        onSuccess={() => { setCloudOpen(false); load() }}
      />
    </div>
  )
}
