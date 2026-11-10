using System.Collections.Frozen;
using System.Collections.Immutable;

namespace AxlLsp.Commands;

public static class CommandRegistry
{
    private static FrozenDictionary<string, ICommand> CommandsByIdentifier
    {
        get
        {
            if (field is not null)
                return field;

            var dict = new Dictionary<string, ICommand>();
            var all = typeof(ICommand).Assembly.GetTypes()
                .Where(type => type.IsAssignableTo(typeof(ICommand)));

            foreach (var subType in all)
            {
                var suitableConstructor = subType.GetConstructor([]);
                if (suitableConstructor is null)
                    continue;

                var command = (ICommand)suitableConstructor.Invoke([]);
                dict.Add(command.Identifier, command);
            }

            field = dict.ToFrozenDictionary();
            return field;
        }
    }

    public static ImmutableArray<ICommand> Commands => CommandsByIdentifier.Values;


    public static void TryExecute(string identifier)
    {
        if (CommandsByIdentifier.TryGetValue(identifier, out var command))
            command.Execute();
    }
}