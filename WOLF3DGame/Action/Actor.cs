using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class Actor : Billboard
    {
        public Actor() : base() => Name = "Actor";

        public override void _Process(float delta)
        {
            base._Process(delta);
            if (MeshInstance.Visible && State != null
                && State.Shape is short shape
                && (ushort)(shape + (State.Rotate ?
                Direction8.Modulus(
                    Direction8.AngleToPoint(
                        GlobalTransform.origin.x,
                        GlobalTransform.origin.z,
                        GetViewport().GetCamera().GlobalTransform.origin.x,
                        GetViewport().GetCamera().GlobalTransform.origin.z
                    ).MirrorZ + Direction,
                    8)
                : 0)) is ushort newFrame
                && newFrame != Page)
                Page = newFrame;
        }

        #region objstruct
        //typedef struct objstruct
        //{
        //    activetype active;
        /*
        typedef enum {
            ac_badobject = -1,
            ac_no,
            ac_yes,
            ac_allways
        }
        activetype;
        */
        //    int ticcount;
        public ushort TicCount;
        //    classtype obclass;
        public string ObjClass;
        //    statetype* state;
        public State State { get; set; } = null;
        //    byte flags;                //    FL_SHOOTABLE, etc
        //#define FL_SHOOTABLE	1
        public bool Shootable = false;
        //#define FL_BONUS		2
        //#define FL_NEVERMARK	4
        public bool NeverMark = false;
        //#define FL_VISABLE		8
        //#define FL_ATTACKMODE	16
        public bool AttackMode = false;
        //#define FL_FIRSTATTACK	32
        public bool FirstAttack = false;
        //#define FL_AMBUSH		64
        public bool Ambush = false;
        //#define FL_NONMARK		128
        public bool NoMark = false;
        //    long distance;            // if negative, wait for that door to open
        //    dirtype dir;
        public Direction8 Direction { get; set; } = Direction8.SOUTH;
        //    fixed x, y;
        //    unsigned tilex, tiley;
        //    byte areanumber;
        //    int viewx;
        //    unsigned viewheight;
        //    fixed transx, transy;        // in global coord

        //    int angle;
        //    int hitpoints;
        public ushort HitPoints = 0;
        //    long speed;
        public uint Speed = 0;

        //    int temp1, temp2, temp3;
        //    struct objstruct    *next,*prev;
        //}
        //objtype;
        #endregion objstruct

        #region StateDelegates
        public static void T_Stand(Actor actor) => actor.T_Stand();
        public Actor T_Stand()
        {
            return this;
        }
        public static void T_Path(Actor actor) => actor.T_Path();
        public Actor T_Path()
        {
            return this;
        }
        public static void T_Chase(Actor actor) => actor.T_Chase();
        public Actor T_Chase()
        {
            return this;
        }
        public static void T_Shoot(Actor actor) => actor.T_Shoot();
        public Actor T_Shoot()
        {
            return this;
        }
        #endregion StateDelegates
    }
}
