using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_BuddyPod : UdonSharpBehaviour
    {
        public GameObject Dial_Funcon;
        public float extendDuration = 5;
        public Transform drogueHead;
        public int drogueIKLength = 2;
        public int drogueLinearLength = 0;
        [NotNull] public DrogueHead drogueHeadTrigger;
        public float reSupplyInterval = 1;
        public float contactMaxDistance = 1f;
        public float contactMinDistance = 0.5f;
        public float drogueRestAngle = 5f;
        public float drogueRestDistance = 0.98f;

        public AudioSource fuelingSound;
        public AudioSource contactSound, detachSound;

        private bool onGround = true;
        [UdonSynced][FieldChangeCallback(nameof(Extend))] private bool _extend;
        private bool Extend
        {
            set
            {
                _extend = value;
                if (Dial_Funcon && Dial_Funcon.activeSelf != value) Dial_Funcon.SetActive(value);
                if (drogueHeadTrigger.gameObject.activeSelf != value) drogueHeadTrigger.gameObject.SetActive(value);
                if (Connected) Connected = false;
            }
            get => _extend;
        }

        [UdonSynced][FieldChangeCallback(nameof(Connected))] private bool _connected;
        private bool Connected
        {
            set
            {
                _connected = value;
                if (!value)
                {
                    targetEntity = null;
                    targetProbe = null;
                }
            }
            get => _connected;
        }

        private string triggerAxis;
        public void DFUNC_LeftDial()
        {
            triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger";
        }
        public void DFUNC_RightDial()
        {
            triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";
        }

        private bool selected;
        public void DFUNC_Selected()
        {
            selected = true;
        }
        public void DFUNC_Deselected()
        {
            selected = false;
        }

        public void KeyboardInput()
        {
            if (Extend || !onGround)
            {
                Extend = !Extend;
                RequestSerialization();
            }
        }

        private Transform[] drogueIKBones;
        private Vector3[] drogueIKBonePositions;
        private Transform drogueBase;
        private Vector3 drogueOffset;
        private float drogueLength;
        private Vector3 drogueRestPosition;
        public void SFEXT_L_EntityStart()
        {
            drogueHeadTrigger.transform.SetParent(drogueHead, false);

            drogueIKBones = new Transform[drogueIKLength];
            drogueIKBonePositions = new Vector3[drogueIKLength];
            var currentBone = drogueHead.parent;
            for (var i = 0; i < drogueIKLength; i++)
            {
                drogueIKBones[drogueIKLength - i - 1] = currentBone;
                drogueIKBonePositions[drogueIKLength - i - 1] = currentBone.localPosition;
                currentBone = currentBone.parent;
            }
            drogueBase = currentBone;
            drogueLength = Vector3.Distance(drogueHead.position, drogueBase.position);
            drogueOffset = drogueBase.InverseTransformVector(drogueHead.parent.position - drogueHead.position);
            drogueRestPosition = Quaternion.AngleAxis(-drogueRestAngle, Vector3.right) * Vector3.up * drogueLength * drogueRestDistance;

            Extend = false;

            ResolveIK(0, Vector3.zero);

            gameObject.SetActive(false);
        }

        private SaccEntity targetEntity;
        private Transform targetProbe;
        private bool hasPilot;
        public void SFEXT_G_PilotEnter()
        {
            gameObject.SetActive(true);
            drogueRestPosition = Quaternion.AngleAxis(-drogueRestAngle, Vector3.right) * Vector3.up * drogueLength * drogueRestDistance;
            hasPilot = true;
        }
        public void SFEXT_G_PilotExit()
        {
            hasPilot = false;
        }

        public void SFEXT_G_TakeOff()
        {
            onGround = false;
        }
        public void SFEXT_G_TouchDown()
        {
            onGround = true;
        }

        private float extendingRatio;
        private float prevReSupplyTime;
        private void Update()
        {
            if (selected)
            {
                if (GetTriggerDown() && (Extend || !onGround))
                {
                    Extend = !Extend;
                    RequestSerialization();
                }
            }
            var extendTarget = Extend ? 1.0f : 0.0f;
            var drogueMoving = !Mathf.Approximately(extendTarget, extendingRatio);
            var retracted = Mathf.Approximately(extendingRatio, 0.0f);

            if (drogueMoving) extendingRatio = Mathf.MoveTowards(extendingRatio, extendTarget, Time.deltaTime / extendDuration);

            if (targetEntity)
            {
                var targetProbePosition = drogueBase.InverseTransformPoint(targetProbe.position) + drogueOffset;
                var targetProbeDistance = targetProbePosition.magnitude / drogueLength;
                if (targetProbeDistance > contactMaxDistance || targetProbeDistance < contactMinDistance)
                {
                    Detach();
                    ResolveIK(extendingRatio, drogueRestPosition);
                }
                else
                {
                    ResolveIK(extendingRatio, targetProbePosition);
                    var time = Time.time;
                    if (time - prevReSupplyTime > reSupplyInterval)
                    {
                        prevReSupplyTime = time;
                        targetEntity.SendEventToExtensions("SFEXT_O_ReSupply");
                        Debug.Log("ReSupply");
                    }
                }
            }
            else if (drogueMoving)
            {
                ResolveIK(extendingRatio, drogueRestPosition);
            }
            else if (Connected)
            {
                FindProbe();
            }

            if (retracted && !drogueMoving && !hasPilot)
            {
                gameObject.SetActive(false);
            }
        }

        public void _Contact()
        {
            if (targetEntity) return;
            Contact(drogueHeadTrigger.targetEntity, drogueHeadTrigger.probeCollider.transform);
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(OnConnected));
        }

        public void OnConnected()
        {
            Connected = true;
            RequestSerialization();
        }

        public void OnDisconnected()
        {
            Connected = false;
            RequestSerialization();
        }

        private void Contact(SaccEntity entity, Transform probe)
        {
            targetEntity = entity;
            targetProbe = probe;
            SetPlay(contactSound, true);
            SetPlay(fuelingSound, true);
        }

        private void Detach()
        {
            targetEntity = null;
            targetProbe = null;
            SetPlay(detachSound, true);
            SetPlay(fuelingSound, false);
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(OnDisconnected));
        }

        private bool prevTrigger;
        private bool GetTriggerDown()
        {
            var trigger = Input.GetAxisRaw(triggerAxis) > 0.75f;
            var triggerDown = trigger && !prevTrigger;
            prevTrigger = trigger;
            return triggerDown;
        }

        private void ResolveIK(float extendingRatio, Vector3 drogueHeadPosition)
        {
            var distance = drogueHeadPosition.magnitude;
            var xz = Vector3.ProjectOnPlane(drogueHeadPosition, Vector3.up) * extendingRatio;
            var y = Vector3.Project(drogueHeadPosition, Vector3.up) * extendingRatio;
            var d = Mathf.Sqrt(Mathf.Max(drogueLength - distance, 0.0f) * 3 * distance / 8) * extendingRatio * Vector3.forward;
            var yScaler = 1.0f / (drogueIKLength - 1.0f);
            var xzScaler = 1.0f / (drogueIKLength - drogueLinearLength - 1.0f);
            for (var i = 0; i < drogueIKLength; i++)
            {
                var ypos = i * yScaler;
                var xzpos = Mathf.Max(i - drogueLinearLength, 0) * xzScaler;
                drogueIKBones[i].position = drogueBase.TransformPoint(ypos * y + xzpos * xz - Parabola1(xzpos) * d * xzpos);
            }
        }

        private void FindProbe()
        {
            var minDistance = float.MaxValue;
            var prefix = drogueHeadTrigger.prefix;
            var headPosition = drogueHeadTrigger.transform.position;
            Collider foundCollider = null;
            SaccEntity foundEntity = null;
            foreach (var collider in Physics.OverlapSphere(headPosition, 10, -1, QueryTriggerInteraction.Collide))
            {
                if (!collider || !collider.gameObject.name.StartsWith(prefix)) continue;

                var rigidbody = collider.attachedRigidbody;
                if (!rigidbody) continue;

                var distance = Vector3.Distance(headPosition, collider.transform.position);
                if (distance >= minDistance) continue;

                var entity = rigidbody.GetComponent<SaccEntity>();
                if (!entity) continue;

                foundCollider = collider;
                foundEntity = entity;
                minDistance = distance;
            }

            if (foundCollider && foundEntity)
            {
                targetEntity = foundEntity;
                targetProbe = foundCollider.transform;
            }
        }

        private void SetPlay(AudioSource audioSource, bool play)
        {
            if (!audioSource || audioSource.isPlaying == play) return;
            if (play) audioSource.Play();
            else audioSource.Stop();
        }

        private float Parabola1(float x)
        {
            return 4.0f * x * (1.0f - x);
        }
    }
}
