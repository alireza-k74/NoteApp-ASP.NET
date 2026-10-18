import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function Layout() {
  const { user, logout } = useAuth()

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="brand">NoteApp</div>
        <nav className="nav">
          <NavLink to="/" end>
            Notes
          </NavLink>
          <NavLink to="/notes/new">New</NavLink>
          <NavLink to="/trash">Trash</NavLink>
        </nav>
        <div className="user-bar">
          <span>{user?.username}</span>
          <button type="button" className="btn ghost" onClick={() => void logout()}>
            Logout
          </button>
        </div>
      </header>
      <main className="page">
        <Outlet />
      </main>
    </div>
  )
}
