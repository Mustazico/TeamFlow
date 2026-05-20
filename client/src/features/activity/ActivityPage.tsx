import { useQuery } from '@tanstack/react-query';
import { activityApi } from '@/lib/api';
import { Card } from '@/components/ui/Card';
import { PageHeader } from '@/components/common/PageHeader';
import { Avatar } from '@/components/ui/Avatar';
import { relativeTime } from '@/lib/utils';
import { Link } from 'react-router-dom';

export function ActivityPage() {
  const { data: activities = [], isLoading } = useQuery({
    queryKey: ['activity', 'recent'],
    queryFn: () => activityApi.recent(100),
  });

  return (
    <div className="h-full flex flex-col p-6 lg:p-8 max-w-6xl w-full mx-auto">
      <PageHeader title="Activity" description="Recent activity across your projects." />
      <Card className="flex-1 min-h-0 flex flex-col overflow-hidden">
        {isLoading ? (
          <p className="text-slate-500 p-5">Loading…</p>
        ) : activities.length === 0 ? (
          <p className="text-slate-600 p-5">No activity yet.</p>
        ) : (
          <ul className="divide-y divide-slate-100 flex-1 min-h-0 overflow-y-auto">
            {activities.map((a) => (
              <li key={a.id} className="flex gap-3 px-5 py-3">
                <Avatar name={a.userName} />
                <div className="flex-1 min-w-0">
                  <p className="text-sm text-slate-800">
                    <span className="font-medium">{a.userName}</span>{' '}
                    <span className="text-slate-600">{a.summary ?? a.action}</span>
                    {a.projectId && a.projectName && (
                      <>
                        {' '}
                        in{' '}
                        <Link
                          to={`/projects/${a.projectId}`}
                          className="text-indigo-600 hover:underline"
                        >
                          {a.projectName}
                        </Link>
                      </>
                    )}
                  </p>
                  <p className="text-xs text-slate-500 mt-0.5">{relativeTime(a.createdAt)}</p>
                </div>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
