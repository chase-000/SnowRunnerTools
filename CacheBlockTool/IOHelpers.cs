using System;
using System.IO;

namespace CacheBlockTool {

	/// <summary>
	/// Simple helpers for cache_block file IO.
	/// </summary>
	public static class IOHelpers {

		private static byte[] __Buffer = new byte[65536];


		public static char[] InvalidPathChars { get; } = Path.GetInvalidPathChars ();
		public static char[] InvalidNameChars { get; } = Path.GetInvalidFileNameChars ();


		public static int ReadInt32 ( this Stream stream ) {
			return ReadValue<int> ( stream );
		}

		public static int[] ReadInt32Array ( this Stream stream , int count ) {
			var result = new int[count];
			for ( int i = 0; i < count; i++ ) {
				result[i] = ReadInt32 ( stream );
			}
			return result;
		}

		public static long ReadInt64 ( this Stream stream ) {
			return ReadValue<long> ( stream );
		}

		public static long[] ReadInt64Array ( this Stream stream , int count ) {
			var result = new long[count];
			for ( int i = 0; i < count; i++ ) {
				result[i] = ReadInt64 ( stream );
			}
			return result;
		}

		public static string ReadString ( this Stream stream , int length ) {
			var buffer = GetBuffer ( length );
			var read = stream.Read ( buffer , 0 , length );
			return CacheBlockFile.Encoding.GetString ( buffer , 0 , read );
		}

		public static unsafe T ReadValue<T> ( this Stream stream ) where T : unmanaged {
			var size = sizeof ( T );
			var buffer = GetBuffer ( size );
			var read = stream.Read ( buffer , 0 , size );
			if ( read != size ) throw new EndOfStreamException ();
			fixed ( byte* p = buffer ) {
				return *(T*) p;
			}
		}

		public static unsafe void WriteValue<T> ( this Stream stream , T value ) where T : unmanaged {
			var buffer = SetBufferTo ( value );
			stream.Write ( buffer , 0 , sizeof ( T ) );
		}


		public static void CopyBytesTo ( this Stream source , Stream target , int length ) {
			const int Chunk = 65536;
			while ( length > 0 ) {
				var buffer = GetBuffer ( Chunk );
				var read = source.Read ( buffer , 0 , Math.Min ( length , Chunk ) );
				if ( read == 0 ) throw new EndOfStreamException ();
				target.Write ( buffer , 0 , read );
				length -= read;
			}
		}

		public static InvalidDataException MakeBadMagicException ( long offset , string type , int expected , int actual ) {
			return new InvalidDataException ( $"Unexpected {type} 0x{actual:X} at offset 0x{offset:X} (should be 0x{expected:X})." );
		}

		public static void ReadMagicByte ( this Stream stream , byte expected ) {
			var actual = stream.ReadByte ();
			if ( actual != expected ) throw MakeBadMagicException ( stream.Position - 1 , "byte" , expected , actual );
		}

		public static void ReadMagicInt32 ( this Stream stream , int expected ) {
			var actual = ReadInt32 ( stream );
			if ( actual != expected ) throw MakeBadMagicException ( stream.Position - 1 , "dword" , expected , actual );
		}



		private static byte[] GetBuffer ( int length ) {
			if ( __Buffer.Length < length ) {
				__Buffer = new byte[length];
			}
			return __Buffer;
		}

		private static unsafe byte[] SetBufferTo<T> ( T value ) where T : unmanaged {
			var buffer = GetBuffer ( sizeof ( T ) );
			fixed ( byte* p = buffer ) {
				*(T*) p = value;
			}
			return buffer;
		}

	}


}
