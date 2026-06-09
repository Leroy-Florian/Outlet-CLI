import { Effect } from 'effect'
import { useState, type FormEvent } from 'react'
import { OutletApi, useEffectFn } from '../runtime'
import { Button } from './ui/button'
import { Input } from './ui/input'
import { Label } from './ui/label'

type Props = {
  onAuthenticated: () => Promise<void> | void
  onSwitch: () => void
  onForgot: () => void
}

export function LoginForm({ onAuthenticated, onSwitch, onForgot }: Props) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const login = useEffectFn((e: string, p: string) => Effect.flatMap(OutletApi, (api) => api.login(e, p)))

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    try {
      await login.run(email, password)
      await onAuthenticated()
    } catch {
      // login.error carries the typed failure rendered below.
    }
  }

  const errorText = login.error
    ? login.error.status === 401
      ? 'Invalid email or password.'
      : login.error.message
    : null

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
            <button type="button" onClick={onForgot} className="ml-auto text-sm underline-offset-4 hover:underline">Forgot your password?</button>
          </div>
          <Input id="password" type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
        </div>
        <Button type="submit" disabled={login.running}>{login.running ? 'Signing in…' : 'Login'}</Button>
        {errorText && <p className="text-destructive text-center text-sm">{errorText}</p>}
      </div>
      <div className="text-center text-sm">
        Don&apos;t have an account?{' '}
        <button type="button" onClick={onSwitch} className="underline underline-offset-4">Sign up</button>
      </div>
    </form>
  )
}
