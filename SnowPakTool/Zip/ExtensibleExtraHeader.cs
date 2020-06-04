using System.Runtime.InteropServices;

namespace SnowPakTool.Zip {

	[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
	public struct ExtensibleExtraHeader {

		// +00 2
		public ExtensibleExtraId Id;
		// +02 2
		public ushort Size;
		// +04

	}

	public enum ExtensibleExtraHeaderField {
		Id,
		Size,
	}

}
