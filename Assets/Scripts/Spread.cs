using UnityEngine;
using TMPro;
using UnityEngine.EventSystems; 

[RequireComponent(typeof(TMP_Text))]
public class TextMeshProHoverSpread2D : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float spreadMagnitude = 0.5f; 
    public float hoverRadius = 2.0f;     
    public float animationSpeed = 10f;   

    private TMP_Text tmpText;
    private Vector3[] initialVertices;
    private Vector3[] targetVertices;
    private bool isHovering = false;
    private Vector3 mouseWorldPos; 

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        tmpText.ForceMeshUpdate(); 
        StoreInitialVertices();
    }

    void Update()
    {
        if (isHovering)
        {
           
            if (tmpText is TextMeshProUGUI) 
            {
                //  pixel screen space
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    tmpText.rectTransform, Input.mousePosition,
                    tmpText.canvas.worldCamera, out localPoint);

                // localPoint ke world space dari TextMeshPro
                mouseWorldPos = tmpText.rectTransform.TransformPoint(localPoint);
            }
            else 
            {
                
                RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    mouseWorldPos = hit.point;
                }
                else
                {
                    isHovering = false; 
                }
            }
        }

        UpdateTextSpread();
    }

    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    void StoreInitialVertices()
    {
        tmpText.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmpText.textInfo;
        initialVertices = new Vector3[textInfo.characterCount * 4];

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (textInfo.characterInfo[i].isVisible)
            {
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                TMP_MeshInfo meshInfo = textInfo.meshInfo[textInfo.characterInfo[i].materialReferenceIndex];

                for (int j = 0; j < 4; j++)
                {
                    initialVertices[vertexIndex + j] = meshInfo.vertices[vertexIndex + j];
                }
            }
        }
        targetVertices = (Vector3[])initialVertices.Clone();
    }

    void UpdateTextSpread()
    {
        tmpText.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmpText.textInfo;

        if (!isHovering)
        {
            targetVertices = (Vector3[])initialVertices.Clone();
        }
        else
        {
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int vertexIndex = charInfo.vertexIndex;
                TMP_MeshInfo meshInfo = textInfo.meshInfo[charInfo.materialReferenceIndex];

                //  lokal TextMeshPro
                Vector3 charCenterLocal = (meshInfo.vertices[vertexIndex] + meshInfo.vertices[vertexIndex + 1] +
                                          meshInfo.vertices[vertexIndex + 2] + meshInfo.vertices[vertexIndex + 3]) / 4f;

                Vector3 charCenterWorld = tmpText.transform.TransformPoint(charCenterLocal);

                float distance = Vector2.Distance(new Vector2(mouseWorldPos.x, mouseWorldPos.y), new Vector2(charCenterWorld.x, charCenterWorld.y));

                if (distance < hoverRadius)
                {
                    float normalizedDistance = 1f - (distance / hoverRadius);
                    Vector3 spreadDirection = (charCenterWorld - mouseWorldPos).normalized;

                    // Fallback 
                    if (distance < 0.01f || spreadDirection == Vector3.zero)
                    {
                       
                        spreadDirection = (charCenterWorld - tmpText.transform.position).normalized;
                       
                        if (spreadDirection == Vector3.zero) spreadDirection = Vector3.up;
                    }

                    Vector3 spreadOffset = spreadDirection * normalizedDistance * spreadMagnitude;

                    for (int j = 0; j < 4; j++)
                    {
                        targetVertices[vertexIndex + j] = initialVertices[vertexIndex + j] + spreadOffset;
                    }
                }
                else
                {
                    for (int j = 0; j < 4; j++)
                    {
                        targetVertices[vertexIndex + j] = initialVertices[vertexIndex + j];
                    }
                }
            }
        }

        for (int i = 0; i < textInfo.characterCount * 4; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i / 4];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = i;

            Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;
            destinationVertices[vertexIndex] = Vector3.Lerp(destinationVertices[vertexIndex], targetVertices[vertexIndex], Time.deltaTime * animationSpeed);
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }

        tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
    }
}