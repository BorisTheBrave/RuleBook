using RuleBook;

namespace RuleBook.Test
{
    [TestFixture]
    public class FuncBookTests
    {
        [Test]
        public void Empty_book_continues()
        {
            var book = new FuncBook<int, int>();

            Assert.That(book.Evaluate(0), Is.EqualTo(RuleResult.Continue));

            Assert.Throws<Exception>(() => book.Invoke(0));
        }

        [Test]
        public void Instead_Returns_Value()
        {
            var book = new FuncBook<int, int>();
            book.AddRule().Instead(x => x + 1);

            Assert.That(book.Invoke(5), Is.EqualTo(6));
        }

        [Test]
        public void Do_Has_SideEffects()
        {
            var book = new FuncBook<int, int>();
            bool wasRun = false;
            book.AddRule().Do(x => { wasRun = true; });
            book.AddRule().Return(6);

            Assert.That(book.Invoke(5), Is.EqualTo(6));
            Assert.IsTrue(wasRun);
        }

        [Test]
        public void When_Skips_body()
        {
            var book = new FuncBook<int, int>();
            book.AddRule().When(x => x % 2 == 0).Instead(x => x / 2);
            book.AddRule().Instead(x => x * 3 + 1);

            Assert.That(book.Invoke(8), Is.EqualTo(4));
            Assert.That(book.Invoke(5), Is.EqualTo(16));
        }

        [Test]
        public void WrapBody()
        {
            var book = new FuncBook<int, int>();
            book.AddRule().WithWrapBody((inner, x) => {
                var result = inner(x * 3);
                if (result.TryGetReturnValue<int>(out var y))
                    return RuleResult.Return(y / 3);
                return result;
                });
            book.AddRule().Instead(x => x + 3);

            Assert.That(book.Invoke(3), Is.EqualTo(4));
        }


        [Test]
        public void Ordering()
        {
            var book = new FuncBook<int, int>();
            var list = new List<string>();
            book.AddRule().Do(_ => list.Add("default1"));
            // Later rules are run later
            book.AddRule().Do(_ => list.Add("default2"));
            // Rules with precondition are preferred over those without
            book.AddRule().When(_ => true).Do(_ => list.Add("when"));
            // Ordering takes effect
            book.AddRule().At(-1).When(_ => true).Do(_ => list.Add("order"));

            book.AddRule().Return(0);

            book.Invoke(0);

            CollectionAssert.AreEqual(new[]
            {
                "order",
                "when",
                "default1",
                "default2"
            }, list);
        }

        [Test]
        public void Mutable_Ordering()
        {
            var book = new FuncBook<int, int>();
            var list = new List<string>();
            var default1 = book.AddRule().Do(_ => list.Add("default1"));
            // Later rules are run later
            var default2 = book.AddRule().Do(_ => list.Add("default2"));
            // Rules with precondition are preferred over those without
            var when = book.AddRule().Do(_ => list.Add("when"));
            // Ordering takes effect
            var order = book.AddRule().When(_ => true).Do(_ => list.Add("order"));

            book.AddRule().Return(0);

            when.Condition = _ => true;
            order.Order = -1;

            book.Invoke(0);

            CollectionAssert.AreEqual(new[]
            {
                "order",
                "when",
                "default1",
                "default2"
            }, list);
        }

        [Test]
        public void Mutable_Parent()
        {
            var book1 = new FuncBook<int, int>();
            var book2 = new FuncBook<int, int>();
            var rule = book1.AddRule().Return(0);
            rule.Parent = book2;

            Assert.That(book1.Evaluate(0), Is.EqualTo(RuleResult.Continue));
            Assert.That(book2.Evaluate(0), Is.EqualTo(RuleResult.Return(0)));
        }
    }
}