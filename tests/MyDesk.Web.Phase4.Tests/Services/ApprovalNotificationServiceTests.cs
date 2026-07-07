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
    public class ApprovalNotificationServiceTests
    {
        private ApprovalNotificationService _service = null!;
        private Mock<DatabaseService> _mockDatabase = null!;
        private Mock<NotificationService> _mockNotification = null!;
        private Mock<ApprovalDelegationService> _mockDelegation = null!;

        [SetUp]
        public void SetUp()
        {
            _mockDatabase = new Mock<DatabaseService>();
            _mockNotification = new Mock<NotificationService>();
            _mockDelegation = new Mock<ApprovalDelegationService>();
            _service = new ApprovalNotificationService(
                _mockDatabase.Object,
                _mockNotification.Object,
                _mockDelegation.Object,
                null);
        }

        [Test]
        public async Task NotifyDelegateAsync_WithValidApproval_SendsNotification()
        {
            // Arrange
            var tenantId = 1;
            var approvalId = 1;
            var delegateUserId = 200;
            var delegatedByUserId = 100;
            var reason = "On leave";

            var approvalTable = CreateApprovalTable(new[]
            {
                new { ApprovalId = 1, Amount = 5000m, Description = "Client meeting", DelegatedByName = "John Manager", DelegateName = "Jane Delegate" }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(approvalTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

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
            var result = await _service.NotifyDelegateAsync(
                tenantId, approvalId, delegateUserId, delegatedByUserId, reason);

            // Assert
            Assert.IsTrue(result);
            _mockNotification.Verify(
                x => x.SendNotificationAsync(
                    tenantId,
                    delegateUserId,
                    "ApprovalDelegated",
                    It.IsAny<Dictionary<string, object>>(),
                    "Approval",
                    approvalId,
                    delegatedByUserId),
                Times.Once);
        }

        [Test]
        public async Task NotifyDelegateAsync_WithInvalidApprovalId_ReturnsFalse()
        {
            // Arrange
            var tenantId = 1;
            var approvalId = 999;
            var delegateUserId = 200;
            var delegatedByUserId = 100;

            var emptyTable = new DataTable();
            emptyTable.Columns.Add("ApprovalId", typeof(int));

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(emptyTable);

            // Act
            var result = await _service.NotifyDelegateAsync(
                tenantId, approvalId, delegateUserId, delegatedByUserId, "reason");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task NotifyEscalationAsync_WithValidApproval_SendsEscalationNotification()
        {
            // Arrange
            var tenantId = 1;
            var approvalId = 1;
            var escalatedToUserId = 300;
            var escalatedByUserId = 100;
            var reason = "Amount exceeds threshold";

            var approvalTable = CreateApprovalTable(new[]
            {
                new { ApprovalId = 1, Amount = 25000m, Description = "Travel expenses", EscalatedByName = "John Manager", EscalatedToName = "Sarah Director", RequestorName = "Tom Employee" }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(approvalTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

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
            var result = await _service.NotifyEscalationAsync(
                tenantId, approvalId, escalatedToUserId, escalatedByUserId, reason);

            // Assert
            Assert.IsTrue(result);
            _mockNotification.Verify(
                x => x.SendNotificationAsync(
                    tenantId,
                    escalatedToUserId,
                    "ApprovalEscalated",
                    It.IsAny<Dictionary<string, object>>(),
                    "Approval",
                    approvalId,
                    escalatedByUserId),
                Times.Once);
        }

        [Test]
        public async Task SendApprovalRemindersAsync_WithPendingApprovals_SendsReminders()
        {
            // Arrange
            var tenantId = 1;
            var daysOld = 3;

            var pendingApprovalsTable = CreatePendingApprovalsTable(new[]
            {
                new { ApprovalId = 1, ApproverUserId = 100, RequestorUserId = 200, Amount = 5000m, CreatedAt = DateTime.UtcNow.AddDays(-4), Description = "Expense", ApproverName = "John Manager", RequestorName = "Tom Employee" },
                new { ApprovalId = 2, ApproverUserId = 100, RequestorUserId = 200, Amount = 3000m, CreatedAt = DateTime.UtcNow.AddDays(-5), Description = "Travel", ApproverName = "John Manager", RequestorName = "Tom Employee" }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(pendingApprovalsTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

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
            var result = await _service.SendApprovalRemindersAsync(tenantId, daysOld);

            // Assert
            Assert.AreEqual(2, result);
            _mockNotification.Verify(
                x => x.SendNotificationAsync(
                    tenantId,
                    It.IsAny<int>(),
                    "ApprovalReminder",
                    It.IsAny<Dictionary<string, object>>(),
                    "Approval",
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task SendApprovalRemindersAsync_WithNoPendingApprovals_ReturnsZero()
        {
            // Arrange
            var tenantId = 1;
            var emptyTable = new DataTable();
            emptyTable.Columns.Add("ApprovalId", typeof(int));

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(emptyTable);

            // Act
            var result = await _service.SendApprovalRemindersAsync(tenantId, 3);

            // Assert
            Assert.AreEqual(0, result);
        }

        [Test]
        public async Task NotifyApprovalDecisionAsync_WithApprovedDecision_SendsApprovedNotification()
        {
            // Arrange
            var tenantId = 1;
            var approvalId = 1;
            var requestorUserId = 200;
            var approverUserId = 100;
            var decision = "Approved";
            var comments = "Looks good";

            var approvalTable = CreateDecisionApprovalTable(new[]
            {
                new { ApprovalId = 1, Amount = 5000m, Description = "Team lunch", ApproverName = "John Manager", RequestorName = "Tom Employee" }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(approvalTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

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
            var result = await _service.NotifyApprovalDecisionAsync(
                tenantId, approvalId, requestorUserId, decision, comments, approverUserId);

            // Assert
            Assert.IsTrue(result);
            _mockNotification.Verify(
                x => x.SendNotificationAsync(
                    tenantId,
                    requestorUserId,
                    "ApprovalApproved",
                    It.IsAny<Dictionary<string, object>>(),
                    "Approval",
                    approvalId,
                    approverUserId),
                Times.Once);
        }

        [Test]
        public async Task NotifyApprovalDecisionAsync_WithRejectedDecision_SendsRejectedNotification()
        {
            // Arrange
            var tenantId = 1;
            var approvalId = 1;
            var requestorUserId = 200;
            var approverUserId = 100;
            var decision = "Rejected";
            var comments = "Missing receipt";

            var approvalTable = CreateDecisionApprovalTable(new[]
            {
                new { ApprovalId = 1, Amount = 5000m, Description = "Team lunch", ApproverName = "John Manager", RequestorName = "Tom Employee" }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(approvalTable);

            _mockDatabase
                .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
                .ReturnsAsync(1);

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
            var result = await _service.NotifyApprovalDecisionAsync(
                tenantId, approvalId, requestorUserId, decision, comments, approverUserId);

            // Assert
            Assert.IsTrue(result);
            _mockNotification.Verify(
                x => x.SendNotificationAsync(
                    tenantId,
                    requestorUserId,
                    "ApprovalRejected",
                    It.IsAny<Dictionary<string, object>>(),
                    "Approval",
                    approvalId,
                    approverUserId),
                Times.Once);
        }

        [Test]
        public async Task GetApprovalNotificationHistoryAsync_WithValidApproval_ReturnsNotificationHistory()
        {
            // Arrange
            var tenantId = 1;
            var approvalId = 1;
            var expectedTable = CreateNotificationHistoryTable(new[]
            {
                new { NotificationId = 1, EventType = "Delegated", RecipientUserId = 200, CreatedAt = DateTime.UtcNow },
                new { NotificationId = 2, EventType = "Reminder", RecipientUserId = 100, CreatedAt = DateTime.UtcNow }
            });

            _mockDatabase
                .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(expectedTable);

            // Act
            var result = await _service.GetApprovalNotificationHistoryAsync(tenantId, approvalId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Rows.Count);
        }

        private DataTable CreateApprovalTable(dynamic[] approvals)
        {
            var table = new DataTable();
            table.Columns.Add("ApprovalId", typeof(int));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("DelegatedByName", typeof(string));
            table.Columns.Add("DelegateName", typeof(string));
            table.Columns.Add("EscalatedByName", typeof(string));
            table.Columns.Add("EscalatedToName", typeof(string));
            table.Columns.Add("RequestorName", typeof(string));

            foreach (var approval in approvals)
            {
                table.Rows.Add(
                    approval.ApprovalId,
                    approval.Amount,
                    approval.Description,
                    approval.DelegatedByName ?? (object)DBNull.Value,
                    approval.DelegateName ?? (object)DBNull.Value,
                    approval.EscalatedByName ?? (object)DBNull.Value,
                    approval.EscalatedToName ?? (object)DBNull.Value,
                    approval.RequestorName ?? (object)DBNull.Value
                );
            }

            return table;
        }

        private DataTable CreatePendingApprovalsTable(dynamic[] approvals)
        {
            var table = new DataTable();
            table.Columns.Add("ApprovalId", typeof(int));
            table.Columns.Add("ApproverUserId", typeof(int));
            table.Columns.Add("RequestorUserId", typeof(int));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("ApproverName", typeof(string));
            table.Columns.Add("RequestorName", typeof(string));

            foreach (var approval in approvals)
            {
                table.Rows.Add(
                    approval.ApprovalId,
                    approval.ApproverUserId,
                    approval.RequestorUserId,
                    approval.Amount,
                    approval.CreatedAt,
                    approval.Description,
                    approval.ApproverName,
                    approval.RequestorName
                );
            }

            return table;
        }

        private DataTable CreateDecisionApprovalTable(dynamic[] approvals)
        {
            var table = new DataTable();
            table.Columns.Add("ApprovalId", typeof(int));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("ApproverName", typeof(string));
            table.Columns.Add("RequestorName", typeof(string));

            foreach (var approval in approvals)
            {
                table.Rows.Add(
                    approval.ApprovalId,
                    approval.Amount,
                    approval.Description,
                    approval.ApproverName,
                    approval.RequestorName
                );
            }

            return table;
        }

        private DataTable CreateNotificationHistoryTable(dynamic[] notifications)
        {
            var table = new DataTable();
            table.Columns.Add("NotificationId", typeof(int));
            table.Columns.Add("EventType", typeof(string));
            table.Columns.Add("RecipientUserId", typeof(int));
            table.Columns.Add("CreatedAt", typeof(DateTime));

            foreach (var notif in notifications)
            {
                table.Rows.Add(
                    notif.NotificationId,
                    notif.EventType,
                    notif.RecipientUserId,
                    notif.CreatedAt
                );
            }

            return table;
        }
    }
}
