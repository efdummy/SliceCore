using System;
using System.IO;

// # include <sys/utime.h>

namespace SliceCore
{
    class Program
    {
	    private const int FILE_NAME_SIZE = 125;
		private const string SEPARATOR = "\n________________________________________________________________________";
		
		// Errors returned to the cmd line
		private enum MainErrors
            {
			NoError=0,
			NotAChunkFile=1,
			SecondParamIsNotAnInteger=2,
			ToManyParameters=3
        }

		static int Main(string[] args)
        {
			int rc = (int)MainErrors.NoError;

			switch (args.Length)
			{
				case 0:
					Usage();
					break;
				case 1:
					//_______ Unslice
					// Looking for CHUNK marker in file name
					if (!args[0].Contains(CHUNK))
						rc=(int)MainErrors.NotAChunkFile;
					else
					    Unslice(args[0]);
					break;
				case 2:
					//_______ Slice
					// Size of chunks is specified
					int size;
					bool ok = int.TryParse(args[1], out size);
					if (ok) Slice(args[0], size);
					else rc = (int)MainErrors.SecondParamIsNotAnInteger;
					break;
				default:
					Usage();
					rc = (int)MainErrors.ToManyParameters;
					break;
			}
			return rc;
		}

		private static void Pub()
		{
			Console.WriteLine(SEPARATOR);
			Console.WriteLine("");
			Console.WriteLine("If you like slice and have any question go to https://github.com/efdummy/SliceCore/");
			Console.WriteLine(SEPARATOR);
		}
		private static void Usage()
		{
			Pub();
			Console.WriteLine();
			Console.WriteLine("SLICE cut your big files in chunks or rebuild them from the chunks.");
			Console.WriteLine("Syntax to slice   : slice filename [size_of_chunks]");
			Console.WriteLine("Syntax to unslice : slice filename.chunk.1");
			Console.WriteLine("Default value for size_of_chunks is 5 Mo");
			Console.WriteLine();
			Console.WriteLine("To slice the 10 Mo bigvideo.mp4 file in two 5 Mo chunks:");
			Console.WriteLine("C:\\> slice bigvideo.mp4");
			Console.WriteLine();
			Console.WriteLine("To slice the 10 Mo bigvideo.mp4 file in five 2 Mo chunks:");
			Console.WriteLine("C:\\> slice bigvideo.mp4 2000000");
			Console.WriteLine();
			Console.WriteLine("To rebuild the bigvideo.mp4 from the chunks:");
			Console.WriteLine("C:\\> slice bigvideo.mp4.chunk.1");
			Console.WriteLine("");
			Console.WriteLine("NB : all the chunks must be in the same folder");
			Console.WriteLine(SEPARATOR);
		}

		const string CHUNK = ".chunk.";

		private static void BuildChunkFileName(string szFile, int iChunkNumber, out string szChunkFileName)
		{
			string szFileNum;

			szChunkFileName=szFile;
			szChunkFileName+=CHUNK;
			szFileNum=iChunkNumber.ToString();
			szChunkFileName+=szFileNum;
		}

		private static void BuildChunkFileNameFromChunk(string szChunkFile, int iChunkNumber, out string szChunkFileName)
		{
			string szFileNum;
			int indexOfChunkMark=szChunkFile.LastIndexOf(CHUNK);

			szChunkFileName=szChunkFile.Substring(0, indexOfChunkMark);
			szChunkFileName+=CHUNK;
			szFileNum=iChunkNumber.ToString();
			szChunkFileName+=szFileNum;
		}
		private static void BuildFileNameFromChunk(string szChunkFile, out string szFileName)
		{
			int n=szChunkFile.LastIndexOf(CHUNK);

			szFileName = szChunkFile.Substring(0, n);
		}

		// Get file creation, last write and last access times
		private static void GetFileUpdateTime(string filePath, out DateTime creationTime, out DateTime lastWriteTime, out DateTime lastAccessTime)
		{
			creationTime=File.GetCreationTime(filePath);
			lastWriteTime=File.GetLastWriteTime(filePath);
			lastAccessTime=File.GetLastAccessTime(filePath);
		}

		// Set file creation, last write and last access times
		private static void SetFileUpdateTime(string filePath, DateTime creationTime, DateTime lastWriteTime, DateTime lastAccessTime)
		{
			File.SetCreationTime(filePath, creationTime);
			File.SetLastWriteTime(filePath, lastWriteTime);
			File.SetLastAccessTime(filePath, lastAccessTime);
		}

		private static void Slice(string szFile, int lChunkMax)
		{
			FileStream input;
			FileStream output;
			
			byte[] buffer=new byte[lChunkMax];
			string szCurrentChunkFile;
			bool flContinue;
			int nbChar, iCurrentChunkNum, lBytes;

			flContinue = true;
			// Test if file exist
			if (!File.Exists(szFile)) flContinue = false;
			// Open input file
			input = new FileStream(szFile, FileMode.Open, FileAccess.Read, FileShare.Read);

			if (flContinue)
			{
				if (input==null)
				{
					Console.WriteLine("\nError {0} while opening {1}\n", 1, szFile);
					flContinue = false;
				}
			}

			if (flContinue)
			{
				// Memorize file's last date and time
				DateTime creationTime, lastWriteTime, lastAccessTime;
				GetFileUpdateTime(szFile, out creationTime, out lastWriteTime, out lastAccessTime);

				Console.WriteLine("I'm slicing {0} in {1} bytes chunks, please wait...", szFile, lChunkMax);

				lBytes=0;
				iCurrentChunkNum = 0;

				while ((nbChar = input.Read(buffer, 0, lChunkMax)) >0)
				{
					// Initialize chunk file name
					++iCurrentChunkNum;
					BuildChunkFileName(szFile, iCurrentChunkNum, out szCurrentChunkFile);
					Console.WriteLine("Slice {0}...", szCurrentChunkFile);
					output = new FileStream(szCurrentChunkFile, FileMode.Create, FileAccess.Write, FileShare.None);
					output.Write(buffer, 0, nbChar);
					output.Close();
					lBytes += nbChar;
				}

				// Close the input file
				input.Close();

				// Set chuncks last access and last write time
				for (int i=1;i<=iCurrentChunkNum;i++)
				{
					BuildChunkFileName(szFile, i, out szCurrentChunkFile);
					// Set file's attributes only if file exists
					if (File.Exists(szCurrentChunkFile)) SetFileUpdateTime(szCurrentChunkFile, creationTime, lastWriteTime, lastAccessTime);
				}
				Console.WriteLine("Everything sliced ({0} chunks & {1} bytes).\n", iCurrentChunkNum, lBytes);
			}
		}

		//_______ USLICE
		private const int IO_BLOCK_SIZE = 7000000;
		private static void Unslice(string szChunkFile)
		{
			FileStream input;
			FileStream output;
			byte[] buffer = new byte[IO_BLOCK_SIZE];
			string szCurrentChunkFile, szCurrentFile;
			bool flContinue;
			int nbChar, iCurrentChunkNum, lBytes;
			
			flContinue = true;

			if (flContinue)
			{
				// Memorize file's last date and time
				DateTime creationTime, lastWriteTime, lastAccessTime;
				GetFileUpdateTime(szChunkFile, out creationTime, out lastWriteTime, out lastAccessTime);

				Console.WriteLine("I'm unslicing {0}, please wait...", szChunkFile);

				// Create output file
				BuildFileNameFromChunk(szChunkFile, out szCurrentFile);
				output = new FileStream(szCurrentFile, FileMode.Create, FileAccess.Write, FileShare.None);
				if (output == null)
				{
					Console.WriteLine("Error {0} while opening {1}", 1, szCurrentFile);
					flContinue = false;
				}

				bool ThereIsMoreChunk = true;

				lBytes = 0;
				iCurrentChunkNum = 0;
				while (ThereIsMoreChunk)
                {
					// Initialize chunk file name
					iCurrentChunkNum++;
					BuildChunkFileNameFromChunk(szChunkFile, iCurrentChunkNum, out szCurrentChunkFile);
					// Open first chunk
					if (File.Exists(szCurrentChunkFile))
					{
						Console.WriteLine("Unslice {0}...", szCurrentChunkFile);
						input = new FileStream(szCurrentChunkFile, FileMode.Open, FileAccess.Read, FileShare.Read);
						// While there are bytes
						while ((nbChar = input.Read(buffer, 0, IO_BLOCK_SIZE)) > 0)
						{
							lBytes += nbChar;
							output.Write(buffer, 0, nbChar);
						}
						input.Close();
					}
					else
						ThereIsMoreChunk = false;
				}

				output.Close();

				// Set output file's last update time
				SetFileUpdateTime(szCurrentFile, creationTime, lastWriteTime, lastAccessTime);

				Console.WriteLine("Everything unsliced ({0} bytes).", lBytes);
			}
		}

	}
}

