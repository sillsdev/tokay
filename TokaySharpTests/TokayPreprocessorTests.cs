using System.IO;
using NUnit.Framework;
using TokaySharp;

namespace TokaySharpTests
{
	[TestFixture]
	public class TokayPreprocessorTests
	{
		[Test]
		public void PreprocessFile_NothingToEmbed_ReturnsATempFileDuplicate()
		{

			var contents = "<html><body></body></html>";
			using (var tmp = new TempFile(contents))
			{
				var resultPath = TokaySharp.TokayPreprocessor.PreprocessFile(tmp.Path);
				Assert.AreEqual(contents,File.ReadAllText(resultPath));
			}
		}
		[Test]
		public void PreprocessFile_HasEmbeddedFileInSameDirector_ReturnsATempFileWhichIncludesIt()
		{
			var hostFileContents = "<html><body><div id='blah'><div id='StickHelloInHere' data-embedhtml='hello.html'></div></div></body></html>";
			var helloPluginContents = "<html><body><textarea>hello</textarea></body></html>";
			using (var hostFile = new TempFile(hostFileContents))
			{
				var helloPluginPath = Path.Combine(Path.GetDirectoryName(hostFile.Path), "hello.html");
				File.WriteAllText(helloPluginPath,helloPluginContents);
				using (TempFile.TrackExisting(helloPluginPath))
				{
					var resultPath = TokayPreprocessor.PreprocessFile(hostFile.Path);
					Assert.AreEqual(
						"<html><body><div id='blah'><div id='StickHelloInHere' data-embedhtml='hello.html'><textarea>hello</textarea></div></div></body></html>",
						File.ReadAllText(resultPath));
				}
			}
		}
	}
}
