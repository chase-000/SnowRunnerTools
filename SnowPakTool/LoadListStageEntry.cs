using System;
using System.IO;

namespace SnowPakTool {

	public class LoadListStageEntry : LoadListEntryBase {

		public override LoadListEntryType Type => LoadListEntryType.Stage;
		public override int StringsCount => 1;
		public string Text { get; set; }


		public override bool IsValidStringsCount ( int count ) {
			return count == 1;
		}

		public override void LoadStrings ( string[] strings ) {
			if ( strings is null ) throw new ArgumentNullException ( nameof ( strings ) );
			if ( !IsValidStringsCount ( strings.Length ) ) throw new NotSupportedException ();
			Text = strings[0];
		}

		public override void WriteStrings ( Stream stream ) {
			base.WriteStrings ( stream );
			stream.WriteLength32String ( Text );
		}

		public override string ToString () {
			return $"[{Index}] {Text} ({DependsOn?.Length ?? 0}) @0x{DependencyEntryOffset:X}/0x{StringsEntryOffset:X}";
		}

	}

}
