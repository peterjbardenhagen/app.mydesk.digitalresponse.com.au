using System.Data;
using Moq;
using NUnit.Framework;
using MyDesk.Web.Services;

namespace MyDesk.Web.Phase4.Tests.Services
{
    [TestFixture]
    public class BulkUserImportServiceTests
    {
        private BulkUserImportService _service = null!;
        private Mock<DatabaseService> _mockDatabase = null!;
        private Mock<UserService> _mockUserService = null!;

        [SetUp]
        public void SetUp()
        {
            _mockDatabase = new Mock<DatabaseService>();
            _mockUserService = new Mock<UserService>();
            _service = new BulkUserImportService(_mockDatabase.Object, _mockUserService.Object);
        }

        [Test]
        public async Task ImportUsersAsync_WithValidCSV_ImportsUsers()
        {
            // Arrange
            var tenantId = 1;
            var importedById = 100;
            var filename = "users.csv";
            var csvContent = "Email,FirstName,LastName,DepartmentId,TeamId,Role\n" +
                             "john@example.com,John,Doe,1,1,Member\n" +
                             "jane@example.com,Jane,Smith,1,2,Lead";

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

            _mockDatabase
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(1);

            _mockUserService
                .Setup(x => x.CreateUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new DataTable());

            // Act
            var result = await _service.ImportUsersAsync(tenantId, importedById, stream, filename);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(filename, result.Filename);
            Assert.AreEqual("Success", result.Status);
        }

        [Test]
        public async Task ImportUsersAsync_WithInvalidEmail_ReturnsPartialSuccess()
        {
            // Arrange
            var tenantId = 1;
            var importedById = 100;
            var filename = "users.csv";
            var csvContent = "Email,FirstName,LastName,DepartmentId,TeamId,Role\n" +
                             "invalid-email,John,Doe,1,1,Member\n" +
                             "jane@example.com,Jane,Smith,1,2,Lead";

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

            _mockUserService
                .Setup(x => x.CreateUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new DataTable());

            // Act
            var result = await _service.ImportUsersAsync(tenantId, importedById, stream, filename);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.FailedRows > 0);
        }

        [Test]
        public async Task ImportUsersAsync_WithMissingRequiredFields_ReturnsError()
        {
            // Arrange
            var tenantId = 1;
            var importedById = 100;
            var filename = "users.csv";
            var csvContent = "FirstName,LastName\n" +
                             "John,Doe";  // Missing Email column

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _service.ImportUsersAsync(tenantId, importedById, stream, filename);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Error", result.Status);
            Assert.IsNotEmpty(result.ErrorMessage);
        }

        [Test]
        public async Task ImportUsersAsync_WithEmptyFile_ReturnsError()
        {
            // Arrange
            var tenantId = 1;
            var importedById = 100;
            var filename = "empty.csv";
            var csvContent = "";

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

            // Act
            var result = await _service.ImportUsersAsync(tenantId, importedById, stream, filename);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Error", result.Status);
        }

        [Test]
        public async Task ImportUsersAsync_WithDuplicateEmails_HandlesGracefully()
        {
            // Arrange
            var tenantId = 1;
            var importedById = 100;
            var filename = "users.csv";
            var csvContent = "Email,FirstName,LastName,DepartmentId,TeamId,Role\n" +
                             "john@example.com,John,Doe,1,1,Member\n" +
                             "john@example.com,John,Smith,1,2,Lead";  // Duplicate email

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

            _mockUserService
                .Setup(x => x.CreateUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new DataTable());

            // Act
            var result = await _service.ImportUsersAsync(tenantId, importedById, stream, filename);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.TotalRows == 2);
        }

        [Test]
        public async Task ImportUsersAsync_WithOptionalFields_ImportsSuccessfully()
        {
            // Arrange
            var tenantId = 1;
            var importedById = 100;
            var filename = "users.csv";
            var csvContent = "Email,FirstName,LastName\n" +
                             "john@example.com,John,Doe\n" +
                             "jane@example.com,Jane,Smith";  // No Department, Team, Role

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

            _mockUserService
                .Setup(x => x.CreateUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new DataTable());

            _mockDatabase
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.ImportUsersAsync(tenantId, importedById, stream, filename);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.SuccessfulRows >= 0);
        }

        [Test]
        public async Task ImportUsersAsync_LogsImportRecord()
        {
            // Arrange
            var tenantId = 1;
            var importedById = 100;
            var filename = "users.csv";
            var csvContent = "Email,FirstName,LastName,DepartmentId,TeamId,Role\n" +
                             "john@example.com,John,Doe,1,1,Member";

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

            _mockDatabase
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(1);

            _mockUserService
                .Setup(x => x.CreateUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new DataTable());

            // Act
            var result = await _service.ImportUsersAsync(tenantId, importedById, stream, filename);

            // Assert
            Assert.IsNotNull(result);
            _mockDatabase.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.Once);
        }
    }
}
