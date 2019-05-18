using System;
using System.Collections.Generic;
using System.Linq;

namespace Profile.Core.Attributes
{
    public static class IndexHelper
    {
        public static IEnumerable<string> GetAttributeValues<T, A>(this T item)
        {
            return item?.GetType()?.GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(A)))
                .Select(p => p.GetValue(item).ToString()) ?? Array.Empty<string>();
        }
    }
}
