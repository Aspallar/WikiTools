using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardNames
{
    internal static class Extensions
    {
        public static bool ContainsIgnoreCase(this List<string> list, string str)
        {
            string upperStr = str.ToUpperInvariant();
            foreach (string listEntry in list)
            {
                if (listEntry.ToUpperInvariant() == upperStr)
                    return true;
            }
            return false;
        }

    }
}
