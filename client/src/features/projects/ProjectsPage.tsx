import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Plus, Users, ListTodo, CheckCircle2 } from 'lucide-react';
import { projectsApi } from '@/lib/api';
import { Card, CardBody } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { FieldError, Input, Label, Textarea } from '@/components/ui/Input';
import { Modal } from '@/components/ui/Modal';
import { PageContainer, PageHeader } from '@/components/common/PageHeader';
import { formatDate, cn } from '@/lib/utils';
import { toast } from 'sonner';
import { isAxiosError } from 'axios';

const DEFAULT_COLOR = '#6366f1';
const PALETTE = ['#6366f1', '#0ea5e9', '#10b981', '#f59e0b', '#f43f5e', '#8b5cf6'];

const createSchema = z.object({
  name: z.string().min(1, 'Required').max(120),
  description: z.string().max(1000).optional(),
  color: z
    .string()
    .regex(/^#[0-9a-fA-F]{6}$/, 'Use a hex color like #6366f1')
    .optional()
    .or(z.literal('')),
});
type CreateValues = z.infer<typeof createSchema>;

export function ProjectsPage() {
  const [open, setOpen] = useState(false);
  const qc = useQueryClient();

  const { data: projects = [], isLoading } = useQuery({
    queryKey: ['projects'],
    queryFn: projectsApi.list,
  });

  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors },
  } = useForm<CreateValues>({
    resolver: zodResolver(createSchema),
    defaultValues: { color: DEFAULT_COLOR },
  });

  const createMutation = useMutation({
    mutationFn: projectsApi.create,
    onSuccess: () => {
      toast.success('Project created');
      qc.invalidateQueries({ queryKey: ['projects'] });
      setOpen(false);
      reset({ color: DEFAULT_COLOR });
    },
    onError: (err) => {
      const msg = isAxiosError(err)
        ? (err.response?.data as { error?: string })?.error ?? 'Failed to create project'
        : 'Failed';
      toast.error(msg);
    },
  });

  return (
    <PageContainer>
      <PageHeader
        title="Projects"
        description="All projects you own or are a member of."
        actions={
          <Button onClick={() => setOpen(true)}>
            <Plus size={16} /> New project
          </Button>
        }
      />

      {isLoading ? (
        <p className="text-slate-500">Loading…</p>
      ) : projects.length === 0 ? (
        <Card>
          <CardBody className="text-center py-12">
            <p className="text-slate-600 mb-3">No projects yet.</p>
            <Button onClick={() => setOpen(true)}>
              <Plus size={16} /> Create your first project
            </Button>
          </CardBody>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {projects.map((p) => (
            <Link key={p.id} to={`/projects/${p.id}`} className="group">
              <Card className="h-full transition-shadow group-hover:shadow-md">
                <CardBody>
                  <div className="flex items-center gap-3 mb-3">
                    <div
                      className="h-10 w-10 rounded-md flex-shrink-0"
                      style={{ backgroundColor: p.color }}
                    />
                    <div className="min-w-0">
                      <h3 className="font-semibold text-slate-900 truncate group-hover:text-indigo-600">
                        {p.name}
                      </h3>
                      <p className="text-xs text-slate-500">
                        Owner: {p.ownerName} · {formatDate(p.createdAt)}
                      </p>
                    </div>
                  </div>
                  {p.description && (
                    <p className="text-sm text-slate-600 line-clamp-2 mb-4">{p.description}</p>
                  )}
                  <div className="flex items-center gap-4 text-xs text-slate-500">
                    <span className="flex items-center gap-1">
                      <Users size={14} />
                      {p.memberCount}
                    </span>
                    <span className="flex items-center gap-1">
                      <ListTodo size={14} />
                      {p.taskCount}
                    </span>
                    <span className="flex items-center gap-1">
                      <CheckCircle2 size={14} />
                      {p.doneTaskCount}/{p.taskCount}
                    </span>
                  </div>
                </CardBody>
              </Card>
            </Link>
          ))}
        </div>
      )}

      <Modal open={open} onClose={() => setOpen(false)} title="Create project">
        <form
          onSubmit={handleSubmit((v) =>
            createMutation.mutate({
              name: v.name,
              description: v.description?.trim() || undefined,
              color: v.color?.trim() || undefined,
            }),
          )}
          className="space-y-4"
        >
          <div>
            <Label htmlFor="name">Name</Label>
            <Input id="name" {...register('name')} />
            <FieldError message={errors.name?.message} />
          </div>
          <div>
            <Label htmlFor="description">Description</Label>
            <Textarea id="description" {...register('description')} />
            <FieldError message={errors.description?.message} />
          </div>
          <div>
            <Label htmlFor="color">Color</Label>
            <Controller
              control={control}
              name="color"
              render={({ field }) => {
                const value = field.value || DEFAULT_COLOR;
                return (
                  <div className="flex items-center gap-3">
                    <input
                      id="color"
                      type="color"
                      value={value}
                      onChange={(e) => field.onChange(e.target.value)}
                      className="h-10 w-14 rounded-md border border-slate-300 bg-white cursor-pointer"
                    />
                    <span className="font-mono text-sm text-slate-600">{value}</span>
                    <div className="flex items-center gap-1 ml-auto">
                      {PALETTE.map((c) => (
                        <button
                          key={c}
                          type="button"
                          onClick={() => field.onChange(c)}
                          className={cn(
                            'h-6 w-6 rounded-full border-2 transition-transform hover:scale-110',
                            value.toLowerCase() === c.toLowerCase()
                              ? 'border-slate-900'
                              : 'border-white shadow-sm',
                          )}
                          style={{ backgroundColor: c }}
                          aria-label={`Pick color ${c}`}
                        />
                      ))}
                    </div>
                  </div>
                );
              }}
            />
            <FieldError message={errors.color?.message} />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={createMutation.isPending}>
              {createMutation.isPending ? 'Creating…' : 'Create'}
            </Button>
          </div>
        </form>
      </Modal>
    </PageContainer>
  );
}
