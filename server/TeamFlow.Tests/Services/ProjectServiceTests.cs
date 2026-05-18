using FluentAssertions;
using TeamFlow.Application.Common.Exceptions;
using TeamFlow.Application.Projects;
using TeamFlow.Application.Projects.Dtos;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;

namespace TeamFlow.Tests.Services;

public class ProjectServiceTests
{
    [Fact]
    public async Task CreateAsync_AddsCallerAsOwnerMember()
    {
        var db = TestDb.Create();
        var me = db.AddUser("Me", "me@x.io");
        var svc = new ProjectService(db, new StubCurrentUser { UserId = me.Id }, new NoopActivityLogger());

        var dto = await svc.CreateAsync(new CreateProjectRequest("P", null, null), CancellationToken.None);

        dto.OwnerId.Should().Be(me.Id);
        db.ProjectMembers.Should().ContainSingle(m => m.ProjectId == dto.Id && m.UserId == me.Id && m.Role == ProjectRole.Owner);
    }

    [Fact]
    public async Task AddMemberAsync_ByEmail_Adds()
    {
        var db = TestDb.Create();
        var owner = db.AddUser("Owner", "owner@x.io");
        var invitee = db.AddUser("Inv", "inv@x.io");
        var project = db.AddProject(owner);

        var svc = new ProjectService(db, new StubCurrentUser { UserId = owner.Id }, new NoopActivityLogger());

        var dto = await svc.AddMemberAsync(project.Id, new AddMemberRequest("inv@x.io", ProjectRole.Member), CancellationToken.None);

        dto.UserId.Should().Be(invitee.Id);
        db.ProjectMembers.Should().Contain(m => m.ProjectId == project.Id && m.UserId == invitee.Id);
    }

    [Fact]
    public async Task AddMemberAsync_UnknownEmail_ThrowsNotFound()
    {
        var db = TestDb.Create();
        var owner = db.AddUser("Owner", "owner@x.io");
        var project = db.AddProject(owner);
        var svc = new ProjectService(db, new StubCurrentUser { UserId = owner.Id }, new NoopActivityLogger());

        var act = () => svc.AddMemberAsync(project.Id, new AddMemberRequest("ghost@x.io", ProjectRole.Member), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddMemberAsync_AsNonAdmin_Throws()
    {
        var db = TestDb.Create();
        var owner = db.AddUser("Owner", "owner@x.io");
        var member = db.AddUser("M", "m@x.io");
        var invitee = db.AddUser("I", "i@x.io");
        var project = db.AddProject(owner);
        db.AddMember(project, member, ProjectRole.Member);

        var svc = new ProjectService(db, new StubCurrentUser { UserId = member.Id }, new NoopActivityLogger());

        var act = () => svc.AddMemberAsync(project.Id, new AddMemberRequest("i@x.io", ProjectRole.Member), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task RemoveMemberAsync_RemovesRow()
    {
        var db = TestDb.Create();
        var owner = db.AddUser("Owner", "owner@x.io");
        var member = db.AddUser("M", "m@x.io");
        var project = db.AddProject(owner);
        db.AddMember(project, member);

        var svc = new ProjectService(db, new StubCurrentUser { UserId = owner.Id }, new NoopActivityLogger());

        await svc.RemoveMemberAsync(project.Id, member.Id, CancellationToken.None);

        db.ProjectMembers.Should().NotContain(m => m.UserId == member.Id);
    }

    [Fact]
    public async Task DeleteAsync_NonOwner_Forbidden()
    {
        var db = TestDb.Create();
        var owner = db.AddUser("Owner", "owner@x.io");
        var admin = db.AddUser("Admin", "a@x.io");
        var project = db.AddProject(owner);
        db.AddMember(project, admin, ProjectRole.Admin);

        var svc = new ProjectService(db, new StubCurrentUser { UserId = admin.Id }, new NoopActivityLogger());

        var act = () => svc.DeleteAsync(project.Id, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
