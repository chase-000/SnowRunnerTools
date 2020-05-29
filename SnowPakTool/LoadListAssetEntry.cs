namespace SnowPakTool {

	public class LoadListAssetEntry : LoadListEntryBase {
		public override LoadListEntryType Type => LoadListEntryType.Asset;
		public override int ExpectedStringsCount => 3;
		public string InternalName { get; set; }
		public string Loader { get; set; }
		public string PakName { get; set; }

		public override string ToString () {
			return $"[{Index}] {PakName}\\{InternalName} ({Loader}) ({DependsOn.Length}) @0x{OrderEntryOffset:X}/0x{NamesEntryOffset:X}";
		}

	}

}
