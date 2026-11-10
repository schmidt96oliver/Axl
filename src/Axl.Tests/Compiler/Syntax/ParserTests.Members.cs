using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Compiler.Syntax;

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
                · · NameRef
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
                · · NameRef
                · · · ??ID
                · Annotation
                · · '@'
                · · NameRef
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
                · · NameRef 'ID'
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
                · · NameRef 'ID'
                · Annotation
                · · '@'
                · · NameRef 'ID2'
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
                · · NameRef 'ID'
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
                · · NameRef 'ID'
                · Annotation
                · · '@'
                · · NameRef 'ID2'
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
                · · NameRef 'ID'
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
                · · NameRef 'ID'
                · Annotation
                · · '@'
                · · NameRef 'ID'
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
                · · NameRef 'ID'
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
                · · NameRef 'ID'
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
                · · NameRef 'ID'
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
                · · NameRef 'ID'
                · 'struct'
                · NameRef 'T'
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
                · · NameRef 'ID'
                · 'pub'
                · 'struct'
                · NameRef 'T'
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
                · · NameRef 'ID'
                · Annotation
                · · '@'
                · · NameRef 'ID2'
                · 'pub'
                · 'struct'
                · NameRef 'T'
                · ';'
                """);

        [Fact]
        public void FunTypeParams_0()
            => InlineSnapshot.Validate(Tree("""
                                            fun A[]() { }
                                            """), """
                ERROR MissingToken@[6, 6): Expected a type parameter name.


                FunDecl
                · 'fun'
                · FunName 'A'
                · TypeParamList
                · · '['
                · · TypeParam
                · · · NameRef
                · · · · ??ID
                · · ']'
                · ParamList '(' ')'
                · FunBody
                · · BlockExpr '{' '}'
                """);
        [Fact]
        public void FunTypeParams_1()
            => InlineSnapshot.Validate(Tree("""
                                            fun A[T1]() { }
                                            """), """
                FunDecl
                · 'fun'
                · FunName 'A'
                · TypeParamList
                · · '['
                · · TypeParam
                · · · NameRef 'T1'
                · · ']'
                · ParamList '(' ')'
                · FunBody
                · · BlockExpr '{' '}'
                """);
        [Fact]
        public void FunTypeParams_2()
            => InlineSnapshot.Validate(Tree("""
                                            fun A[T1, T2]() { }
                                            """), """
                FunDecl
                · 'fun'
                · FunName 'A'
                · TypeParamList
                · · '['
                · · TypeParam
                · · · NameRef 'T1'
                · · ','
                · · TypeParam
                · · · NameRef 'T2'
                · · ']'
                · ParamList '(' ')'
                · FunBody
                · · BlockExpr '{' '}'
                """);
        [Fact]
        public void FunTypeParams_3()
            => InlineSnapshot.Validate(Tree("""
                                            fun A[T1, T2, T3]() { }
                                            """), """
                FunDecl
                · 'fun'
                · FunName 'A'
                · TypeParamList
                · · '['
                · · TypeParam
                · · · NameRef 'T1'
                · · ','
                · · TypeParam
                · · · NameRef 'T2'
                · · ','
                · · TypeParam
                · · · NameRef 'T3'
                · · ']'
                · ParamList '(' ')'
                · FunBody
                · · BlockExpr '{' '}'
                """);
        
        [Fact]
        public void StructTypeParams_0()
            => InlineSnapshot.Validate(Tree("""
                                            struct A[] { }
                                            """), """
                ERROR MissingToken@[9, 9): Expected a type parameter name.


                StructDecl
                · 'struct'
                · NameRef 'A'
                · TypeParamList
                · · '['
                · · TypeParam
                · · · NameRef
                · · · · ??ID
                · · ']'
                · StructBody '{' '}'
                """);
        [Fact]
        public void StructTypeParams_1()
            => InlineSnapshot.Validate(Tree("""
                                            struct A[T1] { }
                                            """), """
                StructDecl
                · 'struct'
                · NameRef 'A'
                · TypeParamList
                · · '['
                · · TypeParam
                · · · NameRef 'T1'
                · · ']'
                · StructBody '{' '}'
                """);
        [Fact]
        public void StructTypeParams_2()
            => InlineSnapshot.Validate(Tree("""
                                            struct A[T1, T2];
                                            """), """
                StructDecl
                · 'struct'
                · NameRef 'A'
                · TypeParamList
                · · '['
                · · TypeParam
                · · · NameRef 'T1'
                · · ','
                · · TypeParam
                · · · NameRef 'T2'
                · · ']'
                · ';'
                """);
        [Fact]
        public void StructTypeParams_3()
            => InlineSnapshot.Validate(Tree("""
                                            struct A[T1, T2, T3];
                                            """), """
                StructDecl
                · 'struct'
                · NameRef 'A'
                · TypeParamList
                · · '['
                · · TypeParam
                · · · NameRef 'T1'
                · · ','
                · · TypeParam
                · · · NameRef 'T2'
                · · ','
                · · TypeParam
                · · · NameRef 'T3'
                · · ']'
                · ';'
                """);
    }
}