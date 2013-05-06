define(["require", "knockout"], function (require, ko) {
	var objects = new Array();
	
	ko.bindingHandlers.command = {
		init: function (element, valueAccessor, allBindingsAccessor, viewModel) {
			var cmd = ko.utils.unwrapObservable(valueAccessor());
			viewModel['CanExecute' + cmd._id] = ko.observable(true);
			var parameter = allBindingsAccessor()["commandParameter"];
			if (ko.isObservable(parameter)) {
				parameter.subscribe(function(newValue) {
					var data = {cmdID: cmd._id, ctxtID: viewModel._id, param: newValue};
					fireEvent('addParameter', JSON.stringify(data));
				});
			}
			var paramData = ko.utils.unwrapObservable(parameter);
			if (typeof paramData === "object")
				paramData = {id: paramData._id};
			var data = {cmdID: cmd._id, ctxtID: viewModel._id, param: paramData};
			fireEvent('addParameter', JSON.stringify(data));
			CheckEnabled(ko.utils.unwrapObservable(viewModel['CanExecute' + cmd._id]), element);
			viewModel['CanExecute' + cmd._id].subscribe(function(newValue) {
				CheckEnabled(newValue, element);
			});
			
			var afterExecute = allBindingsAccessor()["afterExecute"];
            var newValueAccessor = function () {
                var result = {};
                result.click = function(v) {
					cmd.Execute(v);
					if (afterExecute !== undefined) {
						var unwrapped = ko.utils.unwrapObservable(afterExecute);
						unwrapped(v);
					}
				};
                return result;
            };
            return ko.bindingHandlers['event']['init'].call(this, element, newValueAccessor, allBindingsAccessor, viewModel);
		}
	};
	
	var ComponentTemplateSource = function(templateId, options) {
	    var self = this, origAfterRender;
	    self.templateId = templateId;
		self.loaded = false;
	    self.template = ko.observable();
	    self.data = {};
		self.options = options;
		self.component = null;
		if (self.options && self.options.afterRender) {
			origAfterRender = self.options.afterRender;
			self.options.afterRender = function() {
				if (self.loaded) {
					origAfterRender.apply(self.options, arguments);
					if (component.initializeView !== undefined)
						component.initializeView();
				}
			}
		}
	};
	
	ko.utils.extend(ComponentTemplateSource.prototype, {
	    data: function(key, value) {
	        if (arguments.length === 1) {
	            return this.data[key];
	        }
	        this.data[key] = value;
	    },
	    text: function(value) {
			if (!this.loaded)
				this.getTemplate();
		
	        if (arguments.length === 0)
	            return this.template();
			this.template(arguments[0]);
	    },
	    getTemplate: function() {
			var self = this;
			var path = self.options.name.replace(new RegExp("[.]", "g"), "/");
			require([path], function (component) {
				self.component = component;
				self.template(component.template);
				self.loaded = true;
			});
		}
	});
	
	var ComponentTemplateEngine = function() {
	    var engine = new ko.nativeTemplateEngine();
	    engine.templates = {};
	    engine.makeTemplateSource = function(template, bindingContext, options) {
	        // Named template
	        if (typeof template === "string") {
	            var elem = document.getElementById(template);
	            if (elem)
	                return new ko.templateSources.domElement(elem);
	            else {
	                if(!engine.templates[template]) {
	                    engine.templates[template] = new ComponentTemplateSource(template, options);
	                }
	                return engine.templates[template];
	            }
	        }
	        else if ((template.nodeType === 1) || (template.nodeType === 8)) {
	            // Anonymous template
	            return new ko.templateSources.anonymousTemplate(template);
	        }
	    };
	
	    engine.renderTemplate = function (template, bindingContext, options) {
	        var templateSource = engine.makeTemplateSource(template, bindingContext, options);
	        return engine.renderTemplateSource(templateSource, bindingContext, options);
	    };
	
	    return engine;
	};
	
	ko.setTemplateEngine(new ComponentTemplateEngine());
	
	function CheckEnabled(value, element) {
		if (value && element.disabled)
			element.removeAttribute("disabled");
		else if ((!value) && (!element.disabled))
			element.disabled = true;
	}
	
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
	
	function fireEvent(name, data) {
		event = window.document.createEvent('MessageEvent');
		var origin = window.location.protocol + '//' + window.location.host;
		event.initMessageEvent(name, false, false, data, origin, 1234, window, null);
		window.document.dispatchEvent(event);
	}
	
	var tokay = {
		_loadedID: -1,
		loadViewModel: function(viewModelName) {
			fireEvent('getObject', viewModelName);
			var vm = objects[this._loadedID];
			this._loadedID = -1;
			return vm;
		},
	
		_addObject: function(data) {
			var parsed = ko.utils.parseJson(data);
			var obj = {};
			obj._id = parsed.id;
			obj._type = parsed.type;
			
			if (parsed.type === 'coll') {
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
								data.items.push({id: newValue[i]._id});
							else
								data.items.push(newValue[i]);
						}
						fireEvent('updateCollection', JSON.stringify(data));
					}, obj);
				}
			} else if (parsed.type === 'cmd') {
				obj.Execute = function(ctxt) {
					var data = {cmdID: this._id, ctxtID: ctxt._id};
					fireEvent('executeCommand', JSON.stringify(data));
				}.bind(obj);
			} else {
				for (var i = 0; i < parsed.props.length; i++) {
					var prop = parsed.props[i];
					if (prop.value !== null && typeof prop.value === 'object') {
						var objVal = objects[prop.value.id];
						if (objVal._type === 'cmd') {
							obj[prop.name] = objVal;
						} else if (objVal._type === 'coll') {
							if (prop.observe) {
								obj[prop.name] = ko.observable(objVal._items);
								obj[prop.name].subscribe(createObjectSubscriber(prop.name), obj);
							}
							else {
								obj[prop.name] = objVal._items;
							}
						} else if (objVal._type === 'obj') {
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
		},
	
		_updateObject: function(data) {
			var parsed = ko.utils.parseJson(data);
			var obj = objects[parsed.id];
			for (var i = 0; i < parsed.props.length; i++) {
				var prop = parsed.props[i];
				if (prop.value !== null && typeof prop.value === 'object')
					obj[prop.name](objects[prop.value.id]);
				else
					obj[prop.name](prop.value);
			}
		},
	
		_updateCollection: function(data) {
			var parsed = ko.utils.parseJson(data);
			var coll = objects[parsed.id];
			coll._items.removeAll();
			for (var i = 0; i < parsed.items.length; i++) {
				if (typeof parsed.items[i] === 'object')
					coll._items.push(objects[parsed.items[i].id]);
				else
					coll._items.push(parsed.items[i]);
			}
		},
	
		_removeObject: function(id) {
			delete objects[id];
		},
	
		_addEnumeration: function(name, data) {
			window[name] = ko.utils.parseJson(data);
		},
	
		closeDialog: function(result) {
			fireEvent("closeDialog", result);
		}
	};
	
	window.tokay = tokay;
	return tokay;
});
