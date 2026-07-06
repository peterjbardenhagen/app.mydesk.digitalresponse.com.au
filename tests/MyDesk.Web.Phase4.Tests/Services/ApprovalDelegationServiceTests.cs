using System.Data;
using Moq;
using NUnit.Framework;
using MyDesk.Web.Services;

namespace MyDesk.Web.Phase4.Tests.Services
{
    [TestFixture]
    public class ApprovalDelegationServiceTests
    {
        private ApprovalDelegationService _service = null!;
        private Mock<DatabaseService> _mockDatabase = null!;

        [SetUp]
        public void SetUp()
        {
            _mockDatabase = new Mock<DatabaseService>();
            _service = new ApprovalDelegationService(_mockDatabase.Object);
        }

        [Test]
        public async Task CreateDelegationAsync_WithValidData_InsertsDelegation()
        {
            // Arrange
            var tenantId = 1;
            var teamId = 1;
            var fromUserId = 100;
            var toUserId = 101;
            var moduleType = "Expense";
            var minThreshold = 0m;
            var maxThreshold = 5000m;
            var startDate = DateTime.UtcNow;
            var endDate = DateTime.UtcNow.AddMonths(1);
            var canApprove = true;
            var canReject = true;
            var canDelegate = false;
            var canComment = true;

            _mockDatabase
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateDelegationAsync(
                tenantId, teamId, fromUserId, toUserId, moduleType,
                minThreshold, maxThreshold, startDate, endDate,
                canApprove, canReject, canDelegate, canComment);

            // Assert
            _mockDatabase.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Test]
        public async Task GetActiveDelegatesAsync_WithValidUser_ReturnsDelegates()
        {
            // Arrange
            var tenantId = 1;
            var userId = 100;
            var expectedTable = CreateDelegationTable(new[]
            {
                new { DelegationId = 1, FromUserId = 100, ToUserId = 101, ModuleType = "Expense", IsActive = true }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetActiveDelegatesAsync(tenantId, userId, "Expense");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Rows.Count);
            Assert.AreEqual(true, result.Rows[0]["IsActive"]);
        }

        [Test]
        public async Task GetDelegationAsync_WithValidId_ReturnsDelegation()
        {
            // Arrange
            var tenantId = 1;
            var delegationId = 1;
            var expectedTable = CreateDelegationTable(new[]
            {
                new { DelegationId = 1, FromUserId = 100, ToUserId = 101, ModuleType = "Expense", MinThreshold = 0m, MaxThreshold = 5000m }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetDelegationAsync(tenantId, delegationId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Rows.Count);
        }

        [Test]
        public async Task CanApproveAsync_WhenAmountWithinThreshold_ReturnsTrue()
        {
            // Arrange
            var tenantId = 1;
            var delegateId = 101;
            var amount = 3000m;
            var expectedTable = CreateDelegationTable(new[]
            {
                new { DelegationId = 1, ToUserId = 101, MinThreshold = 0m, MaxThreshold = 5000m, CanApprove = true, IsActive = true }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.CanApproveAsync(tenantId, delegateId, amount);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task CanApproveAsync_WhenAmountExceedsMaxThreshold_ReturnsFalse()
        {
            // Arrange
            var tenantId = 1;
            var delegateId = 101;
            var amount = 6000m;
            var expectedTable = CreateDelegationTable(new[]
            {
                new { DelegationId = 1, ToUserId = 101, MinThreshold = 0m, MaxThreshold = 5000m, CanApprove = true, IsActive = true }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.CanApproveAsync(tenantId, delegateId, amount);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task GetUserDelegationsAsync_WithValidUserId_ReturnsDelegations()
        {
            // Arrange
            var tenantId = 1;
            var userId = 100;
            var expectedTable = CreateDelegationTable(new[]
            {
                new { DelegationId = 1, FromUserId = 100, ToUserId = 101, IsActive = true },
                new { DelegationId = 2, FromUserId = 102, ToUserId = 100, IsActive = true }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetUserDelegationsAsync(tenantId, userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Rows.Count > 0);
        }

        [Test]
        public async Task DeactivateDelegationAsync_WithValidId_DeactivatesDelegation()
        {
            // Arrange
            var tenantId = 1;
            var delegationId = 1;

            _mockDatabase
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.DeactivateDelegationAsync(tenantId, delegationId);

            // Assert
            _mockDatabase.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Test]
        public async Task CanApproveAsync_WhenDelegationInactive_ReturnsFalse()
        {
            // Arrange
            var tenantId = 1;
            var delegateId = 101;
            var amount = 3000m;
            var expectedTable = CreateDelegationTable(new[]
            {
                new { DelegationId = 1, ToUserId = 101, MinThreshold = 0m, MaxThreshold = 5000m, CanApprove = true, IsActive = false }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.CanApproveAsync(tenantId, delegateId, amount);

            // Assert
            Assert.IsFalse(result);
        }

        private DataTable CreateDelegationTable(dynamic[] delegations)
        {
            var table = new DataTable();
            table.Columns.Add("DelegationId", typeof(int));
            table.Columns.Add("TenantId", typeof(int));
            table.Columns.Add("TeamId", typeof(int));
            table.Columns.Add("FromUserId", typeof(int));
            table.Columns.Add("ToUserId", typeof(int));
            table.Columns.Add("ModuleType", typeof(string));
            table.Columns.Add("MinThreshold", typeof(decimal));
            table.Columns.Add("MaxThreshold", typeof(decimal));
            table.Columns.Add("StartDate", typeof(DateTime));
            table.Columns.Add("EndDate", typeof(DateTime));
            table.Columns.Add("CanApprove", typeof(bool));
            table.Columns.Add("CanReject", typeof(bool));
            table.Columns.Add("CanDelegate", typeof(bool));
            table.Columns.Add("CanComment", typeof(bool));
            table.Columns.Add("IsActive", typeof(bool));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("UpdatedAt", typeof(DateTime));

            foreach (var delegation in delegations)
            {
                table.Rows.Add(
                    delegation.DelegationId,
                    1,
                    1,
                    delegation.FromUserId,
                    delegation.ToUserId,
                    delegation.ModuleType ?? "Expense",
                    delegation.MinThreshold ?? 0m,
                    delegation.MaxThreshold ?? decimal.MaxValue,
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddMonths(1),
                    delegation.CanApprove ?? true,
                    delegation.CanReject ?? true,
                    delegation.CanDelegate ?? false,
                    delegation.CanComment ?? true,
                    delegation.IsActive ?? true,
                    DateTime.UtcNow,
                    DateTime.UtcNow
                );
            }

            return table;
        }
    }
}
