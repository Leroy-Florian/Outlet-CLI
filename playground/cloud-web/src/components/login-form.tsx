import { useState, type FormEvent } from 'react'
import { Button } from './ui/button'
import { Input } from './ui/input'
import { Label } from './ui/label'

type Props = {
  onAuthenticated: () => Promise<void> | void
  onSwitch: () => void
}

export function LoginForm({ onAuthenticated, onSwitch }: Props) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        credentials: 'include',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      if (!response.ok) {
        setError(response.status === 401 ? 'Invalid email or password.' : `Login failed (${response.status}).`)
        return
      }
      await onAuthenticated()
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Network error.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="flex flex-col gap-6" onSubmit={onSubmit}>
      <div className="flex flex-col items-center gap-2 text-center">
        <h1 className="text-2xl font-bold">Login to your account</h1>
        <p className="text-muted-foreground text-sm text-balance">
          Enter your email below to login to your Outlet organization
        </p>
      </div>
      <div className="grid gap-6">
        <div className="grid gap-3">
          <Label htmlFor="email">Email</Label>
          <Input id="email" type="email" placeholder="m@example.com" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </div>
        <div className="grid gap-3">
          <div className="flex items-center">
            <Label htmlFor="password">Password</Label>
            <a href="#" className="ml-auto text-sm underline-offset-4 hover:underline">
              Forgot your password?
            </a>
          </div>
          <Input id="password" type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
        </div>
        <Button type="submit" disabled={busy}>{busy ? 'Signing in…' : 'Login'}</Button>
        {error && <p className="text-destructive text-center text-sm">{error}</p>}
      </div>
      <div className="text-center text-sm">
        Don&apos;t have an account?{' '}
        <button type="button" onClick={onSwitch} className="underline underline-offset-4">
          Sign up
        </button>
      </div>
    </form>
  )
}
