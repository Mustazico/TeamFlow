import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Bell } from 'lucide-react';
import { notificationsApi } from '@/lib/api';
import type { NotificationDto } from '@/lib/types';
import { cn } from '@/lib/utils';

function timeAgo(iso: string) {
  const diffMs = Date.now() - new Date(iso).getTime();
  const sec = Math.max(1, Math.floor(diffMs / 1000));
  if (sec < 60) return `${sec}s ago`;
  const min = Math.floor(sec / 60);
  if (min < 60) return `${min}m ago`;
  const hr = Math.floor(min / 60);
  if (hr < 24) return `${hr}h ago`;
  const d = Math.floor(hr / 24);
  return `${d}d ago`;
}

export function NotificationBell() {
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const navigate = useNavigate();
  const qc = useQueryClient();

  const { data: count = 0 } = useQuery({
    queryKey: ['notifications', 'unread-count'],
    queryFn: notificationsApi.unreadCount,
    refetchInterval: 10_000,
    refetchOnWindowFocus: true,
    refetchIntervalInBackground: true,
    staleTime: 0,
  });

  const { data: items = [] } = useQuery({
    queryKey: ['notifications', 'list'],
    queryFn: () => notificationsApi.list({ take: 10 }),
    enabled: open,
    refetchInterval: open ? 10_000 : false,
    refetchOnMount: 'always',
    staleTime: 0,
  });

  const markRead = useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['notifications'] });
    },
  });

  const markAllRead = useMutation({
    mutationFn: () => notificationsApi.markAllRead(),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['notifications'] });
    },
  });

  useEffect(() => {
    if (!open) return;
    const onDocClick = (e: MouseEvent) => {
      if (!containerRef.current?.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', onDocClick);
    return () => document.removeEventListener('mousedown', onDocClick);
  }, [open]);

  const handleClick = (n: NotificationDto) => {
    if (!n.isRead) markRead.mutate(n.id);
    setOpen(false);
    if (n.projectId && n.taskItemId) {
      navigate(`/projects/${n.projectId}?task=${n.taskItemId}`);
    } else if (n.projectId) {
      navigate(`/projects/${n.projectId}`);
    }
  };

  return (
    <div className="relative" ref={containerRef}>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="relative p-2 rounded-md text-slate-600 hover:bg-slate-100 hover:text-slate-900"
        aria-label="Notifications"
      >
        <Bell size={20} />
        {count > 0 && (
          <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] px-1 rounded-full bg-red-500 text-white text-[10px] font-semibold flex items-center justify-center">
            {count > 99 ? '99+' : count}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 mt-2 w-80 max-h-[28rem] overflow-y-auto rounded-lg border border-slate-200 bg-white shadow-lg z-50">
          <div className="flex items-center justify-between px-4 py-2 border-b border-slate-100">
            <span className="text-sm font-semibold">Notifications</span>
            <button
              type="button"
              onClick={() => markAllRead.mutate()}
              disabled={count === 0 || markAllRead.isPending}
              className="text-xs text-indigo-600 hover:text-indigo-800 disabled:text-slate-400"
            >
              Mark all read
            </button>
          </div>
          {items.length === 0 ? (
            <div className="px-4 py-6 text-sm text-slate-500 text-center">
              No notifications
            </div>
          ) : (
            <ul className="divide-y divide-slate-100">
              {items.map((n) => (
                <li key={n.id}>
                  <button
                    type="button"
                    onClick={() => handleClick(n)}
                    className={cn(
                      'w-full text-left px-4 py-3 hover:bg-slate-50 flex gap-3',
                      !n.isRead && 'bg-indigo-50/40',
                    )}
                  >
                    <div className="flex-1 min-w-0">
                      <p className="text-sm text-slate-800">{n.message}</p>
                      <p className="text-xs text-slate-500 mt-1">
                        {timeAgo(n.createdAt)}
                      </p>
                    </div>
                    {!n.isRead && (
                      <span className="mt-1 h-2 w-2 rounded-full bg-indigo-500 flex-shrink-0" />
                    )}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
