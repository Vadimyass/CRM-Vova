import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { ProcessLogEntry } from '../api/types'
import { useAsync } from '../components/useAsync'

const statusClass: Record<string, string> = {
  Completed: 'ok',
  Failed: 'err',
  Waiting: 'warn',
}

export function ProcessesPage() {
  const { data: instances, error } = useAsync(() => api.processes(), [])
  const [selected, setSelected] = useState<string | null>(null)
  const [log, setLog] = useState<ProcessLogEntry[]>([])

  useEffect(() => {
    if (!selected) {
      setLog([])
      return
    }

    void api.processLog(selected).then(setLog)
  }, [selected])

  if (error) return <div className="error">{error}</div>
  if (!instances) return <div className="empty">Загрузка…</div>
  if (instances.length === 0) return <div className="empty">Экземпляров процессов пока нет.</div>

  return (
    <>
      <table>
        <thead>
          <tr>
            <th>Процесс</th>
            <th>Версия</th>
            <th>Объект</th>
            <th>Статус</th>
            <th>Старт</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {instances.map((instance) => (
            <tr key={instance.id}>
              <td>{instance.definitionKey}</td>
              <td className="num">v{instance.definitionVersion}</td>
              <td>{instance.subjectEntityName ?? '—'}</td>
              <td>
                <span className={`badge ${statusClass[instance.status] ?? ''}`}>{instance.status}</span>
              </td>
              <td>{new Date(instance.startedOn).toLocaleString('ru-RU')}</td>
              <td style={{ textAlign: 'right' }}>
                <button onClick={() => setSelected(selected === instance.id ? null : instance.id)}>
                  {selected === instance.id ? 'Скрыть журнал' : 'Журнал'}
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {selected && log.length > 0 && (
        <table className="log" style={{ marginTop: '1rem' }}>
          <thead>
            <tr>
              <th>Элемент</th>
              <th>Событие</th>
              <th>Детали</th>
              <th>Время</th>
            </tr>
          </thead>
          <tbody>
            {log.map((entry, index) => (
              <tr key={index}>
                <td>{entry.elementId || '—'}</td>
                <td>{entry.event}</td>
                <td>{entry.details ?? '—'}</td>
                <td>{new Date(entry.timestamp).toLocaleTimeString('ru-RU')}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  )
}
