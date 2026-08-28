import { useState } from 'react'
import { FunnelPage } from './pages/FunnelPage'
import { LeadsPage } from './pages/LeadsPage'
import { ProcessesPage } from './pages/ProcessesPage'
import { TasksPage } from './pages/TasksPage'

type Tab = 'leads' | 'funnel' | 'tasks' | 'processes'

const tabs: { id: Tab; label: string }[] = [
  { id: 'leads', label: 'Лиды' },
  { id: 'funnel', label: 'Воронка' },
  { id: 'tasks', label: 'Задачи' },
  { id: 'processes', label: 'Процессы' },
]

export function App() {
  const [tab, setTab] = useState<Tab>('leads')
  const [version, setVersion] = useState(0)

  return (
    <div className="app">
      <header className="masthead">
        <h1>CRM Vova</h1>
        <span className="sub">этап 0 · ядро продаж и движок процессов</span>
      </header>

      <nav className="tabs">
        {tabs.map((item) => (
          <button key={item.id} className={tab === item.id ? 'active' : ''} onClick={() => setTab(item.id)}>
            {item.label}
          </button>
        ))}
      </nav>

      {tab === 'leads' && <LeadsPage key={version} onQualified={() => setVersion((v) => v + 1)} />}
      {tab === 'funnel' && <FunnelPage key={version} />}
      {tab === 'tasks' && <TasksPage key={version} />}
      {tab === 'processes' && <ProcessesPage key={version} />}
    </div>
  )
}
