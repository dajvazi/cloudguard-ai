import { useEffect, useState } from 'react'
import { Upload } from 'lucide-react'
import { StatusBadge } from '../components/StatusBadge'
import { fetchServices, uploadTerraform, type CloudService } from '../api/client'
import './Services.css'

export function Services() {
  const [services, setServices] = useState<CloudService[]>([])
  const [loading, setLoading] = useState(true)
  const [uploading, setUploading] = useState(false)

  async function load() {
    setLoading(true)
    try {
      setServices(await fetchServices())
    } catch { /* empty */ }
    setLoading(false)
  }

  useEffect(() => { load() }, [])

  async function handleUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setUploading(true)
    try {
      await uploadTerraform(file)
      await load()
    } catch { /* empty */ }
    setUploading(false)
    e.target.value = ''
  }

  if (loading) return <div className="page-loading">Duke ngarkuar...</div>

  return (
    <div className="services-page">
      <header className="page-header">
        <div>
          <h1>Cloud Services</h1>
          <p>Infrastructure services discovered from Terraform</p>
        </div>
        <label className="upload-btn">
          <Upload size={16} />
          {uploading ? 'Uploading...' : 'Upload Terraform'}
          <input
            type="file"
            accept=".tf,.zip"
            onChange={handleUpload}
            disabled={uploading}
            hidden
          />
        </label>
      </header>

      {services.length === 0 ? (
        <div className="empty-state-large">
          <Upload size={48} />
          <h3>No services discovered</h3>
          <p>Upload a Terraform file (.tf or .zip) to discover your infrastructure</p>
        </div>
      ) : (
        <div className="services-grid">
          {services.map((svc) => (
            <div className="service-card" key={svc.id}>
              <div className="service-card-header">
                <strong>{svc.name}</strong>
                <StatusBadge status={svc.status} size="sm" />
              </div>
              <span className="service-card-type">{svc.type}</span>
              {svc.description && (
                <p className="service-card-desc">{svc.description}</p>
              )}
              <div className="service-card-meta">
                {svc.sourceFile && <span>{svc.sourceFile}</span>}
                {svc.parentModule && <span>module: {svc.parentModule}</span>}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
