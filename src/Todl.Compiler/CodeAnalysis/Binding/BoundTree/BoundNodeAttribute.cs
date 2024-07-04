using System;

namespace Todl.Compiler.CodeAnalysis.Binding.BoundTree;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal class BoundNodeAttribute : Attribute
{
}
