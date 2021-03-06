/*
  Copyright (c) 2013 Ant Micro <www.antmicro.com>

  Authors:
   * Konrad Kruczynski (kkruczynski@antmicro.com)

  Permission is hereby granted, free of charge, to any person obtaining
  a copy of this software and associated documentation files (the
  "Software"), to deal in the Software without restriction, including
  without limitation the rights to use, copy, modify, merge, publish,
  distribute, sublicense, and/or sell copies of the Software, and to
  permit persons to whom the Software is furnished to do so, subject to
  the following conditions:

  The above copyright notice and this permission notice shall be
  included in all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
  LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
  OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
  WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

namespace Antmicro.Migrant.VersionTolerance
{
    internal static class StampHelpers
    {
        public static bool IsStampNeeded(Type type, bool treatCollectionAsUserObject)
        {
            return !Helpers.IsWriteableByPrimitiveWriter(type) && (!new CollectionMetaToken(type).IsCollection || treatCollectionAsUserObject);
        }

        public static IEnumerable<FieldInfo> GetFieldsInSerializationOrder(Type type, bool withTransient = false)
        {
            return GetFieldsStructureInSerializationOrder(type, withTransient).SelectMany(x => x.Item2);
        }

        public static IEnumerable<Tuple<Type, IEnumerable<FieldInfo>>> GetFieldsStructureInSerializationOrder(Type type, bool withTransient = false)
        {
            return type.GetAllFieldsStructurized().Select(x => Tuple.Create(x.Item1, x.Item2.Where(y => (withTransient || Helpers.IsNotTransient(y))
                    && x.Item1.FullName != "System.RuntimeType" 
                    && x.Item1 != typeof(ReaderWriterLock) 
                    && !y.GetCustomAttributes(typeof(NonSerializedAttribute), true).Any() 
                    && !y.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Any())
                .OrderBy(y => y.Name).AsEnumerable()));
        }
    }
}