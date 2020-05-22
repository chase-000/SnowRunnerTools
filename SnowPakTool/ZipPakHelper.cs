using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace SnowPakTool {

	public static class ZipPakHelper {

		public static string LoadListName { get; } = "pak.load_list";

		private static readonly byte[] __DummyBuffer = new byte[0];
		private const int ZipVersion = 0x14;


		public static void CreatePak ( string sourceDirectory , string pakLocation ) {
			if ( sourceDirectory is null ) throw new ArgumentNullException ( nameof ( sourceDirectory ) );
			if ( pakLocation is null ) throw new ArgumentNullException ( nameof ( pakLocation ) );

			sourceDirectory = IOHelpers.NormalizeDirectory ( sourceDirectory );
			if ( !Directory.Exists ( sourceDirectory ) ) throw new IOException ( $"Source directory '{sourceDirectory}' does not exist." );

			var listLocation = Path.Combine ( sourceDirectory , LoadListName );
			if ( !File.Exists ( listLocation ) ) throw new IOException ( $"List file '{listLocation}' does not exist." );

			Console.WriteLine ( "Preparing ordered zip file..." );
			using ( var zipStream = File.Open ( pakLocation , FileMode.CreateNew , FileAccess.ReadWrite , FileShare.None ) ) {
				WriteListFile ( zipStream , listLocation );
				using ( var zip = new ZipArchive ( zipStream , ZipArchiveMode.Update ) ) {
					AddFiles ( zip , sourceDirectory );
					Console.WriteLine ( "Compressing..." );
				}
			}
			Console.WriteLine ( "Done." );
		}


		private static void AddFiles ( ZipArchive zip , string sourceDirectory ) {
			var files = Directory.GetFiles ( sourceDirectory , "*" , SearchOption.AllDirectories );
			var i = 0;
			var skipped = false;
			foreach ( var location in Directory.EnumerateFiles ( sourceDirectory , "*" , SearchOption.AllDirectories ) ) {
				Console.Write ( $"\rAdding file {++i}/{files.Length}" );
				var relativeName = location.Substring ( sourceDirectory.Length );
				if ( !skipped && relativeName.Equals ( LoadListName , StringComparison.OrdinalIgnoreCase ) ) {
					skipped = true;
					continue;
				}
				zip.CreateEntryFromFile ( location , relativeName , CompressionLevel.Fastest );
			}
			Console.WriteLine ();
		}

		/// <summary>
		/// The list file must be the first file in the archive, stored without compression.
		/// </summary>
		/// <remarks>
		/// <see cref="ZipArchive"/> does not support real store (0) method and uses deflate instead, so it's written here manually.
		/// </remarks>
		private static void WriteListFile ( Stream zipStream , string listLocation ) {
			using var listStream = File.OpenRead ( listLocation );
			var size = MiscHelpers.EnsureFitsInt32 ( listStream.Length );
			var nameBytes = MiscHelpers.Encoding.GetBytes ( LoadListName );

			var lfh = new LocalFileHeader {
				Signature = LocalFileHeader.DefaultSignature ,
				Version = ZipVersion ,
				Flags = 0 ,
				Compression = 0 ,
				Time = 0 ,
				Date = 0 ,
				Crc32 = ComputeCrc32 ( listStream , size ) ,
				CompressedSize = size ,
				UncompressedSize = size ,
				NameLength = (short) nameBytes.Length ,
				ExtraLength = 0 ,
			};
			zipStream.WriteValue ( lfh );
			zipStream.Write ( nameBytes , 0 , nameBytes.Length );

			listStream.Position = 0;
			listStream.CopyTo ( zipStream );

			var centralDirectoryStart = MiscHelpers.EnsureFitsInt32 ( zipStream.Position );
			var cdfh = new CentralDirectoryFileHeader {
				Signature = CentralDirectoryFileHeader.DefaultSignature ,
				Version = ZipVersion ,
				VersionNeeded = ZipVersion ,
				Flags = 0 ,
				Compression = 0 ,
				Time = 0 ,
				Date = 0 ,
				Crc32 = lfh.Crc32 ,
				CompressedSize = size ,
				UncompressedSize = size ,
				NameLength = (short) nameBytes.Length ,
				ExtraLength = 0 ,
				CommentLength = 0 ,
				DiskNumber = 0 ,
				InternalAttributes = 0 ,
				ExternalAttributes = 0 ,
				LocalOffset = 0 ,
			};
			zipStream.WriteValue ( cdfh );
			zipStream.Write ( nameBytes , 0 , nameBytes.Length );

			var centralDirectoryEnd = MiscHelpers.EnsureFitsInt32 ( zipStream.Position );
			var eocd = new EndOfCentralDirectory {
				Signature = EndOfCentralDirectory.DefaultSignature ,
				DiskNumber = 0 ,
				CentralDirectoryDiskNumber = 0 ,
				DiskRecords = 1 ,
				TotalRecords = 1 ,
				CentralDirectorySize = centralDirectoryEnd - centralDirectoryStart ,
				CentralDirectoryOffset = centralDirectoryStart ,
				CommentLength = 0 ,
			};
			zipStream.WriteValue ( eocd );
		}

		public static int ComputeCrc32 ( this Stream stream , int length ) {
			using var hasher = new Crc32Managed ();
			stream.ProcessChunked ( length , ( buffer , read ) => hasher.TransformBlock ( buffer , 0 , read , buffer , 0 ) );
			hasher.TransformFinalBlock ( __DummyBuffer , 0 , 0 );
			return ( hasher.Hash[0] << 24 ) | ( hasher.Hash[1] << 16 ) | ( hasher.Hash[2] << 8 ) | hasher.Hash[3];
		}


		[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
		private struct LocalFileHeader {
			public const int DefaultSignature = 0x04034B50;
			public int Signature;
			public short Version;
			public short Flags;
			public short Compression;
			public short Time;
			public short Date;
			public int Crc32;
			public int CompressedSize;
			public int UncompressedSize;
			public short NameLength;
			public short ExtraLength;
		}

		[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
		private struct CentralDirectoryFileHeader {
			public const int DefaultSignature = 0x02014B50;
			public int Signature;
			public short Version;
			public short VersionNeeded;
			public short Flags;
			public short Compression;
			public short Time;
			public short Date;
			public int Crc32;
			public int CompressedSize;
			public int UncompressedSize;
			public short NameLength;
			public short ExtraLength;
			public short CommentLength;
			public short DiskNumber;
			public short InternalAttributes;
			public int ExternalAttributes;
			public int LocalOffset;
		}

		[StructLayout ( LayoutKind.Sequential , Pack = 1 )]
		private struct EndOfCentralDirectory {
			public const int DefaultSignature = 0x06054B50;
			public int Signature;
			public short DiskNumber;
			public short CentralDirectoryDiskNumber;
			public short DiskRecords;
			public short TotalRecords;
			public int CentralDirectorySize;
			public int CentralDirectoryOffset;
			public short CommentLength;
		}

	}

}
