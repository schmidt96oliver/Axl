using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Axl.Compiler;
using Axl.Compiler.Syntax;

namespace Axl.Tests.Syntax;

/// <summary>
/// The AST is a typed view over the untyped tree: every accessor searches the node's
/// children at the moment it is read. Nothing about that is checked at compile time, so
/// these tests read <em>every</em> accessor on <em>every</em> node of the corpus and its
/// mutations, and assert four invariants that must hold for any input, however broken:
/// <list type="number">
/// <item>No accessor throws.</item>
/// <item>An accessor declared non-nullable never returns <c>null</c>.</item>
/// <item>Two different accessors on the same node never return the same element.</item>
/// <item>Everything an accessor returns is a descendant of the node it was read from.</item>
/// </list>
/// Invariant 3 is the one that catches an accessor quietly returning the <em>wrong</em>
/// child, which no amount of "does it throw" testing can see.
/// <para>
/// Coverage is only as good as the corpus: an accessor on a node kind no corpus file
/// produces is never read here. <see cref="Corpus_ProducesEveryNodeType"/> reports which
/// ones those are.
/// </para>
/// </summary>
public class AstTests
{
    [Fact]
    public void Corpus_KeepsAccessorInvariants()
        => CorpusMutations.Check(CorpusMutations.AsIs, InspectAccessors,
            "AST accessors broke on valid corpus input.");

    /// <inheritdoc cref="CorpusMutations.Prefixes"/>
    [Fact]
    public void PrefixTruncationOnCorpus_KeepsAccessorInvariants()
        => CorpusMutations.Check(CorpusMutations.Prefixes, InspectAccessors,
            "AST accessors broke on a truncated corpus file.");

    /// <inheritdoc cref="CorpusMutations.TokenDeletions"/>
    [Fact]
    public void TokenDeletionOnCorpus_KeepsAccessorInvariants()
        => CorpusMutations.Check(CorpusMutations.TokenDeletions, InspectAccessors,
            "AST accessors broke on a corpus file with a token deleted.");


    /// <summary>
    /// Reports every AST class the corpus never produces. Those classes are dead weight
    /// in the tests above: their accessors are never read, so the green tests say nothing
    /// about them. Informational — write a corpus file rather than deleting the class.
    /// </summary>
    [Fact]
    public void Corpus_ProducesEveryNodeType()
    {
        var produced = new HashSet<Type>();
        foreach (var (_, text) in CorpusMutations.Files())
        foreach (var node in SyntaxWalk.AllNodesRecursive(Parser.Parse(SourceFileView.FromText(text)).FileSyntax))
            produced.Add(node.GetType());

        var missing = typeof(SyntaxNode).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(SyntaxNode)) && !type.IsAbstract)
            .Where(type => !produced.Contains(type))
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        var output = TestContext.Current.TestOutputHelper;
        if (missing.Count == 0)
        {
            output?.WriteLine($"All {produced.Count} AST classes are produced by the corpus.");
            return;
        }

        output?.WriteLine($"{missing.Count} AST class(es) are never produced by the corpus, " +
                          "so their accessors are never read by the invariant tests:");
        foreach (var name in missing)
            output?.WriteLine($"    {name}");
    }


    /// <summary>
    /// Parses <paramref name="text"/> and checks the four invariants on every node.
    /// </summary>
    private static IEnumerable<Finding> InspectAccessors(string text)
    {
        SyntaxTree? tree = null;
        Exception? parseError = null;
        try
        {
            tree = Parser.Parse(SourceFileView.FromText(text));
        }
        catch (Exception e)
        {
            parseError = e;
        }

        if (tree is null)
        {
            // Parsing itself is ParserTests' business. Report and move on, so a parser
            // regression does not masquerade as an accessor failure.
            yield return new Finding($"Parsing throws {parseError!.GetType().Name}",
                $"Parsing threw {parseError.GetType().Name}: {parseError.Message}");
            yield break;
        }

        var parents = BuildParentMap(tree.FileSyntax);

        foreach (var node in SyntaxWalk.AllNodesRecursive(tree.FileSyntax))
        foreach (var finding in InspectNode(node, parents))
            yield return finding;
    }

    private static IEnumerable<Finding> InspectNode(SyntaxNode node, ParentMap parents)
    {
        // Which accessor already returned a given element, for invariant 3.
        var returnedBy = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);

        foreach (var accessor in AccessorsOf(node.GetType()))
        {
            // --- (1) The accessor must not throw.
            // Sequences are materialized here: an accessor built from a `yield` or a LINQ
            // chain defers its throw until someone walks it, so merely reading the
            // property would let it pass.
            object? value = null;
            IReadOnlyList<object?> items = [];
            Exception? thrown = null;
            try
            {
                value = accessor.Property.GetValue(node);
                if (value is IEnumerable sequence and not string)
                    items = sequence.Cast<object?>().ToList();
            }
            catch (Exception e)
            {
                thrown = e is TargetInvocationException invocation ? invocation.InnerException! : e;
            }

            if (thrown is not null)
            {
                yield return new Finding(
                    $"{accessor.Name} throws {thrown.GetType().Name}",
                    $"Reading {accessor.Name} threw {thrown.GetType().Name}: {thrown.Message}");
                continue;
            }

            // --- (2) A non-nullable accessor must not return null.
            if (value is null && accessor.DeclaredNonNullable)
            {
                yield return new Finding(
                    $"{accessor.Name} returns null",
                    $"{accessor.Name} is declared non-nullable, but returned null. Either the " +
                    "slot is not guaranteed after error recovery, or the accessor should be nullable.");
            }

            foreach (var element in ElementsOf(value, items))
            {
                // --- (3) Two accessors must not return the same element.
                if (returnedBy.TryGetValue(element, out var other))
                {
                    // Sort the pair so the same collision always produces the same key.
                    var (first, second) = StringComparer.Ordinal.Compare(other, accessor.Property.Name) <= 0
                        ? (other, accessor.Property.Name)
                        : (accessor.Property.Name, other);

                    yield return new Finding(
                        $"{node.GetType().Name}: .{first} and .{second} return the same element",
                        $"On {node.Kind}@{node.FullSpan}, both accessors returned the same " +
                        $"{element.GetType().Name}@{element.FullSpan}. One of them is reading " +
                        "the wrong slot.");
                }
                else
                    returnedBy[element] = accessor.Property.Name;

                // --- (4) Everything returned must come from under this node.
                if (!parents.IsDescendantOf(element, node))
                {
                    yield return new Finding(
                        $"{accessor.Name} escapes its node",
                        $"On {node.Kind}@{node.FullSpan}, the accessor returned " +
                        $"{element.GetType().Name}@{element.FullSpan}, which is not a descendant.");
                }
            }
        }
    }


    /// <summary>
    /// The single element an accessor returned, or the elements of the sequence it
    /// returned. Sequence items that are not <see cref="SyntaxElement"/>s — the Dunet
    /// item unions, for instance — are covered by invariant 1 only.
    /// </summary>
    private static IEnumerable<SyntaxElement> ElementsOf(object? value, IReadOnlyList<object?> items)
    {
        if (value is SyntaxElement single)
        {
            yield return single;
            yield break;
        }

        foreach (var item in items)
        {
            if (item is SyntaxElement element)
                yield return element;
        }
    }


    /// <summary>
    /// Maps every element of one tree to its parent, so invariant 4 can walk upwards.
    /// </summary>
    private sealed class ParentMap
    {
        private readonly Dictionary<object, SyntaxNode> _parents = new(ReferenceEqualityComparer.Instance);

        public void Add(SyntaxElement child, SyntaxNode parent) => _parents[child] = parent;

        public bool IsDescendantOf(SyntaxElement element, SyntaxNode node)
        {
            for (var parent = _parents.GetValueOrDefault(element);
                 parent is not null;
                 parent = _parents.GetValueOrDefault(parent))
            {
                if (ReferenceEquals(parent, node))
                    return true;
            }

            return false;
        }
    }

    private static ParentMap BuildParentMap(SyntaxNode root)
    {
        var parents = new ParentMap();

        foreach (var node in SyntaxWalk.AllNodesRecursive(root))
        foreach (var child in node.Children)
            parents.Add(child, node);

        return parents;
    }


    /// <summary>
    /// A public property that belongs to the typed AST, i.e. anything a consumer reads
    /// that <see cref="SyntaxNode"/> itself does not already provide.
    /// </summary>
    /// <param name="Name">
    /// Qualified with the concrete node type, because that is what a reader needs to
    /// find the accessor — not the type that happens to declare it.
    /// </param>
    private readonly record struct Accessor(PropertyInfo Property, string Name, bool DeclaredNonNullable);

    private static readonly ConcurrentDictionary<Type, Accessor[]> AccessorCache = new();

    private static Accessor[] AccessorsOf(Type nodeType)
        => AccessorCache.GetOrAdd(nodeType, static type =>
        {
            // Not thread safe, so it stays local to this factory.
            var nullability = new NullabilityInfoContext();

            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.GetIndexParameters().Length == 0)
                .Where(property => property.DeclaringType != typeof(SyntaxNode)
                                   && property.DeclaringType != typeof(SyntaxElement))
                .Select(property => new Accessor(
                    property,
                    $"{type.Name}.{property.Name}",
                    nullability.Create(property).ReadState == NullabilityState.NotNull))
                .ToArray();
        });
}
