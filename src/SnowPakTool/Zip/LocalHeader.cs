using System.Runtime.InteropServices;

namespace SnowPakTool.Zip {

	[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
	public struct LocalHeader {

		public const int DefaultSignature = 0x04034B50;

		// +00 4
		public int Signature;
		// +04 2
		public ushort VersionNeeded;
		// +06 2
		public ushort Flags;
		// +08 2
		public CompressionMethod Compression;
		// +0A 2
		public ushort Time;
		// +0C 2
		public ushort Date;
		// +0E 4
		public int Crc32;
		// +12 4
		public uint CompressedSize;
		// +16 4
		public uint UncompressedSize;
		// +1A 2
		public ushort NameLength;
		// +1C 2
		public ushort ExtraLength;
		// +1E

	}

	public enum LocalHeaderField {
		Signature,
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
	}

}
