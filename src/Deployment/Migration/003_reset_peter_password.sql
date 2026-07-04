-- Migration 003: Reset Peter Bardenhagen (TL0025) password to a known temporary value.
-- Temporary password: MyDesk2024!
-- Peter can update this after logging in via Settings → Profile.
-- VerifyPassword supports plain-text passwords (legacy path) so no BCrypt hash needed here.

DECLARE @PeterUserId INT = (SELECT TOP 1 UserId FROM Users WHERE UPPER(Code) = 'TL0025');

IF @PeterUserId IS NOT NULL
BEGIN
    UPDATE Users
    SET PW = 'MyDesk2024!', Active = 1, Deleted = 0
    WHERE UserId = @PeterUserId;
    PRINT 'Reset TL0025 (Peter Bardenhagen) password to MyDesk2024!';
END
ELSE
BEGIN
    PRINT 'WARNING: User TL0025 not found — skipping password reset';
END
