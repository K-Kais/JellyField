using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseInput : MonoBehaviour
{
    [SerializeField] private float pressureForce;
    [SerializeField] private float pressureOffset;
    private Ray mouseRay;
    private RaycastHit raycastHit;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out raycastHit))
            {
                Jellyfier jellyfier = raycastHit.collider.GetComponent<Jellyfier>();
                if (jellyfier != null)
                {
                    Vector3 inputPoint = raycastHit.point + (raycastHit.normal * pressureOffset);
                    jellyfier.ApplyPressureToPoint(inputPoint, pressureForce);
                }
            }
        }
    }
}
