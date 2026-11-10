namespace AxlLsp.Commands;

public interface ICommand
{
    string Identifier { get; }

    string Title { get; }

    void Execute();
}