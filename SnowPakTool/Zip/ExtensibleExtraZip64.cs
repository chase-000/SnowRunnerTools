using System.Runtime.InteropServices;

namespace SnowPakTool.Zip {

	/// <summary>
	/// The following is the layout of the zip64 extended 
	/// information "extra" block. If one of the size or
	/// offset fields in the Local or Central directory
	/// record is too small to hold the required data,
	/// a Zip64 extended information record is created.
	/// The order of the fields in the zip64 extended 
	/// information record is fixed, but the fields MUST
	/// only appear if the corresponding Local or Central
	/// directory record field is set to 0xFFFF or 0xFFFFFFFF.
	/// </summary>
	[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
	public struct ExtensibleExtraZip64 {
		public ulong UncompressedSize;
		public ulong CompressedSize;
		public ulong LocalHeaderOffset;
		public uint DiskNumber;
	}

}
