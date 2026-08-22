namespace Axl.Compiler.Syntax;

public static class SyntaxElementEnumerableExtensions
{
    extension(IEnumerable<SyntaxElement> array)
    {
        public IEnumerable<SyntaxElement> AfterToken(TokenKind token)
            => array
                .SkipWhile(element => !(element is Token childToken && childToken.Kind == token))
                .Skip(1); // Skip the token itself

        public IEnumerable<SyntaxNode> NodesOfAnyType<T1, T2>() 
            where T1 : SyntaxNode 
            where T2 : SyntaxNode
            => array.OfType<SyntaxNode>().Where(element => element is T1 or T2);
        
        public T FirstOfType<T>() where T : SyntaxElement
            => array.OfType<T>().First();
        
        public T? FirstOfTypeOrNull<T>() where T : SyntaxElement
            => array.OfType<T>().FirstOrDefault();
        
        public T SecondOfType<T>() where T : SyntaxElement
            => array.OfType<T>().Skip(1).First();
        
        public Token? FirstNonTriviaTokenOrNull()
            => array.OfType<Token>().FirstOrDefault(t => !t.Kind.IsTrivia);
        
        public Token FirstNonTriviaToken()
            => array.OfType<Token>().First(t => !t.Kind.IsTrivia);

        public SyntaxNode FirstOfKind(SyntaxKind kind)
            => array.OfType<SyntaxNode>().First(node => node.Kind == kind);
        
        public SyntaxNode? FirstOfKindOrNull(SyntaxKind kind)
            => array.OfType<SyntaxNode>().FirstOrDefault(node => node.Kind == kind);
        
        public IEnumerable<SyntaxNode> OfKind(SyntaxKind kind)
            => array.OfType<SyntaxNode>().Where(node => node.Kind == kind);
    }
}