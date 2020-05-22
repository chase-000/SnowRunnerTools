using System;
using System.Text;

namespace PakTool {

	public static class MiscHelpers {

		/// <summary>
		/// Encoding used for file names. Could be some other encoding entirely, could be a multi-byte one, but works so far.
		/// </summary>
		public static Encoding Encoding { get; } = Encoding.GetEncoding ( 437 );


		public static int EnsureValidFileSize ( long length ) {
			if ( length > int.MaxValue ) throw new NotSupportedException ( $"File size for exceeds {int.MaxValue}." );
			return (int) length;
		}

	}

}
