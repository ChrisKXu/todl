namespace Todl.Compiler.CodeAnalysis.Binding
{
    internal enum BinderFlags : uint
    {
        None = 0,
        AllowVariableDeclarationInAssignment = 1 // used in repl
    }

    internal static class BinderFlagsExtensions
    {
        public static bool Includes(this BinderFlags self, BinderFlags flags)
            => (self & flags) == flags;
    }
}
