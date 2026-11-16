namespace AxlCompiler.Syntax;

public enum PrecedenceComparisonResult
{
    RightBindsTighter,
    LeftBindsTighter,
    Ambiguous,
}

public static class PrecedenceComparison
{
    public static PrecedenceComparisonResult Compare(Precedence left, Precedence right)
    {
        // --- Ambiguity pairs
        if ((left, right) is (Precedence.Comparison, Precedence.Comparison))
            return PrecedenceComparisonResult.Ambiguous;

        // --- Right-associative pairs
        if (left == right && left is Precedence.Assign)
            return PrecedenceComparisonResult.RightBindsTighter;

        // --- Compare
        // If they are equal, LeftBindsTighter is returned.
        // That means, they will be left-associative.
        return (int)left < (int)right
            ? PrecedenceComparisonResult.RightBindsTighter
            : PrecedenceComparisonResult.LeftBindsTighter;
    }
}