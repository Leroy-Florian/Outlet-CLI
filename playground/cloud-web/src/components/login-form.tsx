import { useState, type ComponentProps, type FormEvent } from 'react'
import { cn } from '../lib/utils'
import { Button } from './ui/button'
import { Input } from './ui/input'
import { Label } from './ui/label'

type Status =
  | { kind: 'idle' }
  | { kind: 'loading' }
  | { kind: 'error'; message: string }
  | { kind: 'success'; displayName: string }

export function LoginForm({ className, ...props }: ComponentProps<'form'>) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [status, setStatus] = useState<Status>({ kind: 'idle' })

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setStatus({ kind: 'loading' })
    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      if (!response.ok) {
        setStatus({
          kind: 'error',
          message: response.status === 401 ? 'Invalid email or password.' : `Login failed (${response.status}).`,
        })
        return
      }
      const body = (await response.json()) as { displayName: string }
      setStatus({ kind: 'success', displayName: body.displayName })
    } catch (error) {
      setStatus({ kind: 'error', message: error instanceof Error ? error.message : 'Network error.' })
    }
  }

  return (
    <form className={cn('flex flex-col gap-6', className)} onSubmit={onSubmit} {...props}>
      <div className="flex flex-col items-center gap-2 text-center">
        <h1 className="text-2xl font-bold">Login to your account</h1>
        <p className="text-muted-foreground text-sm text-balance">
          Enter your email below to login to your Outlet organization
        </p>
      </div>
      <div className="grid gap-6">
        <div className="grid gap-3">
          <Label htmlFor="email">Email</Label>
          <Input
            id="email"
            type="email"
            placeholder="m@example.com"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
        </div>
        <div className="grid gap-3">
          <div className="flex items-center">
            <Label htmlFor="password">Password</Label>
            <a href="#" className="ml-auto text-sm underline-offset-4 hover:underline">
              Forgot your password?
            </a>
          </div>
          <Input
            id="password"
            type="password"
            required
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
        </div>
        <Button type="submit" disabled={status.kind === 'loading'}>
          {status.kind === 'loading' ? 'Signing in…' : 'Login'}
        </Button>
        {status.kind === 'error' && (
          <p className="text-destructive text-center text-sm">{status.message}</p>
        )}
        {status.kind === 'success' && (
          <p className="text-center text-sm">Welcome back, {status.displayName}.</p>
        )}
      </div>
      <div className="text-center text-sm">
        Don&apos;t have an account?{' '}
        <a href="#" className="underline underline-offset-4">
          Sign up
        </a>
      </div>
    </form>
  )
}
