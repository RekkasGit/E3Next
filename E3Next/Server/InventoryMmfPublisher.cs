using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;

namespace E3Core.Server
{
    // Memory-Mapped File publisher for inventory payloads
    // Layout:
    // [Header][Payload]
    // Header:
    //   uint Magic        = 0x4D4D4631 ('MMF1')
    //   uint Version      = 1
    //   uint Seq          = monotonically increasing sequence
    //   uint PayloadBytes = length of payload bytes
    //   uint Reserved0
    //   uint Reserved1
    //   uint Reserved2
    //   uint Reserved3
    //
    // Name: Global\E3Next_Inventory_MMF
    public sealed class InventoryMmfPublisher : IDisposable
    {
        // Header constants
        private const uint HEADER_MAGIC = 0x4D4D4631; // 'MMF1'
        private const uint HEADER_VERSION = 1;
        private const int HEADER_SIZE = 32; // 8 * 4 bytes

        // MMF configuration
        private const string MMF_NAME = @"E3Next_Inventory_MMF";
        // 4 MB buffer per user selection
        private const int MMF_CAPACITY_BYTES = 4 * 1024 * 1024;

        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private uint _seq;
        private readonly object _lock = new object();
        private bool _initialized;

        // Singleton
        private static readonly Lazy<InventoryMmfPublisher> _instance = new Lazy<InventoryMmfPublisher>(() => new InventoryMmfPublisher());
        public static InventoryMmfPublisher Instance => _instance.Value;

        private InventoryMmfPublisher()
        {
            try
            {
                // Use simple CreateOrOpen without explicit security to avoid requiring MemoryMappedFileSecurity type
                _mmf = MemoryMappedFile.CreateOrOpen(MMF_NAME, MMF_CAPACITY_BYTES, MemoryMappedFileAccess.ReadWrite);
                _accessor = _mmf.CreateViewAccessor(0, MMF_CAPACITY_BYTES, MemoryMappedFileAccess.ReadWrite);
                // Initialize header to empty
                _accessor.Write(0, HEADER_MAGIC);
                _accessor.Write(4, HEADER_VERSION);
                _accessor.Write(8, (uint)0); // Seq
                _accessor.Write(12, (uint)0); // PayloadBytes
                _accessor.Write(16, (uint)0);
                _accessor.Write(20, (uint)0);
                _accessor.Write(24, (uint)0);
                _accessor.Write(28, (uint)0);
                _accessor.Flush();
                _initialized = true;

                // Debug-only breadcrumb; avoids extra dependencies
                System.Diagnostics.Debug.WriteLine($"[E3Next] InventoryMmfPublisher OK: name={MMF_NAME}, cap={MMF_CAPACITY_BYTES}");
            }
            catch (Exception ex)
            {
                // Do not throw; leave uninitialized so calls will be no-ops
                _initialized = false;
                System.Diagnostics.Debug.WriteLine($"[E3Next] InventoryMmfPublisher FAILED: {ex.Message}");
            }
        }

        public void Publish(byte[] payload)
        {
            if (!_initialized || payload == null) return;

            if (payload.Length + HEADER_SIZE > MMF_CAPACITY_BYTES)
            {
                // Truncate if oversized to protect against overruns
                // Consumers should handle partial/incomplete payloads by validating framing/magic
                var truncated = new byte[Math.Max(0, MMF_CAPACITY_BYTES - HEADER_SIZE)];
                Buffer.BlockCopy(payload, 0, truncated, 0, truncated.Length);
                payload = truncated;
            }

            lock (_lock)
            {
                try
                {
                    // Write payload first
                    _accessor.WriteArray(HEADER_SIZE, payload, 0, payload.Length);

                    // Increment sequence (wrap naturally)
                    _seq++;

                    // Finally write header fields atomically in order:
                    // Magic, Version already set at creation; update Seq and PayloadBytes
                    _accessor.Write(8, _seq); // Seq
                    _accessor.Write(12, (uint)payload.Length); // PayloadBytes

                    _accessor.Flush();
                }
                catch
                {
                    // Swallow exceptions to avoid impacting caller
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                try { _accessor?.Dispose(); } catch { }
                try { _mmf?.Dispose(); } catch { }
                _accessor = null;
                _mmf = null;
                _initialized = false;
            }
        }
    }
}