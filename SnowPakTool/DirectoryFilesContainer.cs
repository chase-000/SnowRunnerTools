using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SnowPakTool {

	/// <summary>
	/// <see cref="FilesContainer"/> representation of a file system directory.
	/// </summary>
	public class DirectoryFilesContainer : FilesContainer {

		/// <summary>
		/// Creates <see cref="DirectoryFilesContainer" />.
		/// </summary>
		public DirectoryFilesContainer ( string location ) : base ( location ) {
			NormalizedLocation = IOHelpers.NormalizeDirectory ( location );
		}

		public override string NormalizedLocation { get; }
		public override bool IsSingleFile => false;
		public override bool Exists () => Directory.Exists ( NormalizedLocation );

		public override IReadOnlyList<string> GetFiles () {
			return Directory
				.GetFiles ( NormalizedLocation , "*" , SearchOption.AllDirectories )
				.Select ( a => a.Substring ( NormalizedLocation.Length ) )
				.ToList ();
		}

	}

}
