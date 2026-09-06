import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests/browser',
  // The browser suite drives one process-wide simulated device. Serial workers
  // keep one test's stop/play commands from changing another test's runtime.
  workers: 1,
  timeout: 30_000,
  use: { baseURL: 'http://127.0.0.1:5114', trace: 'retain-on-failure' }
});
