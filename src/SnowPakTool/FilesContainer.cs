using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SnowPakTool {

	/// <summary>
	/// Files container: either a stream file or a file system directory.
	/// </summary>
	public abstract class FilesContainer {

		/// <summary>
		/// Creates <see cref="FilesContainer" />.
		/// </summary>
		protected FilesContainer ( string location ) {
			if ( location is null ) throw new ArgumentNullException ( nameof ( location ) );
			Location = location;

		}

		/// <summary>
		/// Creates a new <see cref="FilesContainer" /> depending on what <paramref name="location"/> points at.
		/// </summary>
		public static FilesContainer From ( string location ) {
			if ( location is null ) throw new ArgumentNullException ( nameof ( location ) );
			if ( location.Length == 0 ) throw new ArgumentException ( "Empty container location string." , nameof ( location ) );

			//a mix of guesstimation and definite creation based on object existence and location format
			if ( location[^1] == '\\' || Directory.Exists ( location ) ) {
				return new DirectoryFilesContainer ( location );
			}
			else if ( TryGetFileFactory ( location ) is var factory && factory != null ) {
				return factory ( location );
			}
			else if ( File.Exists ( location ) ) {
				throw new NotSupportedException ( $"Can't determine file format from name: '{location}'" );
			}
			else {
				//location could be either a file or a directory, but neither exists, nor is there a known extension; assume it's a directory
				return new DirectoryFilesContainer ( location );
			}
		}


		/// <summary>
		/// Normalized location string.
		/// </summary>
		public abstract string NormalizedLocation { get; }

		/// <summary>
		/// Original location string as it was passed into the ctor.
		/// </summary>
		public string Location { get; }

		/// <summary>
		/// Indicates whether the container is a single file or some other entity.
		/// </summary>
		public abstract bool IsSingleFile { get; }

		public abstract bool Exists ();

		public abstract IReadOnlyList<string> GetFiles ();



		private static Func<string , FilesContainer> TryGetFileFactory ( string location ) {
			if ( location is null ) throw new ArgumentNullException ( nameof ( location ) );
			var extension = Path.GetExtension ( location );
			if ( ZipArchiveFilesContainer.Extensions.Contains ( extension ) ) {
				return a => new ZipArchiveFilesContainer ( a );
			}
			return null;
		}

	}

}
