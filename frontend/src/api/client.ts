export interface ApiStatus {
  message: string
  version: string
  timestamp: string
}

export interface CloudService {
  id: number
  terraformUploadId: number | null
  name: string
  type: string
  status: string
  description: string | null
  sourceKind: string
  rawResourceType: string | null
  sourceFile: string | null
  moduleSource: string | null
  parentModule: string | null
  createdAt: string
}

export interface Resource {
  id: number
  resourceName: string
  resourceType: string
  source: string | null
  status: string
  discoveredAt: string
}

export interface Incident {
  id: number
  cloudServiceId: number
  cloudServiceName: string
  title: string
  severity: string | null
  status: string
  rootCause: string | null
  createdAt: string
  resolvedAt: string | null
}

export interface Metric {
  id: number
  cloudServiceId: number
  cloudServiceName: string
  metricName: string | null
  unit: string | null
  cpuUsage: number | null
  memoryUsage: number | null
  networkIn: number | null
  networkOut: number | null
  diskReadBytes: number | null
  diskWriteBytes: number | null
  latencyMs: number | null
  errorRate: number | null
  value: number | null
  maximum: number | null
  minimum: number | null
  recordedAt: string
}

export interface Anomaly {
  id: number
  cloudServiceId: number
  cloudServiceName: string
  anomalyType: string | null
  severity: string | null
  aiConfidence: number | null
  description: string | null
  detectedAt: string
}

export interface RecoveryAction {
  id: number
  incidentId: number
  actionType: string | null
  actionStatus: string
  description: string | null
  executedAt: string
}

async function getJson<T>(url: string): Promise<T> {
  const response = await fetch(url)
  if (!response.ok) {
    throw new Error(`API error: ${response.status}`)
  }
  return response.json() as Promise<T>
}

export const fetchStatus = () => getJson<ApiStatus>('/api/status')
export const fetchServices = () => getJson<CloudService[]>('/api/services')
export const fetchResources = () => getJson<Resource[]>('/api/resources')
export const fetchActiveIncidents = () => getJson<Incident[]>('/api/incidents/active')
export const fetchAllIncidents = () => getJson<Incident[]>('/api/incidents')
export const fetchMetrics = () => getJson<Metric[]>('/api/metrics')
export const fetchMetricsByService = (serviceId: number) => getJson<Metric[]>(`/api/metrics/by-service/${serviceId}`)
export const fetchAnomalies = () => getJson<Anomaly[]>('/api/anomalies')
export const fetchRecoveryActions = () => getJson<RecoveryAction[]>('/api/recovery-actions')

export async function uploadTerraform(file: File): Promise<unknown> {
  const formData = new FormData()
  formData.append('file', file)
  const response = await fetch('/api/terraform/upload', {
    method: 'POST',
    body: formData,
  })
  if (!response.ok) throw new Error(`Upload error: ${response.status}`)
  return response.json()
}

export interface SelfHealingResult {
  success: boolean
  message: string
  anomalyId: number | null
  incidentId: number | null
  recoveryActionId: number | null
  aiAnalysis: {
    rootCause: string
    recommendedAction: string
    actionType: string
    severity: string
  } | null
}

export function triggerSelfHealing(serviceId: number): Promise<SelfHealingResult> {
  return fetch(`/api/self-healing/trigger/${serviceId}`, { method: 'POST' })
    .then(r => {
      if (!r.ok) throw new Error(`Self-healing error: ${r.status}`)
      return r.json() as Promise<SelfHealingResult>
    })
}

export function triggerSelfHealingFromAnomaly(anomalyId: number): Promise<SelfHealingResult> {
  return fetch(`/api/self-healing/trigger/anomaly/${anomalyId}`, { method: 'POST' })
    .then(r => {
      if (!r.ok) throw new Error(`Self-healing error: ${r.status}`)
      return r.json() as Promise<SelfHealingResult>
    })
}

// AWS CloudWatch
export interface AwsAlarm {
  alarmName: string
  namespace: string
  metricName: string
  stateValue: string
  stateReason: string | null
  threshold: number
  comparisonOperator: string
  stateUpdatedAt: string | null
}

export interface AwsMetricData {
  namespace: string
  metricName: string
  instanceId: string | null
  average: number
  maximum: number
  minimum: number
  timestamp: string
}

export interface AwsImportResult {
  success: boolean
  message: string
  alarmsImported: number
  metricsImported: number
  servicesDiscovered: number
  alarms: AwsAlarm[]
  metrics: AwsMetricData[]
}

export interface AwsConnectionResult {
  connected: boolean
  message: string
}

export function testAwsConnection(): Promise<AwsConnectionResult> {
  return getJson<AwsConnectionResult>('/api/aws/test-connection')
}

export function importAwsCloudWatch(region: string, namespace?: string, periodMinutes = 60): Promise<AwsImportResult> {
  return fetch('/api/aws/import', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ region, namespace: namespace || null, periodMinutes }),
  }).then(r => {
    if (!r.ok) throw new Error(`AWS import error: ${r.status}`)
    return r.json() as Promise<AwsImportResult>
  })
}
