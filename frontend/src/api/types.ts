export interface Lead {
  id: string
  title: string
  contactName: string | null
  companyName: string | null
  phone: string | null
  email: string | null
  estimatedAmount: number | null
  status: string
  ownerId: string | null
  createdOn: string
}

export interface Opportunity {
  id: string
  title: string
  amount: number
  currency: string
  stageId: string
  stageName: string
  closeDate: string | null
  ownerId: string | null
  stageEnteredOn: string
}

export interface Stage {
  id: string
  name: string
  order: number
  probability: number
  isFinal: boolean
  isWon: boolean
  color: string | null
}

export interface UserTask {
  id: string
  processInstanceId: string
  title: string
  roleCode: string | null
  assigneeId: string | null
  dueDate: string | null
  status: string
  subjectEntityName: string | null
  subjectEntityId: string | null
  createdOn: string
}

export interface ProcessInstance {
  id: string
  definitionKey: string
  definitionVersion: number
  status: string
  subjectEntityName: string | null
  subjectEntityId: string | null
  startedOn: string
  completedOn: string | null
  error: string | null
}

export interface ProcessLogEntry {
  elementId: string
  event: string
  details: string | null
  timestamp: string
}
