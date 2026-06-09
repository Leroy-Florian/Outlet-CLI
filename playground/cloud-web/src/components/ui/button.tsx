import type { ComponentProps } from 'react'
import { cn } from '../../lib/utils'

const variants = {
  default: 'bg-primary text-primary-foreground shadow-xs hover:bg-primary/90',
  outline: 'border bg-background shadow-xs hover:bg-muted hover:text-foreground',
} as const

type ButtonProps = ComponentProps<'button'> & { variant?: keyof typeof variants }

export function Button({ className, variant = 'default', type = 'button', ...props }: ButtonProps) {
  return (
    <button
      type={type}
      className={cn(
        'inline-flex h-9 w-full items-center justify-center gap-2 rounded-md px-4 py-2 text-sm font-medium transition-colors outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50 disabled:pointer-events-none disabled:opacity-50',
        variants[variant],
        className,
      )}
      {...props}
    />
  )
}
