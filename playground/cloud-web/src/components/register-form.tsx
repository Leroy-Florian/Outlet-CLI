import { Effect } from 'effect'
import { useState, type FormEvent } from 'react'
import { OutletApi, useEffectFn } from '../runtime'
import { Button } from './ui/button'
import { Input } from './ui/input'
import { Label } from './ui/label'

type Props = {
  onAuthenticated: () => Promise<void> | void
  onSwitch: () => void
}

export function RegisterForm({ onAuthenticated, onSwitch }: Props) {
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const register = useEffectFn((name: string, e: string, p: string) =>
    Effect.flatMap(OutletApi, (api) => api.register(e, p, name)),
  )

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    try {
      await register.run(displayName, email, password)
      await onAuthenticated()
    } catch {
      // register.error carries the typed failure rendered below.
    }
  }

  return (
    <form className="flex flex-col gap-6" onSubmit={onSubmit}>
      <div className="flex flex-col items-center gap-2 text-center">
        <h1 className="text-2xl font-bold">Create your account</h1>
        <p className="text-muted-foreground text-sm text-balance">Start your Outlet organization in seconds</p>
      </div>
      <div className="grid gap-6">
        <div className="grid gap-3">
          <Label htmlFor="name">Name</Label>
          <Input id="name" required value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
        </div>
        <div className="grid gap-3">
          <Label htmlFor="email">Email</Label>
          <Input id="email" type="email" placeholder="m@example.com" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </div>
        <div className="grid gap-3">
          <Label htmlFor="password">Password</Label>
          <Input id="password" type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
        </div>
        <Button type="submit" disabled={register.running}>{register.running ? 'Creating…' : 'Create account'}</Button>
        {register.error && <p className="text-destructive text-center text-sm">{register.error.message}</p>}
      </div>
      <div className="text-center text-sm">
        Already have an account?{' '}
        <button type="button" onClick={onSwitch} className="underline underline-offset-4">Login</button>
      </div>
    </form>
  )
}
