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
    private Toggle tumbler;
    ShipController ship;
    private void Awake()
    {
        field = transform.GetChild(0).GetComponent<TMP_InputField>();
        tumbler = transform.GetComponent<Toggle>();
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
            case "maneuver":
                newValue = ship.maneuver?1:0; break;
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

        SetValue(newValue);
        isWatching = true;
    }

    private void SetValue(float newValue)
    {
        isWatching = false;
        if (field)
            field.text = newValue.ToString();
        else if(tumbler)
            tumbler.SetIsOnWithoutNotify(newValue > 0.1f);
        isWatching = true;
    }

    private float GetValue()
    {
        float result = 0;
        if (field) result = float.Parse(field.text);
        else if (tumbler) result = tumbler.isOn ? 1 : 0;
        return result;
    }
    
    public void ValueUpdate()
    {
        if (!isWatching) return;
        if (variableName.ToLower() == "_start")
        {
            ship.RestartSim();
            return;
        }

        float newValue = GetValue();
        
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
            case "maneuver":
                ship.maneuver = newValue > 0.99f; break;
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
        SetValue(newValue);
    }
}
