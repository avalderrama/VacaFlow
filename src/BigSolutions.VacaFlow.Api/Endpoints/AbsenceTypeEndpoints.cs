using BigSolutions.VacaFlow.Api.Contracts;
using BigSolutions.VacaFlow.Application.AbsenceTypes;

namespace BigSolutions.VacaFlow.Api.Endpoints;

internal static class AbsenceTypeEndpoints
{
    public static void MapAbsenceTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/absence-types");

        // Receives, delegates, maps — no business conditional (CA-PRE-001).
        // No FallbackPolicy shortcut here: RequireAuthorization() is stated
        // explicitly, same reason as every other endpoint in this codebase.
        group.MapGet("", async (
            ListAbsenceTypesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var types = await handler.Handle(cancellationToken);
            return Results.Ok(types.Select(ToResponse));
        })
        .RequireAuthorization();
    }

    private static AbsenceTypeResponse ToResponse(AbsenceTypeDto dto) =>
        new(dto.Id, dto.Code, dto.Name);
}
