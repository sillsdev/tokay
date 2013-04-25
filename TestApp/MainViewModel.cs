using System;
using System.Collections.ObjectModel;
using TokaySharp;

namespace TestApp
{
	public class MainViewModel : ObservableObject
	{
		private string _text = "Hello";
		private string _newItemName;
		private int _counter;
		private readonly ICommand _incrementCounterCommand;
		private readonly ICommand _decrementCounterCommand;
		private readonly ObservableCollection<ItemViewModel> _items;
		private readonly ICommand _addItemCommand;
		private readonly ICommand _removeItemCommand;
		private readonly ICommand _forceGCCommand;
		private readonly ReadOnlyCollection<string> _options;
		private string _selectedOption;

		public MainViewModel()
		{
			_items = new ObservableCollection<ItemViewModel> {new ItemViewModel("Test1", EnumTypes.Type1), new ItemViewModel("Test2", EnumTypes.Type2)};
			_incrementCounterCommand = new RelayCommand(() => Counter++, () => Counter < 10);
			_decrementCounterCommand = new RelayCommand(() => Counter--, () => Counter > 0);
			_addItemCommand = new RelayCommand(() =>
				{
					_items.Add(new ItemViewModel(_newItemName, EnumTypes.Type3));
					NewItemName = null;
				});
			_removeItemCommand = new RelayCommand<ItemViewModel>(item => _items.Remove(item));
			_forceGCCommand = new RelayCommand(GC.Collect);
			_options = new ReadOnlyCollection<string>(new [] {"Thailand", "United States", "Australia", "Laos", "United Kingdom", "New Zealand"} );
		}

		public string Text
		{
			get { return _text; }
			set { Set(() => Text, ref _text, value); }
		}

		public int Counter
		{
			get { return _counter; }
			set { Set(() => Counter, ref _counter, value); }
		}

		public ObservableCollection<ItemViewModel> Items
		{
			get { return _items; }
		}

		public string NewItemName
		{
			get { return _newItemName; }
			set { Set(() => NewItemName, ref _newItemName, value); }
		}

		public ICommand IncrementCounterCommand
		{
			get { return _incrementCounterCommand; }
		}

		public ICommand DecrementCounterCommand
		{
			get { return _decrementCounterCommand; }
		}

		public ICommand AddItemCommand
		{
			get { return _addItemCommand; }
		}

		public ICommand RemoveItemCommand
		{
			get { return _removeItemCommand; }
		}

		public ICommand ForceGCCommand
		{
			get { return _forceGCCommand; }
		}

		public ReadOnlyCollection<string> Options
		{
			get { return _options; }
		}

		public string SelectedOption
		{
			get { return _selectedOption; }
			set { Set(() => SelectedOption, ref _selectedOption, value); }
		}
	}
}
