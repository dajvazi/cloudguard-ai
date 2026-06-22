import { useEffect, useState } from 'react'
import {
  fetchActiveIncidents,
  fetchResources,
  fetchServices,
  fetchStatus,
  type ApiStatus,
  type CloudService,
  type Incident,
  type Resource,
} from './api/client'
import './App.css'

function App() {
  const [status, setStatus] = useState<ApiStatus | null>(null)
  const [services, setServices] = useState<CloudService[]>([])
  const [resources, setResources] = useState<Resource[]>([])
  const [incidents, setIncidents] = useState<Incident[]>([])
  const [loading, setLoading] = useState(true)
  const [errors, setErrors] = useState<string[]>([])

  useEffect(() => {
    async function loadData() {
      setLoading(true)
      setErrors([])

      const results = await Promise.allSettled([
        fetchStatus(),
        fetchServices(),
        fetchResources(),
        fetchActiveIncidents(),
      ])

      const nextErrors: string[] = []

      if (results[0].status === 'fulfilled') {
        setStatus(results[0].value)
      } else {
        nextErrors.push('API Status')
      }

      if (results[1].status === 'fulfilled') {
        setServices(results[1].value)
      } else {
        nextErrors.push('Cloud Services')
      }

      if (results[2].status === 'fulfilled') {
        setResources(results[2].value)
      } else {
        nextErrors.push('Resources')
      }

      if (results[3].status === 'fulfilled') {
        setIncidents(results[3].value)
      } else {
        nextErrors.push('Incidente')
      }

      setErrors(nextErrors)
      setLoading(false)
    }

    loadData()
  }, [])

  return (
    <main className="app">
      <header className="header">
        <h1>CloudGuard AI</h1>
        <p>Infrastruktura dhe shërbimet nga backend API</p>
      </header>

      {errors.length > 0 && (
        <section className="card error-card">
          <p className="error">
            Disa të dhëna nuk u ngarkuan: {errors.join(', ')}
          </p>
        </section>
      )}

      <section className="card">
        <h2>API Status</h2>
        {loading && <p className="muted">Duke ngarkuar të dhënat...</p>}
        {status && (
          <div className="status">
            <span className="badge">Online</span>
            <p>{status.message}</p>
            <p className="muted">Version: {status.version}</p>
          </div>
        )}
        {!loading && !status && (
          <p className="muted">Backend-i nuk është i arritshëm.</p>
        )}
      </section>

      <section className="card">
        <h2>Cloud Services ({services.length})</h2>
        {services.length === 0 && !loading && (
          <p className="muted">Asnjë shërbim. Upload Terraform për të zbuluar infra.</p>
        )}
        {services.length > 0 && (
          <ul className="data-list">
            {services.map((service) => (
              <li key={service.id}>
                <div>
                  <strong>{service.name}</strong>
                  <p className="muted">{service.type}</p>
                </div>
                <span className={`status-pill status-${service.status.toLowerCase()}`}>
                  {service.status}
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="card">
        <h2>Resources ({resources.length})</h2>
        {resources.length === 0 && !loading && (
          <p className="muted">Asnjë resource i zbuluar ende.</p>
        )}
        {resources.length > 0 && (
          <ul className="data-list">
            {resources.map((resource) => (
              <li key={resource.id}>
                <div>
                  <strong>{resource.resourceName}</strong>
                  <p className="muted">
                    {resource.resourceType}
                    {resource.source ? ` · ${resource.source}` : ''}
                  </p>
                </div>
                <span className="status-pill">{resource.status}</span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="card">
        <h2>Incidente aktive ({incidents.length})</h2>
        {incidents.length === 0 && !loading && (
          <p className="muted">Nuk ka incidente aktive.</p>
        )}
        {incidents.length > 0 && (
          <ul className="data-list">
            {incidents.map((incident) => (
              <li key={incident.id}>
                <div>
                  <strong>{incident.title}</strong>
                  <p className="muted">
                    {incident.cloudServiceName}
                    {incident.severity ? ` · ${incident.severity}` : ''}
                  </p>
                </div>
                <span className="status-pill">{incident.status}</span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </main>
  )
}

export default App
