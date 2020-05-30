using System.IO;

namespace SnowPakTool {

	public class LoadListStageEntry : LoadListEntryBase {

		public override LoadListEntryType Type => LoadListEntryType.Stage;
		public override int ExpectedStringsCount => 1;
		public string Text { get; set; }



		public override void WriteStrings ( Stream stream ) {
			base.WriteStrings ( stream );
			stream.WriteLength32String ( Text );
		}

		public override string ToString () {
			return $"[{Index}] {Text} ({DependsOn?.Length ?? 0}) @0x{DependencyEntryOffset:X}/0x{StringsEntryOffset:X}";
		}

	}

}
