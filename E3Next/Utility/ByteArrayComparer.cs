using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Utility
{
	public class ByteArrayComparer : System.Collections.Generic.IEqualityComparer<byte[]>
	{
		//reason this works is because GetHashCode is called first. If it find something that hashes to this (empty parts of the array count), then it will try a comapirson
		//after that if we have two things that equate via hash, then Equals is called.
		//Here we are using ByteArrayLength to limit the equals check to only check 'up to' the length of the value we are looking for. 
		//small issue with two byte arryas, same hash but being differnt, but the left 'starting ' with what the right has. need to debug to figure out left vs right. which one is the key vs the check value
		//though I will admit the odds of that are... small.

		public Int32 ByteArrayLength = 0;
		public bool Equals(byte[] left, byte[] right)
		{
			if (left.Length < ByteArrayLength)
			{
				return false;
			}
			if (right.Length < ByteArrayLength)
			{
				return false;
			}
			return UnsafeByteArrayManipulation.ByteArraysEqual(left, right, ByteArrayLength);
		}

		//Modified microsoft source code heavily to get a hash code to work off array of bytes
		//hashcodes are computed when added or checked via a key. long as on insert we put the right size for ByteArrayLength, we should be fine.
		public unsafe int GetHashCode(byte[] key)
		{
			unsafe
			{
				fixed (byte* src = key)
				{
					int hash1 = 5381;
					int hash2 = hash1;
					int c = 0;
					Int32 counter = 0;

					// Int32 length = key.Length;
					Int32 length = ByteArrayLength;
					while (counter < length) //till end of array
					{
						c = src[counter];
						hash1 = ((hash1 << 5) + hash1) ^ c;
						counter++;
						if (counter < length)
						{
							c = src[counter];
							hash2 = ((hash2 << 5) + hash2) ^ c;
							counter++;
						}
					}
					//Mersenne Twister
					return hash1 + (hash2 * 1566083941);
				}
			}
		}
		public static unsafe int GetHashCodeStatic(byte[] key, Int32 length)
		{
			unsafe
			{
				fixed (byte* src = key)
				{
					int hash1 = 5381;
					int hash2 = hash1;
					int c = 0;
					Int32 counter = 0;

					while (counter < length) //till end of array
					{
						c = src[counter];
						hash1 = ((hash1 << 5) + hash1) ^ c;
						counter++;
						if (counter < length)
						{
							c = src[counter];
							hash2 = ((hash2 << 5) + hash2) ^ c;
							counter++;
						}
					}
					//Mersenne Twister
					return hash1 + (hash2 * 1566083941);
				}
			}
		}

	}
	public class UnsafeByteArrayManipulation
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static UInt32 GetUInt32FromArray(byte[] source, int index)
		{
			fixed (byte* p = &source[0])
			{
				return *(UInt32*)(p + index);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static Int16 GetInt16FromArray(byte[] source, int index)
		{
			fixed (byte* p = &source[0])
			{
				return *(Int16*)(p + index);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static Int32 GetInt32FromArray(byte[] source, int index)
		{
			fixed (byte* p = &source[0])
			{
				return *(Int32*)(p + index);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void SetInt32IntoArray(byte[] target, int index, Int32 value)
		{
			fixed (byte* p = &target[index])
			{
				*((Int32*)p) = value;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static UInt64 GetUInt64FromArray(byte[] source, int index)
		{
			fixed (byte* p = &source[0])
			{
				return *(UInt64*)(p + index);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static Int64 GetInt64FromArray(byte[] source, int index)
		{
			fixed (byte* p = &source[0])
			{
				return *(Int64*)(p + index);
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void SetInt64IntoArray(byte[] target, int index, Int64 value)
		{
			fixed (byte* p = &target[index])
			{
				*((Int64*)p) = value;
			}
		}

		//Originally taken from a microsoft paper, slightly modified so I could pass in arrays that are partially used.
		//This relies on the fact that Intel CPU's actually compare 10 bytes at a time.
		//http://techmikael.blogspot.com/2009/01/fast-byte-array-comparison-in-c.html
		//note, look at the comment section for the correct code.
		//the length2, passed is in case you use arrays from a buffer pool that isn't exactly the size you need.
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public static unsafe bool ByteArraysEqual(byte[] array1, byte[] array2, Int32 lengthToCheckTo)
		{
			int length = lengthToCheckTo;
			fixed (byte* str = array1)
			{
				byte* chPtr = str;
				fixed (byte* str2 = array2)
				{
					byte* chPtr2 = str2;
					while (length >= 20)
					{
						if ((((*(((int*)chPtr)) != *(((int*)chPtr2))) ||
						(*(((int*)(chPtr + 4))) != *(((int*)(chPtr2 + 4))))) ||
						((*(((int*)(chPtr + 8))) != *(((int*)(chPtr2 + 8)))) ||
						(*(((int*)(chPtr + 12))) != *(((int*)(chPtr2 + 12)))))) ||
						(*(((int*)(chPtr + 16))) != *(((int*)(chPtr2 + 16)))))
							break;

						chPtr += 20;
						chPtr2 += 20;
						length -= 20;
					}

					while (length >= 4)
					{
						if (*(((int*)chPtr)) != *(((int*)chPtr2))) break;
						chPtr += 4;
						chPtr2 += 4;
						length -= 4;
					}

					while (length > 0)
					{
						if (*chPtr != *chPtr2) break;
						chPtr++;
						chPtr2++;
						length--;
					}

					return (length <= 0);
				}
			}
		}
	}

}
