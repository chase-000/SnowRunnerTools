using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace SnowTruckConfig {

	public static class XmlHelpers {

		public static IEnumerable<XNode> ReadFragmentNodes ( XmlReader reader ) {
			reader.MoveToContent ();
			while ( !reader.EOF ) {
				var node = XNode.ReadFrom ( reader );
				if ( node == null ) break;
				yield return node;
			}
		}

		public static XElement ReadFragments ( string location ) {
			if ( location is null ) throw new ArgumentNullException ( nameof ( location ) );
			using var reader = XmlReader.Create ( location , new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment } );
			return new XElement ( "root" , ReadFragmentNodes ( reader ) );
		}

		public static void WriteFragments ( string location , IEnumerable<XNode> nodes ) {
			if ( location is null ) throw new ArgumentNullException ( nameof ( location ) );
			if ( nodes is null ) throw new ArgumentNullException ( nameof ( nodes ) );
			using var writer = XmlWriter.Create ( location , new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment } );
			foreach ( var node in nodes ) {
				node.WriteTo ( writer );
			}
		}

	}
}
