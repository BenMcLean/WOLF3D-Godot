using NUnit.Framework;
using System.Collections.Generic;
using System.Xml.Linq;

namespace WOLF3DTest
{
	public class UnitTest1
	{
		public Dictionary<string, uint> Stuff = new Dictionary<string, uint>();
		[Test]
		public void Test()
		{
			XElement lessThan1 = XElement.Parse("<Condition If=\"Stuff\" LessThan=\"1\" />"),
				greaterThan0 = XElement.Parse("<Condition If=\"Stuff\" GreaterThan=\"0\" />");
			Assert.IsTrue(ConditionalOne(XElement.Parse("<NoConditions/>")));
			Stuff["Stuff"] = 0;
			Assert.IsTrue(ConditionalOne(lessThan1));
			Assert.IsFalse(ConditionalOne(greaterThan0));
			Stuff["Stuff"] = 1;
			Assert.IsFalse(ConditionalOne(lessThan1));
			Assert.IsTrue(ConditionalOne(greaterThan0));
		}
		public bool ConditionalOne(XElement xml) =>
			!(xml?.Attribute("If")?.Value is string stat
			&& !string.IsNullOrWhiteSpace(stat)
			&& Stuff[stat] is uint statusNumber)
			|| ((!uint.TryParse(xml?.Attribute("Equals")?.Value, out uint equals) || statusNumber == equals)
			&& (!uint.TryParse(xml?.Attribute("LessThan")?.Value, out uint less) || statusNumber < less)
			&& (!uint.TryParse(xml?.Attribute("GreaterThan")?.Value, out uint greater) || statusNumber > greater));
	}
}
