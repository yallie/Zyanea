/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * 
 *  MessageWire - https://github.com/tylerjensen/MessageWire
 *  
 * The MIT License (MIT)
 * Copyright (C) 2016-2017 Tyler Jensen
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
 * documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
 * TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
 * CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 * DEALINGS IN THE SOFTWARE.
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MessageWire.Logging
{
    internal class StatsBag
    {
        private bool _useUtcTime = false;

        public StatsBag(bool useUtcTime)
        {
            _useUtcTime = useUtcTime;
            _started = _useUtcTime ? DateTime.UtcNow : DateTime.Now;
        }

        private DateTime _started = DateTime.Now;
        private int _count = 0;
        private ConcurrentDictionary<string, ConcurrentDictionary<string, StatInfo>> _bag
            = new ConcurrentDictionary<string, ConcurrentDictionary<string, StatInfo>>();

        public int Count { get { return _count; } }

        public void Clear()
        {
            _bag.Clear();
        }

        protected const string TimeStampPattern = "yyyy-MM-ddTHH:mm:ss.fff";
        protected string GetTimeStamp()
        {
            return _useUtcTime
                ? DateTime.UtcNow.ToString(TimeStampPattern)
                : DateTime.Now.ToString(TimeStampPattern);
        }

        protected string GetTimeStamp(DateTime dt)
        {
            return _useUtcTime
                ? dt.ToUniversalTime().ToString(TimeStampPattern)
                : dt.ToString(TimeStampPattern);
        }

        public void Add(string category, string name, float value)
        {
            var val = value;
            var container = _bag.GetOrAdd(category, new ConcurrentDictionary<string, StatInfo>());
            container.AddOrUpdate(name, s => new StatInfo(val), (s, t) =>
            {
                t.Increment(val);
                return t;
            });
            Interlocked.Increment(ref _count);
        }

        public string[] GetDump()
        {
            var dumped = _useUtcTime ? DateTime.UtcNow : DateTime.Now;
            TimeSpan ts = dumped - _started;
            var tsTxt = ts.TotalSeconds.ToString("######.000000");
            var lines = new List<string>();
            //add header
            lines.Add(string.Format("<entry fr=\"{0}\" to=\"{1}\" cnt=\"{2}\" secs=\"{3}\">", GetTimeStamp(_started), GetTimeStamp(dumped), _count, tsTxt));

            foreach (var cat in _bag)
            {
                var catCount = (from n in cat.Value select n.Value.Count).Sum();
                lines.Add(string.Format("  <cat nm=\"{0}\" cnt=\"{1}\">", cat.Key, catCount));

                foreach (var stat in cat.Value)
                {
                    if (null == stat.Value) continue;
                    float total = stat.Value.Total;
                    float avg = total == 0.0f 
                        ? 0.0f
                        : total / stat.Value.Count;
                    lines.Add(string.Format("    <stat nm=\"{0}\" cnt=\"{1}\" tot=\"{2}\" avg=\"{3}\" />",
                        stat.Key, stat.Value.Count, total, avg));
                }
                lines.Add("  </cat>");
            }
            lines.Add("</entry>");
            return lines.ToArray();
        }
    }

    internal class StatInfo
    {
        private ConcurrentBag<float> _bag = new ConcurrentBag<float>(); 

        public int Count 
        {
            get { return _bag.Count; }
        }

        public float Total
        {
            get
            {
                return _bag.Sum();
            }
        }

        public StatInfo(float value)
        {
            _bag.Add(value);
        }

        public void Increment(float value)
        {
            _bag.Add(value);
        }
    }
}