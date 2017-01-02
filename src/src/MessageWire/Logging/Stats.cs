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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MessageWire.Logging
{
    public class Stats : LoggerBase, IStats
    {
        private int _statsBufferSize = 10000;
        private const string StatFilePrefixDefault = "stat-";
        private const string StatFileExtensionDefault = ".txt";

        public Stats(string statsDirectory = null,
            string statsFilePrefix = null,
            string statsFileExtension = null,
            int messageBufferSize = 32,
            int statsBufferSize = 10000,
            LogOptions options = LogOptions.LogOnlyToFile,
            LogRollOptions rollOptions = LogRollOptions.Daily,
            int rollMaxMegaBytes = 1024,
            bool useUtcTimeStamp = false)
        {
            if (options != LogOptions.LogOnlyToConsole && string.IsNullOrWhiteSpace(statsDirectory)) throw new ArgumentNullException(nameof(statsDirectory));
            if (options != LogOptions.LogOnlyToConsole)
            {
                _logDirectory = statsDirectory;
                Directory.CreateDirectory(_logDirectory); //will throw if unable - does not throw if already exists
                _logFilePrefix = statsFilePrefix ?? StatFilePrefixDefault;
                _logFileExtension = statsFileExtension ?? StatFileExtensionDefault;
            }
            _messageBufferSize = messageBufferSize;
            _statsBufferSize = statsBufferSize;
            _rollOptions = rollOptions;
            _rollMaxMegaBytes = rollMaxMegaBytes;
            _useUtcTimeStamp = useUtcTimeStamp;

            LogOptions = options;
            if (_messageBufferSize < 1) _messageBufferSize = 1;
            if (_messageBufferSize > 4096) _messageBufferSize = 4096;
            if (_statsBufferSize < 10) _statsBufferSize = 10;
            if (_statsBufferSize > 1000000) _statsBufferSize = 1000000;
            if (_rollOptions == LogRollOptions.Size)
            {
                if (_rollMaxMegaBytes < 1) _rollMaxMegaBytes = 1;
                if (_rollMaxMegaBytes < 4096) _rollMaxMegaBytes = 4096;
            }
        }

        private void WriteLines(string[] lines)
        {
            _logQueue.Enqueue(lines);
            if (_logQueue.Count >= _messageBufferSize)
                Task.Factory.StartNew(() => WriteBuffer(_messageBufferSize));
        }

        public override void FlushLog()
        {
            var items = new List<string[]>();
            foreach (var kvp in _bag)
            {
                items.Add(kvp.Value.GetDump());
            }
            foreach(var list in items) WriteLines(list);
            base.FlushLog();
        }

        //period, cat, nm, count, total
        private int _period = 0;
        private ConcurrentDictionary<int, StatsBag> _bag = new ConcurrentDictionary<int, StatsBag>();

        private void WriteBag(StatsBag bag)
        {
            int p = _period;
            Interlocked.Increment(ref _period);
            var lines = bag.GetDump();
            StatsBag b;
            if (_bag.TryRemove(p, out b)) b.Clear();
            WriteLines(lines);
        }

        public void Log(string name, float value)
        {
            int p = _period;
            var bag = _bag.GetOrAdd(p, new StatsBag(_useUtcTimeStamp));
            bag.Add("unspecified", name, value);
            if (bag.Count >= _statsBufferSize)
            {
                WriteBag(bag);
            }
        }

        public void Log(string category, string name, float value)
        {
            int p = _period;
            var bag = _bag.GetOrAdd(p, new StatsBag(_useUtcTimeStamp));
            bag.Add(category, name, value);
            if (bag.Count >= _statsBufferSize)
            {
                WriteBag(bag);
            }
        }
    }
}