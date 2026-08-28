export interface CreateLeadPayload {
  title: string
  contactName: string | null
  companyName: string | null
  phone: string | null
  email: string | null
  estimatedAmount: number | null
}
