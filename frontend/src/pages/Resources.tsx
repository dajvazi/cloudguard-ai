import { useEffect, useState } from 'react'
import { StatusBadge } from '../components/StatusBadge'
import { fetchResources, type Resource } from '../api/client'
import './Resources.css'

export function Resources() {
  const [resources, setResources] = useState<Resource[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetchResources().then(setResources).catch(() => {}).finally(() => setLoading(false))
  }, [])

  if (loading) return <div className="page-loading">Duke ngarkuar...</div>

  return (
    <div className="resources-page">
      <header className="page-header">
        <div>
          <h1>Resources</h1>
          <p>All discovered infrastructure resources</p>
        </div>
        <span className="resource-count">{resources.length} total</span>
      </header>

      {resources.length === 0 ? (
        <div className="empty-state-large">
          <p>No resources discovered yet.</p>
        </div>
      ) : (
        <div className="resources-table">
          <div className="rtable-header">
            <span>Name</span>
            <span>Type</span>
            <span>Source</span>
            <span>Status</span>
          </div>
          {resources.map((r) => (
            <div className="rtable-row" key={r.id}>
              <span className="resource-name">{r.resourceName}</span>
              <span className="resource-type">{r.resourceType}</span>
              <span className="resource-source">{r.source || '—'}</span>
              <StatusBadge status={r.status} size="sm" />
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
