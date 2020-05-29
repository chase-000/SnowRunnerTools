using System;

namespace SnowPakTool {

	public abstract class LoadListEntryBase {

		public int Index { get; set; }
		public abstract LoadListEntryType Type { get; }
		public abstract int ExpectedStringsCount { get; }
		public long OrderEntryOffset { get; set; }
		public long NamesEntryOffset { get; set; }
		public byte[] MagicA { get; set; }
		public byte[] MagicB { get; set; }
		public int[] DependsOn { get; set; }

		public static LoadListEntryBase FromType ( LoadListEntryType type ) {
			switch ( type ) {
				case LoadListEntryType.Start: return new LoadListStartEntry ();
				case LoadListEntryType.Stage: return new LoadListStageEntry ();
				case LoadListEntryType.Asset: return new LoadListAssetEntry ();
				case LoadListEntryType.End: return new LoadListEndEntry ();
				default: throw new NotSupportedException ();
			}
		}

		public override string ToString () {
			return $"[{Index}] {Type} ({DependsOn.Length}) @0x{OrderEntryOffset:X}/0x{NamesEntryOffset:X}";
		}

	}

}
