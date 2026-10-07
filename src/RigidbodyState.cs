using UnityEngine;

namespace Quicksilver
{
    public struct RigidbodyState
    {
        public float drag;
        public float angularDrag;

        public RigidbodyState(Rigidbody rb)
        {
            drag = rb.drag;
            angularDrag = rb.angularDrag;
        }
    }
}
