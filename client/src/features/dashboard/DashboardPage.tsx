import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Legend,
  LineChart,
  Line,
  CartesianGrid,
} from 'recharts';
import { dashboardApi } from '@/lib/api';
import { Card, CardBody, CardHeader, CardTitle } from '@/components/ui/Card';
import { PageContainer, PageHeader } from '@/components/common/PageHeader';
import { CheckCircle2, FolderKanban, ListTodo, AlertTriangle, User, Circle } from 'lucide-react';

const STATUS_COLORS: Record<string, string> = {
  Todo: '#94a3b8',
  InProgress: '#0ea5e9',
  Review: '#8b5cf6',
  Done: '#10b981',
};
const PRIORITY_COLORS: Record<string, string> = {
  Low: '#94a3b8',
  Medium: '#0ea5e9',
  High: '#f59e0b',
  Critical: '#f43f5e',
};

export function DashboardPage() {
  const [days, setDays] = useState<7 | 14 | 30>(14);
  const { data, isLoading } = useQuery({
    queryKey: ['dashboard', 'overview', days],
    queryFn: () => dashboardApi.overview(days),
  });

  return (
    <PageContainer>
      <PageHeader title="Dashboard" description="Your team's overview at a glance" />
      {isLoading || !data ? (
        <div className="text-slate-500">Loading…</div>
      ) : (
        <>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4 mb-6">
            <StatCard icon={<FolderKanban size={18} />} label="Projects" value={data.stats.totalProjects} tone="indigo" to="/projects" />
            <StatCard icon={<ListTodo size={18} />} label="Total tasks" value={data.stats.totalTasks} tone="sky" />
            <StatCard icon={<Circle size={18} />} label="Open" value={data.stats.openTasks} tone="amber" />
            <StatCard icon={<CheckCircle2 size={18} />} label="Done" value={data.stats.doneTasks} tone="emerald" />
            <StatCard icon={<User size={18} />} label="My tasks" value={data.stats.myAssignedOpen} tone="violet" to="/my-tasks" />
            <StatCard icon={<AlertTriangle size={18} />} label="Overdue" value={data.stats.overdue} tone="rose" to="/overdue" />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
            <Card>
              <CardHeader>
                <CardTitle>Tasks by status</CardTitle>
              </CardHeader>
              <CardBody>
                <ResponsiveContainer width="100%" height={260}>
                  <BarChart data={data.statusBreakdown}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                    <XAxis dataKey="status" stroke="#64748b" fontSize={12} />
                    <YAxis stroke="#64748b" fontSize={12} allowDecimals={false} />
                    <Tooltip />
                    <Bar dataKey="count" radius={[4, 4, 0, 0]}>
                      {data.statusBreakdown.map((d) => (
                        <Cell key={d.status} fill={STATUS_COLORS[d.status] ?? '#6366f1'} />
                      ))}
                    </Bar>
                  </BarChart>
                </ResponsiveContainer>
              </CardBody>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Tasks by priority</CardTitle>
              </CardHeader>
              <CardBody>
                <ResponsiveContainer width="100%" height={260}>
                  <PieChart>
                    <Pie
                      data={data.priorityBreakdown}
                      dataKey="count"
                      nameKey="priority"
                      cx="50%"
                      cy="50%"
                      outerRadius={90}
                      label
                    >
                      {data.priorityBreakdown.map((d) => (
                        <Cell key={d.priority} fill={PRIORITY_COLORS[d.priority] ?? '#6366f1'} />
                      ))}
                    </Pie>
                    <Tooltip />
                    <Legend />
                  </PieChart>
                </ResponsiveContainer>
              </CardBody>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <div className="flex items-center justify-between gap-3">
                <CardTitle>Tasks completed</CardTitle>
                <select
                  value={days}
                  onChange={(e) => setDays(Number(e.target.value) as 7 | 14 | 30)}
                  className="rounded-md border border-slate-300 bg-white px-2 py-1 text-xs h-8"
                >
                  <option value={7}>Last 7 days</option>
                  <option value={14}>Last 14 days</option>
                  <option value={30}>Last 30 days</option>
                </select>
              </div>
            </CardHeader>
            <CardBody>
              <ResponsiveContainer width="100%" height={260}>
                <LineChart data={data.completedByDay}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                  <XAxis
                    dataKey="date"
                    stroke="#64748b"
                    fontSize={12}
                    tickFormatter={(v: string) => v.slice(5)}
                  />
                  <YAxis stroke="#64748b" fontSize={12} allowDecimals={false} />
                  <Tooltip />
                  <Line type="monotone" dataKey="count" stroke="#6366f1" strokeWidth={2} dot={{ r: 3 }} />
                </LineChart>
              </ResponsiveContainer>
            </CardBody>
          </Card>
        </>
      )}
    </PageContainer>
  );
}

function StatCard({
  icon,
  label,
  value,
  tone,
  to,
}: {
  icon: React.ReactNode;
  label: string;
  value: number;
  tone: 'indigo' | 'sky' | 'amber' | 'emerald' | 'violet' | 'rose';
  to?: string;
}) {
  const tones: Record<string, string> = {
    indigo: 'bg-indigo-50 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300',
    sky: 'bg-sky-50 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
    amber: 'bg-amber-50 text-amber-700 dark:bg-amber-500/20 dark:text-amber-300',
    emerald: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
    violet: 'bg-violet-50 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
    rose: 'bg-rose-50 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
  };
  const inner = (
    <Card className={to ? 'transition-shadow hover:shadow-md cursor-pointer' : undefined}>
      <CardBody className="flex items-center gap-3">
        <div className={`h-10 w-10 rounded-md flex items-center justify-center ${tones[tone]}`}>
          {icon}
        </div>
        <div>
          <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
          <p className="text-2xl font-semibold text-slate-900">{value}</p>
        </div>
      </CardBody>
    </Card>
  );
  return to ? <Link to={to}>{inner}</Link> : inner;
}
