using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace E3Next.UI
{
    /// <summary>
    /// Simple TGA image loader for reading EverQuest spell icon files
    /// Based on the TGA format used by EverQuest's spell icon files
    /// </summary>
    public static class TGALoader
    {
        public static Bitmap LoadTGA(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    // Read TGA header
                    byte idLength = br.ReadByte();
                    byte colorMapType = br.ReadByte();
                    byte imageType = br.ReadByte();
                    
                    // Color map specification (5 bytes)
                    br.ReadUInt16(); // firstEntryIndex
                    br.ReadUInt16(); // colorMapLength
                    br.ReadByte();   // colorMapEntrySize
                    
                    // Image specification (10 bytes)
                    br.ReadUInt16(); // xOrigin
                    br.ReadUInt16(); // yOrigin
                    ushort width = br.ReadUInt16();
                    ushort height = br.ReadUInt16();
                    byte pixelDepth = br.ReadByte();
                    byte imageDescriptor = br.ReadByte();
                    
                    // Skip image ID if present
                    if (idLength > 0)
                        br.ReadBytes(idLength);
                    
                    // Only support 32-bit BGRA uncompressed images for now
                    if (imageType != 2 || pixelDepth != 32)
                        return null;
                    
                    // Read pixel data
                    var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), 
                                                   ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    
                    try
                    {
                        int stride = bitmapData.Stride;
                        byte[] pixels = new byte[stride * height];
                        
                        // TGA stores pixels as BGRA, we need ARGB
                        for (int y = 0; y < height; y++)
                        {
                            int targetY = (imageDescriptor & 0x20) != 0 ? y : height - 1 - y; // Check origin
                            for (int x = 0; x < width; x++)
                            {
                                byte b = br.ReadByte();
                                byte g = br.ReadByte();
                                byte r = br.ReadByte();
                                byte a = br.ReadByte();
                                
                                int offset = targetY * stride + x * 4;
                                pixels[offset] = b;     // Blue
                                pixels[offset + 1] = g; // Green
                                pixels[offset + 2] = r; // Red
                                pixels[offset + 3] = a; // Alpha
                            }
                        }
                        
                        System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }
                    
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                E3Core.Utility.e3util._log.Write($"Error loading TGA file {filePath}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Extracts a 40x40 spell icon from a TGA spell sheet at the given grid position
        /// </summary>
        public static Bitmap ExtractSpellIcon(Bitmap tgaBitmap, int gridX, int gridY)
        {
            if (tgaBitmap == null || gridX < 0 || gridX >= 6 || gridY < 0 || gridY >= 6)
                return null;
                
            try
            {
                var iconRect = new Rectangle(gridX * 40, gridY * 40, 40, 40);
                return tgaBitmap.Clone(iconRect, tgaBitmap.PixelFormat);
            }
            catch (Exception ex)
            {
                E3Core.Utility.e3util._log.Write($"Error extracting spell icon at ({gridX},{gridY}): {ex.Message}");
                return null;
            }
        }
    }
}