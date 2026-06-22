import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import App from './App'
import { Dashboard } from './pages/Dashboard'
import { Services } from './pages/Services'
import { Resources } from './pages/Resources'
import { Anomalies } from './pages/Anomalies'
import { Incidents } from './pages/Incidents'
import { Recovery } from './pages/Recovery'
import './index.css'

const router = createBrowserRouter([
  {
    path: '/',
    element: <App />,
    children: [
      { index: true, element: <Dashboard /> },
      { path: 'services', element: <Services /> },
      { path: 'resources', element: <Resources /> },
      { path: 'anomalies', element: <Anomalies /> },
      { path: 'incidents', element: <Incidents /> },
      { path: 'recovery', element: <Recovery /> },
    ],
  },
])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
)
