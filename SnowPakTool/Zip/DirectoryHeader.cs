using System.Runtime.InteropServices;

namespace SnowPakTool.Zip {

	[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
	public struct DirectoryHeader {

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
		public CompressionMethod Compression;
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
		public uint LocalHeaderOffset;
		// +2E

	}

	public enum DirectoryHeaderField {
		Signature,
		VersionMadeBy,
		VersionNeeded,
		Flags,
		Compression,
		Time,
		Date,
		Crc32,
		CompressedSize,
		UncompressedSize,
		NameLength,
		ExtraLength,
		CommentLength,
		DiskNumber,
		InternalAttributes,
		ExternalAttributes,
		LocalHeaderOffset,
	}

}
