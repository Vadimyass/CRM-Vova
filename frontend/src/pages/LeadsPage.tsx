import { useState } from 'react'
import { api } from '../api/client'
import { useAsync } from '../components/useAsync'

const emptyForm = { title: '', contactName: '', companyName: '', amount: '' }

export function LeadsPage({ onQualified }: { onQualified: () => void }) {
  const { data: leads, error, loading, reload } = useAsync(() => api.leads(), [])
  const [form, setForm] = useState(emptyForm)
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState<string | null>(null)

  const create = async () => {
    if (!form.title.trim()) return
    setBusy(true)
    try {
      await api.createLead({
        title: form.title.trim(),
        contactName: form.contactName || null,
        companyName: form.companyName || null,
        phone: null,
        email: null,
        estimatedAmount: form.amount ? Number(form.amount) : null,
      })
      setForm(emptyForm)
      setMessage('Лид создан. Процесс «Обработка нового лида» стартует автоматически.')
      await reload()
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : String(cause))
    } finally {
      setBusy(false)
    }
  }

  const qualify = async (id: string) => {
    setBusy(true)
    try {
      const opportunity = await api.qualifyLead(id)
      setMessage(`Создана сделка «${opportunity.title}» на стадии «${opportunity.stageName}».`)
      await reload()
      onQualified()
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : String(cause))
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <div className="card" style={{ marginBottom: '1.25rem' }}>
        <div className="form-grid">
          <div className="field">
            <label htmlFor="lead-title">Название</label>
            <input
              id="lead-title"
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
              placeholder="Внедрение CRM в «Аквамарин»"
            />
          </div>
          <div className="field">
            <label htmlFor="lead-contact">Контакт</label>
            <input id="lead-contact" value={form.contactName} onChange={(e) => setForm({ ...form, contactName: e.target.value })} />
          </div>
          <div className="field">
            <label htmlFor="lead-company">Компания</label>
            <input id="lead-company" value={form.companyName} onChange={(e) => setForm({ ...form, companyName: e.target.value })} />
          </div>
          <div className="field">
            <label htmlFor="lead-amount">Сумма</label>
            <input id="lead-amount" type="number" value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} />
          </div>
        </div>
        <button className="primary" onClick={create} disabled={busy || !form.title.trim()}>
          Создать лид
        </button>
        {message && <div style={{ marginTop: '0.6rem', color: 'var(--muted)' }}>{message}</div>}
      </div>

      {error && <div className="error">{error}</div>}
      {loading && !leads && <div className="empty">Загрузка…</div>}

      {leads && (
        <table>
          <thead>
            <tr>
              <th>Лид</th>
              <th>Контакт</th>
              <th>Компания</th>
              <th className="num">Сумма</th>
              <th>Статус</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {leads.map((lead) => (
              <tr key={lead.id}>
                <td>{lead.title}</td>
                <td>{lead.contactName ?? '—'}</td>
                <td>{lead.companyName ?? '—'}</td>
                <td className="num">{lead.estimatedAmount?.toLocaleString('ru-RU') ?? '—'}</td>
                <td>
                  <span className={`badge ${lead.status === 'Qualified' ? 'ok' : ''}`}>{lead.status}</span>
                </td>
                <td style={{ textAlign: 'right' }}>
                  <button onClick={() => qualify(lead.id)} disabled={busy || lead.status === 'Qualified'}>
                    Квалифицировать
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  )
}
