namespace Axl.Compiler.Syntax;

public static class SyntaxElementEnumerableExtensions
{
    extension(IEnumerable<SyntaxElement> array)
    {
        public IEnumerable<SyntaxElement> AfterToken(TokenKind token)
            => array
                .SkipWhile(element => !(element is Token childToken && childToken.Kind == token))
                .Skip(1); // Skip the token itself

        public T FirstOfType<T>() where T : SyntaxElement
            => array.OfType<T>().First();
        
        public T? FirstOfTypeOrNull<T>() where T : SyntaxElement
            => array.OfType<T>().FirstOrDefault();
        
        public T SecondOfType<T>() where T : SyntaxElement
            => array.OfType<T>().Skip(1).First();
        
        public Token FirstNonTriviaToken()
            => array.OfType<Token>().First(t => !t.Kind.IsTrivia);
    }
}