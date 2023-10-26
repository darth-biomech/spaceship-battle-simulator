using System;
using System.Collections.Generic;
using UnityEngine;

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
