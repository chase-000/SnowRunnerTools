using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using SnowPakTool;

namespace SnowTruckConfig {

	public static class Program {

		/*
			truck CustomizationCameras --FOV:40 "D:\Games\SnowRunner_backs\mods\.staging\initial-pak\[media]\classes\trucks\ank_mk38.xml"
		*/

		public static int Main ( string[] args ) {

			var root = new RootCommand ( typeof ( Program ).Assembly.GetCustomAttribute<AssemblyTitleAttribute> ().Title );
			root.AddLicenseOption ();


			var cmdTruck = new Command ( "truck" , "Trucks configuration" );
			root.Add ( cmdTruck );

			var cmdTruckCustomizationCameras = new Command ( "CustomizationCameras" );
			cmdTruckCustomizationCameras.AddOption ( new Option<int> ( "--FOV" ) { Required = true } );
			var targetXmlArgument = new Argument<FileInfo> ( "targetXml" ).ExistingOnly ();
			cmdTruckCustomizationCameras.AddArgument ( targetXmlArgument );
			cmdTruckCustomizationCameras.Handler = CommandHandler.Create<FileInfo , int> ( DoTruckCustomizationCameras );
			cmdTruck.Add ( cmdTruckCustomizationCameras );


			var cmdTruckCraneSocket = new Command ( "CraneSocket" );
			cmdTruck.Add ( cmdTruckCraneSocket );

			var cmdTruckCraneSocketAdd = new Command ( "add" );
			cmdTruckCraneSocket.Add ( cmdTruckCraneSocketAdd );

			var cmdTruckCraneSocketAddTopCentral = new Command ( "top-central" );
			cmdTruckCraneSocketAddTopCentral.AddArgument ( targetXmlArgument );
			cmdTruckCraneSocketAddTopCentral.Handler = CommandHandler.Create<FileInfo> ( DoTruckCraneSocketAddTopCentral );
			cmdTruckCraneSocketAdd.Add ( cmdTruckCraneSocketAddTopCentral );


			var cmdTruckExtents = new Command ( "extents" );
			cmdTruckExtents.AddArgument ( targetXmlArgument );
			cmdTruckExtents.Handler = CommandHandler.Create<FileInfo> ( DoTruckExtents );
			cmdTruck.Add ( cmdTruckExtents );


			var cmdExtras = new Command ( "extras" , "Extra configuration" );
			root.Add ( cmdExtras );

			var cmdExtrasRename = new Command ( "rename" , "Rename game objects" );
			cmdExtras.Add ( cmdExtrasRename );

			var cmdExtrasRenameTires = new Command ( "tires" , "Rename tires" );
			var languageOption = new Option<GameLanguage> ( "--language" ) { Required = true };
			cmdExtrasRenameTires.AddOption ( languageOption );
			var directoryArgument = new Argument<DirectoryInfo> ( "directory" , "Path to the directory with mixed contents of initial.pak and initial.pak\\initial.cache_block" ).ExistingOnly ();
			cmdExtrasRenameTires.AddArgument ( directoryArgument );
			cmdExtrasRenameTires.Handler = CommandHandler.Create<DirectoryInfo , GameLanguage> ( ExtrasRenamer.RenameTires );
			cmdExtrasRename.Add ( cmdExtrasRenameTires );

			var cmdExtrasRenameTrucks = new Command ( "trucks" , "Rename trucks" );
			cmdExtrasRenameTrucks.AddOption ( languageOption );
			cmdExtrasRenameTrucks.AddArgument ( directoryArgument );
			cmdExtrasRenameTrucks.Handler = CommandHandler.Create<DirectoryInfo , GameLanguage> ( ExtrasRenamer.RenameTrucks );
			cmdExtrasRename.Add ( cmdExtrasRenameTrucks );

			var cmdExtrasRenameEngines = new Command ( "engines" , "Rename engines" );
			cmdExtrasRenameEngines.AddOption ( languageOption );
			cmdExtrasRenameEngines.AddArgument ( directoryArgument );
			cmdExtrasRenameEngines.Handler = CommandHandler.Create<DirectoryInfo , GameLanguage> ( ExtrasRenamer.RenameEngines );
			cmdExtrasRename.Add ( cmdExtrasRenameEngines );

			return root.InvokeWithMiddleware ( args , CommandLineExtensions.MakePrintLicenseResourceMiddleware ( typeof ( Program ) ) );
		}

		private static void DoTruckCustomizationCameras ( FileInfo targetXml , int fov ) {
			var xml = XmlHelpers.ReadFragments ( targetXml.FullName );
			SetCustomizationCamerasFov ( xml , fov );
			XmlHelpers.WriteFragments ( targetXml.FullName , xml.Nodes () );
		}

		private static void SetCustomizationCamerasFov ( XElement xml , int fov ) {
			var positions = xml.Element ( "Truck" ).Element ( "GameData" ).Element ( "CustomizationCameras" ).Elements ( "CameraPos" );
			foreach ( var position in positions ) {
				position.Attribute ( "FOV" ).SetValue ( fov );
			}
		}

		private static void DoTruckExtents ( FileInfo targetXml ) {
			var xml = XmlHelpers.ReadFragments ( targetXml.FullName );
			var extents = TruckHelpers.GetExtents ( xml.Element ( "Truck" ) );
			Console.WriteLine ( $"Extents: {(extents.minX, extents.minY, extents.minZ)} .. {(extents.maxX, extents.maxY, extents.maxZ)}" );
		}

		private static int DoTruckCraneSocketAddTopCentral ( FileInfo targetXml ) {
			var xml = XmlHelpers.ReadFragments ( targetXml.FullName );
			var truck = xml.Element ( "Truck" );
			var lastCraneSocket = truck.Element ( "GameData" ).Elements ( "CraneSocket" ).LastOrDefault ();
			if ( lastCraneSocket == null ) {
				Console.WriteLine ( "No existing crane sockets found!" );
				return 1;
			}
			var extents = TruckHelpers.GetExtents ( truck );
			var pos = $"(0; {extents.maxY:0.###}; 0)";
			Console.WriteLine ( $"{targetXml.Name}: adding new crane socket at {pos}" );
			var newCraneSocket = new XElement ( "CraneSocket" , new XAttribute ( "Pos" , pos ) );
			lastCraneSocket.AddAfterSelf ( newCraneSocket );
			lastCraneSocket.AddAfterSelf ( new XText ( "\r\n\t\t" ) );
			XmlHelpers.WriteFragments ( targetXml.FullName , xml.Nodes () );
			return 0;
		}

	}

}
