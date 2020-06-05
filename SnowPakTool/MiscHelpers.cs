using System;
using System.Text;

namespace SnowPakTool {

	public static class MiscHelpers {

		private static readonly Lazy<Encoding> __Encoding = new Lazy<Encoding> ( () => {
			Encoding.RegisterProvider ( CodePagesEncodingProvider.Instance );
			return Encoding.GetEncoding ( 437 , EncoderFallback.ExceptionFallback , DecoderFallback.ExceptionFallback );
		} );


		/// <summary>
		/// Encoding used for file names. Could be some other encoding entirely, could be a multi-byte one, but works so far.
		/// </summary>
		public static Encoding Encoding => __Encoding.Value;


		public static ushort EnsureFitsUInt16 ( int value ) {
			if ( value > ushort.MaxValue ) throw new ArgumentOutOfRangeException ( nameof ( value ) , $"Value exceeds {ushort.MaxValue}." );
			return (ushort) value;
		}

		public static int EnsureFitsInt32 ( long value ) {
			if ( value > int.MaxValue ) throw new ArgumentOutOfRangeException ( nameof ( value ) , $"Value exceeds {int.MaxValue}." );
			return (int) value;
		}

		public static ushort GetDosTime ( DateTime dateTime ) {
			return (ushort) (
				dateTime.Second / 2
				| dateTime.Minute << 5
				| dateTime.Hour << 11
			);
		}

		public static ushort GetDosDate ( DateTime dateTime ) {
			if ( dateTime.Year < 1980 || dateTime.Year >= 2108 ) return 0;
			return (ushort) (
				dateTime.Day
				| dateTime.Month << 5
				| ( dateTime.Year - 1980 ) << 9
			);
		}

		public static unsafe int SizeOf<T> () where T : unmanaged {
			return sizeof ( T );
		}

		public static unsafe void SetValueAt<T> ( this byte[] buffer , int offset , T value ) where T : unmanaged {
			if ( buffer is null ) throw new ArgumentNullException ( nameof ( buffer ) );
			if ( offset < 0 || offset + sizeof ( T ) > buffer.Length ) throw new ArgumentOutOfRangeException ( nameof ( offset ) );
			fixed ( byte* p = buffer ) {
				*(T*) ( p + offset ) = value;
			}
		}

		public static unsafe T GetValueAt<T> ( this byte[] buffer , int offset ) where T : unmanaged {
			if ( buffer is null ) throw new ArgumentNullException ( nameof ( buffer ) );
			if ( offset < 0 || offset + sizeof ( T ) > buffer.Length ) throw new ArgumentOutOfRangeException ( nameof ( offset ) );
			fixed ( byte* p = buffer ) {
				return *(T*) ( p + offset );
			}
		}

		public static int ComputeUtf8Crc32 ( string value ) {
			using var hasher = new Crc32Managed ();
			var hash = hasher.ComputeHash ( Encoding.UTF8.GetBytes ( value ) );
			return ( hash[0] << 24 ) | ( hash[1] << 16 ) | ( hash[2] << 8 ) | hash[3];
		}


		internal static void Assert ( bool value ) {
			if ( !value ) throw new InvalidOperationException ();
		}

	}

}
