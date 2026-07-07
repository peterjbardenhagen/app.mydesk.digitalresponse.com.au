using System.Data;
using Moq;
using NUnit.Framework;
using MyDesk.Web.Services;

namespace MyDesk.Web.Phase4.Tests.Services
{
    [TestFixture]
    public class BudgetServiceTests
    {
        private BudgetService _service = null!;
        private Mock<DatabaseService> _mockDatabase = null!;

        [SetUp]
        public void SetUp()
        {
            _mockDatabase = new Mock<DatabaseService>();
            _service = new BudgetService(_mockDatabase.Object);
        }

        [Test]
        public async Task GetBudgetAsync_WithValidDepartmentAndYear_ReturnsBudget()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var fiscalYear = 2026;
            var expectedTable = CreateBudgetTable(new[]
            {
                new { BudgetId = 1, DepartmentId = 1, FiscalYear = 2026, AllocatedAmount = 100000m, SpentAmount = 25000m }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetBudgetAsync(tenantId, departmentId, fiscalYear);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Rows.Count);
            Assert.AreEqual(100000m, result.Rows[0]["AllocatedAmount"]);
        }

        [Test]
        public async Task CreateBudgetAsync_WithValidData_InsertsBudget()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var fiscalYear = 2026;
            var allocatedAmount = 150000m;
            var allowOverspend = false;
            var thresholdPercent = 80;

            _mockDatabase
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateBudgetAsync(
                tenantId, departmentId, fiscalYear, allocatedAmount, allowOverspend, thresholdPercent);

            // Assert
            _mockDatabase.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()), Times.Once);
        }

        [Test]
        public async Task CanApproveAsync_WhenBudgetAvailable_ReturnsTrue()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var amount = 10000m;
            var expectedTable = CreateBudgetTable(new[]
            {
                new { BudgetId = 1, DepartmentId = 1, FiscalYear = 2026, AllocatedAmount = 100000m, SpentAmount = 25000m, EncumberedAmount = 10000m }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.CanApproveAsync(tenantId, departmentId, amount);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task CanApproveAsync_WhenBudgetExceeded_ReturnsFalse()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var amount = 70000m;
            var expectedTable = CreateBudgetTable(new[]
            {
                new { BudgetId = 1, DepartmentId = 1, FiscalYear = 2026, AllocatedAmount = 100000m, SpentAmount = 80000m, EncumberedAmount = 0m, AllowOverspend = false }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.CanApproveAsync(tenantId, departmentId, amount);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task GetRemainingBudgetAsync_WithValidBudget_CalculatesCorrectly()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var expectedTable = CreateBudgetTable(new[]
            {
                new { BudgetId = 1, DepartmentId = 1, FiscalYear = 2026, AllocatedAmount = 100000m, SpentAmount = 30000m, EncumberedAmount = 10000m }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetRemainingBudgetAsync(tenantId, departmentId);

            // Assert
            // Expected: 100000 - 30000 - 10000 = 60000
            Assert.AreEqual(60000m, result);
        }

        [Test]
        public async Task GetBudgetAlertPercentageAsync_WithValidBudget_CalculatesCorrectly()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var expectedTable = CreateBudgetTable(new[]
            {
                new { BudgetId = 1, DepartmentId = 1, FiscalYear = 2026, AllocatedAmount = 100000m, SpentAmount = 50000m }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetBudgetAlertPercentageAsync(tenantId, departmentId);

            // Assert
            // Expected: (50000 / 100000) * 100 = 50%
            Assert.AreEqual(50.0, result);
        }

        [Test]
        public async Task GetDepartmentBudgetsAsync_WithValidTenantId_ReturnsBudgets()
        {
            // Arrange
            var tenantId = 1;
            var expectedTable = CreateBudgetTable(new[]
            {
                new { BudgetId = 1, DepartmentId = 1, FiscalYear = 2026, AllocatedAmount = 100000m, SpentAmount = 25000m },
                new { BudgetId = 2, DepartmentId = 2, FiscalYear = 2026, AllocatedAmount = 50000m, SpentAmount = 10000m }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetDepartmentBudgetsAsync(tenantId, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Rows.Count);
        }

        [Test]
        public async Task AddExpenseAsync_WithValidAmount_UpdatesBudgetSpentAmount()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var fiscalYear = 2026;
            var amount = 5000m;
            var category = "Meals";

            var budgetTable = CreateBudgetTable(new[]
            {
                new { BudgetId = 1, DepartmentId = 1, FiscalYear = 2026, AllocatedAmount = 100000m, SpentAmount = 0m, EncumberedAmount = 0m }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(budgetTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            await _service.AddExpenseAsync(tenantId, departmentId, fiscalYear, amount, category);

            // Assert
            _mockDatabase.Verify(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()), Times.Once);
        }

        private DataTable CreateBudgetTable(dynamic[] budgets)
        {
            var table = new DataTable();
            table.Columns.Add("BudgetId", typeof(int));
            table.Columns.Add("TenantId", typeof(int));
            table.Columns.Add("DepartmentId", typeof(int));
            table.Columns.Add("FiscalYear", typeof(int));
            table.Columns.Add("AllocatedAmount", typeof(decimal));
            table.Columns.Add("SpentAmount", typeof(decimal));
            table.Columns.Add("EncumberedAmount", typeof(decimal));
            table.Columns.Add("AllowOverspend", typeof(bool));
            table.Columns.Add("ThresholdAlertPercentage", typeof(int));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("UpdatedAt", typeof(DateTime));

            foreach (var budget in budgets)
            {
                table.Rows.Add(
                    budget.BudgetId,
                    1,
                    budget.DepartmentId,
                    budget.FiscalYear,
                    budget.AllocatedAmount,
                    budget.SpentAmount,
                    budget.EncumberedAmount ?? 0m,
                    budget.AllowOverspend ?? false,
                    80,
                    "Active",
                    DateTime.UtcNow,
                    DateTime.UtcNow
                );
            }

            return table;
        }
    }
}
