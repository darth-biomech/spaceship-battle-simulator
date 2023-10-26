using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DefaultNamespace
{
    public class SettingsRoller : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private bool mouseOver,pressing,isFloat;
        private float rollerRatio = 1.0f;
        public void OnPointerClick(PointerEventData eventData) { return; }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!ship.clickedOutside && !ship.mouseDown)
                mouseOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            mouseOver = false;
        }
    
        private TMP_InputField field;
        private ShipController ship;

        private void Start()
        {
            field = transform.parent.GetChild(0).GetComponent<TMP_InputField>();
            ship = FindObjectOfType<ShipController>();
            isFloat = transform.parent.GetComponent<SettingsWatcher>().isFloat;
            rollerRatio = transform.parent.GetComponent<SettingsWatcher>().rollerRatio;
        }

        private void Update()
        {
            if (pressing)
            {
                float mouseDelta = isFloat ? Input.GetAxis("Mouse Y")*rollerRatio : Mathf.RoundToInt(Input.GetAxis("Mouse Y") *rollerRatio * 10) ;
             
                mouseDelta += float.Parse(field.text);
                field.text = mouseDelta.ToString();
                
                if (Input.GetAxis("Fire1") < 0.2f)
                {
                    pressing = false;
                    ship.changingSetting = false;
                }
            }
            else
            {
                if(mouseOver && Input.GetAxis("Fire1") > 0.2f && !ship.changingSetting)
                {
                    pressing = true;
                    ship.changingSetting = true;
                }
            }
        }
    }
}