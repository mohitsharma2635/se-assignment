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
public class RemoveAllUsersFromPlanProcedureTests
{
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(1, -1)]
    [DataRow(int.MinValue, int.MinValue)]
    public async Task RemoveAllUsersFromPlanProcedureTests_InvalidIds_ReturnsBadRequest(int planId, int procedureId)
    {
        //Given
        var context = new Mock<RLContext>();
        var sut = new RemoveAllUsersFromPlanProcedureCommandHandler(context.Object);
        var request = new RemoveAllUsersFromPlanProcedureCommand
        {
            PlanId = planId,
            ProcedureId = procedureId
        };

        //When
        var result = await sut.Handle(request, new CancellationToken());

        //Then
        result.Exception.Should().BeOfType(typeof(BadRequestException));
        result.Succeeded.Should().BeFalse();
    }

    [TestMethod]
    public async Task RemoveAllUsersFromPlanProcedureTests_PlanProcedureNotFound_ReturnsNotFound()
    {
        //Given
        var context = DbContextHelper.CreateContext();
        var sut = new RemoveAllUsersFromPlanProcedureCommandHandler(context);
        var request = new RemoveAllUsersFromPlanProcedureCommand { PlanId = 1, ProcedureId = 1 };

        //When
        var result = await sut.Handle(request, new CancellationToken());

        //Then
        result.Exception.Should().BeOfType(typeof(NotFoundException));
        result.Succeeded.Should().BeFalse();
    }

    [TestMethod]
    public async Task RemoveAllUsersFromPlanProcedureTests_HasUsers_RemovesOnlyThatProceduresUsers()
    {
        //Given
        var context = DbContextHelper.CreateContext();
        context.PlanProcedures.AddRange(
            new PlanProcedure { PlanId = 1, ProcedureId = 1 },
            new PlanProcedure { PlanId = 1, ProcedureId = 2 });
        context.PlanProcedureUsers.AddRange(
            new PlanProcedureUser { PlanId = 1, ProcedureId = 1, UserId = 1 },
            new PlanProcedureUser { PlanId = 1, ProcedureId = 1, UserId = 2 },
            new PlanProcedureUser { PlanId = 1, ProcedureId = 2, UserId = 1 });
        await context.SaveChangesAsync();
        var sut = new RemoveAllUsersFromPlanProcedureCommandHandler(context);
        var request = new RemoveAllUsersFromPlanProcedureCommand { PlanId = 1, ProcedureId = 1 };

        //When
        var result = await sut.Handle(request, new CancellationToken());

        //Then
        (await context.PlanProcedureUsers.AnyAsync(ppu => ppu.ProcedureId == 1)).Should().BeFalse();
        (await context.PlanProcedureUsers.CountAsync(ppu => ppu.ProcedureId == 2)).Should().Be(1);
        result.Succeeded.Should().BeTrue();
    }
}
