using System.Runtime.InteropServices;

namespace SnowPakTool.Zip {

	[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
	public struct EndOfDirectory64Locator {

		public const int DefaultSignature = 0x07064B50;

		// +00 4
		public int Signature;
		// +04 4
		public uint EndOfDirectory64DiskNumber;
		// +08 8
		public ulong EndOfDirectory64Offset;
		// +10 4
		public uint TotalDisks;
		// +14

	}

}
