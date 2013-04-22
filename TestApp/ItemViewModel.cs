using Knockout.Net;

namespace TestApp
{
	public enum EnumTypes
	{
		Type1,
		Type2,
		Type3
	}

	public class ItemViewModel : ObservableObject
	{
		private readonly string _name;
		private EnumTypes _type;

		public ItemViewModel(string name, EnumTypes type)
		{
			_name = name;
			_type = type;
		}

		public string Name
		{
			get { return _name; }
		}

		public EnumTypes Type
		{
			get { return _type; }
			set { Set(() => Type, ref _type, value); }
		}
	}
}
