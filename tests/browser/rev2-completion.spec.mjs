import { test, expect } from '@playwright/test';

const caps = (device, width = 32, height = 16) => ({ protocolVersion: 1, device, width, height, colorModel: 0,
  features: ['scenes'], drivers: ['test'], sceneStorageBytes: 1024 });
const runtime = device => ({ protocolVersion: 1, device: caps(device), runtime: { isPlaying: false,
  activeSceneId: null, activeSceneName: null, brightness: 255, positionSeconds: 0, lastError: null } });

test('icono real persiste, aparece en timeline y serializa timing/blink como píxeles', async ({ page }) => {
  let payload;
  page.on('dialog', dialog => dialog.type() === 'prompt' ? dialog.accept('pc') : dialog.accept());
  await page.route('**/api/scene', async route => { payload = route.request().postDataJSON(); await route.fulfill({ json: { sceneId: payload.scene.id } }); });

  await page.goto('/');
  await page.locator('[data-tool="icon"]').click();
  await expect(page.locator('#objectList')).toContainText('Icono pc');
  await expect(page.locator('#objectTimeline .timeline-row')).toHaveCount(1);
  await page.locator('[data-timing="startSeconds"]').fill('6');
  await page.locator('[data-timing="startSeconds"]').press('Tab');
  await page.locator('[data-timing="durationSeconds"]').fill('3');
  await page.locator('[data-timing="durationSeconds"]').press('Tab');
  await page.locator('[data-timing="animationKind"]').selectOption('blink');
  await page.locator('[data-timing="blinkPeriodSeconds"]').fill('0.5');
  await page.locator('[data-timing="blinkPeriodSeconds"]').press('Tab');
  await page.locator('#sendButton').click();
  await expect.poll(() => payload).toBeTruthy();

  const elements = payload.scene.layers.flatMap(layer => layer.elements);
  expect(elements.length).toBeGreaterThan(10);
  expect(elements.every(element => element.kind === 0 && element.startMilliseconds === 6000 &&
    element.durationMilliseconds === 3000 && element.elementAnimation === 2 && element.blinkPeriodMilliseconds === 500)).toBeTruthy();

  await page.locator('#saveButton').click();
  await page.reload();
  await expect(page.locator('#objectList')).toContainText('Icono pc');
  await expect(page.locator('#objectTimeline .timeline-row')).toHaveCount(1);
});

test('timeline selecciona objeto y el Inspector conserva timing, duración y marquee', async ({ page }) => {
  await page.goto('/');
  await page.locator('[data-tool="text"]').click();
  const canvas = page.locator('#pixelCanvas'), box = await canvas.boundingBox();
  await page.mouse.click(box.x + 10, box.y + 10);
  await page.locator('#itText').fill('MG SOLUTIONS');
  await page.locator('#itText').press('Control+Enter');
  await page.locator('[data-timing="startSeconds"]').fill('1.5');
  await page.locator('[data-timing="startSeconds"]').press('Tab');
  await page.locator('[data-timing="durationSeconds"]').fill('4.5');
  await page.locator('[data-timing="durationSeconds"]').press('Tab');
  await page.locator('[data-timing="animationKind"]').selectOption('marquee');
  await page.locator('[data-timing="animationSpeed"]').fill('7');
  await page.locator('[data-timing="animationSpeed"]').press('Tab');

  await page.getByRole('tab', { name: 'Documento' }).click();
  await page.locator('#objectTimeline .timeline-row').click();
  await expect(page.locator('#objectTimeline .timeline-row')).toHaveClass(/selected/);
  await page.getByRole('tab', { name: 'Objeto' }).click();
  await expect(page.getByRole('tab', { name: 'Objeto' })).toHaveAttribute('aria-selected', 'true');
  await expect(page.locator('[data-timing="startSeconds"]')).toHaveValue('1.5');
  await expect(page.locator('[data-timing="durationSeconds"]')).toHaveValue('4.5');
  await expect(page.locator('[data-timing="animationKind"]')).toHaveValue('marquee');
  await expect(page.locator('[data-timing="animationSpeed"]')).toHaveValue('7');
});

test('glyph no soportado se muestra y bloquea Apply y Send', async ({ page }) => {
  await page.goto('/');
  await page.locator('#quickSignButton').click();
  await page.locator('#textValue').fill('PRECIO €');
  await page.locator('#textApply').click();
  await expect(page.locator('#glyphError')).toBeVisible();
  await expect(page.locator('#glyphError')).toContainText('€');
  await expect(page.locator('#textDialog')).toBeVisible();
  await expect(page.locator('#objectList .object-row')).toHaveCount(0);
});

test('catálogo periódico elimina un dispositivo desaparecido y vuelve al simulador', async ({ page }) => {
  const simulator = { id: 'atlas-simulator', name: 'Simulador local', kind: 'simulator', online: true,
    route: { status: '/api/status', output: '/api/output', scene: '/api/scene' }, capabilities: caps('atlas-simulator') };
  const physical = { id: 'atlas-physical', name: 'atlasled-test', kind: 'physical', online: true,
    address: '10.0.0.8', capabilities: caps('atlas-physical') };
  let catalogs = 0;
  await page.route('**/api/devices', route => route.fulfill({ json: ++catalogs === 1 ? [simulator, physical] : [simulator] }));
  await page.route('**/api/devices/atlas-physical/status', route => route.fulfill({ json: runtime('atlas-physical') }));
  await page.route('**/api/devices/atlas-physical/output', route => route.fulfill({ json: { ...runtime('atlas-physical'), snapshot: null } }));

  await page.goto('/');
  await page.getByRole('tab', { name: 'Dispositivo' }).click();
  await page.locator('#deviceSelect').selectOption('atlas-physical');
  await expect(page.locator('#deviceName')).toHaveText('atlasled-test');
  await expect(page.locator('#deviceSelect option')).toHaveCount(1, { timeout: 5000 });
  await expect(page.locator('#deviceSelect')).toHaveValue('atlas-simulator');
  await expect(page.locator('#deviceName')).toHaveText('Simulador local', { timeout: 3000 });
});

test('fallo transitorio del refresco conserva catálogo y selección actuales', async ({ page }) => {
  const simulator = { id: 'atlas-simulator', name: 'Simulador local', kind: 'simulator', online: true,
    route: { status: '/api/status', output: '/api/output', scene: '/api/scene' }, capabilities: caps('atlas-simulator') };
  const physical = { id: 'atlas-physical', name: 'atlasled-test', kind: 'physical', online: true,
    address: '10.0.0.8', capabilities: caps('atlas-physical') };
  let catalogs = 0;
  await page.route('**/api/devices', route => ++catalogs === 1 ? route.fulfill({ json: [simulator, physical] }) : route.abort());
  await page.route('**/api/devices/atlas-physical/status', route => route.fulfill({ json: runtime('atlas-physical') }));
  await page.route('**/api/devices/atlas-physical/output', route => route.fulfill({ json: { ...runtime('atlas-physical'), snapshot: null } }));
  await page.goto('/');
  await page.getByRole('tab', { name: 'Dispositivo' }).click();
  await page.locator('#deviceSelect').selectOption('atlas-physical');
  await page.waitForTimeout(2400);
  await expect(page.locator('#deviceSelect option')).toHaveCount(2);
  await expect(page.locator('#deviceSelect')).toHaveValue('atlas-physical');
});

test('fuentes 3x5 y 5x7 distinguen glyphs críticos en la Scene enviada', async ({ page }) => {
  const payloads = [];
  page.on('dialog', dialog => dialog.accept());
  await page.route('**/api/scene', async route => { const body = route.request().postDataJSON(); payloads.push(body); await route.fulfill({ json: { sceneId: body.scene.id } }); });
  await page.goto('/');
  await page.locator('#quickSignButton').click();
  await page.locator('#textValue').fill('MHNWO0S5IL');
  await page.locator('#textFont').selectOption('3x5');
  await page.locator('#textApply').click();
  await page.locator('#sendButton').click();
  await expect.poll(() => payloads.length).toBe(1);
  let rows = payloads[0].scene.layers[0].elements[0].font.rows;
  for (const [left, right] of [['M','H'],['M','N'],['O','0'],['S','5'],['I','L']]) expect(rows[left]).not.toEqual(rows[right]);

  await page.locator('#itFont').selectOption('5x7');
  await page.locator('#sendButton').click();
  await expect.poll(() => payloads.length).toBe(2);
  rows = payloads[1].scene.layers[0].elements[0].font.rows;
  for (const [left, right] of [['M','H'],['M','N'],['O','0'],['S','5'],['I','L']]) expect(rows[left]).not.toEqual(rows[right]);
});

test('capa nueva queda activa y borrar el último objeto no bloquea recreación', async ({ page }) => {
  page.on('dialog', dialog => dialog.type() === 'prompt' ? dialog.accept('heart') : dialog.accept());
  await page.goto('/');
  await page.getByRole('tab', { name: 'Objeto' }).click();
  await page.locator('#addLayerButton').click();
  await page.locator('[data-tool="icon"]').click();
  await expect(page.locator('.layer-group:has(.layer-row.active)')).toContainText('Icono heart');
  await page.locator('#deleteObjectButton').click();
  await expect(page.locator('.object-row')).toHaveCount(0);
  await page.locator('[data-tool="icon"]').click();
  await expect(page.locator('.layer-group:has(.layer-row.active)')).toContainText('Icono heart');
});
