import { useEffect, useState } from 'react'

/** Minimal hash router — no dependency, owns its routing like the rest of Outlet. */
export function useHashRoute(): string {
  const [route, setRoute] = useState(readHash)

  useEffect(() => {
    const handler = () => setRoute(readHash())
    window.addEventListener('hashchange', handler)
    return () => window.removeEventListener('hashchange', handler)
  }, [])

  return route
}

export function navigate(path: string): void {
  window.location.hash = path
}

function readHash(): string {
  const hash = window.location.hash.slice(1)
  return hash.length > 0 ? hash : '/'
}
