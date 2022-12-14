using System;
using System.IO;

namespace RegionOrebroLan.EPiServer.Data.IO.Extensions
{
	public static class PathExtension
	{
		#region Methods

		public static string GetFullPath(string path, string basePath)
		{
			if(path == null)
				throw new ArgumentNullException(nameof(path));

			if(basePath == null)
				throw new ArgumentNullException(nameof(basePath));

			/*
				When we are at EPiServer 12 we can do:

				return Path.GetFullPath(path, basePath);
			*/

			if(Path.IsPathRooted(path))
				return path;

			return Path.Combine(basePath.TrimEnd(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), path.TrimStart(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
		}

		#endregion
	}
}