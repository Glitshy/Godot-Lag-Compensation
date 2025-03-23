using Godot;
using System;
using PG.LagCompensation.Parametric;

#if TOOLS

namespace PG.LagCompensation.Base
{
    [Tool]
    public partial class LagCompensationPlugin : EditorPlugin
    {
        private EditorNode3DGizmoPlugin gizmoPlugin = new HitColliderGizmos();

        public override void _EnterTree()
        {
            base._EnterTree();

            AddNode3DGizmoPlugin(gizmoPlugin);
        }

        public override void _ExitTree()
        {
            base._ExitTree();

            RemoveNode3DGizmoPlugin(gizmoPlugin);
        }
    }

}

#endif //TOOLS