using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using MyDesk.Web.Services;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Phase5.Tests.Services
{
    /// <summary>
    /// Integration tests for NotificationBackgroundJobService
    /// Tests Hangfire job execution: approval reminders, budget checks, daily digests
    /// Single-tenant validation with 1 user per scenario
    /// </summary>
    [TestFixture]
    public class NotificationBackgroundJobServiceIntegrationTests
    {
        private NotificationBackgroundJobService _service = null!;
        private Mock<IServiceProvider> _mockServiceProvider = null!;
        private Mock<DatabaseService> _mockDatabase = null!;
        private Mock<ApprovalNotificationService> _mockApprovalNotification = null!;
        private Mock<BudgetAlertService> _mockBudgetAlert = null!;

        private const int TestTenantId = 1;

        [SetUp]
        public void SetUp()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockDatabase = new Mock<DatabaseService>();
            _mockApprovalNotification = new Mock<ApprovalNotificationService>();
            _mockBudgetAlert = new Mock<BudgetAlertService>();
            _service = new NotificationBackgroundJobService(_mockServiceProvider.Object, logger: null);
        }

        /// <summary>
        /// Test: Approval reminders job execution
        /// Scenario: Job retrieves active tenants and sends reminders for pending approvals > 3 days old
        /// </summary>
        [Test]
        public async Task SendApprovalReminders_WithPendingApprovals_SendsReminders()
        {
            // Arrange
            var tenantsTable = CreateTenantTable(new[] { 1, 2 });

            var mockScope = new Mock<IServiceScope>();
            var mockScopeProvider = new Mock<IServiceProvider>();

            mockScope.Setup(x => x.ServiceProvider).Returns(mockScopeProvider.Object);

            _mockServiceProvider
                .Setup(x => x.CreateScope())
                .Returns(mockScope.Object);

            mockScopeProvider
                .Setup(x => x.GetService(typeof(DatabaseService)))
                .Returns(_mockDatabase.Object);

            mockScopeProvider
                .Setup(x => x.GetService(typeof(ApprovalNotificationService)))
                .Returns(_mockApprovalNotification.Object);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("Tenants")),
                    null))
                .ReturnsAsync(tenantsTable);

            _mockApprovalNotification
                .Setup(x => x.SendApprovalRemindersAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(2);

            // Act
            await _service.SendApprovalReminders();

            // Assert
            _mockDatabase.Verify(
                x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("TenantId") && sql.Contains("IsActive")),
                    null),
                Times.Once);

            _mockApprovalNotification.Verify(
                x => x.SendApprovalRemindersAsync(1, 3),
                Times.Once);

            _mockApprovalNotification.Verify(
                x => x.SendApprovalRemindersAsync(2, 3),
                Times.Once);

            mockScope.Verify(x => x.Dispose(), Times.Once);
        }

        /// <summary>
        /// Test: Budget threshold check job execution
        /// Scenario: Job checks all department budgets across all tenants
        /// </summary>
        [Test]
        public async Task CheckAllBudgetThresholds_WithActiveBudgets_ChecksEach()
        {
            // Arrange
            var tenantsTable = CreateTenantTable(new[] { 1 });

            var mockScope = new Mock<IServiceScope>();
            var mockScopeProvider = new Mock<IServiceProvider>();

            mockScope.Setup(x => x.ServiceProvider).Returns(mockScopeProvider.Object);

            _mockServiceProvider
                .Setup(x => x.CreateScope())
                .Returns(mockScope.Object);

            mockScopeProvider
                .Setup(x => x.GetService(typeof(DatabaseService)))
                .Returns(_mockDatabase.Object);

            mockScopeProvider
                .Setup(x => x.GetService(typeof(BudgetAlertService)))
                .Returns(_mockBudgetAlert.Object);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("Tenants")),
                    null))
                .ReturnsAsync(tenantsTable);

            _mockBudgetAlert
                .Setup(x => x.CheckAllBudgetsAsync(It.IsAny<int>()))
                .ReturnsAsync(1);

            // Act
            await _service.CheckAllBudgetThresholds();

            // Assert
            _mockBudgetAlert.Verify(
                x => x.CheckAllBudgetsAsync(1),
                Times.Once);

            mockScope.Verify(x => x.Dispose(), Times.Once);
        }

        /// <summary>
        /// Test: Daily digest processing
        /// Scenario: Process digests for users with digest enabled
        /// </summary>
        [Test]
        public async Task ProcessDailyDigests_WithEnabledUsers_ProcessesEach()
        {
            // Arrange
            var usersTable = new DataTable();
            usersTable.Columns.Add("TenantId", typeof(int));
            usersTable.Columns.Add("UserId", typeof(int));
            usersTable.Rows.Add(1, 100);

            var mockScope = new Mock<IServiceScope>();
            var mockScopeProvider = new Mock<IServiceProvider>();

            mockScope.Setup(x => x.ServiceProvider).Returns(mockScopeProvider.Object);

            _mockServiceProvider
                .Setup(x => x.CreateScope())
                .Returns(mockScope.Object);

            mockScopeProvider
                .Setup(x => x.GetService(typeof(DatabaseService)))
                .Returns(_mockDatabase.Object);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationSettings")),
                    null))
                .ReturnsAsync(usersTable);

            var userDetailsTable = new DataTable();
            userDetailsTable.Columns.Add("Email", typeof(string));
            userDetailsTable.Columns.Add("Name", typeof(string));
            userDetailsTable.Rows.Add("user@company.com", "John User");

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("dbo.Users")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(userDetailsTable);

            var alertCountTable = new DataTable();
            alertCountTable.Columns.Add("AlertCount", typeof(int));
            alertCountTable.Rows.Add(3);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("BudgetAlerts")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(alertCountTable);

            var approvalCountTable = new DataTable();
            approvalCountTable.Columns.Add("NotificationCount", typeof(int));
            approvalCountTable.Rows.Add(2);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("ApprovalNotifications")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(approvalCountTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            await _service.ProcessDailyDigests();

            // Assert
            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationDigestLog")),
                    It.Is<Dictionary<string, object?>>(d =>
                        d["BudgetCount"].Equals(3) &&
                        d["ApprovalCount"].Equals(2))),
                Times.Once);

            mockScope.Verify(x => x.Dispose(), Times.Once);
        }

        /// <summary>
        /// Test: Digest processing for single user with no alerts
        /// Scenario: User has digest enabled but no new alerts/approvals
        /// </summary>
        [Test]
        public async Task ProcessDailyDigests_WithNoAlerts_StillLogsDigest()
        {
            // Arrange
            var usersTable = new DataTable();
            usersTable.Columns.Add("TenantId", typeof(int));
            usersTable.Columns.Add("UserId", typeof(int));
            usersTable.Rows.Add(1, 101);

            var mockScope = new Mock<IServiceScope>();
            var mockScopeProvider = new Mock<IServiceProvider>();

            mockScope.Setup(x => x.ServiceProvider).Returns(mockScopeProvider.Object);

            _mockServiceProvider
                .Setup(x => x.CreateScope())
                .Returns(mockScope.Object);

            mockScopeProvider
                .Setup(x => x.GetService(typeof(DatabaseService)))
                .Returns(_mockDatabase.Object);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationSettings")),
                    null))
                .ReturnsAsync(usersTable);

            var userDetailsTable = new DataTable();
            userDetailsTable.Columns.Add("Email", typeof(string));
            userDetailsTable.Columns.Add("Name", typeof(string));
            userDetailsTable.Rows.Add("user@company.com", "Jane User");

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("dbo.Users")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(userDetailsTable);

            var emptyCountTable = new DataTable();
            emptyCountTable.Columns.Add("AlertCount", typeof(int));
            emptyCountTable.Rows.Add(0);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("BudgetAlerts") || sql.Contains("ApprovalNotifications")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(emptyCountTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            await _service.ProcessDailyDigests();

            // Assert
            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationDigestLog")),
                    It.Is<Dictionary<string, object?>>(d =>
                        d["BudgetCount"].Equals(0) &&
                        d["ApprovalCount"].Equals(0))),
                Times.Once);
        }

        /// <summary>
        /// Test: Manual trigger for approval reminders
        /// Scenario: Admin manually triggers reminder job for specific tenant
        /// </summary>
        [Test]
        public async Task TriggerApprovalReminders_WithTenantId_SendsReminders()
        {
            // Arrange
            var mockScope = new Mock<IServiceScope>();
            var mockScopeProvider = new Mock<IServiceProvider>();

            mockScope.Setup(x => x.ServiceProvider).Returns(mockScopeProvider.Object);

            _mockServiceProvider
                .Setup(x => x.CreateScope())
                .Returns(mockScope.Object);

            mockScopeProvider
                .Setup(x => x.GetService(typeof(ApprovalNotificationService)))
                .Returns(_mockApprovalNotification.Object);

            _mockApprovalNotification
                .Setup(x => x.SendApprovalRemindersAsync(TestTenantId))
                .ReturnsAsync(1);

            // Act
            await _service.TriggerApprovalReminders(TestTenantId);

            // Assert
            _mockApprovalNotification.Verify(
                x => x.SendApprovalRemindersAsync(TestTenantId),
                Times.Once);

            mockScope.Verify(x => x.Dispose(), Times.Once);
        }

        /// <summary>
        /// Test: Manual trigger for budget check
        /// Scenario: Admin manually triggers budget check for specific tenant
        /// </summary>
        [Test]
        public async Task TriggerBudgetThresholdCheck_WithTenantId_ChecksBudgets()
        {
            // Arrange
            var mockScope = new Mock<IServiceScope>();
            var mockScopeProvider = new Mock<IServiceProvider>();

            mockScope.Setup(x => x.ServiceProvider).Returns(mockScopeProvider.Object);

            _mockServiceProvider
                .Setup(x => x.CreateScope())
                .Returns(mockScope.Object);

            mockScopeProvider
                .Setup(x => x.GetService(typeof(BudgetAlertService)))
                .Returns(_mockBudgetAlert.Object);

            _mockBudgetAlert
                .Setup(x => x.CheckAllBudgetsAsync(TestTenantId))
                .ReturnsAsync(2);

            // Act
            await _service.TriggerBudgetThresholdCheck(TestTenantId);

            // Assert
            _mockBudgetAlert.Verify(
                x => x.CheckAllBudgetsAsync(TestTenantId),
                Times.Once);

            mockScope.Verify(x => x.Dispose(), Times.Once);
        }

        /// <summary>
        /// Test: Manual trigger for digest processing
        /// Scenario: Admin manually triggers digest processing job
        /// </summary>
        [Test]
        public async Task TriggerDigestProcessing_ExecutesProcessing()
        {
            // Arrange
            var usersTable = new DataTable();
            usersTable.Columns.Add("TenantId", typeof(int));
            usersTable.Columns.Add("UserId", typeof(int));
            usersTable.Rows.Add(1, 102);

            var mockScope = new Mock<IServiceScope>();
            var mockScopeProvider = new Mock<IServiceProvider>();

            mockScope.Setup(x => x.ServiceProvider).Returns(mockScopeProvider.Object);

            _mockServiceProvider
                .Setup(x => x.CreateScope())
                .Returns(mockScope.Object);

            mockScopeProvider
                .Setup(x => x.GetService(typeof(DatabaseService)))
                .Returns(_mockDatabase.Object);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationSettings")),
                    null))
                .ReturnsAsync(usersTable);

            var userDetailsTable = new DataTable();
            userDetailsTable.Columns.Add("Email", typeof(string));
            userDetailsTable.Columns.Add("Name", typeof(string));
            userDetailsTable.Rows.Add("admin@company.com", "Admin User");

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("dbo.Users")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(userDetailsTable);

            var countTable = new DataTable();
            countTable.Columns.Add("AlertCount", typeof(int));
            countTable.Rows.Add(1);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("BudgetAlerts") || sql.Contains("ApprovalNotifications")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(countTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            await _service.TriggerDigestProcessing();

            // Assert
            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationDigestLog")),
                    It.IsAny<Dictionary<string, object?>>()),
                Times.Once);

            mockScope.Verify(x => x.Dispose(), Times.Once);
        }

        // Helper methods
        private DataTable CreateTenantTable(int[] tenantIds)
        {
            var table = new DataTable();
            table.Columns.Add("TenantId", typeof(int));
            foreach (var id in tenantIds)
            {
                table.Rows.Add(id);
            }
            return table;
        }
    }
}
