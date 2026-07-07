using System.Data;
using Moq;
using NUnit.Framework;
using MyDesk.Web.Services;

namespace MyDesk.Web.Phase4.Tests.Services
{
    [TestFixture]
    public class TeamServiceTests
    {
        private TeamService _service = null!;
        private Mock<DatabaseService> _mockDatabase = null!;

        [SetUp]
        public void SetUp()
        {
            _mockDatabase = new Mock<DatabaseService>();
            _service = new TeamService(_mockDatabase.Object);
        }

        [Test]
        public async Task GetTeamsAsync_WithValidTenantId_ReturnsTeams()
        {
            // Arrange
            var tenantId = 1;
            var expectedTable = CreateTeamTable(new[]
            {
                new { TeamId = 1, Name = "Finance Team", DepartmentId = 1, TenantId = 1, Status = "Active" },
                new { TeamId = 2, Name = "HR Team", DepartmentId = 2, TenantId = 1, Status = "Active" }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetTeamsAsync(tenantId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Rows.Count);
        }

        [Test]
        public async Task GetTeamAsync_WithValidId_ReturnsTeam()
        {
            // Arrange
            var tenantId = 1;
            var teamId = 1;
            var expectedTable = CreateTeamTable(new[]
            {
                new { TeamId = 1, Name = "Finance Team", DepartmentId = 1, TenantId = 1, Status = "Active", Description = "Finance Approval Team" }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetTeamAsync(tenantId, teamId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Rows.Count);
            Assert.AreEqual("Finance Team", result.Rows[0]["Name"]);
        }

        [Test]
        public async Task CreateTeamAsync_WithValidData_InsertsTeam()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var name = "Sales Team";
            var description = "Sales Department Team";
            var teamLeadId = 100;

            _mockDatabase
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateTeamAsync(tenantId, departmentId, name, description, teamLeadId);

            // Assert
            _mockDatabase.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()), Times.Once);
        }

        [Test]
        public async Task AddTeamMemberAsync_WithValidData_AddsUserToTeam()
        {
            // Arrange
            var tenantId = 1;
            var teamId = 1;
            var userId = 100;
            var role = "Member";

            _mockDatabase
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.AddTeamMemberAsync(tenantId, teamId, userId, role);

            // Assert
            _mockDatabase.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()), Times.Once);
        }

        [Test]
        public async Task RemoveTeamMemberAsync_WithValidData_RemovesUserFromTeam()
        {
            // Arrange
            var tenantId = 1;
            var teamId = 1;
            var userId = 100;

            _mockDatabase
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.RemoveTeamMemberAsync(tenantId, teamId, userId);

            // Assert
            _mockDatabase.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()), Times.Once);
        }

        [Test]
        public async Task GetTeamMembersAsync_WithValidTeamId_ReturnsMembersWithDetails()
        {
            // Arrange
            var tenantId = 1;
            var teamId = 1;
            var expectedTable = CreateTeamMembersTable(new[]
            {
                new { TeamMemberId = 1, UserId = 100, Name = "John Doe", Email = "john@example.com", Role = "Member", JoinedAt = DateTime.UtcNow },
                new { TeamMemberId = 2, UserId = 101, Name = "Jane Smith", Email = "jane@example.com", Role = "Lead", JoinedAt = DateTime.UtcNow }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetTeamMembersAsync(tenantId, teamId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Rows.Count);
        }

        [Test]
        public async Task GetUserTeamsAsync_WithValidUserId_ReturnsUserTeams()
        {
            // Arrange
            var tenantId = 1;
            var userId = 100;
            var expectedTable = CreateTeamTable(new[]
            {
                new { TeamId = 1, Name = "Finance Team", DepartmentId = 1, TenantId = 1, Status = "Active" }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetUserTeamsAsync(tenantId, userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Rows.Count > 0);
        }

        [Test]
        public async Task UpdateTeamAsync_WithValidData_UpdatesTeam()
        {
            // Arrange
            var tenantId = 1;
            var teamId = 1;
            var name = "Finance Team Updated";
            var description = "Updated Finance Team";

            _mockDatabase
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateTeamAsync(tenantId, teamId, name, description);

            // Assert
            _mockDatabase.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()), Times.Once);
        }

        private DataTable CreateTeamTable(dynamic[] teams)
        {
            var table = new DataTable();
            table.Columns.Add("TeamId", typeof(int));
            table.Columns.Add("TenantId", typeof(int));
            table.Columns.Add("DepartmentId", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("TeamLeadUserId", typeof(int?));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("IsApprovalTeam", typeof(bool));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("UpdatedAt", typeof(DateTime));

            foreach (var team in teams)
            {
                table.Rows.Add(
                    team.TeamId,
                    team.TenantId,
                    team.DepartmentId,
                    team.Name,
                    team.Description ?? "",
                    team.TeamLeadUserId ?? DBNull.Value,
                    team.Status,
                    false,
                    DateTime.UtcNow,
                    DateTime.UtcNow
                );
            }

            return table;
        }

        private DataTable CreateTeamMembersTable(dynamic[] members)
        {
            var table = new DataTable();
            table.Columns.Add("TeamMemberId", typeof(int));
            table.Columns.Add("TeamId", typeof(int));
            table.Columns.Add("UserId", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("Role", typeof(string));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("JoinedAt", typeof(DateTime));

            foreach (var member in members)
            {
                table.Rows.Add(
                    member.TeamMemberId,
                    1,
                    member.UserId,
                    member.Name,
                    member.Email,
                    member.Role,
                    "Active",
                    member.JoinedAt
                );
            }

            return table;
        }
    }
}
