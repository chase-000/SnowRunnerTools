using System;
using System.Text;

namespace SnowPakTool {

	public static class MiscHelpers {

		/// <summary>
		/// Encoding used for file names. Could be some other encoding entirely, could be a multi-byte one, but works so far.
		/// </summary>
		public static Encoding Encoding { get; } = Encoding.GetEncoding ( 437 , EncoderFallback.ExceptionFallback , DecoderFallback.ExceptionFallback );


		public static int EnsureFitsInt32 ( long value ) {
			if ( value > int.MaxValue ) throw new ArgumentOutOfRangeException ( nameof ( value ) , $"Value exceeds {int.MaxValue}." );
			return (int) value;
		}

		public static uint EnsureFitsUInt32 ( long value ) {
			if ( value > uint.MaxValue ) throw new ArgumentOutOfRangeException ( nameof ( value ) , $"Value exceeds {uint.MaxValue}." );
			return (uint) value;
		}

		public static ushort EnsureFitsUInt16 ( int value ) {
			if ( value > ushort.MaxValue ) throw new ArgumentOutOfRangeException ( nameof ( value ) , $"Value exceeds {ushort.MaxValue}." );
			return (ushort) value;
		}

		internal static void Assert ( bool value ) {
			if ( !value ) throw new InvalidOperationException ();
		}

	}

}
