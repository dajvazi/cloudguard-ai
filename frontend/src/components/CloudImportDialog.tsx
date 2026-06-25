import { useState } from 'react'
import {
  X,
  Cloud,
  RefreshCw,
  CheckCircle2,
  AlertCircle,
  Loader2,
  Radio,
  Activity,
  Bell,
} from 'lucide-react'
import {
  testAwsConnection,
  importAwsCloudWatch,
  type AwsImportResult,
} from '../api/client'
import './CloudImportDialog.css'

interface CloudImportDialogProps {
  open: boolean
  onClose: () => void
  onSuccess: (result: AwsImportResult) => void
}

const AWS_REGIONS = [
  'us-east-1', 'us-east-2', 'us-west-1', 'us-west-2',
  'eu-west-1', 'eu-west-2', 'eu-central-1',
  'ap-southeast-1', 'ap-northeast-1',
]

const NAMESPACES = [
  { value: '', label: 'All (EC2, RDS, Lambda, ECS, ELB)' },
  { value: 'AWS/EC2', label: 'AWS/EC2 — Instances' },
  { value: 'AWS/RDS', label: 'AWS/RDS — Databases' },
  { value: 'AWS/Lambda', label: 'AWS/Lambda — Functions' },
  { value: 'AWS/ECS', label: 'AWS/ECS — Containers' },
  { value: 'AWS/ELB', label: 'AWS/ELB — Load Balancers' },
  { value: 'AWS/S3', label: 'AWS/S3 — Storage' },
]

export function CloudImportDialog({ open, onClose, onSuccess }: CloudImportDialogProps) {
  const [region, setRegion] = useState('us-east-1')
  const [namespace, setNamespace] = useState('')
  const [period, setPeriod] = useState(60)
  const [connectionStatus, setConnectionStatus] = useState<'idle' | 'testing' | 'connected' | 'failed'>('idle')
  const [importing, setImporting] = useState(false)
  const [progress, setProgress] = useState(0)
  const [result, setResult] = useState<AwsImportResult | null>(null)
  const [error, setError] = useState('')

  const handleTestConnection = async () => {
    setConnectionStatus('testing')
    setError('')
    try {
      const res = await testAwsConnection()
      setConnectionStatus(res.connected ? 'connected' : 'failed')
      if (!res.connected) setError(res.message)
    } catch (err) {
      setConnectionStatus('failed')
      setError(err instanceof Error ? err.message : 'Connection test failed')
    }
  }

  const handleImport = async () => {
    setImporting(true)
    setProgress(20)
    setError('')
    setResult(null)

    try {
      setProgress(50)
      const res = await importAwsCloudWatch(region, namespace || undefined, period)
      setProgress(100)
      setResult(res)

      if (res.success) {
        setTimeout(() => {
          onSuccess(res)
        }, 2000)
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Import failed')
    }
    setImporting(false)
  }

  const handleReset = () => {
    setResult(null)
    setProgress(0)
    setError('')
  }

  if (!open) return null

  return (
    <div className="dialog-overlay" onClick={onClose}>
      <div className="dialog cloud-import-dialog" onClick={(e) => e.stopPropagation()}>
        <div className="dialog-header">
          <div className="dialog-title-group">
            <Cloud size={20} className="dialog-title-icon" />
            <div>
              <h2>Import from AWS CloudWatch</h2>
              <p>Connect to AWS and import metrics, alarms, and services</p>
            </div>
          </div>
          <button className="dialog-close" onClick={onClose}>
            <X size={18} />
          </button>
        </div>

        <div className="dialog-body">
          {/* Connection Test */}
          <div className="import-section">
            <div className="section-header">
              <Radio size={14} />
              <span>Connection</span>
              {connectionStatus === 'connected' && (
                <span className="conn-badge conn-badge--ok">Connected</span>
              )}
              {connectionStatus === 'failed' && (
                <span className="conn-badge conn-badge--fail">Failed</span>
              )}
            </div>
            <p className="section-desc">
              Credentials are loaded from server environment (.env)
            </p>
            <button
              className="btn-test"
              onClick={handleTestConnection}
              disabled={connectionStatus === 'testing'}
            >
              {connectionStatus === 'testing' ? (
                <><Loader2 size={14} className="spinner" /> Testing...</>
              ) : (
                <><RefreshCw size={14} /> Test AWS Connection</>
              )}
            </button>
          </div>

          {/* Configuration */}
          <div className="import-section">
            <div className="section-header">
              <Activity size={14} />
              <span>Configuration</span>
            </div>

            <div className="form-grid">
              <div className="form-field">
                <label>Region</label>
                <select value={region} onChange={(e) => setRegion(e.target.value)}>
                  {AWS_REGIONS.map((r) => (
                    <option key={r} value={r}>{r}</option>
                  ))}
                </select>
              </div>

              <div className="form-field">
                <label>Namespace</label>
                <select value={namespace} onChange={(e) => setNamespace(e.target.value)}>
                  {NAMESPACES.map((ns) => (
                    <option key={ns.value} value={ns.value}>{ns.label}</option>
                  ))}
                </select>
              </div>

              <div className="form-field">
                <label>Time Period</label>
                <select value={period} onChange={(e) => setPeriod(Number(e.target.value))}>
                  <option value={15}>Last 15 minutes</option>
                  <option value={30}>Last 30 minutes</option>
                  <option value={60}>Last 1 hour</option>
                  <option value={180}>Last 3 hours</option>
                  <option value={360}>Last 6 hours</option>
                  <option value={1440}>Last 24 hours</option>
                </select>
              </div>
            </div>
          </div>

          {/* Progress */}
          {importing && (
            <div className="import-progress">
              <div className="progress-header">
                <Loader2 size={14} className="spinner" />
                <span>Fetching CloudWatch data... {progress}%</span>
              </div>
              <div className="progress-track">
                <div className="progress-fill" style={{ width: `${progress}%` }} />
              </div>
            </div>
          )}

          {/* Error */}
          {error && (
            <div className="import-error">
              <AlertCircle size={16} />
              <span>{error}</span>
            </div>
          )}

          {/* Result */}
          {result && result.success && (
            <div className="import-result">
              <div className="result-header">
                <CheckCircle2 size={16} />
                <strong>Import Successful</strong>
              </div>
              <div className="result-stats">
                <div className="result-stat">
                  <Bell size={14} />
                  <span>{result.alarmsImported} Alarms</span>
                </div>
                <div className="result-stat">
                  <Activity size={14} />
                  <span>{result.metricsImported} Metrics</span>
                </div>
                <div className="result-stat">
                  <Cloud size={14} />
                  <span>{result.servicesDiscovered} Services</span>
                </div>
              </div>
              {result.alarms.length > 0 && (
                <div className="result-preview">
                  <span className="result-preview-title">Alarms:</span>
                  {result.alarms.slice(0, 4).map((a, i) => (
                    <div key={i} className={`alarm-item alarm-item--${a.stateValue.toLowerCase()}`}>
                      <span className="alarm-name">{a.alarmName}</span>
                      <span className="alarm-state">{a.stateValue}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>

        <div className="dialog-footer">
          <button className="btn btn--ghost" onClick={onClose} disabled={importing}>
            Cancel
          </button>
          {result ? (
            <button className="btn btn--primary" onClick={handleReset}>
              Import Again
            </button>
          ) : (
            <button
              className="btn btn--primary"
              onClick={handleImport}
              disabled={importing}
            >
              {importing ? (
                <><Loader2 size={14} className="spinner" /> Importing...</>
              ) : (
                <><Cloud size={14} /> Import CloudWatch Data</>
              )}
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
