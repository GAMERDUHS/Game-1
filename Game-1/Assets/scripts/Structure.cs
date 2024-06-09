using UnityEngine;
using UnityEngine.EventSystems;

public class Structure : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Uimanager.Instance.OnStructureClicked(gameObject);
    }
}
