/** JPEG (.jpg / .jpeg) and PNG are the only accepted formats. */
export const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png'];
export const IMAGE_ACCEPT = '.jpg,.jpeg,.png,image/jpeg,image/png';

export interface ImageReadOptions {
  /** Longest side in pixels; larger images are scaled down. */
  maxDimension: number;
  /** Upper bound for the encoded image in bytes. */
  maxBytes: number;
}

const MAX_SOURCE_BYTES = 15 * 1024 * 1024;

function loadImage(file: File): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const url = URL.createObjectURL(file);
    const img = new Image();
    img.onload = () => {
      URL.revokeObjectURL(url);
      resolve(img);
    };
    img.onerror = () => {
      URL.revokeObjectURL(url);
      reject(new Error(`"${file.name}" could not be read as an image.`));
    };
    img.src = url;
  });
}

/** Size in bytes of the data inside a base64 data URL. */
function dataUrlBytes(dataUrl: string): number {
  const base64 = dataUrl.slice(dataUrl.indexOf(',') + 1);
  return Math.floor((base64.length * 3) / 4);
}

/**
 * Reads a JPEG/PNG file, scales it down and returns it as a base64 data URL in the same format.
 * Rejects other file types and images that cannot be shrunk under maxBytes.
 */
export async function readImageAsDataUrl(file: File, options: ImageReadOptions): Promise<string> {
  if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
    throw new Error(`"${file.name}" is not allowed. Only JPEG, JPG and PNG images can be uploaded.`);
  }
  if (file.size > MAX_SOURCE_BYTES) {
    throw new Error(`"${file.name}" is too large (max 15 MB).`);
  }

  const img = await loadImage(file);
  const outputType = file.type === 'image/png' ? 'image/png' : 'image/jpeg';

  let scale = Math.min(1, options.maxDimension / Math.max(img.naturalWidth, img.naturalHeight));
  let quality = 0.85;

  for (let attempt = 0; attempt < 8; attempt++) {
    const canvas = document.createElement('canvas');
    canvas.width = Math.max(1, Math.round(img.naturalWidth * scale));
    canvas.height = Math.max(1, Math.round(img.naturalHeight * scale));
    const ctx = canvas.getContext('2d');
    if (!ctx) throw new Error('Your browser cannot process images.');

    if (outputType === 'image/jpeg') {
      // JPEG has no transparency; avoid black backgrounds.
      ctx.fillStyle = '#ffffff';
      ctx.fillRect(0, 0, canvas.width, canvas.height);
    }
    ctx.drawImage(img, 0, 0, canvas.width, canvas.height);

    const dataUrl = canvas.toDataURL(outputType, quality);
    if (dataUrlBytes(dataUrl) <= options.maxBytes) return dataUrl;

    // Too big: JPEG loses quality first, PNG (lossless) has to shrink.
    if (outputType === 'image/jpeg' && quality > 0.5) quality -= 0.15;
    else scale *= 0.75;
  }

  throw new Error(`"${file.name}" is too large even after compression. Please use a smaller image.`);
}
