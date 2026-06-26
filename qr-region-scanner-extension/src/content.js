import jsQR from 'jsqr';

if (!window.__qrRegionScannerLoaded) {
  window.__qrRegionScannerLoaded = true;

  const ROOT_ID = 'qr-region-scanner-root';
  const MIN_SELECTION_SIZE = 20;
  let activeCleanup = null;

  chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
    if (message?.type !== 'qr-region:start-selection') {
      return false;
    }

    startSelection();
    sendResponse({ ok: true });
    return false;
  });

  function startSelection() {
    cleanup();

    const host = document.createElement('div');
    host.id = ROOT_ID;
    document.documentElement.appendChild(host);

    const abortController = new AbortController();
    const shadow = host.attachShadow({ mode: 'open' });
    activeCleanup = () => {
      abortController.abort();
      host.remove();
      activeCleanup = null;
    };

    shadow.innerHTML = `
      <style>
        :host {
          all: initial;
          color-scheme: light;
          font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
        }

        .overlay {
          position: fixed;
          inset: 0;
          z-index: 2147483647;
          cursor: crosshair;
          background: rgba(15, 23, 42, 0.18);
          user-select: none;
        }

        .hint,
        .toast,
        .result {
          position: fixed;
          z-index: 2147483647;
          box-sizing: border-box;
          color: #111827;
          background: #ffffff;
          border: 1px solid rgba(148, 163, 184, 0.45);
          box-shadow: 0 24px 70px rgba(15, 23, 42, 0.22);
        }

        .hint {
          top: 18px;
          left: 50%;
          transform: translateX(-50%);
          display: flex;
          gap: 10px;
          align-items: center;
          max-width: min(520px, calc(100vw - 32px));
          padding: 10px 12px;
          border-radius: 8px;
          font-size: 13px;
          line-height: 1.35;
          font-weight: 650;
        }

        .hint kbd {
          padding: 2px 6px;
          border: 1px solid #cbd5e1;
          border-radius: 5px;
          background: #f8fafc;
          font: inherit;
          font-size: 11px;
          font-weight: 750;
        }

        .selection {
          position: fixed;
          z-index: 2147483647;
          display: none;
          box-sizing: border-box;
          border: 2px solid #38bdf8;
          outline: 9999px solid rgba(15, 23, 42, 0.4);
          border-radius: 6px;
          background: rgba(14, 165, 233, 0.08);
          box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.9) inset;
          pointer-events: none;
        }

        .toast {
          left: 50%;
          bottom: 22px;
          transform: translateX(-50%);
          display: none;
          max-width: min(520px, calc(100vw - 32px));
          padding: 10px 12px;
          border-radius: 8px;
          font-size: 13px;
          line-height: 1.4;
          font-weight: 650;
        }

        .result {
          left: 50%;
          top: 50%;
          transform: translate(-50%, -50%);
          display: none;
          width: min(520px, calc(100vw - 32px));
          padding: 16px;
          border-radius: 8px;
          cursor: default;
        }

        .result h2 {
          margin: 0 0 10px;
          font-size: 18px;
          line-height: 1.25;
          font-weight: 750;
          color: #111827;
        }

        .result textarea {
          box-sizing: border-box;
          width: 100%;
          min-height: 108px;
          resize: vertical;
          padding: 10px;
          border: 1px solid #cbd5e1;
          border-radius: 7px;
          color: #111827;
          background: #f8fafc;
          font: 13px/1.45 ui-monospace, SFMono-Regular, Consolas, "Liberation Mono", monospace;
        }

        .actions {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;
          justify-content: flex-end;
          margin-top: 12px;
        }

        button {
          min-width: 78px;
          height: 36px;
          padding: 0 12px;
          border: 1px solid #cbd5e1;
          border-radius: 7px;
          color: #111827;
          background: #ffffff;
          font: inherit;
          font-size: 13px;
          font-weight: 700;
          cursor: pointer;
        }

        button.primary {
          border-color: #2f6feb;
          color: #ffffff;
          background: #2f6feb;
        }

        button:hover {
          filter: brightness(0.96);
        }

        .muted {
          color: #64748b;
        }
      </style>
      <div class="overlay" part="overlay"></div>
      <div class="hint">Drag around the QR code <span class="muted">Release to scan</span> <kbd>Esc</kbd></div>
      <div class="selection"></div>
      <div class="toast"></div>
      <section class="result" role="dialog" aria-modal="true" aria-labelledby="qr-region-title">
        <h2 id="qr-region-title">QR code found</h2>
        <textarea readonly></textarea>
        <div class="actions">
          <button type="button" data-action="scan-again">Scan again</button>
          <button type="button" data-action="copy">Copy</button>
          <button type="button" class="primary" data-action="open">Open</button>
          <button type="button" data-action="close">Close</button>
        </div>
      </section>
    `;

    const overlay = shadow.querySelector('.overlay');
    const selection = shadow.querySelector('.selection');
    const toast = shadow.querySelector('.toast');
    const result = shadow.querySelector('.result');
    const textarea = shadow.querySelector('textarea');
    const openButton = shadow.querySelector('[data-action="open"]');

    let start = null;
    let currentRect = null;

    function onPointerDown(event) {
      if (event.button !== 0) {
        return;
      }

      start = clampPoint(event.clientX, event.clientY);
      currentRect = null;
      selection.style.display = 'block';
      updateSelection(start.x, start.y, start.x, start.y);
      overlay.setPointerCapture(event.pointerId);
    }

    function onPointerMove(event) {
      if (!start) {
        return;
      }

      const point = clampPoint(event.clientX, event.clientY);
      updateSelection(start.x, start.y, point.x, point.y);
    }

    async function onPointerUp(event) {
      if (!start) {
        return;
      }

      const point = clampPoint(event.clientX, event.clientY);
      updateSelection(start.x, start.y, point.x, point.y);
      start = null;

      if (!currentRect || currentRect.width < MIN_SELECTION_SIZE || currentRect.height < MIN_SELECTION_SIZE) {
        showToast('Select a larger area around the QR code.');
        selection.style.display = 'none';
        return;
      }

      await decodeSelection(currentRect);
    }

    function onKeyDown(event) {
      if (event.key === 'Escape') {
        cleanup();
      }
    }

    async function decodeSelection(rect) {
      showToast('Capturing selected area...');

      try {
        const capture = await captureVisibleTab(host);

        if (!capture?.ok) {
          throw new Error(capture?.error || 'Could not capture this tab.');
        }

        showToast('Scanning selected area...');
        const image = await loadImage(capture.dataUrl);
        const decoded = decodeFromImage(image, rect);

        if (!decoded) {
          showToast('No QR code found there. Try a slightly wider box.');
          selection.style.display = 'none';
          return;
        }

        textarea.value = decoded.data;
        openButton.style.display = isLikelyUrl(decoded.data) ? 'inline-block' : 'none';
        toast.style.display = 'none';
        selection.style.display = 'none';
        result.style.display = 'block';
        textarea.focus();
        textarea.select();
      } catch (error) {
        showToast(error?.message || 'Could not scan that area.');
        selection.style.display = 'none';
      }
    }

    function updateSelection(x1, y1, x2, y2) {
      const left = Math.min(x1, x2);
      const top = Math.min(y1, y2);
      const width = Math.abs(x2 - x1);
      const height = Math.abs(y2 - y1);

      currentRect = { left, top, width, height };
      selection.style.left = `${left}px`;
      selection.style.top = `${top}px`;
      selection.style.width = `${width}px`;
      selection.style.height = `${height}px`;
    }

    function showToast(message) {
      toast.textContent = message;
      toast.style.display = 'block';
    }

    shadow.addEventListener('click', async (event) => {
      const button = event.target.closest('button');

      if (!button) {
        return;
      }

      const action = button.dataset.action;

      if (action === 'close') {
        cleanup();
      }

      if (action === 'scan-again') {
        result.style.display = 'none';
        selection.style.display = 'none';
        toast.style.display = 'none';
      }

      if (action === 'copy') {
        await copyText(textarea.value, textarea);
        button.textContent = 'Copied';
        setTimeout(() => {
          button.textContent = 'Copy';
        }, 1200);
      }

      if (action === 'open') {
        await chrome.runtime.sendMessage({
          type: 'qr-region:open-url',
          url: normalizeUrl(textarea.value),
        });
      }
    }, { signal: abortController.signal });
    overlay.addEventListener('pointerdown', onPointerDown, { signal: abortController.signal });
    overlay.addEventListener('pointermove', onPointerMove, { signal: abortController.signal });
    overlay.addEventListener('pointerup', onPointerUp, { signal: abortController.signal });
    window.addEventListener('keydown', onKeyDown, { capture: true, signal: abortController.signal });
  }

  function decodeFromImage(image, rect) {
    const attempts = [
      { rect, padding: 0, maxSize: 1200 },
      { rect, padding: 0.12, maxSize: 1200 },
      { rect, padding: 0.28, maxSize: 1400 },
      { rect, padding: 0.5, maxSize: 1600 },
    ];

    for (const attempt of attempts) {
      const canvas = cropToCanvas(image, attempt.rect, attempt.padding, attempt.maxSize);
      const result = scanCanvas(canvas);

      if (result) {
        return result;
      }
    }

    return null;
  }

  function cropToCanvas(image, rect, paddingRatio, maxSize) {
    const scaleX = image.naturalWidth / window.innerWidth;
    const scaleY = image.naturalHeight / window.innerHeight;
    const padX = rect.width * paddingRatio;
    const padY = rect.height * paddingRatio;

    const sx = clamp((rect.left - padX) * scaleX, 0, image.naturalWidth);
    const sy = clamp((rect.top - padY) * scaleY, 0, image.naturalHeight);
    const right = clamp((rect.left + rect.width + padX) * scaleX, 0, image.naturalWidth);
    const bottom = clamp((rect.top + rect.height + padY) * scaleY, 0, image.naturalHeight);
    const sourceWidth = Math.max(1, right - sx);
    const sourceHeight = Math.max(1, bottom - sy);
    const largestSide = Math.max(sourceWidth, sourceHeight);
    const resizeScale = largestSide < maxSize * 0.55 ? maxSize / largestSide : Math.min(1, maxSize / largestSide);
    const width = Math.max(1, Math.round(sourceWidth * resizeScale));
    const height = Math.max(1, Math.round(sourceHeight * resizeScale));
    const canvas = document.createElement('canvas');
    const context = canvas.getContext('2d', { willReadFrequently: true });

    canvas.width = width;
    canvas.height = height;
    context.imageSmoothingEnabled = false;
    context.drawImage(image, sx, sy, sourceWidth, sourceHeight, 0, 0, width, height);

    return canvas;
  }

  function scanCanvas(canvas) {
    const context = canvas.getContext('2d', { willReadFrequently: true });
    const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
    const normalResult = jsQR(imageData.data, imageData.width, imageData.height, {
      inversionAttempts: 'attemptBoth',
    });

    if (normalResult) {
      return normalResult;
    }

    const thresholded = thresholdImageData(imageData);
    return jsQR(thresholded.data, thresholded.width, thresholded.height, {
      inversionAttempts: 'attemptBoth',
    });
  }

  function thresholdImageData(imageData) {
    const output = new ImageData(new Uint8ClampedArray(imageData.data), imageData.width, imageData.height);
    const data = output.data;
    let total = 0;
    const pixels = data.length / 4;

    for (let index = 0; index < data.length; index += 4) {
      total += 0.299 * data[index] + 0.587 * data[index + 1] + 0.114 * data[index + 2];
    }

    const threshold = total / pixels;

    for (let index = 0; index < data.length; index += 4) {
      const value = 0.299 * data[index] + 0.587 * data[index + 1] + 0.114 * data[index + 2] > threshold ? 255 : 0;
      data[index] = value;
      data[index + 1] = value;
      data[index + 2] = value;
      data[index + 3] = 255;
    }

    return output;
  }

  function clampPoint(x, y) {
    return {
      x: clamp(x, 0, window.innerWidth),
      y: clamp(y, 0, window.innerHeight),
    };
  }

  function clamp(value, min, max) {
    return Math.min(max, Math.max(min, value));
  }

  function loadImage(dataUrl) {
    return new Promise((resolve, reject) => {
      const image = new Image();
      image.onload = () => resolve(image);
      image.onerror = () => reject(new Error('Could not read the captured tab image.'));
      image.src = dataUrl;
    });
  }

  async function captureVisibleTab(host) {
    const previousVisibility = host.style.visibility;
    host.style.visibility = 'hidden';
    await waitForPaint();

    try {
      return await chrome.runtime.sendMessage({ type: 'qr-region:capture-visible-tab' });
    } finally {
      host.style.visibility = previousVisibility;
      await waitForPaint();
    }
  }

  function waitForPaint() {
    return new Promise((resolve) => {
      requestAnimationFrame(() => requestAnimationFrame(resolve));
    });
  }

  function isLikelyUrl(value) {
    const trimmed = value.trim();

    if (/^https?:\/\//i.test(trimmed)) {
      return true;
    }

    if (!/^[\w.-]+\.[a-z]{2,}([/:?#].*)?$/i.test(trimmed)) {
      return false;
    }

    try {
      const url = new URL(normalizeUrl(trimmed));
      return Boolean(url.hostname);
    } catch {
      return false;
    }
  }

  function normalizeUrl(value) {
    const trimmed = value.trim();

    if (/^https?:\/\//i.test(trimmed)) {
      return trimmed;
    }

    return `https://${trimmed}`;
  }

  async function copyText(value, fallbackElement) {
    try {
      await navigator.clipboard.writeText(value);
      return;
    } catch {
      fallbackElement.focus();
      fallbackElement.select();
      document.execCommand('copy');
    }
  }

  function cleanup() {
    if (activeCleanup) {
      activeCleanup();
      return;
    }

    document.getElementById(ROOT_ID)?.remove();
  }
}
