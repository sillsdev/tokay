using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tokay
{
	/// <summary>
	/// This class supports dialogs with Cancel buttons. It wraps a model that knows how to copy itself,
	/// maintaining the original until CopyBackToOriginal is called (typically when the user clicks OK, or
	/// possibly Apply).
	/// Review: we could remove the "knows how to copy itself" by using Reflection to copy all public
	/// properties with setters, possibly supporting some attribute to mark any that should NOT be copied.
	/// But it's safer if the model knows what should be copied (and how deeply).
	/// </summary>
	public class ModelSandboxer<T> where T : ICopyDataTo<T>,new()
	{
		public T OriginalModel { get; private set; }
		public T WorkingModel { get; private set; }

		public ModelSandboxer(T original)
		{
			OriginalModel = original;
			WorkingModel = new T();
			OriginalModel.CopyTo(WorkingModel);
		}

		public void CopyBackToOriginal()
		{
			WorkingModel.CopyTo(OriginalModel);
		}
	}

	public interface ICopyDataTo<in T>
	{
		void CopyTo(T destination);
	}
}
