using System.Reflection;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class State
    {
        public string Name { get; private set; } = null;
        public XElement XML { get; private set; } = null;
        public bool Rotate { get; private set; } = false;
        public short Shape { get; private set; } = -1;
        public short Tics
        {
            get => Assets.SecondsToTics(Seconds);
            private set => Seconds = Assets.TicsToSeconds(value);
        }
        public float Seconds { get; private set; } = 0f;
        public delegate void StateDelegate(Actor actor, float delta = 0f);
        public StateDelegate Think { get; private set; } = null;
        public StateDelegate Act { get; private set; } = null;
        public State Next { get; set; }
        public bool Mark { get; private set; } = true;
        public bool Alive { get; private set; } = true;
        public float SpeakerHeight { get; private set; } = Assets.HalfWallHeight;
        public uint ActorSpeed
        {
            get => (uint)(Speed / Assets.ActorSpeedConversion);
            set => Speed = value * Assets.ActorSpeedConversion;
        }
        public float Speed = 0f;
        public State(XElement xml)
        {
            if ((XML = xml) is XElement)
            {
                if (XML.Attribute("Name")?.Value is string name)
                    Name = name;
                Rotate = XML.IsTrue("Rotate");
                if (short.TryParse(XML.Attribute("Shape")?.Value, out short shape))
                {
                    Shape = shape;
                    if (ushort.TryParse(XML.Attribute("SpeakerHeight")?.Value, out ushort speakerHeight))
                        SpeakerHeight = speakerHeight / Assets.VSwapTextures[shape].GetHeight() * Assets.WallHeight;
                }
                if (short.TryParse(XML.Attribute("Tics")?.Value, out short tics))
                    Tics = tics;
                if (XML.Attribute("Think")?.Value is string sThink
                    && typeof(Actor).GetMethod(sThink, BindingFlags.Public | BindingFlags.Static) is MethodInfo thinkMethod
                    && thinkMethod.CreateDelegate(typeof(StateDelegate)) is StateDelegate think)
                    Think = think;
                if (XML.Attribute("Act")?.Value is string sAct
                    && typeof(Actor).GetMethod(sAct, BindingFlags.Public | BindingFlags.Static) is MethodInfo actMethod
                    && actMethod.CreateDelegate(typeof(StateDelegate)) is StateDelegate act)
                    Act = act;
                Mark = !XML.IsFalse("Mark");
                Alive = !XML.IsFalse("Alive");
                if (ushort.TryParse(XML.Attribute("Speed")?.Value, out ushort speed))
                    ActorSpeed = speed;
            }
            Next = this;
        }
    }
}
