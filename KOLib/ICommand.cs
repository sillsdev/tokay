using System;

namespace TokaySharp
{
	public interface ICommand
	{
		event EventHandler CanExecuteChanged;

		bool CanExecute(object parameter);
		void Execute(object parameter);
	}
}
