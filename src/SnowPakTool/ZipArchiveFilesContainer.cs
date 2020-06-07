using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SnowPakTool {

	/// <summary>
	/// <see cref="FilesContainer"/> representation of a ZIP (PAK) archive.
	/// </summary>
	public class ZipArchiveFilesContainer : FilesContainer {

		public static IEnumerable<string> Extensions { get; } = new string[] { ".zip" , ".pak" };


		/// <summary>
		/// Creates <see cref="ZipArchiveFilesContainer" />.
		/// </summary>
		public ZipArchiveFilesContainer ( string location ) : base ( location ) {
			NormalizedLocation = Path.GetFullPath ( location );
		}

		public override string NormalizedLocation { get; }
		public override bool IsSingleFile => true;
		public override bool Exists () => File.Exists ( NormalizedLocation );

		public override IReadOnlyList<string> GetFiles () {
			using var stream = File.OpenRead ( NormalizedLocation );
			using var zip = new ZipArchive ( stream , ZipArchiveMode.Read );
			return zip.Entries.Select ( a => a.FullName ).ToList ();
		}

	}

}
