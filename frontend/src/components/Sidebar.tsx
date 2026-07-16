import { NavLink } from 'react-router-dom'
import {
  LayoutDashboard,
  Server,
  Boxes,
  BrainCircuit,
  AlertTriangle,
  Wrench,
  Shield,
} from 'lucide-react'
import './Sidebar.css'

const navItems = [
  { to: '/', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/services', icon: Server, label: 'Services' },
  { to: '/resources', icon: Boxes, label: 'Resources' },
  { to: '/anomalies', icon: BrainCircuit, label: 'AI Analysis' },
  { to: '/incidents', icon: AlertTriangle, label: 'Incidents' },
  { to: '/recovery', icon: Wrench, label: 'Recovery Actions' },
]

export function Sidebar() {
  return (
    <aside className="sidebar">
      <div className="sidebar-header">
        <Shield className="sidebar-logo" size={28} />
        <div>
          <h1 className="sidebar-title">CloudGuard</h1>
        </div>
      </div>

      <nav className="sidebar-nav">
        <span className="nav-section">Operations</span>
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/'}
            className={({ isActive }) =>
              `nav-item ${isActive ? 'nav-item--active' : ''}`
            }
          >
            <item.icon size={18} />
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>

      <div className="sidebar-footer">
        <div className="sidebar-status">
          <span className="status-dot status-dot--online" />
          <span>All systems operational</span>
        </div>
      </div>
    </aside>
  )
}
