using System;
using UnityEngine;

namespace phosphorite
{
    public sealed class LocalLanternGrip : IDisposable
    {
        readonly GameObject root;
        readonly GRToolLantern lantern;
        readonly Transform item;
        readonly Rigidbody body;
        readonly GameGrabbable grabbable;
        readonly MeshFilter[] meshes;
        bool held, rightHand, leftGrip, rightGrip, recallPending;
        bool recallRight;
        bool started;

        internal bool IsHeld { get { return held; } }
        internal bool RightHand { get { return rightHand; } }

        internal LocalLanternGrip(GameObject root, GRToolLantern lantern)
        {
            this.root = root;
            this.lantern = lantern;
            item = lantern.gameEntity.transform;
            body = item.GetComponent<Rigidbody>();
            grabbable = item.GetComponent<GameGrabbable>();
            meshes = item.GetComponentsInChildren<MeshFilter>(true);
        }

        internal void Recall(bool right)
        {
            recallRight = right;
            recallPending = true;
        }

        void PlaceAtHand()
        {
            bool right = recallRight;
            var hand = NativePortableAccess.Hand(right);
            if (hand == null || (!held || rightHand != right) && !NativePortableAccess.HandFree(right)) return;
            if (held) Release(false);
            rightHand = right;
            body.isKinematic = true;
            item.SetParent(root.transform, true);
            item.position = hand.position;
            item.rotation = hand.rotation;
            item.localScale = Vector3.one * hand.lossyScale.x;
            if (!root.activeSelf) root.SetActive(true);
            Attach(hand, hand.position, right);
            recallPending = false;
        }

        internal void Update()
        {
            if (recallPending) PlaceAtHand();
            if (!root.activeSelf) return;
            if (!started)
            {
                started = true;
                IgnorePlayerCollisions();
            }
            bool left = NativePortableAccess.Grip(false), right = NativePortableAccess.Grip(true);
            if (held)
            {
                var hand = NativePortableAccess.Hand(rightHand);
                if (hand == null) Release(false);
                else if (!(rightHand ? right : left)) Release(true);
            }
            else
            {
                if (left && !leftGrip) TryGrab(false);
                if (!held && right && !rightGrip) TryGrab(true);
            }
            leftGrip = left;
            rightGrip = right;
        }

        void TryGrab(bool right)
        {
            if (!NativePortableAccess.HandFree(right)) return;
            var hand = NativePortableAccess.Hand(right);
            Vector3 point;
            if (hand != null && NativePortableAccess.CanReach(lantern.gameEntity, meshes, hand.position, out point)) Attach(hand, point, right);
        }

        void Attach(Transform hand, Vector3 point, bool right)
        {
            var position = item.position + hand.position - point;
            var rotation = item.rotation;
            GameGrab grab;
            if (grabbable.GetBestGrabPoint(hand.position, hand.rotation, right ? GamePlayer.RIGHT_HAND : GamePlayer.LEFT_HAND, out grab))
            {
                position = grab.position;
                rotation = grab.rotation;
            }
            Vector3 localPosition = hand.InverseTransformPoint(position);
            Quaternion localRotation = Quaternion.Inverse(hand.rotation) * rotation;
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
            item.SetParent(hand, false);
            item.SetLocalPositionAndRotation(localPosition, localRotation);
            item.localScale = Vector3.one;
            held = true;
            rightHand = right;
            lantern.gameEntity.PlayCatchFx();
            if (lantern.gameEntity.OnGrabbed != null) lantern.gameEntity.OnGrabbed();
        }

        void Release(bool throwItem)
        {
            item.SetParent(root.transform, true);
            body.constraints = RigidbodyConstraints.None;
            body.useGravity = lantern.gameEntity.gravityController == null;
            body.isKinematic = false;
            body.linearVelocity = throwItem ? NativePortableAccess.ReleaseVelocity(rightHand, false) : Vector3.zero;
            body.angularVelocity = throwItem ? NativePortableAccess.ReleaseVelocity(rightHand, true) : Vector3.zero;
            held = false;
            body.WakeUp();
            if (throwItem) lantern.gameEntity.PlayThrowFx();
            if (lantern.gameEntity.OnReleased != null) lantern.gameEntity.OnReleased();
        }

        void IgnorePlayerCollisions()
        {
            var tagger = GorillaTagger.Instance;
            if (tagger == null) return;
            var local = tagger.GetComponentsInChildren<Collider>(true);
            var colliders = item.GetComponentsInChildren<Collider>(true);
            foreach (var own in colliders) foreach (var player in local)
                if (own != null && player != null && own != player && !player.transform.IsChildOf(item)) Physics.IgnoreCollision(own, player, true);
        }

        public void Dispose()
        {
            if (held && item != null) Release(false);
        }
    }
}
