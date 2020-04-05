using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOLF3D.WOLF3DGame
{
    public abstract class Room : Spatial
    {
        public virtual ARVROrigin ARVROrigin { get; set; }
        public virtual ARVRCamera ARVRCamera { get; set; }
        public virtual ARVRController LeftController { get; set; }
        public virtual ARVRController RightController { get; set; }
        public virtual void Enter()
        {
            ARVRCamera.Current = true;
        }
        public virtual void Exit() { }
    }
}
