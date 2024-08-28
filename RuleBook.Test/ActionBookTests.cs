using RuleBook;

namespace RuleBook.Test
{
    [TestFixture]
    public class ActionBookTests
    {
        [Test]
        public void Empty_book_does_nothing()
        {
            var book = new ActionBook<int>();

            Assert.That(book.Evaluate(0), Is.EqualTo(RuleResult.Continue));

            book.Invoke(0);
        }

        [Test]
        public void Stop_stops_Value()
        {
            var book = new ActionBook<int>();
            bool wasRun = false;
            book.AddRule().Stop();
            book.AddRule().Do(_ => wasRun = true);

            book.Invoke(5);
            Assert.That(wasRun, Is.False);

        }
    }
}