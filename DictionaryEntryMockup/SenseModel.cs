using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tokay;

namespace DictionaryEntryMockup
{
	public class SenseModel : ObservableObject
	{
		private string _gloss;

		public string Gloss
		{
			get { return _gloss; }
			set { Set(() => Gloss, ref _gloss, value); }
		}
	}
}
