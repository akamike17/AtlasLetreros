import { test, expect } from '@playwright/test';

const simulator = {
  id: 'atlas-simulator', name: 'Simulador local', kind: 'simulator', online: true,
  route: { scene: '/api/scene', status: '/api/status', output: '/api/output', stop: '/api/stop', brightness: '/api/brightness' },
  capabilities: capabilities('atlas-simulator', 32, 16)
};
const physical = { id: 'atlas-physical', name: 'atlasled-test', kind: 'physical', address: '10.20.30.40', online: true,
  capabilities: capabilities('atlas-physical', 32, 16) };

function capabilities(device, width, height) {
  return { protocolVersion: 1, device, width, height, colorModel: 0, features: ['scenes'], drivers: ['test'], sceneStorageBytes: 1024 };
}
function status(device, width = 32, height = 16) {
  return { protocolVersion: 1, device: capabilities(device, width, height), runtime: {
    isPlaying: false, activeSceneId: null, activeSceneName: null, brightness: 255, lastError: null, positionSeconds: 0
  }};
}

test('selector cambia simulador-físico-simulador y descarta status tardío', async ({ page }) => {
  await page.route('**/api/devices', route => route.fulfill({ json: [simulator, physical] }));
  await page.route('**/api/status', async route => {
    await new Promise(resolve => setTimeout(resolve, 600));
    await route.fulfill({ json: status('atlas-simulator') });
  });
  await page.route('**/api/output', route => route.fulfill({ json: { ...status('atlas-simulator'), snapshot: null } }));
  await page.route('**/api/devices/atlas-physical/status', route => route.fulfill({ json: status('atlas-physical') }));
  await page.route('**/api/devices/atlas-physical/output', route => route.fulfill({ json: { ...status('atlas-physical'), snapshot: null } }));

  await page.goto('/');
  await expect(page.locator('#deviceSelect option')).toHaveCount(2);
  await page.getByRole('tab', { name: 'Dispositivo' }).click();
  await page.locator('#deviceSelect').selectOption('atlas-physical');
  await expect(page.locator('#deviceName')).toHaveText('atlasled-test', { timeout: 3000 });
  await expect(page.locator('#deviceDetails')).toContainText('32 × 16');
  await expect(page.locator('#outputDeviceId')).toHaveText('atlas-physical');
  await expect(page.locator('#outputResolution')).toHaveText('32 × 16');
  await page.waitForTimeout(800);
  await expect(page.locator('#deviceName')).toHaveText('atlasled-test');

  await page.locator('#deviceSelect').selectOption('atlas-simulator');
  await expect(page.locator('#deviceName')).toHaveText('Simulador local', { timeout: 3000 });
});

test('la misma Scene se envía al simulador y al físico seleccionado', async ({ page }) => {
  const uploads = [];
  const commands = [];
  await page.route('**/api/devices', route => route.fulfill({ json: [simulator, physical] }));
  await page.route('**/api/status', route => route.fulfill({ json: status('atlas-simulator') }));
  await page.route('**/api/output', route => route.fulfill({ json: { ...status('atlas-simulator'), snapshot: null } }));
  await page.route('**/api/devices/atlas-physical/status', route => route.fulfill({ json: status('atlas-physical') }));
  await page.route('**/api/devices/atlas-physical/output', route => route.fulfill({ json: { ...status('atlas-physical'), snapshot: null } }));
  await page.route('**/api/scene', async route => { uploads.push({ device: 'atlas-simulator', body: route.request().postDataJSON() }); await route.fulfill({ json: { sceneId: '00000000-0000-0000-0000-000000000001' } }); });
  await page.route('**/api/devices/atlas-physical/scene', async route => { uploads.push({ device: 'atlas-physical', body: route.request().postDataJSON() }); await route.fulfill({ json: { sceneId: '00000000-0000-0000-0000-000000000001' } }); });
  await page.route('**/api/devices/atlas-physical/brightness', async route => { commands.push({ kind: 'brightness', body: route.request().postDataJSON() }); await route.fulfill({ status: 204 }); });
  await page.route('**/api/devices/atlas-physical/stop', async route => { commands.push({ kind: 'stop', body: route.request().postDataJSON() }); await route.fulfill({ status: 204 }); });
  page.on('dialog', dialog => dialog.accept());

  await page.goto('/');
  await expect(page.locator('#deviceState')).toContainText('Conectado');
  await page.locator('#sendButton').click();
  await expect.poll(() => uploads.length).toBe(1);
  await page.getByRole('tab', { name: 'Dispositivo' }).click();
  await page.locator('#deviceSelect').selectOption('atlas-physical');
  await expect(page.locator('#deviceName')).toHaveText('atlasled-test');
  await page.locator('#sendButton').click();
  await expect.poll(() => uploads.length).toBe(2);

  expect(uploads.map(value => value.device)).toEqual(['atlas-simulator', 'atlas-physical']);
  expect(uploads[1].body.scene).toEqual(uploads[0].body.scene);
  await page.getByRole('tab', { name: 'Documento' }).click();
  await page.locator('#brightness').fill('40');
  await page.locator('#stopDeviceButton').click();
  await expect.poll(() => commands.length).toBe(2);
  expect(commands).toEqual([{ kind: 'brightness', body: { protocolVersion: 1, brightness: 102 } },
    { kind: 'stop', body: { protocolVersion: 1 } }]);
});

test('CanvasMismatch bloquea el envío físico incompatible', async ({ page }) => {
  let uploads = 0;
  const incompatible = { ...physical, capabilities: capabilities('atlas-physical', 64, 16) };
  await page.route('**/api/devices', route => route.fulfill({ json: [simulator, incompatible] }));
  await page.route('**/api/status', route => route.fulfill({ json: status('atlas-simulator') }));
  await page.route('**/api/output', route => route.fulfill({ json: { ...status('atlas-simulator'), snapshot: null } }));
  await page.route('**/api/devices/atlas-physical/status', route => route.fulfill({ json: status('atlas-physical', 64, 16) }));
  await page.route('**/api/devices/atlas-physical/output', route => route.fulfill({ json: { ...status('atlas-physical', 64, 16), snapshot: null } }));
  await page.route('**/api/devices/atlas-physical/scene', route => { uploads++; return route.fulfill({ status: 204 }); });

  await page.goto('/');
  await page.getByRole('tab', { name: 'Dispositivo' }).click();
  await page.locator('#deviceSelect').selectOption('atlas-physical');
  await expect(page.locator('#deviceDetails')).toContainText('64 × 16', { timeout: 3000 });
  await page.locator('#sendButton').click();
  await expect(page.locator('#toast')).toContainText('32×16');
  expect(uploads).toBe(0);
});

test('dispositivo físico sin respuesta aparece offline sin afectar el editor', async ({ page }) => {
  await page.route('**/api/devices', route => route.fulfill({ json: [simulator, physical] }));
  await page.route('**/api/status', route => route.fulfill({ json: status('atlas-simulator') }));
  await page.route('**/api/output', route => route.fulfill({ json: { ...status('atlas-simulator'), snapshot: null } }));
  await page.route('**/api/devices/atlas-physical/status', route => route.fulfill({ status: 503, json: { title: 'DeviceUnavailable' } }));
  await page.route('**/api/devices/atlas-physical/output', route => route.fulfill({ status: 503 }));

  await page.goto('/');
  await page.getByRole('tab', { name: 'Dispositivo' }).click();
  await page.locator('#deviceSelect').selectOption('atlas-physical');
  await expect(page.locator('#deviceState')).toContainText('Offline', { timeout: 3000 });
  await expect(page.locator('#pixelCanvas')).toBeVisible();
});
