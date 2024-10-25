using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseInput : MonoBehaviour
{
    [SerializeField] private float pressureForce;
    [SerializeField] private float pressureOffset;
    private Ray mouseRay;
    private RaycastHit raycastHit;
    private Vector2 offset;
    private Transform jelly;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out raycastHit))
            {
                Jellyfier jellyfier = raycastHit.collider.GetComponent<Jellyfier>();
                jelly = raycastHit.collider.transform;
                if (jellyfier != null)
                {
                    Vector3 inputPoint = raycastHit.point + (raycastHit.normal * pressureOffset);
                    jellyfier.ApplyPressureToPoint(inputPoint, pressureForce);
                    offset = jelly.position - raycastHit.point;
                    offset += Vector2.up * 1.5f;
                }
            }
        }
        if (Input.GetMouseButton(0) && jelly != null)
        {
            Vector2 newMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            jelly.position = newMousePos + offset;
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (jelly != null)
            {
                GridManager.Instance.SnapToGrid(jelly);
                jelly = null;
            }
        }
    }

}
