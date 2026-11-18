using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RL.Backend.Commands;
using RL.Backend.Commands.Handlers.PlanProcedures;
using RL.Backend.Exceptions;
using RL.Data;
using RL.Data.DataModels;

namespace RL.Backend.UnitTests;

[TestClass]
public class RemoveUserFromPlanProcedureTests
{
    [TestMethod]
    [DataRow(0, 1, 1)]
    [DataRow(1, -1, 1)]
    [DataRow(1, 1, 0)]
    public async Task RemoveUserFromPlanProcedureTests_InvalidIds_ReturnsBadRequest(int planId, int procedureId, int userId)
    {
        //Given
        var context = new Mock<RLContext>();
        var sut = new RemoveUserFromPlanProcedureCommandHandler(context.Object);
        var request = new RemoveUserFromPlanProcedureCommand
        {
            PlanId = planId,
            ProcedureId = procedureId,
            UserId = userId
        };

        //When
        var result = await sut.Handle(request, new CancellationToken());

        //Then
        result.Exception.Should().BeOfType(typeof(BadRequestException));
        result.Succeeded.Should().BeFalse();
    }

    [TestMethod]
    public async Task RemoveUserFromPlanProcedureTests_PlanProcedureNotFound_ReturnsNotFound()
    {
        //Given
        var context = DbContextHelper.CreateContext();
        var sut = new RemoveUserFromPlanProcedureCommandHandler(context);
        var request = new RemoveUserFromPlanProcedureCommand { PlanId = 1, ProcedureId = 1, UserId = 1 };

        //When
        var result = await sut.Handle(request, new CancellationToken());

        //Then
        result.Exception.Should().BeOfType(typeof(NotFoundException));
        result.Succeeded.Should().BeFalse();
    }

    [TestMethod]
    public async Task RemoveUserFromPlanProcedureTests_Assigned_RemovesOnlyThatUser()
    {
        //Given
        var context = DbContextHelper.CreateContext();
        context.PlanProcedures.Add(new PlanProcedure { PlanId = 1, ProcedureId = 1 });
        context.PlanProcedureUsers.AddRange(
            new PlanProcedureUser { PlanId = 1, ProcedureId = 1, UserId = 1 },
            new PlanProcedureUser { PlanId = 1, ProcedureId = 1, UserId = 2 });
        await context.SaveChangesAsync();
        var sut = new RemoveUserFromPlanProcedureCommandHandler(context);
        var request = new RemoveUserFromPlanProcedureCommand { PlanId = 1, ProcedureId = 1, UserId = 1 };

        //When
        var result = await sut.Handle(request, new CancellationToken());

        //Then
        var remainingUserIds = await context.PlanProcedureUsers.Select(ppu => ppu.UserId).ToListAsync();
        remainingUserIds.Should().BeEquivalentTo(new[] { 2 });
        result.Succeeded.Should().BeTrue();
    }

    [TestMethod]
    public async Task RemoveUserFromPlanProcedureTests_NotAssigned_ReturnsSuccess()
    {
        //Given
        var context = DbContextHelper.CreateContext();
        context.PlanProcedures.Add(new PlanProcedure { PlanId = 1, ProcedureId = 1 });
        await context.SaveChangesAsync();
        var sut = new RemoveUserFromPlanProcedureCommandHandler(context);
        var request = new RemoveUserFromPlanProcedureCommand { PlanId = 1, ProcedureId = 1, UserId = 1 };

        //When
        var result = await sut.Handle(request, new CancellationToken());

        //Then
        result.Succeeded.Should().BeTrue();
    }
}
