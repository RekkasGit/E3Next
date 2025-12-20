using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Utility
{
	public class ArrayBufferPool : NetMQ.IBufferPool
	{
		private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
		public void Return(byte[] buffer)
		{
			_pool.Return(buffer);
		}
		public byte[] Take(int size)
		{
			return _pool.Rent(size);
		}
		public void Dispose() { }
	}
}
