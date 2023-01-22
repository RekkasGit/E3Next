using System.Linq;
using IniParser.Model;
using Xunit;

namespace INIFileParser.Tests.Model
{
    public class KeyDataTests
    {
        [Fact]
        // Ensure consistency of Value getters and setters when multiple values are provided
        public void Value_AlwaysReturnsLastValueOnSet()
        {
            var sut = new KeyData("Test");
            var values = new [] {"Foo", "Bar", "Baz"};
            sut.Value = values[0];
            Assert.Equal(values[0], sut.Value);
            
            sut.Value = values[1];
            Assert.Equal(values[1], sut.Value);
            
            sut.Value = values[2];
            Assert.Equal(values[2], sut.Value);
        }
        
        [Fact]
        // TODO: Do we want to keep this behavior? It seems like an easy footgun as setting `Value` multiple
        // times will result in appending to `ValueList` rather than replacing `ValueList` with a new value.
        // This test will validate the currently expected behavior but feels unintuitive to me
        public void Value_AppendsValuesOnSet()
        {
            var sut = new KeyData("Test");
            var values = new [] {"Foo", "Bar", "Baz"};
            sut.Value = values[0];
            Assert.Equal(values.Take(1), sut.ValueList);
            
            sut.Value = values[1];
            Assert.Equal(values.Take(2), sut.ValueList);
            
            sut.Value = values[2];
            Assert.Equal(values.Take(3), sut.ValueList);
        }
    }
}