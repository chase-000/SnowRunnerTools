using System;
using System.Text;

namespace SnowPakTool {

	public static class MiscHelpers {

		/// <summary>
		/// Encoding used for file names. Could be some other encoding entirely, could be a multi-byte one, but works so far.
		/// </summary>
		public static Encoding Encoding { get; } = Encoding.GetEncoding ( 437 );


		public static int EnsureFitsInt32 ( long value ) {
			if ( value > int.MaxValue ) throw new NotSupportedException ( $"Value exceeds {int.MaxValue}." );
			return (int) value;
		}

		internal static void Assert ( bool value ) {
			if ( !value ) throw new InvalidOperationException ();
		}

	}

}
