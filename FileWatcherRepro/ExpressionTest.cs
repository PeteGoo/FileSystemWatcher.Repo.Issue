using System;
using System.Collections.Generic;
using System.Linq;

namespace FileWatcherRepro
{
    public class ExpressionTest
    {
        public void Foo()
        {
            var result = from int x in "1,2,3,4,5,6"
                         where x > 3
                         select x;
        }
    }

    public static class IntExtensions
    {
        public static IEnumerable<int> Where(this string commaDelimitedInts, Func<int, bool> whereClause)
        {
            return commaDelimitedInts.Split(',').Select(s => s.Trim()).Select(int.Parse);
        }
    }
}