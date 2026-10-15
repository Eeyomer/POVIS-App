using System.Text.RegularExpressions;

namespace POVISAPI.Common
{
    /// <summary>Validates images sent as base64 data URLs (JPEG / JPG / PNG only).</summary>
    public static class ImageDataValidator
    {
        private static readonly Regex Header = new(
            @"^data:image/(?<type>jpeg|jpg|png);base64,",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Returns the normalized data URL ("image/jpg" becomes "image/jpeg") and its content type.</summary>
        public static (string DataUrl, string ContentType) Validate(string? dataUrl, int maxBytes, string label = "Image")
        {
            if (string.IsNullOrWhiteSpace(dataUrl))
                throw new ArgumentException($"{label} is empty.");

            var match = Header.Match(dataUrl.Trim());
            if (!match.Success)
                throw new ArgumentException($"{label} must be a JPEG, JPG or PNG image.");

            var isPng = match.Groups["type"].Value.Equals("png", StringComparison.OrdinalIgnoreCase);
            var base64 = new string(dataUrl.Trim()[match.Length..].Where(c => !char.IsWhiteSpace(c)).ToArray());

            // Cheap size check before decoding: base64 is ~4/3 the size of the bytes.
            if (base64.Length / 4 * 3 > maxBytes + 3)
                throw new ArgumentException($"{label} is too large. The maximum size is {maxBytes / 1_000_000.0:0.#} MB.");

            var buffer = new byte[base64.Length / 4 * 3 + 3];
            if (!Convert.TryFromBase64String(base64, buffer, out var written))
                throw new ArgumentException($"{label} is not valid base64 image data.");
            if (written > maxBytes)
                throw new ArgumentException($"{label} is too large. The maximum size is {maxBytes / 1_000_000.0:0.#} MB.");

            // The bytes must really be the image type that was declared.
            bool looksPng = written >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47;
            bool looksJpeg = written >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;
            if (isPng ? !looksPng : !looksJpeg)
                throw new ArgumentException($"{label} content does not match its image type. Only genuine JPEG, JPG or PNG images are allowed.");

            var contentType = isPng ? "image/png" : "image/jpeg";
            return ($"data:{contentType};base64,{base64}", contentType);
        }
    }
}
