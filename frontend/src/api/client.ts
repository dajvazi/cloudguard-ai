export interface ApiStatus {
  message: string
  version: string
  timestamp: string
}

export interface WeatherForecast {
  date: string
  temperatureC: number
  temperatureF: number
  summary: string
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

export function fetchWeatherForecast() {
  return getJson<WeatherForecast[]>('/api/weatherforecast')
}
