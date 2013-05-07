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
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using Gecko;
using Gecko.DOM;
using Gecko.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tokay
{
	public class CloseDialogRequestedEventArgs : EventArgs
	{
		private readonly DialogResult _dialogResult;

		public CloseDialogRequestedEventArgs(DialogResult dialogResult)
		{
			_dialogResult = dialogResult;
		}

		public DialogResult DialogResult
		{
			get { return _dialogResult; }
		}
	}

	public class TokayControl : UserControl
	{
		public event EventHandler ViewLoaded;
		public event EventHandler<CloseDialogRequestedEventArgs> CloseDialogRequested; 

		private readonly ConditionalWeakTable<object, ObjectData> _objects;
		private readonly Dictionary<string, WeakReference> _idsToObjects; 
		private readonly GeckoWebBrowser _browser;
		private int _curID;
		private readonly SimpleMonitor _collectionMonitor;
		private readonly SimpleMonitor _propertyMonitor;
		private DateTime _lastCleanup;
		private readonly Func<string, object> _getObject;
		private string _currentViewHtmlPath;
		private bool _loaded;
		private readonly HashSet<Type> _enumerations;
		private readonly string _basePath;

		public TokayControl(string basePath, Func<string, object> getObject, string pathToStartupViewHtml)
		{
			_basePath = basePath;

			GeckoFxInitializer.SetUpXulRunner();
			GeckoPreferences.User["security.fileuri.strict_origin_policy"] = false;
			GeckoPreferences.User["dom.max_script_run_time"] = 0;
			GeckoPreferences.User["extensions.blocklist.enabled"] = false;
			GeckoPreferences.User["places.history.enabled"] = false;
			GeckoPreferences.User["network.http.use-cache"] = false;
			GeckoPreferences.User["gfx.font_rendering.graphite.enabled"] = true;
			//GeckoPreferences.User["network.cookie.cookieBehavior"] = 2;

            if (!File.Exists(pathToStartupViewHtml))
                throw new ApplicationException(pathToStartupViewHtml + " does not exist");
			_getObject = getObject;
			_currentViewHtmlPath = pathToStartupViewHtml;
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
			_browser.DomClick += _browser_DomClick;
#if DEBUG
			_browser.EnableJavascriptDebugger();
			_browser.JavascriptError += _browser_JavascriptError;
			_browser.ShowContextMenu += _browser_ShowContextMenu;
#endif
		}

#if DEBUG
		private void _browser_JavascriptError(object sender, JavascriptErrorEventArgs error)
		{
			if (error.Filename == "chrome://tinyjsd/content/tinyjsd-main.js")
				return;

			var msg = string.Format("There was a JScript error in {0} at line {1}: {2}",
										error.Filename, error.Line, error.Message);

			if (msg.Contains("$ is not defined"))
			{
				msg += Environment.NewLine + Environment.NewLine + "Make sure jquery is properly referenced";
			}
		
			MessageBox.Show(msg, "Javascript Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void _browser_ShowContextMenu(object sender, GeckoContextMenuEventArgs e)
		{
			if (e.ContextMenu.MenuItems.Count > 0)
				e.ContextMenu.MenuItems.Add("-");
			bool debuggerPresent = true;
			try
			{
				Xpcom.GetService<nsISupports>("@mozilla.org/network/protocol;1?name=tinyjsd");
			}
			catch (COMException)
			{
				debuggerPresent = false;
			}

			e.ContextMenu.MenuItems.Add(new MenuItem("Debug Javascript", Debug_Click) {Enabled = debuggerPresent});
			e.ContextMenu.MenuItems.Add(new MenuItem("Inspect DOM", Inspect_Click));
		}

		private void Debug_Click(object sender, EventArgs eventArgs)
		{
			var appShellService = new ComPtr<nsIAppShellService>(Xpcom.GetService<nsIAppShellService>(Contracts.AppShellService));
			appShellService.Instance.CreateHiddenWindow();
			nsIDOMWindow domWin = appShellService.Instance.GetHiddenDOMWindowAttribute();
			domWin.Open(new nsAString("chrome://tinyJsd/content/tinyjsd-main.xul"), new nsAString("tinyJsd:mainWindow"), new nsAString("chrome,resizable"));
		}

		private void Inspect_Click(object sender, EventArgs eventArgs)
		{
			if (_browser.Document.GetElementById("FirebugLite") != null)
			{
			    ExecuteScript("Firebug.chrome.toggle(true, true)");
			}
			else
			{
			    var firebugScript = (GeckoScriptElement) _browser.Document.CreateHtmlElement("script");
			    firebugScript.Id = "FirebugLite";
			    firebugScript.SetAttribute("FirebugLite", "4");
			    firebugScript.Type = "text/javascript";
			    string fireBugPath = Path.Combine(GeckoFxInitializer.DirectoryOfApplicationOrSolution, "lib", "firebug-lite", "build", "firebug-lite.js");
			    firebugScript.Src = new Uri(fireBugPath).ToString();
				firebugScript.Text = "{startInNewWindow: true, showIconWhenHidden: false}";
			    _browser.Document.Head.AppendChild(firebugScript);
			}
		}
#endif

		private void _browser_DOMContentLoaded(object sender, DomEventArgs e)
		{
			_browser.DOMContentLoaded -= _browser_DOMContentLoaded;
			ExecuteScript(string.Format(@"
				var require = {{
					baseUrl: '{0}',
					paths: {{knockout: '{1}', tokay: '{2}', css: '{3}', normalize: '{4}', text: '{5}'}}
				}}", new Uri(_basePath), new Uri(Path.Combine(GeckoFxInitializer.DirectoryOfApplicationOrSolution, "lib", "knockout-2.2.1")),
				new Uri(Path.Combine(GeckoFxInitializer.DirectoryOfApplicationOrSolution, "TokayLib", "tokay")),
				new Uri(Path.Combine(GeckoFxInitializer.DirectoryOfApplicationOrSolution, "lib", "css")),
				new Uri(Path.Combine(GeckoFxInitializer.DirectoryOfApplicationOrSolution, "lib", "normalize")),
				new Uri(Path.Combine(GeckoFxInitializer.DirectoryOfApplicationOrSolution, "lib", "text"))));

			var requireScript = (GeckoScriptElement) _browser.Document.CreateHtmlElement("script");
			requireScript.Type = "text/javascript";
			string requirePath = Path.Combine(GeckoFxInitializer.DirectoryOfApplicationOrSolution, "lib", "require.js");
			requireScript.Src = new Uri(requirePath).ToString();
			//requireScript.SetAttribute("onload", "initializeView()");
			_browser.Document.Head.AppendChild(requireScript);
		}

		private void _browser_DomClick(object sender, DomEventArgs e)
		{
			_browser.WebBrowserFocus.Activate();
		}

		private void _browser_DocumentCompleted(object sender, EventArgs e)
		{
			_browser.DocumentCompleted -= _browser_DocumentCompleted;
			if (ViewLoaded != null)
				ViewLoaded(this, new EventArgs());
		}

	    public string CurrentViewHtmlPath
		{
			get { return _currentViewHtmlPath; }
			set
			{
				_currentViewHtmlPath = value;
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

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			_browser.AddMessageEventListener("getObject", GetObject);
			_browser.AddMessageEventListener("updateObject", UpdateObject);
			_browser.AddMessageEventListener("updateCollection", UpdateCollection);
			_browser.AddMessageEventListener("executeCommand", ExecuteCommand);
			_browser.AddMessageEventListener("addParameter", AddParameter);
			_browser.AddMessageEventListener("closeDialog", CloseDialog);

			LoadCurrentView();
			_browser.ResetCursor();

			_loaded = true;
		}

		private void CloseDialog(string result)
		{
			var dialogResult = DialogResult.None;
			switch (result.ToLowerInvariant())
			{
				case "ok":
					dialogResult = DialogResult.OK;
					break;

				case "cancel":
					dialogResult = DialogResult.Cancel;
					break;
			}
			if (CloseDialogRequested != null)
				CloseDialogRequested(this, new CloseDialogRequestedEventArgs(dialogResult));
		}

		private void LoadCurrentView()
		{
			_browser.DOMContentLoaded += _browser_DOMContentLoaded;
			_browser.DocumentCompleted += _browser_DocumentCompleted;
			var uri = new Uri(_currentViewHtmlPath);
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
				{
					ObjectData ctxtData;
					if (_objects.TryGetValue(ctxt, out ctxtData))
						cmd.Execute(GetCommandContextParameter(ctxtData, cmd));
				}
			}
		}

		private void AddParameter(string data)
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
					ObjectData cmdData;
					if (_objects.TryGetValue(cmd, out cmdData))
					{
						if (cmdData.CommandContexts == null)
							cmdData.CommandContexts = new List<WeakReference>();
						bool found = false;
						for (int i = cmdData.CommandContexts.Count - 1; i >= 0; i--)
						{
							object o = cmdData.CommandContexts[i].Target;
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
								cmdData.CommandContexts.RemoveAt(i);
							}
						}
						if (!found)
							cmdData.CommandContexts.Add(new WeakReference(ctxt));

						ObjectData ctxtData;
						if (_objects.TryGetValue(ctxt, out ctxtData))
						{
							if (ctxtData.CommandContextParameters == null)
								ctxtData.CommandContextParameters = new List<Tuple<WeakReference, object>>();
							JToken paramData = jobj["param"];
							if (paramData is JObject)
							{
								object param;
								if (TryGetObject((string) paramData["id"], out param))
								{
									ctxtData.CommandContextParameters.Add(Tuple.Create(new WeakReference(cmd), (object) new WeakReference(param)));
									UpdateCanExecute(cmdID, cmd, ctxtID, param);
								}
							}
							else
							{
								var param = paramData == null ? null : paramData.ToObject<object>();
								ctxtData.CommandContextParameters.Add(Tuple.Create(new WeakReference(cmd), param));
								UpdateCanExecute(cmdID, cmd, ctxtID, param);
							}
						}
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
					if (_objects.TryGetValue(obj, out od))
					{
						if (od.CommandContexts != null)
						{
							for (int i = od.CommandContexts.Count - 1; i >= 0; i--)
							{
								if (!od.CommandContexts[i].IsAlive)
									od.CommandContexts.RemoveAt(i);
							}
							if (od.CommandContexts.Count == 0)
								od.CommandContexts = null;
						}
						if (od.CommandContextParameters != null)
						{
							for (int i = od.CommandContextParameters.Count - 1; i >= 0; i--)
							{
								var weakRefParam = od.CommandContextParameters[i].Item2 as WeakReference;
								if (!od.CommandContextParameters[i].Item1.IsAlive || (weakRefParam != null && !weakRefParam.IsAlive))
									od.CommandContextParameters.RemoveAt(i);
							}
							if (od.CommandContextParameters.Count == 0)
								od.CommandContextParameters = null;
						}
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
			ExecuteScript(string.Format("tokay._removeObject('{0}')", id));
		}

		private void GetObject(string vmName)
		{
			object vm = _getObject(vmName);

			JObject jobj = LoadObject(vm);
			ExecuteScript(string.Format("tokay._loadedID = {0}", (string) jobj["id"]));
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
								ExecuteScript(string.Format("tokay._addEnumeration('{0}', '{1}')", prop.PropertyType.Name, enumObj.ToString(Formatting.None)));
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

				ExecuteScript(string.Format("tokay._addObject('{0}')", jobj.ToString(Formatting.None)));
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
					ExecuteScript(string.Format("tokay._updateCollection('{0}')", jobj.ToString(Formatting.None)));
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
					ExecuteScript(string.Format("tokay._updateObject('{0}')", jobj.ToString(Formatting.None)));
				}
			}
		}

		private void CanExecuteChanged(object sender, EventArgs e)
		{
			if (_propertyMonitor.Busy)
				return;

			ObjectData od;
			if (_objects.TryGetValue(sender, out od) && od.CommandContexts != null)
			{
				var cmd = (ICommand) sender;
				for (int i = od.CommandContexts.Count - 1; i >= 0; i--)
				{
					var ctxt = od.CommandContexts[i].Target;
					if (ctxt != null)
					{
						ObjectData ctxtData;
						if (_objects.TryGetValue(ctxt, out ctxtData))
							UpdateCanExecute(od.ID, cmd, ctxtData.ID, GetCommandContextParameter(ctxtData, cmd));
					}
					else
					{
						od.CommandContexts.RemoveAt(i);
					}
				}
			}
		}

		private object GetCommandContextParameter(ObjectData ctxtData, ICommand cmd)
		{
			object param = null;
			for (int j = ctxtData.CommandContextParameters.Count - 1; j >= 0; j--)
			{
				object o = ctxtData.CommandContextParameters[j].Item1.Target;
				if (o != null)
				{
					if (o == cmd)
					{
						param = ctxtData.CommandContextParameters[j].Item2;
						var weakRefParam = param as WeakReference;
						if (weakRefParam != null)
						{
							param = weakRefParam.Target;
							if (param == null)
								ctxtData.CommandContextParameters.RemoveAt(j);
						}
						break;
					}
				}
				else
				{
					ctxtData.CommandContextParameters.RemoveAt(j);
				}
			}
			return param;
		}

		private void UpdateCanExecute(string cmdID, ICommand cmd, string ctxtID, object param)
		{
			using (_propertyMonitor.Enter())
			{
				var jobj = new JObject(
					new JProperty("id", ctxtID),
					new JProperty("props", new JArray(
						new JObject(
							new JProperty("name", string.Format("CanExecute{0}", cmdID)),
							new JProperty("value", cmd.CanExecute(param))))));
				ExecuteScript(string.Format("tokay._updateObject('{0}')", jobj.ToString(Formatting.None)));
			}
		}

		private string ExecuteScript(string script)
		{
			using (var context = new AutoJSContext(_browser.JSContext))
			{
				string result;
				context.EvaluateScript(script, out result);
				return result;
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

			public IList<WeakReference> CommandContexts { get; set; }

			public IList<Tuple<WeakReference, object>> CommandContextParameters { get; set; } 
		}
	}
}
