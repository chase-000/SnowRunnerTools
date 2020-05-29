namespace SnowPakTool {

	public class LoadListStageEntry : LoadListEntryBase {

		public override LoadListEntryType Type => LoadListEntryType.Stage;
		public override int ExpectedStringsCount => 1;
		public string Text { get; set; }

		public override string ToString () {
			return $"[{Index}] {Text} ({DependsOn.Length}) @0x{OrderEntryOffset:X}/0x{NamesEntryOffset:X}";
		}

	}

}
