using System;
using System.Collections.Generic;
using System.Data;
using Moq;
using NUnit.Framework;
using MyDesk.Web.Services;

namespace MyDesk.Web.Phase4.Tests.Services
{
    [TestFixture]
    public class BudgetAlertServiceTests
    {
        private BudgetAlertService _service = null!;
        private Mock<DatabaseService> _mockDatabase = null!;
        private Mock<NotificationService> _mockNotification = null!;

        [SetUp]
        public void SetUp()
        {
            _mockDatabase = new Mock<DatabaseService>();
            _mockNotification = new Mock<NotificationService>();
            _service = new BudgetAlertService(
                _mockDatabase.Object,
                _mockNotification.Object,
                null);
        }

        [Test]
        public async Task CheckBudgetThresholdAsync_WhenUsageAbove80Percent_CreatesAlert()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var budgetTable = CreateBudgetTable(new[]
            {
                new { BudgetId = 1, AllocatedAmount = 100000m, SpentAmount = 85000m, EncumberedAmount = 0m, ThresholdAlertPercentage = 80, AllowOverspend = false }
            });

            var deptTable = CreateDepartmentTable(new[]
            {
                new { DepartmentId = 1, Name = "Finance", ManagerUserId = 100 }
            });

            var emptyAlerts = new DataTable();
            emptyAlerts.Columns.Add("AlertId", typeof(int));

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(budgetTable)
                .Returns((string sql, Dictionary<string, object> _) =>
                {
                    if (sql.Contains("BudgetAlerts") && sql.Contains("SELECT TOP 1"))
                        return Task.FromResult(emptyAlerts);
                    if (sql.Contains("Departments"))
                        return Task.FromResult(deptTable);
                    if (sql.Contains("Teams"))
                        return Task.FromResult(new DataTable());
                    return Task.FromResult(budgetTable);
                });

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CheckBudgetThresholdAsync(tenantId, departmentId);

            // Assert
            Assert.IsTrue(result);
            _mockDatabase.Verify(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.AtLeastOnce);
        }

        [Test]
        public async Task CheckBudgetThresholdAsync_WhenUsageBelow80Percent_NoAlert()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var budgetTable = CreateBudgetTable(new[]
            {
                new { BudgetId = 1, AllocatedAmount = 100000m, SpentAmount = 50000m, EncumberedAmount = 0m, ThresholdAlertPercentage = 80, AllowOverspend = false }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(budgetTable);

            // Act
            var result = await _service.CheckBudgetThresholdAsync(tenantId, departmentId);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task CheckBudgetThresholdAsync_WhenBudgetFull_CreatesCriticalAlert()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var budgetTable = CreateBudgetTable(new[]
            {
                new { BudgetId = 1, AllocatedAmount = 100000m, SpentAmount = 100000m, EncumberedAmount = 0m, ThresholdAlertPercentage = 80, AllowOverspend = false }
            });

            var deptTable = CreateDepartmentTable(new[]
            {
                new { DepartmentId = 1, Name = "Finance", ManagerUserId = 100 }
            });

            var emptyAlerts = new DataTable();
            emptyAlerts.Columns.Add("AlertId", typeof(int));

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Returns((string sql, Dictionary<string, object> _) =>
                {
                    if (sql.Contains("BudgetAlerts") && sql.Contains("SELECT TOP 1"))
                        return Task.FromResult(emptyAlerts);
                    if (sql.Contains("Departments"))
                        return Task.FromResult(deptTable);
                    if (sql.Contains("Teams"))
                        return Task.FromResult(new DataTable());
                    return Task.FromResult(budgetTable);
                });

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CheckBudgetThresholdAsync(tenantId, departmentId);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task CheckBudgetThresholdAsync_WithNoBudget_ReturnsFalse()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var emptyTable = new DataTable();
            emptyTable.Columns.Add("BudgetId", typeof(int));

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(emptyTable);

            // Act
            var result = await _service.CheckBudgetThresholdAsync(tenantId, departmentId);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task AcknowledgeBudgetAlertAsync_WithValidAlert_AcknowledgesIt()
        {
            // Arrange
            var tenantId = 1;
            var alertId = 1;
            var userId = 100;

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.AcknowledgeBudgetAlertAsync(tenantId, alertId, userId);

            // Assert
            Assert.IsTrue(result);
            _mockDatabase.Verify(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Test]
        public async Task GetBudgetAlertHistoryAsync_WithValidDepartment_ReturnsAlerts()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var expectedTable = CreateAlertTable(new[]
            {
                new { AlertId = 1, UsagePercentage = 85, AlertType = "Threshold" },
                new { AlertId = 2, UsagePercentage = 100, AlertType = "Full" }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetBudgetAlertHistoryAsync(tenantId, departmentId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Rows.Count);
        }

        [Test]
        public async Task GetUnacknowledgedAlertsAsync_WithValidUser_ReturnsUnacknowledgedAlerts()
        {
            // Arrange
            var tenantId = 1;
            var userId = 100;
            var expectedTable = CreateAlertTable(new[]
            {
                new { AlertId = 1, UsagePercentage = 85, AlertType = "Threshold" }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetUnacknowledgedAlertsAsync(tenantId, userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Rows.Count > 0);
        }

        [Test]
        public async Task CheckAllBudgetsAsync_ChecksAllActiveBudgets()
        {
            // Arrange
            var tenantId = 1;
            var budgetsTable = new DataTable();
            budgetsTable.Columns.Add("DepartmentId", typeof(int));
            budgetsTable.Rows.Add(1);
            budgetsTable.Rows.Add(2);

            var emptyBudgetTable = new DataTable();
            emptyBudgetTable.Columns.Add("BudgetId", typeof(int));

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Returns((string sql, Dictionary<string, object> _) =>
                {
                    if (sql.Contains("SELECT DISTINCT DepartmentId"))
                        return Task.FromResult(budgetsTable);
                    return Task.FromResult(emptyBudgetTable);
                });

            // Act
            var result = await _service.CheckAllBudgetsAsync(tenantId);

            // Assert
            Assert.AreEqual(0, result);  // No alerts created (budgets don't exist)
        }

        private DataTable CreateBudgetTable(dynamic[] budgets)
        {
            var table = new DataTable();
            table.Columns.Add("BudgetId", typeof(int));
            table.Columns.Add("AllocatedAmount", typeof(decimal));
            table.Columns.Add("SpentAmount", typeof(decimal));
            table.Columns.Add("EncumberedAmount", typeof(decimal));
            table.Columns.Add("ThresholdAlertPercentage", typeof(int));
            table.Columns.Add("AllowOverspend", typeof(bool));

            foreach (var budget in budgets)
            {
                table.Rows.Add(
                    budget.BudgetId,
                    budget.AllocatedAmount,
                    budget.SpentAmount,
                    budget.EncumberedAmount,
                    budget.ThresholdAlertPercentage,
                    budget.AllowOverspend
                );
            }

            return table;
        }

        private DataTable CreateDepartmentTable(dynamic[] departments)
        {
            var table = new DataTable();
            table.Columns.Add("DepartmentId", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("ManagerUserId", typeof(int));

            foreach (var dept in departments)
            {
                table.Rows.Add(
                    dept.DepartmentId,
                    dept.Name,
                    dept.ManagerUserId ?? DBNull.Value
                );
            }

            return table;
        }

        private DataTable CreateAlertTable(dynamic[] alerts)
        {
            var table = new DataTable();
            table.Columns.Add("AlertId", typeof(int));
            table.Columns.Add("UsagePercentage", typeof(int));
            table.Columns.Add("AlertType", typeof(string));
            table.Columns.Add("DepartmentName", typeof(string));
            table.Columns.Add("AlertLevel", typeof(string));
            table.Columns.Add("CreatedAt", typeof(DateTime));

            foreach (var alert in alerts)
            {
                table.Rows.Add(
                    alert.AlertId,
                    alert.UsagePercentage,
                    alert.AlertType,
                    "Test Department",
                    alert.AlertType == "Full" ? "Critical" : "Warning",
                    DateTime.UtcNow
                );
            }

            return table;
        }
    }
}
