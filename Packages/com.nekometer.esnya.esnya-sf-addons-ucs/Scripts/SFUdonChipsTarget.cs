using UdonSharp;
using UnityEngine;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using UCS;

namespace EsnyaAircraftAssets
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SFUdonChipsTarget : UdonSharpBehaviour
    {
        public float hitPoints = 30f;
        public float fee = 50.0f;
        public GameObject[] ExplodeOther;
        private Animator animaor;
        private float fullHealth;
        private UdonChips udonChips;

        private void Start()
        {
            animaor = GetComponent<Animator>();
            fullHealth = hitPoints;
            udonChips = GameObject.Find("UdonChips").GetComponent<UdonChips>();
        }

        private void OnParticleCollision(GameObject other)
        {
            if (other == null) return;

            if (hitPoints < 10)
            {
                udonChips.money += fee;
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Explode));
            }
            else SendCustomNetworkEvent(NetworkEventTarget.All, nameof(TargetTakeDamage));
        }

        public void TargetTakeDamage()
        {
            hitPoints -= 10;
        }

        public void Explode()
        {
            animaor.SetTrigger("explode");
            hitPoints = fullHealth;

            if (ExplodeOther != null)
            {
                foreach (GameObject Exploder in ExplodeOther)
                {
                    UdonBehaviour ExploderUdon = (UdonBehaviour)Exploder.GetComponent(typeof(UdonBehaviour));
                    if (ExploderUdon != null)
                    {
                        ExploderUdon.SendCustomEvent("Explode");
                    }
                }
            }
        }
    }
}
