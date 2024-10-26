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
    private Jellyfier jellyfier;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out raycastHit))
            {
                jellyfier = raycastHit.collider.GetComponent<Jellyfier>();
                if (jellyfier != null)
                {
                    Vector3 inputPoint = raycastHit.point + (raycastHit.normal * pressureOffset);
                    jellyfier.ApplyPressureToPoint(inputPoint, pressureForce);
                    offset = jellyfier.transform.position - raycastHit.point;
                    offset += Vector2.up * 1.5f;
                }
            }
        }
        if (Input.GetMouseButton(0) && jellyfier != null)
        {
            Vector2 newMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            jellyfier.transform.position = newMousePos + offset;
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (jellyfier != null)
            {
                GridManager.Instance.SnapToGrid(jellyfier.transform);
                jellyfier.Combine();
                jellyfier = null;
            }
        }
    }

}
