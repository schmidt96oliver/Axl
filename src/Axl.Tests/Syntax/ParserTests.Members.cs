using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Syntax;

public partial class ParserTests
{
    public sealed class Members
    {
        [Fact]
        public void Annotations_Alone_1()
            => InlineSnapshot.Validate(Tree("""
                                            @
                                            """), """
                ERROR MissingToken@[1, 1): Expected an identifier.


                Garbage
                · Annotation
                · · '@'
                · · IdName
                · · · ??ID
                """);
        [Fact]
        public void Annotations_Alone_2()
            => InlineSnapshot.Validate(Tree("""
                                            @ @
                                            """), """
                ERROR MissingToken@[1, 1): Expected an identifier.
                ERROR MissingToken@[3, 3): Expected an identifier.


                Garbage
                · Annotation
                · · '@'
                · · IdName
                · · · ??ID
                · Annotation
                · · '@'
                · · IdName
                · · · ??ID
                """);
        [Fact]
        public void Annotations_Alone_3()
            => InlineSnapshot.Validate(Tree("""
                                            @ID
                                            """), """
                ERROR MissingToken@[3, 3): Expected a member.


                Garbage
                · Annotation
                · · '@'
                · · IdName 'ID'
                """);
        [Fact]
        public void Annotations_Alone_4()
            => InlineSnapshot.Validate(Tree("""
                                            @ID @ID2
                                            """), """
                ERROR MissingToken@[8, 8): Expected a member.


                Garbage
                · Annotation
                · · '@'
                · · IdName 'ID'
                · Annotation
                · · '@'
                · · IdName 'ID2'
                """);
        [Fact]
        public void Annotations_Pub_1()
            => InlineSnapshot.Validate(Tree("""
                                            @ID pub
                                            """), """
                ERROR MissingToken@[7, 7): Expected a member.


                Garbage
                · Annotation
                · · '@'
                · · IdName 'ID'
                · 'pub'
                """);
        [Fact]
        public void Annotations_Pub_2()
            => InlineSnapshot.Validate(Tree("""
                                            @ID @ID2 pub
                                            """), """
                ERROR MissingToken@[12, 12): Expected a member.


                Garbage
                · Annotation
                · · '@'
                · · IdName 'ID'
                · Annotation
                · · '@'
                · · IdName 'ID2'
                · 'pub'
                """);
        [Fact]
        public void Annotations_Fun_1()
            => InlineSnapshot.Validate(Tree("""
                                            @ID fun A() { }
                                            """), """
                FunDecl
                · Annotation
                · · '@'
                · · IdName 'ID'
                · 'fun'
                · FunName 'A'
                · ParamList '(' ')'
                · FunBody
                · · BlockExpr '{' '}'
                """);
        [Fact]
        public void Annotations_Fun_2()
            => InlineSnapshot.Validate(Tree("""
                                            @ID @ID fun A() { }
                                            """), """
                FunDecl
                · Annotation
                · · '@'
                · · IdName 'ID'
                · Annotation
                · · '@'
                · · IdName 'ID'
                · 'fun'
                · FunName 'A'
                · ParamList '(' ')'
                · FunBody
                · · BlockExpr '{' '}'
                """);
        [Fact]
        public void Annotations_Fun_3()
            => InlineSnapshot.Validate(Tree("""
                                            @ID pub fun A() { }
                                            """), """
                FunDecl
                · Annotation
                · · '@'
                · · IdName 'ID'
                · 'pub'
                · 'fun'
                · FunName 'A'
                · ParamList '(' ')'
                · FunBody
                · · BlockExpr '{' '}'
                """);
        [Fact]
        public void Annotations_Fun_4()
            => InlineSnapshot.Validate(Tree("""
                                            @ID static fun A() { }
                                            """), """
                FunDecl
                · Annotation
                · · '@'
                · · IdName 'ID'
                · 'static'
                · 'fun'
                · FunName 'A'
                · ParamList '(' ')'
                · FunBody
                · · BlockExpr '{' '}'
                """);
        [Fact]
        public void Annotations_Fun_5()
            => InlineSnapshot.Validate(Tree("""
                                            @ID mut fun A() { }
                                            """), """
                FunDecl
                · Annotation
                · · '@'
                · · IdName 'ID'
                · 'mut'
                · 'fun'
                · FunName 'A'
                · ParamList '(' ')'
                · FunBody
                · · BlockExpr '{' '}'
                """);
        [Fact]
        public void Annotations_Struct_1()
            => InlineSnapshot.Validate(Tree("""
                                            @ID struct T;
                                            """), """
                StructDecl
                · Annotation
                · · '@'
                · · IdName 'ID'
                · 'struct'
                · IdName 'T'
                · ';'
                """);
        [Fact]
        public void Annotations_Struct_2()
            => InlineSnapshot.Validate(Tree("""
                                            @ID pub struct T;
                                            """), """
                StructDecl
                · Annotation
                · · '@'
                · · IdName 'ID'
                · 'pub'
                · 'struct'
                · IdName 'T'
                · ';'
                """);
        [Fact]
        public void Annotations_Struct_3()
            => InlineSnapshot.Validate(Tree("""
                                            @ID @ID2 pub struct T;
                                            """), """
                StructDecl
                · Annotation
                · · '@'
                · · IdName 'ID'
                · Annotation
                · · '@'
                · · IdName 'ID2'
                · 'pub'
                · 'struct'
                · IdName 'T'
                · ';'
                """);
    }
}