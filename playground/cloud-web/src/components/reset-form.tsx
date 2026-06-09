import { Effect } from 'effect'
import { useState, type FormEvent } from 'react'
import { OutletApi, useEffectFn } from '../runtime'
import { Button } from './ui/button'
import { Input } from './ui/input'
import { Label } from './ui/label'

type Props = {
  onDone: () => void
  onSwitch: () => void
}

export function ResetForm({ onDone, onSwitch }: Props) {
  const [email, setEmail] = useState('')
  const [token, setToken] = useState('')
  const [password, setPassword] = useState('')
  const [info, setInfo] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const forgot = useEffectFn((e: string) => Effect.flatMap(OutletApi, (api) => api.forgotPassword(e)))
  const reset = useEffectFn((e: string, t: string, p: string) => Effect.flatMap(OutletApi, (api) => api.resetPassword(e, t, p)))

  async function onRequest() {
    setError(null)
    setInfo(null)
    try {
      const issued = await forgot.run(email)
      if (issued) {
        setToken(issued)
        setInfo('Reset token issued (dev: prefilled below). Set a new password.')
      } else {
        setInfo('If that email exists, a reset token was issued.')
      }
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Request failed.')
    }
  }

  async function onReset(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    try {
      await reset.run(email, token, password)
      onDone()
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Reset failed.')
    }
  }

  return (
    <form className="flex flex-col gap-6" onSubmit={onReset}>
      <div className="flex flex-col items-center gap-2 text-center">
        <h1 className="text-2xl font-bold">Reset your password</h1>
        <p className="text-muted-foreground text-sm text-balance">Request a reset token, then choose a new password</p>
      </div>
      <div className="grid gap-6">
        <div className="grid gap-3">
          <Label htmlFor="email">Email</Label>
          <div className="flex gap-2">
            <Input id="email" type="email" placeholder="m@example.com" required value={email} onChange={(e) => setEmail(e.target.value)} />
            <Button type="button" variant="outline" className="w-auto px-3" disabled={forgot.running} onClick={() => void onRequest()}>
              {forgot.running ? '…' : 'Request'}
            </Button>
          </div>
        </div>
        {info && <p className="text-muted-foreground text-sm">{info}</p>}
        <div className="grid gap-3">
          <Label htmlFor="token">Reset token</Label>
          <Input id="token" required value={token} onChange={(e) => setToken(e.target.value)} />
        </div>
        <div className="grid gap-3">
          <Label htmlFor="password">New password</Label>
          <Input id="password" type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
        </div>
        <Button type="submit" disabled={reset.running}>{reset.running ? 'Resetting…' : 'Reset password'}</Button>
        {error && <p className="text-destructive text-center text-sm">{error}</p>}
      </div>
      <div className="text-center text-sm">
        <button type="button" onClick={onSwitch} className="underline underline-offset-4">Back to login</button>
      </div>
    </form>
  )
}
