import { Outlet } from 'react-router-dom'
import { Sidebar } from './components/Sidebar'
import './App.css'

function App() {
  return (
    <>
      <Sidebar />
      <main className="main-content">
        <Outlet />
      </main>
    </>
  )
}

export default App
