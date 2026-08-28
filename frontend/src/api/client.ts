import type { CreateLeadPayload } from './payloads'
import type { Lead, Opportunity, ProcessInstance, ProcessLogEntry, Stage, UserTask } from './types'

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })

  if (!response.ok) {
    const body = await response.text()
    throw new Error(body || `${response.status} ${response.statusText}`)
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}

export const api = {
  leads: () => request<Lead[]>('/api/leads'),
  createLead: (payload: CreateLeadPayload) =>
    request<Lead>('/api/leads', { method: 'POST', body: JSON.stringify(payload) }),
  qualifyLead: (id: string) => request<Opportunity>(`/api/leads/${id}/qualify`, { method: 'POST' }),

  opportunities: () => request<Opportunity[]>('/api/opportunities'),
  stages: () => request<Stage[]>('/api/stages'),
  moveStage: (id: string, stageId: string) =>
    request<Opportunity>(`/api/opportunities/${id}/stage`, {
      method: 'POST',
      body: JSON.stringify({ stageId }),
    }),

  tasks: () => request<UserTask[]>('/api/tasks'),
  completeTask: (id: string, result: Record<string, unknown>) =>
    request<void>(`/api/tasks/${id}/complete`, { method: 'POST', body: JSON.stringify({ result }) }),

  processes: () => request<ProcessInstance[]>('/api/processes'),
  processLog: (id: string) => request<ProcessLogEntry[]>(`/api/processes/${id}/log`),
}
