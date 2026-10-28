using Axl.Compiler.Symbols;
using Axl.Compiler.Syntax.Tree;

namespace Axl.Compiler.Mir;

public static class MirPrinter
{
    public static string Print(this MutMirFunction function)
        => $"""
           --- fun {function.DebugName}({ string.Join(", ", function.Locals.Where(l => l.Kind is LocalKind.Parameter).Select(PrintWithType)) })
           { string.Join('\n', function.Locals.Where(l => l.Kind is not LocalKind.Parameter)
               .Select(PrintWithType)) }
           
           { string.Join("\n\n", function.BasicBlocks.Select(Print)) }
           """;

    public static string Print(this Place place)
        => place switch
        {
            Place.Local { Id: var id, Kind: var kind } => kind switch
            {
                LocalKind.Return => $"{id}[ret]",
                LocalKind.Parameter => $"{id}[{(place.Syntax as ParamSyntax)?.Name.Text.ToString() ?? ""}]",
                LocalKind.Variable => $"{id}[{(place.Syntax as VarDeclSyntax)?.Name.Text.ToString() ?? ""}]",
                _ => $"{id}",
            },
            Place.Field { Receiver: var receiver, FieldSymbol: var field } => $"{receiver.Print()}.{field.Name}",
            Place.Deref { Reference: var reference } => $"(deref {reference.Print()})",
            Place.Discard => "_",
        };

    public static string PrintWithType(this Place place)
        => place switch
        {
            Place.Local { Type: RefTypeSymbol { Inner: var inner } } => $"{place.Print()}: ref {inner.QualifiedName}",
            Place.Local => $"{place.Print()}: {place.Type.QualifiedName}",
            Place.Field { Receiver: var receiver, FieldSymbol: var field } =>
                $"{receiver.Print()}.{field.Name}: {place.Type.QualifiedName}",
            Place.Deref { Reference: var reference } => $"(deref {reference.Print()}): {reference.Type.QualifiedName}",
            Place.Discard => "_",
        };
    
    public static string Print(this Operand operand)
        => operand switch
        {
            Operand.Copy { Place: var place } => $"copy {place.Print()}",
            Operand.Ref { Place: var place } => $"ref {place.Print()}",
            Operand.Const { Value: var constValue } => constValue switch
                {
                    string s => $"const { PrintConstString(s) }",
                    _ => $"const {constValue.Value}",
                },
                
            Operand.Unreachable => "unreachable",
            Operand.Empty => "empty",
        };

    private static string PrintConstString(string str)
    {
        str = str.Replace("\n", "\\n").Replace("\r", "\\r");
        return str.Length < 10 ? $"\"{str}\"" : $"\"{str[..10]}\"..";
    }


    public static string Print(this Instruction instruction)
        => instruction switch
        {
            Instruction.Assign assign => $"{assign.Target.Print()} = {assign.Value.Print()}",
            Instruction.Call call => $"{call.Target.Print()} = call {
                call.Fun.QualifiedName}({ string.Join(", ", call.Arguments.Select(Print)) })",
            Instruction.Intrinsic intrinsic => $"{intrinsic.Target.Print()} = intrinsic {
                intrinsic.Kind}({string.Join(", ", intrinsic.Arguments.Select(Print))})",
            Instruction.VmCall vmCall => $"{vmCall.Target.Print()} = vmcall {
                vmCall.Kind}({string.Join(", ", vmCall.Arguments.Select(Print))})",
            Instruction.StructInit structInit => $"{structInit.Target.Print()} = struct {structInit.Struct.QualifiedName}({ string.Join(", ", structInit.FieldValues.Select(
                fieldInit => $"{fieldInit.Key.Name} = {fieldInit.Value.Print()}")) })",
        };

    public static string Print(this Terminator terminator)
        => terminator switch
        {
            Terminator.Branch branch => $"branch {branch.Condition.Print()} - True: {branch.True.Id} False: {branch.False.Id}",
            Terminator.Goto @goto => $"goto {@goto.Block.Id}",
            Terminator.Return => "return",
            Terminator.Unreachable => "unreachable",
        };

    public static string Print(this MutBasicBlock basicBlock)
        => basicBlock.Instructions.Count > 0
            ? $"""
               {basicBlock.Id}: {basicBlock.DebugLabel}
               {string.Join('\n', basicBlock.Instructions.Select(i => "   " + i.Print()))}
                  {basicBlock.Terminator?.Print() ?? "missing terminator"}
               """
            : $"""
               {basicBlock.Id}: {basicBlock.DebugLabel}
                  {basicBlock.Terminator?.Print() ?? "missing terminator"}
               """;
}