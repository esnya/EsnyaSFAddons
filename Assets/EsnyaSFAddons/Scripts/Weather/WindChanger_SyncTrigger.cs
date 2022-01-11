using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace EsnyaAircraftAssets
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(VRC_Pickup))]
    [RequireComponent(typeof(VRCObjectSync))]
    public class WindChanger_SyncTrigger : UdonSharpBehaviour
    {
        public override void OnPickupUseDown()
        {
            GetComponentInChildren<WindChanger_Sync>()._Sync();
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void Reset()
        {
            GetComponent<VRCObjectSync>().AllowCollisionOwnershipTransfer = false;
        }
#endif
    }
}
