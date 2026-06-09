import { LoginForm } from './components/login-form'

/** shadcn/ui "login-04" block: a split screen — login form on the left, brand panel on the right. */
export function App() {
  return (
    <div className="grid min-h-svh lg:grid-cols-2">
      <div className="flex flex-col gap-4 p-6 md:p-10">
        <div className="flex justify-center gap-2 md:justify-start">
          <a href="#" className="flex items-center gap-2 font-medium">
            <div className="bg-primary text-primary-foreground flex size-6 items-center justify-center rounded-md">
              <OutletMark />
            </div>
            Outlet
          </a>
        </div>
        <div className="flex flex-1 items-center justify-center">
          <div className="w-full max-w-xs">
            <LoginForm />
          </div>
        </div>
      </div>
      <div className="bg-muted relative hidden lg:block">
        <div className="absolute inset-0 bg-gradient-to-br from-zinc-900 via-zinc-800 to-zinc-700" />
        <div className="absolute inset-0 flex items-center justify-center p-10">
          <blockquote className="max-w-md text-balance text-center text-lg font-medium text-zinc-100">
            “A copy-paste registry for .NET backend infrastructure. Own the code; swap the provider in one line.”
          </blockquote>
        </div>
      </div>
    </div>
  )
}

function OutletMark() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="size-4" aria-hidden="true">
      <rect x="3" y="3" width="18" height="18" rx="4" stroke="currentColor" strokeWidth="2" />
      <circle cx="9" cy="10" r="1.4" fill="currentColor" />
      <circle cx="15" cy="10" r="1.4" fill="currentColor" />
      <path d="M8 15h8" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}
