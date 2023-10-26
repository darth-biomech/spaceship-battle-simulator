using UnityEngine;

namespace DefaultNamespace
{
    public class RocketController : MonoBehaviour
    {
        public float accelerationG = 50;
        public float angularVelocity = 90;
        public Vector3 prevFramePos;
        private Vector3 _velocity;
        private ShipController ship;
        private Rigidbody _rb,t_rb;
        public ShipController target;

        private void Awake()
        {
            ship = FindObjectOfType<ShipController>();
            _rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            t_rb = target.GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (ship.simIsActive)
            {
                
                Vector3 TgtPosAtTime(float time, Vector3 pos0, Vector3 vel0, Vector3 acc)
                { return pos0 + vel0 * time + acc * time * time * 0.5f;}

                float t = ((9.80665f * accelerationG * ShipController._scale) * 1000);
                Vector3 projectedPos = TgtPosAtTime(
                    (target.transform.position-transform.position+ (_rb.velocity * Time.fixedDeltaTime)).magnitude / t, 
                    target.transform.position,
                    target._rb.velocity,
                    target.transform.up * target.currentAcceleration);
                for (int i = 0; i < 2; i++)
                {
                    Vector3 oldpos = projectedPos;
                    projectedPos = TgtPosAtTime(
                        (oldpos-transform.position+(_rb.velocity * Time.fixedDeltaTime)).magnitude / t, 
                        target.transform.position,
                        target._rb.velocity ,
                        target.transform.up * target.currentAcceleration);
                    if ((oldpos - projectedPos).magnitude < target._box.size.y / 2) break;
                }

                _rb.velocity = _velocity;
                _velocity += (transform.forward * (9.80665f * accelerationG * ShipController._scale)) * Time.fixedDeltaTime;
                _rb.angularVelocity = Vector3.zero;
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(
                        (projectedPos - transform.position).normalized),
                    angularVelocity * Time.fixedDeltaTime);
            }
            else if (_rb.velocity != Vector3.zero) _rb.velocity = Vector3.zero;
        }
    }
}