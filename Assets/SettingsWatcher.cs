using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsWatcher : MonoBehaviour
{
    private bool isWatching = true;
    public bool isFloat;
    public string variableName = "";
    public float rollerRatio = 1.0f;
    private TMP_InputField field;
    ShipController ship;
    private void Awake()
    {
        field = transform.GetChild(0).GetComponent<TMP_InputField>();
    }

    // Start is called before the first frame update
    void Start()
    {
        ship = FindObjectOfType<ShipController>();
        
        if (variableName.ToLower() == "_start")
            return;
      
        isWatching = false;
        float newValue = 0;
        switch (variableName.ToLower())
        {
            case "width":
                newValue = ship.shipWidth; break;
            case "length":
                newValue = ship.shipLength; break;
            case "height":
                newValue = ship.shipHeight; break;
            case "distance":
                newValue = ship.initDistanceKm; break;
            case "velocity":
                newValue = ship.shipInitVelocityKms; break;
            case "accel":
                newValue = ship.shipAccelerationG; break;
            case "rotation":
                newValue = ship.shipAngularSpeed; break;
            case "simspeed":
                newValue = ship.simSpeed; break;
            case "enemyguns":
                newValue = ship.enemyGuns; break;
            case "enemyrate":
                newValue = ship.enemyFireRate; break;
            case "projectileaccuracy":
                newValue = ship.enemyProjectileSpread; break;
            case "projectilespeed":
                newValue = ship.enemyProjectileSpeedKms; break;
            default:
                Debug.LogWarning("no variable "+variableName);
                break;
        }

        field.text = newValue.ToString();
        isWatching = true;
    }

    public void ValueUpdate()
    {
        if (!isWatching) return;
        if (variableName.ToLower() == "_start")
        {
            ship.RestartSim();
            return;
        }

        float newValue = float.Parse(field.text);
        
        if (isFloat)
        {
            newValue = Mathf.Round(newValue * 100) / 100;
        }
        if (newValue < 0)
        {
            newValue = 0;
        }
        switch (variableName.ToLower())
        {
            case "width":
                ship.shipWidth = newValue; ship.Setup(); break;
            case "length":
                ship.shipLength = newValue; ship.Setup(); break;
            case "height":
                ship.shipHeight = newValue; ship.Setup(); break;
            case "distance":
                ship.initDistanceKm = newValue; ship.Setup(); break;
            case "velocity":
                ship.shipInitVelocityKms = newValue; break;
            case "accel":
                ship.shipAccelerationG = newValue; break;
            case "rotation":
                ship.shipAngularSpeed = newValue; break;
            case "simspeed":
                newValue = Mathf.Clamp01(newValue);
                ship.simSpeed = newValue;
                break;
            case "enemyguns":
                int aValue = Mathf.Min(10,Mathf.Max(1, Mathf.RoundToInt(newValue)));
                newValue = aValue;
                ship.enemyGuns = aValue;
                ship.UpdateVars();
                break;
            case "enemyrate":
                ship.enemyFireRate = newValue; ship.UpdateVars(); break;
            case "projectileaccuracy":
                ship.enemyProjectileSpread = newValue; ship.UpdateVars(); break;
            case "projectilespeed":
                ship.enemyProjectileSpeedKms = newValue; ship.UpdateVars(); break;
            default:
                Debug.LogWarning("no variable "+variableName);
                break;
        }
        isWatching = false;
        field.text = newValue.ToString();
        isWatching = true;
    }
}
