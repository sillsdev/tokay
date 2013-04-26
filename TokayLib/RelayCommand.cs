using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Input;

namespace Tokay
{
	public class RelayCommand : RelayCommandBase
	{
		private readonly Action _execute;
		private readonly Func<bool> _canExecute;

		public RelayCommand(Action execute)
			: this(execute, null)
		{
		}

		public RelayCommand(Action execute, Expression<Func<bool>> canExecute)
		{
			_execute = execute;
			if (canExecute != null)
			{
				_canExecute = canExecute.Compile();
				SubscribeToChanges(canExecute);
			}
		}

		public override bool CanExecute(object parameter)
		{
			if (_canExecute != null)
				return _canExecute();
			return true;
		}

		public override void Execute(object parameter)
		{
			_execute();
		}
	}

	public class RelayCommand<T> : RelayCommandBase
	{
		private readonly Action<T> _execute;
		private readonly Func<T, bool> _canExecute;

		public RelayCommand(Action<T> execute)
			: this(execute, null)
		{
		}

		public RelayCommand(Action<T> execute, Expression<Func<T, bool>> canExecute)
		{
			_execute = execute;
			if (canExecute != null)
			{
				_canExecute = canExecute.Compile();
				SubscribeToChanges(canExecute);
			}
		}

		public override bool CanExecute(object parameter)
		{
			if (_canExecute != null)
			{
				T typedParameter;
				return TryConvert(parameter, out typedParameter) && _canExecute(typedParameter);
			}
			return true;
		}

		public override void Execute(object parameter)
		{
            T typedParameter;
            if (TryConvert(parameter, out typedParameter))
                _execute(typedParameter);
		}

		private static bool TryConvert(object parameter, out T typedParameter)
		{
			if (parameter is T || typeof(T).IsClass && parameter == null)
			{
				typedParameter = (T)parameter;
				return true;
			}

			if (typeof (IConvertible).IsAssignableFrom(typeof (T)) && parameter is IConvertible)
			{
				try
				{
					typedParameter = (T) Convert.ChangeType(parameter, typeof (T));
					return true;
				}
				catch
				{
				}
			}

			typedParameter = default(T);
			return false;
		}
	}

	public abstract class RelayCommandBase : ICommand
	{
		private readonly WeakEvent<EventHandler> _canExecuteChanged = new WeakEvent<EventHandler>();
        private readonly List<IWatcher> _watchers = new List<IWatcher>();

        public event EventHandler CanExecuteChanged
        {
            add { _canExecuteChanged.Add(value); }
            remove { _canExecuteChanged.Remove(value); }
        }

		public abstract bool CanExecute(object parameter);
		public abstract void Execute(object parameter);

		public void RaiseCanExecuteChanged()
		{
			_canExecuteChanged.Invoke(h => h(this, EventArgs.Empty));
		}

        /// <summary>
        /// Analyzes an expression for its notifying componetns and subscribes to their changes.
        /// </summary>
        /// <param name="expression">The expression to analyze.</param>
        protected void SubscribeToChanges(Expression expression)
        {
            var visitor = new NotifierFindingExpressionVisitor();
            visitor.Visit(expression);

            foreach (var notifier in visitor.NotifyingMembers)
                _watchers.Add(new NotifyingMemberWatcher(notifier));

            foreach (var notifier in visitor.NotifyingCollections)
                _watchers.Add(new NotifyingCollectionWatcher(notifier));

            foreach (var notifier in visitor.BindingLists)
                _watchers.Add(new BindingListWatcher(notifier));

            foreach (var watcher in _watchers)
            {
                watcher.Changed += OnWatcherChanged;
                watcher.SubscribeToCurrentNotifier();
            }
        }

        private void OnWatcherChanged(object sender, EventArgs e)
        {
			RaiseCanExecuteChanged();

            foreach (var watcher in _watchers)
                watcher.SubscribeToCurrentNotifier();
        }

        /// <summary>
        /// Unsubscribes from all change notifications.
        /// </summary>
        public void Dispose()
        {
            foreach (IWatcher watcher in _watchers)
                watcher.Dispose();
        }

        #region Watchers

        private interface IWatcher : IDisposable
        {
            event EventHandler Changed;

            void SubscribeToCurrentNotifier();
        }

        private abstract class Watcher<T> : IWatcher where T : class
        {
            public event EventHandler Changed = delegate { };

            private readonly Func<T> _accessor;
            private T _current;

	        protected Watcher(Expression accessor)
            {
                if (accessor.NodeType == ExpressionType.Constant)
                {
                    // do this outside the closure so we do it only once.
                    var value = (T)((ConstantExpression)accessor).Value;
                    _accessor = () => value;
                }
                else
                    _accessor = GetAccessor(accessor);
            }

            public void SubscribeToCurrentNotifier()
            {
                if (_current != null)
                    Unsubscribe(_current);

                _current = _accessor();

                if (_current != null)
                    Subscribe(_current);
            }

            protected abstract void Subscribe(T notifier);

            protected abstract void Unsubscribe(T notifier);

            protected virtual void OnChanged()
            {
                Changed(this, EventArgs.Empty);
            }

            public virtual void Dispose()
            {
                if (_current != null)
                    Unsubscribe(_current);
            }

            private static Func<T> GetAccessor(Expression expression)
            {
                ConstantExpression root;
                var members = GetMemberChain(expression, out root);

                if (root == null) return null;

                if (root.Value == null) return () => null;

                var nullChecks = members
                    .Select((m, i) => members.Take(i).Aggregate((Expression)root, Expression.MakeMemberAccess))
                    .Where(a => a.Type.IsClass)
                    .Select(a => Expression.NotEqual(a, Expression.Constant(null, a.Type)))
                    .Aggregate(Expression.AndAlso);

                var returnLabel = Expression.Label(typeof(T));
                var lambda = Expression.Lambda<Func<T>>(
                    Expression.Block(
                        Expression.IfThen(nullChecks, Expression.Return(returnLabel, Expression.Convert(expression, typeof(T)))),
                        Expression.Label(returnLabel, Expression.Constant(null, typeof(T)))));

                return lambda.Compile();
            }

            private static Stack<MemberInfo> GetMemberChain(Expression expression, out ConstantExpression root)
            {
                var members = new Stack<MemberInfo>(16);

                var node = expression;

                root = null;

                while (node != null)
                    switch (node.NodeType)
                    {
                        case ExpressionType.MemberAccess:
                            var memberExpr = (MemberExpression)node;
                            members.Push(memberExpr.Member);
                            node = memberExpr.Expression;
                            break;

                        case ExpressionType.Constant:
                            root = (ConstantExpression)node;
                            node = null;
                            break;

                        default: throw new NotSupportedException(node.NodeType.ToString());
                    }

                return members;
            }
        }

        private class NotifyingMemberWatcher : Watcher<INotifyPropertyChanged>
        {
            private readonly string _memberName;

            public NotifyingMemberWatcher(MemberExpression notifyingMember)
                : base(notifyingMember.Expression)
            {
                _memberName = notifyingMember.Member.Name;
            }

            protected override void Subscribe(INotifyPropertyChanged notifier)
            {
                notifier.PropertyChanged += OnNotifierPropertyChanged;
            }

            protected override void Unsubscribe(INotifyPropertyChanged notifier)
            {
                notifier.PropertyChanged -= OnNotifierPropertyChanged;
            }

            private void OnNotifierPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == _memberName)
                    OnChanged();
            }
        }

        private class NotifyingCollectionWatcher : Watcher<INotifyCollectionChanged>
        {
            public NotifyingCollectionWatcher(Expression expression) : base(expression) { }

            protected override void Subscribe(INotifyCollectionChanged notifier)
            {
                notifier.CollectionChanged += OnNotifierCollectionChanged;
            }

            protected override void Unsubscribe(INotifyCollectionChanged notifier)
            {
                notifier.CollectionChanged -= OnNotifierCollectionChanged;
            }

            private void OnNotifierCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action != NotifyCollectionChangedAction.Move)
                    OnChanged();
            }
        }

        private class BindingListWatcher : Watcher<IBindingList>
        {
            public BindingListWatcher(Expression accessor) : base(accessor) { }

            protected override void Subscribe(IBindingList notifier)
            {
                notifier.ListChanged += OnNotifierListChanged;
            }

            protected override void Unsubscribe(IBindingList notifier)
            {
                notifier.ListChanged -= OnNotifierListChanged;
            }

            private void OnNotifierListChanged(object sender, ListChangedEventArgs e)
            {
                switch (e.ListChangedType)
                {
                    case ListChangedType.ItemAdded:
                    case ListChangedType.ItemDeleted:
                    case ListChangedType.Reset:
                        OnChanged();
                        break;
                }
            }
        }

        #endregion

        private class NotifierFindingExpressionVisitor : ExpressionVisitor
        {
            public readonly HashSet<MemberExpression> NotifyingMembers = new HashSet<MemberExpression>(PropertyChainEqualityComparer.Instance);

            public readonly HashSet<Expression> NotifyingCollections = new HashSet<Expression>(PropertyChainEqualityComparer.Instance);

            public readonly HashSet<Expression> BindingLists = new HashSet<Expression>(PropertyChainEqualityComparer.Instance);

            protected override Expression VisitMember(MemberExpression node)
            {
                if (IsPropertyChain(node.Expression))
                {
                    if (typeof(INotifyPropertyChanged).IsAssignableFrom(node.Expression.Type))
                        NotifyingMembers.Add(node);
                }

                return base.VisitMember(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                RegisterNotifyingCollection(node.Object);

                return base.VisitMethodCall(node);
            }

            protected override Expression VisitIndex(IndexExpression node)
            {
                RegisterNotifyingCollection(node.Object);

                return base.VisitIndex(node);
            }

            private void RegisterNotifyingCollection(Expression node)
            {
                if (IsPropertyChain(node))
                    // if node is both of these types, we only want one registration.
                    if (typeof(INotifyCollectionChanged).IsAssignableFrom(node.Type))
                        NotifyingCollections.Add(node);
                    else if (typeof(IBindingList).IsAssignableFrom(node.Type))
                        BindingLists.Add(node);
            }

            private static bool IsPropertyChain(Expression node)
            {
                while (true)
                {
                    switch (node.NodeType)
                    {
                        case ExpressionType.Constant:
                            return true;

                        case ExpressionType.MemberAccess:
                            var memberExpr = (MemberExpression)node;
                            node = memberExpr.Expression;
                            break;

                        default:
                            return false;
                    }
                }
            }

            private class PropertyChainEqualityComparer : IEqualityComparer<Expression>
            {
                public static readonly PropertyChainEqualityComparer Instance = new PropertyChainEqualityComparer();

                private PropertyChainEqualityComparer() { }

                public bool Equals(Expression x, Expression y)
                {
                    while (true)
                    {
                        if (x.NodeType != y.NodeType) return false;

                        switch (x.NodeType)
                        {
                            case ExpressionType.Constant:
                                var xConstantExpr = (ConstantExpression)x;
                                var yConstantExpr = (ConstantExpression)y;

                                return ReferenceEquals(xConstantExpr.Value, yConstantExpr.Value);

                            case ExpressionType.MemberAccess:
                                var xMemberExpr = (MemberExpression)x;
                                var yMemberExpr = (MemberExpression)y;

                                if (xMemberExpr.Member != yMemberExpr.Member)
                                    return false;

                                x = xMemberExpr.Expression;
                                y = yMemberExpr.Expression;
                                break;

                            default:
                                throw new InvalidOperationException(x.NodeType.ToString());
                        }
                    }
                }

                public int GetHashCode(Expression node)
                {
                    var hash = 17;

                    while (true)
                    {
                        switch (node.NodeType)
                        {
                            case ExpressionType.Constant:
                                var constantExpr = (ConstantExpression)node;

                                return hash * 31 + (constantExpr.Value != null ? constantExpr.Value.GetHashCode() : 0);

                            case ExpressionType.MemberAccess:
                                var memberExpr = (MemberExpression)node;

                                hash = hash * 31 + memberExpr.Member.GetHashCode();

                                node = memberExpr.Expression;
                                break;

                            default:
                                throw new InvalidOperationException(node.NodeType.ToString());
                        }
                    }
                }
            }
        }
	}
}
