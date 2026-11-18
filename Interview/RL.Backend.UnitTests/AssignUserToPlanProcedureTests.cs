using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using RL.Backend.Commands;
using RL.Backend.Commands.Handlers.PlanProcedures;
using RL.Backend.Exceptions;
using RL.Data;
using RL.Data.DataModels;

namespace RL.Backend.UnitTests;

[TestClass]
public class AssignUserToPlanProcedureTests
{
    [TestMethod]
    [DataRow(-1, 1, 1)]
    [DataRow(1, 0, 1)]
    [DataRow(1, 1, int.MinValue)]
    public async Task AssignUserToPlanProcedureTests_InvalidIds_ReturnsBadRequest(int planId, int procedureId, int userId)
    {
        //Given
        var context = new Mock<RLContext>();
        var sut = new AssignUserToPlanProcedureCommandHandler(context.Object);
        var request = new AssignUserToPlanProcedureCommand
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
    public async Task AssignUserToPlanProcedureTests_PlanProcedureNotFound_ReturnsNotFound()
    {
        //Given
        var context = DbContextHelper.CreateContext();
        await SeedAsync(context, planId: 1, procedureId: 1, userId: 1);
        var sut = new AssignUserToPlanProcedureCommandHandler(context);
        var request = new AssignUserToPlanProcedureCommand { PlanId = 1, ProcedureId = 2, UserId = 1 };

        //When
        var result = await sut.Handle(request, new CancellationToken());

        //Then
        result.Exception.Should().BeOfType(typeof(NotFoundException));
        result.Succeeded.Should().BeFalse();
    }

    [TestMethod]
    public async Task AssignUserToPlanProcedureTests_UserNotFound_ReturnsNotFound()
    {
        //Given
        var context = DbContextHelper.CreateContext();
        await SeedAsync(context, planId: 1, procedureId: 1, userId: 1);
        var sut = new AssignUserToPlanProcedureCommandHandler(context);
        var request = new AssignUserToPlanProcedureCommand { PlanId = 1, ProcedureId = 1, UserId = 99 };

        //When
        var result = await sut.Handle(request, new CancellationToken());

        //Then
        result.Exception.Should().BeOfType(typeof(NotFoundException));
        result.Succeeded.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(1, 1, 1)]
    [DataRow(19, 1010, 3)]
    public async Task AssignUserToPlanProcedureTests_NotAssigned_SavesAssignment(int planId, int procedureId, int userId)
    {
        //Given
        var context = DbContextHelper.CreateContext();
        await SeedAsync(context, planId, procedureId, userId);
        var sut = new AssignUserToPlanProcedureCommandHandler(context);
        var request = new AssignUserToPlanProcedureCommand { PlanId = planId, ProcedureId = procedureId, UserId = userId };

        //When
        var result = await sut.Handle(request, new CancellationToken());

        //Then
        var dbAssignment = await context.PlanProcedureUsers
            .FirstOrDefaultAsync(ppu => ppu.PlanId == planId && ppu.ProcedureId == procedureId && ppu.UserId == userId);
        dbAssignment.Should().NotBeNull();
        result.Value.Should().BeOfType(typeof(Unit));
        result.Succeeded.Should().BeTrue();
    }

    [TestMethod]
    public async Task AssignUserToPlanProcedureTests_AlreadyAssigned_ReturnsSuccessWithoutDuplicate()
    {
        //Given
        var context = DbContextHelper.CreateContext();
        await SeedAsync(context, planId: 1, procedureId: 1, userId: 1);
        context.PlanProcedureUsers.Add(new PlanProcedureUser { PlanId = 1, ProcedureId = 1, UserId = 1 });
        await context.SaveChangesAsync();
        var sut = new AssignUserToPlanProcedureCommandHandler(context);
        var request = new AssignUserToPlanProcedureCommand { PlanId = 1, ProcedureId = 1, UserId = 1 };

        //When
        var result = await sut.Handle(request, new CancellationToken());

        //Then
        (await context.PlanProcedureUsers.CountAsync()).Should().Be(1);
        result.Succeeded.Should().BeTrue();
    }

    private static async Task SeedAsync(RLContext context, int planId, int procedureId, int userId)
    {
        context.Plans.Add(new Plan { PlanId = planId });
        context.Procedures.Add(new Procedure { ProcedureId = procedureId, ProcedureTitle = "Test Procedure" });
        context.PlanProcedures.Add(new PlanProcedure { PlanId = planId, ProcedureId = procedureId });
        context.Users.Add(new User { UserId = userId, Name = "Test User" });
        await context.SaveChangesAsync();
    }
}
