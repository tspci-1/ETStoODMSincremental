/*
 
Copyright© 2018 Project Consultants, LLC
 
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.”
 
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace CalcEngine
{
    /// <summary>
    /// Caches expressions based on their string representation.
    /// This saves parsing time.
    /// </summary>
    /// <remarks>
    /// Uses weak references to avoid accumulating unused expressions.
    /// </remarks>
    class ExpressionCache
    {
        Dictionary<string, WeakReference> _dct;
        CalcEngine _ce;
        int _hitCount;

        public ExpressionCache(CalcEngine ce)
        {
            _ce = ce;
            _dct = new Dictionary<string, WeakReference>();
        }

        // gets the parsed version of a string expression
        public Expression this[string expression]
        {
            get
            {
                Expression x = null;
                WeakReference wr;

                // get expression from cache
                if (_dct.TryGetValue(expression, out wr))
                {
                    x = wr.Target as Expression;
                }

                // if failed, parse now and store
                if (x == null)
                {
                    // remove all dead references from dictionary
                    if (_dct.Count > 100 && _hitCount++ > 100)
                    {
                        RemoveDeadReferences();
                        _hitCount = 0;
                    }

                    // store this expression
                    x = _ce.Parse(expression);
                    _dct[expression] = new WeakReference(x);
                }

                // return the parsed expression
                return x;
            }
        }

        // remove all dead references from the cache
        void RemoveDeadReferences()
        {
            for (bool done = false; !done; )
            {
                done = true;
                foreach (var k in _dct.Keys)
                {
                    if (!_dct[k].IsAlive)
                    {
                        _dct.Remove(k);
                        done = false;
                        break;
                    }
                }
            }
        }
    }
}
