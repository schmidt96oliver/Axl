using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests.Lsp;

public sealed partial class CompletionTests
{
    public sealed class StructInit
    {
        [Fact]
        public void Single_1()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub Field1: I32; pub Field2: I32; pub Field3: I32; }
                                                     let t = Test(|
                                                     """), "Field1 =  Field2 =  Field3 = ");
        [Fact]
        public void Single_2()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub Field1: I32; pub Field2: I32; pub Field3: I32; }
                                                     let t = Test(Fi|
                                                     """), "Field1 =  Field2 =  Field3 = ");
        [Fact]
        public void Single_3()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub Field1: I32; pub Field2: I32; pub Field3: I32; }
                                                     let t = Test(Fi|e
                                                     """), "Field1 =  Field2 =  Field3 = ");
        [Fact]
        public void Single_4()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub Field1: I32; pub Field2: I32; pub Field3: I32; }
                                                     let t = Test(Field1 |
                                                     """), "");
        [Fact]
        public void Single_5()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub Field1: I32; pub Field2: I32; pub Field3: I32; }
                                                     let t = Test(Field1 = |
                                                     """), """
                Base
                Panic Assert TODO Unreachable Print PrintLine
                return if mut true false
                Test Bool F32 F64 I32 I64 Never String Unit
                """);
        [Fact]
        public void Single_6()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub Field1: I32; pub Field2: I32; pub Field3: I32; }
                                                     let t = Test(Field1 = 1,|
                                                     """), "Field2 =  Field3 = ");
        [Fact]
        public void Single_7()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub Field1: I32; pub Field2: I32; pub Field3: I32; }
                                                     let t = Test(Field1 = 1, |
                                                     """), "Field2 =  Field3 = ");
        [Fact]
        public void Single_8()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub Field1: I32; pub Field2: I32; pub Field3: I32; }
                                                     let t = Test(Field1 = 1, Fi|
                                                     """), "Field2 =  Field3 = ");
        [Fact]
        public void Single_9()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub Field1: I32; pub Field2: I32; pub Field3: I32; }
                                                     let t = Test(Field1 = 1, Field2 = 2, |
                                                     """), "Field3 = ");
        [Fact]
        public void Single_10()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub Field1: I32; pub Field2: I32; pub Field3: I32; }
                                                     let t = Test(Field1 = 1, Field2 = 2, F|)
                                                     """), "Field3 = ");


        [Fact]
        public void Many_1()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub F1: I32; F2: I32; }
                                                     struct Test[T] { pub F1: I32; F3: I32; }
                                                     struct Test[T,U] { pub F3: I32; F4: I32; }
                                                     let t = Test(|
                                                     """), "F1 =  F2 =  F3 =  F4 = ");
        [Fact]
        public void Many_2()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub F1: I32; F2: I32; }
                                                     struct Test[T] { pub F1: I32; F3: I32; }
                                                     struct Test[T,U] { pub F3: I32; F4: I32; }
                                                     let t = Test(F1 = 1, |
                                                     """), "F2 =  F3 = ");
        [Fact]
        public void Many_3()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub F1: I32; F2: I32; }
                                                     struct Test[T] { pub F1: I32; F3: I32; }
                                                     struct Test[T,U] { pub F3: I32; F4: I32; }
                                                     let t = Test(F2 = 1, |
                                                     """), "F1 = ");
        [Fact]
        public void Many_4()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub F1: I32; F2: I32; }
                                                     struct Test[T] { pub F1: I32; F3: I32; }
                                                     struct Test[T,U] { pub F3: I32; F4: I32; }
                                                     let t = Test(F4 = 1, |
                                                     """), "F3 = ");
        [Fact]
        public void Many_5()
            => InlineSnapshot.Validate(ItemsWithBase("""
                                                     struct Test { pub F1: I32; F2: I32; }
                                                     struct Test[T] { pub F1: I32; F3: I32; }
                                                     struct Test[T,U] { pub F3: I32; F4: I32; }
                                                     let t = Test(F3 = 1, |
                                                     """), "F1 =  F4 = ");
    }
}