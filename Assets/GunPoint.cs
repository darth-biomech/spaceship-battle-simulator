using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class GunPoint : MonoBehaviour
    {
        public enum ProjectileType {Kinetic,Laser,Missile}

        public ProjectileType type = ProjectileType.Kinetic;
        private float _scale = 0.001f;
        public List<Projectile> projectiles = new();
        public List<Projectile> drawLines = new();
        public GOPool<LineRenderer> lines = new();
        private ShipController ship;
        private float firerateDelay = 0.4f;
        
        [Header("Enemy params")]
        public float enemyFireRate = 10;
        public float enemyProjectileSpeedKms = 10;
        public float enemyProjectileSpread = 10;
        public bool isActive;
        public void RestartSim()
        {
            enemyFireRate = ship.enemyFireRate;
            enemyProjectileSpeedKms = ship.enemyProjectileSpeedKms;
            enemyProjectileSpread = ship.enemyProjectileSpread;
            lines.returnAll();
            drawLines.Clear();
            projectiles.Clear();
        }

        public void UpdateVars()
        {
            enemyFireRate = ship.enemyFireRate;
            enemyProjectileSpeedKms = ship.enemyProjectileSpeedKms;
            enemyProjectileSpread = ship.enemyProjectileSpread; 
        }
        public void Start()
        {
            ship = transform.parent.GetComponent<ShipController>();
            _scale = ShipController._scale;
            lines.Populate(ship.transform.GetChild(4).GetChild(1).gameObject,200);
        }

        public void Update()
        {
            if (!ship.simIsActive) return;
            
            lines.returnAll();
            if (drawLines.Count > 0)
            {
                foreach (Projectile p in drawLines.ToArray())
                {
                    Color clr = ((p.curPos - p.target.transform.position).magnitude < p.velocity.magnitude * Time.fixedDeltaTime*4) ? 
                        Color.red : Color.white;
                

                    LineRenderer line = lines.getObj();
                    if (line)
                    {
                        line.SetPosition(0, p.curPos - (p.velocity * 0.01f));
                        line.SetPosition(1, p.curPos);
                        line.startColor = Color.clear;
                        line.endColor = clr;
                    }

                }
                drawLines.Clear();
            }
        }

        public void FixedUpdate()
        {
            if (!ship.simIsActive) return;
            if (firerateDelay < 0 && isActive)
            {
                FireProjectile();
                if (type == ProjectileType.Missile)
                    firerateDelay = enemyFireRate;
                else
                    firerateDelay = 1 / enemyFireRate;
            }
            else if (firerateDelay > 0) firerateDelay -= Time.fixedDeltaTime;

            UpdateProjectiles();
        }

        private void FireProjectile()
        {
            if (type == ProjectileType.Kinetic) FireKineticProjectile();
            else if (type == ProjectileType.Missile) FireMissile();
        }

        private void FireMissile()
        {
            RocketController missile = Instantiate(ship.transform.GetChild(4).GetChild(2).gameObject).GetComponent<RocketController>();
            missile.gameObject.SetActive(true);
            missile.target = ship;
            missile.transform.rotation = Quaternion.Euler(new Vector3(
                Random.Range(-1, 1) * 180 ,
                Random.Range(-1, 1) * 180 ,
                Random.Range(-1, 1) * 180 
            ));
            ship.shotsFired += 1; 
        }
        private void FireKineticProjectile()
    {
      Vector3 TgtPosAtTime(float time, Vector3 pos0, Vector3 vel0, Vector3 acc)
      { return pos0 + vel0 * time + acc * time * time * 0.5f;}
      
      Vector3 projectedPos = TgtPosAtTime(
          ship.transform.position.magnitude / ((enemyProjectileSpeedKms * _scale) * 1000), 
          ship.transform.position,
          ship._rb.velocity,
          ship.transform.up * ship.currentAcceleration);
      
      for (int i = 0; i < 3; i++)
      {
          Vector3 oldpos = projectedPos;
          projectedPos = TgtPosAtTime(
              oldpos.magnitude / ((enemyProjectileSpeedKms * _scale) * 1000), 
                    ship.transform.position,
                    ship._rb.velocity ,
                    ship.transform.up *
                    ship.currentAcceleration);
          if ((oldpos - projectedPos).magnitude < ship._box.size.y / 4) break;
      }

      projectedPos += new Vector3(
          Random.Range(-1, 1) * _scale * enemyProjectileSpread,
          Random.Range(-1, 1) * _scale * enemyProjectileSpread,
          Random.Range(-1, 1) * _scale * enemyProjectileSpread
      );

      
          Projectile p = new Projectile
          {
              curPos = Vector3.zero,
              velocity = projectedPos.normalized * ((enemyProjectileSpeedKms * _scale) * 1000),
              targetPos = projectedPos,
              dist = projectedPos.magnitude,
              target = ship
          };
          projectiles.Add(p);
          ship.shotsFired += 1; 
      
    }

    private void UpdateProjectiles()
    {
        if (projectiles.Count == 0) return;
        List<Projectile> newlist = new ();
        for (int i = 0; i < projectiles.Count; i++)
        {
            Projectile p = projectiles[i];
            Projectile projectile = p;
            bool terminate = false;
            Vector3 nextpos = p.curPos + (p.velocity * Time.fixedDeltaTime);
            if (nextpos.magnitude > p.dist+10)
                terminate = true;
            projectile.curPos = nextpos;
            
            Debug.DrawLine(Vector3.zero, p.curPos, Color.blue, Time.deltaTime);
            if ((p.curPos - p.target.transform.position).magnitude < 10)
            {
                drawLines.Add(p);
                if ((p.curPos - p.target.transform.position).magnitude < p.velocity.magnitude * Time.fixedDeltaTime*2)
                {
                    RaycastHit hit = DebugEx.TraceSphereRC(new DebugEx.TraceSettings(
                        new Ray(p.curPos, nextpos.normalized))
                        {
                            distance = p.velocity.magnitude * Time.fixedDeltaTime,
                            debugDraw = false,
                            sphereRadius = 1*_scale
                        }
                    );
                    
                    if (hit.collider)
                    {
                        nextpos *= 10;
                        p.target.RegisterHit(hit.point);
                        terminate = true;
                    }
                }
            }
            if(!terminate)
              newlist.Add(projectile);
        }

        projectiles = newlist;
    }

    }
}