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
            book.AddRule().Instead(6);

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
            book.AddRule().WrapBody((inner, x) => {
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
            book.AddRule().Do(_ => list.Add("default"));
            // Later rules are run later
            book.AddRule().Do(_ => list.Add("default2"));
            // Rules with precondition are preferred over those without
            book.AddRule().When(_ => true).Do(_ => list.Add("when"));
            // Ordering takes effect
            book.AddRule().At(-1).When(_ => true).Do(_ => list.Add("order"));

            book.AddRule().Instead(0);

            book.Invoke(0);

            CollectionAssert.AreEqual(new[]
            {
                "order",
                "when",
                "default",
                "default2"
            }, list);
        }
    }
}