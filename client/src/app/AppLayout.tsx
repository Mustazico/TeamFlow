import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { authApi } from '@/lib/api';
import { Avatar } from '@/components/ui/Avatar';
import { NotificationBell } from '@/components/common/NotificationBell';
import { ThemeToggle } from '@/components/common/ThemeToggle';
import { LayoutDashboard, FolderKanban, Activity, LogOut, ListTodo } from 'lucide-react';
import { cn } from '@/lib/utils';

const navItems = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/projects', label: 'Projects', icon: FolderKanban },
  { to: '/my-tasks', label: 'My Tasks', icon: ListTodo },
  { to: '/activity', label: 'Activity', icon: Activity },
];

export function AppLayout() {
  const { user, refreshToken, clear } = useAuthStore();
  const navigate = useNavigate();

  const logout = async () => {
    if (refreshToken) {
      try {
        await authApi.logout(refreshToken);
      } catch {
        /* ignore */
      }
    }
    clear();
    navigate('/login', { replace: true });
  };

  return (
    <div className="h-screen flex">
      <aside className="hidden md:flex w-60 bg-slate-900 text-slate-100 flex-col h-screen sticky top-0">
        <div className="px-5 py-5 border-b border-slate-800">
          <div className="flex items-center gap-2">
            <div className="h-8 w-8 rounded-md bg-indigo-500 flex items-center justify-center font-bold">
              T
            </div>
            <span className="font-semibold text-lg">TeamFlow</span>
          </div>
        </div>
        <nav className="flex-1 p-3 space-y-1">
          {navItems.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 px-3 py-2 rounded-md text-sm transition-colors',
                  isActive
                    ? 'bg-indigo-600 text-white'
                    : 'text-slate-300 hover:bg-slate-800 hover:text-white',
                )
              }
            >
              <Icon size={18} />
              {label}
            </NavLink>
          ))}
        </nav>
        <div className="p-3 border-t border-slate-800">
          {user && (
            <div className="flex items-center gap-2 px-2 py-2">
              <Avatar name={user.displayName} size="md" />
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium truncate">{user.displayName}</p>
                <p className="text-xs text-slate-400 truncate">{user.email}</p>
              </div>
              <button
                onClick={logout}
                className="text-slate-400 hover:text-white p-1 rounded hover:bg-slate-800"
                title="Sign out"
              >
                <LogOut size={16} />
              </button>
            </div>
          )}
        </div>
      </aside>
      <main className="flex-1 overflow-hidden flex flex-col h-screen">
        <header className="h-14 shrink-0 border-b border-slate-200 bg-white flex items-center justify-between md:justify-end px-4 md:px-6 gap-2">
          <div className="flex items-center gap-2 md:hidden">
            <div className="h-8 w-8 rounded-md bg-indigo-500 flex items-center justify-center font-bold text-white">
              T
            </div>
            <span className="font-semibold text-lg text-slate-900">TeamFlow</span>
          </div>
          <div className="flex items-center gap-1 md:gap-2">
            <ThemeToggle />
            <NotificationBell />
            {user && (
              <div className="flex items-center gap-1 md:hidden">
                <Avatar name={user.displayName} size="sm" />
                <button
                  onClick={logout}
                  className="p-2 rounded-md text-slate-600 hover:bg-slate-100 hover:text-slate-900"
                  aria-label="Sign out"
                  title="Sign out"
                >
                  <LogOut size={18} />
                </button>
              </div>
            )}
          </div>
        </header>
        <div className="flex-1 overflow-y-auto overflow-x-hidden">
          <Outlet />
        </div>
        <nav
          aria-label="Primary"
          className="md:hidden shrink-0 h-16 bg-white border-t border-slate-200 shadow-[0_-1px_3px_rgba(0,0,0,0.05)] grid grid-cols-4"
        >
          {navItems.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                cn(
                  'flex flex-col items-center justify-center gap-0.5 text-[11px] font-medium transition-colors',
                  isActive
                    ? 'text-indigo-600'
                    : 'text-slate-500 hover:text-slate-900',
                )
              }
            >
              <Icon size={20} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>
      </main>
    </div>
  );
}
