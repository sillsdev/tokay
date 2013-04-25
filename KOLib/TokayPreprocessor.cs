using System;
using System.IO;

using HtmlAgilityPack;

namespace TokaySharp
{
	/// <summary>
	/// NOTE: we aren't using this at the moment, and may never
	/// </summary>
	public class TokayPreprocessor
	{
		/// <summary>
		/// Processes data-embedhtml elements
		/// </summary>
		/// <param name="pathToHtml"></param>
		/// <returns>a path to a new temp file that the caller is reponsible for deleting</returns>
		public static string PreprocessFile(string pathToHtml)
		{
			var hostDocument = new HtmlAgilityPack.HtmlDocument();
			hostDocument.Load(pathToHtml);
			var embedNodes = hostDocument.DocumentNode.SelectNodes("//*[@data-embedhtml]");
			if(embedNodes!=null)
			{
				foreach (var node in embedNodes)
				{
					string relativePath = node.GetAttributeValue("data-embedhtml", "");
					if (string.IsNullOrEmpty(relativePath))
						throw new ApplicationException("Found empty data-embed in " + pathToHtml);
					var plugInPath = Path.Combine(Path.GetDirectoryName(pathToHtml), relativePath);
					if (!File.Exists(plugInPath))
						throw new ApplicationException(pathToHtml + " requested that we embed " + relativePath + " but it could not be found at " + plugInPath);

					var pluginHtmlDocument = new HtmlDocument();
					pluginHtmlDocument.Load(plugInPath);
					node.InnerHtml = pluginHtmlDocument.DocumentNode.SelectSingleNode("//body").InnerHtml;
				}
			}

			var resultPath = TempFile.GetHtmlTempPath();
			hostDocument.Save(resultPath);

			return resultPath;
		}
	}
}