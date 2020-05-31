using System.Runtime.InteropServices;

namespace SnowPakTool.Zip {

	[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
	public struct CentralDirectoryFileHeader {

		public const int DefaultSignature = 0x02014B50;

		// +00 4
		public int Signature;
		// +04 2
		public ushort VersionMadeBy;
		// +06 2
		public ushort VersionNeeded;
		// +08 2
		public ushort Flags;
		// +0A 2
		public ushort Compression;
		// +0C 2
		public ushort Time;
		// +0E 2
		public ushort Date;
		// +10 4
		public int Crc32;
		// +14 4
		public uint CompressedSize;
		// +18 4
		public uint UncompressedSize;
		// +1C 2
		public ushort NameLength;
		// +1E 2
		public ushort ExtraLength;
		// +20 2
		public ushort CommentLength;
		// +22 2
		public ushort DiskNumber;
		// +24 2
		public ushort InternalAttributes;
		// +26 4
		public uint ExternalAttributes;
		// +2A 4
		public uint LocalOffset;
		// +2E

		public CentralDirectoryFileHeader ( LocalFileHeader header ) {
			Signature = DefaultSignature;
			VersionMadeBy = header.VersionNeeded;
			VersionNeeded = header.VersionNeeded;
			Flags = header.Flags;
			Compression = header.Compression;
			Time = header.Time;
			Date = header.Date;
			Crc32 = header.Crc32;
			CompressedSize = header.CompressedSize;
			UncompressedSize = header.UncompressedSize;
			NameLength = header.NameLength;
			ExtraLength = 0;
			CommentLength = 0;
			DiskNumber = 0;
			InternalAttributes = 0;
			ExternalAttributes = 0;
			LocalOffset = 0;
		}

	}

}
