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

namespace DefaultNamespace
{
    public class EnemyController : MonoBehaviour
    {
        [Header("Enemy params")]
        public float enemyFireRate = 10;
        public int enemyGuns = 1;
        public float enemyProjectileSpeedKms = 10;
        public float enemyProjectileSpread = 10;
        public List<GunPoint> gunPoints = new();

        
        private void Start()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                GunPoint point = transform.GetChild(i).GetComponent<GunPoint>();
                if (point)
                    gunPoints.Add(point);
            }
            UpdateVars();
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

    }
}