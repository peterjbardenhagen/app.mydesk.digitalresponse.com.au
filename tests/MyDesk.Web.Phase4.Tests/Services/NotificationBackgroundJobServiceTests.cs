using System;
using System.Collections.Generic;
using System.Data;
using Moq;
using NUnit.Framework;
using MyDesk.Web.Services;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Phase4.Tests.Services
{
    [TestFixture]
    public class NotificationBackgroundJobServiceTests
    {
        private NotificationBackgroundJobService _service = null!;
        private Mock<DatabaseService> _mockDatabase = null!;
        private Mock<ApprovalNotificationService> _mockApprovalNotification = null!;
        private Mock<BudgetAlertService> _mockBudgetAlert = null!;

        [SetUp]
        public void SetUp()
        {
            _mockDatabase = new Mock<DatabaseService>();
            _mockApprovalNotification = new Mock<ApprovalNotificationService>();
            _mockBudgetAlert = new Mock<BudgetAlertService>();
            _service = new NotificationBackgroundJobService(
                _mockDatabase.Object,
                _mockApprovalNotification.Object,
                _mockBudgetAlert.Object,
                null);
        }

        [Test]
        public async Task SendApprovalReminders_WithActiveTenants_SendsRemindersForEachTenant()
        {
            // Arrange
            var tenantsTable = CreateTenantsTable(new[]
            {
                new { TenantId = 1 },
                new { TenantId = 2 }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<object?>()))
                .ReturnsAsync(tenantsTable);

            _mockApprovalNotification
                .Setup(x => x.SendApprovalRemindersAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(3);  // 3 reminders per tenant

            // Act
            await _service.SendApprovalReminders();

            // Assert
            _mockApprovalNotification.Verify(
                x => x.SendApprovalRemindersAsync(It.IsAny<int>(), 3),
                Times.Exactly(2));  // Called for each tenant
        }

        [Test]
        public async Task SendApprovalReminders_WithNoTenants_CompletesGracefully()
        {
            // Arrange
            var emptyTable = new DataTable();
            emptyTable.Columns.Add("TenantId", typeof(int));

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<object?>()))
                .ReturnsAsync(emptyTable);

            // Act
            await _service.SendApprovalReminders();

            // Assert - should not throw
            _mockApprovalNotification.Verify(
                x => x.SendApprovalRemindersAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test]
        public async Task CheckAllBudgetThresholds_WithActiveTenants_ChecksBudgetsForEachTenant()
        {
            // Arrange
            var tenantsTable = CreateTenantsTable(new[]
            {
                new { TenantId = 1 },
                new { TenantId = 2 }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<object?>()))
                .ReturnsAsync(tenantsTable);

            _mockBudgetAlert
                .Setup(x => x.CheckAllBudgetsAsync(It.IsAny<int>()))
                .ReturnsAsync(2);  // 2 alerts per tenant

            // Act
            await _service.CheckAllBudgetThresholds();

            // Assert
            _mockBudgetAlert.Verify(
                x => x.CheckAllBudgetsAsync(It.IsAny<int>()),
                Times.Exactly(2));  // Called for each tenant
        }

        [Test]
        public async Task ProcessDailyDigests_WithActiveUsers_ProcessesDigestForEachUser()
        {
            // Arrange
            var usersTable = CreateUsersTable(new[]
            {
                new { TenantId = 1, UserId = 100, Name = "John", Email = "john@test.com" },
                new { TenantId = 1, UserId = 200, Name = "Jane", Email = "jane@test.com" }
            });

            var userDataTable = CreateUserDataTable(new[]
            {
                new { Email = "john@test.com", Name = "John" }
            });

            var budgetAlertsTable = new DataTable();
            budgetAlertsTable.Columns.Add("AlertCount", typeof(int));
            budgetAlertsTable.Rows.Add(2);

            var approvalNotifsTable = new DataTable();
            approvalNotifsTable.Columns.Add("NotificationCount", typeof(int));
            approvalNotifsTable.Rows.Add(1);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(s => s.Contains("NotificationSettings")),
                    It.IsAny<object?>()))
                .ReturnsAsync(usersTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(s => s.Contains("FROM dbo.Users")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(userDataTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(s => s.Contains("BudgetAlerts")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(budgetAlertsTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(s => s.Contains("ApprovalNotifications")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(approvalNotifsTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            await _service.ProcessDailyDigests();

            // Assert
            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(s => s.Contains("NotificationDigestLog")),
                    It.IsAny<Dictionary<string, object?>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task TriggerApprovalReminders_WithValidTenant_CallsApprovalNotificationService()
        {
            // Arrange
            var tenantId = 1;

            _mockApprovalNotification
                .Setup(x => x.SendApprovalRemindersAsync(tenantId, It.IsAny<int>()))
                .ReturnsAsync(5);

            // Act
            await _service.TriggerApprovalReminders(tenantId);

            // Assert
            _mockApprovalNotification.Verify(
                x => x.SendApprovalRemindersAsync(tenantId, It.IsAny<int>()),
                Times.Once);
        }

        [Test]
        public async Task TriggerBudgetThresholdCheck_WithValidTenant_CallsBudgetAlertService()
        {
            // Arrange
            var tenantId = 1;

            _mockBudgetAlert
                .Setup(x => x.CheckAllBudgetsAsync(tenantId))
                .ReturnsAsync(3);

            // Act
            await _service.TriggerBudgetThresholdCheck(tenantId);

            // Assert
            _mockBudgetAlert.Verify(
                x => x.CheckAllBudgetsAsync(tenantId),
                Times.Once);
        }

        [Test]
        public async Task TriggerDigestProcessing_CallsProcessDailyDigests()
        {
            // Arrange
            var usersTable = new DataTable();
            usersTable.Columns.Add("TenantId", typeof(int));
            usersTable.Columns.Add("UserId", typeof(int));
            usersTable.Rows.Add(1, 100);

            var userDataTable = CreateUserDataTable(new[]
            {
                new { Email = "user@test.com", Name = "User" }
            });

            var budgetAlertsTable = new DataTable();
            budgetAlertsTable.Columns.Add("AlertCount", typeof(int));
            budgetAlertsTable.Rows.Add(0);

            var approvalNotifsTable = new DataTable();
            approvalNotifsTable.Columns.Add("NotificationCount", typeof(int));
            approvalNotifsTable.Rows.Add(0);

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<object?>()))
                .ReturnsAsync(usersTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(s => s.Contains("FROM dbo.Users")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(userDataTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(s => s.Contains("BudgetAlerts")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(budgetAlertsTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(s => s.Contains("ApprovalNotifications")),
                    It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(approvalNotifsTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            await _service.TriggerDigestProcessing();

            // Assert - should complete without error
            Assert.Pass();
        }

        private DataTable CreateTenantsTable(dynamic[] tenants)
        {
            var table = new DataTable();
            table.Columns.Add("TenantId", typeof(int));

            foreach (var tenant in tenants)
            {
                table.Rows.Add(tenant.TenantId);
            }

            return table;
        }

        private DataTable CreateUsersTable(dynamic[] users)
        {
            var table = new DataTable();
            table.Columns.Add("TenantId", typeof(int));
            table.Columns.Add("UserId", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Email", typeof(string));

            foreach (var user in users)
            {
                table.Rows.Add(
                    user.TenantId,
                    user.UserId,
                    user.Name,
                    user.Email
                );
            }

            return table;
        }

        private DataTable CreateUserDataTable(dynamic[] users)
        {
            var table = new DataTable();
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("Name", typeof(string));

            foreach (var user in users)
            {
                table.Rows.Add(
                    user.Email,
                    user.Name
                );
            }

            return table;
        }
    }
}
