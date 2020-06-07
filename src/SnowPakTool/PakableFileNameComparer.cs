using System;
using System.Collections.Generic;

namespace SnowPakTool {

	public class PakableFileNameComparer : IComparer<string> {

		public static PakableFileNameComparer Instance { get; } = new PakableFileNameComparer ();

		private PakableFileNameComparer () { }

		public int Compare ( string x , string y ) {
			var first = ZipPakHelper.LoadListName.Equals ( x , StringComparison.OrdinalIgnoreCase );
			var second = ZipPakHelper.LoadListName.Equals ( y , StringComparison.OrdinalIgnoreCase );
			if ( first ) {
				if ( second ) return 0;
				return -1;
			}
			if ( second ) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare ( x , y );
		}

	}

}
