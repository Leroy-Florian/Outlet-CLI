import { useEffect, useMemo, useState } from 'react'
import { getCatalog, sendEmail } from './api'
import type { Adapter, Catalog, Concern, OptionValue, SendResult } from './types'

function defaultOptions(adapter: Adapter): Record<string, OptionValue> {
  const values: Record<string, OptionValue> = {}
  for (const field of adapter.options) {
    values[field.name] = field.type === 'bool' ? false : ''
  }
  return values
}

export function App() {
  const [catalog, setCatalog] = useState<Catalog | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [concernId, setConcernId] = useState('')
  const [adapterId, setAdapterId] = useState('')
  const [options, setOptions] = useState<Record<string, OptionValue>>({})
  const [from, setFrom] = useState('demo@outlet.dev')
  const [to, setTo] = useState('dev@example.com')
  const [subject, setSubject] = useState('Hello from Outlet')
  const [text, setText] = useState('Swapping email providers is a one-line change.')
  const [result, setResult] = useState<SendResult | null>(null)
  const [sendError, setSendError] = useState<string | null>(null)
  const [sending, setSending] = useState(false)

  useEffect(() => {
    getCatalog()
      .then((loaded) => {
        setCatalog(loaded)
        const firstEnabled = loaded.concerns.find((concern) => concern.enabled)
        if (firstEnabled) {
          setConcernId(firstEnabled.id)
          setAdapterId(firstEnabled.adapters[0]?.id ?? '')
        }
      })
      .catch((error: unknown) => setLoadError(error instanceof Error ? error.message : String(error)))
  }, [])

  const concern: Concern | undefined = useMemo(
    () => catalog?.concerns.find((item) => item.id === concernId),
    [catalog, concernId],
  )
  const adapter: Adapter | undefined = useMemo(
    () => concern?.adapters.find((item) => item.id === adapterId),
    [concern, adapterId],
  )

  useEffect(() => {
    if (adapter) {
      setOptions(defaultOptions(adapter))
      setResult(null)
      setSendError(null)
    }
  }, [adapter])

  async function handleSend() {
    if (!adapter) return
    setSending(true)
    setSendError(null)
    setResult(null)
    try {
      const sent = await sendEmail({
        adapter: adapter.id,
        options,
        message: {
          from,
          to: to.split(',').map((value) => value.trim()).filter(Boolean),
          subject,
          text: text || null,
          html: null,
        },
      })
      setResult(sent)
    } catch (error: unknown) {
      setSendError(error instanceof Error ? error.message : String(error))
    } finally {
      setSending(false)
    }
  }

  if (loadError) {
    return <div className="status error">Could not reach the playground API: {loadError}</div>
  }
  if (!catalog) {
    return <div className="status">Loading…</div>
  }

  return (
    <div className="layout">
      <aside className="sidebar">
        <h1>Outlet</h1>
        <p className="tagline">Swagger UI for infra ports</p>
        <nav>
          {catalog.concerns.map((item) => (
            <button
              key={item.id}
              className={`concern ${item.id === concernId ? 'active' : ''}`}
              disabled={!item.enabled}
              onClick={() => {
                setConcernId(item.id)
                setAdapterId(item.adapters[0]?.id ?? '')
              }}
            >
              {item.name}
              {!item.enabled && <span className="soon">soon</span>}
            </button>
          ))}
        </nav>
      </aside>

      <main className="main">
        {concern && (
          <>
            <header className="head">
              <h2>{concern.name}</h2>
              <code className="port">port: {concern.port}</code>
            </header>

            <section className="adapters">
              {concern.adapters.map((item) => (
                <button
                  key={item.id}
                  className={`adapter ${item.id === adapterId ? 'active' : ''}`}
                  onClick={() => setAdapterId(item.id)}
                >
                  {item.name}
                </button>
              ))}
            </section>

            {adapter && (
              <p className="swap">
                swap = one line: <code>services.{adapter.registration}(…)</code>
              </p>
            )}

            {adapter && (
              <section className="card">
                <h3>Options</h3>
                <div className="grid">
                  {adapter.options.map((field) => (
                    <label key={field.name} className="field">
                      <span>
                        {field.name}
                        {field.secret ? ' 🔒' : ''}
                      </span>
                      {field.type === 'bool' ? (
                        <input
                          type="checkbox"
                          checked={Boolean(options[field.name])}
                          onChange={(event) =>
                            setOptions((current) => ({ ...current, [field.name]: event.target.checked }))
                          }
                        />
                      ) : (
                        <input
                          type={field.secret ? 'password' : field.type === 'int' ? 'number' : 'text'}
                          value={String(options[field.name] ?? '')}
                          onChange={(event) =>
                            setOptions((current) => ({ ...current, [field.name]: event.target.value }))
                          }
                        />
                      )}
                    </label>
                  ))}
                </div>
              </section>
            )}

            <section className="card">
              <h3>Message</h3>
              <div className="grid">
                <label className="field">
                  <span>from</span>
                  <input value={from} onChange={(event) => setFrom(event.target.value)} />
                </label>
                <label className="field">
                  <span>to (comma-separated)</span>
                  <input value={to} onChange={(event) => setTo(event.target.value)} />
                </label>
                <label className="field">
                  <span>subject</span>
                  <input value={subject} onChange={(event) => setSubject(event.target.value)} />
                </label>
                <label className="field wide">
                  <span>text</span>
                  <textarea value={text} onChange={(event) => setText(event.target.value)} />
                </label>
              </div>
            </section>

            <button className="send" disabled={sending || !adapter} onClick={handleSend}>
              {sending ? 'Sending…' : 'Send'}
            </button>

            {sendError && <div className="status error">error: {sendError}</div>}

            {result && (
              <section className={`result ${result.success ? 'ok' : 'fail'}`}>
                <div className="verdict">{result.success ? '✅ Sent' : '❌ Not delivered'}</div>
                <dl>
                  <dt>adapter</dt>
                  <dd>
                    {result.adapterType} <span className="muted">behind {result.port}</span>
                  </dd>
                  <dt>elapsed</dt>
                  <dd>{result.elapsedMs} ms</dd>
                  {result.messageId && (
                    <>
                      <dt>message id</dt>
                      <dd>{result.messageId}</dd>
                    </>
                  )}
                  {result.error && (
                    <>
                      <dt>provider error</dt>
                      <dd className="err">{result.error}</dd>
                    </>
                  )}
                </dl>
              </section>
            )}
          </>
        )}
      </main>
    </div>
  )
}
