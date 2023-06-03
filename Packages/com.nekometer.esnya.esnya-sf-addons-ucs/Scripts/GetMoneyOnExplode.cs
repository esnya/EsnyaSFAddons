using UdonSharp;
using UCS;

namespace EsnyaSFAddons.UCS
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GetMoneyOnExplode : UdonSharpBehaviour
    {
        public float money = 50.0f;
        private UdonChips udonChips;

        private void Start()
        {
            udonChips = UdonChips.GetInstance();
        }

        public void Explode()
        {
            udonChips.money += money;
        }
    }
}
