using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PakTool {

	public class CacheBlockWriter : CacheBlockFile {

		public CacheBlockWriter ( Stream stream ) : base ( stream ) {
		}

		public static IEnumerable<FileEntry> GetFileEntries ( string directory ) {
			if ( directory is null ) throw new ArgumentNullException ( nameof ( directory ) );
			directory = IOHelpers.NormalizeDirectory ( directory );
			if ( !Directory.Exists ( directory ) ) throw new IOException ( $"Source directory '{directory}' does not exist." );
			if ( Directory.EnumerateFiles ( directory , "*" ).Any () ) throw new IOException ( $"Source directory '{directory}' has files in it. It should only contain directories." );
			return Directory
				.EnumerateFiles ( directory , "*" , SearchOption.AllDirectories )
				.Select ( a => FileEntry.FromExternalName ( a.Substring ( directory.Length ) ) )
				;
		}

		public void Pack ( string sourceDirectory , IReadOnlyCollection<FileEntry> entries ) {
			if ( sourceDirectory is null ) throw new ArgumentNullException ( nameof ( sourceDirectory ) );
			if ( entries is null ) throw new ArgumentNullException ( nameof ( entries ) );
			WriteHeader ( entries.Count );
			WriteNamesTable ( entries );
			Stream.WriteByte ( 1 );
			WriteOffsetsSizesZeroesAndData ( sourceDirectory , entries );
		}

		private void WriteOffsetsSizesZeroesAndData ( string sourceDirectory , IReadOnlyCollection<FileEntry> entries ) {
			var offsetsPosition = Stream.Position;
			var sizesPosition = offsetsPosition + 8L * entries.Count + 1;
			var zeroesPosition = sizesPosition + 4L * entries.Count + 1;
			BaseOffset = zeroesPosition + 4L * entries.Count;

			var i = 0;
			long offset = 0;
			foreach ( var item in entries ) {
				Console.Write ( $"\rPacking: {i + 1}/{entries.Count}" );
				var sourceLocation = Path.Combine ( sourceDirectory , item.ExternalName );
				using ( var source = File.Open ( sourceLocation , FileMode.Open , FileAccess.Read , FileShare.None ) ) {
					var size = MiscHelpers.EnsureValidFileSize ( source.Length );

					Stream.Position = offsetsPosition + 8L * i;
					Stream.WriteValue ( offset );
					Stream.Position = sizesPosition + 4L * i;
					Stream.WriteValue ( size );
					Stream.Position = zeroesPosition + 4L * i;
					Stream.WriteValue ( 0 );

					Stream.Position = BaseOffset + offset;
					source.CopyBytesTo ( Stream , size );
					offset += size;
				}
				i++;
			}
			Console.WriteLine ();
		}

		private void WriteNamesTable ( IReadOnlyCollection<FileEntry> entries ) {
			foreach ( var item in entries ) {
				Stream.WriteValue ( item.InternalName.Length );
				Stream.WriteString ( item.InternalName );
			}
		}

		private void WriteHeader ( int count ) {
			Stream.Write ( Signature , 0 , Signature.Length );
			Stream.WriteValue ( 1 );
			Stream.WriteByte ( 1 );
			Stream.WriteValue ( count );
			Stream.WriteValue ( 4 );
			Stream.WriteByte ( 1 );
		}

	}

}
