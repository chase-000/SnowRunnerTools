using System.Runtime.InteropServices;

namespace SnowPakTool.Zip {

	[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
	public struct EndOfDirectory64 {

		public const int DefaultSignature = 0x06064B50;

		// +00 4
		public int Signature;
		// +04 8
		public ulong Size;
		// +0C 2
		public ushort VersionMadeBy;
		// +0E 2
		public ushort VersionNeeded;
		// +10 4
		public uint DiskNumber;
		// +14 4
		public uint DirectoryDiskNumber;
		// +18 8
		public ulong DiskRecords;
		// +20 8
		public ulong TotalRecords;
		// +28 8
		public ulong DirectorySize;
		// +30 8
		public ulong DirectoryOffset;
		// +38

	}

	public enum EndOfDirectory64Field {
		Signature,
		Size,
		VersionMadeBy,
		VersionNeeded,
		DiskNumber,
		DirectoryDiskNumber,
		DiskRecords,
		TotalRecords,
		DirectorySize,
		DirectoryOffset,
	}

}
