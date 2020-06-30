using System.Reflection;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class State
    {
        public string Name { get; set; } = null;
        public XElement XML { get; set; } = null;
        public bool Rotate { get; set; } = false;
        public short Shape { get; set; } = -1;
        public short Tics
        {
            get => SecondsToTics(Seconds);
            set => Seconds = TicsToSeconds(value);
        }
        public float Seconds { get; set; } = 0f;
        public delegate void StateDelegate(Actor actor);
        public StateDelegate Think { get; set; } = null;
        public StateDelegate Act { get; set; } = null;
        public State Next { get; set; }

        public State(XElement xml)
        {
            XML = xml;
            if (xml?.Attribute("Name")?.Value is string name)
                Name = name;
            Rotate = xml.IsTrue("Rotate");
            if (short.TryParse(xml?.Attribute("Shape")?.Value, out short shape))
                Shape = shape;
            if (short.TryParse(xml?.Attribute("Tics")?.Value, out short tics))
                Tics = tics;
            if (xml?.Attribute("Think")?.Value is string sThink
                && typeof(Actor).GetMethod(sThink, BindingFlags.Public | BindingFlags.Static) is MethodInfo thinkMethod
                && thinkMethod.CreateDelegate(typeof(StateDelegate)) is StateDelegate think)
                Think = think;
            if (xml?.Attribute("Act")?.Value is string sAct
                && typeof(Actor).GetMethod(sAct, BindingFlags.Public | BindingFlags.Static) is MethodInfo actMethod
                && actMethod.CreateDelegate(typeof(StateDelegate)) is StateDelegate act)
                Act = act;
            Next = this;
        }

        public static float TicsToSeconds(int tics) => tics / 70f;
        public static short SecondsToTics(float seconds) => (short)(seconds * 70f);
    }
}
