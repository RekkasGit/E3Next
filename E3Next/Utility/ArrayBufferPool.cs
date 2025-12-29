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

		private static readonly Int32 _maxSizeFromPool = 600;

		public void Return(byte[] buffer)
		{
			//if (buffer.Length <= _maxSizeFromPool)
			{
			
				_pool.Return(buffer);
			}
		}
		public byte[] Take(int size)
		{
			//if(size>_maxSizeFromPool)
			//{
			//	return new byte[size];
			//}

			return _pool.Rent(size);

		}
		public void Dispose() { }
	}
}
