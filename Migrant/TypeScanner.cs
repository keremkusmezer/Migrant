using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Text;

namespace AntMicro.Migrant
{
	/// <summary>
	/// Gives unique, consecutive IDs to types by setting them in array.
	/// </summary>
    public class TypeScanner
    {
		/// <summary>
		/// Initializes a new instance of the <see cref="AntMicro.Migrant.TypeScanner"/> class.
		/// </summary>
        public TypeScanner()
        {
            types = new Type[0];
        }

		/// <summary>
		/// Scans the given type and gives ID to this type, its base type, type of
		/// its fields recursively. Also throws if a non-serializable type is
		/// encountered.
		/// </summary>
		/// <param name='typeToScan'>
		/// Type to scan.
		/// </param>
        public void Scan(Type typeToScan)
        {
            var typeSet = new HashSet<Type>();
            var typeStack = new Stack<Type>();
            ScanRecursiveWithStack(typeSet, typeToScan, typeStack);
            types = types.Union(typeSet).Distinct().ToArray();
        }

		/// <returns>
		/// Returns the array, containing all already known types. Every type appear in
		/// this array exactly once, so the index can be used as a unique ID of this type.
		/// </returns>
        public Type[] GetTypeArray()
        {
            return (Type[])types.Clone();
        }

        private static void ScanRecursiveWithStack(HashSet<Type> typeSet, Type typeToScan, Stack<Type> typeStack)
        {
            typeStack.Push(typeToScan);
            ScanRecursive(typeSet, typeToScan, typeStack);
            typeStack.Pop();
        }

        private static void ScanRecursive(HashSet<Type> typeSet, Type typeToScan, Stack<Type> typeStack)
        {
            if(typeToScan == null)
            {
                return;
            }
            if(typeof(ISpeciallySerializable).IsAssignableFrom(typeToScan) || typeToScan.IsDefined(typeof(TransientAttribute), false))
            {
                typeSet.Add(typeToScan);
            }
            BreakOnIllegalType(typeToScan, typeStack);
            foreach(var type in GetElementTypes(typeToScan))
            {
                ScanRecursiveWithStack(typeSet, type, typeStack);
            }
            if(!IsSerializable(typeToScan) || !typeSet.Add(typeToScan))
            {
                return;
            }
            if(!IsPrimitive(typeToScan) && !typeToScan.IsValueType)
            { //cannot add IsValueType to IsPrimitive, because it's used by ShouldFieldsBeScanned
                ScanRecursiveWithStack(typeSet, typeToScan.BaseType, typeStack);
            }
            if(!ShouldFieldsBeScanned(typeToScan))
            {
                return;
            }
            //TODO: unified check
            var fields = typeToScan.GetAllFields(false).Where(x => !x.Attributes.HasFlag(FieldAttributes.Literal) && !x.IsTransient());
            var typesToAdd = fields.Select(x => x.FieldType).Where(x => !x.IsInterface)
                .Distinct();
            foreach(var type in typesToAdd)
            {
                ScanRecursiveWithStack(typeSet, type, typeStack);
            }
        }

        private static void BreakOnIllegalType(Type typeToScan, IEnumerable<Type> stack)
        {
            if (!IllegalTypes.Contains(typeToScan) && !typeToScan.IsPointer)
            {
                return;
            }
            var revStack = stack.Reverse();
            var path = new StringBuilder(revStack.First().Name);
            foreach (var elem in revStack.Skip(1))
            {
                path.Append(" -> ").Append(elem.Name);
            }
            throw new ArgumentException(String.Format(
                "Type {0} is not serializable. Consider adding transient attribute.\nThe path to this type is:\n{1}",
                typeToScan, path
                                            )
                );
        }

        private static bool IsPrimitive(Type type)
        {
            return type.IsPrimitive || type.IsEnum;
        }

        private static bool IsSerializable(Type type)
        {
            return !type.IsInterface; //no double checks
        }

        private static bool ShouldFieldsBeScanned(Type type)
        {
            return !IsPrimitive(type) && !type.GetInterfaces().Any(x =>
                                                                   (x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>)) 
                                                                   || x == typeof(ICollection)
                                              );
        }

        private static IEnumerable<Type> GetElementTypes(Type type)
        {
            if(type.IsArray)
            {
                return new [] { type.GetElementType() };
            }
            if(type.IsGenericType)
            {
                return type.GetGenericArguments();
            }
            return new Type[0];
            // TODO: for collections, dictionaries and so on
        }

        private static readonly HashSet<Type> IllegalTypes = new HashSet<Type>
        {
            typeof(IntPtr)
        };

        private Type[] types;
    }
}
