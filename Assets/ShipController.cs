using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using DefaultNamespace;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[Serializable]
public struct Projectile
{
    public Vector3 curPos;
    public Vector3 targetPos;
    public Vector3 velocity;
    public float dist;
    public LineRenderer line;
    public ShipController target;
}

public struct HitInfo
{
    public Material hitMat;
    public MeshRenderer hitRef;
}

[Serializable]
public class GOPool<T> where T : class
{
    public List<T> objPool;
    public List<T> objActive;
    public GameObject template;
    public int maxPoolSize;

    public void Populate(GameObject _template, int count = 1,int size = 500)
    {
        if (_template)
        {
            T templateComponent = _template.GetComponent<T>();
            if (templateComponent == null)
            {
                Debug.LogError("template doesn't have '"+typeof(T)+"' component!");
                return;
            }
            maxPoolSize = Mathf.Max(count,size);
            template = _template;
            template.SetActive(true);
            for (int i = 0; i < count; i++)
            {
                GameObject newobj = GameObject.Instantiate(template);
                returnObj(newobj.GetComponent<T>());
            }

            template.SetActive(false);
        }
        else
        {
            Debug.LogError("no template");
        }
    }
    
    public T getObj()
    {
        T obj = null;
        
        if (objPool.Count == 0)
        {
            if( objActive.Count <= maxPoolSize)
                obj = GameObject.Instantiate(template).GetComponent<T>();
            else
            {
                obj = objActive[0];
                objActive.Remove(objActive[0]);
            }
        }
        else
        {
            obj = objPool[objPool.Count - 1];
            objPool.Remove(obj);
        }

        T result = obj;
        objActive.Add(result);
        GameObject tr = (obj as Component)?.gameObject;
        if (tr != null) tr.SetActive(true);
        return obj;
    }
    public void returnObj(T obj)
    {
        
        if (objActive.Contains(obj))
            objActive.Remove(obj);
        
        objPool.Add(obj);

        GameObject tr = (obj as Component)?.gameObject;
        if (tr != null)
        {
            tr.gameObject.SetActive(false);
            tr.transform.SetParent(null);
            tr.transform.position = Vector3.zero;
            tr.transform.rotation = Quaternion.Euler(Vector3.zero);
            tr.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.LogError("No transform!");
        }
    }
    public void returnAll()
    {
        if (objActive.Count > 0 )
        {
            T[] actives = objActive.ToArray();
            foreach (T obj in actives)
            {
                returnObj(obj);
            }
        }
    }

}

public class ShipController : MonoBehaviour
{
    public TextMeshProUGUI field;
    public bool simIsActive;
    public int shotsFired, shotsHit;
    internal const float _scale = 0.001f;
    internal BoxCollider _box;
    internal Rigidbody _rb;
    private Transform _ship, _exhaust,_cameraDist,_cameraRot,_cameraPos,_attacker;
    public float simSpeed = 1;
    public bool maneuver;
    
    [Header("Standoff params")]
    public float initDistanceKm = 200;
    public float shipInitVelocityKms = 30;
    
    [Header("Ship dimensions in meters")]
    public float shipWidth = 20;
    public float shipLength = 20;
    public float shipHeight = 220;
    
    [Header("Ship maneuverability")]
    public float shipAccelerationG = 10;
    public float shipAngularSpeed = 10;
    
    [Header("Enemy params")]
    public float enemyFireRate = 10;
    public int enemyGuns = 1;
    public float enemyProjectileSpeedKms = 10;
    public float enemyProjectileSpread = 10;

    internal float currentAcceleration = 0;
    private float rotDelay = 0;
    private Quaternion newRot;
    
    public List<HitInfo> registeredHits = new();
    public Vector3 _velocity;
    public GOPool<MeshRenderer> hits = new();
    public bool changingSetting,clickedOutside,mouseDown;
    public List<GunPoint> gunPoints = new();
    [SerializeField] AnimationCurve rotateCurve;
    void Awake()
    {
        Setup();
    }

    private void Start()
    {
        _exhaust.gameObject.SetActive(false);
        hits.Populate(transform.GetChild(4).GetChild(0).gameObject,200,100);

        for (int i = 0; i < transform.childCount; i++)
        {
            GunPoint point = transform.GetChild(i).GetComponent<GunPoint>();
            if (point)
                gunPoints.Add(point);
        }
    }

    public void UpdateVars()
    {
        for (int i = 0; i < gunPoints.Count; i++)
        {
            if (i < enemyGuns)
            {
                gunPoints[i].UpdateVars();
                gunPoints[i].isActive = true;
            }
            else
                gunPoints[i].isActive = false;
        }
    }
    public void RestartSim()
    {
        shotsFired = 0;
        shotsHit = 0;
        _exhaust.gameObject.SetActive(true);
        simIsActive = false;
        hits.returnAll();
        Setup();
        simIsActive = true;
        for (int i = 0; i < gunPoints.Count; i++)
        {
            gunPoints[i].RestartSim();
            gunPoints[i].isActive = i < enemyGuns;
        }

        transform.position = new Vector3(0, 0, initDistanceKm * (_scale * 1000));
        _rb.velocity = transform.up * shipInitVelocityKms * _scale * 1000;
    }
    public void Setup()
    {
        if (!_cameraPos)
        {
            _rb = GetComponent<Rigidbody>();
            _box = GetComponent<BoxCollider>();
            _ship = transform.GetChild(0);
            _exhaust = transform.GetChild(1);
            _cameraPos = transform.GetChild(2);
            _cameraRot = _cameraPos.GetChild(0);
            _cameraDist = _cameraRot.GetChild(0);
            _attacker = GameObject.Find("Attacker").transform;
        }

        SetShipParams();
        if (!simIsActive || !Application.isPlaying)
        {
            transform.position = new Vector3(0, 0, initDistanceKm * (_scale * 1000));
        }
        _cameraPos.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        _attacker.localScale = Vector3.one * transform.position.magnitude *0.01f;
    }

    private void OnValidate()
    {
        if (Application.isPlaying) SetShipParams();
        else Setup();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSimStatus();
        _attacker.localScale = Vector3.one * transform.position.magnitude*0.01f;
        simSpeed = Mathf.Clamp01(simSpeed);
        Time.timeScale = simSpeed;
        _cameraDist.localPosition = new Vector3(
            0,
            0,
            Mathf.Clamp(_cameraDist.localPosition.z + (Input.GetAxis("Mouse ScrollWheel")*(_scale/0.001f)), -100, -0.003f)
            );
        Quaternion rot = Quaternion.Euler(Input.GetAxis("Mouse Y")*5,Input.GetAxis("Mouse X")*-5,0);
        if (Input.GetAxis("Fire1") > 0.2f)
        {
            if (!mouseDown)
            {
                mouseDown = true;
                clickedOutside = !IsPointerOverUIObject();
            }
            if(clickedOutside)
                _cameraRot.localRotation *= rot;
        }
        else if (mouseDown)
        {
            mouseDown = false;
            clickedOutside = false;
        }
        UpdateHits();
    }

    private void UpdateSimStatus()
    {
        field.text = "Distance from the enemy: " + Convert.ToInt32(transform.position.magnitude / (_scale * 1000))
                     + "km, shots fired: " + shotsFired
                     + ", shots hit: " + shotsHit
            ;
    }

    bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
    
    float totalRotateTime = 2f; 
    float currentRotateTime = 0f;
    private Quaternion oldRot;
    private void FixedUpdate()
    {
        if (simIsActive)
        {
            if (rotDelay < 0.00001f)
            {
                float RndF() { return (360 * Random.value) - 180; }

                if (maneuver)
                {
                    newRot = Quaternion.Euler(RndF(), RndF(), RndF());
                    oldRot = transform.rotation;
                    totalRotateTime = Quaternion.Angle(newRot, oldRot) / shipAngularSpeed;
                    currentRotateTime = 0;
                }
                currentAcceleration = (shipAccelerationG * (9.80665f * _scale)) * Random.Range(0.5f,1);
                rotDelay = Random.Range(totalRotateTime, totalRotateTime+5);
            }

            rotDelay -= Time.fixedDeltaTime;
            _rb.velocity = _velocity;
            _velocity += (transform.up * currentAcceleration) * Time.fixedDeltaTime;
            _rb.angularVelocity = Vector3.zero;
            
            currentRotateTime += Time.fixedDeltaTime;
            var normalizedProgress = Mathf.Clamp01(currentRotateTime / totalRotateTime) ; // 0-1
            var easing = rotateCurve.Evaluate(normalizedProgress);
            transform.rotation = Quaternion.Lerp(oldRot,newRot,easing);
          //  transform.rotation = Quaternion.RotateTowards(transform.rotation, newRot, shipAngularSpeed * Time.fixedDeltaTime);
            if ((transform.rotation.eulerAngles - newRot.eulerAngles).magnitude < 1)
            {
                rotDelay = 0;
            }
            _rb.angularVelocity = Vector3.zero;
            
        }
        _cameraPos.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
    }

    public void RegisterHit(Vector3 hit)
    {
        MeshRenderer mark = hits.getObj();
        Transform mT = mark.transform;
        shotsHit += 1;

        mT.parent = transform.GetChild(3);
        mT.position = hit;
        mT.localScale = Vector3.one * _scale * 8;
        registeredHits.Add(new HitInfo()
        {
            hitMat = mark.material,
            hitRef = mark
        });
    }

    private void UpdateHits()
    {
        HitInfo[] dealtHits = registeredHits.ToArray();
        foreach (HitInfo hit in dealtHits)
        {
            Color clr = hit.hitMat.GetColor("_Color");
            hit.hitMat.SetColor( "_Color",Color.Lerp(clr, Color.black, Time.fixedDeltaTime/4)); 
            if (clr.r + clr.b + clr.g < 0.01f)
            {
                registeredHits.Remove(hit);
                hit.hitMat.SetColor( "_Color",Color.red);
                hits.returnObj(hit.hitRef);
            }
                
        }
    }
    
    public void SetShipParams()
    {
        if(_box)
            _box.size = new Vector3(shipWidth, shipHeight, shipLength) * _scale;
        if(_ship)
            _ship.localScale = new Vector3(shipWidth, shipHeight, shipLength) * _scale;
        if(_exhaust)
            _exhaust.localPosition = new Vector3(0, -shipHeight/2, 0) * _scale;
        _velocity = transform.up * shipInitVelocityKms * _scale;
        //accelerationMax = shipAcceleration * _scale;
        
      //  firerateDelay = shipFireRate / Time.deltaTime;
    }
}
