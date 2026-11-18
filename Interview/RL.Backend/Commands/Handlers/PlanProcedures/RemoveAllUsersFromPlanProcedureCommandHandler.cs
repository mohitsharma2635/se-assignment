using MediatR;
using Microsoft.EntityFrameworkCore;
using RL.Backend.Exceptions;
using RL.Backend.Models;
using RL.Data;

namespace RL.Backend.Commands.Handlers.PlanProcedures;

public class RemoveAllUsersFromPlanProcedureCommandHandler : IRequestHandler<RemoveAllUsersFromPlanProcedureCommand, ApiResponse<Unit>>
{
    private readonly RLContext _context;

    public RemoveAllUsersFromPlanProcedureCommandHandler(RLContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<Unit>> Handle(RemoveAllUsersFromPlanProcedureCommand request, CancellationToken cancellationToken)
    {
        try
        {
            //Validate request
            if (request.PlanId < 1)
                return ApiResponse<Unit>.Fail(new BadRequestException("Invalid PlanId"));
            if (request.ProcedureId < 1)
                return ApiResponse<Unit>.Fail(new BadRequestException("Invalid ProcedureId"));

            var planProcedureExists = await _context.PlanProcedures
                .AnyAsync(pp => pp.PlanId == request.PlanId && pp.ProcedureId == request.ProcedureId, cancellationToken);
            if (!planProcedureExists)
                return ApiResponse<Unit>.Fail(new NotFoundException($"ProcedureId: {request.ProcedureId} not found in PlanId: {request.PlanId}"));

            var planProcedureUsers = await _context.PlanProcedureUsers
                .Where(ppu => ppu.PlanId == request.PlanId && ppu.ProcedureId == request.ProcedureId)
                .ToListAsync(cancellationToken);

            if (planProcedureUsers.Count == 0)
                return ApiResponse<Unit>.Succeed(new Unit());

            _context.PlanProcedureUsers.RemoveRange(planProcedureUsers);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<Unit>.Succeed(new Unit());
        }
        catch (Exception e)
        {
            return ApiResponse<Unit>.Fail(e);
        }
    }
}
