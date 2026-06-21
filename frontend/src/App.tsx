import { useEffect, useState } from 'react'
import {
  fetchStatus,
  fetchWeatherForecast,
  type ApiStatus,
  type WeatherForecast,
} from './api/client'
import './App.css'

function App() {
  const [status, setStatus] = useState<ApiStatus | null>(null)
  const [forecast, setForecast] = useState<WeatherForecast[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    async function loadData() {
      try {
        setLoading(true)
        setError(null)

        const [statusData, forecastData] = await Promise.all([
          fetchStatus(),
          fetchWeatherForecast(),
        ])

        setStatus(statusData)
        setForecast(forecastData)
      } catch (err) {
        setError(
          err instanceof Error
            ? err.message
            : 'Nuk u lidh dot me backend-in .NET',
        )
      } finally {
        setLoading(false)
      }
    }

    loadData()
  }, [])

  return (
    <main className="app">
      <header className="header">
        <h1>CloudGuard AI</h1>
        <p>React + .NET full-stack projekt</p>
      </header>

      <section className="card">
        <h2>Lidhja me API</h2>
        {loading && <p className="muted">Duke u lidhur me backend...</p>}
        {error && <p className="error">{error}</p>}
        {status && (
          <div className="status">
            <span className="badge">Online</span>
            <p>{status.message}</p>
            <p className="muted">Version: {status.version}</p>
          </div>
        )}
      </section>

      <section className="card">
        <h2>Parashikimi i motit (demo API)</h2>
        {forecast.length > 0 && (
          <ul className="forecast-list">
            {forecast.map((item) => (
              <li key={item.date}>
                <strong>{item.date}</strong>
                <span>
                  {item.temperatureC}°C / {item.summary}
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </main>
  )
}

export default App
