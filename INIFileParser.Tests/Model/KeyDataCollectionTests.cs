using IniParser.Model;
using Xunit;

namespace INIFileParser.Tests.Model
{
    public class KeyDataCollectionTests
    {
        [Fact]
        public void AddKey_AppendsAndSetsSingleValues()
        {
            var sut = new KeyDataCollection();
            var added = sut.AddKey("Test", "Foo");
            Assert.True(added);
            
            var kd = sut.GetKeyData("Test");
            Assert.Equal("Foo", kd.Value);
            
            added = sut.AddKey("Test", "Bar");
            Assert.False(added);
            
            kd = sut.GetKeyData("Test");
            Assert.Equal(new [] { "Foo", "Bar" }, kd.ValueList);
        }
        
        [Fact]
        public void AddKey_AppendsAndSetsMultipleValues()
        {
            var sut = new KeyDataCollection();
            var added = sut.AddKey("Test", "Foo", "Bar");
            Assert.True(added);
            
            var kd = sut.GetKeyData("Test");
            Assert.Equal(new []{"Foo", "Bar"}, kd.ValueList);
            
            added = sut.AddKey("Test", "Biz", "Baz");
            Assert.False(added);
            
            kd = sut.GetKeyData("Test");
            Assert.Equal(new [] { "Foo", "Bar", "Biz", "Baz" }, kd.ValueList);
        }

        [Fact]
        public void Merge_DoesNotReplaceExistingKeys()
        {
            var sut = new KeyDataCollection();
            var toMerge = new KeyDataCollection();

            sut.AddKey("Test", "Foo");
            toMerge.AddKey("Test", "Bar");
            sut.Merge(toMerge);

            var result = sut.GetKeyData("Test");
            Assert.Equal("Foo", result.Value);
            Assert.Single(result.ValueList);
        }
        
        [Fact]
        public void Merge_IncludesAllValues()
        {
            var sut = new KeyDataCollection();
            var toMerge = new KeyDataCollection();
            var values = new[] { "Foo", "Bar" };
            
            toMerge.AddKey("Test", values);
            sut.Merge(toMerge);

            var result = sut.GetKeyData("Test");
            Assert.Equal(values, result.ValueList);
        }
    }
}