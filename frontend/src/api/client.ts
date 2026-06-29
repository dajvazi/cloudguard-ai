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
  runbookId: string | null
  ssmCommandId: string | null
  executionOutput: string | null
  executedViaSsm: boolean
  aiAnalysis: {
    rootCause: string
    recommendedAction: string
    actionType: string
    severity: string
  } | null
}

async function postSelfHealing(url: string): Promise<SelfHealingResult> {
  const response = await fetch(url, { method: 'POST' })
  const body = (await response.json().catch(() => null)) as SelfHealingResult | null

  if (body && typeof body.message === 'string')
    return body

  throw new Error(`Self-healing error: ${response.status}`)
}

export function triggerSelfHealing(serviceId: number): Promise<SelfHealingResult> {
  return postSelfHealing(`/api/self-healing/trigger/${serviceId}`)
}

export function triggerSelfHealingFromAnomaly(anomalyId: number): Promise<SelfHealingResult> {
  return postSelfHealing(`/api/self-healing/trigger/anomaly/${anomalyId}`)
}

export function triggerSelfHealingFromIncident(incidentId: number): Promise<SelfHealingResult> {
  return postSelfHealing(`/api/self-healing/trigger/incident/${incidentId}`)
}

export interface HealingOption {
  runbookId: string
  name: string
  description: string
  effect: string
  recommended: boolean
}

export interface HealingAnalysis {
  success: boolean
  serviceName: string
  anomalyType: string | null
  aiAnalysis: {
    rootCause: string
    recommendedAction: string
    actionType: string
    severity: string
  } | null
  options: HealingOption[]
}

export function analyzeForHealing(serviceId: number): Promise<HealingAnalysis> {
  return getJson<HealingAnalysis>(`/api/self-healing/analyze/${serviceId}`)
}

export function executeRunbook(serviceId: number, runbookId: string): Promise<SelfHealingResult> {
  return postSelfHealing(`/api/self-healing/execute/${serviceId}/${runbookId}`)
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
  anomaliesCreated: number
  incidentsCreated: number
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

export function reevaluateAwsHealth(): Promise<{ anomaliesCreated: number; incidentsCreated: number }> {
  return fetch('/api/aws/reevaluate', { method: 'POST' })
    .then(r => {
      if (!r.ok) throw new Error(`Reevaluate error: ${r.status}`)
      return r.json() as Promise<{ anomaliesCreated: number; incidentsCreated: number }>
    })
}

export async function importAwsCloudWatch(region: string, namespace?: string, periodMinutes = 60): Promise<AwsImportResult> {
  const response = await fetch('/api/aws/import', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ region, namespace: namespace || null, periodMinutes }),
  })
  const body = (await response.json().catch(() => null)) as AwsImportResult | null
  if (body && typeof body.message === 'string')
    return body
  throw new Error(`AWS import error: ${response.status}`)
}

// Admin purge
export interface PurgeResult {
  module: string
  deletedCount: number
  message: string
}

export type PurgeModule =
  | 'metrics'
  | 'anomalies'
  | 'recovery-actions'
  | 'incidents'
  | 'services'
  | 'resources'
  | 'terraform'
  | 'aws'

async function deleteJson<T>(url: string): Promise<T> {
  const response = await fetch(url, { method: 'DELETE' })
  if (!response.ok) {
    throw new Error(`API error: ${response.status}`)
  }
  return response.json() as Promise<T>
}

export function purgeModule(module: PurgeModule): Promise<PurgeResult> {
  return deleteJson<PurgeResult>(`/api/admin/${module}`)
}

export const purgeMetrics = () => purgeModule('metrics')
export const purgeAnomalies = () => purgeModule('anomalies')
export const purgeRecoveryActions = () => purgeModule('recovery-actions')
export const purgeIncidents = () => purgeModule('incidents')
export const purgeServices = () => purgeModule('services')
export const purgeResources = () => purgeModule('resources')
export const purgeTerraform = () => purgeModule('terraform')
export const purgeAws = () => purgeModule('aws')
