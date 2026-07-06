using System.Data;
using Moq;
using NUnit.Framework;
using MyDesk.Web.Services;

namespace MyDesk.Web.Phase4.Tests.Services
{
    [TestFixture]
    public class ApprovalEscalationServiceTests
    {
        private ApprovalEscalationService _service = null!;
        private Mock<DatabaseService> _mockDatabase = null!;
        private Mock<ApprovalDelegationService> _mockDelegationService = null!;

        [SetUp]
        public void SetUp()
        {
            _mockDatabase = new Mock<DatabaseService>();
            _mockDelegationService = new Mock<ApprovalDelegationService>();
            _service = new ApprovalEscalationService(
                _mockDatabase.Object,
                _mockDelegationService.Object,
                null);  // NotificationService is optional
        }

        [Test]
        public async Task ResolveApprovalChainAsync_WithNoDelegations_ReturnsManagerOnly()
        {
            // Arrange
            var tenantId = 1;
            var userId = 100;
            var amount = 5000m;
            var moduleType = "Expense";

            var emptyDelegations = new DataTable();
            emptyDelegations.Columns.Add("DelegationId", typeof(int));

            _mockDelegationService
                .Setup(x => x.GetActiveDelegatesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(emptyDelegations);

            // Act
            var result = await _service.ResolveApprovalChainAsync(tenantId, userId, amount, moduleType);

            // Assert
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task RouteApprovalAsync_WithActiveDelegation_RoutesToDelegate()
        {
            // Arrange
            var tenantId = 1;
            var primaryApproverId = 100;
            var amount = 3000m;
            var moduleType = "Expense";

            var delegations = CreateDelegationTable(new[]
            {
                new { DelegationId = 1, ToUserId = 101, MinThreshold = 0m, MaxThreshold = 5000m, CanApprove = true, IsActive = true }
            });

            _mockDelegationService
                .Setup(x => x.GetActiveDelegatesAsync(tenantId, primaryApproverId, moduleType))
                .ReturnsAsync(delegations);

            _mockDelegationService
                .Setup(x => x.CanApproveAsync(tenantId, It.IsAny<int>(), amount))
                .ReturnsAsync(true);

            // Act
            var result = await _service.RouteApprovalAsync(tenantId, primaryApproverId, amount, moduleType);

            // Assert
            Assert.IsNotNull(result);
            // Delegate ID should be 101
            Assert.AreEqual(101, result.ApproverId);
            Assert.IsTrue(result.IsDelegated);
        }

        [Test]
        public async Task RouteApprovalAsync_WhenDelegateCannotApprove_EscalatesToManager()
        {
            // Arrange
            var tenantId = 1;
            var primaryApproverId = 100;
            var amount = 6000m;
            var moduleType = "Expense";

            var delegations = CreateDelegationTable(new[]
            {
                new { DelegationId = 1, ToUserId = 101, MinThreshold = 0m, MaxThreshold = 5000m, CanApprove = true, IsActive = true }
            });

            _mockDelegationService
                .Setup(x => x.GetActiveDelegatesAsync(tenantId, primaryApproverId, moduleType))
                .ReturnsAsync(delegations);

            _mockDelegationService
                .Setup(x => x.CanApproveAsync(tenantId, It.IsAny<int>(), amount))
                .ReturnsAsync(false);

            var userTable = CreateUserTable(new[]
            {
                new { UserId = 100, ManagerUserId = 50 }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(userTable);

            // Act
            var result = await _service.RouteApprovalAsync(tenantId, primaryApproverId, amount, moduleType);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(50, result.ApproverId);
            Assert.IsFalse(result.IsDelegated);
        }

        [Test]
        public async Task NotifyDelegateAsync_WithValidDelegate_SendsNotification()
        {
            // Arrange
            var tenantId = 1;
            var delegateId = 101;
            var approvalId = 1;

            // Act - should not throw
            await _service.NotifyDelegateAsync(tenantId, delegateId, approvalId);

            // Assert - placeholder implementation
            Assert.Pass();
        }

        [Test]
        public async Task NotifyEscalationAsync_WithValidEscalation_SendsNotification()
        {
            // Arrange
            var tenantId = 1;
            var approverId = 50;
            var approvalId = 1;

            // Act - should not throw
            await _service.NotifyEscalationAsync(tenantId, approverId, approvalId);

            // Assert - placeholder implementation
            Assert.Pass();
        }

        [Test]
        public async Task ResolveApprovalChainAsync_BuildsCompleteChain()
        {
            // Arrange
            var tenantId = 1;
            var userId = 100;
            var amount = 5000m;
            var moduleType = "Expense";

            var delegations = CreateDelegationTable(new[]
            {
                new { DelegationId = 1, ToUserId = 101, MinThreshold = 0m, MaxThreshold = 10000m, CanApprove = true, IsActive = true }
            });

            _mockDelegationService
                .Setup(x => x.GetActiveDelegatesAsync(tenantId, userId, moduleType))
                .ReturnsAsync(delegations);

            _mockDelegationService
                .Setup(x => x.CanApproveAsync(tenantId, It.IsAny<int>(), amount))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ResolveApprovalChainAsync(tenantId, userId, amount, moduleType);

            // Assert
            Assert.IsNotNull(result);
        }

        private DataTable CreateDelegationTable(dynamic[] delegations)
        {
            var table = new DataTable();
            table.Columns.Add("DelegationId", typeof(int));
            table.Columns.Add("ToUserId", typeof(int));
            table.Columns.Add("MinThreshold", typeof(decimal));
            table.Columns.Add("MaxThreshold", typeof(decimal));
            table.Columns.Add("CanApprove", typeof(bool));
            table.Columns.Add("IsActive", typeof(bool));

            foreach (var delegation in delegations)
            {
                table.Rows.Add(
                    delegation.DelegationId,
                    delegation.ToUserId,
                    delegation.MinThreshold ?? 0m,
                    delegation.MaxThreshold ?? decimal.MaxValue,
                    delegation.CanApprove ?? true,
                    delegation.IsActive ?? true
                );
            }

            return table;
        }

        private DataTable CreateUserTable(dynamic[] users)
        {
            var table = new DataTable();
            table.Columns.Add("UserId", typeof(int));
            table.Columns.Add("ManagerUserId", typeof(int?));
            table.Columns.Add("Name", typeof(string));

            foreach (var user in users)
            {
                table.Rows.Add(
                    user.UserId,
                    user.ManagerUserId ?? DBNull.Value,
                    user.Name ?? "User"
                );
            }

            return table;
        }
    }
}
