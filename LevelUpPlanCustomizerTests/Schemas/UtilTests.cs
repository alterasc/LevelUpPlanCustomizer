using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LevelUpPlanCustomizer.Base.Schemas.Tests
{
    [TestClass()]
    public class UtilTests
    {
        [TestMethod()]
        public void GuidParsingTest()
        {
            var asserted = Guid.Parse("3adc3439f98cb534ba98df59838f02c7");
            var ref1 = Utils.ParseRef("3adc3439f98cb534ba98df59838f02c7");
            Assert.AreEqual(asserted, ref1);
            var ref2 = Utils.ParseRef("link: 3adc3439f98cb534ba98df59838f02c7 (CavalierClass :BlueprintCharacterClass)");
            Assert.AreEqual(asserted, ref2);
            var ref3 = Utils.ParseRef("Blueprint:3adc3439f98cb534ba98df59838f02c7:PaladinClass");
            Assert.AreEqual(asserted, ref3);
        }
    }
}