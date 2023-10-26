using System;
using System.Collections.Generic;
using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

//TODO: Rockets and lasers, point defense
public class ShipController : MonoBehaviour
{
    /// <summary>
    /// Scale of the whole simulation, since Unity is kind of bad at space-worthy distances.<br/><br/>
    /// 
    /// Default scale is such that 1 unity unit (meter) corresponds to 1 kilometer, which,
    /// given unity's limits, gives the approximate max distance sim can work at being ~25000 km.
    /// Which can be small for some of the tests, but it's an Unity limitation since going
    /// smaller scale than 0.001 seems to result in more precision errors anyway.
    /// </summary>
    internal const float _scale = 0.001f;
    
    public bool isMainShip = true;
    public TextMeshProUGUI field;
    public static bool simIsActive;
    public int shotsFired, shotsHit;
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
    public float enemyFireRate = 20;
    public int enemyGuns = 1;
    public float enemyProjectileSpeedKms = 100;
    public float enemyProjectileSpread = 100;

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
            
                var normalizedProgress = Mathf.Clamp01(currentRotateTime / totalRotateTime); // 0-1
                var easing = rotateCurve.Evaluate(normalizedProgress);
            
                transform.rotation = Quaternion.Lerp(oldRot, newRot, easing);

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
