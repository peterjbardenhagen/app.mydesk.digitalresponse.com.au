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
    /// Integration tests for Phase 5 Notifications & Alerts
    /// Tests full notification pipeline: preference check → template retrieval → database insertion → queue creation
    /// Single-user validation per platform (email, in-app)
    /// </summary>
    [TestFixture]
    public class NotificationServiceIntegrationTests
    {
        private NotificationService _service = null!;
        private Mock<DatabaseService> _mockDatabase = null!;

        private const int TestTenantId = 1;
        private const int TestUserId = 100;
        private const string TestEmail = "user@company.com";
        private const string TestUserName = "John Manager";

        [SetUp]
        public void SetUp()
        {
            _mockDatabase = new Mock<DatabaseService>();
            _service = new NotificationService(_mockDatabase.Object, logger: null);
        }

        /// <summary>
        /// Test: Email notification delivery pipeline
        /// Scenario: User has email enabled, template exists, should queue email and log notification
        /// </summary>
        [Test]
        public async Task SendNotificationAsync_EmailEnabled_QueuesEmailAndCreatesLog()
        {
            // Arrange
            var prefTable = CreatePreferencesTable(enableEmail: true, enableInApp: false);
            var userTable = CreateUserTable();
            var templateTable = CreateTemplateTable("BudgetAlert");

            var logResultTable = new DataTable();
            logResultTable.Columns.Add("NotificationId", typeof(int));
            logResultTable.Rows.Add(1001);

            var callOrder = new List<string>();

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationSettings")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> _) =>
                {
                    callOrder.Add("GetPreferences");
                    return Task.FromResult(prefTable);
                });

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("dbo.Users")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> _) =>
                {
                    callOrder.Add("GetUser");
                    return Task.FromResult(userTable);
                });

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationTemplates")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> _) =>
                {
                    callOrder.Add("GetTemplate");
                    return Task.FromResult(templateTable);
                });

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("INSERT INTO dbo.NotificationLog")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> _) =>
                {
                    callOrder.Add("InsertLog");
                    return Task.FromResult(logResultTable);
                });

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> _) =>
                {
                    if (sql.Contains("EmailQueue"))
                        callOrder.Add("InsertEmailQueue");
                    else if (sql.Contains("InAppNotifications"))
                        callOrder.Add("InsertInApp");
                    return Task.FromResult(1);
                });

            var placeholders = new Dictionary<string, object>
            {
                { "DepartmentName", "Finance" },
                { "UsagePercentage", 85.5 },
                { "AlertType", "Threshold" }
            };

            // Act
            var result = await _service.SendNotificationAsync(
                TestTenantId, TestUserId, "BudgetAlert", placeholders,
                entityType: "Department", entityId: 1, triggeredByUserId: 50);

            // Assert
            Assert.IsTrue(result > 0, "Notification log ID should be returned");
            Assert.AreEqual(1001, result);

            // Verify call sequence: preferences → user → template → log → email queue
            Assert.That(callOrder, Is.EqualTo(new[] { "GetPreferences", "GetUser", "GetTemplate", "InsertLog", "InsertEmailQueue" }));

            _mockDatabase.Verify(
                x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationSettings")),
                    It.Is<Dictionary<string, object?>>(d =>
                        d["TenantId"].Equals(TestTenantId) &&
                        d["UserId"].Equals(TestUserId))),
                Times.Once);

            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("EmailQueue")),
                    It.Is<Dictionary<string, object?>>(d =>
                        d["Email"].Equals(TestEmail))),
                Times.Once);
        }

        /// <summary>
        /// Test: In-app notification delivery pipeline
        /// Scenario: User has in-app enabled, should create in-app notification and update unread count
        /// </summary>
        [Test]
        public async Task SendNotificationAsync_InAppEnabled_CreatesInAppNotification()
        {
            // Arrange
            var prefTable = CreatePreferencesTable(enableEmail: false, enableInApp: true);
            var userTable = CreateUserTable();
            var templateTable = CreateTemplateTable("ApprovalRequired");

            var notificationStateTable = new DataTable();
            notificationStateTable.Columns.Add("StateId", typeof(int));
            notificationStateTable.Rows.Add(1);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationSettings")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(prefTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("dbo.Users")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(userTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationTemplates")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(templateTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationState")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(notificationStateTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            var placeholders = new Dictionary<string, object>
            {
                { "Amount", 5000m },
                { "Description", "Client proposal review" }
            };

            // Act
            var result = await _service.SendNotificationAsync(
                TestTenantId, TestUserId, "ApprovalRequired", placeholders,
                entityType: "Approval", entityId: 500);

            // Assert
            Assert.AreEqual(0, result, "Email not queued, so log ID should be 0");

            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("InAppNotifications")),
                    It.Is<Dictionary<string, object?>>(d =>
                        d["UserId"].Equals(TestUserId) &&
                        d["Title"].ToString()!.Contains("Approval"))),
                Times.Once);

            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("UPDATE dbo.NotificationState")),
                    It.IsAny<Dictionary<string, object?>>()),
                Times.Once);
        }

        /// <summary>
        /// Test: Both email and in-app notifications enabled
        /// Scenario: User receives both channels simultaneously
        /// </summary>
        [Test]
        public async Task SendNotificationAsync_BothChannelsEnabled_SendsBothNotifications()
        {
            // Arrange
            var prefTable = CreatePreferencesTable(enableEmail: true, enableInApp: true);
            var userTable = CreateUserTable();
            var templateTable = CreateTemplateTable("ExpenseApproved");

            var logResultTable = new DataTable();
            logResultTable.Columns.Add("NotificationId", typeof(int));
            logResultTable.Rows.Add(2001);

            var notificationStateTable = new DataTable();
            notificationStateTable.Columns.Add("StateId", typeof(int));
            notificationStateTable.Rows.Add(1);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationSettings")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(prefTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("dbo.Users")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(userTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationTemplates")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(templateTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("INSERT INTO dbo.NotificationLog")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(logResultTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationState")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(notificationStateTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            var placeholders = new Dictionary<string, object>
            {
                { "Amount", 2500m }
            };

            // Act
            var result = await _service.SendNotificationAsync(
                TestTenantId, TestUserId, "ExpenseApproved", placeholders);

            // Assert
            Assert.AreEqual(2001, result);

            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("EmailQueue")),
                    It.IsAny<Dictionary<string, object?>>()),
                Times.Once);

            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("InAppNotifications")),
                    It.IsAny<Dictionary<string, object?>>()),
                Times.Once);
        }

        /// <summary>
        /// Test: User has no preferences set
        /// Scenario: Should return 0 and not attempt delivery
        /// </summary>
        [Test]
        public async Task SendNotificationAsync_NoPreferencesFound_Returns0()
        {
            // Arrange
            var emptyTable = new DataTable();
            emptyTable.Columns.Add("EnableEmailNotifications", typeof(bool));

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationSettings")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(emptyTable);

            var placeholders = new Dictionary<string, object> { { "test", "value" } };

            // Act
            var result = await _service.SendNotificationAsync(
                TestTenantId, TestUserId, "SomeEvent", placeholders);

            // Assert
            Assert.AreEqual(0, result);

            _mockDatabase.Verify(
                x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("dbo.Users")),
                    It.IsAny<Dictionary<string, object?>>()),
                Times.Never);
        }

        /// <summary>
        /// Test: Template placeholder replacement
        /// Scenario: {{DepartmentName}} and {{UsagePercentage}} should be replaced in subject/body
        /// </summary>
        [Test]
        public async Task SendNotificationAsync_PlaceholderReplacement_ReplacesValuesInSubjectAndBody()
        {
            // Arrange
            var prefTable = CreatePreferencesTable(enableEmail: true, enableInApp: false);
            var userTable = CreateUserTable();

            var templateTable = new DataTable();
            templateTable.Columns.Add("TemplateId", typeof(int));
            templateTable.Columns.Add("Subject", typeof(string));
            templateTable.Columns.Add("BodyHtml", typeof(string));
            templateTable.Columns.Add("InAppTitle", typeof(string));
            templateTable.Columns.Add("InAppBody", typeof(string));
            templateTable.Columns.Add("InAppIcon", typeof(string));
            templateTable.Rows.Add(
                1,
                "Budget Alert: {{DepartmentName}} at {{UsagePercentage}}%",
                "<p>Hello {{RecipientName}}, {{DepartmentName}} has reached {{UsagePercentage}}% of budget</p>",
                "Budget Alert",
                "Budget threshold exceeded",
                "warning");

            var logResultTable = new DataTable();
            logResultTable.Columns.Add("NotificationId", typeof(int));
            logResultTable.Rows.Add(3001);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationSettings")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(prefTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("dbo.Users")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(userTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationTemplates")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(templateTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("INSERT INTO dbo.NotificationLog")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(logResultTable);

            string capturedSubject = "";
            string capturedBody = "";

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("EmailQueue")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> param) =>
                {
                    capturedSubject = param["Subject"].ToString() ?? "";
                    capturedBody = param["Body"].ToString() ?? "";
                    return Task.FromResult(1);
                });

            var placeholders = new Dictionary<string, object>
            {
                { "DepartmentName", "Finance" },
                { "UsagePercentage", 85 }
            };

            // Act
            await _service.SendNotificationAsync(
                TestTenantId, TestUserId, "BudgetAlert", placeholders);

            // Assert
            Assert.That(capturedSubject, Contains.Substring("Finance"));
            Assert.That(capturedSubject, Contains.Substring("85"));
            Assert.That(capturedBody, Contains.Substring("John Manager")); // RecipientName
            Assert.That(capturedBody, Contains.Substring("Finance"));
            Assert.That(capturedBody, Contains.Substring("85"));
        }

        /// <summary>
        /// Test: Bulk notification delivery
        /// Scenario: Send same notification to multiple users
        /// </summary>
        [Test]
        public async Task SendBulkNotificationAsync_MultipleUsers_SendsToAll()
        {
            // Arrange
            var recipientIds = new List<int> { 100, 101, 102 };
            int sendCount = 0;

            var prefTable = CreatePreferencesTable(enableEmail: true, enableInApp: false);
            var userTable = CreateUserTable();
            var templateTable = CreateTemplateTable("SystemAlert");

            var logResultTable = new DataTable();
            logResultTable.Columns.Add("NotificationId", typeof(int));
            logResultTable.Rows.Add(4001);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationSettings")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(prefTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("dbo.Users")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(userTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationTemplates")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(templateTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("INSERT INTO dbo.NotificationLog")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> _) =>
                {
                    sendCount++;
                    return Task.FromResult(logResultTable);
                });

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            var placeholders = new Dictionary<string, object>
            {
                { "Message", "Maintenance scheduled" }
            };

            // Act
            await _service.SendBulkNotificationAsync(
                TestTenantId, recipientIds, "SystemAlert", placeholders);

            // Assert
            Assert.AreEqual(3, sendCount, "Should send to all 3 recipients");

            _mockDatabase.Verify(
                x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationLog")),
                    It.IsAny<Dictionary<string, object?>>()),
                Times.Exactly(3));
        }

        /// <summary>
        /// Test: Get unread notifications with pagination
        /// Scenario: Fetch first 10 unread notifications for user
        /// </summary>
        [Test]
        public async Task GetUnreadNotificationsAsync_WithLimit10_ReturnsFirst10()
        {
            // Arrange
            var notificationsTable = new DataTable();
            notificationsTable.Columns.Add("NotificationId", typeof(int));
            notificationsTable.Columns.Add("Title", typeof(string));
            notificationsTable.Columns.Add("Message", typeof(string));
            notificationsTable.Columns.Add("Icon", typeof(string));
            notificationsTable.Columns.Add("ActionUrl", typeof(string));
            notificationsTable.Columns.Add("ActionText", typeof(string));
            notificationsTable.Columns.Add("Type", typeof(string));
            notificationsTable.Columns.Add("Category", typeof(string));
            notificationsTable.Columns.Add("EntityType", typeof(string));
            notificationsTable.Columns.Add("EntityId", typeof(int));
            notificationsTable.Columns.Add("CreatedAt", typeof(DateTime));
            notificationsTable.Columns.Add("IsRead", typeof(bool));

            for (int i = 1; i <= 10; i++)
            {
                notificationsTable.Rows.Add(
                    i, $"Title {i}", $"Message {i}", "info", "/page", "Action",
                    "Action", "Approval", "Approval", 100 + i, DateTime.UtcNow.AddMinutes(-i), false);
            }

            var stateTable = new DataTable();
            stateTable.Columns.Add("UnreadTotal", typeof(int));
            stateTable.Rows.Add(15);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("InAppNotifications")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(notificationsTable);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationState")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(stateTable);

            // Act
            var (notifications, unreadCount) = await _service.GetUnreadNotificationsAsync(TestTenantId, TestUserId, limit: 10);

            // Assert
            Assert.AreEqual(10, notifications.Count);
            Assert.AreEqual(15, unreadCount);
            Assert.AreEqual("Title 1", notifications[0].Title);
            Assert.AreEqual(101, notifications[0].EntityId);
        }

        /// <summary>
        /// Test: Mark notifications as read
        /// Scenario: User marks single notification as read
        /// </summary>
        [Test]
        public async Task MarkAsReadAsync_WithValidId_UpdatesNotification()
        {
            // Arrange
            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("UPDATE dbo.InAppNotifications")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            await _service.MarkAsReadAsync(1001);

            // Assert
            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("IsRead = 1")),
                    It.Is<Dictionary<string, object?>>(d => d["NotificationId"].Equals(1001))),
                Times.Once);
        }

        /// <summary>
        /// Test: Update user notification preferences
        /// Scenario: User changes email frequency to "Daily"
        /// </summary>
        [Test]
        public async Task UpdatePreferencesAsync_ChangesEmailFrequency_UpdatesDatabase()
        {
            // Arrange
            var prefs = new NotificationPreferences
            {
                EnableEmailNotifications = true,
                EmailOnApprovalRequired = true,
                EmailDigestFrequency = "Daily",
                EnableInAppNotifications = true,
                EnableSmsNotifications = false
            };

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            await _service.UpdatePreferencesAsync(TestTenantId, TestUserId, prefs);

            // Assert
            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("UPDATE dbo.NotificationSettings")),
                    It.Is<Dictionary<string, object?>>(d =>
                        d["DigestFreq"].Equals("Daily"))),
                Times.Once);
        }

        // Helper methods
        private DataTable CreatePreferencesTable(bool enableEmail, bool enableInApp)
        {
            var table = new DataTable();
            table.Columns.Add("EnableEmailNotifications", typeof(bool));
            table.Columns.Add("EmailOnApprovalRequired", typeof(bool));
            table.Columns.Add("EnableInAppNotifications", typeof(bool));
            table.Rows.Add(enableEmail, true, enableInApp);
            return table;
        }

        private DataTable CreateUserTable()
        {
            var table = new DataTable();
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Rows.Add(TestEmail, TestUserName);
            return table;
        }

        private DataTable CreateTemplateTable(string eventType)
        {
            var table = new DataTable();
            table.Columns.Add("TemplateId", typeof(int));
            table.Columns.Add("Subject", typeof(string));
            table.Columns.Add("BodyHtml", typeof(string));
            table.Columns.Add("InAppTitle", typeof(string));
            table.Columns.Add("InAppBody", typeof(string));
            table.Columns.Add("InAppIcon", typeof(string));

            string subject = eventType switch
            {
                "BudgetAlert" => "Budget Alert for {{DepartmentName}}",
                "ApprovalRequired" => "Approval Required: {{Description}}",
                "ExpenseApproved" => "Your expense for {{Amount}} has been approved",
                "SystemAlert" => "System Notification: {{Message}}",
                _ => "Notification"
            };

            string body = eventType switch
            {
                "BudgetAlert" => "<p>{{DepartmentName}} budget alert</p>",
                "ApprovalRequired" => "<p>Please review {{Description}}</p>",
                "ExpenseApproved" => "<p>Amount {{Amount}} approved</p>",
                "SystemAlert" => "<p>{{Message}}</p>",
                _ => "<p>You have a notification</p>"
            };

            table.Rows.Add(1, subject, body, eventType, $"{eventType} body", "info");
            return table;
        }
    }
}
