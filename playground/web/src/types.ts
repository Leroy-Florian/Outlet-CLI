export type OptionType = 'string' | 'int' | 'bool'

export interface OptionField {
  name: string
  type: OptionType
  secret: boolean
}

export interface Adapter {
  id: string
  name: string
  registration: string
  options: OptionField[]
}

export interface Concern {
  id: string
  name: string
  port: string
  enabled: boolean
  adapters: Adapter[]
}

export interface Catalog {
  concerns: Concern[]
}

export type OptionValue = string | boolean

export interface SendRequest {
  adapter: string
  options: Record<string, OptionValue>
  message: {
    from: string
    to: string[]
    subject: string
    text: string | null
    html: string | null
  }
}

export interface SendResult {
  success: boolean
  messageId: string | null
  error: string | null
  elapsedMs: number
  adapterType: string
  port: string
}
