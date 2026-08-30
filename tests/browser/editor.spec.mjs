import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page, request }) => {
  await request.post('/api/stop', { data: { protocolVersion: 1 } });
  await page.goto('/');
  await expect(page.locator('#deviceState')).toContainText('Conectado');
});

test('texto editable, persistencia, undo/redo y preview separado del envío', async ({ page }) => {
  await page.getByRole('button', { name: 'T Texto' }).click();
  const canvas = page.locator('#pixelCanvas');
  const box = await canvas.boundingBox();
  await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
  await page.locator('#textValue').fill('ATLAS');
  await page.getByRole('button', { name: 'Aplicar' }).click();
  await expect(page.locator('.object-row')).toContainText('ATLAS');

  await page.getByRole('button', { name: '↶ Deshacer' }).click();
  await expect(page.locator('.object-row')).toHaveCount(0);
  await page.getByRole('button', { name: '↷ Rehacer' }).click();
  await expect(page.locator('.object-row')).toHaveCount(1);

  await page.getByRole('button', { name: 'Guardar', exact: true }).click();
  await page.getByRole('button', { name: '▶' }).click();
  await expect(page.locator('#runtimeState')).toHaveText('STOPPED');
  await page.getByRole('button', { name: '■' }).click();
  await page.reload();
  await expect(page.locator('.object-row')).toContainText('ATLAS');
});

test('envío estable, salida visible y stop persistente', async ({ page }) => {
  await page.getByRole('button', { name: '✎ Lápiz' }).click();
  const box = await page.locator('#pixelCanvas').boundingBox();
  await page.mouse.move(box.x + 10, box.y + 10);
  await page.mouse.down();
  await page.mouse.move(box.x + 100, box.y + 60, { steps: 5 });
  await page.mouse.up();
  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: 'Enviar', exact: true }).click();
  await expect(page.locator('#runtimeState')).toHaveText('PLAYING');
  await expect(page.locator('#outputScene')).not.toHaveText('Sin escena');
  await page.getByRole('button', { name: 'Detener dispositivo' }).click();
  await expect(page.locator('#runtimeState')).toHaveText('STOPPED');
});

test('API rechaza payloads incompatibles sin error 500', async ({ request }) => {
  const response = await request.post('/api/design', { data: {
    protocolVersion: 1, sceneId: '00000000-0000-0000-0000-000000000000',
    name: '', width: 1, height: 1, pixels: [], brightness: 10,
    animation: 'desconocida', durationSeconds: 1, speed: 1, repeat: false
  }});
  expect(response.status()).toBe(400);
  expect((await response.json()).title).toBe('InvalidSceneId');
});

test('todas las herramientas producen objetos y clear es reversible', async ({ page }) => {
  const canvas = page.locator('#pixelCanvas');
  const box = await canvas.boundingBox();
  const drag = async (name, ax, ay, bx, by) => {
    await page.getByRole('button', { name }).click();
    const currentBox = await canvas.boundingBox();
    await page.mouse.move(currentBox.x + ax, currentBox.y + ay);
    await page.mouse.down();
    await page.mouse.move(currentBox.x + bx, currentBox.y + by, { steps: 7 });
    await page.mouse.up();
  };
  await page.getByRole('button', { name: '◩ Relleno' }).click();
  const fillBox = await canvas.boundingBox();
  await canvas.click({ position: { x: fillBox.width - 20, y: fillBox.height - 20 } });
  await drag('✎ Lápiz', 20, 20, 180, 70);
  await drag('⌫ Borrador', 60, 30, 90, 50);
  await drag('╱ Línea', 30, 100, 180, 160);
  await drag('□ Rectángulo', 110, 30, 180, 90);
  await drag('○ Círculo', 190, 40, 270, 100);
  await expect(page.locator('.object-row')).toHaveCount(6);

  await canvas.dispatchEvent('pointerdown', { pointerId: 19, clientX: box.x + 10, clientY: box.y + 10, bubbles: true });
  await canvas.dispatchEvent('pointercancel', { pointerId: 19, clientX: box.x + 10, clientY: box.y + 10, bubbles: true });
  await page.getByRole('button', { name: '⌧ Limpiar' }).click();
  await page.getByRole('button', { name: 'Confirmar' }).click();
  await expect(page.locator('.object-row')).toHaveCount(0);
  await page.getByRole('button', { name: '↶ Deshacer' }).click();
  await expect(page.locator('.object-row')).toHaveCount(6);
});

test('fuentes bitmap, edición de texto, imagen y biblioteca multiproyecto', async ({ page }) => {
  const canvas = page.locator('#pixelCanvas');
  const box = await canvas.boundingBox();
  const addText = async (text, font) => {
    await page.getByRole('button', { name: 'T Texto' }).click();
    await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
    await page.locator('#textValue').fill(text);
    await page.locator('#textFont').selectOption(font);
    await page.getByRole('button', { name: 'Aplicar' }).click();
  };
  await addText('ABC', '3x5');
  await addText('TEXTO MUY LARGO SIN TRUNCAR', '5x7');
  await page.locator('.object-row').first().click();
  await page.getByRole('button', { name: '↖ Selección' }).click();
  await page.mouse.dblclick(box.x + box.width / 2, box.y + box.height / 2);
  if (await page.locator('#textDialog').isVisible()) {
    await page.locator('#textValue').fill('EDITADO');
    await page.getByRole('button', { name: 'Aplicar' }).click();
  }

  const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAFElEQVR42mP8z8AARAwMjDAGAAANHQEDasKb6QAAAABJRU5ErkJggg==', 'base64');
  await page.locator('#imageInput').setInputFiles({ name: 'pixel.png', mimeType: 'image/png', buffer: png });
  await expect(page.locator('#objectList')).toContainText('pixel.png');
  await page.getByRole('button', { name: /pixel\.pngimage/ }).dblclick();
  await expect(page.locator('#imageDialog')).toBeVisible();
  await page.locator('#imageWidth').fill('12');
  await page.locator('#imageHeight').fill('8');
  await page.locator('#imageFit').selectOption('cover');
  await page.locator('#imageCropLeft').fill('1');
  await page.getByRole('button', { name: 'Aplicar' }).click();

  let semanticPayload;
  page.on('request', request => { if (request.url().endsWith('/api/scene')) semanticPayload = request.postDataJSON(); });
  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: 'Enviar', exact: true }).click();
  await expect(page.locator('#runtimeState')).toHaveText('PLAYING');
  expect(semanticPayload.scene.layers).toHaveLength(3);
  expect(semanticPayload.scene.layers.flatMap(layer => layer.elements).some(element => element.kind === 4)).toBeTruthy();
  expect(semanticPayload.scene.layers.flatMap(layer => layer.elements).some(element => element.kind === 5)).toBeTruthy();

  await page.locator('#designName').fill('Diseño A');
  await expect(page.locator('#dirtyIndicator')).toBeVisible();
  await page.getByRole('button', { name: 'Guardar', exact: true }).click();
  await page.getByRole('button', { name: 'Proyectos' }).click();
  await page.getByRole('button', { name: 'Guardar como' }).click();
  await page.locator('#designName').fill('Diseño B');
  await page.getByRole('button', { name: 'Guardar', exact: true }).click();
  await page.getByRole('button', { name: 'Proyectos' }).click();
  await expect(page.locator('.project-card')).toHaveCount(2);
  await expect(page.locator('.project-gallery')).toContainText('Diseño A');
  await page.getByRole('button', { name: 'Eliminar' }).click();
  await page.getByRole('button', { name: 'Confirmar' }).click();
  await expect(page.locator('.project-card')).toHaveCount(1);
});

test('preview animado no envía, offline se muestra y snapshot refleja envío', async ({ page, request }) => {
  const canvas = page.locator('#pixelCanvas');
  const box = await canvas.boundingBox();
  await page.getByRole('button', { name: '✎ Lápiz' }).click();
  const drawBox = await canvas.boundingBox();
  await page.mouse.click(drawBox.x + 40, drawBox.y + 40);
  await expect(page.locator('.object-row')).toHaveCount(1);
  await page.locator('#animation').selectOption('blink');
  let designCalls = 0;
  page.on('request', req => { if (req.url().endsWith('/api/design')) designCalls++; });
  await page.getByRole('button', { name: '▶' }).click();
  await expect(page.locator('#currentTime')).not.toHaveText('00:00.0');
  expect(designCalls).toBe(0);
  await page.getByRole('button', { name: '■' }).click();

  await page.route('**/api/status', route => route.abort());
  await page.route('**/api/snapshot', route => route.abort());
  await expect(page.locator('#deviceState')).toContainText('Offline', { timeout: 2500 });
  await page.unroute('**/api/status');
  await page.unroute('**/api/snapshot');
  await expect(page.locator('#deviceState')).toContainText('Conectado', { timeout: 2500 });

  await page.locator('#animation').selectOption('none');
  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: 'Enviar', exact: true }).click();
  await expect(page.locator('#runtimeState')).toHaveText('PLAYING');
  const snapshot = await (await request.get('/api/snapshot')).json();
  expect(snapshot.pixels.some(pixel => pixel.isOn)).toBeTruthy();
});

test('transition y easing se previsualizan y viajan en SceneDocument', async ({ page }) => {
  const canvas = page.locator('#pixelCanvas');
  await page.getByRole('button', { name: '✎ Lápiz' }).click();
  const box = await canvas.boundingBox();
  await page.mouse.click(box.x + 30, box.y + 30);
  await page.locator('#transition').selectOption('wipe');
  await page.locator('#transitionDuration').fill('1.2');
  await page.locator('#easing').selectOption('3');
  await page.getByRole('button', { name: '▶' }).click();
  await expect(page.locator('#currentTime')).not.toHaveText('00:00.0');
  await page.getByRole('button', { name: '■' }).click();
  let payload;
  page.on('request', request => { if (request.url().endsWith('/api/scene')) payload = request.postDataJSON(); });
  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: 'Enviar', exact: true }).click();
  await expect(page.locator('#runtimeState')).toHaveText('PLAYING');
  expect(payload.scene.transition).toEqual({ kind: 1, durationMilliseconds: 1200, easing: 3 });
});
