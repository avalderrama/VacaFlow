// TypeScript mirrors of the real API contracts (verified against
// src/BigSolutions.VacaFlow.Api/Contracts/*.cs). Dates travel as
// "yyyy-MM-dd" strings (DateOnly's JSON form); the client never parses them
// into Date objects (S1, US-017 plan).

export type EmployeeRole = 'Employee' | 'Manager';

export interface AuthenticatedUser {
  id: string;
  fullName: string;
  email: string;
  role: EmployeeRole;
}

export interface AbsenceType {
  id: string;
  code: string;
  name: string;
}

export type RequestState = 'Draft' | 'Submitted' | 'Approved' | 'Rejected' | 'Cancelled';

export interface RequestDetail {
  id: string;
  absenceTypeId: string;
  startDate: string;
  endDate: string;
  reason: string;
  state: RequestState;
}

export interface RequestPayload {
  absenceTypeId: string;
  startDate: string;
  endDate: string;
  reason: string;
}

export interface ApiError {
  code: string;
  message: string;
  field?: string | null;
}
