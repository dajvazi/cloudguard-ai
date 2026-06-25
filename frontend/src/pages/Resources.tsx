import { useEffect, useState, useMemo } from 'react'
import { StatusBadge } from '../components/StatusBadge'
import { DeleteAllButton } from '../components/DeleteAllButton'
import { ServiceMetricsPanel } from '../components/ServiceMetricsPanel'
import { fetchResources, fetchServices, fetchMetrics, purgeResources, type Resource, type Metric } from '../api/client'
import './Resources.css'

export function Resources() {
  const [resources, setResources] = useState<Resource[]>([])
  const [metricsByName, setMetricsByName] = useState<Record<string, Metric[]>>({})
  const [loading, setLoading] = useState(true)

  function load() {
    setLoading(true)
    Promise.allSettled([fetchResources(), fetchServices(), fetchMetrics()])
      .then(([res, svc, met]) => {
        if (res.status === 'fulfilled') setResources(res.value)

        if (svc.status === 'fulfilled' && met.status === 'fulfilled') {
          const serviceIdToName = Object.fromEntries(svc.value.map((s) => [s.id, s.name]))
          const grouped: Record<string, Metric[]> = {}

          for (const m of met.value) {
            const name = serviceIdToName[m.cloudServiceId]
            if (!name) continue
            if (!grouped[name]) grouped[name] = []
            grouped[name].push(m)
          }
          setMetricsByName(grouped)
        }
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  const withMetrics = useMemo(
    () => resources.filter((r) => (metricsByName[r.resourceName]?.length ?? 0) > 0),
    [resources, metricsByName],
  )

  if (loading) return <div className="page-loading">Duke ngarkuar...</div>

  return (
    <div className="resources-page">
      <header className="page-header">
        <div>
          <h1>Resources</h1>
          <p>Infrastructure resources me metrikat e tyre</p>
        </div>
        <div className="page-header-actions">
          <span className="resource-count">{resources.length} total</span>
          <DeleteAllButton
            label="Delete All"
            confirmMessage="Fshi të gjitha resources (Terraform inventory)? Ky veprim nuk kthehet mbrapsht."
            onDelete={purgeResources}
            onSuccess={() => load()}
          />
        </div>
      </header>

      {resources.length === 0 ? (
        <div className="empty-state-large">
          <p>No resources discovered yet.</p>
        </div>
      ) : (
        <div className="resources-list">
          {resources.map((r) => {
            const metrics = metricsByName[r.resourceName] ?? []
            const hasMetrics = metrics.length > 0

            return (
              <div className={`resource-card ${hasMetrics ? 'resource-card--monitored' : ''}`} key={r.id}>
                <div className="resource-card-header">
                  <div>
                    <span className="resource-name">{r.resourceName}</span>
                    <span className="resource-type">{r.resourceType}</span>
                  </div>
                  <StatusBadge status={r.status} size="sm" />
                </div>
                <div className="resource-card-meta">
                  <span>{r.source || '—'}</span>
                  {hasMetrics && (
                    <span className="metric-count-badge">{metrics.length} metrics</span>
                  )}
                </div>
                {hasMetrics && <ServiceMetricsPanel metrics={metrics} compact />}
              </div>
            )
          })}
        </div>
      )}

      {withMetrics.length === 0 && resources.length > 0 && (
        <p className="resources-hint">
          Metrikat shfaqen kur importon nga AWS CloudWatch. Terraform zbulon vetëm resources.
        </p>
      )}
    </div>
  )
}
