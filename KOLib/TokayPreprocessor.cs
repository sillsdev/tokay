using System;
using System.IO;
using HtmlAgilityPack;

namespace TokaySharp
{
	public class TokayPreprocessor
	{
		/// <summary>
		/// Processes data-embedhtml elements
		/// </summary>
		/// <param name="pathToHtml"></param>
		/// <returns>a path to a new temp file that the caller is reponsible for deleting</returns>
		public static string PreprocessFile(string pathToHtml)
		{
			HtmlDocument hostDocument = new HtmlDocument();
			hostDocument.Load(pathToHtml);
			foreach (var node in hostDocument.DocumentNode.SelectNodes("//*[@data-embed]"))
			{
				string relativePath = node.GetAttributeValue("data-embed", "");
				if(string.IsNullOrEmpty(relativePath))
					throw new ApplicationException("Found empty data-embed in "+pathToHtml);
				var plugInPath = Path.Combine(Path.GetDirectoryName(pathToHtml), relativePath);
				if(!File.Exists(plugInPath))
					throw new ApplicationException(pathToHtml+" requested that we embed "+relativePath+" but it could not be found at "+plugInPath);

				var pluginHtmlDocument = new HtmlDocument();
				pluginHtmlDocument.Load(plugInPath);
				node.InnerHtml = pluginHtmlDocument.DocumentNode.SelectSingleNode("body").InnerHtml;
			}

			var resultPath = TempFile.GetHtmlTempPath();
			hostDocument.Save(resultPath);

			return resultPath;
		}
	}
}