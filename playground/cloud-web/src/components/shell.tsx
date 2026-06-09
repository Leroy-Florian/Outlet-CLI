import type { ReactNode } from 'react'
import type { SessionUser } from '../domain/models'
import { navigate } from '../router'
import { Button } from './ui/button'

export function Shell({ user, onLogout, children }: { user: SessionUser; onLogout: () => void; children: ReactNode }) {
  return (
    <div className="min-h-svh">
      <header className="flex items-center justify-between border-b px-6 py-4">
        <button onClick={() => navigate('/')} className="flex items-center gap-2 font-medium">
          <div className="bg-primary text-primary-foreground flex size-6 items-center justify-center rounded-md text-xs">O</div>
          Outlet Cloud
        </button>
        <div className="flex items-center gap-3 text-sm">
          <span className="bg-muted rounded-full px-2 py-0.5 text-xs font-medium">{user.plan}</span>
          <span className="text-muted-foreground">{user.email}</span>
          <Button variant="outline" className="w-auto px-3" onClick={onLogout}>Log out</Button>
        </div>
      </header>
      <main className="mx-auto max-w-4xl p-6 md:p-10">{children}</main>
    </div>
  )
}
