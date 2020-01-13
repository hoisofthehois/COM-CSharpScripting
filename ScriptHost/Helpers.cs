using System;
using System.Collections.Generic;

namespace csharpscripting
{
	internal static class Helpers
	{

		internal static void Apply<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach (var item in collection)
				action(item);
		}

		internal static Object CreateInstance(this Type type) => Activator.CreateInstance(type);

		internal static bool Like(this String original, String compare)
		{
			return original.ToLowerInvariant() == compare.ToLowerInvariant();
		}

	}
}
