import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests/browser',
  timeout: 30_000,
  use: { baseURL: 'http://127.0.0.1:5114', trace: 'retain-on-failure' }
});
