using System.Diagnostics;

namespace CacheBlockTool {
	[DebuggerDisplay ( "{InternalName}: {Size} @ {RelativeOffset}" )]
	public class FileEntry {
		public string InternalName { get; set; }
		public string ExternalName { get; set; }
		public long RelativeOffset { get; set; }
		public int Size { get; set; }
		public int Zero { get; set; }
	}

}
