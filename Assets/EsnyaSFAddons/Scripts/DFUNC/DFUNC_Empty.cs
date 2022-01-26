using UdonSharp;

namespace EsnyaAircraftAssets
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_Empty : UdonSharpBehaviour
    {
        public void SFEXT_L_EntityStart() { }
        public void DFUNC_LeftDial() { }
        public void DFUNC_RightDial() { }
        public void DFUNC_Selected() { }
        public void DFUNC_Deselected() { }
    }
}