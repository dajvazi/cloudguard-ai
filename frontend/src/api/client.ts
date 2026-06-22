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

async function getJson<T>(url: string): Promise<T> {
  const response = await fetch(url)

  if (!response.ok) {
    throw new Error(`API error: ${response.status}`)
  }

  return response.json() as Promise<T>
}

export function fetchStatus() {
  return getJson<ApiStatus>('/api/status')
}

export function fetchServices() {
  return getJson<CloudService[]>('/api/services')
}

export function fetchResources() {
  return getJson<Resource[]>('/api/resources')
}

export function fetchActiveIncidents() {
  return getJson<Incident[]>('/api/incidents/active')
}
