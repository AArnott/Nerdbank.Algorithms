using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Web.Caching;
using System.CodeDom.Compiler;

namespace NerdBank.Tools
{
	/// <summary>
	/// Summary description for StreamUtilities.
	/// </summary>
	public class StreamUtilities
	{
		/// <summary>
		/// Converts a stream to a persisted file.
		/// </summary>
		/// <param name="stream">
		/// The stream to convert to a file.
		/// </param>
		/// <param name="filename">
		/// The path and filename to save the new file as.
		/// </param>
		public static void Save(Stream stream, string filename)
		{
			if( stream == null ) throw new ArgumentNullException("stream");
			if( filename == null || filename.Length == 0 ) throw new ArgumentNullException("filename");

			// Create the file by the name specified
			StreamWriter sw = new StreamWriter(filename);
			
			// Forward the contents of the input stream directly into
			// the file stream.
			int byteRead;
			while( (byteRead = stream.ReadByte()) >= 0 )
				sw.Write((byte)byteRead);

			// Close the stream we were writing to
			sw.Close();
			// Close the stream we were reading from
			stream.Close();
		}

		/// <summary>
		/// Saves the stream to a temporary file.
		/// </summary>
		/// <param name="stream">
		/// The stream to persist to disk.
		/// </param>
		/// <returns>
		/// The name of the temporary file used.
		/// </returns>
		/// <remarks>
		/// The temporary file is NOT automatically removed.
		/// </remarks>
		public static string Save(Stream stream)
		{
			return SaveTemporary(stream, "tmp");
		}
		/// <summary>
		/// Saves the stream to a temporary file.
		/// </summary>
		/// <param name="stream">
		/// The stream to persist to disk.
		/// </param>
		/// <param name="fileExtension">
		/// The extension of the temporary file to be created.
		/// </param>
		/// <returns>
		/// The name of the temporary file used.
		/// </returns>
		/// <remarks>
		/// The temporary file is NOT automatically removed.
		/// </remarks>
		public static string SaveTemporary(Stream stream, string fileExtension)
		{
			if( stream == null ) throw new ArgumentNullException("stream");

			using( TempFileCollection tfc = new TempFileCollection() )
			{
				string filename = tfc.AddExtension(fileExtension, true);
				Save(stream, filename);
				return filename;
			}
		}

		/// <summary>
		/// Sends the contents of a file down a stream and deletes the temporary file.
		/// </summary>
		/// <param name="filename">
		/// The name of the temporary file.
		/// </param>
		/// <returns>
		/// The stream that contains the last copy of the file's contents.
		/// </returns>
		public static Stream ConvertFileToStreamAndDelete(string filename)
		{
			if( filename == null || filename.Length == 0 ) throw new ArgumentNullException("filename");

			// Create a memory stream to buffer the entire contents of the file
			FileInfo fi = new FileInfo(filename);
			MemoryStream ms = new MemoryStream((int)fi.Length);
			
			// Open the file and read its contents into the memory stream
			FileStream fs = fi.OpenRead();
			int byteRead;
			while( (byteRead = fs.ReadByte()) >= 0 )
				ms.WriteByte((byte)byteRead);			
			ms.Position = 0; // start at beginning of file contents

			// Delete the temporary file
			fi.Delete();

			// Return the in-memory copy
			return ms;
		}
	}
}
