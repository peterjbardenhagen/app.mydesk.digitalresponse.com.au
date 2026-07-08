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
    /// Integration tests for BudgetAlertService + NotificationService
    /// Tests end-to-end scenario: budget threshold exceeded → alert created → notification sent
    /// Single-user validation (department manager receives notification)
    /// </summary>
    [TestFixture]
    public class BudgetAlertNotificationIntegrationTests
    {
        private BudgetAlertService _budgetService = null!;
        private Mock<DatabaseService> _mockDatabase = null!;
        private Mock<NotificationService> _mockNotification = null!;

        private const int TestTenantId = 1;
        private const int TestDepartmentId = 1;
        private const int TestManagerUserId = 100;
        private const string DepartmentName = "Finance";

        [SetUp]
        public void SetUp()
        {
            _mockDatabase = new Mock<DatabaseService>();
            _mockNotification = new Mock<NotificationService>();
            _budgetService = new BudgetAlertService(
                _mockDatabase.Object,
                _mockNotification.Object,
                logger: null);
        }

        /// <summary>
        /// Test: Budget threshold exceeded → creates alert and notifies manager
        /// Scenario: Department spending reaches 85% of budget, manager receives notification
        /// </summary>
        [Test]
        public async Task CheckBudgetThresholdAsync_At85Percent_CreatesAlertAndNotifiesManager()
        {
            // Arrange
            var budgetTable = CreateBudgetTable(
                budgetId: 1,
                allocated: 100000m,
                spent: 85000m,
                encumbered: 0m,
                threshold: 80,
                allowOverspend: false);

            var departmentTable = CreateDepartmentTable(
                departmentId: TestDepartmentId,
                name: DepartmentName,
                managerUserId: TestManagerUserId);

            var teamsTable = new DataTable();
            teamsTable.Columns.Add("TeamLeadUserId", typeof(int));

            var emptyAlertsTable = new DataTable();
            emptyAlertsTable.Columns.Add("AlertId", typeof(int));

            var alertResultTable = new DataTable();
            alertResultTable.Columns.Add("AlertId", typeof(int));
            alertResultTable.Rows.Add(1001);

            var callLog = new List<string>();

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("DepartmentBudgets")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> _) =>
                {
                    callLog.Add("GetBudget");
                    return Task.FromResult(budgetTable);
                });

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("BudgetAlerts") && sql.Contains("SELECT TOP 1")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> _) =>
                {
                    callLog.Add("CheckRecentAlert");
                    return Task.FromResult(emptyAlertsTable);
                });

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("INSERT INTO BudgetAlerts")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> _) =>
                {
                    callLog.Add("InsertAlert");
                    return Task.FromResult(alertResultTable);
                });

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("Departments") && sql.Contains("ManagerUserId")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> _) =>
                {
                    callLog.Add("GetDepartment");
                    return Task.FromResult(departmentTable);
                });

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("Teams")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> _) =>
                {
                    callLog.Add("GetTeams");
                    return Task.FromResult(teamsTable);
                });

            _mockNotification
                .Setup(x => x.SendNotificationAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .ReturnsAsync(1);

            // Act
            var result = await _budgetService.CheckBudgetThresholdAsync(TestTenantId, TestDepartmentId);

            // Assert
            Assert.IsTrue(result, "Should return true when alert is created");

            // Verify call sequence
            Assert.That(callLog, Contains.Item("GetBudget"));
            Assert.That(callLog, Contains.Item("CheckRecentAlert"));
            Assert.That(callLog, Contains.Item("InsertAlert"));
            Assert.That(callLog, Contains.Item("GetDepartment"));
            Assert.That(callLog, Contains.Item("GetTeams"));

            // Verify manager was notified
            _mockNotification.Verify(
                x => x.SendNotificationAsync(
                    TestTenantId,
                    TestManagerUserId,
                    "BudgetAlert",
                    It.Is<Dictionary<string, object>>(d =>
                        d["DepartmentName"].ToString() == DepartmentName &&
                        d["AlertType"].ToString() == "Threshold"),
                    "Department",
                    TestDepartmentId,
                    It.IsAny<int>()),
                Times.Once);
        }

        /// <summary>
        /// Test: Budget fully exhausted → creates critical alert
        /// Scenario: Department spending reaches 100%, manager receives critical alert
        /// </summary>
        [Test]
        public async Task CheckBudgetThresholdAsync_At100Percent_CreatesCriticalAlert()
        {
            // Arrange
            var budgetTable = CreateBudgetTable(
                budgetId: 2,
                allocated: 50000m,
                spent: 50000m,
                encumbered: 0m,
                threshold: 80,
                allowOverspend: false);

            var departmentTable = CreateDepartmentTable(
                departmentId: TestDepartmentId,
                name: "Operations",
                managerUserId: TestManagerUserId);

            var teamsTable = new DataTable();
            teamsTable.Columns.Add("TeamLeadUserId", typeof(int));

            var emptyAlertsTable = new DataTable();
            emptyAlertsTable.Columns.Add("AlertId", typeof(int));

            var alertResultTable = new DataTable();
            alertResultTable.Columns.Add("AlertId", typeof(int));
            alertResultTable.Rows.Add(1002);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("DepartmentBudgets")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(budgetTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("BudgetAlerts") && sql.Contains("SELECT TOP 1")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(emptyAlertsTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("INSERT INTO BudgetAlerts")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(alertResultTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("Departments")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(departmentTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("Teams")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(teamsTable);

            string capturedAlertType = "";
            string capturedAlertLevel = "";

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("INSERT INTO BudgetAlerts")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> param) =>
                {
                    capturedAlertType = param["Type"].ToString() ?? "";
                    capturedAlertLevel = param["Level"].ToString() ?? "";
                    return Task.FromResult(alertResultTable);
                });

            _mockNotification
                .Setup(x => x.SendNotificationAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .ReturnsAsync(1);

            // Act
            var result = await _budgetService.CheckBudgetThresholdAsync(TestTenantId, TestDepartmentId);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("Full", capturedAlertType);
            Assert.AreEqual("Critical", capturedAlertLevel);

            _mockNotification.Verify(
                x => x.SendNotificationAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    "BudgetAlert",
                    It.Is<Dictionary<string, object>>(d => d["AlertType"].ToString() == "Full"),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Once);
        }

        /// <summary>
        /// Test: Budget below threshold → no alert
        /// Scenario: Department spending at 50%, should not create alert
        /// </summary>
        [Test]
        public async Task CheckBudgetThresholdAsync_Below80Percent_NoAlert()
        {
            // Arrange
            var budgetTable = CreateBudgetTable(
                budgetId: 3,
                allocated: 100000m,
                spent: 50000m,
                encumbered: 0m,
                threshold: 80,
                allowOverspend: false);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(budgetTable);

            // Act
            var result = await _budgetService.CheckBudgetThresholdAsync(TestTenantId, TestDepartmentId);

            // Assert
            Assert.IsFalse(result, "Should not create alert when usage is below threshold");
            _mockNotification.Verify(
                x => x.SendNotificationAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Never);
        }

        /// <summary>
        /// Test: Recent alert exists → skip duplicate notification
        /// Scenario: Alert was sent in last 24 hours, should not send another
        /// </summary>
        [Test]
        public async Task CheckBudgetThresholdAsync_RecentAlertExists_SkipsDuplicate()
        {
            // Arrange
            var budgetTable = CreateBudgetTable(
                budgetId: 4,
                allocated: 100000m,
                spent: 85000m,
                encumbered: 0m,
                threshold: 80,
                allowOverspend: false);

            var recentAlertTable = new DataTable();
            recentAlertTable.Columns.Add("AlertId", typeof(int));
            recentAlertTable.Rows.Add(9001); // Alert exists

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("DepartmentBudgets")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(budgetTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("BudgetAlerts") && sql.Contains("SELECT TOP 1")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(recentAlertTable);

            // Act
            var result = await _budgetService.CheckBudgetThresholdAsync(TestTenantId, TestDepartmentId);

            // Assert
            Assert.IsFalse(result, "Should return false when recent alert already exists");
            _mockNotification.Verify(
                x => x.SendNotificationAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Never);
        }

        /// <summary>
        /// Test: Check all budgets in tenant
        /// Scenario: Tenant has 3 departments, should check all budgets
        /// </summary>
        [Test]
        public async Task CheckAllBudgetsAsync_MultipleDepartments_ChecksEach()
        {
            // Arrange
            var departmentsTable = new DataTable();
            departmentsTable.Columns.Add("DepartmentId", typeof(int));
            departmentsTable.Rows.Add(1);
            departmentsTable.Rows.Add(2);
            departmentsTable.Rows.Add(3);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("SELECT DISTINCT DepartmentId")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(departmentsTable);

            int checkCount = 0;

            // Mock individual budget checks
            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("DepartmentBudgets")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> _) =>
                {
                    checkCount++;
                    // Return empty budget so no alerts created
                    var emptyTable = new DataTable();
                    emptyTable.Columns.Add("BudgetId", typeof(int));
                    return Task.FromResult(emptyTable);
                });

            // Act
            var result = await _budgetService.CheckAllBudgetsAsync(TestTenantId);

            // Assert
            Assert.AreEqual(0, result, "No alerts should be created (empty budgets)");
            Assert.AreEqual(3, checkCount, "Should check all 3 departments");

            _mockDatabase.Verify(
                x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("SELECT DISTINCT DepartmentId")),
                    It.Is<Dictionary<string, object?>>(d => d["TenantId"].Equals(TestTenantId))),
                Times.Once);
        }

        /// <summary>
        /// Test: Acknowledge budget alert
        /// Scenario: Manager marks alert as acknowledged with timestamp and user ID
        /// </summary>
        [Test]
        public async Task AcknowledgeBudgetAlertAsync_WithValidAlert_MarksAcknowledged()
        {
            // Arrange
            var alertId = 1001;
            var acknowledgedByUserId = TestManagerUserId;

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _budgetService.AcknowledgeBudgetAlertAsync(
                TestTenantId, alertId, acknowledgedByUserId);

            // Assert
            Assert.IsTrue(result);

            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("UPDATE BudgetAlerts")),
                    It.Is<Dictionary<string, object?>>(d =>
                        d["AlertId"].Equals(alertId) &&
                        d["UserId"].Equals(acknowledgedByUserId))),
                Times.Once);
        }

        /// <summary>
        /// Test: Get alert history for department
        /// Scenario: Retrieve last 30 days of alerts for a department
        /// </summary>
        [Test]
        public async Task GetBudgetAlertHistoryAsync_WithValidDepartment_ReturnsAlerts()
        {
            // Arrange
            var alertsTable = new DataTable();
            alertsTable.Columns.Add("AlertId", typeof(int));
            alertsTable.Columns.Add("UsagePercentage", typeof(int));
            alertsTable.Columns.Add("SpentAmount", typeof(decimal));
            alertsTable.Columns.Add("AllocatedAmount", typeof(decimal));
            alertsTable.Columns.Add("AlertType", typeof(string));
            alertsTable.Columns.Add("AlertLevel", typeof(string));
            alertsTable.Columns.Add("IsAcknowledged", typeof(bool));
            alertsTable.Columns.Add("CreatedAt", typeof(DateTime));

            alertsTable.Rows.Add(1001, 85, 85000m, 100000m, "Threshold", "Warning", false, DateTime.UtcNow.AddDays(-1));
            alertsTable.Rows.Add(1002, 100, 100000m, 100000m, "Full", "Critical", true, DateTime.UtcNow.AddDays(-5));

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(alertsTable);

            // Act
            var result = await _budgetService.GetBudgetAlertHistoryAsync(TestTenantId, TestDepartmentId);

            // Assert
            Assert.AreEqual(2, result.Rows.Count);
            Assert.AreEqual(1001, result.Rows[0]["AlertId"]);
            Assert.AreEqual("Threshold", result.Rows[0]["AlertType"]);
        }

        // Helper methods
        private DataTable CreateBudgetTable(int budgetId, decimal allocated, decimal spent, decimal encumbered, int threshold, bool allowOverspend)
        {
            var table = new DataTable();
            table.Columns.Add("BudgetId", typeof(int));
            table.Columns.Add("AllocatedAmount", typeof(decimal));
            table.Columns.Add("SpentAmount", typeof(decimal));
            table.Columns.Add("EncumberedAmount", typeof(decimal));
            table.Columns.Add("ThresholdAlertPercentage", typeof(int));
            table.Columns.Add("AllowOverspend", typeof(bool));
            table.Rows.Add(budgetId, allocated, spent, encumbered, threshold, allowOverspend);
            return table;
        }

        private DataTable CreateDepartmentTable(int departmentId, string name, int managerUserId)
        {
            var table = new DataTable();
            table.Columns.Add("DepartmentId", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("ManagerUserId", typeof(int));
            table.Rows.Add(departmentId, name, managerUserId);
            return table;
        }
    }
}
