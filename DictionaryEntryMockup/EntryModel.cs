using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Tokay;

namespace DictionaryEntryMockup
{
	public class EntryModel : ObservableObject
	{
		private string _form;
		ObservableCollection<SenseModel> _senses = new ObservableCollection<SenseModel>();

		public string Form
		{
			get { return _form; }
			set { Set(() => Form, ref _form, value); }
		}

		public ObservableCollection<SenseModel> Senses
		{
			get { return _senses; }
		}
	}
}
