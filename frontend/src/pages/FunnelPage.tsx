import { useState } from 'react'
import { api } from '../api/client'
import { useAsync } from '../components/useAsync'

export function FunnelPage() {
  const stages = useAsync(() => api.stages(), [])
  const opportunities = useAsync(() => api.opportunities(), [])
  const [dragging, setDragging] = useState<string | null>(null)
  const [dropTarget, setDropTarget] = useState<string | null>(null)

  const move = async (opportunityId: string, stageId: string) => {
    await api.moveStage(opportunityId, stageId)
    await opportunities.reload()
  }

  if (stages.error || opportunities.error) {
    return <div className="error">{stages.error ?? opportunities.error}</div>
  }

  if (!stages.data || !opportunities.data) {
    return <div className="empty">Загрузка…</div>
  }

  if (opportunities.data.length === 0) {
    return <div className="empty">Сделок пока нет — квалифицируйте лид на вкладке «Лиды».</div>
  }

  return (
    <div className="board">
      {stages.data.map((stage) => {
        const deals = opportunities.data!.filter((o) => o.stageId === stage.id)
        const total = deals.reduce((sum, deal) => sum + deal.amount, 0)

        return (
          <div
            key={stage.id}
            className={`column ${dropTarget === stage.id ? 'drop-target' : ''}`}
            onDragOver={(e) => {
              e.preventDefault()
              setDropTarget(stage.id)
            }}
            onDragLeave={() => setDropTarget((current) => (current === stage.id ? null : current))}
            onDrop={async () => {
              setDropTarget(null)
              if (dragging) {
                await move(dragging, stage.id)
                setDragging(null)
              }
            }}
          >
            <h3>
              <span>{stage.name}</span>
              <span>{stage.probability}%</span>
            </h3>
            {deals.map((deal) => (
              <div
                key={deal.id}
                className={`deal ${dragging === deal.id ? 'dragging' : ''}`}
                draggable
                onDragStart={() => setDragging(deal.id)}
                onDragEnd={() => setDragging(null)}
              >
                <div className="title">{deal.title}</div>
                <div className="amount">
                  {deal.amount.toLocaleString('ru-RU')} {deal.currency}
                </div>
              </div>
            ))}
            {deals.length > 0 && (
              <div className="amount" style={{ color: 'var(--muted)', fontSize: '0.75rem', paddingTop: '0.3rem' }}>
                Итого: {total.toLocaleString('ru-RU')}
              </div>
            )}
          </div>
        )
      })}
    </div>
  )
}
