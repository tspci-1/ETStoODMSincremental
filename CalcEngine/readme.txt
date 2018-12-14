/*
 
Copyright© 2018 Project Consultants, LLC
 
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.”
 
*/

////////////////////////////////////////////////////////////////
//
// CalcEngine
// A Calculation Engine for .NET
//
////////////////////////////////////////////////////////////////


----------------------------------------------------------------
Version 1.0.0.2		Aug 2011

- TEXT function now uses CurrentCulture instead of InvariantCulture
	note: InvariantCulture does a weird job with currency formats!

----------------------------------------------------------------
Version 1.0.0.1		Aug 2011

- Fixed BindingExpression to update the DataContext when the 
  expression is fetched from the cache.

- Honor CultureInfo.TextInfo.ListSeparator
	so in US systems you would write "Sum(123.4, 567.8)"
	and in DE systems you would write "Sum(123,4; 567,8)"

- Changed default CultureInfo to InvariantCulture
	to make the component deterministic by default

- Improved Expression comparison logic
	both expressions should be of the same type

----------------------------------------------------------------
Version 1.0.0.0		Aug 2011

- First release on CodeProject:
	http://www.codeproject.com/KB/recipes/CalcEngine.aspx