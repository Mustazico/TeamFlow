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
      <aside className="w-60 bg-slate-900 text-slate-100 flex flex-col h-screen sticky top-0">
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
        <header className="h-14 shrink-0 border-b border-slate-200 bg-white flex items-center justify-end px-6 gap-2">
          <ThemeToggle />
          <NotificationBell />
        </header>
        <div className="flex-1 overflow-y-auto overflow-x-hidden">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
