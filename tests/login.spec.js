const { test, expect } = require('@playwright/test');

/**
 * Techlight MyDesk Login Tests
 * 
 * These tests verify the simplified authentication workflow:
 * 1. User accesses /Default.asp
 * 2. If not logged in, sees unified login page
 * 3. Enters credentials and submits
 * 4. On success, redirected to Dashboard
 * 
 * Test credentials:
 * - Username: peter bardenhagen
 * - Password: fairmont
 */

test.describe('MyDesk Login Flow', () => {
  
  test.beforeEach(async ({ page }) => {
    // Clear cookies and storage before each test
    await page.context().clearCookies();
  });

  test('should display login page when not authenticated', async ({ page }) => {
    // Navigate to the entry point
    await page.goto('/Default.asp');
    
    // Verify we're on the login page
    await expect(page).toHaveTitle(/MyDesk|Sign In/i);
    
    // Verify login form elements are visible
    await expect(page.locator('#Username')).toBeVisible();
    await expect(page.locator('#Password')).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();
    
    // Verify page branding
    await expect(page.locator('.logo-title')).toContainText('Techlight');
    await expect(page.locator('.welcome-text h2')).toContainText('Welcome Back');
  });

  test('should show error message for invalid credentials', async ({ page }) => {
    await page.goto('/Default.asp');
    
    // Enter invalid credentials
    await page.fill('#Username', 'invaliduser');
    await page.fill('#Password', 'wrongpassword');
    
    // Submit form
    await page.click('button[type="submit"]');
    
    // Wait for redirect back to login with error
    await page.waitForURL(/.*Default.asp.*/);
    
    // Verify error message is displayed
    await expect(page.locator('.error-message')).toBeVisible();
    await expect(page.locator('.error-message')).toContainText(/Login failed|incorrect/i);
  });

  test('should successfully login with valid credentials', async ({ page }) => {
    // Note: This test requires the local dev server to be running with database access
    test.skip(process.env.SKIP_LOGIN_TEST === 'true', 'Skipping login test - dev server not available');
    
    await page.goto('/Default.asp');
    
    // Enter valid credentials
    await page.fill('#Username', 'peter bardenhagen');
    await page.fill('#Password', 'fairmont');
    
    // Submit form
    await page.click('button[type="submit"]');
    
    // Wait for redirect to dashboard
    await page.waitForURL(/.*Dashboard.asp.*/, { timeout: 10000 });
    
    // Verify we're on the dashboard
    await expect(page).toHaveTitle(/Dashboard|MyDesk/i);
    
    // Verify welcome message or dashboard content
    await expect(page.locator('body')).toContainText(/Welcome|Dashboard|peter/i);
  });

  test('should redirect to dashboard when already logged in', async ({ page, context }) => {
    // Note: This test requires the local dev server to be running with database access
    test.skip(process.env.SKIP_LOGIN_TEST === 'true', 'Skipping login test - dev server not available');
    
    // First, login to establish session
    await page.goto('/Default.asp');
    await page.fill('#Username', 'peter bardenhagen');
    await page.fill('#Password', 'fairmont');
    await page.click('button[type="submit"]');
    await page.waitForURL(/.*Dashboard.asp.*/, { timeout: 10000 });
    
    // Get cookies from the authenticated session
    const cookies = await context.cookies();
    
    // Clear page and go back to entry point
    await page.goto('/Default.asp');
    
    // Should redirect immediately to dashboard (no login form shown)
    await page.waitForURL(/.*Dashboard.asp.*/, { timeout: 5000 });
    
    // Verify we're still on dashboard
    await expect(page).toHaveTitle(/Dashboard|MyDesk/i);
  });

  test('login form should have proper accessibility attributes', async ({ page }) => {
    await page.goto('/Default.asp');
    
    // Check for required attribute
    await expect(page.locator('#Username')).toHaveAttribute('required', '');
    await expect(page.locator('#Password')).toHaveAttribute('required', '');
    
    // Check for autocomplete attributes
    await expect(page.locator('#Username')).toHaveAttribute('autocomplete', 'username');
    await expect(page.locator('#Password')).toHaveAttribute('autocomplete', 'current-password');
    
    // Check for proper labels
    await expect(page.locator('label[for="Username"]')).toContainText('Username');
    await expect(page.locator('label[for="Password"]')).toContainText('Password');
  });

  test('should validate required fields', async ({ page }) => {
    await page.goto('/Default.asp');
    
    // Try to submit empty form
    await page.click('button[type="submit"]');
    
    // HTML5 validation should prevent submission
    // The page should still show the login form
    await expect(page.locator('#Username')).toBeVisible();
    await expect(page).toHaveURL(/.*Default.asp.*/);
  });

  test('page should be responsive on mobile viewport', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    await page.goto('/Default.asp');
    
    // Verify login container is visible and properly sized
    const loginContainer = page.locator('.login-container');
    await expect(loginContainer).toBeVisible();
    
    // Check that it fits within viewport (no horizontal scroll)
    const bodyBox = await page.locator('body').boundingBox();
    const containerBox = await loginContainer.boundingBox();
    
    expect(containerBox.width).toBeLessThanOrEqual(bodyBox.width);
  });
});

test.describe('Navigation and Links', () => {
  test('should have working help link', async ({ page }) => {
    await page.goto('/Default.asp');
    
    const helpLink = page.locator('a[href="https://techlight.com.au"]');
    await expect(helpLink).toBeVisible();
    await expect(helpLink).toHaveAttribute('target', '_blank');
  });

  test('should have forgot password link with alert', async ({ page }) => {
    await page.goto('/Default.asp');
    
    const forgotPasswordLink = page.locator('a:has-text("Forgot Password")');
    await expect(forgotPasswordLink).toBeVisible();
    
    // Click should trigger alert
    page.on('dialog', async dialog => {
      expect(dialog.type()).toBe('alert');
      expect(dialog.message()).toContain('administrator');
      await dialog.accept();
    });
    
    await forgotPasswordLink.click();
  });
});
