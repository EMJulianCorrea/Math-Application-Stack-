const { test, expect } = require('@playwright/test');

const uniqueId = Date.now();

const testUser = {
  email: `playwright.${uniqueId}@mathstack.test`,
  testPassword: 'NotASecret-UsedOnlyForPlaywrightTests123!'
};

test('health endpoint returns ok', async ({ request }) => {
  const response = await request.get('/health');

  expect(response.ok()).toBeTruthy();
  expect(await response.text()).toBe('ok');
});

/**
test('intentional failing test to view Playwright report', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'This heading does not exist' })).toBeVisible();
});
*/

test('home page loads successfully', async ({ page }) => {
  await page.goto('/');

  await expect(page).toHaveTitle(/Home Page - MathAPIClient/);
  await expect(page.getByRole('heading', { name: 'Welcome' })).toBeVisible();
});

test('main navigation links are visible before login', async ({ page }) => {
  await page.goto('/');

  const nav = page.locator('header nav');

  await expect(nav.getByRole('link', { name: 'MathAPIClient' })).toBeVisible();
  await expect(nav.getByRole('link', { name: 'Calculate' })).toBeVisible();
  await expect(nav.getByRole('link', { name: 'History' })).toBeVisible();
  await expect(nav.getByRole('link', { name: 'Login' })).toBeVisible();
});

test('login page loads and links to register', async ({ page }) => {
  await page.goto('/Auth/Login');

  await expect(page).toHaveTitle(/Login - MathAPIClient/);
  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
  await expect(page.getByLabel('Email')).toBeVisible();
  await expect(page.getByLabel('Password')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Login' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Register' })).toBeVisible();
});

test('register page loads and links back to login', async ({ page }) => {
  await page.goto('/Auth/Register');

  await expect(page).toHaveTitle(/Register - MathAPIClient/);
  await expect(page.getByRole('heading', { name: 'Register' })).toBeVisible();
  await expect(page.getByLabel('Email')).toBeVisible();
  await expect(page.getByLabel('Password')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Register' })).toBeVisible();
  await expect(page.getByRole('link', { name: /Already have an account\? Login/i })).toBeVisible();
});

test('calculate page redirects unauthenticated user to login', async ({ page }) => {
  await page.goto('/Math/Calculate');

  await expect(page).toHaveURL(/\/Auth\/Login/i);
  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
});

test('history page redirects unauthenticated user to login', async ({ page }) => {
  await page.goto('/Math/History');

  await expect(page).toHaveURL(/\/Auth\/Login/i);
  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
});

test.describe.serial('authenticated user features', () => {
  test.beforeAll(async ({ browser }) => {
    const page = await browser.newPage();

    await page.goto('/Auth/Register');

    await page.getByLabel('Email').fill(testUser.email);
    await page.getByLabel('Password').fill(testUser.testPassword);
    await page.getByRole('button', { name: 'Register' }).click();

    await expect(page).toHaveURL(/\/Math\/Calculate/i);
    await expect(page.getByRole('heading', { name: /Welcome to the Calculator/i })).toBeVisible();

    await page.close();
  });

  test.beforeEach(async ({ page }) => {
    await page.goto('/Auth/Login');

    await page.getByLabel('Email').fill(testUser.email);
    await page.getByLabel('Password').fill(testUser.testPassword);
    await page.getByRole('button', { name: 'Login' }).click();

    await expect(page).toHaveURL(/\/Math\/Calculate/i);
    await expect(page.getByRole('heading', { name: /Welcome to the Calculator/i })).toBeVisible();
  });

  test('registered user can access calculate page', async ({ page }) => {
    await page.goto('/Math/Calculate');

    await expect(page).toHaveURL(/\/Math\/Calculate/i);
    await expect(page.getByRole('heading', { name: /Welcome to the Calculator/i })).toBeVisible();
  });

  test('registered user can perform a calculation', async ({ page }) => {
    await page.goto('/Math/Calculate');

    await page.locator('input[name="FirstNumber"]').fill('10');
    await page.locator('input[name="SecondNumber"]').fill('5');
    await page.locator('select[name="Operation"]').selectOption('1');
    await page.getByRole('button', { name: 'Calculate' }).click();

    await expect(page.getByText(/Result is 15/i)).toBeVisible();
  });

  test('registered user can view calculation history', async ({ page }) => {
    await page.goto('/Math/Calculate');

    await page.locator('input[name="FirstNumber"]').fill('10');
    await page.locator('input[name="SecondNumber"]').fill('5');
    await page.locator('select[name="Operation"]').selectOption('1');
    await page.getByRole('button', { name: 'Calculate' }).click();

    await expect(page.getByText(/Result is 15/i)).toBeVisible();

    await page.goto('/Math/History');

    await expect(page).toHaveTitle(/History - MathAPIClient/);
    await expect(page.getByRole('heading', { name: 'History' })).toBeVisible();

    const table = page.locator('table');

    await expect(table).toContainText('10');
    await expect(table).toContainText('+');
    await expect(table).toContainText('5');
    await expect(table).toContainText('15');
  });

  test('registered user can clear calculation history', async ({ page }) => {
    await page.goto('/Math/Calculate');

    await page.locator('input[name="FirstNumber"]').fill('10');
    await page.locator('input[name="SecondNumber"]').fill('5');
    await page.locator('select[name="Operation"]').selectOption('1');
    await page.getByRole('button', { name: 'Calculate' }).click();

    await expect(page.getByText(/Result is 15/i)).toBeVisible();

    await page.goto('/Math/History');
    await page.getByRole('button', { name: 'Clear' }).click();

    await expect(page).toHaveURL(/\/Math\/History/i);
    await expect(page.getByRole('heading', { name: 'History' })).toBeVisible();
    await expect(page.locator('body')).toContainText('History');
  });

  test('registered user can log out', async ({ page }) => {
    await page.goto('/Auth/LogOut');

    await expect(page).toHaveURL(/\/Auth\/Login/i);
    await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
  });
});