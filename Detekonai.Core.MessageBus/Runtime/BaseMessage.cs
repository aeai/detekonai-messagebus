using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Detekonai.Core
{
	public abstract class BaseMessage
	{
		// this kept as field to maximize performance
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "It's an internal field")]
		internal static readonly Dictionary<Type, int> Keys;
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "It's a super internal field")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "The filed only can be used by the MessageBus and nothing else, no need to use field here")]
		internal readonly int Id;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Without the static constructor the initialization time of the Keys would be implementation dependant")]
		static BaseMessage()
		{
			Keys = new Dictionary<Type, int>();
			var types = AppDomain.CurrentDomain.GetAssemblies().Where(x => !IsOmittable(x)).SelectMany(s => s.GetTypes()).Where(p => p.IsSubclassOf(typeof(BaseMessage)));
			int id = 0;
			foreach(Type t in types)
			{
				Keys[t] = id;
				id++;
			}
		}

		private static bool IsOmittable(Assembly assembly)
		{
			string assemblyName = assembly.GetName().Name;
			return StartsWith("System") ||
				StartsWith("Microsoft") ||
				StartsWith("Windows") ||
				StartsWith("Unity") ||
				StartsWith("netstandard");

			bool StartsWith(string value) => assemblyName.StartsWith(value, ignoreCase: false, culture: CultureInfo.CurrentCulture);
		}

		public BaseMessage()
		{
			if(!Keys.TryGetValue(GetType(), out Id))
			{
				throw new InvalidOperationException("Tried to create an unregistered event object!!!");
			}
		}

	}
}
