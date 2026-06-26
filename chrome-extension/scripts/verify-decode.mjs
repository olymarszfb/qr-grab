import jsQR from 'jsqr';
import QRCode from 'qrcode';

const payload = 'https://example.test/qr-grab-extension-verify';
const qr = QRCode.create(payload, { errorCorrectionLevel: 'M' });
const moduleCount = qr.modules.size;
const scale = 8;
const margin = 4;
const width = (moduleCount + margin * 2) * scale;
const data = new Uint8ClampedArray(width * width * 4);

for (let y = 0; y < width; y += 1) {
  for (let x = 0; x < width; x += 1) {
    const moduleX = Math.floor(x / scale) - margin;
    const moduleY = Math.floor(y / scale) - margin;
    const isQrModule =
      moduleX >= 0 &&
      moduleX < moduleCount &&
      moduleY >= 0 &&
      moduleY < moduleCount &&
      qr.modules.get(moduleX, moduleY);
    const value = isQrModule ? 0 : 255;
    const index = (y * width + x) * 4;
    data[index] = value;
    data[index + 1] = value;
    data[index + 2] = value;
    data[index + 3] = 255;
  }
}

const result = jsQR(data, width, width, { inversionAttempts: 'attemptBoth' });

if (result?.data !== payload) {
  throw new Error(`Expected ${payload}, got ${result?.data || 'no decode'}`);
}

console.log(`Verified QR decode: ${result.data}`);
