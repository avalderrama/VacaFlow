using BigSolutions.VacaFlow.Api.Contracts;
using BigSolutions.VacaFlow.Api.ErrorHandling;
using BigSolutions.VacaFlow.Application.Requests;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Api.Endpoints;

internal static class RequestEndpoints
{
    public static void MapRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/requests");

        // Receives, delegates, maps — no business conditional (CA-PRE-001).
        // Create returns 201 + Location with the identifier only, never the
        // created request body (ADR-012, SAD.md §18).
        group.MapPost("", async (
            CreateRequestContract contract,
            CreateRequestHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateRequestCommand(
                contract.AbsenceTypeId,
                contract.StartDate,
                contract.EndDate,
                contract.Reason);

            var result = await handler.Handle(command, cancellationToken);
            return result.ToCreatedResult(id => $"/api/requests/{id}", id => new { id });
        })
        .RequireAuthorization();

        // Update returns 204 — never the mutated request body (ADR-012).
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateRequestContract contract,
            UpdateRequestHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateRequestCommand(
                id,
                contract.AbsenceTypeId,
                contract.StartDate,
                contract.EndDate,
                contract.Reason);

            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization();

        // No query parameters (FR-VIS-001: "the caller cannot influence this
        // through parameters") — the visible set is decided entirely by
        // ICurrentUser.Role, never by anything the client sends.
        group.MapGet("", async (
            ListVisibleRequestsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(cancellationToken);
            return result.ToOkResult(dtos => dtos.Select(ToSummaryResponse).ToList());
        })
        .RequireAuthorization();

        // Errors are already mapped in ErrorStatusMap by US-016
        // (VF-REQ-004 -> 403, VF-REQ-006 -> 404) — nothing to add there.
        group.MapGet("/{id:guid}", async (
            Guid id,
            GetRequestByIdHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(id, cancellationToken);
            return result.ToOkResult(ToDetailResponse);
        })
        .RequireAuthorization();

        // Empty body (FRD.md §6.3) — no contract to bind. 204 on success,
        // never the updated request (ADR-012, same delta as Create/Update).
        group.MapPost("/{id:guid}/submit", async (
            Guid id,
            SubmitRequestHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(id, cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization();

        // Empty body (FRD.md §6.3) — no contract to bind, same shape as submit.
        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            CancelRequestHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(id, cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization();

        // { comment? } (FRD.md §6.3) — no responsibleManagerId in the
        // contract (FR-DEC-006). DecisionType.Approved is wired here, never
        // read from the payload — reject (US-022) wires Rejected onto the
        // same DecideRequestHandler below.
        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            ApproveRequestContract contract,
            DecideRequestHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DecideRequestCommand(id, DecisionType.Approved, contract.Comment);
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization();

        // { comment? } (FRD.md §6.3) — same shared contract shape and
        // handler as approve; DecisionType.Rejected is wired here, never
        // read from the payload.
        group.MapPost("/{id:guid}/reject", async (
            Guid id,
            RejectRequestContract contract,
            DecideRequestHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DecideRequestCommand(id, DecisionType.Rejected, contract.Comment);
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization();
    }

    private static RequestDetailResponse ToDetailResponse(RequestDetailDto dto) =>
        new(dto.Id, dto.AbsenceTypeId, dto.StartDate, dto.EndDate, dto.Reason, dto.State);

    private static RequestSummaryResponse ToSummaryResponse(RequestSummaryDto dto) => new(
        dto.Id,
        new AbsenceTypeSummaryResponse(dto.AbsenceType.Id, dto.AbsenceType.Code, dto.AbsenceType.Name),
        dto.StartDate,
        dto.EndDate,
        dto.Reason,
        dto.State,
        new EmployeeSummaryResponse(dto.Employee.Id, dto.Employee.FullName),
        dto.CreatedAtUtc);
}
