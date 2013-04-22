using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Input;
using Gecko;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Knockout.Net
{
	public class KOControl : UserControl
	{
		private readonly ConditionalWeakTable<object, ObjectData> _objects;
		private readonly Dictionary<string, WeakReference> _idsToObjects; 
		private readonly GeckoWebBrowser _browser;
		private int _curID;
		private readonly SimpleMonitor _collectionMonitor;
		private readonly SimpleMonitor _propertyMonitor;
		private DateTime _lastCleanup;
		private readonly Func<string, object> _getObject;
		private string _currentView;
		private bool _loaded;
		private readonly HashSet<Type> _enumerations; 

		public KOControl(Func<string, object> getObject, string pathToStartupViewHtml)
		{
            if(!File.Exists(pathToStartupViewHtml))
                throw new ApplicationException(pathToStartupViewHtml + " does not exist");
			_getObject = getObject;
			_currentView = pathToStartupViewHtml;
			_collectionMonitor = new SimpleMonitor();
			_propertyMonitor = new SimpleMonitor();
			_lastCleanup = DateTime.Now;
			_enumerations = new HashSet<Type>();

			_objects = new ConditionalWeakTable<object, ObjectData>();
			_idsToObjects = new Dictionary<string, WeakReference>();

			_browser = new GeckoWebBrowser();
			Controls.Add(_browser);
			Application.Idle += Application_Idle;
			_browser.Dock = DockStyle.Fill;
			_browser.DisableWmImeSetContext = true;
			_browser.JavascriptError += _browser_JavascriptError;
		}

        public static void InitializeGeckoFx()//todo: make it so client doesn't need to call this explicitly
        {
            GeckoFxInitializer.SetUpXulRunner();
        }

	    public string CurrentView
		{
			get { return _currentView; }
			set
			{
				_currentView = value;
				if (_loaded)
					LoadCurrentView();
			}
		}

		private void Application_Idle(object sender, EventArgs e)
		{
			var now = DateTime.Now;
			TimeSpan span = now - _lastCleanup;
			if (span.TotalMilliseconds >= 30000)
			{
				CleanupInactiveObjects();
				_lastCleanup = now;
			}
		}

		private void _browser_JavascriptError(object sender, JavascriptErrorEventArgs e)
		{
			throw new NotImplementedException();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			_browser.AddMessageEventListener("getObject", GetObject);
			_browser.AddMessageEventListener("updateObject", UpdateObject);
			_browser.AddMessageEventListener("updateCollection", UpdateCollection);
			_browser.AddMessageEventListener("executeCommand", ExecuteCommand);
			_browser.AddMessageEventListener("setContext", SetContext);

			LoadCurrentView();
			_browser.ResetCursor();
			_loaded = true;
		}

		private void LoadCurrentView()
		{
			string path = _currentView;
//			if (!Path.IsPathRooted(path)) now requires the client to give us the actual path
//				path = Path.Combine(Directory.GetCurrentDirectory(), path);
			var uri = new Uri(path);
			_browser.Navigate(uri.AbsoluteUri);
		}

		private void UpdateObject(string data)
		{
			if (_propertyMonitor.Busy)
				return;

			JObject jobj = JObject.Parse(data);
			var id = (string) jobj["id"];
			object obj;
			if (TryGetObject(id, out obj))
			{
				using (_propertyMonitor.Enter())
				{
					var props = (JArray) jobj["props"];
					foreach (JObject prop in props)
					{
						PropertyInfo pi = obj.GetType().GetProperty((string) prop["name"]);
						object value = null;
						var jval = prop["value"];
						if (jval != null)
						{
							var objVal = jval as JObject;
							JToken idToken;
							if (objVal != null && objVal.TryGetValue("id", out idToken))
							{
								var valID = (string) idToken;
								if (!TryGetObject(valID, out value))
									value = null;
							}
							else
							{
								value = prop["value"].ToObject(pi.PropertyType);
							}
						}
						pi.SetValue(obj, value, null);
					}
				}
			}
		}

		private void UpdateCollection(string data)
		{
			if (_collectionMonitor.Busy)
				return;

			JObject jobj = JObject.Parse(data);
			var id = (string) jobj["id"];
			object obj;
			if (TryGetObject(id, out obj))
			{
				using (_collectionMonitor.Enter())
				{
					var coll = obj as IList;
					if (coll != null)
					{
						coll.Clear();
						var items = (JArray) jobj["items"];
						foreach (JToken item in items)
						{
							object val;
							var itemObj = item as JObject;
							if (itemObj != null)
							{
								var valID = (string) itemObj["id"];
								if (!TryGetObject(valID, out val))
									val = null;
							}
							else
							{
								val = item.ToObject<object>();
							}

							if (val != null)
								coll.Add(val);
						}
					}
				}
			}
		}

		private void ExecuteCommand(string data)
		{
			JObject jobj = JObject.Parse(data);
			var cmdID = (string) jobj["cmdID"];
			ICommand cmd;
			if (TryGetObject(cmdID, out cmd))
			{
				var ctxtID = (string) jobj["ctxtID"];
				object ctxt;
				if (TryGetObject(ctxtID, out ctxt))
					cmd.Execute(ctxt);
			}
		}

		private void SetContext(string data)
		{
			if (_propertyMonitor.Busy)
				return;

			JObject jobj = JObject.Parse(data);
			var cmdID = (string) jobj["cmdID"];
			ICommand cmd;
			if (TryGetObject(cmdID, out cmd))
			{
				var ctxtID = (string) jobj["ctxtID"];
				object ctxt;
				if (TryGetObject(ctxtID, out ctxt))
				{
					ObjectData od;
					if (_objects.TryGetValue(cmd, out od))
					{
						if (od.Contexts == null)
							od.Contexts = new List<WeakReference>();
						bool found = false;
						for (int i = od.Contexts.Count - 1; i >= 0; i--)
						{
							object o = od.Contexts[i].Target;
							if (o != null)
							{
								if (o == ctxt)
								{
									found = true;
									break;
								}
							}
							else
							{
								od.Contexts.RemoveAt(i);
							}
						}
						if (!found)
							od.Contexts.Add(new WeakReference(ctxt));
						UpdateCanExecute(cmdID, cmd, ctxtID, ctxt);
					}
				}
			}
		}

		private bool TryGetObject<T>(string id, out T obj) where T : class
		{
			WeakReference objRef = _idsToObjects[id];
			object o = objRef.Target;
			if (o != null)
			{
				obj = o as T;
				return obj != null;
			}

			obj = null;
			RemoveObject(id);
			return false;
		}

		private void CleanupInactiveObjects()
		{
			var toRemove = new List<string>();
			foreach (KeyValuePair<string, WeakReference> kvp in _idsToObjects)
			{
				object obj = kvp.Value.Target;
				if (obj != null)
				{
					ObjectData od;
					if (_objects.TryGetValue(obj, out od) && od.Contexts != null)
					{
						for (int i = od.Contexts.Count - 1; i >= 0; i--)
						{
							if (!od.Contexts[i].IsAlive)
								od.Contexts.RemoveAt(i);
						}
						if (od.Contexts.Count == 0)
							od.Contexts = null;

					}
				}
				else
				{
					toRemove.Add(kvp.Key);
				}
			}

			foreach (string key in toRemove)
				RemoveObject(key);
		}

		private void RemoveObject(string id)
		{
			_idsToObjects.Remove(id);
			ExecuteScript(string.Format("ko.net._removeObject('{0}')", id));
		}

		private void GetObject(string vmName)
		{
			object vm = _getObject(vmName);

			JObject jobj = LoadObject(vm);
			ExecuteScript(string.Format("ko.net._loadedID = {0}", (string) jobj["id"]));
		}

		private JObject LoadObject(object obj)
		{
			ObjectData od;
			if (!_objects.TryGetValue(obj, out od))
			{
				string id = _curID.ToString(CultureInfo.InvariantCulture);
				_curID++;

				od = new ObjectData(id);
				_objects.Add(obj, od);
				_idsToObjects[id] = new WeakReference(obj);

				JObject jobj;
				var enumerable = obj as IEnumerable;
				if (enumerable != null)
				{
					var collChangedObj = obj as INotifyCollectionChanged;
					if (collChangedObj != null)
						collChangedObj.CollectionChanged += CollectionChanged;
					jobj = new JObject(
						new JProperty("type", "coll"),
						new JProperty("id", id),
						new JProperty("observe", collChangedObj != null),
						new JProperty("items", new JArray(enumerable.Cast<object>().Select(GetJsonValue))));
				}
				else
				{
					var cmd = obj as ICommand;
					if (cmd != null)
					{
						cmd.CanExecuteChanged += CanExecuteChanged;
						jobj = new JObject(
							new JProperty("type", "cmd"),
							new JProperty("id", id));
					}
					else
					{
						var propChangedObj = obj as INotifyPropertyChanged;
						if (propChangedObj != null)
							propChangedObj.PropertyChanged += PropertyChanged;

						jobj = new JObject(
							new JProperty("type", "obj"),
							new JProperty("id", id));

						var props = new JArray();
						foreach (PropertyInfo prop in obj.GetType().GetProperties().Where(p => p.PropertyType.IsPublic && p.GetIndexParameters().Length == 0))
						{
							if (prop.PropertyType.IsEnum && !_enumerations.Contains(prop.PropertyType))
							{
								var enumObj = new JObject();
								foreach (object enumValue in Enum.GetValues(prop.PropertyType))
									enumObj.Add(new JProperty(Enum.GetName(prop.PropertyType, enumValue), enumValue));
								ExecuteScript(string.Format("ko.net._addEnumeration('{0}', '{1}')", prop.PropertyType.Name, enumObj.ToString(Formatting.None)));
								_enumerations.Add(prop.PropertyType);
							}

							var propObj = new JObject {new JProperty("name", prop.Name)};
							JToken value = GetJsonValue(prop.GetValue(obj, null));
							if (value != null)
								propObj.Add(new JProperty("value", value));
							propObj.Add(new JProperty("observe", propChangedObj != null && prop.CanWrite));
							props.Add(propObj);
						}
						jobj.Add(new JProperty("props", props));
					}
				}

				ExecuteScript(string.Format("ko.net._addObject('{0}')", jobj.ToString(Formatting.None)));
			}

			return new JObject(new JProperty("id", od.ID));
		}

		private JToken GetJsonValue(object val)
		{
			if (val == null)
				return null;

			if (IsNumericType(val) || val is bool || val is string)
				return new JValue(val);

			return LoadObject(val);
		}

		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_collectionMonitor.Busy)
				return;

			ObjectData od;
			if (_objects.TryGetValue(sender, out od))
			{
				using (_collectionMonitor.Enter())
				{
					var enumerable = (IEnumerable) sender;
					var jobj = new JObject(
						new JProperty("id", od.ID),
						new JProperty("items", new JArray(enumerable.Cast<object>().Select(GetJsonValue))));
					ExecuteScript(string.Format("ko.net._updateCollection('{0}')", jobj.ToString(Formatting.None)));
				}
			}
		}

		private void PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (_propertyMonitor.Busy)
				return;

			ObjectData od;
			if (_objects.TryGetValue(sender, out od))
			{
				using (_propertyMonitor.Enter())
				{
					PropertyInfo prop = sender.GetType().GetProperty(e.PropertyName);
					object val = prop.GetValue(sender, null);

					var jobj = new JObject(
						new JProperty("id", od.ID),
						new JProperty("props", new JArray(
							new JObject(
								new JProperty("name", prop.Name),
								new JProperty("value", GetJsonValue(val))))));
					ExecuteScript(string.Format("ko.net._updateObject('{0}')", jobj.ToString(Formatting.None)));
				}
			}
		}

		private void CanExecuteChanged(object sender, EventArgs e)
		{
			if (_propertyMonitor.Busy)
				return;

			ObjectData od;
			if (_objects.TryGetValue(sender, out od) && od.Contexts != null)
			{
				var cmd = (ICommand) sender;
				for (int i = od.Contexts.Count - 1; i >= 0; i--)
				{
					var ctxt = od.Contexts[i].Target;
					if (ctxt != null)
					{
						ObjectData ctxtData;
						if (_objects.TryGetValue(ctxt, out ctxtData))
							UpdateCanExecute(od.ID, cmd, ctxtData.ID, ctxt);
					}
					else
					{
						od.Contexts.RemoveAt(i);
					}
				}
			}
		}

		private void UpdateCanExecute(string cmdID, ICommand cmd, string ctxtID, object ctxt)
		{
			using (_propertyMonitor.Enter())
			{
				var jobj = new JObject(
					new JProperty("id", ctxtID),
					new JProperty("props", new JArray(
						new JObject(
							new JProperty("name", string.Format("CanExecute{0}", cmdID)),
							new JProperty("value", cmd.CanExecute(ctxt))))));
				ExecuteScript(string.Format("ko.net._updateObject('{0}')", jobj.ToString(Formatting.None)));
			}
		}

		private void ExecuteScript(string script)
		{
			using (var context = new AutoJSContext(_browser.JSContext))
			{
				string result;
				context.EvaluateScript(script, out result);
			}
		}

		private bool IsNumericType(object obj)
		{
			switch (Type.GetTypeCode(obj.GetType()))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
				default:
					return false;
			}
		}

		private class ObjectData
		{
			private readonly string _id;

			public ObjectData(string id)
			{
				_id = id;
			}

			public string ID
			{
				get { return _id; }
			}

			public IList<WeakReference> Contexts { get; set; }
		}
	}
}
