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
  approval: RequestDecision | null;
}

export interface RequestDecision {
  responsibleManagerName: string;
  decision: 'Approved' | 'Rejected';
  comment: string | null;
  decidedAtUtc: string;
}

// The C# contract's fields are all nullable (Guid?/DateOnly?/string?) so the
// handler's own Validate() reports a proper VF-VAL-001 for a missing field.
// A blank form control's value is '' — sending that literally would fail
// ASP.NET Core's JSON model binding for Guid?/DateOnly? before Validate()
// ever runs, surfacing as a generic 500 instead of the catalogued error.
// null is the only value that binds correctly to "absent".
export interface RequestPayload {
  absenceTypeId: string | null;
  startDate: string | null;
  endDate: string | null;
  reason: string | null;
}

export interface RequestSummary {
  id: string;
  absenceType: { id: string; code: string; name: string };
  startDate: string;
  endDate: string;
  reason: string;
  state: RequestState;
  employee: { id: string; fullName: string };
  createdAtUtc: string;
}

export interface ApiError {
  code: string;
  message: string;
  field?: string | null;
}
