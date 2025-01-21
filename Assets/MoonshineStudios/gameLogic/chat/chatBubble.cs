using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ChatBubble : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private ContentSizeFitter contentSizeFitter;
    [SerializeField] private VerticalLayoutGroup verticalLayoutGroup;

    private void Awake()
    {
        // Ensure components are properly configured
        if (contentSizeFitter)
        {
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        if (verticalLayoutGroup)
        {
            verticalLayoutGroup.childControlHeight = true;
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childForceExpandWidth = false;
        }
    }
}
