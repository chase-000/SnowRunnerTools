﻿using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
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
			cmdTruckCustomizationCameras.AddArgument ( new Argument<FileInfo> ( "targetXml" ).ExistingOnly () );
			cmdTruckCustomizationCameras.Handler = CommandHandler.Create<FileInfo , int> ( DoTruckCustomizationCameras );
			cmdTruck.Add ( cmdTruckCustomizationCameras );


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

	}

}
