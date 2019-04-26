using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamData
{

    internal sealed class DaysRange
    {
        private DateTimeOffset _next;
        private object syncLock = new object();

        public DaysRange(DateTimeOffset firstDay)
        {
            _next = firstDay;
        }
        
        public DateTimeOffset Next()
        {
            DateTimeOffset _current;
            lock(syncLock)
            {
                _current = _next;
                _next = _next.AddDays(1);
            }
            return _current;
        }
    }
}
