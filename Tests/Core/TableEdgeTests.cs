
using NUnit.Framework;
using TableCore.Core;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class TableEdgeTests
    {
        [Test]
        public void TableEdge_HasCorrectValues()
        {
            var values = System.Enum.GetValues(typeof(TableEdge));
            Assert.That(values, Has.Length.EqualTo(4));
            Assert.That(values, Contains.Item(TableEdge.Bottom));
            Assert.That(values, Contains.Item(TableEdge.Right));
            Assert.That(values, Contains.Item(TableEdge.Top));
            Assert.That(values, Contains.Item(TableEdge.Left));
        }
    }
}
