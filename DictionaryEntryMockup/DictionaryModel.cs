using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Tokay;

namespace DictionaryEntryMockup
{
	public class DictionaryModel : ObservableObject
	{
		private string _title;
		ObservableCollection<EntryModel> _entries = new ObservableCollection<EntryModel>();

		public string Title
		{
			get { return _title; }
			set { Set(() => Title, ref _title, value); }
		}

		public ObservableCollection<EntryModel> Entries
		{
			get { return _entries; }
		}

		public DictionaryModel()
		{
			Title = "Kalaba";
			var entry1 = new EntryModel() {Form="palaver"};
			_entries.Add(entry1);
			var entry2 = new EntryModel() { Form = "converso" };
			_entries.Add(entry2);

			var sense1_1 = new SenseModel() {Gloss = "chatter"};
			entry1.Senses.Add(sense1_1);
			var sense1_2 = new SenseModel() { Gloss = "champion" };
			entry1.Senses.Add(sense1_2);
			var sense2_1 = new SenseModel() { Gloss = "conversation" };
			entry2.Senses.Add(sense2_1);
		}
	}
}
