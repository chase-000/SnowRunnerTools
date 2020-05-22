using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PakTool {

	public class CacheBlockReader : CacheBlockFile {

		public CacheBlockReader ( Stream stream ) : base ( stream ) {
			var count = ReadHeader ();
			FileEntries = new FileEntry[count];
			ReadFileEntries ();
			BaseOffset = stream.Position;
		}


		public void UnpackAll ( string targetDirectory ) {
			Unpack ( targetDirectory , FileEntries );
		}

		public void Unpack ( string targetDirectory , IReadOnlyCollection<FileEntry> entries ) {
			var i = 0;
			foreach ( var item in entries ) {
				i++;
				Console.Write ( $"\rUnpacking: {i}/{entries.Count}" );
				var targetLocation = Path.Combine ( targetDirectory , item.ExternalName );
				Stream.Position = BaseOffset + item.RelativeOffset;
				Directory.CreateDirectory ( Path.GetDirectoryName ( targetLocation ) );
				using ( var target = File.Open ( targetLocation , FileMode.CreateNew , FileAccess.Write ) ) {
					Stream.CopyBytesTo ( target , item.Size );
				}
			}
			Console.WriteLine ();
		}


		private int ReadHeader () {
			var bytes = new byte[Signature.Length];
			var read = Stream.Read ( bytes , 0 , Signature.Length );
			if ( read < Signature.Length || !bytes.SequenceEqual ( Signature ) ) throw new InvalidDataException ( "Unknown file header format." );
			Stream.ReadMagicInt32 ( 1 );
			Stream.ReadMagicByte ( 1 );
			var count = Stream.ReadInt32 ();
			Stream.ReadMagicInt32 ( 4 );
			Stream.ReadMagicByte ( 1 );
			return count;
		}

		private void ReadFileEntries () {
			ReadNames ();
			Stream.ReadMagicByte ( 1 );
			ReadOffsets ();
			Stream.ReadMagicByte ( 1 );
			ReadSizes ();
			Stream.ReadMagicByte ( 1 );
			ReadZeroes ();
		}

		private void ReadNames () {
			for ( int i = 0; i < FileEntries.Length; i++ ) {
				var length = Stream.ReadInt32 ();
				var name = Stream.ReadString ( length );
				FileEntries[i] = FileEntry.FromInternalName ( name );
			}
		}

		private void ReadOffsets () {
			for ( int i = 0; i < FileEntries.Length; i++ ) {
				FileEntries[i].RelativeOffset = Stream.ReadInt64 ();
			}
		}

		private void ReadSizes () {
			for ( int i = 0; i < FileEntries.Length; i++ ) {
				FileEntries[i].Size = Stream.ReadInt32 ();
			}
		}

		private void ReadZeroes () {
			for ( int i = 0; i < FileEntries.Length; i++ ) {
				var zero = Stream.ReadInt32 ();
				if ( zero != 0 ) throw new InvalidDataException ();
			}
		}

	}

}
