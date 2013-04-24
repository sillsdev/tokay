(function (factory) {
	// Module systems magic dance.

	if (typeof require === "function" && typeof exports === "object" && typeof module === "object") {
		// CommonJS or Node: hard-coded dependency on "knockout"
		factory(require("knockout"), exports);
	} else if (typeof define === "function" && define["amd"]) {
		// AMD anonymous module with hard-coded dependency on "knockout"
		define(["knockout", "exports"], factory);
	} else {
		// <script> tag: use the global `ko` object, attaching a `net` property
		factory(ko, ko.net = {});
	}
}(function (ko, exports) {
	var objects = new Array();
	
	ko.bindingHandlers.command = {
		init: function (element, valueAccessor, allBindingsAccessor, viewModel) {
			var cmd = ko.utils.unwrapObservable(valueAccessor());
			viewModel['CanExecute' + cmd._id] = ko.observable(true);
			var data = {cmdID: cmd._id, ctxtID: viewModel._id};
			fireEvent('setContext', JSON.stringify(data));
			CheckEnabled(ko.utils.unwrapObservable(viewModel['CanExecute' + cmd._id]), element);
			viewModel['CanExecute' + cmd._id].subscribe(function(newValue) {
				CheckEnabled(newValue, element);
			});
			
            var newValueAccessor = function () {
                var result = {};
                result.click = cmd.Execute;
                return result;
            };
            return ko.bindingHandlers['event']['init'].call(this, element, newValueAccessor, allBindingsAccessor, viewModel);
		}
	};
	
	function CheckEnabled(value, element) {
		if (value && element.disabled)
			element.removeAttribute("disabled");
		else if ((!value) && (!element.disabled))
			element.disabled = true;
	}
	
	exports._loadedID = -1;
	exports.loadViewModel = function(viewModelName) {
		fireEvent('getObject', viewModelName);
		var vm = objects[exports._loadedID];
		exports._loadedID = -1;
		return vm;
	};
	
	exports._addObject = function(data) {
		var parsed = ko.utils.parseJson(data);
		var obj = {};
		obj._id = parsed.id;
		obj._type = parsed.type;
		
		if (parsed.type == 'coll') {
			obj._items = parsed.observe ? ko.observableArray() : new Array();
			for (var i = 0; i < parsed.items.length; i++) {
				if (typeof parsed.items[i] === 'object')
					obj._items.push(objects[parsed.items[i].id]);
				else
					obj._items.push(parsed.items[i]);
			}
			if (parsed.observe) {
				obj._items.subscribe(function(newValue) {
					var data = {};
					data.id = this._id;
					data.items = new Array();
					for (var i = 0; i < newValue.length; i++) {
						if (typeof newValue[i] === 'object')
							data.items.push(objects[newValue[i].id]);
						else
							data.items.push(newValue[i]);
					}
					fireEvent('updateCollection', JSON.stringify(data));
				}, obj);
			}
		} else if (parsed.type == 'cmd') {
			obj.Execute = function(ctxt) {
				var data = {cmdID: this._id, ctxtID: ctxt._id};
				fireEvent('executeCommand', JSON.stringify(data));
			}.bind(obj);
		} else {
			for (var i = 0; i < parsed.props.length; i++) {
				var prop = parsed.props[i];
				if (prop.value != null && typeof prop.value === 'object') {
					var objVal = objects[prop.value.id];
					if (objVal._type == 'cmd') {
						obj[prop.name] = objVal;
					} else if (objVal._type == 'coll') {
						if (prop.observe) {
							obj[prop.name] = ko.observable(objVal._items);
							obj[prop.name].subscribe(createObjectSubscriber(prop.name), obj);
						}
						else {
							obj[prop.name] = objVal._items;
						}
					} else if (objVal._type == 'obj') {
						if (prop.observe) {
							obj[prop.name] = ko.observable(objVal);
							obj[prop.name].subscribe(createObjectSubscriber(prop.name), obj);
						}
						else {
							obj[prop.name] = objVal;
						}
					}	
				} else if (prop.observe) {
					obj[prop.name] = ko.observable(prop.value);
					obj[prop.name].subscribe(createValueSubscriber(prop.name), obj);
				} else {
					obj[prop.name] = prop.value;
				}
			}
		}
		
		objects[parsed.id] = obj;
	};
	
	function createObjectSubscriber(propName) {
		return function(newValue) {
			var data = {id: this._id, props: [{name: propName, value: {id: newValue._id}}]};
			fireEvent('updateObject', JSON.stringify(data));
		};
	}
	
	function createValueSubscriber(propName) {
		return function(newValue) {
			var data = {id: this._id, props: [{name: propName, value: newValue}]};
			fireEvent('updateObject', JSON.stringify(data));
		};
	}
	
	exports._updateObject = function(data) {
		var parsed = ko.utils.parseJson(data);
		var obj = objects[parsed.id];
		for (var i = 0; i < parsed.props.length; i++) {
			var prop = parsed.props[i];
			if (prop.value != null && typeof prop.value === 'object')
				obj[prop.name](objects[prop.value.id]);
			else
				obj[prop.name](prop.value);
		}
	};
	
	exports._updateCollection = function(data) {
		var parsed = ko.utils.parseJson(data);
		var coll = objects[parsed.id];
		coll._items.removeAll();
		for (var i = 0; i < parsed.items.length; i++) {
			if (typeof parsed.items[i] === 'object')
				coll._items.push(objects[parsed.items[i].id]);
			else
				coll._items.push(parsed.items[i]);
		}
	};
	
	exports._removeObject = function(id) {
		delete objects[id];
	};
	
	function fireEvent(name, data) {
		event = window.document.createEvent('MessageEvent');
		var origin = window.location.protocol + '//' + window.location.host;
		event.initMessageEvent(name, false, false, data, origin, 1234, window, null);
		window.document.dispatchEvent(event);
	}
	
	exports._addEnumeration = function(name, data) {
		window[name] = ko.utils.parseJson(data);
	}
	
	exports.setDialogResult = function(result) {
		fireEvent("setDialogResult", result);
	}
}));
