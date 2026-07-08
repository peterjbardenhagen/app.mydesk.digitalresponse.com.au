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
    /// Tests for NotificationRetryService
    /// Validates exponential backoff, retry scheduling, and permanent failure handling
    /// </summary>
    [TestFixture]
    public class NotificationRetryServiceTests
    {
        private NotificationRetryService _service = null!;
        private Mock<DatabaseService> _mockDatabase = null!;

        private const int TestTenantId = 1;

        [SetUp]
        public void SetUp()
        {
            _mockDatabase = new Mock<DatabaseService>();
            _service = new NotificationRetryService(_mockDatabase.Object, logger: null);
        }

        /// <summary>
        /// Test: Process failed notification for first retry
        /// Expected: Status='Pending', RetryCount=1, delay=60 seconds
        /// </summary>
        [Test]
        public async Task ProcessFailedNotificationsAsync_FirstRetry_SchedulesWithMinimalDelay()
        {
            // Arrange
            var failedTable = new DataTable();
            failedTable.Columns.Add("EmailQueueId", typeof(int));
            failedTable.Columns.Add("ToEmail", typeof(string));
            failedTable.Columns.Add("ToName", typeof(string));
            failedTable.Columns.Add("FromEmail", typeof(string));
            failedTable.Columns.Add("FromName", typeof(string));
            failedTable.Columns.Add("Subject", typeof(string));
            failedTable.Columns.Add("BodyHtml", typeof(string));
            failedTable.Columns.Add("NotificationLogId", typeof(int));
            failedTable.Columns.Add("RetryCount", typeof(int));
            failedTable.Columns.Add("LastRetryAt", typeof(DateTime));
            failedTable.Columns.Add("ErrorMessage", typeof(string));
            failedTable.Rows.Add(
                1001, "user@company.com", "User Name", "noreply@app.com", "App",
                "Notification subject", "<p>Body</p>", 5001, 0, DBNull.Value, "Connection timeout");

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("EmailQueue")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(failedTable);

            int capturedRetryCount = 0;
            int capturedDelay = 0;

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("UPDATE dbo.EmailQueue")),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> param) =>
                {
                    if (param.ContainsKey("Delay"))
                    {
                        capturedDelay = (int)param["Delay"];
                    }
                    return Task.FromResult(1);
                });

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("NotificationRetryLog")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.ProcessFailedNotificationsAsync(TestTenantId);

            // Assert
            Assert.AreEqual(1, result, "Should process 1 failed email");
            Assert.AreEqual(60, capturedDelay, "First retry should have 60 second delay");

            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("UPDATE dbo.EmailQueue")),
                    It.Is<Dictionary<string, object?>>(d => d["Delay"].Equals(60))),
                Times.Once);
        }

        /// <summary>
        /// Test: Process notification with multiple retries
        /// Expected: Exponential backoff - attempt 3 gets 240 second (4 minute) delay
        /// </summary>
        [Test]
        public async Task ProcessFailedNotificationsAsync_ThirdRetry_ExponentialBackoff()
        {
            // Arrange
            var failedTable = new DataTable();
            failedTable.Columns.Add("EmailQueueId", typeof(int));
            failedTable.Columns.Add("ToEmail", typeof(string));
            failedTable.Columns.Add("ToName", typeof(string));
            failedTable.Columns.Add("FromEmail", typeof(string));
            failedTable.Columns.Add("FromName", typeof(string));
            failedTable.Columns.Add("Subject", typeof(string));
            failedTable.Columns.Add("BodyHtml", typeof(string));
            failedTable.Columns.Add("NotificationLogId", typeof(int));
            failedTable.Columns.Add("RetryCount", typeof(int));
            failedTable.Columns.Add("LastRetryAt", typeof(DateTime));
            failedTable.Columns.Add("ErrorMessage", typeof(string));

            // Simulate email already retried twice
            failedTable.Rows.Add(
                1002, "user@company.com", "User Name", "noreply@app.com", "App",
                "Subject", "<p>Body</p>", 5002, 2, DateTime.UtcNow.AddMinutes(-5), "SMTP error");

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.Is<string>(sql => sql.Contains("EmailQueue")),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(failedTable);

            int capturedDelay = 0;

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> param) =>
                {
                    if (param.ContainsKey("Delay"))
                    {
                        capturedDelay = (int)param["Delay"];
                    }
                    return Task.FromResult(1);
                });

            // Act
            await _service.ProcessFailedNotificationsAsync(TestTenantId);

            // Assert
            // Attempt 3: 60 * 2^2 = 240 seconds = 4 minutes
            Assert.AreEqual(240, capturedDelay, "Third retry should have 240 second delay (exponential backoff)");
        }

        /// <summary>
        /// Test: Mark email as permanently failed
        /// Expected: Status='DeadLettered', NotificationLog marked as Failed with error message
        /// </summary>
        [Test]
        public async Task MarkAsPermanentlyFailedAsync_WithMaxRetriesExceeded_DeadsLetters()
        {
            // Arrange
            var queueTable = new DataTable();
            queueTable.Columns.Add("NotificationLogId", typeof(int));
            queueTable.Rows.Add(5003);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(queueTable);

            string capturedStatus = "";
            string capturedError = "";

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .Returns((string sql, Dictionary<string, object?> param) =>
                {
                    if (sql.Contains("EmailQueue"))
                    {
                        capturedStatus = param["Error"]?.ToString() ?? "";
                    }
                    return Task.FromResult(1);
                });

            var finalError = "Max retries exceeded: SMTP server unreachable";

            // Act
            var result = await _service.MarkAsPermanentlyFailedAsync(1003, finalError);

            // Assert
            Assert.IsTrue(result);

            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("UPDATE dbo.EmailQueue")),
                    It.Is<Dictionary<string, object?>>(d =>
                        d["Error"].ToString()!.Contains("Max retries"))),
                Times.Once);

            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("UPDATE dbo.NotificationLog")),
                    It.Is<Dictionary<string, object?>>(d =>
                        d["Error"].ToString()!.Contains("Max retries"))),
                Times.Once);
        }

        /// <summary>
        /// Test: Retry specific email manually
        /// Expected: Status set to Pending, RetryCount incremented
        /// </summary>
        [Test]
        public async Task RetrySpecificEmailAsync_WithValidEmail_QueuesForRetry()
        {
            // Arrange
            var emailTable = new DataTable();
            emailTable.Columns.Add("RetryCount", typeof(int));
            emailTable.Rows.Add(2);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(emailTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.RetrySpecificEmailAsync(1004);

            // Assert
            Assert.IsTrue(result);

            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.Is<string>(sql => sql.Contains("UPDATE dbo.EmailQueue")),
                    It.Is<Dictionary<string, object?>>(d =>
                        d["Status"].ToString() == "Pending")),
                Times.Once);
        }

        /// <summary>
        /// Test: Retry email that already exceeded max retries
        /// Expected: Returns false, does not queue
        /// </summary>
        [Test]
        public async Task RetrySpecificEmailAsync_MaxRetriesExceeded_ReturnsFalse()
        {
            // Arrange
            var emailTable = new DataTable();
            emailTable.Columns.Add("RetryCount", typeof(int));
            emailTable.Rows.Add(5);  // Max retries = 5

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(emailTable);

            // Act
            var result = await _service.RetrySpecificEmailAsync(1005);

            // Assert
            Assert.IsFalse(result, "Should not retry when max retries exceeded");

            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()),
                Times.Never);
        }

        /// <summary>
        /// Test: Get retry history for email
        /// Expected: Returns all previous retry attempts with timestamps
        /// </summary>
        [Test]
        public async Task GetRetryHistoryAsync_WithMultipleRetries_ReturnsAll()
        {
            // Arrange
            var historyTable = new DataTable();
            historyTable.Columns.Add("AttemptNumber", typeof(int));
            historyTable.Columns.Add("NextRetryAt", typeof(DateTime));
            historyTable.Columns.Add("CreatedAt", typeof(DateTime));

            var now = DateTime.UtcNow;
            historyTable.Rows.Add(1, now.AddMinutes(1), now);
            historyTable.Rows.Add(2, now.AddMinutes(3), now.AddMinutes(1.5));
            historyTable.Rows.Add(3, now.AddMinutes(7), now.AddMinutes(3.5));

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(historyTable);

            // Act
            var result = await _service.GetRetryHistoryAsync(1006);

            // Assert
            Assert.AreEqual(3, result.Rows.Count);
            Assert.AreEqual(1, result.Rows[0]["AttemptNumber"]);
            Assert.AreEqual(2, result.Rows[1]["AttemptNumber"]);
            Assert.AreEqual(3, result.Rows[2]["AttemptNumber"]);
        }

        /// <summary>
        /// Test: Get failure statistics for tenant
        /// Expected: Counts of total/never-retried/in-retry/exceeded-max notifications
        /// </summary>
        [Test]
        public async Task GetFailureStatisticsAsync_WithVariousStatuses_ReturnsAccurateCounts()
        {
            // Arrange
            var statsTable = new DataTable();
            statsTable.Columns.Add("TotalFailed", typeof(int));
            statsTable.Columns.Add("NeverRetried", typeof(int));
            statsTable.Columns.Add("InRetry", typeof(int));
            statsTable.Columns.Add("ExceededMax", typeof(int));
            statsTable.Rows.Add(15, 5, 7, 3);

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(statsTable);

            // Act
            var result = await _service.GetFailureStatisticsAsync(TestTenantId);

            // Assert
            Assert.AreEqual(15, result.TotalFailed);
            Assert.AreEqual(5, result.NeverRetried);
            Assert.AreEqual(7, result.InRetry);
            Assert.AreEqual(3, result.ExceededMax);
        }

        /// <summary>
        /// Test: No failed notifications in queue
        /// Expected: Returns 0 retried count
        /// </summary>
        [Test]
        public async Task ProcessFailedNotificationsAsync_NoFailedEmails_Returns0()
        {
            // Arrange
            var emptyTable = new DataTable();
            emptyTable.Columns.Add("EmailQueueId", typeof(int));

            _mockDatabase
                .Setup(x => x.QueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(emptyTable);

            // Act
            var result = await _service.ProcessFailedNotificationsAsync(TestTenantId);

            // Assert
            Assert.AreEqual(0, result);

            _mockDatabase.Verify(
                x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object?>>()),
                Times.Never);
        }
    }
}
