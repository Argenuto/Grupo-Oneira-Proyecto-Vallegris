using UnityEngine;
using UnityEngine.Localization;

public class DialogueItemGiver : MonoBehaviour
{
    [SerializeField] private LocalizedString itemName;
    [SerializeField] private Sprite itemIcon;

    public void GiveItem()
    {
        if (ItemObtainedManager.Instance != null)
        {
            ItemObtainedManager.Instance.ShowItem(itemName.GetLocalizedString(), itemIcon);
        }
    }
}