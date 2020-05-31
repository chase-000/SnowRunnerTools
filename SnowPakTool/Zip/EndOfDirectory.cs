using System.Runtime.InteropServices;

namespace SnowPakTool.Zip {

	[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
	public struct EndOfDirectory {

		public const int DefaultSignature = 0x06054B50;

		// +00 4
		public int Signature;
		// +04 2
		public ushort DiskNumber;
		// +06 2
		public ushort DirectoryDiskNumber;
		// +08 2
		public ushort DiskRecords;
		// +0A 2
		public ushort TotalRecords;
		// +0C 4
		public uint DirectorySize;
		// +10 4
		public uint DirectoryOffset;
		// +14 2
		public ushort CommentLength;
		// +16

	}

}
