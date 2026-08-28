import { useState } from 'react'
import { api } from '../api/client'
import { useAsync } from '../components/useAsync'

export function TasksPage() {
  const { data: tasks, error, reload } = useAsync(() => api.tasks(), [])
  const [busy, setBusy] = useState<string | null>(null)

  const complete = async (id: string, approved: boolean) => {
    setBusy(id)
    try {
      await api.completeTask(id, { approved })
      await reload()
    } finally {
      setBusy(null)
    }
  }

  if (error) return <div className="error">{error}</div>
  if (!tasks) return <div className="empty">Загрузка…</div>
  if (tasks.length === 0) return <div className="empty">Открытых задач нет. Переведите сделку на другую стадию — процесс поставит задачу.</div>

  return (
    <table>
      <thead>
        <tr>
          <th>Задача</th>
          <th>Роль</th>
          <th>Объект</th>
          <th>Срок</th>
          <th />
        </tr>
      </thead>
      <tbody>
        {tasks.map((task) => (
          <tr key={task.id}>
            <td>{task.title}</td>
            <td>{task.roleCode ?? '—'}</td>
            <td>{task.subjectEntityName ?? '—'}</td>
            <td>{task.dueDate ? new Date(task.dueDate).toLocaleString('ru-RU') : '—'}</td>
            <td style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
              <button className="primary" onClick={() => complete(task.id, true)} disabled={busy === task.id}>
                Согласовать
              </button>{' '}
              <button onClick={() => complete(task.id, false)} disabled={busy === task.id}>
                Отклонить
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
