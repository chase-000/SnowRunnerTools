using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SnowPakTool {

	/// <summary>
	/// Simple helpers for cache_block file IO.
	/// </summary>
	public static class IOHelpers {

		private static byte[] __Buffer = new byte[65536];


		public static char[] InvalidPathChars { get; } = Path.GetInvalidPathChars ();
		public static char[] InvalidNameChars { get; } = Path.GetInvalidFileNameChars ();
		public static char[] Wildcards { get; } = new[] { '*' , '?' };


		public static int ReadInt32 ( this Stream stream ) {
			return ReadValue<int> ( stream );
		}

		public static int[] ReadInt32Array ( this Stream stream , int count ) {
			return ReadValuesArray<int> ( stream , count );
		}

		public static long ReadInt64 ( this Stream stream ) {
			return ReadValue<long> ( stream );
		}

		public static long[] ReadInt64Array ( this Stream stream , int count ) {
			return ReadValuesArray<long> ( stream , count );
		}

		public static byte[] ReadByteArray ( this Stream stream , int count ) {
			var result = new byte[count];
			var read = stream.Read ( result , 0 , count );
			if ( read < count ) throw new EndOfStreamException ();
			return result;
		}

		public static string ReadString ( this Stream stream , int bytesCount ) {
			var buffer = GetBuffer ( bytesCount );
			var read = stream.Read ( buffer , 0 , bytesCount );
			if ( read != bytesCount ) throw new EndOfStreamException ();
			return MiscHelpers.Encoding.GetString ( buffer , 0 , read );
		}

		public static string ReadLength32String ( this Stream stream ) {
			var bytesCount = stream.ReadInt32 ();
			return ReadString ( stream , bytesCount );
		}

		public static string[] ReadLength32StringsArray ( this Stream stream , int count ) {
			var result = new string[count];
			for ( int i = 0; i < count; i++ ) {
				result[i] = ReadLength32String ( stream );
			}
			return result;
		}

		public static void WriteString ( this Stream stream , string value ) {
			var buffer = GetBuffer ( value.Length * 4 );
			var bytesCount = MiscHelpers.Encoding.GetBytes ( value , 0 , value.Length , buffer , 0 );
			stream.Write ( buffer , 0 , bytesCount );
		}

		public static void WriteLength32String ( this Stream stream , string value ) {
			var buffer = GetBuffer ( 4 + value.Length * 4 );
			var bytesCount = MiscHelpers.Encoding.GetBytes ( value , 0 , value.Length , buffer , 4 );
			buffer.SetValueAt ( 0 , bytesCount );
			stream.Write ( buffer , 0 , 4 + bytesCount );
		}

		public static T ReadValue<T> ( this Stream stream ) where T : unmanaged {
			var size = MiscHelpers.SizeOf<T> ();
			var buffer = GetBuffer ( size );
			var read = stream.Read ( buffer , 0 , size );
			if ( read != size ) throw new EndOfStreamException ();
			return buffer.GetValueAt<T> ( 0 );
		}

		public static T ReadValue<T> ( this Stream stream , int offset ) where T : unmanaged {
			var size = MiscHelpers.SizeOf<T> ();
			if ( offset < 0 || offset > size ) throw new ArgumentOutOfRangeException ( nameof ( offset ) );
			var buffer = GetBuffer ( size );
			Array.Clear ( buffer , 0 , offset );
			var remainder = size - offset;
			if ( remainder > 0 ) {
				var read = stream.Read ( buffer , offset , remainder );
				if ( read != remainder ) throw new EndOfStreamException ();
			}
			return buffer.GetValueAt<T> ( 0 );
		}

		public static T[] ReadValuesArray<T> ( this Stream stream , int count ) where T : unmanaged {
			var result = new T[count];
			for ( int i = 0; i < count; i++ ) {
				result[i] = ReadValue<T> ( stream );
			}
			return result;
		}

		public static void WriteValue<T> ( this Stream stream , T value ) where T : unmanaged {
			var size = MiscHelpers.SizeOf<T> ();
			var buffer = GetBuffer ( size );
			buffer.SetValueAt ( 0 , value );
			stream.Write ( buffer , 0 , size );
		}

		public static void WriteValuesArray<T> ( this Stream stream , T[] values ) where T : unmanaged {
			for ( int i = 0; i < values.Length; i++ ) {
				WriteValue ( stream , values[i] );
			}
		}

		public static void ProcessChunked ( this Stream stream , long length , Action<byte[] , int> action ) {
			const int Chunk = 65536;
			while ( length > 0 ) {
				var buffer = GetBuffer ( Chunk );
				var read = stream.Read ( buffer , 0 , (int) Math.Min ( length , Chunk ) );
				if ( read == 0 ) throw new EndOfStreamException ();
				action ( buffer , read );
				length -= read;
			}

		}

		public static void CopyBytesTo ( this Stream source , Stream target , int length ) {
			ProcessChunked ( source , length , ( buffer , read ) => target.Write ( buffer , 0 , read ) );
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

		public static string NormalizeDirectory ( string directory ) {
			if ( directory is null ) throw new ArgumentNullException ( nameof ( directory ) );
			directory = Path.GetFullPath ( directory );
			return directory[^1] == '\\' ? directory : directory + '\\';
		}

		public static bool FileExistsOrWildcardDirectoryExists ( string location ) {
			return FileExistsOrWildcardDirectoryExists ( location , out _ , out _ );
		}

		public static bool FileExistsOrWildcardDirectoryExists ( string location , out string directory , out string name ) {
			if ( location is null ) throw new ArgumentNullException ( nameof ( location ) );
			directory = Path.GetDirectoryName ( location );
			name = Path.GetFileName ( location );
			if ( location.IndexOfAny ( Wildcards ) < 0 ) {
				return File.Exists ( location );
			}
			else {
				if ( !Directory.Exists ( directory ) ) return false;
				if ( name.Length <= 0 ) return false;
				return name.IndexOfAny ( Path.GetInvalidFileNameChars () ) < 0;
			}
		}

		public static int ComputeCrc32 ( this Stream stream , long length ) {
			using var hasher = new Crc32Managed ();
			stream.ProcessChunked ( length , ( buffer , read ) => hasher.TransformBlock ( buffer , 0 , read , buffer , 0 ) );
			hasher.TransformFinalBlock ( __Buffer , 0 , 0 );
			return ( hasher.Hash[0] << 24 ) | ( hasher.Hash[1] << 16 ) | ( hasher.Hash[2] << 8 ) | hasher.Hash[3];
		}

		public static IEnumerable<KeyValuePair<string , string>> GetRelativeAndFullNames ( string location , IEnumerable<string> excludedDirectories = null ) {
			location = NormalizeDirectory ( location );
			IEnumerable<string> files;
			if ( excludedDirectories == null ) {
				files = Directory.EnumerateFiles ( location , "*" , SearchOption.AllDirectories );
			}
			else {
				var excluded = new HashSet<string> ( excludedDirectories , StringComparer.OrdinalIgnoreCase );
				files = Directory.EnumerateFiles ( location , "*" ).Concat (
					Directory.EnumerateDirectories ( location )
						.Where ( a => !excluded.Contains ( Path.GetFileName ( a ) ) )
						.SelectMany ( a => Directory.EnumerateFiles ( a , "*" , SearchOption.AllDirectories ) )
				);
			}
			return files.Select ( a => new KeyValuePair<string , string> ( a.Substring ( location.Length ) , a ) );
		}



		private static byte[] GetBuffer ( int length ) {
			if ( __Buffer.Length < length ) {
				__Buffer = new byte[length];
			}
			return __Buffer;
		}

	}


}
