using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace E3NextUI.Util
{
	public class WinUtils
	{
		[DllImport("ntdll.dll", SetLastError = true)]
		internal static extern uint RtlGetVersion(out OsVersionInfo versionInformation); // return type should be the NtStatus enum

		[StructLayout(LayoutKind.Sequential)]
		internal struct OsVersionInfo
		{
			private readonly uint OsVersionInfoSize;

			internal readonly uint MajorVersion;
			internal readonly uint MinorVersion;

			private readonly uint BuildNumber;

			private readonly uint PlatformId;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			private readonly string CSDVersion;
		}
	}
}
