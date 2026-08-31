import { test, expect } from '@playwright/test';

const protocolVersion = 1;

function validScene(overrides = {}) {
  return {
    protocolVersion,
    id: crypto.randomUUID(),
    name: 'Hardening test',
    width: 32,
    height: 16,
    durationMilliseconds: 1000,
    colorModel: 0,
    layers: [
      {
        name: 'Capa',
        visible: true,
        elements: []
      }
    ],
    animations: [],
    transition: null,
    ...overrides
  };
}

async function expectControlledClientError(response) {
  expect(
    response.status(),
    `Se esperaba HTTP 4xx controlado, pero respondió ${response.status()}`
  ).toBeGreaterThanOrEqual(400);

  expect(
    response.status(),
    `La API devolvió HTTP ${response.status()}, posible excepción no controlada`
  ).toBeLessThan(500);
}

test.describe('AtlasLetrero hardening / pruebas destructivas', () => {

  test.beforeEach(async ({ request }) => {
    // Dejar el simulador en estado conocido.
    await request.post('/api/stop', {
      data: { protocolVersion }
    });
  });

  test('scene con layers null nunca debe producir 500', async ({ request }) => {
    const response = await request.post('/api/scene', {
      data: {
        protocolVersion,
        brightness: 128,
        scene: validScene({
          layers: null
        })
      }
    });

    await expectControlledClientError(response);
  });

  test('scene con animations null nunca debe producir 500', async ({ request }) => {
    const response = await request.post('/api/scene', {
      data: {
        protocolVersion,
        brightness: 128,
        scene: validScene({
          animations: null
        })
      }
    });

    await expectControlledClientError(response);
  });

  test('scene con elements null nunca debe producir 500', async ({ request }) => {
    const response = await request.post('/api/scene', {
      data: {
        protocolVersion,
        brightness: 128,
        scene: validScene({
          layers: [
            {
              name: 'Capa rota',
              visible: true,
              elements: null
            }
          ]
        })
      }
    });

    await expectControlledClientError(response);
  });

  test('play con protocolo inválido nunca debe producir 500', async ({ request }) => {
    const response = await request.post('/api/play', {
      data: {
        protocolVersion: 999,
        sceneId: crypto.randomUUID()
      }
    });

    await expectControlledClientError(response);
  });

  test('stop con protocolo inválido nunca debe producir 500', async ({ request }) => {
    const response = await request.post('/api/stop', {
      data: {
        protocolVersion: 999
      }
    });

    await expectControlledClientError(response);
  });

  test('brightness con protocolo inválido nunca debe producir 500', async ({ request }) => {
    const response = await request.post('/api/brightness', {
      data: {
        protocolVersion: 999,
        brightness: 128
      }
    });

    await expectControlledClientError(response);
  });

  test('frame binario corrupto nunca debe producir 500', async ({ request }) => {
    const response = await request.post('/api/frame', {
      headers: {
        'Content-Type': 'application/octet-stream'
      },
      data: Buffer.from([
        0x42, 0x41, 0x44, 0x21,
        0x01, 0x00, 0x00, 0x00
      ])
    });

    await expectControlledClientError(response);
  });

  test('scene con transición enum inválida nunca debe producir 500', async ({ request }) => {
    const response = await request.post('/api/scene', {
      data: {
        protocolVersion,
        brightness: 128,
        scene: validScene({
          transition: {
            kind: 99,
            durationMilliseconds: 500,
            easing: 0
          }
        })
      }
    });

    await expectControlledClientError(response);
  });

  test('scene excesivamente compleja es rechazada controladamente', async ({ request }) => {
    const elements = Array.from({ length: 10001 }, (_, index) => ({
      kind: 0,
      x: index % 32,
      y: Math.floor(index / 32) % 16,
      x2: 0,
      y2: 0,
      width: 0,
      height: 0,
      radius: 0,
      color: {
        r: 255,
        g: 255,
        b: 255,
        w: 0
      },
      fill: false
    }));

    const response = await request.post('/api/scene', {
      data: {
        protocolVersion,
        brightness: 128,
        scene: validScene({
          layers: [
            {
              name: 'Demasiados elementos',
              visible: true,
              elements
            }
          ]
        })
      }
    });

    await expectControlledClientError(response);
  });

  test('fuente 8x8 debe viajar realmente como 8x8', async ({ page }) => {
    await page.goto('/');

    await expect(page.locator('#deviceState'))
      .toContainText('Conectado');

    const canvas = page.locator('#pixelCanvas');
    const box = await canvas.boundingBox();

    expect(box).not.toBeNull();

    await page.getByRole('button', { name: 'T Texto' }).click();

    await page.mouse.click(
      box.x + box.width / 2,
      box.y + box.height / 2
    );

    await page.locator('#itText').fill('A');
    await page.locator('#itFont').selectOption('8x8');
    await page.locator('#itText').press('Control+Enter');

    let payload = null;

    page.on('request', request => {
      if (request.url().endsWith('/api/scene')) {
        payload = request.postDataJSON();
      }
    });

    page.once('dialog', dialog => dialog.accept());

    await page.getByRole('button', {
      name: 'Enviar',
      exact: true
    }).click();

    await expect(page.locator('#runtimeState'))
      .toHaveText('PLAYING');

    expect(payload).not.toBeNull();

    const textElement = payload.scene.layers
      .flatMap(layer => layer.elements)
      .find(element => element.kind === 4);

    expect(textElement).toBeTruthy();

    expect(
      textElement.font.width,
      'La fuente seleccionada 8x8 fue serializada con otro ancho'
    ).toBe(8);

    expect(
      textElement.font.height,
      'La fuente seleccionada 8x8 fue serializada con otra altura'
    ).toBe(8);
  });

  test('fuente 10x14 debe viajar realmente como 10x14', async ({ page }) => {
    await page.goto('/');

    await expect(page.locator('#deviceState'))
      .toContainText('Conectado');

    const canvas = page.locator('#pixelCanvas');
    const box = await canvas.boundingBox();

    expect(box).not.toBeNull();

    await page.getByRole('button', { name: 'T Texto' }).click();

    await page.mouse.click(
      box.x + box.width / 2,
      box.y + box.height / 2
    );

    await page.locator('#itText').fill('A');
    await page.locator('#itFont').selectOption('10x14');
    await page.locator('#itText').press('Control+Enter');

    let payload = null;

    page.on('request', request => {
      if (request.url().endsWith('/api/scene')) {
        payload = request.postDataJSON();
      }
    });

    page.once('dialog', dialog => dialog.accept());

    await page.getByRole('button', {
      name: 'Enviar',
      exact: true
    }).click();

    await expect(page.locator('#runtimeState'))
      .toHaveText('PLAYING');

    expect(payload).not.toBeNull();

    const textElement = payload.scene.layers
      .flatMap(layer => layer.elements)
      .find(element => element.kind === 4);

    expect(textElement).toBeTruthy();

    expect(
      textElement.font.width,
      'La fuente seleccionada 10x14 fue serializada con otro ancho'
    ).toBe(10);

    expect(
      textElement.font.height,
      'La fuente seleccionada 10x14 fue serializada con otra altura'
    ).toBe(14);
  });

  test('letrero rápido mide overflow, activa marquee y conserva multilinea', async ({ page }) => {
    await page.goto('/');
    await page.locator('#quickSignButton').click();
    await expect(page.locator('#textDialog')).toBeVisible();
    await page.locator('#textValue').fill('SE ARREGLAN COMPUTADORAS');
    await expect(page.locator('#textMeasurements')).toContainText('Tamaño texto:');
    await expect(page.locator('#textMeasurements')).toContainText('Matriz: 32 × 16 px');
    await expect(page.locator('#textScroll')).toBeChecked();
    await expect(page.locator('#textPreview')).toHaveAttribute('width', '32');
    await expect(page.locator('#textPreview')).toHaveAttribute('height', '16');

    await page.locator('#textValue').fill('SE ARREGLAN\nCOMPUTADORAS');
    await expect(page.locator('#textMeasurements')).toContainText('× 15 px');
  });

  test('output combina runtime y snapshot sin peticiones concurrentes visibles', async ({ page, request }) => {
    await page.goto('/');
    const response = await request.get('/api/output');
    expect(response.ok()).toBeTruthy();
    const output = await response.json();
    expect(output.protocolVersion).toBe(1);
    expect(output.runtime).toHaveProperty('positionSeconds');
    expect(output.snapshot.width).toBe(32);
    expect(output.snapshot.height).toBe(16);
    await expect(page.locator('#followOutputButton')).toBeVisible();
    await expect(page.locator('#outputMatch')).toBeVisible();
  });

  test('capas permiten crear, renombrar, ocultar, bloquear, duplicar, reordenar y deshacer', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('tab', { name: 'Objeto' }).click();
    await expect(page.locator('.layer-row')).toHaveCount(1);
    await page.locator('#addLayerButton').click();
    await expect(page.locator('.layer-row')).toHaveCount(2);
    await page.locator('#undoButton').click();
    await expect(page.locator('.layer-row')).toHaveCount(1);
    await page.locator('#redoButton').click();
    await expect(page.locator('.layer-row')).toHaveCount(2);

    let top = page.locator('.layer-row').first();
    let promptStep = 0;
    const renameDialogs = dialog => dialog.accept(promptStep++ === 0 ? 'renombrar' : 'Anuncios');
    page.on('dialog', renameDialogs);
    await top.getByTitle('Menú de capa').click();
    page.off('dialog', renameDialogs);
    await expect(page.locator('#objectList')).toContainText('Anuncios');
    await top.getByTitle('Visibilidad').click();
    await top.getByTitle('Bloqueo').click();
    await expect(top.getByTitle('Bloqueo')).toContainText('🔒');

    page.once('dialog', dialog => dialog.accept('duplicar'));
    await top.getByTitle('Menú de capa').click();
    await expect(page.locator('.layer-row')).toHaveCount(3);
    const source = page.locator('.layer-row').first();
    const target = page.locator('.layer-row').last();
    await source.dragTo(target);
    await expect(page.locator('.layer-row')).toHaveCount(3);
    page.once('dialog', dialog => dialog.accept('eliminar'));
    await page.locator('.layer-row').first().getByTitle('Menú de capa').click();
    await expect(page.locator('.layer-row')).toHaveCount(2);
  });

  test('proyecto legacy migra objects a layers y persiste schemaVersion', async ({ page }) => {
    await page.goto('/');
    const id = crypto.randomUUID();
    await page.evaluate(async legacy => {
      const database = await new Promise((resolve, reject) => {
        const request = indexedDB.open('atlas-letrero', 1);
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
      });
      await new Promise((resolve, reject) => {
        const request = database.transaction('projects', 'readwrite').objectStore('projects').put(legacy);
        request.onsuccess = resolve;
        request.onerror = () => reject(request.error);
      });
      localStorage.setItem('atlas-current-project', legacy.id);
    }, { id, sceneId: crypto.randomUUID(), version: 1, name: 'Legacy', width: 32, height: 16,
      brightness: 72, duration: 6, speed: 1, animation: 'none', repeat: true,
      objects: [{ id: crypto.randomUUID(), type: 'line', name: 'Legacy line', visible: true,
        x: 0, y: 0, x2: 2, y2: 2, color: '#ffffff' }], createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() });
    await page.reload();
    await expect(page.locator('#objectList')).toContainText('Legacy line');
    const stored = await page.evaluate(async projectId => {
      const database = await new Promise(resolve => { const request = indexedDB.open('atlas-letrero', 1); request.onsuccess = () => resolve(request.result); });
      return await new Promise(resolve => { const request = database.transaction('projects').objectStore('projects').get(projectId); request.onsuccess = () => resolve(request.result); });
    }, id);
    expect(stored.schemaVersion).toBe(2);
    expect(stored.layers[0].objects).toHaveLength(1);
    expect(stored.objects).toBeUndefined();
  });

  test('texto se edita en inspector lateral con preview vivo y Escape revierte', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('button', { name: 'T Texto' }).click();
    const canvas = page.locator('#pixelCanvas');
    const box = await canvas.boundingBox();
    await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
    await expect(page.locator('#textDialog')).not.toBeVisible();
    await expect(page.locator('#textInspector')).toBeVisible();
    await expect(page.locator('#itText')).toBeFocused();
    await page.locator('#itText').fill('BORRADOR');
    await expect(page.locator('#pixelCanvas')).toBeVisible();
    await page.locator('#itText').press('Escape');
    await expect(page.locator('#itText')).toHaveValue('');
    await page.locator('#itText').fill('CONFIRMADO');
    await page.locator('#itText').press('Control+Enter');
    await expect(page.locator('#objectList')).toContainText('CONFIRMADO');
  });

  test('herramientas pixel perfect exponen pincel, elipse, cuentagotas, zoom y atajos de selección', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('#brushSize')).toHaveValue('1');
    await expect(page.locator('#zoomPreset')).toHaveValue('100');
    await expect(page.getByRole('button', { name: /Cuentagotas/ })).toBeVisible();

    await page.locator('#brushSize').selectOption('4');
    await page.getByRole('button', { name: /Lápiz/ }).click();
    const canvas = page.locator('#pixelCanvas');
    let box = await canvas.boundingBox();
    await page.mouse.move(box.x + 20, box.y + 20);
    await page.mouse.down();
    await page.mouse.move(box.x + box.width - 20, box.y + box.height - 20, { steps: 2 });
    await page.mouse.up();
    await expect(page.locator('#objectList')).toContainText('Trazo');

    await page.keyboard.press('Control+d');
    await expect(page.locator('.object-row')).toHaveCount(2);
    await page.keyboard.press('Shift+ArrowRight');
    await page.keyboard.press('Control+c');
    await page.keyboard.press('Control+v');
    await expect(page.locator('.object-row')).toHaveCount(3);

    await page.locator('#zoomPreset').selectOption('400');
    await expect(page.locator('#zoomValue')).toHaveText('400%');
    box = await canvas.boundingBox();
    await page.mouse.move(box.x + 2, box.y + 2);
    await expect(page.locator('#cursorCoords')).toContainText('X 0');
    await page.locator('#zoomFit').click();
    await expect(page.locator('#zoomValue')).toHaveText('100%');
  });

  test('inspector contextual separa documento, objeto, animación de escena y dispositivo', async ({ page }) => {
    await page.goto('/');
    const tabs = page.locator('#contextTabs [role="tab"]');
    await expect(tabs).toHaveCount(4);
    await expect(page.locator('#context-document')).toBeVisible();
    await expect(page.locator('#context-animation')).toBeHidden();
    await page.locator('#documentName').fill('Panel contextual');
    await expect(page.locator('#designName')).toHaveValue('Panel contextual');

    await page.getByRole('tab', { name: 'Animación' }).click();
    await expect(page.locator('#context-animation')).toContainText('Animación de escena');
    await expect(page.locator('#context-animation')).toContainText('no admite animación por objeto');
    await expect(page.locator('#animation')).toBeVisible();

    await page.getByRole('button', { name: /Lápiz/ }).click();
    const canvas = page.locator('#pixelCanvas');
    const box = await canvas.boundingBox();
    await page.mouse.click(box.x + 20, box.y + 20);
    await page.getByRole('tab', { name: 'Objeto' }).click();
    await expect(page.locator('#objectContextFields')).toContainText('Trazo');
    await expect(page.locator('[data-object-field="brushSize"]')).toBeVisible();

    await page.getByRole('tab', { name: 'Dispositivo' }).click();
    await expect(page.locator('#contextConnection')).toHaveText('Conectado');
    await expect(page.locator('#contextDeviceResolution')).not.toHaveText('—');
    await expect(page.locator('#contextDriver')).not.toHaveText('—');
    await expect(page.locator('#contextSceneId')).not.toHaveText('—');
    await expect(page.locator('#contextVersion')).not.toHaveText('—');
    await expect(page.locator('#contextOutput')).not.toHaveText('—');
  });

  test('cambio de resolución requiere estrategia, muestra preview y conserva texto semántico', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('button', { name: 'T Texto' }).click();
    const canvas = page.locator('#pixelCanvas');
    let box = await canvas.boundingBox();
    await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
    await page.locator('#itText').fill('SEMANTICO');
    await page.locator('#itText').press('Control+Enter');
    await page.getByRole('tab', { name: 'Documento' }).click();

    await page.locator('#resolutionProfile').selectOption('64x16');
    await expect(page.locator('#resolutionDialog')).toBeVisible();
    await expect(page.locator('#canvasInfo')).toContainText('32 × 16');
    await expect(page.locator('#resolutionPreview')).toBeVisible();
    await expect(page.locator('#resolutionCompatibility')).toContainText('no es compatible');
    await expect(page.locator('input[name="resizeMode"]')).toHaveCount(4);
    await page.getByLabel('Escalar nearest-neighbor').check();
    await expect(page.locator('#resolutionSummary')).toContainText('64 × 16');
    await page.locator('#resolutionCancel').click();
    await expect(page.locator('#canvasInfo')).toContainText('32 × 16');

    await page.locator('#resolutionProfile').selectOption('64x16');
    await page.getByLabel('Centrar contenido').check();
    await page.locator('#resolutionApply').click();
    await expect(page.locator('#canvasInfo')).toContainText('64 × 16');
    await expect(page.locator('#objectList')).toContainText('SEMANTICO');

    let payload;
    page.on('request', request => { if (request.url().endsWith('/api/scene')) payload = request.postDataJSON(); });
    page.once('dialog', dialog => dialog.accept());
    await page.getByRole('button', { name: 'Enviar', exact: true }).click();
    await expect.poll(() => payload).toBeTruthy();
    expect(payload.scene.layers.flatMap(layer => layer.elements).some(element => element.kind === 4)).toBeTruthy();
  });

  test('device select usa selectedDevice real y sólo registra el simulador local', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('tab', { name: 'Dispositivo' }).click();
    const select = page.locator('#deviceSelect');
    await expect(select.locator('option')).toHaveCount(1);
    await expect(select).toHaveValue('atlas-simulator');
    await expect(select.locator('option')).toHaveText('Simulador local');
    await expect(page.locator('#deviceName')).toHaveText('Simulador local');

    const device = await page.evaluate(() => ({
      id: window.selectedDevice.id,
      name: window.selectedDevice.name,
      kind: window.selectedDevice.kind,
      baseUrl: window.selectedDevice.baseUrl,
      route: window.selectedDevice.route,
      online: window.selectedDevice.online,
      capabilities: window.selectedDevice.capabilities
    }));
    expect(device).toMatchObject({
      id: 'atlas-simulator', name: 'Simulador local', kind: 'simulator', baseUrl: '', online: true,
      route: { scene: '/api/scene', status: '/api/status', output: '/api/output', stop: '/api/stop' }
    });
    expect(device.capabilities).toMatchObject({ width: 32, height: 16 });

    let sceneUrl;
    page.on('request', request => { if (request.url().endsWith('/api/scene')) sceneUrl = request.url(); });
    page.once('dialog', dialog => dialog.accept());
    await page.getByRole('button', { name: 'Enviar', exact: true }).click();
    await expect.poll(() => sceneUrl).toContain('/api/scene');
    await page.getByRole('button', { name: 'Detener dispositivo' }).click();
    await expect(page.locator('#runtimeState')).toHaveText('STOPPED');
  });

  test('salida real muestra metadatos verificables, grid y ampliación', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('#outputDevice')).toHaveText('Simulador local');
    await expect(page.locator('#outputDeviceId')).toHaveText('atlas-simulator');
    await expect(page.locator('#outputResolution')).toHaveText('32 × 16');
    await expect(page.locator('#outputColorModel')).toHaveText('RGB');
    await expect(page.locator('#outputDriver')).toHaveText('simulator.virtual');
    await expect(page.locator('#outputStatus')).toHaveText(/Detenido|Reproduciendo/);
    await expect(page.locator('#outputBrightness')).toHaveText(/\d+%/);
    await expect(page.locator('#outputPosition')).not.toHaveText('—');
    await expect(page.locator('#outputFps')).toHaveText('30');
    await expect(page.locator('#outputUpdated')).not.toHaveText('—');
    await expect(page.locator('#outputCanvas')).toHaveCSS('image-rendering', 'pixelated');

    await page.getByRole('button', { name: /Lápiz/ }).click();
    const canvas = page.locator('#pixelCanvas');
    const box = await canvas.boundingBox();
    await page.mouse.click(box.x + 20, box.y + 20);
    page.once('dialog', dialog => dialog.accept());
    await page.getByRole('button', { name: 'Enviar', exact: true }).click();
    await expect(page.locator('#outputScene')).not.toHaveText('—');
    await expect(page.locator('#outputSceneId')).not.toHaveText('—');

    await page.getByRole('button', { name: 'Cuadrícula', exact: true }).click();
    await expect(page.locator('#outputGridButton')).toHaveClass(/primary/);
    await page.getByRole('button', { name: 'Ampliar salida' }).click();
    await expect(page.locator('#simulatorPanel')).toHaveClass(/expanded/);
    await expect(page.getByRole('button', { name: 'Reducir salida' })).toBeVisible();
  });

  test('proyectos muestran metadatos y autosave espera al final de la operación', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('#saveState')).toHaveText('Guardado');
    await page.evaluate(() => {
      window.__projectWrites = 0;
      const original = IDBObjectStore.prototype.put;
      IDBObjectStore.prototype.put = function (...args) {
        window.__projectWrites++;
        return original.apply(this, args);
      };
    });

    await page.getByRole('button', { name: /Lápiz/ }).click();
    const canvas = page.locator('#pixelCanvas');
    const box = await canvas.boundingBox();
    await page.mouse.move(box.x + 20, box.y + 20);
    await page.mouse.down();
    await page.mouse.move(box.x + box.width - 20, box.y + box.height - 20, { steps: 3 });
    await page.waitForTimeout(1200);
    expect(await page.evaluate(() => window.__projectWrites)).toBe(0);
    await expect(page.locator('#saveState')).toHaveText('Cambios');
    const unloadPrevented = await page.evaluate(() => {
      const event = new Event('beforeunload', { cancelable: true });
      window.dispatchEvent(event);
      return event.defaultPrevented;
    });
    expect(unloadPrevented).toBeTruthy();

    await page.mouse.up();
    await expect(page.locator('#saveState')).toHaveText('Autoguardado', { timeout: 2500 });
    expect(await page.evaluate(() => window.__projectWrites)).toBeGreaterThan(0);
    const stored = await page.evaluate(async () => {
      const database = await new Promise(resolve => { const request = indexedDB.open('atlas-letrero', 1); request.onsuccess = () => resolve(request.result); });
      const id = localStorage.getItem('atlas-current-project');
      return await new Promise(resolve => { const request = database.transaction('projects').objectStore('projects').get(id); request.onsuccess = () => resolve(request.result); });
    });
    expect(stored.schemaVersion).toBe(2);
    expect(stored.lastSaveKind).toBe('autosaved');
    expect(stored.layers.flatMap(layer => layer.objects)).toHaveLength(1);

    await page.getByRole('button', { name: 'Proyectos' }).click();
    const card = page.locator('.project-card').first();
    await expect(card.locator('canvas')).toBeVisible();
    await expect(card).toContainText('32 × 16 · 1 objetos');
    await expect(card).toContainText('Sin animación');
    await expect(card).toContainText('Autoguardado');
    const thumbnailHasPixels = await card.locator('canvas').evaluate(canvas => [...canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data].some((value, index) => index % 4 !== 3 && value > 30));
    expect(thumbnailHasPixels).toBeTruthy();
  });

  test('imagen conserva fuente y previsualiza ajustes LED sin dependencias', async ({ page }) => {
    await page.goto('/');
    const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAFElEQVR42mP8z8AARAwMjDAGAAANHQEDasKb6QAAAABJRU5ErkJggg==', 'base64');
    await page.locator('#imageInput').setInputFiles({ name: 'fuente.png', mimeType: 'image/png', buffer: png });
    await page.getByRole('button', { name: /fuente\.pngimage/ }).dblclick();
    await expect(page.locator('#imageDialog')).toBeVisible();
    await expect(page.locator('.image-previews')).toContainText('ORIGINAL');
    await expect(page.locator('.image-previews')).toContainText('RESULTADO LED');
    await expect(page.locator('#imageOriginalPreview')).toBeVisible();
    await expect(page.locator('#imageLedPreview')).toBeVisible();
    await expect(page.locator('#imageFit option')).toHaveCount(4);
    await expect(page.locator('#imageFit')).toContainText('Crop');

    const originalBefore = await page.locator('#imageOriginalPreview').evaluate(canvas => [...canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data]);
    const ledBefore = await page.locator('#imageLedPreview').evaluate(canvas => [...canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data]);
    await page.locator('#imageFit').selectOption('crop');
    await page.locator('#imageNearest').uncheck();
    await page.locator('#imageTransparent').uncheck();
    await page.locator('#imageBrightness').fill('0');
    await page.locator('#imageContrast').fill('150');
    await page.locator('#imageThreshold').fill('128');
    await page.locator('#imageDither').check();
    await expect(page.locator('#imageBrightnessValue')).toHaveText('0%');
    await expect(page.locator('#imageContrastValue')).toHaveText('150%');
    await expect(page.locator('#imageThresholdValue')).toHaveText('128');
    const originalDuring = await page.locator('#imageOriginalPreview').evaluate(canvas => [...canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data]);
    const ledAfter = await page.locator('#imageLedPreview').evaluate(canvas => [...canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data]);
    expect(originalDuring).toEqual(originalBefore);
    expect(ledAfter).not.toEqual(ledBefore);

    await page.getByRole('button', { name: 'Aplicar' }).click();
    await page.getByRole('button', { name: /fuente\.pngimage/ }).dblclick();
    const originalReopened = await page.locator('#imageOriginalPreview').evaluate(canvas => [...canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height).data]);
    expect(originalReopened).toEqual(originalBefore);
    await expect(page.locator('#imageFit')).toHaveValue('crop');
    await expect(page.locator('#imageDither')).toBeChecked();
  });

  test('responseError traduce códigos conocidos y conserva detalle técnico fuera del mensaje', async ({ page }) => {
    await page.goto('/');
    const translated = await page.evaluate(async () => {
      const make = (title, detail) => window.atlasResponseError(new Response(JSON.stringify({ title, detail }), { status: 400, headers: { 'Content-Type': 'application/json' } }));
      const protocol = await make('ProtocolVersionUnsupported', 'Protocol 99 rejected internally');
      const scene = await make('InvalidScene', 'Element 42 has an invalid payload');
      const offline = window.atlasOfflineError(new TypeError('Failed to fetch host'));
      return [protocol, scene, offline].map(error => ({ message: error.message, code: error.code, status: error.status, technicalDetail: error.technicalDetail }));
    });
    expect(translated[0]).toEqual({ message: 'Este dispositivo usa una versión incompatible.', code: 'ProtocolVersionUnsupported', status: 400, technicalDetail: 'Protocol 99 rejected internally' });
    expect(translated[1]).toEqual({ message: 'El diseño contiene datos que el dispositivo no puede reproducir.', code: 'InvalidScene', status: 400, technicalDetail: 'Element 42 has an invalid payload' });
    expect(translated[2]).toMatchObject({ message: 'El dispositivo no está disponible.', code: 'Offline', technicalDetail: 'Failed to fetch host' });

    await page.getByRole('tab', { name: 'Documento' }).click();
    await page.locator('#resolutionProfile').selectOption('64x16');
    await page.locator('#resolutionApply').click();
    const technicalLogs = [];
    page.on('console', message => { if (message.type() === 'error') technicalLogs.push(message.text()); });
    page.once('dialog', dialog => dialog.accept());
    await page.getByRole('button', { name: 'Enviar', exact: true }).click();
    await expect(page.locator('#toast')).toHaveText('El diseño es 64×16 y el dispositivo es 32×16.');
    expect(await page.locator('#toast').textContent()).not.toContain('CanvasMismatch');
    await expect.poll(() => technicalLogs.join('\n')).toContain('[CanvasMismatch]');

    await page.route('**/api/status', route => route.abort());
    await expect(page.locator('#deviceState')).toContainText('Offline', { timeout: 2500 });
    await page.getByRole('button', { name: 'Enviar', exact: true }).click();
    await expect(page.locator('#toast')).toHaveText('El dispositivo no está disponible.');
  });

  test('performance mantiene loops sin concurrencia y drag fluido en tres resoluciones', async ({ page }) => {
    await page.addInitScript(() => {
      window.__intervalCalls = 0;
      const original = window.setInterval;
      window.setInterval = function (...args) {
        window.__intervalCalls++;
        return original.apply(this, args);
      };
    });
    let outputInFlight = 0, outputMax = 0, statusInFlight = 0, statusMax = 0;
    await page.route('**/api/output', async route => {
      outputInFlight++;
      outputMax = Math.max(outputMax, outputInFlight);
      await new Promise(resolve => setTimeout(resolve, 180));
      await route.continue();
      outputInFlight--;
    });
    await page.route('**/api/status', async route => {
      statusInFlight++;
      statusMax = Math.max(statusMax, statusInFlight);
      await new Promise(resolve => setTimeout(resolve, 1100));
      await route.continue();
      statusInFlight--;
    });
    await page.goto('/');
    await expect(page.locator('#deviceState')).toContainText('Conectado', { timeout: 4000 });
    await page.waitForTimeout(2400);
    expect(outputMax).toBe(1);
    expect(statusMax).toBe(1);
    expect(await page.evaluate(() => window.__intervalCalls)).toBe(0);

    const initialDomNodes = await page.evaluate(() => document.querySelectorAll('*').length);
    const started = Date.now();
    for (const profile of ['32x16', '64x16', '128x8']) {
      await page.getByRole('tab', { name: 'Documento' }).click();
      if (await page.locator('#resolutionProfile').inputValue() !== profile) {
        await page.locator('#resolutionProfile').selectOption(profile);
        await page.locator('#resolutionApply').click();
      }
      const [width, height] = profile.split('x').map(Number);
      await expect(page.locator('#pixelCanvas')).toHaveAttribute('width', String(width));
      await expect(page.locator('#pixelCanvas')).toHaveAttribute('height', String(height));
      await page.getByRole('button', { name: /Lápiz/ }).click();
      const canvas = page.locator('#pixelCanvas');
      const box = await canvas.boundingBox();
      await page.mouse.move(box.x + 2, box.y + 2);
      await page.mouse.down();
      await page.mouse.move(box.x + box.width - 2, box.y + box.height - 2, { steps: 60 });
      await page.mouse.up();
    }
    expect(Date.now() - started).toBeLessThan(10_000);
    await expect(page.locator('.object-row')).toHaveCount(3);
    const finalDomNodes = await page.evaluate(() => document.querySelectorAll('*').length);
    expect(finalDomNodes - initialDomNodes).toBeLessThan(40);

    const before = await page.locator('#currentTime').textContent();
    await page.getByRole('button', { name: '▶' }).click();
    await expect(page.locator('#currentTime')).not.toHaveText(before);
    await page.getByRole('button', { name: '■' }).click();
  });

});
