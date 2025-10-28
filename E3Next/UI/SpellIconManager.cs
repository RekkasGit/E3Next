using MonoCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace E3Next.UI
{
    /// <summary>
    /// Manages loading and caching of EverQuest spell icons for ImGui display
    /// Loads TGA files from EQ's uifiles/default directory and converts them to ImGui textures
    /// </summary>
    public static class SpellIconManager
    {
        private static List<IntPtr> _spellIconTextures = new List<IntPtr>();
        private static bool _iconsLoaded = false;
        private static string _eqDirectory = string.Empty;
        
        /// <summary>
        /// Initializes the spell icon system by loading all spell icon TGA files
        /// </summary>
        public static void Initialize(string eqDirectory)
        {
            if (_iconsLoaded) return;
            
            _eqDirectory = eqDirectory;
            LoadSpellIcons();
            _iconsLoaded = true;
        }
        
        /// <summary>
        /// Gets the ImGui texture ID for the specified spell icon index
        /// </summary>
        public static IntPtr GetSpellIconTexture(int iconIndex)
        {
            if (!_iconsLoaded || iconIndex < 0 || iconIndex >= _spellIconTextures.Count)
                return IntPtr.Zero;
                
            return _spellIconTextures[iconIndex];
        }
        
        /// <summary>
        /// Returns the total number of loaded spell icons
        /// </summary>
        public static int GetIconCount()
        {
            return _spellIconTextures.Count;
        }
        
        /// <summary>
        /// Checks if the spell icon system is initialized and ready
        /// </summary>
        public static bool IsReady()
        {
            return _iconsLoaded;
        }
        
        /// <summary>
        /// Cleanup method to free texture resources
        /// </summary>
        public static void Cleanup()
        {
            foreach (var texture in _spellIconTextures)
            {
                if (texture != IntPtr.Zero)
                {
                    try
                    {
                        E3ImGUI.mq_DestroyTexture(texture);
                    }
                    catch (Exception ex)
                    {
                        E3Core.Utility.e3util._log.Write($"SpellIconManager: Error destroying texture: {ex.Message}");
                    }
                }
            }
            _spellIconTextures.Clear();
            _iconsLoaded = false;
        }
        
        private static void LoadSpellIcons()
        {
            if (string.IsNullOrEmpty(_eqDirectory))
            {
                E3Core.Utility.e3util._log.Write("SpellIconManager: EQ directory not set, cannot load spell icons");
                return;
            }
            
            string uiPath = Path.Combine(_eqDirectory, "uifiles", "default");
            if (!Directory.Exists(uiPath))
            {
                E3Core.Utility.e3util._log.Write($"SpellIconManager: UI path not found: {uiPath}");
                return;
            }
            
            int iconsLoaded = 0;
            
            // Load spell icon files (spells01.tga through spells63.tga)
            for (int i = 1; i <= 63; i++)
            {
                string fileName = Path.Combine(uiPath, $"spells{i:D2}.tga");
                if (!File.Exists(fileName)) continue;
                
                try
                {
                    using (var tgaBitmap = TGALoader.LoadTGA(fileName))
                    {
                        if (tgaBitmap == null) continue;
                        
                        // Extract each 40x40 icon from the 6x6 grid
                        for (int y = 0; y < 6; y++)
                        {
                            for (int x = 0; x < 6; x++)
                            {
                                using (var iconBitmap = TGALoader.ExtractSpellIcon(tgaBitmap, x, y))
                                {
                                    if (iconBitmap != null)
                                    {
                                        IntPtr textureId = CreateImGuiTexture(iconBitmap);
                                        _spellIconTextures.Add(textureId);
                                        iconsLoaded++;
                                    }
                                    else
                                    {
                                        // Add null pointer for missing icons to maintain index consistency
                                        _spellIconTextures.Add(IntPtr.Zero);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    E3Core.Utility.e3util._log.Write($"SpellIconManager: Error loading {fileName}: {ex.Message}");
                }
            }
            
            E3Core.Utility.e3util._log.Write($"SpellIconManager: Loaded {iconsLoaded} spell icons from {_spellIconTextures.Count} slots");
        }
        
        /// <summary>
        /// Converts a bitmap to an ImGui texture ID
        /// This is a placeholder - actual implementation would depend on the specific ImGui backend
        /// </summary>
        private static IntPtr CreateImGuiTexture(Bitmap bitmap)
        {
            try
            {
                // Lock the bitmap data
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                               ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                
                try
                {
                    // Create texture data array
                    int dataSize = bitmapData.Stride * bitmap.Height;
                    byte[] textureData = new byte[dataSize];
                    Marshal.Copy(bitmapData.Scan0, textureData, 0, dataSize);
                    
                    // Convert BGRA to RGBA if needed
                    for (int i = 0; i < textureData.Length; i += 4)
                    {
                        byte b = textureData[i];     // Blue
                        byte g = textureData[i + 1]; // Green
                        byte r = textureData[i + 2]; // Red
                        byte a = textureData[i + 3]; // Alpha
                        
                        textureData[i] = r;     // Red
                        textureData[i + 1] = g; // Green
                        textureData[i + 2] = b; // Blue
                        textureData[i + 3] = a; // Alpha
                    }
                    
                    // This would be replaced with actual ImGui texture creation
                    // For now, we'll use a placeholder approach that stores the bitmap data
                    IntPtr texturePtr = CreateTextureFromData(textureData, bitmap.Width, bitmap.Height);
                    return texturePtr;
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
            }
            catch (Exception ex)
            {
                E3Core.Utility.e3util._log.Write($"SpellIconManager: Error creating texture: {ex.Message}");
                return IntPtr.Zero;
            }
        }
        
        /// <summary>
        /// Creates a texture from raw RGBA data using MQ2Mono's native texture creation
        /// </summary>
        private static IntPtr CreateTextureFromData(byte[] data, int width, int height)
        {
            try
            {
                // Call the native MQ2Mono function to create the texture
                return E3ImGUI.mq_CreateTextureFromData(data, width, height, 4); // 4 channels for RGBA
            }
            catch (Exception ex)
            {
                E3Core.Utility.e3util._log.Write($"SpellIconManager: Error calling native texture creation: {ex.Message}");
                return IntPtr.Zero;
            }
            // Example pseudo-code for what this might look like:
            // return MQ2ImGuiIntegration.CreateTexture(data, width, height);
            
            // For now, return a non-zero pointer to indicate "texture created"
            // This will need to be replaced with actual texture creation code
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            return handle.AddrOfPinnedObject();
        }
        
        /// <summary>
        /// Helper method to resize an icon to the desired display size
        /// </summary>
        public static Bitmap ResizeIcon(Bitmap original, int targetSize)
        {
            if (original == null) return null;
            
            try
            {
                var resized = new Bitmap(targetSize, targetSize);
                using (var graphics = Graphics.FromImage(resized))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(original, 0, 0, targetSize, targetSize);
                }
                return resized;
            }
            catch (Exception ex)
            {
                E3Core.Utility.e3util._log.Write($"SpellIconManager: Error resizing icon: {ex.Message}");
                return null;
            }
        }
    }
}