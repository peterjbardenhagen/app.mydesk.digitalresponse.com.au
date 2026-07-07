using System.Data;
using Moq;
using NUnit.Framework;
using MyDesk.Web.Services;

namespace MyDesk.Web.Phase4.Tests.Services
{
    [TestFixture]
    public class DepartmentServiceTests
    {
        private DepartmentService _service = null!;
        private Mock<DatabaseService> _mockDatabase = null!;

        [SetUp]
        public void SetUp()
        {
            _mockDatabase = new Mock<DatabaseService>();
            _service = new DepartmentService(_mockDatabase.Object);
        }

        [Test]
        public async Task GetDepartmentsAsync_WithValidTenantId_ReturnsDepartments()
        {
            // Arrange
            var tenantId = 1;
            var expectedTable = CreateDepartmentTable(new[]
            {
                new { DepartmentId = 1, Name = "Finance", TenantId = 1, Status = "Active" },
                new { DepartmentId = 2, Name = "HR", TenantId = 1, Status = "Active" }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetDepartmentsAsync(tenantId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Rows.Count);
            Assert.AreEqual("Finance", result.Rows[0]["Name"]);
            Assert.AreEqual("HR", result.Rows[1]["Name"]);
        }

        [Test]
        public async Task GetDepartmentAsync_WithValidId_ReturnsDepartment()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var expectedTable = CreateDepartmentTable(new[]
            {
                new { DepartmentId = 1, Name = "Finance", TenantId = 1, Status = "Active", Description = "Finance Department" }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetDepartmentAsync(tenantId, departmentId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Rows.Count);
            Assert.AreEqual("Finance", result.Rows[0]["Name"]);
        }

        [Test]
        public async Task CreateDepartmentAsync_WithValidData_InsertsAndReturnsDepartment()
        {
            // Arrange
            var tenantId = 1;
            var name = "Engineering";
            var description = "Engineering Department";
            var managerId = 100;
            var costCenter = "ENG001";

            var expectedTable = CreateDepartmentTable(new[]
            {
                new { DepartmentId = 3, Name = name, TenantId = tenantId, Status = "Active", Description = description, ManagerUserId = managerId, CostCenter = costCenter }
            });

            _mockDatabase
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.CreateDepartmentAsync(tenantId, name, description, managerId, costCenter);

            // Assert
            Assert.IsNotNull(result);
            _mockDatabase.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()), Times.Once);
        }

        [Test]
        public async Task UpdateDepartmentAsync_WithValidData_UpdatesDepartment()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;
            var name = "Finance Updated";
            var description = "Updated Finance Department";

            _mockDatabase
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateDepartmentAsync(tenantId, departmentId, name, description);

            // Assert
            _mockDatabase.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()), Times.Once);
        }

        [Test]
        public async Task DeleteDepartmentAsync_WithValidId_ArchivesDepartment()
        {
            // Arrange
            var tenantId = 1;
            var departmentId = 1;

            _mockDatabase
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.DeleteDepartmentAsync(tenantId, departmentId);

            // Assert
            _mockDatabase.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()), Times.Once);
        }

        [Test]
        public async Task GetDepartmentsAsync_WithNonexistentTenant_ReturnsEmptyTable()
        {
            // Arrange
            var tenantId = 999;
            var emptyTable = new DataTable();
            emptyTable.Columns.Add("DepartmentId", typeof(int));
            emptyTable.Columns.Add("Name", typeof(string));

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(emptyTable);

            // Act
            var result = await _service.GetDepartmentsAsync(tenantId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Rows.Count);
        }

        private DataTable CreateDepartmentTable(dynamic[] departments)
        {
            var table = new DataTable();
            table.Columns.Add("DepartmentId", typeof(int));
            table.Columns.Add("TenantId", typeof(int));
            table.Columns.Add("ParentDepartmentId", typeof(int?));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("ManagerUserId", typeof(int?));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("CostCenter", typeof(string));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("UpdatedAt", typeof(DateTime));

            foreach (var dept in departments)
            {
                table.Rows.Add(
                    dept.DepartmentId,
                    dept.TenantId,
                    null,
                    dept.Name,
                    dept.Description ?? "",
                    dept.ManagerUserId ?? DBNull.Value,
                    dept.Status,
                    dept.CostCenter ?? "",
                    DateTime.UtcNow,
                    DateTime.UtcNow
                );
            }

            return table;
        }
    }
}
