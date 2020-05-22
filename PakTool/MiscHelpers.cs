using System;

namespace PakTool {

	public static class MiscHelpers {

		public static int EnsureValidFileSize ( long length ) {
			if ( length > int.MaxValue ) throw new NotSupportedException ( $"File size for exceeds {int.MaxValue}." );
			return (int) length;
		}

	}

}
