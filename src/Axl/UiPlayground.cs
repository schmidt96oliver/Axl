using Axl.Compiler;
using Axl.Compiler.Diagnostics;
using Axl.Compiler.Syntax;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Axl;

/// <summary>
/// Interactive Terminal.Gui view of the parse of <see cref="TestFilePath"/>.
/// Diagnostics on top, the syntax tree below. Reloads whenever the file changes on disk.
/// </summary>
public static class UiPlayground
{
    private static readonly string TestFilePath = Path.Combine("..", "..", "..", "..", "src", "Axl", "test.axl");

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    public static void Run()
    {
        using var app = Application.Create();
        app.Init();

        using var window = new PlaygroundWindow(app, TestFilePath);
        app.Run(window);
    }


    #region Colors

    /// <summary>
    /// Only the foreground and the style of these ever reach the screen - the background
    /// is taken from whatever the tree already painted, so that selection stays visible.
    /// </summary>
    private static Attribute Fg(StandardColor color, TextStyle style = TextStyle.None)
        => new(new Color(color), new Color(StandardColor.Black), style);

    private static readonly Attribute PlainAttribute = Fg(StandardColor.White);
    private static readonly Attribute KindAttribute = Fg(StandardColor.BrightGreen, TextStyle.Bold);
    private static readonly Attribute ErrorKindAttribute = Fg(StandardColor.BrightRed, TextStyle.Bold);
    private static readonly Attribute ErrorLineAttribute = Fg(StandardColor.BrightRed);
    private static readonly Attribute TokenAttribute = Fg(StandardColor.Khaki);
    private static readonly Attribute CoveredTextAttribute = Fg(StandardColor.Gray);
    private static readonly Attribute InErrorTokenAttribute = Fg(StandardColor.Red);
    private static readonly Attribute BrokenTokenAttribute = Fg(StandardColor.BrightRed, TextStyle.Underline);

    private static readonly Attribute DiagnosticErrorAttribute = Fg(StandardColor.BrightRed, TextStyle.Bold);
    private static readonly Attribute DiagnosticWarningAttribute = Fg(StandardColor.Gold, TextStyle.Bold);
    private static readonly Attribute DiagnosticInfoAttribute = Fg(StandardColor.SteelBlue, TextStyle.Bold);
    private static readonly Attribute LocationAttribute = Fg(StandardColor.DarkGray);
    private static readonly Attribute RelatedAttribute = Fg(StandardColor.DarkGray, TextStyle.Underline);
    private static readonly Attribute OkAttribute = Fg(StandardColor.BrightGreen);

    #endregion


    /// <summary>
    /// A pre-styled run of text inside a <see cref="Row"/>.
    /// </summary>
    private readonly record struct Segment(string Text, Attribute Attribute);

    /// <summary>
    /// Cells that Terminal.Gui's <c>Branch.GetLinePrefix</c> spends on one level of depth: the branch
    /// line of that ancestor and a space. Only holds while <see cref="TreeStyle.ShowBranchLines"/> is on.
    /// </summary>
    private const int CellsPerLevel = 2;

    /// <summary>
    /// One line in a tree. Carries its own colors, so that a single line can mix
    /// e.g. a green <see cref="SyntaxKind"/> with red underlined tokens.
    /// </summary>
    private sealed class Row(List<Segment> segments)
    {
        public List<Segment> Segments { get; } = segments;

        public List<Row> Children { get; } = [];

        /// <summary>
        /// For every <see cref="SyntaxKind.Error"/> node this row hangs under, how many levels above the
        /// row it sits - so <c>1</c> for a direct child. That is all it takes to find the spine running
        /// down its children, and unlike an absolute depth it cannot drift out of step with the tree.
        /// </summary>
        public IReadOnlyList<int> ErrorLanesAbove { get; init; } = [];

        /// <summary>The uncolored line, which is what the tree lays out and measures.</summary>
        public string Text { get; } = string.Concat(segments.Select(s => s.Text));

        public override string ToString() => Text;
    }


    /// <summary>
    /// A <see cref="TreeView{T}"/> that paints <see cref="Row.Segments"/> and folds on a plain
    /// left click anywhere on a row, not just on the expand/collapse symbol.
    /// </summary>
    private sealed class RowTreeView : TreeView<Row>
    {
        private readonly bool _selectable;

        /// <param name="selectable">
        /// <c>false</c> for a read-only display: no focus, no selected row, no highlight.
        /// </param>
        public RowTreeView(bool selectable = true)
        {
            _selectable = selectable;

            TreeBuilder = new DelegateTreeBuilder<Row>(row => row.Children, row => row.Children.Count > 0);
            AspectGetter = row => row.Text;
            MultiSelect = false;
            CanFocus = selectable;
            // Turning this off would shrink a depth level to a single cell and break CellsPerLevel.
            Style.ShowBranchLines = true;
            DrawLine += (_, e) => PaintSegments(e);
        }

        public void SetRoots(IEnumerable<Row> roots)
        {
            ClearObjects();
            AddObjects(roots);
            ExpandAll();

            if (_selectable)
            {
                GoToFirst();
                return;
            }

            SelectedObject = null;
            ScrollOffsetVertical = 0;
            ScrollOffsetHorizontal = 0;
        }

        protected override bool OnMouseEvent(Mouse mouse)
        {
            var row = mouse.Position is { } position ? GetObjectOnRow(position.Y) : null;
            var isFoldClick = mouse.Flags.HasFlag(MouseFlags.LeftButtonClicked) && row is { Children.Count: > 0 };

            // Let the base view select the row (and fold, if the expand symbol itself was hit).
            var wasExpanded = isFoldClick && IsExpanded(row!);
            var handled = base.OnMouseEvent(mouse);

            // The base did not fold, so the click was somewhere else on the row - fold it ourselves.
            if (isFoldClick && IsExpanded(row!) == wasExpanded)
            {
                if (wasExpanded)
                    Collapse(row!);
                else
                    Expand(row!);
            }

            if (!_selectable)
                SelectedObject = null;

            return handled;
        }

        private static void PaintSegments(DrawTreeViewLineEventArgs<Row> e)
        {
            var cells = e.Cells;
            if (cells is null || e.Model is null)
                return;

            // Negative when the view is scrolled horizontally, so the first segments fall off to the left.
            var index = e.IndexOfModelText;

            // A row reads [ancestor lanes][own connector][expand symbol][text], so the connector sits two
            // cells before the text. Deriving it from the text keeps this right under horizontal scrolling.
            var connector = index - 2;

            foreach (var levelsUp in e.Model.ErrorLanesAbove)
            {
                // The spine running down the error node's children sits one level inside the error node
                // itself: on a child that cell is its connector, in the rows between two children it is
                // the branch line of the child whose subtree they belong to.
                Paint(connector - CellsPerLevel * (levelsUp - 1), ErrorLineAttribute);

                // On a child that cell is the connector, so take the dash reaching to its text as well.
                if (levelsUp == 1)
                    Paint(connector + 1, ErrorLineAttribute);
            }

            foreach (var segment in e.Model.Segments)
            {
                foreach (var _ in segment.Text)
                {
                    if (index >= cells.Count)
                        return;

                    Paint(index, segment.Attribute);
                    index++;
                }
            }

            void Paint(int at, Attribute attribute)
            {
                if (at < 0 || at >= cells.Count)
                    return;

                // Keep the background the tree painted, so that any highlighting survives.
                var cell = cells[at];
                var background = cell.Attribute?.Background ?? new Color(StandardColor.Black);
                cell.Attribute = new Attribute(attribute.Foreground, background, attribute.Style);
                cells[at] = cell;
            }
        }
    }


    private sealed class PlaygroundWindow : Window
    {
        private readonly IApplication _app;
        private readonly string _path;
        private readonly FrameView _diagnosticsFrame;
        private readonly RowTreeView _diagnosticsView = new(selectable: false);
        private readonly FrameView _treeFrame;
        private readonly RowTreeView _treeView = new(selectable: false);
        private readonly Label _status = new();

        private string? _previousText;

        public PlaygroundWindow(IApplication app, string path)
        {
            _app = app;
            _path = path;

            Title = $"Axl Playground - {Path.GetFullPath(path)}";
            BorderStyle = LineStyle.None;

            _diagnosticsFrame = new FrameView
            {
                Title = "Diagnostics",
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Absolute(5),

                // Nothing in here is selectable, so taking focus would only light up the title.
                CanFocus = false,
            };
            _diagnosticsView.Width = Dim.Fill();
            _diagnosticsView.Height = Dim.Fill();
            _diagnosticsFrame.Add(_diagnosticsView);

            _treeFrame = new FrameView
            {
                Title = $"Syntax Tree - {Path.GetFileName(path)}",
                X = 0,
                Y = Pos.Bottom(_diagnosticsFrame),
                Width = Dim.Fill(),
                Height = Dim.Fill(1),
                CanFocus = false,
            };
            _treeView.Width = Dim.Fill();
            _treeView.Height = Dim.Fill();
            _treeFrame.Add(_treeView);

            _status.X = 0;
            _status.Y = Pos.AnchorEnd(1);
            _status.Width = Dim.Fill();
            _status.Text = "click a row to fold/unfold  -  wheel to scroll  -  reloads on save  -  Esc to quit";

            Add(_diagnosticsFrame, _treeFrame, _status);

            KeyDown += OnKeyDown;

            Reload();
            _app.AddTimeout(PollInterval, () =>
            {
                Reload();
                return true;
            });
        }

        private void OnKeyDown(object? sender, Key key)
        {
            if (key != Key.Esc)
                return;

            _app.RequestStop(this);
            key.Handled = true;
        }

        private void Reload()
        {
            SourceFileView source;
            try
            {
                source = SourceFileView.FromFile(_path);
            }
            catch (IOException)
            {
                // The editor is probably mid-write. Try again on the next tick.
                return;
            }

            if (source.File.Text == _previousText)
                return;
            _previousText = source.File.Text;

            var syntaxTree = Parser.Parse(source);
            var builder = new RowBuilder(syntaxTree, source);

            var diagnosticRows = builder.BuildDiagnostics();
            _diagnosticsView.SetRoots(diagnosticRows);
            _diagnosticsFrame.Title = syntaxTree.Diagnostics.Length switch
            {
                0 => "Diagnostics",
                var count => $"Diagnostics ({count})",
            };
            var diagnosticLines = diagnosticRows.Sum(row => 1 + row.Children.Count);
            _diagnosticsFrame.Height = Dim.Absolute(Math.Clamp(diagnosticLines + 2, 3, 14));

            _treeView.SetRoots([builder.BuildTree()]);
            _treeFrame.Title = syntaxTree.HasError
                ? $"Syntax Tree - {Path.GetFileName(_path)} (has errors)"
                : $"Syntax Tree - {Path.GetFileName(_path)}";

            SetNeedsLayout();
            SetNeedsDraw();
        }
    }


    /// <summary>
    /// Turns a <see cref="SyntaxTree"/> into colored <see cref="Row"/>s.
    /// </summary>
    private sealed class RowBuilder(SyntaxTree syntaxTree, SourceFileView source)
    {
        public Row BuildTree()
        {
            // The root covers the whole file, so a preview of it says nothing.
            var root = new Row([new Segment($"{syntaxTree.Root.Kind}", KindAttribute)]);
            AddChildren(root, syntaxTree.Root, onErrorNode: false, errorLanesAbove: []);
            return root;
        }

        public List<Row> BuildDiagnostics()
        {
            if (syntaxTree.Diagnostics.Length == 0)
                return [new Row([new Segment("No diagnostics.", OkAttribute)])];

            var rows = new List<Row>();
            foreach (var diagnostic in syntaxTree.Diagnostics)
            {
                var severityAttribute = diagnostic.DefaultSeverity switch
                {
                    DiagnosticSeverity.Error => DiagnosticErrorAttribute,
                    DiagnosticSeverity.Warning => DiagnosticWarningAttribute,
                    _ => DiagnosticInfoAttribute,
                };

                var locations = string.Join(" ", diagnostic.Locations.Select(GetLocationText));
                var row = new Row([
                    new Segment(diagnostic.Id, severityAttribute),
                    new Segment($" {locations}", LocationAttribute),
                    new Segment($":  {diagnostic.Message}", PlainAttribute),
                ]);

                foreach (var related in diagnostic.Related)
                {
                    row.Children.Add(new Row([
                        new Segment("related", RelatedAttribute),
                        new Segment($"@{GetLocationText(related.Location)}: {related.Label}", PlainAttribute),
                    ]));
                }

                rows.Add(row);
            }

            return rows;
        }

        /// <param name="onErrorNode">
        /// Whether <paramref name="node"/> is a <see cref="SyntaxKind.Error"/>, which only colors its
        /// own tokens red. A valid node inside an error node is green again, together with its tokens.
        /// </param>
        /// <param name="errorLanesAbove">
        /// Already relative to the children being added here, not to <paramref name="node"/>.
        /// </param>
        private void AddChildren(Row parent, SyntaxNode node, bool onErrorNode, IReadOnlyList<int> errorLanesAbove)
        {
            foreach (var child in node.Children)
            {
                if (child is Token { Kind.IsTrivia: false } token)
                {
                    parent.Children.Add(new Row(TokenSegments(token, onErrorNode))
                    {
                        ErrorLanesAbove = errorLanesAbove,
                    });
                }
                else if (child is SyntaxNode childNode)
                {
                    var isErrorNode = childNode.Kind is SyntaxKind.Garbage or SyntaxKind.ErrorExpr;
                    var kindAttribute = isErrorNode ? ErrorKindAttribute : KindAttribute;

                    var nonTrivia = childNode.Children
                        .Where(el => el is Token { Kind.IsTrivia: false } or SyntaxNode)
                        .ToList();

                    var segments = new List<Segment> { new($"{childNode.Kind}", kindAttribute) };

                    // A node that only holds tokens fits on one line. It spells its source out in
                    // full there, so a shortened preview next to the kind would only repeat it.
                    if (nonTrivia.All(el => el is Token))
                    {
                        foreach (var childToken in nonTrivia.OfType<Token>())
                        {
                            segments.Add(new Segment(" ", PlainAttribute));
                            segments.AddRange(TokenSegments(childToken, isErrorNode));
                        }

                        parent.Children.Add(new Row(segments) { ErrorLanesAbove = errorLanesAbove });
                        continue;
                    }

                    segments.Add(CoveredTextSegment(childNode));

                    // Everything below moves one level away from the error nodes we already hang under,
                    // and an error node itself opens a lane one level above its own children.
                    IReadOnlyList<int> deeperLanes = isErrorNode
                        ? [1, .. errorLanesAbove.Select(levelsUp => levelsUp + 1)]
                        : [.. errorLanesAbove.Select(levelsUp => levelsUp + 1)];

                    var childRow = new Row(segments) { ErrorLanesAbove = errorLanesAbove };
                    parent.Children.Add(childRow);
                    AddChildren(childRow, childNode, onErrorNode: isErrorNode, deeperLanes);
                }
            }
        }

        /// <summary>
        /// The source a node covers, kept short so that it stays a hint next to the kind instead of
        /// turning into a second view of the file.
        /// </summary>
        private Segment CoveredTextSegment(SyntaxNode node)
        {
            const int maxLength = 10;

            // Trivia at the edges is not what the node is about, so the tighter span reads better.
            var text = Escape(source.GetText(node.SyntaxSpan ?? node.Span));
            if (text.Length > maxLength)
                text = $"{text[..maxLength]}…";

            return new Segment($" {text}", CoveredTextAttribute);
        }

        private List<Segment> TokenSegments(Token token, bool onErrorNode)
        {
            var attribute = token.IsMissing ? BrokenTokenAttribute :
                onErrorNode ? InErrorTokenAttribute : TokenAttribute;
            var text = token.IsMissing
                ? $"{token.Kind.DisplayName}?"
                : $"'{Escape(source.GetText(token.Span))}'";

            var segments = new List<Segment> { new(text, attribute) };
            // if (token.IsMissing)
            //     segments.Add(new("missing ", ErrorKindAttribute));
            // segments.Add(new(text, attribute));

            return segments;
        }

        private string GetLocationText(SourceLocation location)
        {
            var startLinePos = location.File.GetLinePositionOrEof(location.Span.First);
            var endLinePos = location.File.GetLinePositionOrEof(location.Span.End);

            if (startLinePos.Line == endLinePos.Line)
                return $"l.{startLinePos.Line} @ {startLinePos.Column}-{endLinePos.Column}";

            return $"l.{startLinePos.Line}@{startLinePos.Column} - l.{endLinePos.Line}@{endLinePos.Column}";
        }

        /// <summary>Every row is a single line, so line breaks and tabs must become visible escapes.</summary>
        private static string Escape(ReadOnlySpan<char> text)
            => text.ToString().Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    }
}