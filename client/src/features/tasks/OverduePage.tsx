import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { tasksApi } from '@/lib/api';
import { Card, CardBody } from '@/components/ui/Card';
import { PageContainer, PageHeader } from '@/components/common/PageHeader';
import { PriorityBadge, StatusBadge } from '@/components/ui/Badge';
import { Avatar } from '@/components/ui/Avatar';
import { Calendar } from 'lucide-react';
import { formatDate } from '@/lib/utils';

export function OverduePage() {
  const { data: tasks = [], isLoading } = useQuery({
    queryKey: ['tasks', 'overdue'],
    queryFn: tasksApi.overdue,
  });

  return (
    <PageContainer>
      <PageHeader
        title="Overdue tasks"
        description="Tasks past their due date across all your projects."
      />
      {isLoading ? (
        <p className="text-slate-500">Loading…</p>
      ) : tasks.length === 0 ? (
        <Card>
          <CardBody>
            <p className="text-slate-600">No overdue tasks. Nice work!</p>
          </CardBody>
        </Card>
      ) : (
        <Card>
          <ul className="divide-y divide-slate-100">
            {tasks.map((t) => (
              <li key={t.id}>
                <Link
                  to={`/projects/${t.projectId}?task=${t.id}`}
                  className="block px-5 py-3 hover:bg-slate-50 transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-slate-900 truncate">{t.title}</p>
                      <div className="flex items-center gap-2 mt-1">
                        <StatusBadge status={t.status} />
                        <PriorityBadge priority={t.priority} />
                        {t.dueDate && (
                          <span className="text-xs text-rose-600 inline-flex items-center gap-1 font-medium">
                            <Calendar size={12} />
                            {formatDate(t.dueDate)}
                          </span>
                        )}
                      </div>
                    </div>
                    {t.assigneeName && (
                      <div className="flex items-center gap-2 text-xs text-slate-600">
                        <Avatar name={t.assigneeName} size="sm" />
                        <span className="hidden sm:inline">{t.assigneeName}</span>
                      </div>
                    )}
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        </Card>
      )}
    </PageContainer>
  );
}
