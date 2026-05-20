import { useLocation, useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { GoogleLogin } from '@react-oauth/google';
import { authApi } from '@/lib/api';
import { useAuthStore } from '@/stores/authStore';
import { Button } from '@/components/ui/Button';
import { toast } from 'sonner';
import { isAxiosError } from 'axios';

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation() as { state?: { from?: { pathname?: string } } };
  const setAuth = useAuthStore((s) => s.setAuth);

  const googleMutation = useMutation({
    mutationFn: authApi.googleLogin,
    onSuccess: (data) => {
      setAuth(data);
      toast.success(`Welcome, ${data.user.displayName}`);
      navigate(location.state?.from?.pathname ?? '/dashboard', { replace: true });
    },
    onError: (err) => {
      const msg = isAxiosError(err)
        ? (err.response?.data as { error?: string })?.error ?? 'Login failed'
        : 'Login failed';
      toast.error(msg);
    },
  });

  const guestMutation = useMutation({
    mutationFn: authApi.guestLogin,
    onSuccess: (data) => {
      setAuth(data);
      toast.success('Signed in as guest (read-only)');
      navigate(location.state?.from?.pathname ?? '/dashboard', { replace: true });
    },
    onError: () => {
      toast.error('Guest login failed');
    },
  });

  return (
    <AuthShell title="Sign in to TeamFlow" subtitle="Sign in with your Google account to continue.">
      <div className="space-y-4">
        <div className="flex justify-center">
          <GoogleLogin
            onSuccess={(credentialResponse) => {
              if (credentialResponse.credential) {
                googleMutation.mutate(credentialResponse.credential);
              }
            }}
            onError={() => toast.error('Google sign-in failed')}
            size="large"
            width="100%"
            text="signin_with"
          />
        </div>
        <div className="relative my-2">
          <div className="absolute inset-0 flex items-center">
            <span className="w-full border-t border-slate-200" />
          </div>
          <div className="relative flex justify-center text-xs uppercase">
            <span className="bg-white px-2 text-slate-500">Or</span>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          className="w-full"
          disabled={guestMutation.isPending}
          onClick={() => guestMutation.mutate()}
        >
          {guestMutation.isPending ? 'Signing in…' : 'Continue as guest (read-only)'}
        </Button>
      </div>
    </AuthShell>
  );
}

export function AuthShell({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-indigo-50 via-white to-violet-50 p-4">
      <div className="w-full max-w-md">
        <div className="flex items-center justify-center gap-2 mb-6">
          <div className="h-10 w-10 rounded-lg bg-indigo-600 text-white flex items-center justify-center font-bold text-lg">
            T
          </div>
          <span className="text-2xl font-semibold text-slate-900">TeamFlow</span>
        </div>
        <div className="bg-white rounded-xl shadow-lg border border-slate-200 p-8">
          <h1 className="text-2xl font-semibold text-slate-900 mb-1">{title}</h1>
          {subtitle && <p className="text-sm text-slate-600 mb-6">{subtitle}</p>}
          {children}
        </div>
      </div>
    </div>
  );
}
